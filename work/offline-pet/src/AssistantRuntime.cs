using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;

public class Shortcut {public string Name{get;set;}public string Path{get;set;}public override string ToString(){return Name+"  —  "+Path;}}
public class BubbleAppearance {public string Style="paper",Position="auto";public int FontSize=15,Width=320;}
public class AssistantConfig {public BubbleAppearance BubbleAppearance=new BubbleAppearance();public List<Shortcut> Shortcuts=new List<Shortcut>();public MonitorConfig Monitor=new MonitorConfig();public bool Bubbles=true;public double CpuThreshold=80;public double MemoryThreshold=2048;public List<double> ProcessCpuPoints,ProcessMemoryPoints;public bool ProcessAlerts=true,ProcessExitAlerts=true,CodexAlerts=true,NetworkAlerts=true;}
public class WatchedProcess {
 public int Id;public string Name;public DateTime Start;public TimeSpan Cpu;public DateTime Sampled;public double CpuPercent,MemoryMiB;public bool Ended;public int HighSamples;public readonly Dictionary<string,int> AlertStreaks=new Dictionary<string,int>();
 public override string ToString(){return Name+" ["+Id+"]  "+(Ended?"已退出":String.Format("CPU {0:F1}%  内存 {1:F0} MiB",CpuPercent,MemoryMiB));}
}
public class AssistantSnapshot {public SystemSample Sample;public string SystemText,NetworkText,IPText,CodexText;public string[] Notices;public WatchedProcess[] Watches;public int CodexRow;}
public class AssistantRuntime {
 public readonly DownloadLimiter Limiter=new DownloadLimiter();
 public readonly ProxyHealthMonitor ProxyHealth=new ProxyHealthMonitor();public readonly CodexQuotaReader QuotaReader;
 public readonly MonitorEngine Monitor;DateTime monitorSaved;
 readonly string home,configPath;readonly JavaScriptSerializer json=new JavaScriptSerializer();
 public string ReportProcessUsage(){string text=Config.Monitor.ProcessReportEnabled?ProcessUsageLogic.Report(Telemetry.ProcessSnapshot,"cpu",Config.Monitor.ProcessReportCount,DateTime.UtcNow)+"\n\n"+ProcessUsageLogic.Report(Telemetry.ProcessSnapshot,"gpu",Config.Monitor.ProcessReportCount,DateTime.UtcNow):"进程排行采集已关闭，请先在性能设置中开启。";Notify("process-usage-report",text,3);return text;}
 public AssistantConfig Config;public readonly CodexFeed Codex=new CodexFeed();public readonly NoticeFeed Notices=new NoticeFeed();
 public readonly MonitorArchive Archive;public readonly CodexUsageArchive TokenHistory;public readonly ClashTrafficSource SplitHistory;DateTime nextTokenScan,nextSplitScan;System.Threading.Tasks.Task tokenWork,splitWork;
 public readonly Telemetry Telemetry=new Telemetry();public readonly SystemNotificationInbox SystemNotifications=new SystemNotificationInbox();
 readonly Dictionary<int,WatchedProcess> watches=new Dictionary<int,WatchedProcess>();
 bool? connected;ulong previousIdle,previousTotal;
 public event Action<string,string,int> Notification;
 [StructLayout(LayoutKind.Sequential)]struct FileTime {public uint Low,High;public ulong Value{get{return ((ulong)High<<32)|Low;}}}
 [DllImport("kernel32.dll")]static extern bool GetSystemTimes(out FileTime idle,out FileTime kernel,out FileTime user);
 [StructLayout(LayoutKind.Sequential)]class MemoryInfo {public uint Length=(uint)Marshal.SizeOf(typeof(MemoryInfo));public uint Load;public ulong TotalPhys,AvailPhys,TotalPage,AvailPage,TotalVirtual,AvailVirtual,AvailExtended;}
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool GlobalMemoryStatusEx([In,Out]MemoryInfo info);
 public AssistantRuntime(string folder,string dataFolder=null){
  configPath=Path.Combine(dataFolder??folder,"assistant.json");QuotaReader=new CodexQuotaReader(dataFolder??folder);
  home=folder;Directory.CreateDirectory(AssistantLogic.EventDirectory(home));
  try{Config=json.Deserialize<AssistantConfig>(File.ReadAllText(configPath));}catch(IOException){}catch(ArgumentException){}
  if(Config==null)Config=new AssistantConfig();if(Config.Monitor==null)Config.Monitor=new MonitorConfig();Monitor=new MonitorEngine(Config.Monitor);if(Config.Monitor.Quota==null)Config.Monitor.Quota=new CodexQuotaConfig();if(Config.Monitor.Quota.Wake==null)Config.Monitor.Quota.Wake=new CodexWakeConfig();if(Config.Shortcuts==null)Config.Shortcuts=new List<Shortcut>();
  Archive=new MonitorArchive(dataFolder??folder);TokenHistory=new CodexUsageArchive(dataFolder??folder);SplitHistory=new ClashTrafficSource(dataFolder??folder);
  if(Config.Shortcuts.Count==0){AddDefault("桌面",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));AddDefault("文档",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));AddDefault("下载",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"));AddDefault("桌宠目录",home);}
 }
 void AddDefault(string name,string path){if(Directory.Exists(path))Config.Shortcuts.Add(new Shortcut{Name=name,Path=path});}
 void ScheduleHistory(){if(Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1")return;var cfg=Config.Monitor;if(cfg.CodexUsageHistory&&DateTime.UtcNow>=nextTokenScan&&(tokenWork==null||tokenWork.IsCompleted)){nextTokenScan=DateTime.UtcNow.AddSeconds(10);string source=String.IsNullOrWhiteSpace(cfg.HistoryCodexHome)?cfg.Quota.Home:cfg.HistoryCodexHome;tokenWork=System.Threading.Tasks.Task.Run(()=>TokenHistory.Scan(source));}if(cfg.ClashHistory&&DateTime.UtcNow>=nextSplitScan&&(splitWork==null||splitWork.IsCompleted)){nextSplitScan=DateTime.UtcNow.AddSeconds(1);string address=cfg.ClashController;splitWork=System.Threading.Tasks.Task.Run(()=>{var value=SplitHistory.Fetch(address,DateTime.UtcNow);lock(this)if(Config.Monitor.ClashHistory&&address==Config.Monitor.ClashController&&value.Recorded)Archive.AddSplit(DateTime.UtcNow,value.DirectDown,value.DirectUp,value.ProxyDown,value.ProxyUp,value.Unknown);});}}
 public void Save(){File.WriteAllText(configPath,json.Serialize(Config),Encoding.UTF8);}
 public void Watch(Process p){using(p){var w=new WatchedProcess{Id=p.Id,Name=p.ProcessName,Start=p.StartTime.ToUniversalTime(),Cpu=p.TotalProcessorTime,Sampled=DateTime.UtcNow};watches[w.Id]=w;}}
 public void Unwatch(int id){watches.Remove(id);}
 void Notify(string key,string text,int row){if((key.StartsWith("exit:")&&!Config.ProcessExitAlerts)||(key.StartsWith("codex:")&&!Config.CodexAlerts)||((key.StartsWith("network:")||key.StartsWith("monitor:proxy:"))&&!Config.NetworkAlerts)){Notices.Add(key,text,DateTime.UtcNow);return;}if(Notices.Add(key,text,DateTime.UtcNow)&&Notification!=null)Notification(key,text,row);}
 readonly CodexSessionTitles sessionTitles=new CodexSessionTitles();
 void ReadEvents(){
  var incoming=new List<Tuple<DateTime,string,string,string>>();
  foreach(string file in Directory.GetFiles(AssistantLogic.EventDirectory(home),"*.json").Where(EventProtocol.Named).Take(100)){
   bool owned=false;
   try{
    if(new FileInfo(file).Length>16384||(File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)continue;
    var e=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(file));string name,id;DateTime when;
    if(!EventProtocol.Read(file,e,out name,out id,out when))continue;owned=true;
    if(when>DateTime.UtcNow.AddMinutes(1)||when<DateTime.UtcNow.AddMinutes(-15))continue;
    object supplied;e.TryGetValue("title",out supplied);string title=sessionTitles.Resolve(id,supplied,Config.Monitor.Quota.Home);incoming.Add(Tuple.Create(when,id,AssistantLogic.State(name),title));
   }catch(IOException){continue;}catch(ArgumentException){}catch(InvalidOperationException){}catch(UnauthorizedAccessException){}
   finally{if(owned)try{File.Delete(file);}catch(IOException){}catch(UnauthorizedAccessException){}}
  }
  foreach(var e in incoming.OrderBy(x=>x.Item1)){
   CodexTask old;bool changed=!Codex.Tasks.TryGetValue(e.Item2,out old)||old.Status!=e.Item3;
   Codex.Apply(e.Item2,e.Item3,e.Item1,e.Item4);
   if(changed&&Codex.Tasks[e.Item2].Updated==e.Item1){
    string title=String.IsNullOrEmpty(Codex.Tasks[e.Item2].Title)?"Codex 会话":Codex.Tasks[e.Item2].Title;
    string text="Codex · "+title+"："+AssistantLogic.Label(e.Item3);
    if(e.Item3=="interrupted"&&!incoming.Any(next=>next.Item2==e.Item2&&next.Item1>e.Item1))Notify("codex:"+e.Item2+":"+e.Item3,text,5);
    else if(e.Item3!="waiting")Notices.Add("codex:"+e.Item2+":"+e.Item3,text,DateTime.UtcNow);
   }
  }
  foreach(var task in Codex.Tasks.Values.ToArray())if(Codex.TakeAttention(task.Id,DateTime.UtcNow)){string title=String.IsNullOrEmpty(task.Title)?"Codex 会话":task.Title;Notify("codex:"+task.Id+":waiting","Codex · "+title+"：等待你确认",6);}
 }
 void SampleWatches(){
  DateTime now=DateTime.UtcNow;
  foreach(var w in watches.Values){
   if(w.Ended)continue;
   try{using(var p=Process.GetProcessById(w.Id)){
    if(p.StartTime.ToUniversalTime()!=w.Start){w.Ended=true;Notify("exit:"+w.Id,w.Name+" ["+w.Id+"] 已退出。",3);continue;}
    TimeSpan cpu=p.TotalProcessorTime;w.CpuPercent=AssistantLogic.Cpu((cpu-w.Cpu).TotalSeconds,(now-w.Sampled).TotalSeconds,Environment.ProcessorCount);w.Cpu=cpu;w.Sampled=now;w.MemoryMiB=p.WorkingSet64/1048576.0;
    foreach(var alert in ProcessWatchAlerts.Observe(w,Config))Notify("monitor:process:"+w.Id+":"+alert.Key,w.Name+" · "+alert.Text,6);
   }}catch(ArgumentException){w.Ended=true;Notify("exit:"+w.Id,w.Name+" ["+w.Id+"] 已退出。",3);}catch(InvalidOperationException){w.Ended=true;Notify("exit:"+w.Id,w.Name+" ["+w.Id+"] 已退出。",3);}catch(System.ComponentModel.Win32Exception){}
  }
 }
 public AssistantSnapshot Poll(){
  ReadEvents();foreach(var notice in SystemNotifications.Poll()){string text=notice.App+" · "+notice.Title+(String.IsNullOrEmpty(notice.Body)?"":"\n"+notice.Body);if(SystemNotifications.Config.Quiet)Notices.Add("system:"+notice.Id,text,DateTime.UtcNow);else Notify("system:"+notice.Id,text,6);}SampleWatches();
  var memory=new MemoryInfo();GlobalMemoryStatusEx(memory);
  FileTime idle,kernel,user;double cpu=0;double? measuredCpu=null;
  if(GetSystemTimes(out idle,out kernel,out user)){
   ulong total=kernel.Value+user.Value;
   if(previousTotal!=0&&total>previousTotal)cpu=Math.Max(0,Math.Min(100,(1.0-(double)(idle.Value-previousIdle)/(total-previousTotal))*100));
   if(previousTotal!=0&&total>previousTotal)measuredCpu=cpu;previousTotal=total;previousIdle=idle.Value;
  }
  var addresses=new List<string>();var networks=new List<string>();
  try{foreach(var nic in NetworkInterface.GetAllNetworkInterfaces()){
   if(nic.OperationalStatus!=OperationalStatus.Up||nic.NetworkInterfaceType==NetworkInterfaceType.Loopback)continue;
   foreach(var ip in nic.GetIPProperties().UnicastAddresses.Where(i=>i.Address.AddressFamily==AddressFamily.InterNetwork)){addresses.Add(ip.Address.ToString());networks.Add(nic.Name+"  "+ip.Address);}
  }}catch(NetworkInformationException){}
  bool online=addresses.Count>0;if(connected.HasValue&&connected.Value!=online)Notify("network:"+online,online?"检测到活动网卡。":"当前没有活动IPv4网卡。",online?3:6);connected=online;
  string codex=Codex.Tasks.Count==0?"尚未收到 Codex 事件（不等于未安装）。\n如已安装接入，无需重复安装；请在 Codex 的 Hooks（钩子）设置中审核并信任，再重新打开会话并发送消息。":String.Join(Environment.NewLine,Codex.Tasks.Values.OrderByDescending(t=>t.Updated).Select(t=>(String.IsNullOrEmpty(t.Title)?"Codex 会话":t.Title)+"  "+AssistantLogic.Label(Codex.State(t.Id,DateTime.UtcNow))+"  更新于 "+t.Updated.ToLocalTime().ToString("HH:mm:ss")).ToArray());
  Telemetry.ProcessReportingEnabled=Config.Monitor.ProcessReportEnabled;Telemetry.RefreshMilliseconds=Config.Monitor.RefreshMilliseconds;Telemetry.SelectedNetworkId=Config.Monitor.NetworkId;var sample=Telemetry.Sample(measuredCpu,(memory.TotalPhys-memory.AvailPhys)/1073741824.0,memory.TotalPhys/1073741824.0);sample.Quota=QuotaReader.Sample(Config.Monitor.Quota);QuotaReader.MaybeWake(sample.Quota,Config.Monitor.Quota,DateTime.UtcNow,Save);if(Config.Monitor.CollectHistory)Archive.Observe(sample);ScheduleHistory();var proxyNotice=ProxyHealth.Poll(Config.Monitor,DateTime.UtcNow);if(proxyNotice!=null)Notify(proxyNotice.Key,proxyNotice.Text,6);var resets=QuotaReader.ObserveResets(sample.Quota,Config.Monitor.Quota,DateTime.UtcNow);foreach(var reset in resets)if(Config.Monitor.Quota.ResetDetectedAlerts)Notify("codex-reset:"+reset.Minutes+":"+reset.ObservedUtc.Ticks,"Codex "+reset.Report()+(reset.AwaitingStart?"\n额度可用，等待首次请求启动新周期。":"\n距离下一次预计重置还有 "+BubbleLogic.TimeLeft(reset.NewResetUtc-DateTime.UtcNow)),3);var resetReminders=QuotaReader.ResetReminderNotices(sample.Quota,Config.Monitor.Quota,DateTime.UtcNow);foreach(var reminder in resetReminders)Notify("codex-reset-soon:"+reminder.Minutes+":"+reminder.ResetUtc.Ticks+":"+reminder.LeadMinutes,reminder.Text,3);var subscription=Config.Monitor.Quota.Subscription;string subscriptionNotice=subscription==null?null:subscription.Observe(DateTime.Now);if(subscriptionNotice!=null)Notify("codex-subscription:"+subscription.ExpiresLocal+":"+DateTime.Now.ToString("yyyyMMdd"),subscriptionNotice,3);if(resets.Length>0||resetReminders.Length>0||subscriptionNotice!=null){Save();QuotaReader.Dirty=false;}foreach(string alert in QuotaReader.Alerts(sample.Quota,Config.Monitor.Quota,DateTime.UtcNow))Notify("monitor:codex:"+alert,alert,6);foreach(var alert in Monitor.Observe(sample))Notify("monitor:"+alert.Key,Config.Monitor.ProcessReportEnabled&&Config.Monitor.ProcessReportOnAlert?ProcessUsageLogic.AlertText(alert.Text,alert.Key,sample.Processes,Config.Monitor.ProcessReportCount,sample.Utc):alert.Text,6);if((Monitor.Dirty||QuotaReader.Dirty)&&DateTime.UtcNow-monitorSaved>TimeSpan.FromMinutes(1)){Save();Monitor.Dirty=false;QuotaReader.Dirty=false;monitorSaved=DateTime.UtcNow;}
  return new AssistantSnapshot{Sample=sample,SystemText=String.Format("CPU（全机） {0:F1}%\n内存 {1:F2} / {2:F2} GiB\n逻辑处理器 {3} 个",cpu,(memory.TotalPhys-memory.AvailPhys)/1073741824.0,memory.TotalPhys/1073741824.0,Environment.ProcessorCount),NetworkText=networks.Count==0?"没有活动IPv4网卡。":String.Join(Environment.NewLine,networks.ToArray()),IPText=String.Join(Environment.NewLine,addresses.Distinct().ToArray()),CodexText=codex,Notices=Notices.Items.ToArray(),Watches=watches.Values.ToArray(),CodexRow=Codex.Animation(DateTime.UtcNow)};
 }
}
