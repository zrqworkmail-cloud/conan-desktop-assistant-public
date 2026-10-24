using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web.Script.Serialization;
public class SkillRuntime {
 readonly string path,calendarFolder;public string CalendarStatus="日历未加载";readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=4194304};
 public readonly SkillEngine Engine;public string LoadError;
 public SkillRuntime(string home,string dataFolder=null){
  path=Path.Combine(dataFolder??home,"skills.json");calendarFolder=Path.Combine(dataFolder??home,"calendars");SkillData data=null;
  if(File.Exists(path))try{data=json.Deserialize<SkillData>(File.ReadAllText(path));if(data==null||data.Focus==null||data.Reminders==null||data.Notes==null)throw new ArgumentException("数据结构不完整");}catch(Exception e){if(!(e is IOException)&&!(e is ArgumentException)&&!(e is UnauthorizedAccessException))throw;LoadError="技能数据无法读取，原文件已保留："+e.Message;}
  Engine=new SkillEngine(data??new SkillData());
  LoadCalendars(Path.Combine(home,"calendars"));if(!String.Equals(Path.GetFullPath(calendarFolder),Path.GetFullPath(Path.Combine(home,"calendars")),StringComparison.OrdinalIgnoreCase))LoadCalendars(calendarFolder);
  if(Engine.Calendars.Count>0)CalendarStatus="已缓存年份："+String.Join("、",Engine.Calendars.Keys.OrderBy(y=>y).Select(y=>y.ToString()).ToArray())+" · 来源 holiday-cn / 国务院公告";
 }
 void LoadCalendars(string folder){
  if(Directory.Exists(folder))foreach(var file in Directory.GetFiles(folder,"*.json"))try{int year;if(!Int32.TryParse(Path.GetFileNameWithoutExtension(file),out year))continue;var calendar=ParseCalendar(File.ReadAllText(file),year);Engine.Calendars[year]=calendar;}catch(Exception e){if(!(e is IOException)&&!(e is ArgumentException)&&!(e is UnauthorizedAccessException))throw;CalendarStatus="部分日历无法读取："+e.Message;}
 }
 public static HolidayYear ParseCalendar(string text,int year){var value=new JavaScriptSerializer{MaxJsonLength=262144}.Deserialize<HolidayYear>(text);if(value==null||value.year!=year||value.days==null||value.days.Count==0||value.papers==null||value.papers.Length==0)throw new ArgumentException("日历年份或公告来源不完整。");var seen=new HashSet<string>();foreach(var day in value.days){DateTime date;if(day==null||!DateTime.TryParseExact(day.date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date)||date.Year!=year||!seen.Add(day.date))throw new ArgumentException("日历包含错误或重复的日期。");}foreach(var paper in value.papers){Uri uri;if(!Uri.TryCreate(paper,UriKind.Absolute,out uri)||uri.Scheme!="https"||!(uri.Host=="gov.cn"||uri.Host.EndsWith(".gov.cn",StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("日历缺少政府公告链接。");}return value;}
 public async Task<string> SyncCalendars(){ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var status=new List<string>();foreach(int year in new[]{DateTime.Now.Year,DateTime.Now.Year+1})try{using(var client=new CalendarClient()){client.Encoding=Encoding.UTF8;var download=client.DownloadStringTaskAsync(new Uri("https://raw.githubusercontent.com/NateScarlet/holiday-cn/master/"+year+".json"));if(await Task.WhenAny(download,Task.Delay(15000))!=download){client.CancelAsync();throw new WebException("日历同步超时。");}var text=await download;var value=ParseCalendar(text,year);Directory.CreateDirectory(calendarFolder);string path=Path.Combine(calendarFolder,year+".json"),temp=path+".tmp";File.WriteAllText(temp,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);Engine.Calendars[year]=value;status.Add(year+" 已同步");}}catch(Exception e){if(!(e is WebException)&&!(e is IOException)&&!(e is ArgumentException)&&!(e is UnauthorizedAccessException))throw;status.Add(year+" 未更新（可能尚未公布或网络不可用），保留已有缓存");}CalendarStatus=String.Join("；",status.ToArray());return CalendarStatus;}
 class CalendarClient:WebClient {protected override WebRequest GetWebRequest(Uri address){var request=base.GetWebRequest(address);request.Timeout=15000;return request;}}
 public void Save(){if(LoadError!=null)throw new IOException(LoadError);string temp=path+".tmp";File.WriteAllText(temp,json.Serialize(Engine.Data),new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);Engine.Dirty=false;}
 public List<SkillAlert> Tick(DateTime now){if(LoadError!=null)return new List<SkillAlert>();var result=Engine.Tick(now);if(Engine.Dirty)Save();return result;}
}
