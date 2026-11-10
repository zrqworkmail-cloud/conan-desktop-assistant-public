using Conan.PluginApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
public class SystemSample {
 public string NetworkDetails="等待网络连接采样";public TimeSpan MonitorUptime;
 public ProcessUsageSnapshot Processes;public DateTime GpuSampleUtc;public CodexQuotaSnapshot Quota;
 public string DiskState="等待磁盘采样";public DateTime Utc;public double? Cpu,Gpu,GpuTemperature,CpuTemperature,Download,Upload,VramUsed,VramTotal,DiskBusy,DiskRead,DiskWrite,CpuMHz,DiskTemperature;
 public double MemoryUsed,MemoryTotal;public string GpuName="",GpuState="显卡驱动来源尚不可用",TemperatureState="未连接 LibreHardwareMonitor · CPU核心温度不可用",NetworkName="没有活动网卡",NetworkId="";public string[] NetworkIds=new string[0],NetworkNames=new string[0];
}
public class Telemetry:IDisposable {
 readonly PluginManager plugins;bool disposed;
 public Telemetry():this(null){}
 public Telemetry(PluginManager manager){plugins=manager;}
 public bool ProcessReportingEnabled=true;public int RefreshMilliseconds=1000;public string SelectedNetworkId="";
 ITelemetryBackend Backend{get{return !disposed&&plugins!=null?plugins.Service<ITelemetryBackend>("system-monitor"):null;}}
 public bool Available{get{return Backend!=null;}}
 public ProcessUsageSnapshot ProcessSnapshot{get{return disposed||plugins==null?null:plugins.Invoke<ITelemetryBackend,ProcessUsageSnapshot>("system-monitor",backend=>new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<ProcessUsageSnapshot>(backend.ProcessSnapshotJson),null);}}
 public SystemSample Sample(double? cpu,double used,double total){
  var unavailable=new SystemSample{Utc=DateTime.UtcNow,NetworkName="系统监控插件未启用",NetworkDetails="请在插件中心检查系统监控",GpuState="系统监控插件不可用",DiskState="系统监控插件不可用",TemperatureState="系统监控插件不可用"};
  if(disposed||plugins==null)return unavailable;
  return plugins.Invoke<ITelemetryBackend,SystemSample>("system-monitor",backend=>{backend.ProcessReportingEnabled=ProcessReportingEnabled;backend.RefreshMilliseconds=RefreshMilliseconds;backend.SelectedNetworkId=SelectedNetworkId;var result=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<SystemSample>(backend.Sample(cpu,used,total));if(result==null)throw new InvalidDataException("插件未返回采样数据。");return result;},unavailable);
 }
 public void Dispose(){disposed=true;}
 public static string ConnectionDetails(NetworkInterface nic,IPv4InterfaceStatistics counters){
  var ip=nic.GetIPProperties();var unicast=ip.UnicastAddresses;var v4=unicast.Where(x=>x.Address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork).ToArray();
  string mac=String.Join(":",nic.GetPhysicalAddress().GetAddressBytes().Select(b=>b.ToString("X2")));
  return "网卡："+nic.Name+"\n设备："+nic.Description+"\n状态："+nic.OperationalStatus+" · "+nic.NetworkInterfaceType+"\n链路速率："+(nic.Speed/1000000.0).ToString("F0")+" Mbps（非实际下载速度）\nMAC："+mac+
   "\nIPv4："+String.Join(" / ",v4.Select(x=>x.Address.ToString()))+"\n子网掩码："+String.Join(" / ",v4.Select(x=>x.IPv4Mask.ToString()))+
   "\nIPv6："+String.Join(" / ",unicast.Where(x=>x.Address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetworkV6).Select(x=>x.Address.ToString()))+
   "\n默认网关："+String.Join(" / ",ip.GatewayAddresses.Select(x=>x.Address.ToString()))+"\nDNS："+String.Join(" / ",ip.DnsAddresses.Select(x=>x.ToString()))+
   "\n累计接收："+(counters.BytesReceived/1048576.0).ToString("F1")+" MiB · 累计发送："+(counters.BytesSent/1048576.0).ToString("F1")+" MiB\n接收错误："+counters.IncomingPacketsWithErrors+" · 发送错误："+counters.OutgoingPacketsWithErrors+"\n累计值来自网卡 IPv4 计数器；起点由系统/网卡决定。";
 }
 public static string Speed(double? v){if(!v.HasValue)return "等待采样";return v>=1048576?(v.Value/1048576).ToString("F1")+" MiB/s":(v.Value/1024).ToString("F1")+" KiB/s";}
}
