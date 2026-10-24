using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Web.Script.Serialization;
class AdvancedTests {
 static int failed;static void Check(string name,bool ok){Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failed++;}
 static object Get(object o,string n){return o.GetType().GetField(n).GetValue(o);}static void Set(object o,string n,object v){o.GetType().GetField(n).SetValue(o,v);}
 static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n).Invoke(o,a);}
 static bool Reject(Action a){try{a();return false;}catch(TargetInvocationException e){return e.InnerException is ArgumentException;}}
 static int Main(){
  var assembly=typeof(SkillEngine).Assembly;var type=assembly.GetType("AdvancedEngine");Check("advanced skills engine is available",type!=null);if(type==null)return 1;
  var data=Activator.CreateInstance(assembly.GetType("AdvancedData"));var engine=Activator.CreateInstance(type,new[]{data});var now=new DateTime(2026,10,14,1,0,0,DateTimeKind.Utc);
  var task=Call(engine,"AddTask","写报告",now.AddMinutes(1),2);var tasks=(IList)Get(data,"Tasks");Check("new task retains its deadline and priority",tasks.Count==1&&(int)Get(task,"Priority")==2);
  Check("blank tasks are rejected",Reject(()=>Call(engine,"AddTask"," ",null,0)));
  Check("invalid task priority is rejected",Reject(()=>Call(engine,"AddTask","坏优先级",null,9)));
  Check("task does not alert before deadline",((IList)Call(engine,"Tick",now)).Count==0);
  Check("due task creates an actionable alert",((IList)Call(engine,"Tick",now.AddMinutes(2))).Count==1);
  Check("overdue task does not alert every tick",((IList)Call(engine,"Tick",now.AddMinutes(3))).Count==0);
  Call(engine,"ToggleTask",Get(task,"Id"));Check("task can be marked complete",(bool)Get(task,"Done"));
  Call(engine,"ToggleTask",Get(task,"Id"));Check("task can be reopened",!(bool)Get(task,"Done"));
  var json=new JavaScriptSerializer();var restored=json.Deserialize(json.Serialize(data),data.GetType());var engine2=Activator.CreateInstance(type,new[]{restored});Check("task alert state survives persistence",((IList)Call(engine2,"Tick",now.AddMinutes(4))).Count==0);
  Call(engine,"DeleteTask",Get(task,"Id"));Check("task deletion removes matching item",tasks.Count==0);
  var history=Activator.CreateInstance(assembly.GetType("ClipboardHistory"));Call(history,"Capture","private");Check("clipboard capture is disabled by default",((IList)Get(history,"Items")).Count==0);
  Call(history,"SetEnabled",true);Call(history,"Capture","one");Call(history,"Capture","one");Check("clipboard ignores consecutive duplicates",((IList)Get(history,"Items")).Count==1);
  for(int i=0;i<35;i++)Call(history,"Capture","item"+i);Check("clipboard history is bounded",((IList)Get(history,"Items")).Count==30);
  Call(history,"Capture",new string('x',10001));Check("oversized clipboard is skipped",((IList)Get(history,"Items")).Count==30);
  Call(history,"SetEnabled",false);Check("disabling history clears its contents",((IList)Get(history,"Items")).Count==0);
  Check("history contents never enter persisted settings",!json.Serialize(data).Contains("item34"));
  Check("new feature settings are opt in",!(bool)Get(data,"ClipboardEnabled")&&!(bool)Get(data,"SpeakNotifications")&&!(bool)Get(data,"Hotkeys"));
  var skill=new SkillEngine(new SkillData());var timer=skill.AddCountdown("循环",1,now);var repeat=typeof(SkillEngine).GetMethod("SetRepeat");Check("repeat scheduling is available",repeat!=null);
  if(repeat!=null){repeat.Invoke(skill,new object[]{timer.Id,60});skill.Tick(now.AddHours(5));Check("missed repeats emit only once and reschedule",timer.Enabled&&timer.DueUtc==now.AddHours(6));repeat.Invoke(skill,new object[]{timer.Id,0});skill.Tick(now.AddHours(6));Check("repeat can return to a one shot reminder",!timer.Enabled);}
  Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
