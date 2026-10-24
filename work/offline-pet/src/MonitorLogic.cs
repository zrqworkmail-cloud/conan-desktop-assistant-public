using System;
using System.Linq;
using System.Collections.Generic;
public class MonitorRule {public string Key,Label,Unit;public bool Enabled;public double Threshold;public List<double> Thresholds;}
public class TrafficDay {public string Day;public double Download,Upload;}
public class MonitorConfig {
 public bool ArchiveBars=true,ArchiveLines=true;public bool CollectHistory=true,CodexUsageHistory=true,ClashHistory;public string HistoryCodexHome="",ClashController="http://127.0.0.1:9090";
 public bool ProcessReportEnabled=true,ProcessReportOnAlert=true;public int ProcessReportCount=3;
 public string VisualStyle="original";
 public bool StartupAuthorizationPrompt=true;public double DownloadLimitMiB=10;public int RefreshMilliseconds=1000;public CodexQuotaConfig Quota=new CodexQuotaConfig();
 public string ExitProxyMode="system",ExitProxyAddress="";
 public bool ExitLookupEnabled;public string CardBackground="",TaskbarScreen="";public double CardShade=.58;
 public string ExpectedExitIp="",ExpectedExitCountry="";
 public List<NavigationOrder> NavigationOrder=new List<NavigationOrder>();
 public List<ColorPreset> ColorPresets=new List<ColorPreset>();
 public List<TaskbarPreset> TaskbarPresets=new List<TaskbarPreset>();
 public TaskbarAppearance Appearance=new TaskbarAppearance();
 public bool AlertBubbles=true,AlertActions=true;public bool TaskbarAllScreens;public bool TaskbarPositionLocked=true;
 public bool Taskbar,ClickCard=true,Alerts=true,AlertSound=true;public int TaskbarOffset=8,HoldSeconds=9,CooldownSeconds=120;public string TaskbarSide="left",NetworkId="";
 public List<string> TaskbarItems=new List<string>{"download","upload","cpu","memory"};
 public List<TrafficDay> Days=new List<TrafficDay>();
 public List<MonitorRule> Rules=new List<MonitorRule>{
  new MonitorRule{Key="cpu",Label="CPU占用",Unit="%",Enabled=true,Threshold=90},new MonitorRule{Key="memory",Label="内存占用",Unit="%",Enabled=true,Threshold=90},new MonitorRule{Key="gpu",Label="GPU占用",Unit="%",Threshold=95},new MonitorRule{Key="cpuTemp",Label="CPU温度",Unit="°C",Enabled=true,Threshold=85},new MonitorRule{Key="gpuTemp",Label="GPU温度",Unit="°C",Enabled=true,Threshold=85},new MonitorRule{Key="disk",Label="磁盘活动",Unit="%",Threshold=95},new MonitorRule{Key="download",Label="下载速度",Unit="MiB/s",Threshold=50},new MonitorRule{Key="upload",Label="上传速度",Unit="MiB/s",Threshold=20}};
}
public class MonitorAlert {public string Key,Text;public double Value,Threshold;}
public class MonitorEngine {
 class State {public DateTime? Above;public DateTime Last=DateTime.MinValue;public bool Active;public double Threshold;}
 readonly Dictionary<string,State> states=new Dictionary<string,State>();DateTime lastSample;string lastNetwork;public bool Dirty;
 public readonly MonitorConfig Config;
 public MonitorEngine(MonitorConfig config){Config=config;if(Config.Appearance==null)Config.Appearance=new TaskbarAppearance();Config.Appearance.EnsureItems();try{Config.Appearance.Validate();}catch(ArgumentException){Config.Appearance=new TaskbarAppearance();}var defaults=new MonitorConfig();if(Config.Rules==null)Config.Rules=defaults.Rules;if(Config.Days==null)Config.Days=new List<TrafficDay>();if(Config.TaskbarItems==null)Config.TaskbarItems=defaults.TaskbarItems;Config.TaskbarItems=Config.TaskbarItems.Where(k=>TaskbarAppearance.Keys.Contains(k)).Distinct().Take(TaskbarAppearance.Keys.Length).ToList();if(Config.TaskbarItems.Count==0)Config.TaskbarItems=defaults.TaskbarItems;foreach(var rule in defaults.Rules)if(!Config.Rules.Any(r=>r!=null&&r.Key==rule.Key))Config.Rules.Add(rule);}
 public static double? Value(SystemSample s,string key){if(s==null)return null;switch(key){case "codex5h":return s.Quota==null?null:s.Quota.Remaining(300,DateTime.UtcNow);case "codex7d":return s.Quota==null?null:s.Quota.Remaining(10080,DateTime.UtcNow);case "cpu":return s.Cpu;case "gpu":return s.Gpu;case "memory":return s.MemoryTotal>0?(double?)(s.MemoryUsed/s.MemoryTotal*100):null;case "cpuTemp":return s.CpuTemperature;case "gpuTemp":return s.GpuTemperature;case "disk":return s.DiskBusy;case "download":return s.Download/1048576;case "upload":return s.Upload/1048576;case "diskRead":return s.DiskRead/1048576;case "diskWrite":return s.DiskWrite/1048576;case "frequency":return s.CpuMHz;default:return null;}}
 public static string Label(string key){switch(key){case "codex5h":return "额度5h";case "codex7d":return "额度周";case "download":return "↓";case "upload":return "↑";case "cpu":return "CPU";case "memory":return "内存";case "gpu":return "GPU";case "cpuTemp":return "CPU温";case "gpuTemp":return "GPU温";case "disk":return "磁盘";default:return key;}}
 public static string Format(SystemSample s,string key){var v=Value(s,key);if(!v.HasValue||Double.IsNaN(v.Value)||Double.IsInfinity(v.Value))return "—";if(key=="download"||key=="upload")return Telemetry.Speed(v*1048576);return v.Value.ToString("F0")+(key.EndsWith("Temp")?"°C":key=="frequency"?" MHz":"%");}
 public MonitorAlert[] Observe(SystemSample sample){if(sample==null)return new MonitorAlert[0];DateTime now=sample.Utc;double dt=(now-lastSample).TotalSeconds;
  if(Config.CollectHistory&&lastNetwork==sample.NetworkId&&dt>0&&dt<=10&&sample.Download.HasValue&&sample.Upload.HasValue&&!Double.IsNaN(sample.Download.Value)&&!Double.IsInfinity(sample.Download.Value)&&!Double.IsNaN(sample.Upload.Value)&&!Double.IsInfinity(sample.Upload.Value)){string day=now.ToLocalTime().ToString("yyyy-MM-dd");var item=Config.Days.FirstOrDefault(d=>d.Day==day);if(item==null){item=new TrafficDay{Day=day};Config.Days.Add(item);}item.Download+=Math.Max(0,sample.Download.Value)*dt;item.Upload+=Math.Max(0,sample.Upload.Value)*dt;while(Config.Days.Count>180)Config.Days.RemoveAt(0);Dirty=true;}
  bool gap=lastSample!=default(DateTime)&&(dt<=0||dt>10);lastSample=now;lastNetwork=sample.NetworkId;var alerts=new List<MonitorAlert>();
  foreach(var rule in Config.Rules.Where(r=>r!=null))foreach(double point in ThresholdPoints.Effective(rule.Thresholds,rule.Threshold,.01,rule.Unit=="%"?100:rule.Unit=="°C"?150:100000)){
   var threshold=point;string stateKey=rule.Key+":"+point.ToString(System.Globalization.CultureInfo.InvariantCulture);State state;if(!states.TryGetValue(stateKey,out state))states[stateKey]=state=new State();var value=Value(sample,rule.Key);
   if(!Config.Alerts||!rule.Enabled||threshold<=0||Double.IsNaN(threshold)||Double.IsInfinity(threshold)||!value.HasValue||Double.IsNaN(value.Value)||Double.IsInfinity(value.Value)||state.Threshold!=threshold||gap){state.Above=null;state.Active=false;state.Threshold=threshold;if(!Config.Alerts||!rule.Enabled||threshold<=0||Double.IsNaN(threshold)||Double.IsInfinity(threshold)||!value.HasValue)continue;}
   if(value.Value<threshold*.92){state.Above=null;state.Active=false;continue;}if(value.Value<threshold){state.Above=null;continue;}
   if(!state.Above.HasValue)state.Above=now;
   if(!state.Active&&(now-state.Above.Value).TotalSeconds>=Math.Max(0,Config.HoldSeconds)&&(now-state.Last).TotalSeconds>=Math.Max(30,Config.CooldownSeconds)){state.Active=true;state.Last=now;alerts.Add(new MonitorAlert{Key=rule.Thresholds==null?rule.Key:stateKey,Value=value.Value,Threshold=threshold,Text=rule.Label+"持续偏高："+value.Value.ToString("F1")+rule.Unit+"，预警值 "+threshold.ToString("F0")+rule.Unit+"。"});}
  }return alerts.ToArray();
 }
 public double Total(bool upload,int days,DateTime now){string start=now.ToLocalTime().Date.AddDays(1-days).ToString("yyyy-MM-dd"),end=now.ToLocalTime().ToString("yyyy-MM-dd");return Config.Days.Where(d=>String.CompareOrdinal(d.Day,start)>=0&&String.CompareOrdinal(d.Day,end)<=0).Sum(d=>upload?d.Upload:d.Download);}
}
