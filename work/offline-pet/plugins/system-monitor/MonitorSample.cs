namespace Conan.Plugins.SystemMonitor {
using Conan.PluginApi;
using System;
public class SystemSample {
 public string NetworkDetails="等待网络连接采样";public TimeSpan MonitorUptime;
 public ProcessUsageSnapshot Processes;public DateTime GpuSampleUtc;
 public string DiskState="等待磁盘采样";public DateTime Utc;public double? Cpu,Gpu,GpuTemperature,CpuTemperature,Download,Upload,VramUsed,VramTotal,DiskBusy,DiskRead,DiskWrite,CpuMHz,DiskTemperature;
 public double MemoryUsed,MemoryTotal;public string GpuName="",GpuState="显卡驱动来源尚不可用",TemperatureState="未连接 LibreHardwareMonitor · CPU核心温度不可用",NetworkName="没有活动网卡",NetworkId="";public string[] NetworkIds=new string[0],NetworkNames=new string[0];
}

}
