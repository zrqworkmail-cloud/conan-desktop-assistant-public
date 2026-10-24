using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
internal static class LimiterSecurity {
 internal static readonly string[] Files={"ConanBandwidth.exe","WinDivert.dll","WinDivert64.sys"};
 internal static string Expected(string file){return file==Files[0]?LimiterHashes.Helper:file==Files[1]?LimiterHashes.Dll:LimiterHashes.Driver;}
 sealed class Lease:IDisposable {internal readonly List<FileStream> Streams=new List<FileStream>();public void Dispose(){foreach(var stream in Streams)stream.Dispose();}}
 internal static IDisposable Verify(string folder){folder=QuotaExecutionTrust.LocalPath(folder);var lease=new Lease();try{foreach(string file in Files){string path=Path.Combine(folder,file);if(!File.Exists(path)||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new IOException("限速组件缺失或使用了链接："+file);var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);lease.Streams.Add(stream);using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=Expected(file))throw new IOException("限速组件校验失败，已阻止管理员启动："+file);}return lease;}catch{lease.Dispose();throw;}}
 static string Quote(string value){return "'"+value.Replace("'","''")+"'";}
 internal static string Script(string folder,int parent,double rate,string eventId){string template;using(var resource=typeof(LimiterSecurity).Assembly.GetManifestResourceStream("Conan.LimiterBootstrap")){if(resource==null)throw new IOException("缺少内置的受保护启动器。");using(var reader=new StreamReader(resource))template=reader.ReadToEnd();}string expected=String.Join(";",Files.Select(file=>Quote(file)+"="+Quote(Expected(file))).ToArray());return "$source="+Quote(QuotaExecutionTrust.LocalPath(folder))+"\n$expected=@{"+expected+"}\n$helperArgs=@("+Quote(parent.ToString(System.Globalization.CultureInfo.InvariantCulture))+","+Quote(rate.ToString(System.Globalization.CultureInfo.InvariantCulture))+","+Quote(eventId)+")\n"+template;}
 internal static string PowerShell{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe");}}
}
