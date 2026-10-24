using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
class ProjectAuditTests {
 static readonly Dictionary<string,object> results=new Dictionary<string,object>();static object Get(object o,string name){return o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}static void Call(object o,string name,params object[] args){o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);}
 static void Record(string name,bool observed){results[name]=observed;Console.WriteLine((observed?"REPRO ":"NOT_REPRO ")+name);}
 [STAThread]static int Main(string[] args){Directory.CreateDirectory(args[0]);string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"audit-data");Directory.CreateDirectory(root);var config=Path.Combine(root,"assistant.json");File.WriteAllText(config,"{invalid config");var runtime=new AssistantRuntime(root);runtime.Save();Record("corrupt_assistant_config_overwritten",File.ReadAllText(config)!="{invalid config");runtime.Telemetry.Dispose();
 var invalid=new SkillData();invalid.Reminders.Add(null);bool throws=false;try{new SkillEngine(invalid).Tick(DateTime.UtcNow);}catch(NullReferenceException){throws=true;}Record("null_reminder_entry_breaks_tick",throws);
 string source=Path.Combine(root,"import"),target=Path.Combine(root,"target");Directory.CreateDirectory(source);Directory.CreateDirectory(target);File.WriteAllText(Path.Combine(source,"skills.json"),new JavaScriptSerializer().Serialize(invalid));bool accepted=true;try{UserData.QueueImport(source,target);}catch(ArgumentException){accepted=false;}Record("import_accepts_null_reminder_entry",accepted);
 double x=100,y=100;MovementLogic.Step(ref x,ref y,2100,100,5,1920,0,3000,800);Record("cross_monitor_rect_clamps_actor_in_one_frame",x>=1920);
 var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window pet=null;try{
 var card=new MonitorCard();card.Update(new SystemSample{Utc=DateTime.UtcNow.AddHours(-1),Cpu=66,MemoryUsed=8,MemoryTotal=16});var labels=(Dictionary<string,TextBlock>)Get(card,"labels");Record("details_card_accepts_one_hour_old_cpu",labels["cpu"].Text=="66%");card.Close();
 pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();((DispatcherTimer)Get(pet,"ticker")).Stop();((DispatcherTimer)Get(pet,"skillTicker")).Stop();((System.Threading.Timer)Get(pet,"assistantTimer")).Change(-1,-1);((VoiceRuntime)Get(pet,"voices")).Settings.Enabled=false;var skills=(SkillRuntime)Get(pet,"skills");skills.Engine.Data.EndOfWork.Sound=false;skills.Engine.Data.EndOfWork.Gestures=false;var timer=skills.Engine.AddCountdown("普通倒计时",10,DateTime.UtcNow);skills.Engine.SetReminderPoints(timer.Id,new[]{5});var alert=skills.Engine.Tick(timer.DueUtc.AddMinutes(-5)).First(a=>a.Key.StartsWith("timer:"));((BubbleQueue)Get(pet,"messages")).Clear();if(alert.Count>0)Call(pet,"NotifyWorkEnd",alert);else Call(pet,"NotifyUser",alert.Key,alert.Text,alert.Row);Record("ordinary_countdown_routed_as_work_end",((BubbleQueue)Get(pet,"messages")).Current.Key=="work-end");
 var now=DateTime.UtcNow;var sample=new CodexQuotaSnapshot{CheckedUtc=now,AccountKey="test"};sample.Windows.Add(new CodexQuotaWindow{Minutes=300,Remaining=25,ResetUtc=now.AddMinutes(4).AddSeconds(32)});var reminders=new CodexQuotaReader().ResetReminderNotices(sample,new CodexQuotaConfig(),now);Record("requested_reset_notice_has_readable_countdown",reminders.Length==1&&reminders[0].Text.Contains("4分 32秒"));var message=new BubbleMessage{CountdownUtc=now.AddMinutes(4).AddSeconds(32),CountdownLabel="Codex 五小时额度"};Record("requested_reset_countdown_advances",message.DisplayText(now).Contains("4分 32秒")&&message.DisplayText(now.AddSeconds(2)).Contains("4分 30秒"));Record("requested_reset_zero_waits_for_verification",message.DisplayText(now.AddMinutes(5)).Contains("等待官方核验"));
 }finally{if(pet!=null)pet.Close();app.Shutdown();}File.WriteAllText(Path.Combine(args[0],"audit-reproductions.json"),new JavaScriptSerializer().Serialize(results));return results.Values.All(v=>(bool)v)?0:1;}
}
