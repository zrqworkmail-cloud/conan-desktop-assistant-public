using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
class BubbleWindowTests {
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int index);
 static int failed;static void Check(string name,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+name);if(!pass)failed++;}
 [STAThread]static int Main(string[] args){
  var app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;var bubble=new SpeechBubble();var foreground=GetForegroundWindow();
  Rect area=SystemParameters.WorkArea;
  var m=new BubbleMessage{Key="codex:preview",Text="需要你确认下一步操作。\n气泡支持中文换行、长文本和表情 😀；鼠标移上去可以暂停计时，点击查看详情可以回到通知记录。",Notification=true,Row=6};
  bubble.Render(m,2,area.Left+area.Width/2,area.Top+320,area.Top+530,area);
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(600)};
  timer.Tick+=(s,e)=>{timer.Stop();
   try{
    Check("bubble does not steal foreground focus",GetForegroundWindow()==foreground);
    var handle=new WindowInteropHelper(bubble).Handle;Check("bubble uses no-activate window style",(GetWindowLong(handle,-20)&0x08000000)!=0);
    Check("long text grows vertically",bubble.ActualHeight>120&&bubble.ActualHeight<260);
    Directory.CreateDirectory(args[0]);Save(bubble,Path.Combine(args[0],"bubble-notification.png"));
    bubble.Render(new BubbleMessage{Key="casual",Text="真相，往往藏在被忽略的细节里。",Row=0},0,area.Left+area.Width/2,area.Top+320,area.Top+530,area);Save(bubble,Path.Combine(args[0],"bubble-casual.png"));
    bubble.Render(m,0,area.Left+12,area.Top+15,area.Top+245,area);
    Check("top-left bubble stays in work area",bubble.Left>=area.Left&&bubble.Top>=area.Top&&bubble.Left+bubble.Width<=area.Right&&bubble.Top+bubble.Height<=area.Bottom);Save(bubble,Path.Combine(args[0],"bubble-edge.png"));
    bool dismissed=false,details=false;bubble.Dismissed+=()=>dismissed=true;bubble.Details+=()=>details=true;
    Click(bubble,"关闭气泡");Click(bubble,"查看详情");Check("close button raises dismiss action",dismissed);Check("details button raises navigation action",details);
    bubble.Render(null,0,0,0,0,area);Check("expired bubble hides without closing pet",!bubble.IsVisible);
   }catch(Exception ex){Console.WriteLine(ex.Message);failed++;}
   finally{bubble.Close();app.Shutdown();}
  };timer.Start();app.Run();Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
 static void Save(Window window,string path){window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.Width),(int)Math.Ceiling(window.Height),96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(path))encoder.Save(file);}
 static void Click(DependencyObject root,string name){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);var b=child as Button;if(b!=null&&(Convert.ToString(b.Content)==name||System.Windows.Automation.AutomationProperties.GetName(b)==name)){b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return;}Click(child,name);}}
}
