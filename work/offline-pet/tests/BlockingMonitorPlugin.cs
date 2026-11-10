using System;using System.Threading;using Conan.PluginApi;
public class BlockingMonitorPlugin:ITelemetryBackend {
 public static readonly ManualResetEvent Entered=new ManualResetEvent(false),Release=new ManualResetEvent(false);
 public bool Disposed;public string Id{get{return "system-monitor";}}
 public bool ProcessReportingEnabled{get;set;}public int RefreshMilliseconds{get;set;}public string SelectedNetworkId{get;set;}
 public string ProcessSnapshotJson{get{return "null";}}
 public void Initialize(PluginContext context){}
 public string Sample(double? cpu,double used,double total){Entered.Set();Release.WaitOne();return "{\"Cpu\":37,\"MemoryUsed\":8,\"MemoryTotal\":16}";}
 public void Dispose(){Disposed=true;}
}
