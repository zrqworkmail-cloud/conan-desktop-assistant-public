using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class SignatureTests {
 static int failures,checks;static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}static void Call(object o,string n,params object[] a){o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,a);}static void Check(string n,bool v){checks++;Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)failures++;}
 static void Pump(double seconds){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(seconds)};timer.Tick+=(s,e)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window pet=null;try{pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();var voice=(VoiceRuntime)Field(pet,"voices");var sprites=(SpriteActions)Field(pet,"signature");voice.SetVolume(.01);
  foreach(var id in new[]{"deduction","watch","bowtie","kick","skate","count","praise","alert"}){Check("loaded "+id,sprites.Has(id));var a=sprites.Frame(id,0);Check("frame advances "+id,!Object.ReferenceEquals(a,sprites.Frame(id,.6)));Check("canonical cell "+id,a.PixelWidth==192&&a.PixelHeight==208);}
  foreach(var clip in voice.Catalog.Clips){Check("mapped "+clip.Id,sprites.Has(clip.Action)&&!String.IsNullOrEmpty(clip.Emotion));int opened=voice.OpenedCount;voice.Preview(clip);var wait=Stopwatch.StartNew();while(voice.OpenedCount==opened&&wait.Elapsed.TotalSeconds<3)Pump(.02);Check("real playback drives pose "+clip.Id,voice.OpenedCount>opened&&(string)Field(pet,"signatureAction")==clip.Action&&(int)Field(pet,"signatureCount")==clip.Count);voice.Stop();Check("stop releases pose "+clip.Id,!(bool)Field(pet,"signatureVoice")&&(double)Field(pet,"signatureUntil")==0);}
  var one=voice.Catalog.Clips.First(c=>c.Count==1);var five=voice.Catalog.Clips.First(c=>c.Count==5);Check("spoken count selects matching fingers",!Object.ReferenceEquals(sprites.Frame("count",.4,one.Count),sprites.Frame("count",.4,five.Count)));Check("alarm and frustration are distinct",!Object.ReferenceEquals(sprites.Frame("alert",.5,0,"警觉"),sprites.Frame("alert",.5,0,"懊恼")));
  voice.Preview(voice.Catalog.Clips.First(c=>c.Id=="damage1"));Pump(1.2);Check("natural end completes without stuck voice",!voice.Playing&&!(bool)Field(pet,"signatureVoice"));
  Call(pet,"SetMovement","skate");Check("skate mode controls real movement",(string)Field(pet,"moveMode")=="skate"&&File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.ini")).Contains("moveMode=skate"));Check("pet accepts file handoff",pet.AllowDrop);Call(pet,"SetMovement","rest");
  voice.Preview(voice.Catalog.Clips.First(c=>c.Id=="chance"));Pump(.15);Call(pet,"PlaySignature","praise",2.0,false);Check("manual action cannot replace voice pose",(string)Field(pet,"signatureAction")=="watch");pet.Hide();Check("hide releases active pose",!voice.Playing&&(double)Field(pet,"signatureUntil")==0);pet.Show();
  Call(pet,"OpenTerminal");var terminal=(TerminalWindow)Field(pet,"terminal");terminal.ShowPage("动作与移动");terminal.UpdateLayout();Check("equipment page has sprite previews",Field(terminal,"actionSprites")!=null);terminal.ShowPage("证物卡");terminal.ShowPage("总览");terminal.ShowPage("证物卡");Check("evidence page can reattach",Field(terminal,"evidence") is FileEvidenceView);string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"evidence-test.txt");File.WriteAllText(file,"abc",new System.Text.UTF8Encoding(false));terminal.OpenEvidence(file);var evidence=Field(terminal,"evidence");Pump(.3);Check("actual file SHA256",(string)Field(evidence,"hash")=="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");terminal.OpenEvidence(file+".missing");Pump(.05);Check("missing file clears old evidence",Field(evidence,"hash")==null&&Field(evidence,"selected")==null);
  terminal.ShowPage("动作与移动");terminal.UpdateLayout();var element=(FrameworkElement)terminal.Content;var image=new RenderTargetBitmap((int)element.ActualWidth,(int)element.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(element);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));Directory.CreateDirectory(args[0]);using(var f=File.Create(Path.Combine(args[0],"v5.2-equipment-native.png")))png.Save(f);
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(pet!=null)pet.Close();app.Shutdown();}Console.WriteLine("Checks: "+checks+" Failures: "+failures);return failures==0?0:1;}
}
