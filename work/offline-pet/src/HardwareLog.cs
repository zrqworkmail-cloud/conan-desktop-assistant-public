using System;using System.IO;using System.Text;using System.Linq;using System.Globalization;using System.Diagnostics;using System.Runtime.InteropServices;
internal class HardwareLogSample {internal double? Cpu,Gpu,Disk;internal DateTime Utc;}
internal static class HardwareLog {
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool QueryFullProcessImageName(IntPtr handle,uint flags,StringBuilder name,ref int size);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
 internal static HardwareLogSample ReadRunning(DateTime now){foreach(var process in Process.GetProcessesByName("LibreHardwareMonitor")){try{var handle=OpenProcess(0x1000,false,process.Id);if(handle==IntPtr.Zero)continue;string folder=null;try{var path=new StringBuilder(32768);int size=path.Capacity;if(QueryFullProcessImageName(handle,0,path,ref size))folder=Path.GetDirectoryName(QuotaExecutionTrust.LocalPath(path.ToString()));}finally{CloseHandle(handle);}if(folder==null)continue;foreach(var file in new DirectoryInfo(folder).GetFiles("LibreHardwareMonitorLog-*.csv").Where(f=>now-f.LastWriteTimeUtc<TimeSpan.FromSeconds(45)).OrderByDescending(f=>f.LastWriteTimeUtc).Take(4)){var sample=ReadFile(file.FullName,now);if(sample!=null&&sample.Cpu.HasValue)return sample;}}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}finally{process.Dispose();}}return null;}
 internal static HardwareLogSample ReadFile(string path,DateTime now){
  using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
   byte[] bytes=new byte[65536];int n=stream.Read(bytes,0,bytes.Length);string first=Encoding.UTF8.GetString(bytes,0,n);int end=first.IndexOf('\n');if(end<0)return null;string[] ids=first.Substring(0,end).Trim('\r','\uFEFF').Split(',');
   stream.Position=Math.Max(0,stream.Length-bytes.Length);n=stream.Read(bytes,0,bytes.Length);string tail=Encoding.UTF8.GetString(bytes,0,n);var lines=tail.Split('\n');
   // The last entry can still be in the writer's buffer. Only consume newline-terminated records.
   for(int row=lines.Length-2;row>=0;row--){var fields=lines[row].TrimEnd('\r').Split(',');DateTime time;if(fields.Length!=ids.Length||!DateTime.TryParse(fields[0],CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out time))continue;time=time.ToUniversalTime();if(now<time||now-time>TimeSpan.FromSeconds(45))return null;var sample=new HardwareLogSample{Utc=time};
    for(int i=1;i<ids.Length;i++){double v;if(!ids[i].Contains("/temperature/")||!Double.TryParse(fields[i],NumberStyles.Float,CultureInfo.InvariantCulture,out v)||Double.IsNaN(v)||Double.IsInfinity(v)||v<=0||v>=150)continue;string id=ids[i];if(id.StartsWith("/intelcpu/")||id.StartsWith("/amdcpu/")||id.StartsWith("/cpu/"))sample.Cpu=Math.Max(sample.Cpu??v,v);else if(id.StartsWith("/nvidiagpu/")||id.StartsWith("/amdgpu/")||id.StartsWith("/intelgpu/"))sample.Gpu=Math.Max(sample.Gpu??v,v);else if(id.StartsWith("/hdd/")||id.StartsWith("/nvme/")||id.StartsWith("/ssd/"))sample.Disk=Math.Max(sample.Disk??v,v);}
    return sample;
   }return null;
  }
 }
}
