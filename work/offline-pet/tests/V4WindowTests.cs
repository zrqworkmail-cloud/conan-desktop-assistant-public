using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Web.Script.Serialization;
class V4WindowTests {
 static int failures;static void Check(string name,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
 static object Field(object value,string name){return value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
 static Button FindButton(DependencyObject root,string name){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);var b=c as Button;if(b!=null&&Convert.ToString(b.Content)==name)return b;var found=FindButton(c,name);if(found!=null)return found;}return null;}
 static int ButtonCount(DependencyObject root){int count=0;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);if(c is Button)count++;count+=ButtonCount(c);}return count;}
 static void Shot(Window w,string path){w.UpdateLayout();var client=(FrameworkElement)w.Content;var bmp=new RenderTargetBitmap((int)client.ActualWidth,(int)client.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(client);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(path))png.Save(f);}
 [STAThread]static int Main(string[] args){
  string home=AppDomain.CurrentDomain.BaseDirectory;Environment.SetEnvironmentVariable("CONAN_NOTIFICATION_DIR",Path.Combine(home,"notification-test"));Environment.SetEnvironmentVariable("CONAN_PET_EVENT_DIR",Path.Combine(home,"events"));var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var runtime=new AssistantRuntime(home);var skill=new SkillRuntime(home);var advanced=new AdvancedRuntime(home);string mode="rest",opened=null;var w=new TerminalWindow(runtime,skill,advanced,home,p=>opened=p,()=>opened="assistant",m=>mode=m,()=>mode);
  w.ToolPage=p=>{opened=p;return new TextBlock{Text=p};};
  try{
   Check("notification capture defaults off",!runtime.SystemNotifications.Config.Enabled);runtime.Poll();Thread.Sleep(500);var snapshot=runtime.Poll();Check("CPU is a real bounded measurement",snapshot.Sample.Cpu.HasValue&&snapshot.Sample.Cpu>=0&&snapshot.Sample.Cpu<=100);Check("memory uses measured physical capacity",snapshot.Sample.MemoryTotal>0&&snapshot.Sample.MemoryUsed>0&&snapshot.Sample.MemoryUsed<=snapshot.Sample.MemoryTotal);Check("network difference supplies bytes per second",snapshot.Sample.Download.HasValue&&snapshot.Sample.Download>=0);Console.WriteLine("GPU source: "+snapshot.Sample.GpuState);
   w.Show();w.Refresh(snapshot);for(int i=0;i<5;i++){Thread.Sleep(180);snapshot=runtime.Poll();w.Refresh(snapshot);}w.UpdateLayout();Check("generated background is packaged",File.Exists(Path.Combine(home,"detective-console-background-v1.png")));Check("native theme palette is loaded",w.FindResource("DeskSurface") is SolidColorBrush);
   Directory.CreateDirectory(args[0]);foreach(string page in new[]{"总览","性能监控","计划与专注","通知中心","动作与移动","效率工具","总览"}){w.ShowPage(page);w.UpdateLayout();Check("native page renders "+page,ButtonCount(w)>=8);if(page=="总览"||page=="通知中心"||page=="动作与移动")Shot(w,Path.Combine(args[0],"v4-"+page+".png"));}
   w.ShowPage("动作与移动");w.UpdateLayout();FindButton(w,"四角巡逻").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check("movement button changes actual mode",mode=="patrol");w.ShowPage("效率工具");w.UpdateLayout();FindButton(w,"便签").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check("skill button calls existing functional page",opened=="便签");w.ShowPage("总览");w.Width=1000;w.Height=700;w.UpdateLayout();Check("compact window remains navigable",ButtonCount(w)>=8);Shot(w,Path.Combine(args[0],"v4-compact.png"));w.Width=1440;w.Height=1320;w.UpdateLayout();Shot(w,Path.Combine(args[0],"v4-console.png"));
   var inbox=runtime.SystemNotifications;inbox.Config.Enabled=true;inbox.Config.TitleOnly=true;string folder=Environment.GetEnvironmentVariable("CONAN_NOTIFICATION_DIR");var json=new JavaScriptSerializer();var notice=new SystemNotice{Id="fixture-1",App="测试应用",AppId="Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",Title="测试标题",Body="只在测试夹具中存在的正文",Utc=DateTime.UtcNow};File.WriteAllText(Path.Combine(folder,"inbox","1.json"),json.Serialize(notice));var fresh=inbox.Poll();Check("explicit opt in accepts valid new notification",fresh.Count==1);Check("title only drops notification body",fresh[0].Body=="");File.WriteAllText(Path.Combine(folder,"inbox","2.json"),json.Serialize(notice));Check("duplicate notification not replayed",inbox.Poll().Count==0);inbox.Block("测试应用");notice.Id="fixture-2";File.WriteAllText(Path.Combine(folder,"inbox","3.json"),json.Serialize(notice));Check("source filtering rejects blocked application",inbox.Poll().Count==0);notice.App="其他应用";notice.Utc=DateTime.UtcNow.AddMinutes(-10);File.WriteAllText(Path.Combine(folder,"inbox","4.json"),json.Serialize(notice));Check("stale notification not replayed",inbox.Poll().Count==0);Check("unsafe application target rejected",!DesktopActions.OpenNotificationApp("x\" & calc.exe"));
   var trend=(TrendView)Field(w,"trend");for(int i=0;i<100;i++)trend.Add(new SystemSample{Utc=DateTime.UtcNow.AddSeconds(i),Cpu=i%100});Check("chart history has a fixed capacity",trend.Samples.Count==60);
   skill.Engine.StartFocus(DateTime.UtcNow);skill.Tick(DateTime.UtcNow.AddMinutes(26));var restored=new SkillRuntime(home);Check("focus daily history survives restart",restored.Engine.Data.FocusDays.Values.Sum()==25);
  }catch(Exception e){Console.WriteLine(e);failures++;}finally{w.Close();advanced.Dispose();app.Shutdown();}Console.WriteLine("Failures: "+failures);return failures==0?0:1;
 }
}
