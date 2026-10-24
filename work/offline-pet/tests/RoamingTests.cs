using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Windows;
class RoamingTests {
 static int failures;
 static object Get(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Call(object o,string n,params object[] args){o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
 static void Check(string name,bool ok){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static void Pump(double seconds){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(seconds)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window pet=null;try{
  pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();var voice=(VoiceRuntime)Get(pet,"voices");voice.Settings.Volume=.01;voice.Settings.Enabled=true;voice.Settings.Interactions=true;
  ((DispatcherTimer)Get(pet,"ticker")).Stop();((DispatcherTimer)Get(pet,"skillTicker")).Stop();((BubbleQueue)Get(pet,"messages")).Clear();
  Call(pet,"PlayIdle","kick");double kickStart=(double)Get(pet,"footballKickStart");for(int i=0;i<=108;i++)Call(pet,"TickFootball",kickStart+i/120.0,1.0/120);for(int wait=0;wait<60&&voice.OpenedCount==0;wait++)Pump(.05);Check("autonomous kick requests matching original recording",voice.Playing&&(voice.CurrentId=="finish1"||voice.CurrentId=="finish2"));voice.Stop();Call(pet,"StopFootball");
  voice.Settings.Interactions=false;typeof(ConanPet).GetField("roam",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,true);var seen=new HashSet<int>();bool search=false,kick=false;for(int i=0;i<ActionTiming.IdleActions.Length;i++){Call(pet,"RandomIdle");int index=(int)Get(pet,"lastIdle");seen.Add(index);string id=ActionTiming.IdleActions[index];if(id=="search")search=(string)Get(pet,"signatureAction")=="search";if(id=="kick")kick=(string)Get(pet,"footballMode")=="volley";}Check("one autonomous rotation uses all registered actions",seen.Count==ActionTiming.IdleActions.Length&&search&&kick);Call(pet,"StopFootball");voice.Settings.Interactions=true;
  Call(pet,"OpenTerminal");var terminal=(TerminalWindow)Get(pet,"terminal");terminal.ShowPage("提醒");terminal.UpdateLayout();var panel=(FrameworkElement)terminal.Content;Directory.CreateDirectory(args[0]);var bitmap=new RenderTargetBitmap((int)panel.ActualWidth,(int)panel.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(panel);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(args[0],"v5.6-work-end-native.png")))encoder.Save(file);Check("reminder page includes multiple work end slots",Get(Get(pet,"skillsWindow"),"endSlots")!=null);
  Pump(8.1);int opened=voice.OpenedCount;string played=null;voice.Started+=clip=>played=clip.Id;Call(pet,"NotifyWorkEnd",new SkillAlert{Key="work-end:test",Text="距离下班还有 15 分钟。",Row=3,Count=15});for(int wait=0;wait<60&&voice.OpenedCount==opened;wait++)Pump(.05);Check("countdown plays exact original number",played=="rensa15"&&voice.OpenedCount>opened);if(played!="rensa15")Console.WriteLine("Playback status: "+voice.Status);Check("countdown displays matching two hand pose",(string)Get(pet,"signatureAction")=="count"&&(int)Get(pet,"signatureCount")==15);Check("countdown preserves contextual bubble",((BubbleQueue)Get(pet,"messages")).Current.Text.Contains("15 分钟"));voice.Stop();
  voice.Settings.Interactions=false;Call(pet,"PlayIdle","deduction");Check("interaction mute keeps autonomous animation silent",!voice.Playing);
  pet.Close();pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();((DispatcherTimer)Get(pet,"ticker")).Stop();((BubbleQueue)Get(pet,"messages")).Clear();voice=(VoiceRuntime)Get(pet,"voices");voice.Settings.Volume=.01;double deadline=((Stopwatch)Get(pet,"clock")).Elapsed.TotalSeconds+20;typeof(ConanPet).GetField("walkUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,deadline);Check("departure original voice accepted",voice.PlayAmbient("travel",false));for(int wait=0;wait<60&&voice.OpenedCount==0;wait++)Pump(.05);Check("departure sound keeps running and avoids blocking bubble",voice.OpenedCount>0&&(double)Get(pet,"walkUntil")==deadline&&((BubbleQueue)Get(pet,"messages")).Current==null);
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(pet!=null)pet.Close();app.Shutdown();}Console.WriteLine("Failures: "+failures);return failures==0?0:1;}
}
