using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
class MonitorPetClickTests {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 static object Get(object o,string name){return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}
 static void Pump(int ms){var f=new DispatcherFrame();var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(ms)};t.Tick+=(s,e)=>{t.Stop();f.Continue=false;};t.Start();Dispatcher.PushFrame(f);}
 [STAThread]static int Main(string[] args){int checks=0,failures=0;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};app.DispatcherUnhandledException+=(s,e)=>{Console.WriteLine("FAIL native callback "+e.Exception);e.Handled=true;failures++;};var pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);try{pet.Show();Pump(250);((DispatcherTimer)Get(pet,"ticker")).Stop();((DispatcherTimer)Get(pet,"skillTicker")).Stop();((System.Threading.Timer)Get(pet,"assistantTimer")).Dispose();var assistant=(AssistantRuntime)Get(pet,"assistant");assistant.Config.Monitor.Taskbar=true;assistant.Config.Monitor.TaskbarSide="right";assistant.Config.Monitor.ExitLookupEnabled=false;var bar=(TaskbarMonitor)Get(pet,"taskbarMonitor");bar.Update(new SystemSample{Cpu=61,MemoryUsed=8,MemoryTotal=16,Utc=DateTime.UtcNow},assistant.Config.Monitor);for(int i=0;i<12;i++){SendMessage(bar.Handle,0x202,IntPtr.Zero,new IntPtr(0x50005));Pump(100);var card=(MonitorCard)Get(pet,"monitorCard");bool ok=card!=null&&card.IsVisible&&card.Owner==pet&&card.TaskbarAnchor.HasValue;checks++;Console.WriteLine((ok?"PASS":"FAIL")+" real pet taskbar open cycle "+i);if(!ok){failures++;break;}card.Close();Pump(120);ok=Get(pet,"monitorCard")==null&&pet.IsVisible&&bar.Visible&&IsWindowVisible(bar.Handle);checks++;Console.WriteLine((ok?"PASS":"FAIL")+" close keeps pet and taskbar alive cycle "+i);if(!ok)failures++;} }catch(Exception e){Console.WriteLine(e);failures++;}finally{pet.Close();Pump(100);app.Shutdown();}Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"pet-click-results.json"),"{\"checks\":"+checks+",\"failures\":"+failures+"}");return failures==0?0:1;}
}
