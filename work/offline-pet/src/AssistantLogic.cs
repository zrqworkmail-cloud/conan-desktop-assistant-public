using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class AssistantLogic {
 public static string EventDirectory(string home){string custom=Environment.GetEnvironmentVariable("CONAN_PET_EVENT_DIR");if(!String.IsNullOrWhiteSpace(custom))return custom;return Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1"?Path.Combine(home,"events"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Codex","ConanAssistant","events");}
 public static double Cpu(double cpuSeconds,double elapsed,int cores){
  if(cpuSeconds<0||elapsed<=0||cores<=0)return 0;return Math.Max(0,Math.Min(100,cpuSeconds/elapsed/cores*100));
 }
 public static string State(string name){
  switch(name){case "SessionStart":return "ready";case "UserPromptSubmit":return "thinking";case "PreToolUse":case "PostToolUse":return "working";case "PermissionRequest":return "waiting";case "Stop":return "ended";case "Interrupt":return "interrupted";case "SessionEnd":return "closed";default:return null;}
 }
 public static bool LocalTarget(string path){return !String.IsNullOrWhiteSpace(path)&&Path.IsPathRooted(path)&&!path.StartsWith(@"\\")&&(File.Exists(path)||Directory.Exists(path));}
 public static string Label(string state){
  switch(state){case "ready":return "已连接";case "thinking":return "正在处理";case "working":return "正在执行工具";case "waiting":return "等待你确认";case "ended":return "本轮已返回结果";case "interrupted":return "已中断";case "closed":return "会话已结束";case "stale":return "状态已过期，等待更新";default:return "尚未收到任务事件";}
 }
}
public class CodexTask {public string Id,Status,Title;public DateTime Updated;public bool AttentionSent;}
public class CodexFeed {
 public readonly Dictionary<string,CodexTask> Tasks=new Dictionary<string,CodexTask>();
 public void Apply(string id,string state,DateTime when,string title=null){
  CodexTask old;if(Tasks.TryGetValue(id,out old)&&old.Updated>=when){if(!String.IsNullOrEmpty(title))old.Title=title;return;}
  if(!Tasks.ContainsKey(id)&&Tasks.Count>=64)Tasks.Remove(Tasks.Values.OrderBy(t=>t.Updated).First().Id);
  Tasks[id]=new CodexTask{Id=id,Status=state,Updated=when,Title=String.IsNullOrEmpty(title)&&old!=null?old.Title:title};
 }
 public string State(string id,DateTime now){CodexTask t;if(!Tasks.TryGetValue(id,out t))return null;return now-t.Updated>TimeSpan.FromMinutes(15)?"stale":t.Status;}
 public bool TakeAttention(string id,DateTime now){CodexTask task;if(!Tasks.TryGetValue(id,out task)||task.AttentionSent||State(id,now)!="waiting"||now-task.Updated<TimeSpan.FromSeconds(3))return false;task.AttentionSent=true;return true;}
 public int Animation(DateTime now){
  var states=Tasks.Keys.Select(id=>State(id,now)).ToArray();
  if(states.Contains("waiting"))return 6;
  if(states.Contains("thinking")||states.Contains("working"))return 7;
  return -1;
 }
}
public class NoticeFeed {
 public readonly List<string> Items=new List<string>();
 readonly Dictionary<string,DateTime> last=new Dictionary<string,DateTime>();
 public bool Add(string key,string text,DateTime when){
  DateTime old;if(last.TryGetValue(key,out old)&&when-old<TimeSpan.FromSeconds(30))return false;
  last[key]=when;if(last.Count>256)last.Remove(last.OrderBy(x=>x.Value).First().Key);
  Items.Insert(0,when.ToLocalTime().ToString("HH:mm:ss")+"  "+text);if(Items.Count>100)Items.RemoveAt(100);return true;
 }
}
