using System;
using System.Linq;
class SkillsTests {
 static int failed;static void Check(string name,bool value){Console.WriteLine((value?"PASS ":"FAIL ")+name);if(!value)failed++;}
 static void Reject(string name,Action run){try{run();Check(name,false);}catch(ArgumentException){Check(name,true);}}
 static int Main(){
  DateTime now=new DateTime(2026,10,13,4,0,0,DateTimeKind.Utc);var e=new SkillEngine(new SkillData());
  e.StartFocus(now);Check("work deadline uses configured duration",e.Data.Focus.EndUtc==now.AddMinutes(25));
  e.PauseFocus(now.AddMinutes(10));Check("pause keeps remaining time",e.Data.Focus.Paused&&Math.Abs(e.Data.Focus.RemainingSeconds-900)<1);
  e.ResumeFocus(now.AddMinutes(30));Check("resume uses remaining duration",e.Data.Focus.EndUtc==now.AddMinutes(45));
  var a=e.Tick(now.AddMinutes(45));Check("work completion starts short break",a.Count==1&&e.Data.Focus.Mode=="short"&&e.Data.Focus.Running);
  Check("completed work is counted",e.Data.Focus.Completed==1&&e.Data.Focus.TotalMinutes==25);
  a=e.Tick(now.AddMinutes(50));Check("break ends ready for next work",a.Count==1&&!e.Data.Focus.Running&&e.Data.Focus.Mode=="work");
  e.Data.Focus.Completed=3;e.StartFocus(now.AddHours(1));e.Tick(now.AddHours(1).AddMinutes(25));Check("fourth session gets long break",e.Data.Focus.Mode=="long");
  e.StopFocus();Check("stop clears running and paused state",!e.Data.Focus.Running&&!e.Data.Focus.Paused);
  Reject("invalid countdown duration rejected",()=>e.AddCountdown("bad",0,now));
  Reject("past reminder rejected",()=>e.AddReminder("bad",now.AddSeconds(-1),now));
  var r=e.AddCountdown("测试倒计时",1,now);a=e.Tick(now.AddMinutes(1));Check("countdown fires once",a.Any(x=>x.Key=="timer:"+r.Id)&&!r.Enabled);
  Check("finished countdown never floods",e.Tick(now.AddMinutes(2)).Count==0);
  e.Snooze(r.Id,now.AddMinutes(2));Check("snooze schedules another five minutes",r.Enabled&&r.DueUtc==now.AddMinutes(7));
  e.SetWellness("water",true,30,now);a=e.Tick(now.AddHours(10));Check("resume emits only one missed wellness alert",a.Count(x=>x.Key=="wellness:water")==1);
  Check("wellness next time starts from resume",e.Data.Reminders.Single(x=>x.Kind=="water").DueUtc==now.AddHours(10).AddMinutes(30));
  e.SetWellness("water",false,30,now);Check("wellness can be disabled",!e.Data.Reminders.Single(x=>x.Kind=="water").Enabled);
  Check("HTTPS is accepted for explicit shortcuts",SkillLogic.WebTarget("https://example.com"));Check("unsafe URL scheme rejected",!SkillLogic.WebTarget("javascript:alert(1)"));
  Check("line cleanup removes empty whitespace lines",SkillLogic.CleanLines(" a \r\n  \r\nb ",false)=="a"+Environment.NewLine+"b");
  Check("line dedupe preserves first occurrence",SkillLogic.CleanLines("a\nb\na",true)=="a"+Environment.NewLine+"b");
  Check("JSON formatting preserves content",SkillLogic.PrettyJson("{\"x\":[1,\"a b\"]}").Contains("  \"x\""));
  string id="01a12ec9-fe00-7682-82ec-d1a870250a48";
  Check("Codex routes to an existing thread URL",NotificationRoute.Resolve("codex:"+id).Value=="codex://threads/"+id);
  Check("invalid thread ID falls back to Codex panel",NotificationRoute.Resolve("codex:fake-id").Value==null);
  Check("process notification routes by PID",NotificationRoute.Resolve("process:123").Kind=="process");
  Check("wellness notification routes to its page",NotificationRoute.Resolve("wellness:water").Kind=="wellness");
  var n=e.SaveNote(null,"笔记","正文");e.SaveNote(n.Id,"修改","新正文");Check("note updates instead of duplicating",e.Data.Notes.Count==1&&n.Text=="新正文");
  e.DeleteNote(n.Id);Check("note deletion is explicit",e.Data.Notes.Count==0);
  Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
