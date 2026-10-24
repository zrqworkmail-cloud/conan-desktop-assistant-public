using System;
using System.Collections.Generic;
class V4LogicTests {
 static int failures;static void Check(string label,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+label);if(!pass)failures++;}
 static int Main(){
  double x=0,y=0;bool arrived=MovementLogic.Step(ref x,ref y,100,100,10,-50,-50,200,200);Check("diagonal movement changes both axes",x>0&&y>0&&!arrived);Check("speed is independent of angle",Math.Abs(Math.Sqrt(x*x+y*y)-10)<.001);
  x=-100;y=-100;MovementLogic.Step(ref x,ref y,500,500,10000,-100,-80,40,60);Check("negative monitor bounds respected",x==40&&y==60);
  x=2;y=3;Check("arrival clamps without overshoot",MovementLogic.Step(ref x,ref y,4,5,20,0,0,100,100)&&x==4&&y==5);
  x=10;y=10;MovementLogic.Step(ref x,ref y,50,50,10,0,0,-10,-10);Check("oversized pet remains in valid origin",x==0&&y==0);
  var rate=new NetworkRate();Check("first network sample has no invented speed",!rate.Sample("a",100,200,1).HasValue);Check("byte delta divided by elapsed time",rate.Sample("a",300,600,3).Value.Download==100);Check("counter reset breaks series",!rate.Sample("a",2,3,4).HasValue);Check("NIC change resets baseline",!rate.Sample("b",20,30,5).HasValue);Check("long sleep breaks series",!rate.Sample("b",200,300,40).HasValue);
  var data=new SkillData();var e=new SkillEngine(data);var now=DateTime.UtcNow;e.StartFocus(now);e.Tick(now.AddMinutes(26));Check("completed focus is recorded by date",data.FocusDays[now.AddMinutes(26).ToLocalTime().ToString("yyyy-MM-dd")]==25);e.Tick(now.AddMinutes(26));Check("focus completion recorded only once",data.FocusDays.Count==1&&data.FocusDays[now.ToLocalTime().ToString("yyyy-MM-dd")]==25);
  return failures==0?0:1;
 }
}
