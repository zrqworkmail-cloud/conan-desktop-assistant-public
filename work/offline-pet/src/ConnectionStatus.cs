using System;using System.IO;
internal class ConnectionStatus {
 internal string QuotaIssue,SensorIssue;internal bool NeedsAttention{get{return QuotaIssue!=null||SensorIssue!=null;}}
 internal static ConnectionStatus Inspect(AssistantRuntime runtime){var result=new ConnectionStatus();var cfg=runtime.Config.Monitor;
  if(cfg.Quota.Enabled&&(cfg.TaskbarItems.Contains("codex5h")||cfg.TaskbarItems.Contains("codex7d")||!String.IsNullOrWhiteSpace(cfg.Quota.Executable)))try{using(runtime.QuotaReader.Trust.OpenVerified(cfg.Quota.Executable,cfg.Quota.Home)){}}catch(Exception e){if(!(e is IOException)&&!(e is ArgumentException)&&!(e is UnauthorizedAccessException))throw;result.QuotaIssue=e.Message;}
  if(cfg.TaskbarItems.Contains("cpuTemp")&&HardwareLog.ReadRunning(DateTime.UtcNow)==null)result.SensorIssue=SensorProvider.Running?"采集器正在运行，但没有新鲜CPU温度日志。请开启 Log Sensors，日志间隔设为5秒，并处理采集器自身的提示。":"温度采集器未运行。需启动官方采集器并保留后台运行；Windows会要求UAC确认。";
  return result;
 }
}
