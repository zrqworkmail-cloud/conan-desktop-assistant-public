using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Forms=System.Windows.Forms;
public class AdvancedRuntime:IDisposable {
 readonly string path;readonly JavaScriptSerializer json=new JavaScriptSerializer();
 public readonly AdvancedEngine Engine;public readonly ClipboardHistory Clipboard=new ClipboardHistory();public string LoadError,HotkeyStatus="全局快捷键未启用";
 public event Action SettingsChanged;uint sequence;bool clipboardWasEnabled;object voice;
 class FileStamp {public bool Exists;public long Length;public DateTime Time;public string Error;}
 readonly Dictionary<string,FileStamp> stamps=new Dictionary<string,FileStamp>();DateTime lastFiles=DateTime.MinValue;
 [DllImport("user32.dll")]static extern uint GetClipboardSequenceNumber();
 public AdvancedRuntime(string home){
  path=Path.Combine(home,"advanced-skills.json");AdvancedData data=null;
  if(File.Exists(path))try{data=json.Deserialize<AdvancedData>(File.ReadAllText(path));if(data==null||data.Tasks==null||data.Files==null)throw new ArgumentException("数据结构不完整");}catch(Exception e){if(!(e is IOException)&&!(e is ArgumentException)&&!(e is UnauthorizedAccessException))throw;LoadError="扩展技能数据读取失败，已保留原文件："+e.Message;}
  Engine=new AdvancedEngine(data??new AdvancedData());
 }
 public void Save(){if(LoadError!=null)throw new IOException(LoadError);var temp=path+".tmp";File.WriteAllText(temp,json.Serialize(Engine.Data),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);Engine.Dirty=false;if(SettingsChanged!=null)SettingsChanged();}
 public void SetClipboardEnabled(bool enabled){Engine.Data.ClipboardEnabled=enabled;Clipboard.SetEnabled(enabled);sequence=GetClipboardSequenceNumber();clipboardWasEnabled=enabled;Save();}
 public void ClearClipboard(){Clipboard.Items.Clear();sequence=GetClipboardSequenceNumber();}
 public void AddFile(string target){
  if(!File.Exists(target)||target.StartsWith(@"\\"))throw new ArgumentException("请选择一个存在的本机文件。");
  var full=Path.GetFullPath(target);if(Engine.Data.Files.Any(f=>String.Equals(f.Path,full,StringComparison.OrdinalIgnoreCase)))return;
  if(Engine.Data.Files.Count>=20)throw new ArgumentException("最多监控20个文件。");
  var item=new FileWatchSetting{Id=Guid.NewGuid().ToString("N"),Path=full};Engine.Data.Files.Add(item);stamps[item.Id]=ReadStamp(full);Save();
 }
 public void RemoveFile(string id){Engine.Data.Files.RemoveAll(f=>f.Id==id);stamps.Remove(id);Save();}
 static FileStamp ReadStamp(string path){try{var f=new FileInfo(path);return new FileStamp{Exists=f.Exists,Length=f.Exists?f.Length:0,Time=f.Exists?f.LastWriteTimeUtc:DateTime.MinValue};}catch(IOException e){return new FileStamp{Error=e.Message};}catch(UnauthorizedAccessException e){return new FileStamp{Error=e.Message};}}
 public List<SkillAlert> Tick(DateTime now){
  var result=new List<SkillAlert>();if(LoadError!=null)return result;
  result.AddRange(Engine.Tick(now));
  bool enabled=Engine.Data.ClipboardEnabled;
  if(enabled!=clipboardWasEnabled){Clipboard.SetEnabled(enabled);sequence=GetClipboardSequenceNumber();clipboardWasEnabled=enabled;}
  if(enabled){uint next=GetClipboardSequenceNumber();if(next!=sequence){try{if(Forms.Clipboard.ContainsText())Clipboard.Capture(Forms.Clipboard.GetText());sequence=next;}catch(ExternalException){}}}
  if((now-lastFiles).TotalSeconds>=3){lastFiles=now;
   foreach(var file in Engine.Data.Files){var value=ReadStamp(file.Path);FileStamp before;
    if(stamps.TryGetValue(file.Id,out before)&&value.Error==null&&before.Error==null&&(value.Exists!=before.Exists||value.Length!=before.Length||value.Time!=before.Time))result.Add(new SkillAlert{Key="file:"+file.Id,Text="文件"+(!value.Exists?"已删除":!before.Exists?"已恢复":"有更新")+"："+Path.GetFileName(file.Path),Row=7});
    if(value.Error!=null&&(before==null||before.Error!=value.Error))result.Add(new SkillAlert{Key="file:"+file.Id,Text="文件暂时无法读取："+Path.GetFileName(file.Path),Row=5});
    stamps[file.Id]=value;
   }
  }
  if(Engine.Dirty)Save();return result;
 }
 public void Speak(string text){
  if(String.IsNullOrWhiteSpace(text))return;if(text.Length>2000)throw new ArgumentException("每次最多朗读2000字。");
  try{if(voice==null){var type=Type.GetTypeFromProgID("SAPI.SpVoice");if(type==null)throw new ArgumentException("本机未安装可用的Windows语音引擎。");voice=Activator.CreateInstance(type);}voice.GetType().InvokeMember("Speak",BindingFlags.InvokeMethod,null,voice,new object[]{text,19});}
  catch(COMException){throw new ArgumentException("Windows语音引擎无法播报，请检查系统语音组件。");}catch(TargetInvocationException){throw new ArgumentException("Windows语音引擎无法播报，请检查系统语音组件。");}
 }
 public void StopSpeech(){if(voice!=null)try{voice.GetType().InvokeMember("Speak",BindingFlags.InvokeMethod,null,voice,new object[]{"",3});}catch(COMException){}catch(TargetInvocationException){}}
 public static string StorageStatus(){
  var lines=new List<string>();var power=Forms.SystemInformation.PowerStatus;string battery=power.BatteryChargeStatus==Forms.BatteryChargeStatus.NoSystemBattery?"未检测到电池":Math.Round(power.BatteryLifePercent*100)+"% · "+(power.PowerLineStatus==Forms.PowerLineStatus.Online?"已接通电源":"使用电池");lines.Add("电池 / 电源    "+battery);lines.Add("");
  foreach(var drive in DriveInfo.GetDrives().Where(d=>d.DriveType==DriveType.Fixed))try{if(drive.IsReady)lines.Add(drive.Name+"    可用 "+(drive.AvailableFreeSpace/1073741824.0).ToString("F1")+" GiB / "+(drive.TotalSize/1073741824.0).ToString("F1")+" GiB");}catch(IOException){}catch(UnauthorizedAccessException){}
  return String.Join(Environment.NewLine,lines.ToArray());
 }
 public void Dispose(){StopSpeech();Clipboard.SetEnabled(false);if(voice!=null){Marshal.FinalReleaseComObject(voice);voice=null;}}
}
