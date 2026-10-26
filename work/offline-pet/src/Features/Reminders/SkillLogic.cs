using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
public class SkillFocus {
 public List<int> AdvanceMinutes=new List<int>(),FiredOffsets=new List<int>();public long ReminderCycle;
 public DateTime EndUtc;public bool Running,Paused;public double RemainingSeconds;public string Mode="work",Day="";
 public int WorkMinutes=25,ShortMinutes=5,LongMinutes=15,Completed,TotalMinutes;
}
public class SkillReminder {
 public List<int> AdvanceMinutes=new List<int>();public List<int> FiredOffsets=new List<int>();public long ReminderCycle;
 public string Id,Kind="timer",Label;public bool Enabled=true;public DateTime DueUtc;public int RepeatMinutes;
 public override string ToString(){return (Enabled?"待提醒":RepeatMinutes>0?"已关闭":"已完成")+" · "+DueUtc.ToLocalTime().ToString("MM-dd HH:mm")+" · "+Label+(RepeatMinutes>0?" · 每"+RepeatMinutes+"分钟":"");}
}
public class SkillNote {public string Id,Title,Text;public DateTime UpdatedUtc;public override string ToString(){return Title;}}
public class WorkEndSlot {public string Label="下班";public bool Enabled;public int MinuteOfDay=-1;public string ScheduleKey="";public List<int> Fired=new List<int>();}
public class WorkEndSettings:WorkEndSlot {public List<int> AdvanceMinutes=ReminderPoints.WorkDefaults.ToList();public bool Sound=true,Gestures=true;public int CalendarMode;public List<WorkEndSlot> Slots;}
public class CalendarSyncSettings {public bool Enabled;}
public class HolidayDay {public string name,date;public bool isOffDay;}
public class HolidayYear {public int year;public string[] papers;public List<HolidayDay> days;}
public class SkillData {public List<int> WellnessAdvanceMinutes=new List<int>();public WorkEndSettings EndOfWork=new WorkEndSettings();public CalendarSyncSettings CalendarSync=new CalendarSyncSettings();public Dictionary<string,int> FocusDays=new Dictionary<string,int>();public SkillFocus Focus=new SkillFocus();public List<SkillReminder> Reminders=new List<SkillReminder>();public List<SkillNote> Notes=new List<SkillNote>();public int WaterMinutes=45,RestMinutes=60;}
public class SkillAlert {public string Key,Text;public int Row,Count;}
public class RouteInfo {public string Kind,Value;}
public static class NotificationRoute {
 public static RouteInfo Resolve(string key){
  key=key??"";Guid id;int pid;
  if(key.StartsWith("codex:")){string value=key.Substring(6);return new RouteInfo{Kind="codex",Value=Guid.TryParseExact(value,"D",out id)?"codex://threads/"+id.ToString("D"):null};}
  if(key.StartsWith("process:")&&Int32.TryParse(key.Substring(8),out pid)&&pid>0)return new RouteInfo{Kind="process",Value=pid.ToString()};
  if(key.StartsWith("network"))return new RouteInfo{Kind="network",Value="ms-settings:network-status"};
  if(key.StartsWith("task:"))return new RouteInfo{Kind="tasks",Value=key.Substring(5)};
  if(key.StartsWith("file:"))return new RouteInfo{Kind="files",Value=key.Substring(5)};
  if(key=="focus")return new RouteInfo{Kind="focus"};
  if(key=="work-end"||key.StartsWith("work-end:"))return new RouteInfo{Kind="reminders"};
  if(key.StartsWith("timer:"))return new RouteInfo{Kind="reminders",Value=key.Substring(6)};
  if(key.StartsWith("wellness:"))return new RouteInfo{Kind="wellness"};
  return new RouteInfo{Kind="history"};
 }
}
public static class SkillLogic {
 public static bool WebTarget(string text){Uri uri;return Uri.TryCreate(text,UriKind.Absolute,out uri)&&(uri.Scheme=="http"||uri.Scheme=="https")&&!String.IsNullOrEmpty(uri.Host)&&String.IsNullOrEmpty(uri.UserInfo);}
 public static string CleanLines(string text,bool unique){var lines=text.Replace("\r\n","\n").Replace('\r','\n').Split('\n').Select(x=>x.Trim()).Where(x=>x.Length>0);return String.Join(Environment.NewLine,(unique?lines.Distinct(StringComparer.Ordinal):lines).ToArray());}
 public static string PrettyJson(string text){var json=new JavaScriptSerializer();return FormatJson(json.DeserializeObject(text),0,json);}
 static string FormatJson(object value,int level,JavaScriptSerializer json){
  string pad=new string(' ',level*2),inner=pad+"  ";var map=value as IDictionary<string,object>;
  if(map!=null)return map.Count==0?"{}":"{\n"+String.Join(",\n",map.Select(p=>inner+json.Serialize(p.Key)+": "+FormatJson(p.Value,level+1,json)).ToArray())+"\n"+pad+"}";
  var list=value as IList;if(list!=null)return list.Count==0?"[]":"[\n"+String.Join(",\n",list.Cast<object>().Select(x=>inner+FormatJson(x,level+1,json)).ToArray())+"\n"+pad+"]";
  return json.Serialize(value);
 }
 public static string Clock(double seconds){var time=TimeSpan.FromSeconds(Math.Max(0,Math.Ceiling(seconds)));return time.TotalHours>=1?time.ToString(@"hh\:mm\:ss"):time.ToString(@"mm\:ss");}
}
public class SkillEngine {
 public readonly SkillData Data;public bool Dirty;public readonly Dictionary<int,HolidayYear> Calendars=new Dictionary<int,HolidayYear>();
 public SkillEngine(SkillData data){Data=data;if(Data.CalendarSync==null)Data.CalendarSync=new CalendarSyncSettings();if(Data.EndOfWork==null)Data.EndOfWork=new WorkEndSettings();if(Data.EndOfWork.Fired==null)Data.EndOfWork.Fired=new List<int>();if(Data.FocusDays==null)Data.FocusDays=new Dictionary<string,int>();}
 public static void Minutes(int value,int maximum){if(value<1||value>maximum)throw new ArgumentOutOfRangeException("minutes","分钟范围为1–"+maximum+"。");}
 public double FocusRemaining(DateTime now){return Data.Focus.Paused?Data.Focus.RemainingSeconds:Data.Focus.Running?Math.Max(0,(Data.Focus.EndUtc-now).TotalSeconds):0;}
 void Day(DateTime now){string day=now.ToLocalTime().ToString("yyyy-MM-dd");if(Data.Focus.Day!=day){Data.Focus.Day=day;Data.Focus.Completed=0;Data.Focus.TotalMinutes=0;Dirty=true;}}
 public void StartFocus(DateTime now){var f=Data.Focus;if(f.Running||f.Paused)throw new ArgumentException("已有专注计时，请先停止或继续当前计时。");Minutes(f.WorkMinutes,180);Minutes(f.ShortMinutes,120);Minutes(f.LongMinutes,120);Day(now);f.Mode="work";f.Paused=false;f.Running=true;f.EndUtc=now.AddMinutes(f.WorkMinutes);Dirty=true;}
 public void PauseFocus(DateTime now){var f=Data.Focus;if(!f.Running)return;f.RemainingSeconds=FocusRemaining(now);f.Running=false;f.Paused=true;Dirty=true;}
 public void ResumeFocus(DateTime now){var f=Data.Focus;if(!f.Paused)return;f.EndUtc=now.AddSeconds(f.RemainingSeconds);f.ReminderCycle=f.EndUtc.Ticks;f.Running=true;f.Paused=false;Dirty=true;}
 public void StopFocus(){var f=Data.Focus;f.Running=false;f.Paused=false;f.RemainingSeconds=0;f.Mode="work";Dirty=true;}
 public List<SkillAlert> Tick(DateTime now){
  Day(now);var alerts=new List<SkillAlert>();var f=Data.Focus;
  if(f.Running&&now<f.EndUtc){if(f.ReminderCycle!=f.EndUtc.Ticks){f.ReminderCycle=f.EndUtc.Ticks;f.FiredOffsets=new List<int>();Dirty=true;}if(f.FiredOffsets==null)f.FiredOffsets=new List<int>();double duration=f.Mode=="work"?f.WorkMinutes:f.Mode=="long"?f.LongMinutes:f.ShortMinutes;int before=(int)Math.Ceiling((f.EndUtc-now).TotalMinutes);if(before>0&&before<duration&&(f.AdvanceMinutes??new List<int>()).Contains(before)&&!f.FiredOffsets.Contains(before)){f.FiredOffsets.Add(before);Dirty=true;alerts.Add(new SkillAlert{Key="timer:focus:"+f.EndUtc.Ticks+":"+before,Text=(f.Mode=="work"?"专注阶段":"休息阶段")+"还有 "+before+" 分钟结束。",Row=3});}}
  if(f.Running&&now>=f.EndUtc){
   if(f.Mode=="work"){f.Completed++;f.TotalMinutes+=f.WorkMinutes;string date=now.ToLocalTime().ToString("yyyy-MM-dd");int minutes;Data.FocusDays.TryGetValue(date,out minutes);Data.FocusDays[date]=minutes+f.WorkMinutes;foreach(var old in Data.FocusDays.Keys.OrderByDescending(d=>d).Skip(366).ToArray())Data.FocusDays.Remove(old);f.Mode=f.Completed%4==0?"long":"short";f.EndUtc=now.AddMinutes(f.Mode=="long"?f.LongMinutes:f.ShortMinutes);alerts.Add(new SkillAlert{Key="focus",Text="这一轮专注结束了。休息"+(f.Mode=="long"?f.LongMinutes:f.ShortMinutes)+"分钟，活动一下。",Row=3});}
   else{f.Running=false;f.Mode="work";alerts.Add(new SkillAlert{Key="focus",Text="休息结束。准备好了就开始下一轮专注。",Row=3});}Dirty=true;
  }
  foreach(var r in Data.Reminders.Where(x=>x!=null&&x.Enabled&&x.DueUtc>now)){
   if(r.ReminderCycle!=r.DueUtc.Ticks){r.ReminderCycle=r.DueUtc.Ticks;r.FiredOffsets=new List<int>();Dirty=true;}if(r.FiredOffsets==null)r.FiredOffsets=new List<int>();
   int before=(int)Math.Ceiling((r.DueUtc-now).TotalMinutes);if(((r.Kind=="timer"?r.AdvanceMinutes:Data.WellnessAdvanceMinutes)??new List<int>()).Contains(before)&&before>0&&(r.RepeatMinutes<=0||before<r.RepeatMinutes)&&!r.FiredOffsets.Contains(before)){r.FiredOffsets.Add(before);Dirty=true;alerts.Add(new SkillAlert{Key=(r.Kind=="timer"?"timer:":"wellness:")+r.Id+":"+r.DueUtc.Ticks+":"+before,Text=r.Label+"：还有 "+before+" 分钟。",Row=3,Count=before});}
  }
  foreach(var r in Data.Reminders.Where(x=>x!=null&&x.Enabled&&x.DueUtc<=now).ToArray()){
   alerts.Add(new SkillAlert{Key=r.Kind=="timer"?"timer:"+r.Id:"wellness:"+r.Kind,Text=r.Label+"：到时间了。",Row=r.Kind=="timer"?3:6});
   if(r.RepeatMinutes>0)r.DueUtc=now.AddMinutes(r.RepeatMinutes);else r.Enabled=false;Dirty=true;
  }
  alerts.AddRange(WorkEndTick(now));
  return alerts;
 }
 public bool? WorkDay(DateTime local){var setting=Data.EndOfWork;bool weekday=local.DayOfWeek!=DayOfWeek.Saturday&&local.DayOfWeek!=DayOfWeek.Sunday;if(setting.CalendarMode==2)return true;if(setting.CalendarMode==1)return weekday;HolidayYear year;if(!Calendars.TryGetValue(local.Year,out year))return null;var day=year.days.FirstOrDefault(d=>d.date==local.ToString("yyyy-MM-dd"));return day==null?weekday:!day.isOffDay;}
 public WorkEndSlot[] WorkEndSlots(){return Data.EndOfWork.Slots==null?new WorkEndSlot[]{Data.EndOfWork}:Data.EndOfWork.Slots.Where(s=>s!=null).ToArray();}
 public string WorkEndStatus(DateTime now){var local=now.ToLocalTime();if(!Data.EndOfWork.Enabled)return "下班倒计时未启用";var slots=WorkEndSlots().Where(s=>s.Enabled&&s.MinuteOfDay>=0&&s.MinuteOfDay<1440).OrderBy(s=>s.MinuteOfDay).ToArray();if(slots.Length==0)return "请添加并启用下班时段";var work=WorkDay(local);if(!work.HasValue)return local.Year+" 年节假日日历缺失，请同步后再提醒";if(!work.Value)return "今天休息，不发送下班提醒";var next=slots.FirstOrDefault(s=>local.Date.AddMinutes(s.MinuteOfDay)>local);return next==null?"今天所有下班时间已到":next.Label+" · "+TimeSpan.FromMinutes(next.MinuteOfDay).ToString(@"hh\:mm")+" · 还有 "+SkillLogic.Clock((local.Date.AddMinutes(next.MinuteOfDay)-local).TotalSeconds);}
 public void SetWorkEnd(bool enabled,int minute,int mode){if(minute<0||minute>=1440||mode<0||mode>2)throw new ArgumentException("请设置有效的下班时间和工作日规则。");Data.EndOfWork.Slots=null;Data.EndOfWork.Enabled=enabled;Data.EndOfWork.MinuteOfDay=minute;Data.EndOfWork.CalendarMode=mode;Dirty=true;}
 public void SetWorkEnds(bool enabled,int mode,WorkEndSlot[] slots){
  if(mode<0||mode>2||slots==null||slots.Length>20)throw new ArgumentException("请设置有效的工作日规则，最多20个下班时段。");
  foreach(var slot in slots)if(slot==null||String.IsNullOrWhiteSpace(slot.Label)||slot.Label.Trim().Length>20||slot.MinuteOfDay<0||slot.MinuteOfDay>=1440)throw new ArgumentException("时段名称需为1–20字，请选择有效的小时和分钟。");
  if(slots.Where(s=>s.Enabled).GroupBy(s=>s.MinuteOfDay).Any(g=>g.Count()>1))throw new ArgumentException("已启用的下班时段不能使用相同时间。");
  var old=WorkEndSlots();var saved=new List<WorkEndSlot>();foreach(var slot in slots){var previous=old.FirstOrDefault(o=>o.MinuteOfDay==slot.MinuteOfDay);saved.Add(new WorkEndSlot{Label=slot.Label.Trim(),Enabled=slot.Enabled,MinuteOfDay=slot.MinuteOfDay,ScheduleKey=previous==null?"":previous.ScheduleKey,Fired=previous==null||previous.Fired==null?new List<int>():new List<int>(previous.Fired)});}
  Data.EndOfWork.Enabled=enabled;Data.EndOfWork.CalendarMode=mode;Data.EndOfWork.Slots=saved;Dirty=true;
 }
 List<SkillAlert> WorkEndTick(DateTime now){var alerts=new List<SkillAlert>();if(!Data.EndOfWork.Enabled)return alerts;var local=now.ToLocalTime();if(WorkDay(local)!=true)return alerts;
  foreach(var s in WorkEndSlots().OrderBy(s=>s.MinuteOfDay)){if(!s.Enabled||s.MinuteOfDay<0||s.MinuteOfDay>=1440)continue;int minute=(int)Math.Ceiling((local.Date.AddMinutes(s.MinuteOfDay)-local).TotalMinutes);if(!(Data.EndOfWork.AdvanceMinutes??ReminderPoints.WorkDefaults.ToList()).Contains(minute))continue;string key=local.ToString("yyyy-MM-dd")+":"+s.MinuteOfDay+":"+Data.EndOfWork.CalendarMode;if(s.Fired==null)s.Fired=new List<int>();if(s.ScheduleKey!=key){s.ScheduleKey=key;s.Fired.Clear();Dirty=true;}if(s.Fired.Contains(minute))continue;s.Fired.Add(minute);Dirty=true;alerts.Add(new SkillAlert{Key="work-end:"+key+":"+minute,Text=minute==0?s.Label+"时间到了。":"距离"+s.Label+"（"+TimeSpan.FromMinutes(s.MinuteOfDay).ToString(@"hh\:mm")+"）还有 "+minute+" 分钟。",Row=3,Count=minute});}return alerts;
 }
 public SkillReminder AddCountdown(string label,int minutes,DateTime now){Minutes(minutes,1440);return AddReminder(label,now.AddMinutes(minutes),now);}
 public SkillReminder AddReminder(string label,DateTime due,DateTime now){
  if(String.IsNullOrWhiteSpace(label)||label.Trim().Length>120)throw new ArgumentException("请输入1–120字的提醒内容。");
  if(due<=now||due>now.AddYears(1))throw new ArgumentException("请选择未来一年内的时间。");
  if(Data.Reminders.Count>=100){var done=Data.Reminders.FirstOrDefault(x=>!x.Enabled);if(done!=null)Data.Reminders.Remove(done);else throw new ArgumentException("最多同时保存100项提醒。");}
  var r=new SkillReminder{Id=Guid.NewGuid().ToString("N"),Label=label.Trim(),DueUtc=due,Kind="timer"};Data.Reminders.Add(r);Dirty=true;return r;
 }
 public void SetReminderPoints(string id,IEnumerable<int> points){var values=ReminderPoints.Parse(String.Join(",",points));var r=Data.Reminders.First(x=>x.Id==id);if(r.Kind!="timer")throw new ArgumentException("健康提醒请在健康页设置。");r.AdvanceMinutes=values;Dirty=true;}
 public void SetRepeat(string id,int minutes){if(minutes<0||minutes>10080)throw new ArgumentException("重复间隔范围为0–10080分钟，0为单次提醒。");var r=Data.Reminders.First(x=>x.Id==id);if(r.Kind!="timer")throw new ArgumentException("健康提醒请在健康页设置。");r.RepeatMinutes=minutes;Dirty=true;}
 public void Snooze(string id,DateTime now){var r=Data.Reminders.First(x=>x.Id==id);r.Enabled=true;r.DueUtc=now.AddMinutes(5);Dirty=true;}
 public void SetWellness(string kind,bool enabled,int minutes,DateTime now){
  if(kind!="water"&&kind!="rest")throw new ArgumentException("未知提醒类型。");Minutes(minutes,240);
  var r=Data.Reminders.FirstOrDefault(x=>x.Kind==kind);if(r==null){r=new SkillReminder{Id=Guid.NewGuid().ToString("N"),Kind=kind,Label=kind=="water"?"喝水提醒":"休息提醒"};Data.Reminders.Add(r);}
  r.Enabled=enabled;r.RepeatMinutes=minutes;r.DueUtc=now.AddMinutes(minutes);if(kind=="water")Data.WaterMinutes=minutes;else Data.RestMinutes=minutes;Dirty=true;
 }
 public SkillNote SaveNote(string id,string title,string text){
  if(String.IsNullOrWhiteSpace(text)||text.Length>20000)throw new ArgumentException("便签正文需为1–20000字。");
  if(title.Length>60)throw new ArgumentException("便签标题最多60字。");
  var n=Data.Notes.FirstOrDefault(x=>x.Id==id);if(n==null){if(Data.Notes.Count>=50)throw new ArgumentException("最多保存50张便签。");n=new SkillNote{Id=Guid.NewGuid().ToString("N")};Data.Notes.Add(n);}
  n.Title=String.IsNullOrWhiteSpace(title)?"未命名便签":title.Trim();n.Text=text;n.UpdatedUtc=DateTime.UtcNow;Dirty=true;return n;
 }
 public void DeleteNote(string id){Data.Notes.RemoveAll(x=>x.Id==id);Dirty=true;}
}
