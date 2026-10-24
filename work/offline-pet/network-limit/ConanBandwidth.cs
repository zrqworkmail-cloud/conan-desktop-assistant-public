using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;
class ConanBandwidth {
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool SetDefaultDllDirectories(uint flags);
 [DllImport("WinDivert.dll",CharSet=CharSet.Ansi,CallingConvention=CallingConvention.Cdecl,SetLastError=true)]static extern IntPtr WinDivertOpen(string filter,int layer,short priority,ulong flags);
 [DllImport("WinDivert.dll",CallingConvention=CallingConvention.Cdecl,SetLastError=true)]static extern bool WinDivertRecv(IntPtr h,[Out]byte[] packet,uint length,out uint received,[Out]byte[] address);
 [DllImport("WinDivert.dll",CallingConvention=CallingConvention.Cdecl,SetLastError=true)]static extern bool WinDivertSend(IntPtr h,byte[] packet,uint length,out uint sent,byte[] address);
 [DllImport("WinDivert.dll",CallingConvention=CallingConvention.Cdecl)]static extern bool WinDivertShutdown(IntPtr h,int how);
 [DllImport("WinDivert.dll",CallingConvention=CallingConvention.Cdecl)]static extern bool WinDivertClose(IntPtr h);
 [DllImport("WinDivert.dll",CallingConvention=CallingConvention.Cdecl)]static extern bool WinDivertSetParam(IntPtr h,int param,ulong value);
 static int Main(string[] args){IntPtr handle=new IntPtr(-1);try{int owner;double mib;if((args.Length!=3&&args.Length!=4)||!Int32.TryParse(args[0],out owner)||!Double.TryParse(args[1],NumberStyles.Float,CultureInfo.InvariantCulture,out mib)||Double.IsNaN(mib)||Double.IsInfinity(mib)||mib<.1||mib>10000||!args[2].StartsWith("Local\\ConanBandwidth-"))return 2;
  if(!SetDefaultDllDirectories(0xA00))return 7;
  using(var parent=Process.GetProcessById(owner))using(var stop=EventWaitHandle.OpenExisting(args[2]+"-stop"))using(var ready=EventWaitHandle.OpenExisting(args[2]+"-ready")){
   string filter=args.Length==4&&args[3]=="--test-loopback"?"loopback and tcp.SrcPort == 53111 and tcp.PayloadLength > 0":"inbound and !loopback and !impostor and (tcp.PayloadLength > 0 or udp.PayloadLength > 0)";if(args.Length==4&&args[3]!="--test-loopback")return 2;handle=WinDivertOpen(filter,0,-1000,0);if(handle==new IntPtr(-1))return Marshal.GetLastWin32Error();WinDivertSetParam(handle,0,4096);WinDivertSetParam(handle,1,2000);WinDivertSetParam(handle,2,4194304);ready.Set();
   IntPtr active=handle;int shutdown=0;var guard=new Timer(s=>{if(parent.HasExited||stop.WaitOne(0)){if(Interlocked.Exchange(ref shutdown,1)==0){ready.Reset();WinDivertShutdown(active,1);}}},null,0,200);try{
    byte[] packet=new byte[65575],address=new byte[80];var clock=Stopwatch.StartNew();var pacer=new DownloadPacer(mib*1048576);uint length,sent;
    while(WinDivertRecv(handle,packet,(uint)packet.Length,out length,address)){
     if(Volatile.Read(ref shutdown)==0){double wait=pacer.Reserve((int)length,clock.Elapsed.TotalSeconds);long until=clock.ElapsedMilliseconds+(long)(wait*1000);while(clock.ElapsedMilliseconds<until&&!stop.WaitOne((int)Math.Min(100,Math.Max(1,until-clock.ElapsedMilliseconds)))){} }
     if(!WinDivertSend(handle,packet,length,out sent,address))return 3;
    }
    int error=Marshal.GetLastWin32Error();return Volatile.Read(ref shutdown)!=0||error==232?0:4;
   }finally{using(var completed=new ManualResetEvent(false)){guard.Dispose(completed);completed.WaitOne();}}
  }
 }catch(System.ComponentModel.Win32Exception){return 5;}catch(UnauthorizedAccessException){return 5;}catch(Exception){return 6;}finally{if(handle!=new IntPtr(-1))WinDivertClose(handle);}}
}
