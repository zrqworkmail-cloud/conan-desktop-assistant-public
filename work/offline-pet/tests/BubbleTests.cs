using System;
class BubbleTests {
 static int failed;static void Check(string name,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+name);if(!pass)failed++;}
 static BubbleMessage Message(string key,string text,bool notice){return new BubbleMessage{Key=key,Text=text,Notification=notice,Row=3};}
 static int Main(){
  Check("long messages get more reading time",BubbleLogic.Duration(new string('字',80),true)>BubbleLogic.Duration("完成",true));
  Check("notification time is bounded",BubbleLogic.Duration(new string('字',1000),true)<=20);
  Check("preview preserves surrogate pairs",!Char.IsHighSurrogate(BubbleLogic.Preview(new string('字',139)+"😀后续")[139]));
  var p=BubbleLogic.Place(990,300,500,320,120,0,0,1000,800);Check("right edge stays visible",p.X>=0&&p.X+320<=1000);
  p=BubbleLogic.Place(10,20,230,320,120,0,0,1000,800);Check("top edge flips bubble below pet",!p.Above&&p.Y>=238);
  p=BubbleLogic.Place(-1900,300,500,320,120,-1920,0,0,1080);Check("negative monitor coordinates remain valid",p.X>=-1920&&p.X+320<=0);
  p=BubbleLogic.Place(500,700,790,320,120,0,0,1000,800);Check("bottom edge prefers bubble above",p.Above&&p.Y+120<=692);
  var q=new BubbleQueue();q.Add(Message("casual","你好",false),0);q.Add(Message("codex:a:PermissionRequest","等待确认",true),1);Check("notification replaces casual speech",q.Current.Notification);
  q.Add(Message("casual","随便说说",false),2);Check("casual speech cannot overwrite notification",q.Current.Text=="等待确认");
  q.Add(Message("codex:b:Stop","第二个会话",true),2);Check("different notifications queue",q.Pending==1&&q.Current.Text=="等待确认");
  q.Add(Message("codex:a:Stop","回合结束",true),3);Check("latest same-session event supersedes stale state",q.Current.Text=="回合结束"&&q.Pending==1);
  q.Add(Message("codex:b:PermissionRequest","第二个会话等待",true),4);Check("queued session is updated in place",q.Pending==1);
  q.Dismiss(5);Check("dismiss advances to next notification",q.Current.Text=="第二个会话等待");
  double until=q.Until;q.Tick(6,false);q.Tick(8,true);Check("hover pauses expiry",q.Until==until+2);
  q.Tick(q.Until+1,false);Check("messages eventually expire",q.Current==null);
  for(int i=0;i<20;i++)q.Add(Message("process:"+i,"进程通知",true),30);Check("pending notifications are bounded",q.Pending==8);
  q.Clear();Check("hide clears transient bubbles",q.Current==null&&q.Pending==0);
  Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
