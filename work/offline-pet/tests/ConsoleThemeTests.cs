using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class ConsoleThemeTests {
 static int checks,failures;
 static void Check(string name,bool ok){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static void Pump(){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 static object Get(object value,string name){return value.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);}
 static void Call(object value,string name){value.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,null);}
 static T Find<T>(DependencyObject item,Func<T,bool> predicate) where T:DependencyObject {var typed=item as T;if(typed!=null&&predicate(typed))return typed;for(int i=0;i<VisualTreeHelper.GetChildrenCount(item);i++){var result=Find<T>(VisualTreeHelper.GetChild(item,i),predicate);if(result!=null)return result;}return null;}
 static Button Button(DependencyObject root,string text){return Find<Button>(root,b=>Convert.ToString(b.Content)==text);}
 static void Click(DependencyObject root,string text){var b=Button(root,text);if(b==null)throw new Exception("Missing button "+text);b.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));}
 static void Shot(Window w,string path){w.UpdateLayout();var root=(FrameworkElement)w.Content;var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create(path))png.Save(file);}
 [STAThread]static int Main(string[] args){Directory.CreateDirectory(args[0]);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window pet=null;try{
  pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();((DispatcherTimer)Get(pet,"ticker")).Stop();((DispatcherTimer)Get(pet,"skillTicker")).Stop();Call(pet,"OpenTerminal");var w=(TerminalWindow)Get(pet,"terminal");w.Width=1440;w.Height=1000;w.ShowPage("动作与移动");w.UpdateLayout();
  Check("native A starts as default",w.Theme=="a");Check("return visible on primary page",Find<Button>(w,b=>Convert.ToString(b.Content)=="← 返回总览")!=null);
  Click(w,"四角巡逻");Check("selection updates real movement",Convert.ToString(Get(pet,"moveMode"))=="patrol");
  var gaze=Find<CheckBox>(w,c=>Convert.ToString(c.Content)=="视线跟随");gaze.IsChecked=true;gaze.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));Check("switch updates actual pet flag",(bool)Get(pet,"gaze"));
  var state=gaze.Template.FindName("State",gaze) as TextBlock;Check("custom switch shows active text",state!=null&&state.Text=="已开启");
  Shot(w,Path.Combine(args[0],"console-a-movement.png"));
  var modeButton=Button(w,"四角巡逻");Click(w,"B · 月下基德");w.UpdateLayout();Check("B theme activates",w.Theme=="b");Check("theme does not rebuild current controls",Object.ReferenceEquals(modeButton,Button(w,"四角巡逻")));Check("theme preserves actual movement",Convert.ToString(Get(pet,"moveMode"))=="patrol");Check("theme preserves switch value",gaze.IsChecked==true);Check("theme choice persists to preferences",File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.ini")).Contains("terminalTheme=b"));Check("Kid decoration rendered",Find<KidEmblem>(w,x=>true)!=null);
  Check("light theme uses light surfaces",((SolidColorBrush)w.FindResource("DeskSurface")).Color.R>240);Shot(w,Path.Combine(args[0],"console-b-movement.png"));
  Click(w,"原声配音");w.UpdateLayout();Check("nested voice page opens",Find<TextBlock>(w,t=>t.Text=="柯南 · 日语原声")!=null);Check("nested return names parent",Button(w,"← 返回动作与移动")!=null);
  var volume=Find<Slider>(w,x=>true);volume.Value=35;var voices=(VoiceRuntime)Get(pet,"voices");Check("slider changes real voice volume",Math.Abs(voices.Settings.Volume-.35)<.001);Click(w,"A · 柯南");Check("theme preserves voice editor value",volume.Value==35);var master=Find<CheckBox>(w,c=>Convert.ToString(c.Content)=="启用原声配音");master.IsChecked=false;master.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));Check("voice master disables volume",!volume.IsEnabled&&!voices.Settings.Enabled);master.IsChecked=true;master.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));Check("voice enable restores previous volume",volume.IsEnabled&&volume.Value==35);Shot(w,Path.Combine(args[0],"console-a-voice.png"));Click(w,"B · 月下基德");Shot(w,Path.Combine(args[0],"console-b-voice.png"));
  w.BackPage();w.UpdateLayout();Check("back returns to movement settings",Button(w,"四角巡逻")!=null);Check("back preserves saved selection",System.Windows.Automation.AutomationProperties.GetItemStatus(Button(w,"四角巡逻"))=="已选中");
  w.ShowPage("便签");w.UpdateLayout();var title=Find<TextBox>(w,t=>System.Windows.Automation.AutomationProperties.GetName(t)=="便签标题");title.Text="未保存的主题切换验证";Click(w,"A · 柯南");Check("theme preserves unsaved text",title.Text=="未保存的主题切换验证");Check("theme keeps same text editor",Object.ReferenceEquals(title,Find<TextBox>(w,t=>System.Windows.Automation.AutomationProperties.GetName(t)=="便签标题")));
  w.ShowPage("下班倒计时");w.UpdateLayout();Click(w,"＋下午");w.UpdateLayout();Check("schedule selector remains available",Find<ComboBox>(w,c=>System.Windows.Automation.AutomationProperties.GetName(c).Contains("下班小时"))!=null);var hour=Find<ComboBox>(w,c=>System.Windows.Automation.AutomationProperties.GetName(c).Contains("下班小时"));hour.BringIntoView();w.UpdateLayout();Pump();hour.IsDropDownOpen=true;w.UpdateLayout();Pump();var popup=hour.Template.FindName("PART_Popup",hour) as System.Windows.Controls.Primitives.Popup;Check("custom option popup opens",popup!=null&&popup.IsOpen&&popup.Child!=null);hour.SelectedIndex=19;hour.IsDropDownOpen=false;Check("custom selector retains chosen hour",hour.SelectedIndex==19);Click(w,"B · 月下基德");Shot(w,Path.Combine(args[0],"console-b-reminders.png"));
  w.ShowPage("总览");w.UpdateLayout();Check("root has no back button",Find<Button>(w,b=>b.Visibility==Visibility.Visible&&Convert.ToString(b.Content).StartsWith("← 返回"))==null);Shot(w,Path.Combine(args[0],"console-b-overview.png"));
  foreach(var route in new[]{"性能监控","计划与专注","文件与启动","效率工具","开发助手","通知中心","动作与移动","动作演示","原声配音","动作与移动"}){w.ShowPage(route);w.UpdateLayout();Check("native route renders "+route,Find<Button>(w,b=>Convert.ToString(b.Content)=="B · 月下基德")!=null);}
  w.Width=1100;w.Height=760;w.ShowPage("动作与移动");w.UpdateLayout();Check("compact settings keep theme control",Button(w,"A · 柯南").ActualWidth>0);Shot(w,Path.Combine(args[0],"console-b-compact.png"));
  w.Close();Call(pet,"OpenTerminal");var reopened=(TerminalWindow)Get(pet,"terminal");Check("reopened console restores chosen theme",reopened.Theme=="b");pet.Close();pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();((DispatcherTimer)Get(pet,"ticker")).Stop();((DispatcherTimer)Get(pet,"skillTicker")).Stop();Call(pet,"OpenTerminal");Check("new pet restores theme from disk",((TerminalWindow)Get(pet,"terminal")).Theme=="b");
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(pet!=null)pet.Close();app.Shutdown();}Console.WriteLine("Checks: "+checks+" Failures: "+failures);return failures==0?0:1;}
}
