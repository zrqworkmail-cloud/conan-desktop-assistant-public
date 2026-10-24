using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Windows;
using Forms=System.Windows.Forms;

// Program assets remain beside the executable; personal state has one stable home.
static class UserData {
 public static readonly string[] SettingsFiles={"settings.ini","assistant.json","skills.json","advanced-skills.json","voice-settings.json"};
 public static string DirectoryFor(string appFolder){return Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1"?appFolder:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ConanDesktopAssistant");}
 public static bool HasSettings(string folder){return SettingsFiles.Any(name=>File.Exists(Path.Combine(folder,name)));}
 static string[] ImportFiles(string folder){var files=SettingsFiles.Where(n=>File.Exists(Path.Combine(folder,n))).ToList();string calendars=Path.Combine(folder,"calendars");if(Directory.Exists(calendars))foreach(string p in Directory.GetFiles(calendars,"*.json")){int year;if(Int32.TryParse(Path.GetFileNameWithoutExtension(p),out year))files.Add(Path.Combine("calendars",Path.GetFileName(p)));}return files.ToArray();}
 static void Validate(string folder,string[] files){
  if(!files.Any(n=>SettingsFiles.Contains(n)))throw new ArgumentException("所选文件夹没有旧版设置，请选择旧版 ConanPet.exe 所在的文件夹。");
  var json=new JavaScriptSerializer{MaxJsonLength=8388608};
  foreach(string name in files){string p=Path.Combine(folder,name);if(new FileInfo(p).Length>8388608)throw new ArgumentException("设置文件过大："+name);string text=File.ReadAllText(p);bool valid;
   if(name=="settings.ini")valid=text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Any(s=>s.StartsWith("scale=")||s.StartsWith("roam=")||s.StartsWith("terminalTheme="));
   else if(name=="skills.json"){var d=json.Deserialize<SkillData>(text);valid=d!=null&&d.Focus!=null&&d.Reminders!=null&&d.Notes!=null;}
   else if(name=="advanced-skills.json"){var d=json.Deserialize<AdvancedData>(text);valid=d!=null&&d.Tasks!=null&&d.Files!=null;}
   else if(name=="voice-settings.json"){var d=json.Deserialize<VoiceSettings>(text);valid=d!=null&&!Double.IsNaN(d.Volume)&&!Double.IsInfinity(d.Volume)&&d.Volume>=0&&d.Volume<=1;}
   else if(name=="assistant.json")valid=json.Deserialize<AssistantConfig>(text)!=null;
   else{int year=Int32.Parse(Path.GetFileNameWithoutExtension(name),CultureInfo.InvariantCulture);SkillRuntime.ParseCalendar(text,year);valid=true;}
   if(!valid)throw new ArgumentException("设置格式不完整，未导入："+name);
  }
 }
 static void CopyImported(string source,string destination,string name){
  if(name!="assistant.json"){File.Copy(source,destination,true);return;}
  var json=new JavaScriptSerializer{MaxJsonLength=8388608};var data=json.Deserialize<AssistantConfig>(File.ReadAllText(source));if(data.Monitor==null)data.Monitor=new MonitorConfig();if(data.Monitor.Quota==null)data.Monitor.Quota=new CodexQuotaConfig();data.Monitor.ClashHistory=false;data.Monitor.CodexUsageHistory=false;data.Monitor.HistoryCodexHome="";data.Monitor.Quota.Executable="";data.Monitor.Quota.Home="";data.Monitor.Quota.Enabled=false;File.WriteAllText(destination,json.Serialize(data),new UTF8Encoding(false));
 }
 public static int ImportMissing(string source,string target){
  string[] files=ImportFiles(source);Validate(source,files);Directory.CreateDirectory(target);int copied=0;
  foreach(string name in files){string to=Path.Combine(target,name);if(File.Exists(to))continue;Directory.CreateDirectory(Path.GetDirectoryName(to));CopyImported(Path.Combine(source,name),to,name);copied++;}return copied;
 }
 public static void QueueImport(string source,string target){
  if(String.Equals(Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar),Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("所选文件夹已经是当前数据目录，无需导入。");
  string[] files=ImportFiles(source);Validate(source,files);Directory.CreateDirectory(target);string pending=Path.Combine(target,"pending-import");if(Directory.Exists(pending))throw new ArgumentException("已有待导入的数据，请先退出并重新打开桌宠，或取消待导入。");
  string stage=Path.Combine(target,"import-stage-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
  try{foreach(string name in files){string to=Path.Combine(stage,name);Directory.CreateDirectory(Path.GetDirectoryName(to));CopyImported(Path.Combine(source,name),to,name);}Validate(stage,files);Directory.Move(stage,pending);}
  finally{if(Directory.Exists(stage))Directory.Delete(stage,true);}
 }
 public static string ApplyPending(string target){
  string pending=Path.Combine(target,"pending-import");if(!Directory.Exists(pending))return null;string[] files=ImportFiles(pending);Validate(pending,files);
  string backup=Path.Combine(target,"backups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(backup);
  foreach(string name in files){string existing=Path.Combine(target,name);if(File.Exists(existing)){string copy=Path.Combine(backup,name);Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Copy(existing,copy);}}
  var applied=new List<string>();try{
   foreach(string name in files){string destination=Path.Combine(target,name);Directory.CreateDirectory(Path.GetDirectoryName(destination));string temp=destination+".import.tmp";CopyImported(Path.Combine(pending,name),temp,name);if(File.Exists(destination))File.Replace(temp,destination,null);else File.Move(temp,destination);applied.Add(name);}
  }catch{foreach(string name in applied){string old=Path.Combine(backup,name),destination=Path.Combine(target,name);if(File.Exists(old))File.Copy(old,destination,true);else File.Delete(destination);}throw;}
  Directory.Delete(pending,true);return backup;
 }
 public static void CancelPending(string target){string pending=Path.Combine(target,"pending-import");if(Directory.Exists(pending))Directory.Delete(pending,true);}
 public static void Prepare(string appFolder){
  if(Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1")return;string target=DirectoryFor(appFolder);Directory.CreateDirectory(target);ApplyPending(target);string marker=Path.Combine(target,"initialized.txt");if(File.Exists(marker))return;
  try{if(!HasSettings(target)){
   if(HasSettings(appFolder))ImportMissing(appFolder,target);
   else if(MessageBox.Show("新版将设置固定保存在：\n"+target+"\n\n是否选择旧版程序文件夹，导入之前的设置、便签、提醒与音量？\n首次导入一次即可，以后更新无需再复制。也可稍后在“数据与备份”中导入。","首次升级 · 导入旧版设置",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes){using(var picker=new Forms.FolderBrowserDialog{Description="选择旧版 ConanPet.exe 所在的文件夹"})if(picker.ShowDialog()==Forms.DialogResult.OK)ImportMissing(picker.SelectedPath,target);}
  }
  }catch(Exception e){if(!(e is IOException)&&!(e is UnauthorizedAccessException)&&!(e is ArgumentException)&&!(e is InvalidOperationException))throw;MessageBox.Show("旧版数据未能完整导入，原文件已保留。可进入工作台的“数据与备份”重新选择旧目录。\n"+e.Message,"导入设置",MessageBoxButton.OK,MessageBoxImage.Warning);}
  File.WriteAllText(marker,"User data v1\n"+DateTime.UtcNow.ToString("O"),new UTF8Encoding(false));
 }
}
