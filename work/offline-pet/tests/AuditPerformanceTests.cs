using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Web.Script.Serialization;
class AuditPerformanceTests {
 [STAThread]static int Main(string[] args){var c=new MonitorConfig{TaskbarItems=TaskbarAppearance.Keys.ToList()};c.Appearance.AutoFit=false;c.Appearance.FontName="Microsoft Yahei UI";var s=new SystemSample{Cpu=66,MemoryTotal=32,MemoryUsed=20,Gpu=45,CpuTemperature=65,GpuTemperature=50,Download=120*1048576,Upload=40*1048576};var layout=TaskbarPainter.Geometry(c,1,100,s);double[] times=new double[2];using(var b=new Bitmap(layout.Size.Width,layout.Size.Height))using(var g=Graphics.FromImage(b)){for(int mode=0;mode<2;mode++){c.Appearance.GradientText=mode==1;for(int i=0;i<5;i++)TaskbarPainter.Draw(g,c,s,1,layout);var watch=Stopwatch.StartNew();for(int i=0;i<60;i++)TaskbarPainter.Draw(g,c,s,1,layout);watch.Stop();times[mode]=watch.Elapsed.TotalMilliseconds/60;}}Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"render-cost.json"),new JavaScriptSerializer().Serialize(new {solid_ms_per_frame=times[0],gradient_ms_per_frame=times[1],ratio=times[1]/Math.Max(.001,times[0]),frames=60,items=10,dpi=1,note="One local warm run; not a cross-machine benchmark"}));Console.WriteLine("Solid="+times[0].ToString("F3")+"ms; gradient="+times[1].ToString("F3")+"ms/frame");return 0;}
}
