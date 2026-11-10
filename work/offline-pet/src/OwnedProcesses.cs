using System;using System.Collections.Generic;using System.Diagnostics;
// Tracks only processes started explicitly as internal workers by this instance.
internal sealed class OwnedProcesses:IDisposable {
 readonly object gate=new object();readonly HashSet<Process> processes=new HashSet<Process>();bool stopped;
 public bool IsStopped{get{lock(gate)return stopped;}}
 public Process Start(ProcessStartInfo info){lock(gate){if(stopped)throw new ObjectDisposedException("OwnedProcesses");var process=Process.Start(info);if(process==null)throw new InvalidOperationException("无法启动后台进程。");processes.Add(process);return process;}}
 public void Forget(Process process){lock(gate)processes.Remove(process);}
 public void Dispose(){lock(gate){if(stopped)return;stopped=true;foreach(var process in processes)try{if(!process.HasExited)process.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}processes.Clear();}}
}
