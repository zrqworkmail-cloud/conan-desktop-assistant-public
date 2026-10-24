using System;
using System.IO;
class AssistantTests {
 static int failures;
 static void Check(string name,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+name);if(!pass)failures++;}
 static int Main(){
  Check("CPU is normalized across logical processors",AssistantLogic.Cpu(2,1,4)==50);
  Check("CPU sampling never yields invalid percentage",AssistantLogic.Cpu(-2,0,0)==0);
  Check("hook maps permission to waiting",AssistantLogic.State("PermissionRequest")=="waiting");
  Check("stop means turn ended, not assumed successful",AssistantLogic.State("Stop")=="ended");
  Check("unrecognized hook is ignored",AssistantLogic.State("Unexpected")==null);
  var feed=new CodexFeed();DateTime t=DateTime.UtcNow;
  feed.Apply("a","working",t);feed.Apply("a","ended",t.AddSeconds(2));feed.Apply("a","working",t.AddSeconds(1));
  Check("out of order events cannot reopen ended turn",feed.State("a",t.AddSeconds(3))=="ended");
  feed.Apply("b","waiting",t.AddSeconds(3));
  Check("waiting session outranks finished session",feed.Animation(t.AddSeconds(4))==6);
  Check("stale tasks are shown as unknown",feed.State("b",t.AddMinutes(16))=="stale");
  Check("stale tasks do not animate as active",feed.Animation(t.AddMinutes(16))==-1);
  string temp=Path.GetTempFileName();
  Check("existing local file is valid shortcut",AssistantLogic.LocalTarget(temp));
  Check("URLs cannot become automatic launch targets",!AssistantLogic.LocalTarget("https://example.com"));
  Check("missing shortcut is rejected",!AssistantLogic.LocalTarget(temp+"missing"));
  File.Delete(temp);
  var notices=new NoticeFeed();
  Check("first notification is accepted",notices.Add("process:42","退出",t));
  Check("repeated notification is deduplicated",!notices.Add("process:42","退出",t.AddSeconds(2)));
  for(int i=0;i<130;i++)notices.Add("unique:"+i,"通知",t.AddSeconds(i));
  Check("notification history stays bounded",notices.Items.Count==100);
  Console.WriteLine("Failures: "+failures);return failures==0?0:1;
 }
}
