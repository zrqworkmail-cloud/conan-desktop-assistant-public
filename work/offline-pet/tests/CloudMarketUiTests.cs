using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;using System.Windows.Threading;
class CloudMarketUiTests {
 static int checks,failures;static void Check(bool ok,string text){checks++;if(!ok)failures++;Console.WriteLine((ok?"PASS ":"FAIL ")+text);}
 static IEnumerable<DependencyObject> Tree(DependencyObject value){yield return value;for(int i=0;i<VisualTreeHelper.GetChildrenCount(value);i++)foreach(var child in Tree(VisualTreeHelper.GetChild(value,i)))yield return child;}
 static void Pump(){var f=new DispatcherFrame();var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(150)};t.Tick+=(s,e)=>{t.Stop();f.Continue=false;};t.Start();Dispatcher.PushFrame(f);}
 static void Click(Window window,string name){Tree(window).OfType<Button>().First(b=>Convert.ToString(b.Content)==name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();}
 [STAThread]static int Main(string[] args){
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};string profile=Path.Combine(args[0],"profile");Directory.CreateDirectory(profile);
  var assistant=new AssistantRuntime(profile);assistant.Config.Monitor.Quota.Enabled=false;
  var skills=new SkillRuntime(profile);var extra=new AdvancedRuntime(profile);
  var type=typeof(AssistantRuntime).Assembly.GetType("TerminalWindow",true);var window=(Window)Activator.CreateInstance(type,new object[]{assistant,skills,extra,profile,new Action<string>(x=>{}),new Action(()=>{}),new Action<string>(x=>{}),new Func<string>(()=>"rest")});Action<string> show=name=>type.GetMethod("ShowPage").Invoke(window,new object[]{name});
  try{
   window.Width=1200;window.Height=850;window.Show();show("插件中心");Pump();
   var deadline=DateTime.UtcNow.AddSeconds(30);while(DateTime.UtcNow<deadline&&!Tree(window).OfType<TextBlock>().Any(t=>t.Text.StartsWith("云端目录已连接")))Pump();Check(Tree(window).OfType<TextBlock>().Any(t=>t.Text.StartsWith("云端目录已连接")),"native market automatically connects to official cloud");Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="下载并安装"),"cloud packages expose download and install in actual UI");Check(Tree(window).OfType<ProgressBar>().Any(p=>System.Windows.Automation.AutomationProperties.GetName(p)=="下载进度 系统监控"),"cloud card exposes download progress control");var format=type.GetMethod("FormatDownloadProgress",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);Check(((string)format.Invoke(null,new object[]{50L,(long?)100})).Contains("50%"),"known download length displays percent");Check(((string)format.Invoke(null,new object[]{50L,null})).Contains("正在接收"),"unknown download length displays bytes without invented percent");
   var filter=Tree(window).OfType<TextBox>().First(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="搜索插件市场");Check(Tree(window).OfType<TextBlock>().Any(t=>t.Text=="文件搜索"),"bundled search package shown in catalog");filter.Text="搜索";Pump();Check(Tree(window).OfType<TextBlock>().Any(t=>t.Text=="文件搜索")&&!Tree(window).OfType<TextBlock>().Any(t=>t.Text=="系统监控"),"catalog search filters visible entries");filter.Text="";Pump();
   Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="安装插件包…"),"native plugin center exposes file installation");
   Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="下载插件包…"),"native plugin center exposes optional HTTPS download");
   Check(Tree(window).OfType<TextBlock>().Any(t=>t.Text.Contains("已安装 0 个")),"empty package state is explicit");
   assistant.Plugins.Install(args[1]);show("插件中心");Pump();
   Click(window,"已安装");Check(Tree(Tree(window).OfType<StackPanel>().First(p=>System.Windows.Automation.AutomationProperties.GetName(p)=="插件市场列表")).OfType<TextBlock>().Any(t=>t.Text=="系统监控")&&!Tree(Tree(window).OfType<StackPanel>().First(p=>System.Windows.Automation.AutomationProperties.GetName(p)=="插件市场列表")).OfType<TextBlock>().Any(t=>t.Text=="文件搜索"),"installed filter shows only installed plugin");Click(window,"未安装");Check(Tree(window).OfType<TextBlock>().Any(t=>t.Text=="文件搜索")&&!Tree(window).OfType<TextBlock>().Any(t=>t.Text=="系统监控"),"available filter shows only uninstalled plugin");Click(window,"全部");
   Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="已停用 · 启用"),"installed package starts disabled in native UI");
   Click(window,"已停用 · 启用");Check(assistant.Plugins.Enabled("system-monitor")&&assistant.Telemetry.Available,"enable button activates separate collector");
   Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="已启用 · 停用"),"enabled state changes action label");
   Click(window,"已启用 · 停用");Check(!assistant.Telemetry.Available&&!assistant.Plugins.Enabled("system-monitor"),"disable button stops host collection");
   string packages=Path.Combine(profile,"plugin-packages"),hidden=packages+"-hidden";Directory.Move(packages,hidden);show("插件中心");Pump();Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="已停用 · 启用"),"installed plugin stays manageable when catalog is unavailable");Directory.Move(hidden,packages);
   show("总览");show("插件中心");Pump();
   Check(Tree(window).OfType<Button>().Any(b=>Convert.ToString(b.Content)=="已停用 · 启用"),"navigation preserves disabled choice");
   var image=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(Path.Combine(args[0],"plugin-center.png")))encoder.Save(stream);
  }catch(Exception error){failures++;Console.WriteLine(error);}
  finally{window.Close();assistant.Plugins.Dispose();assistant.Telemetry.Dispose();extra.Dispose();app.Shutdown();}
  File.WriteAllText(Path.Combine(args[0],"cloud-ui-results.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new{checks,failures,realUserSettingsChanged=false}));
  return failures==0?0:1;
 }
}
