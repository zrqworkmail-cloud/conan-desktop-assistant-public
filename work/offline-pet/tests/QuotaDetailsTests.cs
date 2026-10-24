using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Web.Script.Serialization;
class QuotaDetailsTests {
 static int checks,failures;
 static void Check(string name,bool ok){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static void Pump(int milliseconds=200){var f=new DispatcherFrame();var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(milliseconds)};t.Tick+=(s,e)=>{t.Stop();f.Continue=false;};t.Start();Dispatcher.PushFrame(f);}
 static T Find<T>(DependencyObject o,Func<T,bool> match)where T:DependencyObject{var item=o as T;if(item!=null&&match(item))return item;for(int i=0;i<VisualTreeHelper.GetChildrenCount(o);i++){var r=Find<T>(VisualTreeHelper.GetChild(o,i),match);if(r!=null)return r;}return null;}
 [STAThread]static int Main(string[] args){Directory.CreateDirectory(args[0]);var now=DateTime.UtcNow;var q=new CodexQuotaSnapshot{CheckedUtc=now,NextCheckUtc=now.AddSeconds(60)};q.Windows.Add(new CodexQuotaWindow{Minutes=300,Remaining=78,ResetUtc=now.AddHours(2)});q.Windows.Add(new CodexQuotaWindow{Minutes=10080,Remaining=62,ResetUtc=now.AddDays(3)});var a=new TaskbarAppearance();
 Check("default reset countdown",TaskbarPainter.QuotaValue(q,300,a,now)=="78% · 02:00:00");
 Check("weekly day countdown",TaskbarPainter.QuotaValue(q,10080,a,now).Contains("3天"));
 Check("countdown ticks independently",TaskbarPainter.QuotaValue(q,300,a,now.AddSeconds(1))=="78% · 01:59:59");
 a.QuotaDisplay="remaining";Check("remaining only",TaskbarPainter.QuotaValue(q,300,a,now)=="78%");a.QuotaDisplay="query";Check("next query distinct",TaskbarPainter.QuotaValue(q,300,a,now).Contains("查询 01:00"));
 a.QuotaDisplay="reset";Check("reset only",TaskbarPainter.QuotaValue(q,300,a,now)=="02:00:00");
 Check("stale data hidden",TaskbarPainter.QuotaValue(q,300,a,now.AddMinutes(6))=="—");Check("future sample hidden",TaskbarPainter.QuotaValue(q,300,a,now.AddSeconds(-1))=="—");q.RequiresAuthorization=true;Check("authorization preserved",TaskbarPainter.QuotaValue(q,300,a,now)=="待授权");q.RequiresAuthorization=false;
 q.Windows[0].ResetUtc=now;Check("elapsed reset not fabricated",TaskbarPainter.QuotaValue(q,300,a,now)=="待核验");q.Windows[0].ResetUtc=now.AddHours(2);
 var json=new JavaScriptSerializer();Check("legacy settings default",json.Deserialize<TaskbarAppearance>("{}").QuotaDisplay=="remaining-reset");a.QuotaDisplay="query";Check("display mode persists",json.Deserialize<TaskbarAppearance>(json.Serialize(a)).QuotaDisplay=="query");
 var nic=System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(n=>n.OperationalStatus==System.Net.NetworkInformation.OperationalStatus.Up&&n.NetworkInterfaceType!=System.Net.NetworkInformation.NetworkInterfaceType.Loopback);if(nic!=null){var details=Telemetry.ConnectionDetails(nic,nic.GetIPv4Statistics());Check("real adapter metadata",details.Contains(nic.Name)&&details.Contains("DNS：")&&details.Contains("子网掩码：")&&details.Contains("累计接收："));}
 var cfg=new MonitorConfig();cfg.TaskbarItems=new System.Collections.Generic.List<string>{"codex5h","codex7d"};cfg.Appearance.AutoFit=false;var sample=new SystemSample{Utc=now,Quota=q};var size=TaskbarPainter.Geometry(cfg,1,100,sample).Size;q.Windows[0].Remaining=1;q.Windows[1].Remaining=9;Check("countdown column stable",TaskbarPainter.Geometry(cfg,1,100,sample).Size==size);q.Windows[0].Remaining=78;
 var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var card=new MonitorCard();card.Update(new SystemSample{Utc=now,Quota=q,Cpu=40,MemoryUsed=8,MemoryTotal=16,NetworkDetails="网卡：测试网卡\nIPv4：192.0.2.1\n网关：192.0.2.254",MonitorUptime=TimeSpan.FromMinutes(42)});card.Show();Pump();
 var quotaFields=(System.Collections.Generic.Dictionary<int,TextBlock>)typeof(MonitorCard).GetField("quotaLabels",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(card);string beforeTick=quotaFields[300].Text;Pump(1100);Check("native card timer advances without new sample",quotaFields[300].Text!=beforeTick);
 foreach(string tab in new[]{"总览","网络连接","Codex 额度"}){var b=Find<Button>(card,x=>Convert.ToString(x.Content)==tab);Check("detail tab "+tab,b!=null);if(b!=null)b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();var root=(FrameworkElement)card.Content;var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(args[0],tab+".png")))encoder.Save(file);}
 card.Update(null);var field=typeof(MonitorCard).GetField("quotaText",BindingFlags.Instance|BindingFlags.NonPublic);Check("unavailable clears old quota",!((TextBlock)field.GetValue(card)).Text.Contains("78%")&&quotaFields[300].Text=="—");card.Close();app.Shutdown();File.WriteAllText(Path.Combine(args[0],"quota-details-results.json"),"{\"checks\":"+checks+",\"failures\":"+failures+"}");return failures==0?0:1;
 }
}
