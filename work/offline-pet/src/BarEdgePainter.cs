using System;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;
// Stable coverage masks retain the measured fill boundary; no animation or random refresh noise.
static class BarEdgePainter {
 static readonly int[] Bayer={0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
 public static void Draw(Graphics g,RectangleF area,double fraction,UsageRamp ramp,TaskbarAppearance a,double dpi){
  if(a.BarEdge=="square"){ramp.Draw(g,area,fraction);return;}
  double fill=area.Width*Math.Max(0,Math.Min(1,fraction));if(fill<=0||area.Height<=0)return;
  int width=(int)Math.Ceiling(fill),height=(int)Math.Ceiling(area.Height);double radius=Math.Min(a.BarRadius*dpi,Math.Min(fill/2,area.Height/2)),soft=Math.Min(fill,a.BarSoftness*dpi);int block=Math.Max(1,(int)Math.Round(a.BarFragmentSize*dpi));
  using(var image=new Bitmap(width,height,PixelFormat.Format32bppArgb)){
   var data=image.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{var bytes=new byte[data.Stride*height];var colors=new Color[width];var paint=ramp.ForValue(fraction*100);double colorWidth=ramp.FollowValue?fill:area.Width;for(int x=0;x<width;x++)colors[x]=paint.Sample(100.0*(x+.5)/colorWidth);
    for(int y=0;y<height;y++)for(int x=0;x<width;x++){
     double px=x+.5,py=y+.5;if(px>fill||py>area.Height)continue;
     double dx=Math.Max(radius-px,Math.Max(px-(fill-radius),0)),dy=Math.Max(radius-py,Math.Max(py-(area.Height-radius),0));double coverage=radius==0?1:Math.Max(0,Math.Min(1,radius-Math.Sqrt(dx*dx+dy*dy)+.5));
     if(a.BarEdge=="feather")coverage*=Math.Max(0,Math.Min(1,Math.Min(Math.Min(px,Math.Min(py,area.Height-py))/Math.Max(1,Math.Min(soft,area.Height/2)),(fill-px)/Math.Max(1,soft))));
     if(a.BarEdge=="fragments"&&px>fill-soft){int bx=x/block,by=y/block;double threshold=((bx*17+by*29)%31+.5)/31;coverage*=threshold<(fill-px)/Math.Max(1,soft)?1:0;}
     // A color-key taskbar cannot represent alpha. Spatial dithering exposes the real taskbar instead of black halos.
     if(a.Transparent)coverage=coverage>(Bayer[(y%4)*4+x%4]+.5)/16?1:0;
     if(ramp.Mode=="mosaic"&&(int)(px/(colorWidth/ramp.Cells))!=(int)((px+Math.Min(1,colorWidth/ramp.Cells*.16))/(colorWidth/ramp.Cells)))coverage=0;
     int i=y*data.Stride+x*4;bytes[i]=colors[x].B;bytes[i+1]=colors[x].G;bytes[i+2]=colors[x].R;bytes[i+3]=(byte)Math.Round(255*coverage);
    }Marshal.Copy(bytes,0,data.Scan0,bytes.Length);
   }finally{image.UnlockBits(data);}g.DrawImageUnscaled(image,(int)Math.Round(area.X),(int)Math.Round(area.Y));
  }
 }
}
