using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
class FootballWindow:Window {
 readonly FootballWorld ball;readonly SpriteView image=new SpriteView();readonly Stopwatch clock=Stopwatch.StartNew();readonly BitmapSource[] spins=new BitmapSource[4];Point previous;double previousTime;Vector velocity;
 public Func<bool> AllowDrag,ThrowOnRelease;public event Action Released,Dismissed;
 public FootballWindow(FootballWorld world){ball=world;Title="柯南 · 足球";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;ShowActivated=false;Topmost=true;Content=image;
  string[] pixels={".....oooooo.....","...oowwwwwwoo...","..owww####wwwo..",".owww######wwwo.",".owwww####wwwwwo","ow#wwwwwwwwww#wo","o###wwwwwwww###o","o###www##www###o","o##www####www##o","owwww######wwwwo","owwwww####wwwwwo",".owwwwwwwwwwwwo.",".o##wwwwwwww##o.","..o###wwww###o..","...oo######oo...",".....oooooo....."};var bytes=new byte[16*16*4];for(int y=0;y<16;y++)for(int x=0;x<16;x++){char c=x<pixels[y].Length?pixels[y][x]:'.';int index=(y*16+x)*4;byte shade=c=='#'?(byte)29:c=='o'?(byte)155:(byte)245;if(c!='.'){bytes[index]=bytes[index+1]=bytes[index+2]=shade;bytes[index+3]=255;}}spins[0]=BitmapSource.Create(16,16,96,96,PixelFormats.Bgra32,null,bytes,64);spins[0].Freeze();for(int i=1;i<4;i++){spins[i]=new TransformedBitmap(spins[0],new RotateTransform(i*90));spins[i].Freeze();}
  image.MouseLeftButtonDown+=(s,e)=>{if(AllowDrag!=null&&!AllowDrag())return;ball.Dragging=true;velocity=new Vector();previous=CursorPoint();previousTime=clock.Elapsed.TotalSeconds;image.CaptureMouse();e.Handled=true;};
  image.MouseMove+=(s,e)=>{if(!ball.Dragging)return;var point=CursorPoint();double now=clock.Elapsed.TotalSeconds,dt=now-previousTime;Vector delta=point-previous;ball.X+=delta.X;ball.Y+=delta.Y;if(dt>.001)velocity=delta/dt;previous=point;previousTime=now;RenderBall();e.Handled=true;};
  image.MouseLeftButtonUp+=(s,e)=>{if(!ball.Dragging)return;ball.Dragging=false;image.ReleaseMouseCapture();if(clock.Elapsed.TotalSeconds-previousTime>.12)velocity=new Vector(0,-300);if(velocity.Length<80)velocity=new Vector(180,-350);bool toss=ThrowOnRelease==null||ThrowOnRelease();ball.Throw(ball.X,ball.Y,toss?velocity.X:0,toss?velocity.Y:0,ball.Radius);if(Released!=null)Released();e.Handled=true;};
  image.LostMouseCapture+=(s,e)=>{if(ball.Dragging){ball.Dragging=false;ball.VX=ball.VY=0;if(Released!=null)Released();}};
  image.MouseRightButtonUp+=(s,e)=>{if(Dismissed!=null)Dismissed();e.Handled=true;};
 }
 Point CursorPoint(){var p=System.Windows.Forms.Cursor.Position;var source=PresentationSource.FromVisual(this);return source==null?new Point(p.X,p.Y):source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X,p.Y));}
 public void RenderBall(){Width=Height=ball.Radius*2;Left=ball.X-ball.Radius;Top=ball.Y-ball.Radius;image.Width=Width;image.Height=Height;image.Display(spins[((int)(Math.Abs(ball.Angle)/90))%4]);}
}
