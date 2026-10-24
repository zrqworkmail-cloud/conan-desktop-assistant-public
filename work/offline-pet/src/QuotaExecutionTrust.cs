using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
public sealed class QuotaAuthorizationException:IOException {public QuotaAuthorizationException(string message):base(message){}}
public class QuotaProgramIdentity {public string Executable,Home,Sha256,Machine,User;}
public sealed class QuotaProgramPermit:IDisposable {public readonly QuotaProgramIdentity Identity;readonly FileStream file;internal QuotaProgramPermit(QuotaProgramIdentity identity,FileStream stream){Identity=identity;file=stream;}public void Dispose(){file.Dispose();}}
// Deliberately separate from importable assistant.json. Only an explicit UI grant writes this file.
public sealed class QuotaExecutionTrust {
 readonly string path;readonly JavaScriptSerializer json=new JavaScriptSerializer();
 public QuotaExecutionTrust(string dataFolder){path=Path.Combine(dataFolder,"quota-program-trust.dat");}
 public static string LocalPath(string value){if(String.IsNullOrWhiteSpace(value)||value.Length<3||!Char.IsLetter(value[0])||value[1]!=':'||value[2]!='\\')throw new ArgumentException("程序和数据目录必须为本机磁盘的绝对路径，不接受网络或设备路径。");string full=Path.GetFullPath(value);if(new DriveInfo(Path.GetPathRoot(full)).DriveType==DriveType.Network)throw new ArgumentException("不允许从网络磁盘启动额度程序。");return full;}
 static string Digest(Stream stream){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
 static string HomePath(string value){if(String.IsNullOrWhiteSpace(value))value=Environment.GetEnvironmentVariable("CODEX_HOME");if(String.IsNullOrWhiteSpace(value))value=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");value=LocalPath(value);if(!Directory.Exists(value))throw new IOException("Codex数据目录不存在，请在额度页选择正确目录。");return value;}
 public QuotaProgramIdentity Inspect(string exe,string home){exe=CodexQuotaReader.FindExecutable(exe);home=HomePath(home);using(var file=new FileStream(exe,FileMode.Open,FileAccess.Read,FileShare.Read))return new QuotaProgramIdentity{Executable=exe,Home=home,Sha256=Digest(file),Machine=LocalProtection.Machine,User=LocalProtection.User};}
 public void Approve(QuotaProgramIdentity identity){var actual=Inspect(identity.Executable,identity.Home);if(actual.Sha256!=identity.Sha256)throw new IOException("程序在确认期间已发生变化，请重新核对后授权。");Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllBytes(temp,LocalProtection.Protect(Encoding.UTF8.GetBytes(json.Serialize(actual))));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
 public void Revoke(){if(File.Exists(path))File.Delete(path);}
 public QuotaProgramPermit OpenVerified(string exe,string home){QuotaProgramIdentity approved=null;try{if(File.Exists(path)&&new FileInfo(path).Length<=16384)approved=json.Deserialize<QuotaProgramIdentity>(Encoding.UTF8.GetString(LocalProtection.Unprotect(File.ReadAllBytes(path))));}catch(IOException){}catch(ArgumentException){}catch(CryptographicException){}if(approved==null)throw new QuotaAuthorizationException("额度程序尚未授权，请在额度页点击“授权并连接额度服务”。");if(approved.Machine!=LocalProtection.Machine||approved.User!=LocalProtection.User)throw new QuotaAuthorizationException("此授权不属于当前电脑或Windows账户，请重新确认。");exe=CodexQuotaReader.FindExecutable(exe);home=HomePath(home);if(!String.Equals(exe,approved.Executable,StringComparison.OrdinalIgnoreCase)||!String.Equals(home.TrimEnd('\\'),approved.Home==null?null:approved.Home.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new QuotaAuthorizationException("额度程序或数据目录与授权记录不一致，请重新确认。");var stream=new FileStream(exe,FileMode.Open,FileAccess.Read,FileShare.Read);try{if(Digest(stream)!=approved.Sha256)throw new QuotaAuthorizationException("额度程序内容已变化，已暂停启动；请核对更新后重新授权。");return new QuotaProgramPermit(approved,stream);}catch{stream.Dispose();throw;}}
}
