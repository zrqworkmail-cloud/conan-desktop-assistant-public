using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.Generic;
class PixelRenderingTests {
 static int failed;static void Check(string name,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+name);if(!pass)failed++;}
 static object Field(object o,string n){var f=o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance);return f==null?null:f.GetValue(o);}
 static void Invoke(object o,string n,params object[] args){o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);}
 [STAThread]static int Main(string[] args){
  var app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
  var view=new SpriteView{Width=64,Height=64};view.Display(BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,255,255,255,0,0,255,255,0,0,255,0,0,255,255},8));view.Measure(new Size(64,64));view.Arrange(new Rect(0,0,64,64));
  var rendered=new RenderTargetBitmap(64,64,96,96,PixelFormats.Pbgra32);rendered.Render(view);var pixels=new byte[64*64*4];rendered.CopyPixels(pixels,64*4,0);var colors=new HashSet<int>();for(int i=0;i<pixels.Length;i+=4)colors.Add(BitConverter.ToInt32(pixels,i));
  Check("pixel magnification introduces no interpolated colors",colors.Count==2);Check("sprite enables nearest-neighbor scaling",RenderOptions.GetBitmapScalingMode(view)==BitmapScalingMode.NearestNeighbor);Check("sprite snaps to physical device pixels",view.SnapsToDevicePixels);
  var pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};timer.Tick+=(s,e)=>{timer.Stop();try{
   typeof(ConanPet).GetField("awakeUntil",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pet,Double.MaxValue);
   typeof(ConanPet).GetField("scale",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pet,5.0);Invoke(pet,"ResizePet");Invoke(pet,"Tick",null,EventArgs.Empty);var sprite=(SpriteView)Field(pet,"sprite");pet.UpdateLayout();
   Check("large pet uses original atlas pixels",sprite.Frame.PixelWidth==192&&sprite.Frame.PixelHeight==208);
   Check("large pet retains nearest-neighbor mode",RenderOptions.GetBitmapScalingMode(sprite)==BitmapScalingMode.NearestNeighbor);
   Check("large pet fits current work area",pet.Height<=SystemParameters.WorkArea.Height+1);
   var transform=PresentationSource.FromVisual(pet).CompositionTarget.TransformToDevice;double zoom=sprite.ActualHeight*transform.M22/208;Check("large pet uses whole physical pixel magnification",Math.Abs(zoom-Math.Round(zoom))<.0001);
   var matrix=sprite.RenderTransform.Value;Check("pixel sprite is not fractionally rotated or stretched",Math.Abs(matrix.M11-1)<.00001&&Math.Abs(matrix.M22-1)<.00001&&Math.Abs(matrix.M12)<.00001&&Math.Abs(matrix.M21)<.00001);
   var tray=(System.Windows.Forms.NotifyIcon)Field(pet,"tray");bool hdMenu=false;foreach(System.Windows.Forms.ToolStripItem item in tray.ContextMenuStrip.Items){var menu=item as System.Windows.Forms.ToolStripMenuItem;if(menu!=null){if(menu.Text.Contains("高清"))hdMenu=true;foreach(System.Windows.Forms.ToolStripItem child in menu.DropDownItems)if(child.Text.Contains("高清"))hdMenu=true;}}Check("menus do not switch to rejected HD artwork",!hdMenu);
   Invoke(pet,"Animate",3);Invoke(pet,"Tick",null,EventArgs.Empty);Check("wave animation still uses original atlas frames",sprite.Frame.PixelHeight==208);
   Directory.CreateDirectory(args[0]);var preview=new SpriteView{Width=768,Height=832};preview.Display(sprite.Frame);preview.Measure(new Size(768,832));preview.Arrange(new Rect(0,0,768,832));var image=new RenderTargetBitmap(768,832,96,96,PixelFormats.Pbgra32);image.Render(preview);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var f=File.Create(Path.Combine(args[0],"pixel-pet-large.png")))png.Save(f);
  }catch(Exception error){Console.WriteLine(error);failed++;}finally{pet.Close();app.Shutdown();}};timer.Start();app.Run(pet);Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
