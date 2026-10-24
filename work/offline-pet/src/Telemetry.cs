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
 readonly ProcessUsageSampler processSampler=new ProcessUsageSampler();public bool ProcessReportingEnabled=true;public ProcessUsageSnapshot ProcessSnapshot{get{return processSampler.Current;}}
 readonly ExtraTelemetry extra=new ExtraTelemetry();
 volatile bool disposed;int gpuSampling,extraSampling;readonly object extraGate=new object();volatile SystemSample extended;
 public void Dispose(){disposed=true;processSampler.Dispose();lock(extraGate)extra.Dispose();}
 readonly Stopwatch clock=Stopwatch.StartNew();readonly NetworkRate rate=new NetworkRate();public int RefreshMilliseconds=1000;TimeSpan CacheLifetime{get{return TimeSpan.FromMilliseconds(Math.Max(5000,Math.Max(500,Math.Min(5000,RefreshMilliseconds))*3));}}public string SelectedNetworkId="";double lastGpu=-10;class GpuReading{public DateTime CheckedUtc;public double? Load,Temperature,Vram,Total;public string Name="",State="等待驱动采样";}volatile GpuReading gpuReading=new GpuReading();
 public SystemSample Sample(double? cpu,double used,double total){
  var value=new SystemSample{Processes=processSampler.Poll(ProcessReportingEnabled),Utc=DateTime.UtcNow,Cpu=cpu,MemoryUsed=used,MemoryTotal=total,MonitorUptime=clock.Elapsed};
  try{
   var nics=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback&&n.NetworkInterfaceType!=NetworkInterfaceType.Tunnel).ToArray();
   value.NetworkIds=nics.Select(n=>n.Id).ToArray();value.NetworkNames=nics.Select(n=>n.Name).ToArray();
   var nic=nics.FirstOrDefault(n=>n.Id==SelectedNetworkId)??nics.FirstOrDefault(n=>n.GetIPProperties().GatewayAddresses.Count>0)??nics.FirstOrDefault();
   if(nic!=null){value.NetworkId=nic.Id;value.NetworkName=nic.Name;var counters=nic.GetIPv4Statistics();value.NetworkDetails=ConnectionDetails(nic,counters);var speed=rate.Sample(nic.Id,counters.BytesReceived,counters.BytesSent,clock.Elapsed.TotalSeconds);if(speed.HasValue){value.Download=speed.Value.Download;value.Upload=speed.Value.Upload;}}
  }catch(NetworkInformationException){value.NetworkName="网卡读取失败";value.NetworkDetails="当前网络连接读取失败 · 等待下一次采样";}
  if(clock.Elapsed.TotalSeconds-lastGpu>=1&&System.Threading.Interlocked.CompareExchange(ref gpuSampling,1,0)==0){lastGpu=clock.Elapsed.TotalSeconds;System.Threading.Tasks.Task.Run(()=>{try{if(!disposed){var reading=SampleGpu();reading.CheckedUtc=DateTime.UtcNow;if(!disposed)gpuReading=reading;}}finally{System.Threading.Interlocked.Exchange(ref gpuSampling,0);}});}
  var latest=gpuReading;bool gpuFresh=latest.CheckedUtc!=default(DateTime)&&DateTime.UtcNow>=latest.CheckedUtc&&DateTime.UtcNow-latest.CheckedUtc<=CacheLifetime;value.GpuSampleUtc=latest.CheckedUtc;value.Gpu=gpuFresh?latest.Load:null;value.GpuTemperature=gpuFresh?latest.Temperature:null;value.VramUsed=gpuFresh?latest.Vram:null;value.VramTotal=gpuFresh?latest.Total:null;value.GpuName=latest.Name;value.GpuState=!gpuFresh&&latest.CheckedUtc!=default(DateTime)?"显卡读数已过期 · 等待新采样":latest.State;
  var cached=extended;if(cached!=null&&DateTime.UtcNow>=cached.Utc&&DateTime.UtcNow-cached.Utc<=CacheLifetime){value.DiskBusy=cached.DiskBusy;value.DiskRead=cached.DiskRead;value.DiskWrite=cached.DiskWrite;value.CpuMHz=cached.CpuMHz;value.DiskTemperature=cached.DiskTemperature;value.DiskState=cached.DiskState;value.CpuTemperature=cached.CpuTemperature;value.TemperatureState=cached.TemperatureState;if(!value.Gpu.HasValue&&(!cached.GpuState.StartsWith("NVIDIA")||gpuFresh)){value.Gpu=cached.Gpu;value.GpuName=cached.GpuName;value.GpuState=cached.GpuState;value.GpuSampleUtc=cached.Utc;}if(!value.GpuTemperature.HasValue)value.GpuTemperature=cached.GpuTemperature;}
  if(System.Threading.Interlocked.CompareExchange(ref extraSampling,1,0)==0){var pending=new SystemSample{Gpu=gpuFresh?latest.Load:null,GpuTemperature=gpuFresh?latest.Temperature:null,GpuName=latest.Name,GpuState=latest.State};System.Threading.Tasks.Task.Run(()=>{try{lock(extraGate){if(!disposed){extra.Sample(pending);pending.Utc=DateTime.UtcNow;extended=pending;}}}finally{System.Threading.Interlocked.Exchange(ref extraSampling,0);}});}return value;
 }
 public static string ConnectionDetails(NetworkInterface nic,IPv4InterfaceStatistics counters){
  var ip=nic.GetIPProperties();var unicast=ip.UnicastAddresses;var v4=unicast.Where(x=>x.Address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork).ToArray();
  string mac=String.Join(":",nic.GetPhysicalAddress().GetAddressBytes().Select(b=>b.ToString("X2")));
  return "网卡："+nic.Name+"\n设备："+nic.Description+"\n状态："+nic.OperationalStatus+" · "+nic.NetworkInterfaceType+"\n链路速率："+(nic.Speed/1000000.0).ToString("F0")+" Mbps（非实际下载速度）\nMAC："+mac+
   "\nIPv4："+String.Join(" / ",v4.Select(x=>x.Address.ToString()))+"\n子网掩码："+String.Join(" / ",v4.Select(x=>x.IPv4Mask.ToString()))+
   "\nIPv6："+String.Join(" / ",unicast.Where(x=>x.Address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetworkV6).Select(x=>x.Address.ToString()))+
   "\n默认网关："+String.Join(" / ",ip.GatewayAddresses.Select(x=>x.Address.ToString()))+"\nDNS："+String.Join(" / ",ip.DnsAddresses.Select(x=>x.ToString()))+
   "\n累计接收："+(counters.BytesReceived/1048576.0).ToString("F1")+" MiB · 累计发送："+(counters.BytesSent/1048576.0).ToString("F1")+" MiB\n接收错误："+counters.IncomingPacketsWithErrors+" · 发送错误："+counters.OutgoingPacketsWithErrors+"\n累计值来自网卡 IPv4 计数器；起点由系统/网卡决定。";
 }
 GpuReading SampleGpu(){
  var result=new GpuReading();string exe=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"nvidia-smi.exe");if(!File.Exists(exe))exe=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"NVIDIA Corporation","NVSMI","nvidia-smi.exe");if(!File.Exists(exe)){result.State="未找到NVIDIA驱动采样工具；其他显卡暂不支持";return result;}
  try{using(var p=Process.Start(new ProcessStartInfo(exe,"--query-gpu=name,utilization.gpu,memory.used,memory.total,temperature.gpu --format=csv,noheader,nounits"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})){
   if(!p.WaitForExit(1500)){p.Kill();result.State="驱动采样超时";return result;}string line=p.StandardOutput.ReadLine();if(p.ExitCode!=0||line==null){result.State="驱动未返回读数";return result;}var fields=line.Split(',');if(fields.Length<5){result.State="驱动返回格式不可用";return result;}result.Name=fields[0].Trim();result.Load=Number(fields[1],100);result.Vram=Number(fields[2],1048576);result.Total=Number(fields[3],1048576);result.Temperature=Number(fields[4],150);result.State="NVIDIA驱动 · 第一块GPU · 每1秒";
  }}catch(System.ComponentModel.Win32Exception){result.State="驱动工具无法启动";}catch(InvalidOperationException){result.State="驱动进程未就绪";}catch(IOException){result.State="驱动输出读取失败";}
  return result;
 }
 static double? Number(string text,double maximum){double v;return Double.TryParse(text.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out v)&&!Double.IsNaN(v)&&!Double.IsInfinity(v)&&v>=0&&v<=maximum?(double?)v:null;}
 public static string Speed(double? v){if(!v.HasValue)return "等待采样";return v>=1048576?(v.Value/1048576).ToString("F1")+" MiB/s":(v.Value/1024).ToString("F1")+" KiB/s";}
}
