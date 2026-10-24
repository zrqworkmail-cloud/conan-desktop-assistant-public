using System;
public class FootballWorld {
 public const double MotionRate=.6;
 public bool Active,Dragging,HorizontalFlight;double flightY;public double X,Y,VX,VY,Angle,Radius=12;
 public void Throw(double x,double y,double vx,double vy,double radius){Radius=Math.Max(4,radius);X=x;Y=y;VX=Math.Max(-800*Radius/12,Math.Min(800*Radius/12,vx));VY=Math.Max(-800*Radius/12,Math.Min(800*Radius/12,vy));Active=true;Dragging=false;HorizontalFlight=false;}
 public void Strike(double vx,double vy,bool horizontal=false){VX=vx;VY=horizontal?0:vy;HorizontalFlight=horizontal;flightY=Y;Active=true;Dragging=false;}
 public void Stop(){Active=false;Dragging=false;VX=VY=0;}
 public void Step(double seconds,double left,double top,double right,double bottom){
  if(!Active||Dragging||seconds<=0)return;double remaining=Math.Min(.25,seconds)*MotionRate,scale=Radius/12;
  if(right-left<2*Radius||bottom-top<2*Radius)return;
  while(remaining>0){double dt=Math.Min(remaining,1.0/120);remaining-=dt;bool resting=Y>=bottom-Radius-.01&&Math.Abs(VY)<1;
   X+=VX*dt;if(HorizontalFlight){flightY=Math.Max(top+Radius,Math.Min(bottom-Radius,flightY));Y=flightY;VY=0;}else if(!resting){Y+=VY*dt+400*scale*dt*dt;VY+=800*scale*dt;}else{Y=bottom-Radius;VY=0;VX*=Math.Exp(-2.8*dt);if(Math.Abs(VX)<2*scale)VX=0;}
   if(X<left+Radius){X=left+Radius;VX=Math.Abs(VX)*.8;}if(X>right-Radius){X=right-Radius;VX=-Math.Abs(VX)*.8;}
   if(!HorizontalFlight&&Y<top+Radius){Y=top+Radius;VY=Math.Abs(VY)*.75;}if(!HorizontalFlight&&Y>bottom-Radius){Y=bottom-Radius;VY=-Math.Abs(VY)*.64;VX*=.85;if(Math.Abs(VY)<24*scale)VY=0;}
   Angle+=VX*dt/Radius*180/Math.PI;
  }
 }
 static readonly double[] FlickTimes={0,.12,.20,.28,.36,.42,.48,.64,.80,.96,1.12,1.28},VolleyTimes={0,.12,.25,.36,.52,.68,.88,1.04,1.24,1.45,1.70,1.92},BicycleTimes={0,.18,.38,.64,.90,1.20,1.40,1.60,1.76,1.92,2.06,2.26,2.46,2.70};
 public static double ContactTime(string action){return action=="flick"?.48:action=="bicycle"?1.2:.88;}
 public static double ActionDuration(string action){return action=="flick"?1.44:action=="bicycle"?2.9:2.16;}
 public static int ActionFrame(string action,double elapsed){var times=action=="flick"?FlickTimes:action=="bicycle"?BicycleTimes:VolleyTimes;int frame=0;while(frame<times.Length-1&&elapsed+.0000001>=times[frame+1])frame++;return frame;}
 public static int KickFrame(double elapsed){return ActionFrame("volley",elapsed);}
 public static double ContactX(string action){return action=="flick"?145:action=="bicycle"?51:154;}
 public static double ContactY(string action){return action=="flick"?145:action=="bicycle"?55:116;}
 public static double ShotVX(string action){return action=="flick"?20:action=="bicycle"?380:780;}
 public static double ShotVY(string action){return action=="flick"?-600:action=="bicycle"?-300:-240;}
 public static double JuggleHeight(double elapsed){double t=((elapsed%.8)+.8)%.8;return 200*t*(.8-t);}
}
