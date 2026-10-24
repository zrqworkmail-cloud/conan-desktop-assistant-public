using System;
public static class PetLogic {
 public static readonly int[][] Durations={
  new[]{280,110,110,140,140,320},new[]{80,80,80,80,80,80,80,80},new[]{80,80,80,80,80,80,80,80},
  new[]{140,140,140,280},new[]{140,140,140,140,280},new[]{140,140,140,140,140,140,140,240},
  new[]{150,150,150,150,150,260},new[]{120,120,120,120,120,220},new[]{150,150,150,150,150,280}};
 public static double FitScale(double requested,double workHeight,double dpi,int nativeHeight){if(Double.IsNaN(requested)||Double.IsInfinity(requested))requested=1.15;double screen=Math.Max(.75,(workHeight-110)/208),pixels=nativeHeight>0?nativeHeight/(208*Math.Max(.1,dpi)):Double.MaxValue;return Math.Max(.75,Math.Min(requested,Math.Min(screen,pixels)));}
 public static double FitPixelScale(double requested,double workHeight,double dpi){double value=FitScale(requested,workHeight,dpi,0);return value*dpi>=2?Math.Floor(value*dpi+0.000001)/dpi:value;}
 public static int Gaze(double dx,double dy) {
  if(dx*dx+dy*dy<24*24)return -1;
  double angle=Math.Atan2(dx,-dy);if(angle<0)angle+=Math.PI*2;
  return (int)Math.Floor(angle/(Math.PI/8)+0.5)%16;
 }
 public static double Clamp(double value,double min,double max){return Math.Max(min,Math.Min(value,Math.Max(min,max)));}
 public static void Step(ref double x,ref int direction,double distance,double min,double max){
  x+=direction*distance;
  if(x>=max){x=Math.Max(min,max);direction=-1;}
  if(x<=min){x=min;direction=1;}
 }
 public static int Frame(int row,int elapsed){
  int total=0;foreach(int ms in Durations[row])total+=ms;
  int t=((elapsed%total)+total)%total;
  for(int i=0;i<Durations[row].Length;i++){if(t<Durations[row][i])return i;t-=Durations[row][i];}
  return 0;
 }
 public static int Duration(int row){int total=0;foreach(int ms in Durations[row])total+=ms;return total;}
}
