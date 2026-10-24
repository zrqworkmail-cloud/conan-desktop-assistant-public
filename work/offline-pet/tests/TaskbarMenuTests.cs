using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
class TaskbarMenuTests {
 [StructLayout(LayoutKind.Sequential)]struct Pos{public IntPtr Hwnd,After;public int X,Y,Width,Height;public uint Flags;}
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr w,IntPtr l);
 sealed class Probe:Forms.NativeWindow,IDisposable{public int Positions,Demotions;public Probe(IntPtr h){AssignHandle(h);}protected override void WndProc(ref Forms.Message m){if(m.Msg==0x46){Positions++;var p=(Pos)Marshal.PtrToStructure(m.LParam,typeof(Pos));if(p.After==new IntPtr(-2)&&(p.Flags&4)==0)Demotions++;}base.WndProc(ref m);}public void Reset(){Positions=Demotions=0;}public void Dispose(){ReleaseHandle();}}
 static int checks,failures;static void Check(bool ok,string name){checks++;if(!ok)failures++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);}
 static void Pump(int ms){var f=new DispatcherFrame();var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};t.Tick+=(s,e)=>{t.Stop();f.Continue=false;};t.Start();Dispatcher.PushFrame(f);}
 static SystemSample Sample(double download){return new SystemSample{Utc=DateTime.UtcNow,Cpu=35,MemoryUsed=8,MemoryTotal=16,Download=download,Upload=10240};}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var m=new TaskbarMonitor();try{var cfg=new MonitorConfig{Taskbar=true,TaskbarSide="right"};cfg.Appearance.Transparent=false;m.Update(Sample(1024),cfg);Pump(60);using(var probe=new Probe(m.Handle)){
 var point=new System.Drawing.Point(m.Left+10,m.Top+5);long packed=(long)(ushort)point.X|((long)(ushort)point.Y<<16);SendMessage(m.Handle,0x7B,m.Handle,new IntPtr(packed));Pump(60);var menu=m.ContextMenuStrip;Check(menu.Visible,"native context-menu message opens real popup");var bounds=m.Bounds;var menuBounds=menu.Bounds;probe.Reset();
 for(int i=0;i<5;i++){m.Update(Sample(12345.6*1048576),cfg);m.Position();m.RestoreShellLayer();m.QueueShellRefresh();Pump(40);}
 Console.WriteLine("NATIVE during menu: positioning="+probe.Positions+" demotions="+probe.Demotions);
 Check(probe.Demotions==0,"no topmost demotion while popup is open");Check(probe.Positions==0,"polling and shell refresh do not reposition menu owner");Check(m.Bounds==bounds,"owner bounds remain stable despite wider readings");Check(menu.Visible&&menu.Bounds==menuBounds,"context popup stays open at same coordinates");Check(m.Sample.Download>1000000,"live sample continues advancing while menu is open");
 menu.Close();Pump(100);Check(!menu.Visible,"context popup closes normally");Check(probe.Positions>0,"deferred positioning resumes after popup closes");Check(m.Bounds.Width>bounds.Width,"pending wider reading layout applied after closing");probe.Reset();m.QueueShellRefresh();Pump(80);Check(probe.Positions>0,"shell refresh resumes native window updates outside menu");
 SendMessage(m.Handle,0x7B,m.Handle,new IntPtr(packed));Pump(40);Check(menu.Visible,"popup can be reopened");cfg.Taskbar=false;m.Update(Sample(0),cfg);Pump(60);Check(!IsWindowVisible(m.Handle)&&!menu.Visible,"disabling widget closes its popup too");cfg.Taskbar=true;m.Update(Sample(1024),cfg);Pump(60);
 SendMessage(m.Handle,0x7B,m.Handle,new IntPtr(packed));Pump(40);m.Dispose();Pump(60);Check(m.IsDisposed&&!m.ShellHooksActive,"disposing with popup releases hooks safely");
 }}catch(Exception e){failures++;Console.WriteLine(e);}finally{m.Dispose();app.Shutdown();}Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"menu-results.json"),new JavaScriptSerializer().Serialize(new{checks,failures,physical_pointer_click_verified=false,native_context_menu_message_verified=true}));return failures==0?0:1;}
}
