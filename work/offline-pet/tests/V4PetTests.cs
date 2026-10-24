using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
class V4PetTests {
 static int failures;static void Check(string n,bool v){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)failures++;}
 static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
 static void Invoke(object o,string n,params object[] args){o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
 [STAThread]static int Main(){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);int stage=0;double x=0,y=0;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(650)};timer.Tick+=(s,e)=>{try{
  if(stage++==0){((BubbleQueue)Field(pet,"messages")).Clear();Set(pet,"awakeUntil",Double.MaxValue);Set(pet,"lines",false);Set(pet,"gaze",false);Invoke(pet,"SetMovement","roam");x=pet.Left;y=pet.Top;Set(pet,"moveX",x);Set(pet,"moveY",y);Set(pet,"targetX",x+120);Set(pet,"targetY",y-120);Set(pet,"walkUntil",((Stopwatch)Field(pet,"clock")).Elapsed.TotalSeconds+20);Set(pet,"nextWalk",Double.MaxValue);return;}
  if(stage==2){Check("real pet moves horizontally",Math.Abs(pet.Left-x)>2);Check("real pet moves vertically",Math.Abs(pet.Top-y)>2);x=pet.Left;y=pet.Top;Set(pet,"menuOpen",true);return;}
  if(stage==3){Check("menu interaction pauses both coordinates",pet.Left==x&&pet.Top==y);Set(pet,"menuOpen",false);Invoke(pet,"SetMovement","rest");x=pet.Left;y=pet.Top;return;}
  Check("rest mode stops autonomous movement",pet.Left==x&&pet.Top==y);Invoke(pet,"OpenTerminal");Check("pet opens native v4 workbench",Field(pet,"terminal") is TerminalWindow);Check("original atlas frame dimensions preserved",((SpriteView)Field(pet,"sprite")).Frame.PixelWidth==192);timer.Stop();pet.Close();app.Shutdown();
 }catch(Exception error){Console.WriteLine(error);failures++;timer.Stop();pet.Close();app.Shutdown();}};timer.Start();app.Run(pet);return failures==0?0:1;}
}
