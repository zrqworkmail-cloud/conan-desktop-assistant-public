using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
class WorkEndTests {
 static int failures,checks;static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
 static SkillEngine Engine(bool enabled=true,bool weekdays=true){return new SkillEngine(Json.Deserialize<SkillData>("{\"EndOfWork\":{\"Enabled\":"+enabled.ToString().ToLower()+",\"CalendarMode\":"+(weekdays?1:2)+",\"MinuteOfDay\":1080}}"));}
 static DateTime At(int day,int hour,int minute,int second=0){return new DateTime(2026,10,day,hour,minute,second,DateTimeKind.Local).ToUniversalTime();}
 static void Check(string name,bool ok){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static int Main(string[] args){
  var e=Engine();foreach(int m in new[]{15,10,8,6,5,4,3,2,1}){var alerts=e.Tick(At(19,17,60-m));Check("exact countdown "+m,alerts.Count==1&&alerts[0].Key.StartsWith("work-end:")&&alerts[0].Text.Contains(m+" 分钟"));Check("same minute never repeats "+m,e.Tick(At(19,17,60-m,20)).Count==0);e=new SkillEngine(Json.Deserialize<SkillData>(Json.Serialize(e.Data)));Check("restart preserves dedup "+m,e.Tick(At(19,17,60-m,40)).Count==0);}
  Check("disabled schedule stays silent",Engine(false).Tick(At(19,17,45)).Count==0);Check("weekend is excluded",Engine().Tick(At(24,17,45)).Count==0);Check("all days can include weekend",Engine(true,false).Tick(At(24,17,45)).Count==1);Check("no extra thresholds",Engine().Tick(At(19,17,53)).Count==0);Check("after work no stale announcement",Engine().Tick(At(19,18,1)).Count==0);
  e=Engine();e.Tick(At(19,17,44));var resumed=e.Tick(At(19,17,56));Check("resume skips missed thresholds",resumed.Count==1&&resumed[0].Text.Contains("4 分钟"));Check("next workday resets dedup",e.Tick(At(20,17,56)).Count==1);
  e=Engine();e.Data.EndOfWork.CalendarMode=0;Check("unknown calendar never assumes workday",e.WorkDay(new DateTime(2027,1,4))==null&&e.Tick(At(19,17,45)).Count==0);var calendar=SkillRuntime.ParseCalendar(File.ReadAllText(args[0]),2026);e.Calendars[2026]=calendar;Check("makeup Saturday is a workday",e.WorkDay(new DateTime(2026,10,10))==true);Check("holiday weekday is off",e.WorkDay(new DateTime(2026,10,1))==false);Check("ordinary weekday is work",e.WorkDay(new DateTime(2026,10,19))==true);Check("ordinary weekend is off",e.WorkDay(new DateTime(2026,10,24))==false);Check("makeup day triggers countdown",e.Tick(new DateTime(2026,10,10,17,45,0,DateTimeKind.Local).ToUniversalTime()).Count==1);Check("holiday suppresses countdown",e.Tick(new DateTime(2026,10,1,17,45,0,DateTimeKind.Local).ToUniversalTime()).Count==0);try{SkillRuntime.ParseCalendar(File.ReadAllText(args[0]),2027);Check("mismatched year rejected",false);}catch(ArgumentException){Check("mismatched year rejected",true);}e.SetWorkEnd(true,19*60,0);Check("changed end time recomputes",e.Tick(new DateTime(2026,10,19,18,45,0,DateTimeKind.Local).ToUniversalTime()).Count==1);
  e=Engine();e.Tick(At(19,17,45));e.SetWorkEnds(true,1,new[]{new WorkEndSlot{Label="中午",Enabled=true,MinuteOfDay=720},new WorkEndSlot{Label="下午",Enabled=true,MinuteOfDay=1080},new WorkEndSlot{Label="晚上",Enabled=true,MinuteOfDay=1260}});
  Check("migration preserves old countdown dedup",e.Tick(At(19,17,45,20)).Count==0);
  Check("noon countdown has correct label",e.Tick(At(19,11,45)).Single().Text.Contains("中午（12:00）"));Check("evening countdown independent",e.Tick(At(19,20,50)).Single().Count==10);
  e=new SkillEngine(Json.Deserialize<SkillData>(Json.Serialize(e.Data)));Check("multiple slots persist after restart",e.WorkEndSlots().Length==3);Check("each slot dedup survives restart",e.Tick(At(19,11,45,30)).Count==0&&e.Tick(At(19,20,50,30)).Count==0);
  Check("status selects nearest upcoming shift",e.WorkEndStatus(At(19,13,0)).Contains("下午"));Check("status after last shift",e.WorkEndStatus(At(19,22,0)).Contains("已到"));
  var noon=e.Data.EndOfWork.Slots[0];noon.Enabled=false;Check("disabled slot does not announce",e.Tick(At(20,11,45)).Count==0);Check("other slot still enabled",e.Tick(At(20,17,45)).Count==1);
  e.Data.EndOfWork.CalendarMode=0;e.Calendars[2026]=calendar;Check("all slots skip holiday",e.Tick(At(1,11,45)).Count==0&&e.Tick(At(1,17,45)).Count==0&&e.Tick(At(1,20,45)).Count==0);Check("multiple slots obey makeup day",e.Tick(At(10,17,45)).Count==1&&e.Tick(At(10,20,45)).Count==1);
  try{e.SetWorkEnds(true,1,new[]{new WorkEndSlot{Label="重复一",Enabled=true,MinuteOfDay=720},new WorkEndSlot{Label="重复二",Enabled=true,MinuteOfDay=720}});Check("duplicate enabled times rejected",false);}catch(ArgumentException){Check("duplicate enabled times rejected",true);}Check("invalid update leaves settings intact",e.WorkEndSlots().Length==3);
  e.SetWorkEnds(false,1,e.WorkEndSlots());Check("master switch silences all shifts",e.Tick(At(21,17,45)).Count==0);e.SetWorkEnds(true,1,new WorkEndSlot[0]);Check("empty schedule is silent",e.Tick(At(21,17,45)).Count==0);
  Console.WriteLine("Checks: "+checks+" Failures: "+failures);return failures==0?0:1;
 }
}
