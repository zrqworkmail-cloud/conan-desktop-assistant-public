using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
#if !LIMITER_HELPER
public sealed class DownloadLimiter:IDisposable {
 Process process;EventWaitHandle stop,ready;string state="限速未开启";
 public string Status{get{if(process!=null){if(process.HasExited){int code=process.ExitCode;Dispose();state=code==0?"限速已结束":"限速组件启动失败（代码"+code+"），未启用";}else return ready!=null&&ready.WaitOne(0)?"整机下载限速运行中 · "+LimitMiB.ToString("F1")+" MiB/s":"等待管理员授权 / 初始化流量组件…";}return state;}}
 public bool Running{get{return process!=null&&!process.HasExited;}}public double LimitMiB;
 public void Start(string home,double mib){if(Running)throw new InvalidOperationException("请先停止当前限速。");if(Double.IsNaN(mib)||Double.IsInfinity(mib)||mib<.1||mib>10000)throw new ArgumentException("平均下载上限0.1–10000 MiB/s。");Dispose();string folder=Path.Combine(home,"network-limit");using(VerifyComponents(folder)){}string id="Local\\ConanBandwidth-"+Guid.NewGuid().ToString("N");stop=new EventWaitHandle(false,EventResetMode.ManualReset,id+"-stop");ready=new EventWaitHandle(false,EventResetMode.ManualReset,id+"-ready");try{string script=LimiterSecurity.Script(folder,Process.GetCurrentProcess().Id,mib,id);process=Process.Start(new ProcessStartInfo(LimiterSecurity.PowerShell,"-NoProfile -NonInteractive -EncodedCommand "+Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script))){WorkingDirectory=Environment.GetFolderPath(Environment.SpecialFolder.System),UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden});LimitMiB=mib;state="等待限速组件";}catch{Dispose();throw;}}
 internal static IDisposable VerifyComponents(string folder){return LimiterSecurity.Verify(folder);}
 public void Dispose(){if(stop!=null){stop.Set();stop.Dispose();stop=null;}if(ready!=null){ready.Dispose();ready=null;}if(process!=null){process.Dispose();process=null;}state="限速未开启";}
}
#endif
public class DownloadPacer {
 readonly double bytesPerSecond;double scheduled=Double.NegativeInfinity;
 public DownloadPacer(double rate){if(Double.IsNaN(rate)||Double.IsInfinity(rate)||rate<=0)throw new ArgumentException("无效速率");bytesPerSecond=rate;}
 public double Reserve(int length,double seconds){scheduled=Math.Max(seconds-.025,scheduled)+length/bytesPerSecond;return Math.Max(0,scheduled-seconds);}
}
