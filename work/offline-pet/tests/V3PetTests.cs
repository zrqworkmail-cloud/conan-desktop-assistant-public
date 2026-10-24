using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class V3PetTests {
 static int failed;static void Check(string n,bool v){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)failed++;}
 static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
 static void Invoke(object o,string n,params object[] args){o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);}
 [STAThread]static int Main(string[] args){
  var app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;var pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);var field=typeof(ConanPet).GetField("advanced",BindingFlags.Instance|BindingFlags.NonPublic);Check("pet connects advanced skill runtime",field!=null);if(field==null){pet.Close();return 1;}
  var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};timer.Tick+=(s,e)=>{timer.Stop();try{
   var extra=(AdvancedRuntime)Field(pet,"advanced");var item=extra.Engine.AddTask("真实待办提醒",DateTime.UtcNow.AddSeconds(-1),1);Invoke(pet,"OpenSkills","总览");
   Invoke(pet,"OpenDetails",new BubbleMessage{Key="task:"+item.Id,Text="待办",Notification=true});var window=(SkillsWindow)Field(pet,"skillsWindow");Check("task details selects task page",Convert.ToString(((TabItem)((TabControl)Field(window,"tabs")).SelectedItem).Header)=="待办计划");
   Invoke(pet,"OpenDetails",new BubbleMessage{Key="file:missing",Text="文件",Notification=true});Check("file details selects file monitoring page",Convert.ToString(((TabItem)((TabControl)Field(window,"tabs")).SelectedItem).Header)=="文件观察");
   extra.Engine.Data.Hotkeys=true;extra.Save();Check("hotkeys register with an explicit status",extra.HotkeyStatus.Contains("已启用")||extra.HotkeyStatus.Contains("占用"));extra.Engine.Data.Hotkeys=false;extra.Save();Check("hotkeys can be released",extra.HotkeyStatus.Contains("未启用"));
   typeof(ConanPet).GetField("scale",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(pet,4.8);Invoke(pet,"ResizePet");Invoke(pet,"Tick",null,EventArgs.Empty);
   var sprite=(SpriteView)Field(pet,"sprite");Check("large mode uses original pixel artwork",sprite.Frame.PixelWidth==192&&sprite.Frame.PixelHeight==208);Check("large mode stays within the working area",pet.Height<=SystemParameters.WorkArea.Height+1);Invoke(pet,"Animate",7);Invoke(pet,"Tick",null,EventArgs.Empty);Check("thinking uses original animation frames",sprite.Frame.PixelHeight==208);
   var hit=sprite.InputHitTest(new Point(sprite.ActualWidth/2,sprite.ActualHeight*.55));Check("original alpha hit test finds the body",hit!=null);
   Directory.CreateDirectory(args[0]);var bitmap=new RenderTargetBitmap((int)sprite.ActualWidth,(int)sprite.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(sprite);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(Path.Combine(args[0],"v3-pixel-thinking.png")))encoder.Save(f);
   Invoke(pet,"ShowMenu");
  }catch(Exception err){Console.WriteLine(err);failed++;}finally{pet.Close();app.Shutdown();}};timer.Start();app.Run(pet);Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
