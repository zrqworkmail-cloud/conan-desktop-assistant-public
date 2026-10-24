using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class VoiceWindowTests {
 static int failures;static void Check(string name,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(!value)failures++;}
 static void Click(FrameworkElement item){item.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=Mouse.PreviewMouseUpEvent});}
 static bool Pump(Func<bool> done,int seconds){var frame=new DispatcherFrame();DateTime until=DateTime.UtcNow.AddSeconds(seconds);var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(30)};timer.Tick+=(s,e)=>{if(done()||DateTime.UtcNow>=until){timer.Stop();frame.Continue=false;}};timer.Start();Dispatcher.PushFrame(frame);return done();}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};string home=AppDomain.CurrentDomain.BaseDirectory;VoiceRuntime voice=null;Window window=null;try{
  voice=new VoiceRuntime(home);Check("all 29 clips listed",voice.Catalog.Clips.Count==29);Check("every clip packaged",voice.Catalog.Clips.All(c=>File.Exists(Path.Combine(home,"voices",c.File))));
  foreach(var clip in voice.Catalog.Clips){var player=new MediaPlayer{Volume=0};bool ready=false,failed=false;player.MediaOpened+=(s,e)=>ready=true;player.MediaFailed+=(s,e)=>failed=true;player.Open(new Uri(Path.Combine(home,"voices",clip.File)));Pump(()=>ready||failed,6);Check("native MP3 decode "+clip.Id,ready&&!failed&&player.NaturalDuration.HasTimeSpan&&player.NaturalDuration.TimeSpan.TotalSeconds>0);player.Close();}
  foreach(var pair in new[]{new[]{"click","questclear"},new[]{"search-found","questclear"},new[]{"task-complete","lose"},new[]{"focus-complete","lose"},new[]{"focus-start","skill2"},new[]{"search-error","damage1"}}){using(var routed=new VoiceRuntime(home)){routed.Settings.Volume=.01;Check("semantic event selects recording "+pair[0],routed.PlayEvent(pair[0],null,false)&&routed.CurrentId==pair[1]);}}
  using(var optional=new VoiceRuntime(home)){Check("reminder voice defaults off",!optional.Settings.Reminders&&!optional.PlayEvent("reminder","r",false));optional.Settings.Reminders=true;optional.Settings.Volume=.01;Check("reminder opt in uses short cue",optional.PlayEvent("reminder","r",false)&&optional.CurrentId=="skill2");}
  voice.Settings.Volume=.01;var sample=voice.Catalog.Clips.First(c=>c.Id=="questclear");Check("manual preview accepted",voice.Preview(sample));Check("real playback opens",Pump(()=>voice.OpenedCount>0,8));var playerField=typeof(VoiceRuntime).GetField("player",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var live=(MediaPlayer)playerField.GetValue(voice);Check("playback clock advances",Pump(()=>live.Position.TotalMilliseconds>150,3));Check("real playback finishes",Pump(()=>voice.EndedCount>0,8));
  voice.Preview(sample);voice.Settings.Enabled=false;voice.Apply();Check("mute immediately stops",!voice.Playing);Check("mute blocks preview",!voice.Preview(sample));using(var restored=new VoiceRuntime(home))Check("mute and volume persist",!restored.Settings.Enabled&&Math.Abs(restored.Settings.Volume-.01)<.0001);
  voice.Settings.Enabled=true;voice.SetVolume(.45);voice.Apply();var panel=new VoicePanel(voice);window=new Window{Content=panel,Width=900,Height=900,Background=TerminalStyle.BrushOf("#06101D"),Foreground=TerminalStyle.Text};window.Resources.MergedDictionaries.Add(TerminalStyle.Resources());window.Show();window.UpdateLayout();Check("voice controls render",panel.ActualHeight>500);
  var list=panel.Children.OfType<ListBox>().Single();var instant=panel.Children.OfType<CheckBox>().Single(c=>c.Content.ToString()=="点击语音直接试听");
  Check("click preview defaults enabled",voice.Settings.ClickToPreview&&instant.IsChecked==true);Check("opening page does not autoplay",!voice.Playing);
  list.SelectedItem=sample;list.ScrollIntoView(sample);window.UpdateLayout();Check("selection alone does not autoplay",!voice.Playing);var item=(ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(sample);Click(item);Check("click starts selected recording",voice.Playing&&voice.CurrentId==sample.Id);Check("clicked recording opens",Pump(()=>voice.OpenedCount>1,6));int opened=voice.OpenedCount;Click(item);Check("same item can replay",Pump(()=>voice.OpenedCount>opened,6));
  var other=voice.Catalog.Clips.First(c=>c.Id=="rensa09");list.SelectedItem=other;list.ScrollIntoView(other);window.UpdateLayout();Click((ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(other));Check("next click replaces recording",voice.Playing&&voice.CurrentId==other.Id);voice.Stop();
  instant.IsChecked=false;instant.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Click(item);Check("disabled click only selects",!voice.Playing&&!voice.Settings.ClickToPreview);using(var saved=new VoiceRuntime(home))Check("click switch persists",!saved.Settings.ClickToPreview);
  voice.Preview(sample);Check("manual preview works with switch off",voice.Playing);voice.Stop();instant.IsChecked=true;instant.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Check("enabling switch does not autoplay",!voice.Playing);Click(list);Check("list background does not play",!voice.Playing);
  Directory.CreateDirectory(args[0]);window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)panel.ActualWidth,(int)panel.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(panel);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(args[0],"v5.4-voice-controls.png")))png.Save(f);panel.Detach();
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(window!=null)window.Close();if(voice!=null)voice.Dispose();app.Shutdown();}Console.WriteLine("Failures: "+failures);return failures==0?0:1;}
}
