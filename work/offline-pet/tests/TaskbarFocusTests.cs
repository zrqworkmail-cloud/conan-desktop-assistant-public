using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
class TaskbarFocusTests {
 [StructLayout(LayoutKind.Sequential)]struct R{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct P{public int X,Y;}
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string c,string n);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out R r);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(P p);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int ht,uint flags);
 static int checks,failures;
 static void Check(bool ok,string name){checks++;if(!ok)failures++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);}
 static void Pump(int ms){var f=new DispatcherFrame();var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};t.Tick+=(s,e)=>{t.Stop();f.Continue=false;};t.Start();Dispatcher.PushFrame(f);}
 static bool Queued(TaskbarMonitor m){return (bool)typeof(TaskbarMonitor).GetField("shellRefreshQueued",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m);}
 static void Event(TaskbarMonitor m,uint kind,IntPtr hwnd){typeof(TaskbarMonitor).GetMethod("OnShellEvent",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m,new object[]{IntPtr.Zero,kind,hwnd,0,0,0u,0u});}
 sealed class Cover:Forms.Form{protected override bool ShowWithoutActivation{get{return true;}}protected override Forms.CreateParams CreateParams{get{var c=base.CreateParams;c.ExStyle|=0x08000000;return c;}}}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var m=new TaskbarMonitor();var cover=new Cover();try{
  var c=new MonitorConfig{Taskbar=true,TaskbarSide="right"};c.Appearance.Transparent=false;
  m.Update(new SystemSample{Cpu=55,MemoryUsed=8,MemoryTotal=16,Utc=DateTime.UtcNow},c);Pump(80);
  var shell=FindWindow("Shell_TrayWnd",null);Check(shell!=IntPtr.Zero,"primary taskbar exists");
  Check(TaskbarMonitor.IsShellTaskbar(shell),"primary taskbar classified");Check(!TaskbarMonitor.IsShellTaskbar(m.Handle),"own window rejected");Check(!TaskbarMonitor.IsShellTaskbar(IntPtr.Zero),"null rejected");Check(m.ShellHooksActive,"both native event hooks installed");Check(IsWindowVisible(m.Handle),"isolated monitor visible");
  R r;GetWindowRect(m.Handle,out r);var point=new P{X=(r.Left+r.Right)/2,Y=(r.Top+r.Bottom)/2};
  var foreground=GetForegroundWindow();cover.FormBorderStyle=Forms.FormBorderStyle.None;cover.ShowInTaskbar=false;cover.BackColor=System.Drawing.Color.Purple;cover.Show();SetWindowPos(cover.Handle,new IntPtr(-1),r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top,0x10|0x40);Pump(40);
  Check(WindowFromPoint(point)==cover.Handle,"opaque topmost window reproduces occlusion");
  Event(m,3,cover.Handle);Check(!Queued(m),"unrelated foreground event ignored");
  Event(m,0x8001,shell);Check(!Queued(m),"unrelated shell event ignored");
  Event(m,3,shell);Check(Queued(m),"shell foreground event queued without polling");for(int i=0;i<10;i++)Event(m,3,shell);Check(Queued(m),"burst events coalesced");Pump(80);
  Check(!Queued(m),"queued refresh completed");Check(WindowFromPoint(point)==m.Handle,"monitor restored above opaque cover");Check(GetForegroundWindow()==foreground,"restoration does not steal foreground");
  R after;GetWindowRect(m.Handle,out after);Check(r.Left==after.Left&&r.Top==after.Top&&r.Right==after.Right&&r.Bottom==after.Bottom,"position and size preserved");Check(!c.Appearance.Transparent&&c.Taskbar,"settings unchanged");
  c.Taskbar=false;m.Position();Check(!IsWindowVisible(m.Handle),"disabled monitor hidden");Event(m,3,shell);m.RestoreShellLayer();Pump(40);Check(!IsWindowVisible(m.Handle)&&!Queued(m),"shell event cannot reenable disabled widget");
  c.Taskbar=true;m.Update(new SystemSample{Cpu=45,Utc=DateTime.UtcNow},c);Pump(40);Event(m,3,shell);Check(Queued(m),"refresh pending before disposal");m.Dispose();Pump(80);Check(!m.ShellHooksActive,"native hooks released on disposal");Check(m.IsDisposed,"queued callback tolerates disposal");
 }catch(Exception e){failures++;Console.WriteLine(e);}finally{cover.Dispose();m.Dispose();app.Shutdown();}
 var output=args.Length>0?args[0]:".";Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"focus-results.json"),new JavaScriptSerializer().Serialize(new{checks,failures,physical_taskbar_click="not simulated; actual user reproduction recorded separately"}));return failures==0?0:1;}
}
