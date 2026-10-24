using System;
class LogicTests {
 static int failures;
 static void Check(string name, bool pass) { Console.WriteLine((pass ? "PASS " : "FAIL ") + name); if (!pass) failures++; }
 static int Main() {
  Check("gaze cardinal up", PetLogic.Gaze(0,-100)==0);
  Check("gaze cardinal right", PetLogic.Gaze(100,0)==4);
  Check("gaze cardinal down", PetLogic.Gaze(0,100)==8);
  Check("gaze cardinal left", PetLogic.Gaze(-100,0)==12);
  Check("near cursor keeps neutral", PetLogic.Gaze(2,3)==-1);
  Check("diagonal upper left", PetLogic.Gaze(-100,-100)==14);
  double x=95; int direction=1;
  PetLogic.Step(ref x,ref direction,20,0,100);
  Check("right edge reverses and stays in work area", x==100 && direction==-1);
  x=-195;direction=-1;PetLogic.Step(ref x,ref direction,20,-200,-50);
  Check("negative monitor left edge reverses",x==-200 && direction==1);
  Check("disconnected monitor saved location is clamped",PetLogic.Clamp(5000,-200,600)==600);
  Check("small screen remains valid",PetLogic.Clamp(12,20,10)==20);
  int[] counts={6,8,8,4,5,8,6,6,6};
  bool all=true;for(int row=0;row<9;row++)for(int ms=0;ms<6000;ms+=13){int f=PetLogic.Frame(row,ms);if(f<0||f>=counts[row])all=false;}
  Check("animations never select unused transparent cells",all);
  Check("idle initial dwell is preserved",PetLogic.Frame(0,279)==0 && PetLogic.Frame(0,280)==1);
  Check("jump apex timing is preserved",PetLogic.Frame(4,280)==2);
  var fit=typeof(PetLogic).GetMethod("FitScale");Check("large scale respects source pixels and display DPI",fit!=null);if(fit!=null){double value=(double)fit.Invoke(null,new object[]{5.0,1500.0,2.0,1448});Check("200 percent DPI never stretches beyond native height",Math.Abs(value-1448.0/416)<.0001);value=(double)fit.Invoke(null,new object[]{5.0,600.0,1.0,1448});Check("large mode fits a smaller work area",Math.Abs(value-490.0/208)<.0001);}
  Console.WriteLine("Failures: "+failures);return failures==0?0:1;
 }
}
