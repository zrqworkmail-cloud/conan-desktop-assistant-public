using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
class ExtraTelemetry:IDisposable {
 PerformanceCounter disk,read,write,frequency;List<PerformanceCounter> gpu=new List<PerformanceCounter>();DateTime retry;bool ready,counterWarm;
 public string Status="等待Windows性能计数器";
 class Temperatures{public double? Cpu{get;set;}public double? Gpu{get;set;}public double? Disk{get;set;}public string State{get;set;}public DateTime CheckedUtc{get;set;}}volatile Temperatures sensors;DateTime sensorRead;
 public void Sample(SystemSample sample){
  if(!ready&&DateTime.UtcNow>=retry){retry=DateTime.UtcNow.AddMinutes(1);foreach(var old in new[]{disk,read,write,frequency})if(old!=null)old.Dispose();disk=read=write=frequency=null;try{disk=new PerformanceCounter("PhysicalDisk","% Idle Time","_Total");read=new PerformanceCounter("PhysicalDisk","Disk Read Bytes/sec","_Total");write=new PerformanceCounter("PhysicalDisk","Disk Write Bytes/sec","_Total");frequency=new PerformanceCounter("Processor Information","Processor Frequency","_Total");ready=true;}catch(Exception e){if(!(e is InvalidOperationException)&&!(e is UnauthorizedAccessException)&&!(e is System.ComponentModel.Win32Exception))throw;Status="部分性能计数器不可用";}}
  sample.DiskBusy=Read(disk,100);if(sample.DiskBusy.HasValue)sample.DiskBusy=100-sample.DiskBusy.Value;sample.DiskRead=Read(read,Double.MaxValue);sample.DiskWrite=Read(write,Double.MaxValue);sample.CpuMHz=Read(frequency,100000);sample.DiskState=Status;if(!counterWarm){sample.DiskBusy=sample.DiskRead=sample.DiskWrite=null;counterWarm=true;}
  if(!sample.Gpu.HasValue){if(DateTime.UtcNow>=gpuRetry){gpuRetry=DateTime.UtcNow.AddMinutes(1);foreach(var old in gpu)old.Dispose();gpu.Clear();try{var category=new PerformanceCounterCategory("GPU Engine");foreach(string name in category.GetInstanceNames().Where(n=>n.Contains("engtype_3D")||n.Contains("engtype_Compute")).Take(96))gpu.Add(new PerformanceCounter("GPU Engine","Utilization Percentage",name));foreach(var c in gpu)Read(c,100);}catch(Exception e){if(!(e is InvalidOperationException)&&!(e is UnauthorizedAccessException)&&!(e is System.ComponentModel.Win32Exception))throw;}}
   var loads=gpu.Select(c=>Read(c,100)).Where(v=>v.HasValue).Select(v=>v.Value).ToArray();if(loads.Length>0){sample.Gpu=loads.Max();sample.GpuState="Windows GPU Engine · 3D/Compute进程引擎峰值（非全卡总利用率）";sample.GpuName="Windows图形引擎";}}
  ReadSensors(sample);
  if(sample.DiskBusy.HasValue)sample.DiskState="Windows PhysicalDisk · 全部物理磁盘汇总";
 }
 int sensorBusy;volatile bool disposed;
 void ReadSensors(SystemSample sample){
  var current=sensors;bool fromLog=current!=null&&current.State=="LibreHardwareMonitor 本地日志";
  if(DateTime.UtcNow-sensorRead>TimeSpan.FromSeconds(fromLog?5:30)&&System.Threading.Interlocked.CompareExchange(ref sensorBusy,1,0)==0){sensorRead=DateTime.UtcNow;System.Threading.Tasks.Task.Run(()=>{try{var next=FetchSensors();if(!disposed)sensors=next;}finally{System.Threading.Interlocked.Exchange(ref sensorBusy,0);}});}
  if(current!=null){if(current.CheckedUtc!=default(DateTime)&&(DateTime.UtcNow<current.CheckedUtc||DateTime.UtcNow-current.CheckedUtc>TimeSpan.FromSeconds(15))){sample.TemperatureState="CPU温度日志已过期，等待采集器更新";return;}sample.TemperatureState=current.State??"LibreHardwareMonitor 未返回CPU核心温度；请检查采集器权限和硬件支持";sample.CpuTemperature=Temperature(current.Cpu);if(!sample.GpuTemperature.HasValue)sample.GpuTemperature=Temperature(current.Gpu);sample.DiskTemperature=Temperature(current.Disk);if(sample.CpuTemperature.HasValue)sample.TemperatureState=(current.State??"LibreHardwareMonitor WMI")+" · CPU核心/封装最高值 · "+(fromLog?"每5秒":"每30秒")+"后台";}
 }
 Temperatures FetchSensors(){var log=HardwareLog.ReadRunning(DateTime.UtcNow);if(log!=null)return new Temperatures{Cpu=log.Cpu,Gpu=log.Gpu,Disk=log.Disk,CheckedUtc=log.Utc,State="LibreHardwareMonitor 本地日志"};string script=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"bridge","read-hardware.ps1");if(!System.IO.File.Exists(script))return new Temperatures{State="温度读取脚本缺失，请完整解压发布包"};var processes=Process.GetProcessesByName("LibreHardwareMonitor");bool running=processes.Length>0;foreach(var p in processes)p.Dispose();if(!running)return new Temperatures{State="LibreHardwareMonitor 未运行 · 在数据来源页查看接入说明"};try{using(var p=Process.Start(new ProcessStartInfo(LimiterSecurity.PowerShell,"-NoProfile -ExecutionPolicy Bypass -File \""+script+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})){if(!p.WaitForExit(4000))p.Kill();else if(p.ExitCode==0)return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Temperatures>(p.StandardOutput.ReadToEnd());}}catch(Exception e){if(!(e is InvalidOperationException)&&!(e is System.ComponentModel.Win32Exception)&&!(e is System.IO.IOException)&&!(e is ArgumentException))throw;}return new Temperatures{State="LibreHardwareMonitor 温度查询失败或超时，请检查WMI和管理员权限"};}
 static double? Temperature(double? value){return value.HasValue&&value>0&&value<150?value:null;}
 DateTime gpuRetry;
 static double? Read(PerformanceCounter counter,double max){if(counter==null)return null;try{double value=counter.NextValue();return !Double.IsNaN(value)&&!Double.IsInfinity(value)&&value>=0?(double?)Math.Min(max,value):null;}catch(Exception e){if(!(e is InvalidOperationException)&&!(e is UnauthorizedAccessException)&&!(e is System.ComponentModel.Win32Exception))throw;return null;}}
 public void Dispose(){disposed=true;foreach(var c in new[]{disk,read,write,frequency}.Concat(gpu))if(c!=null)c.Dispose();}
}
