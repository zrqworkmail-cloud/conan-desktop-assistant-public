using System;
public static class MovementLogic {
 public static bool Approach(ref double x,ref double y,double targetX,double targetY,double distance,double minX,double minY,double maxX,double maxY){targetX=Math.Max(minX,Math.Min(targetX,Math.Max(minX,maxX)));targetY=Math.Max(minY,Math.Min(targetY,Math.Max(minY,maxY)));double dx=targetX-x,dy=targetY-y,length=Math.Sqrt(dx*dx+dy*dy),step=Math.Max(0,distance);if(length<=step){x=targetX;y=targetY;return true;}if(length>0){x+=dx/length*step;y+=dy/length*step;}return false;}
 public static bool Step(ref double x,ref double y,double targetX,double targetY,double distance,double minX,double minY,double maxX,double maxY){
  maxX=Math.Max(minX,maxX);maxY=Math.Max(minY,maxY);x=Math.Max(minX,Math.Min(x,maxX));y=Math.Max(minY,Math.Min(y,maxY));targetX=Math.Max(minX,Math.Min(targetX,maxX));targetY=Math.Max(minY,Math.Min(targetY,maxY));double dx=targetX-x,dy=targetY-y,length=Math.Sqrt(dx*dx+dy*dy);
  if(length<=Math.Max(0,distance)){x=targetX;y=targetY;return true;}if(length>0){x+=dx/length*Math.Max(0,distance);y+=dy/length*Math.Max(0,distance);}return false;
 }
}
public struct NetworkSpeed {public double Download,Upload;}
public class NetworkRate {
 string previousId;long received,sent;double time;
 public NetworkSpeed? Sample(string id,long rx,long tx,double seconds){
  bool valid=previousId==id&&seconds>time&&seconds-time<=15&&rx>=received&&tx>=sent;NetworkSpeed? result=valid?(NetworkSpeed?)new NetworkSpeed{Download=(rx-received)/(seconds-time),Upload=(tx-sent)/(seconds-time)}:null;
  previousId=id;received=rx;sent=tx;time=seconds;return result;
 }
}
