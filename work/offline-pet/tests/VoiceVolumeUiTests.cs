using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class VoiceVolumeUiTests {
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
 pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();((DispatcherTimer)Get(pet,"ticker")).Stop();((DispatcherTimer)Get(pet,"skillTicker")).Stop();Call(pet,"OpenTerminal");var w=(TerminalWindow)Get(pet,"terminal");w.Width=1440;w.Height=1000;w.ShowPage("动作与移动");w.UpdateLayout();var voice=(VoiceRuntime)Get(pet,"voices");
 var quick=Find<Slider>(w,x=>System.Windows.Automation.AutomationProperties.GetName(x)=="柯南原声音量");
 Check("quick volume is visible in companion settings",quick!=null&&quick.IsVisible);
 Check("new installation starts at twenty percent",Math.Abs(voice.Settings.Volume-.2)<.001&&quick.Value==20);
 quick.Value=15;Check("quick slider updates runtime volume",Math.Abs(voice.Settings.Volume-.15)<.001);
 using(var restored=new VoiceRuntime(AppDomain.CurrentDomain.BaseDirectory))Check("volume is saved immediately",Math.Abs(restored.Settings.Volume-.15)<.001);
 var label=Find<TextBlock>(w,x=>System.Windows.Automation.AutomationProperties.GetName(x)=="柯南原声音量值");Check("percentage is displayed",label.Text=="15%");
 Click(w,"B · 月下基德");Check("theme preserves slider instance and value",Object.ReferenceEquals(quick,Find<Slider>(w,x=>System.Windows.Automation.AutomationProperties.GetName(x)=="柯南原声音量"))&&quick.Value==15);Shot(w,Path.Combine(args[0],"companion-volume.png"));
 quick.Value=0;Check("zero percent blocks actual playback",voice.Settings.Volume==0&&!voice.Preview(voice.Catalog.Clips[0]));Check("zero percent shows mute status",label.Text=="0% · 静音");
 Click(w,"原声配音");w.UpdateLayout();var full=Find<Slider>(w,x=>true);Check("voice page reads quick volume",full.Value==0);full.Value=35;w.BackPage();w.UpdateLayout();quick=Find<Slider>(w,x=>System.Windows.Automation.AutomationProperties.GetName(x)=="柯南原声音量");Check("return reads voice page changes",quick.Value==35);
 voice.SetVolume(.1);voice.Apply();Check("runtime updates quick slider live",quick.Value==10);
 Click(w,"原声配音");w.UpdateLayout();full=Find<Slider>(w,x=>true);Check("cached voice page stays synchronized",full.Value==10);
 var master=Find<CheckBox>(w,c=>Convert.ToString(c.Content)=="启用原声配音");master.IsChecked=false;master.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));Check("master switch disables both sliders",!quick.IsEnabled&&!full.IsEnabled);master.IsChecked=true;master.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));Check("reenable retains chosen level",quick.IsEnabled&&full.IsEnabled&&full.Value==10);
 using(var restored=new VoiceRuntime(AppDomain.CurrentDomain.BaseDirectory))Check("restart restores selected volume",restored.Settings.Enabled&&Math.Abs(restored.Settings.Volume-.1)<.001);
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(pet!=null)pet.Close();app.Shutdown();}Console.WriteLine("Checks: "+checks+" Failures: "+failures);return failures==0?0:1;}
}
