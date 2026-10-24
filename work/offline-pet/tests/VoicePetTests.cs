using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class VoicePetTests {
 static int failures;static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}static void Invoke(object o,string n,params object[] a){o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,a);}static void Check(string n,bool v){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)failures++;}
 static Button Button(DependencyObject root,string name){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);var b=child as Button;if(b!=null&&Convert.ToString(b.Content)==name)return b;var found=Button(child,name);if(found!=null)return found;}return null;}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window pet=null;try{pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();var voice=(VoiceRuntime)Field(pet,"voices");voice.Settings.Volume=.01;Invoke(pet,"OpenTerminal");var terminal=(TerminalWindow)Field(pet,"terminal");terminal.ShowPage("动作与移动");terminal.UpdateLayout();Button(terminal,"原声配音").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));terminal.UpdateLayout();Check("workbench opens real voice controls",Field(terminal,"voicePanel") is VoicePanel);Check("all 29 clips available in pet",voice.Catalog.Clips.Count==29);
  var tray=(System.Windows.Forms.NotifyIcon)Field(pet,"tray");var mute=tray.ContextMenuStrip.Items.OfType<System.Windows.Forms.ToolStripMenuItem>().First(x=>x.Text=="原声静音");mute.PerformClick();Check("right menu mutes runtime",!voice.Settings.Enabled&&!voice.Playing&&mute.Checked);mute.PerformClick();Check("right menu restores runtime",voice.Settings.Enabled&&!mute.Checked);
  Invoke(pet,"PlayVoice","click",null);Check("click routes to signature recording",voice.Playing&&voice.CurrentId=="questclear");voice.Stop();voice.Settings.Enabled=false;voice.Apply();Invoke(pet,"PlayVoice","task-complete","test-task");Check("muted task cannot play",!voice.Playing);voice.Settings.Enabled=true;pet.Hide();Invoke(pet,"PlayVoice","click",null);Check("hidden pet cannot speak automatically",!voice.Playing);pet.Show();
  voice.SetVolume(.45);terminal.ShowPage("原声配音");terminal.UpdateLayout();terminal.ShowPage("总览");terminal.ShowPage("原声配音");terminal.UpdateLayout();Check("voice page reattaches without duplicate window",Field(terminal,"voicePanel") is VoicePanel);Directory.CreateDirectory(args[0]);var c=(FrameworkElement)terminal.Content;var bitmap=new RenderTargetBitmap((int)c.ActualWidth,(int)c.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(c);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(args[0],"v5.1-native-voice-page.png")))png.Save(f);
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(pet!=null)pet.Close();app.Shutdown();}Console.WriteLine("Failures: "+failures);return failures==0?0:1;}
}
