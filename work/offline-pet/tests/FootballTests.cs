using System;
class FootballTests {
 static int failures,checks;static void Check(string name,bool ok){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static int Main(){var ball=new FootballWorld();ball.Throw(100,100,200,0,12);ball.Step(.05,0,0,600,400);Check("throw moves through real coordinates",ball.X>100&&ball.Y>100);Check("gravity accelerates falling ball",ball.VY>0);
  ball.Throw(585,100,400,0,12);for(int i=0;i<10;i++)ball.Step(.01,0,0,600,400);Check("right wall reflects velocity",ball.VX<0&&ball.X<=588);ball.Throw(15,100,-400,0,12);for(int i=0;i<10;i++)ball.Step(.01,0,0,600,400);Check("left wall reflects velocity",ball.VX>0&&ball.X>=12);
  ball.Throw(200,385,100,300,12);ball.Step(.03,0,0,600,400);Check("floor bounces upward",ball.VY<0&&ball.Y<=388);Check("floor loses energy",Math.Abs(ball.VY)<330);
  ball.Throw(200,15,0,-300,12);ball.Step(.03,0,0,600,400);Check("ceiling reflects downward",ball.VY>0&&ball.Y>=12);ball.Throw(200,300,0,100,12);for(int i=0;i<3000;i++)ball.Step(.01,0,0,600,400);Check("ball settles without microbounce",Math.Abs(ball.Y-388)<.01&&ball.VY==0);
  ball.Dragging=true;double x=ball.X,y=ball.Y;ball.Step(.1,0,0,600,400);Check("drag owns ball position",ball.X==x&&ball.Y==y);ball.Dragging=false;ball.Strike(700,-400);Check("volley launches ball upward",ball.VX==700&&ball.VY==-400);ball.Stop();ball.Step(.1,0,0,600,400);Check("hidden ball stops simulation",!ball.Active);
  ball.Throw(100,100,200,0,12);var other=new FootballWorld();other.Throw(100,100,200,0,12);for(int i=0;i<60;i++)ball.Step(1.0/60,0,0,2000,2000);for(int i=0;i<120;i++)other.Step(1.0/120,0,0,2000,2000);Check("physics stable across refresh rates",Math.Abs(ball.X-other.X)<.01&&Math.Abs(ball.Y-other.Y)<.01);
  Check("kick releases on contact frame",FootballWorld.KickFrame(.87)==5&&FootballWorld.KickFrame(.88)==6);Check("kick ends at landing frame",FootballWorld.KickFrame(2.16)==11);Check("juggle starts and ends at shoe",Math.Abs(FootballWorld.JuggleHeight(0))<.01&&Math.Abs(FootballWorld.JuggleHeight(.8))<.01);Check("juggle rises between touches",FootballWorld.JuggleHeight(.4)>30);
  ball.Throw(100,100,200,0,12);ball.Step(.1,0,0,2000,2000);Check("ball travels at sixty percent pace",Math.Abs(ball.X-112)<.01);ball.Throw(100,100,9999,-9999,12);Check("drag throw velocity capped",ball.VX<=800&&ball.VY>=-800);
  Check("flick timing matches contact",FootballWorld.ActionFrame("flick",.479)==5&&FootballWorld.ActionFrame("flick",.48)==6);Check("bicycle timing matches contact",FootballWorld.ActionFrame("bicycle",1.199)==4&&FootballWorld.ActionFrame("bicycle",1.2)==5);Check("bicycle ends after recovery",FootballWorld.ActionFrame("bicycle",2.9)==13);Check("upward flick is mostly vertical",Math.Abs(FootballWorld.ShotVY("flick"))>10*FootballWorld.ShotVX("flick"));
  Check("volley stays fast with moderate upward angle",FootballWorld.ShotVX("volley")>=700&&Math.Abs(FootballWorld.ShotVY("volley"))/FootballWorld.ShotVX("volley")>.25&&Math.Abs(FootballWorld.ShotVY("volley"))/FootballWorld.ShotVX("volley")<.4);
  Check("bicycle foot contact matches new toe",FootballWorld.ContactX("bicycle")==51&&FootballWorld.ContactY("bicycle")==55);
  Check("bicycle landing then recovery never wraps",FootballWorld.ActionFrame("bicycle",2.06)==10&&FootballWorld.ActionFrame("bicycle",2.46)==12&&FootballWorld.ActionFrame("bicycle",30)==13);
  Console.WriteLine("Checks: "+checks+" Failures: "+failures);return failures==0?0:1;
 }
}
