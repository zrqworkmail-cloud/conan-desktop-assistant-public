using System;
public static class ActionTiming {
 public static readonly string[] IdleActions={"wave","jump","bow","surprise","think","glasses","sleep","search","deduction","watch","bowtie","kick","skate","count","praise","alert","juggle","flick","bicycle"};
 public static double Duration(string id,int frames,double requested){return id=="kick"?frames*.16:id=="count"?Math.Max(.9,requested):Math.Max(requested,frames*.16);}
 public static double TravelSpeed(double scale,bool skate){return (skate?240:100)*scale;}
 public const int SearchFrameMilliseconds=260;
 public static int SearchFrame(double elapsed,bool repeat){int frame=(int)(Math.Max(0,elapsed)*1000/SearchFrameMilliseconds);return repeat?frame%8:Math.Min(7,frame);}
 public static double RetrieveSpeed(double scale){return 70*scale;}
 public const double WalkStride=96;
 public static double WalkPhase(double distance,double scale){return Math.Max(0,distance)/(WalkStride*scale)%1;}
 public static int WalkFrame(double distance,double scale){return (int)(WalkPhase(distance,scale)*8);}
 public static bool CanIdle(double now,double walkingUntil){return now>=walkingUntil;}
 public static int NumberPose(int number){return Math.Max(1,Math.Min(20,number));}
 public static int OneShot(int frames,double elapsed,double duration){int hold=Math.Max(1,frames/2);double lead=(hold+1)*.16,tail=(frames-1-hold)*.16;int frame=elapsed<lead?(int)(Math.Max(0,elapsed)/.16):elapsed<duration-tail?hold:hold+1+(int)((elapsed-duration+tail)/.16);return Math.Max(0,Math.Min(frames-1,frame));}
 public static int MovingSkate(double elapsed){return elapsed<.48?Math.Max(0,(int)(elapsed/.16)):3+(int)((elapsed-.48)/.4)%3;}
}
