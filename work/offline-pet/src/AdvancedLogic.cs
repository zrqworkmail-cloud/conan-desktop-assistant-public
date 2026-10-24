using System;
using System.Linq;
using System.Collections.Generic;
public class SkillTask {
 public List<int> FiredOffsets=new List<int>();public long ReminderCycle;
 public string Id,Title;public DateTime? DueUtc;public DateTime CreatedUtc;public int Priority;public bool Done,Notified;
 public override string ToString(){return (Done?"✓ ":Priority==2?"! ":"○ ")+Title+(DueUtc.HasValue?"   · "+DueUtc.Value.ToLocalTime().ToString("MM-dd HH:mm"):"");}
}
public class FileWatchSetting {public string Id,Path;public override string ToString(){return System.IO.Path.GetFileName(Path)+"   · "+Path;}}
public class AdvancedData {
 public List<int> TaskAdvanceMinutes=new List<int>();
 public List<SkillTask> Tasks=new List<SkillTask>();public List<FileWatchSetting> Files=new List<FileWatchSetting>();
 public bool ClipboardEnabled,SpeakNotifications,Hotkeys;
}
public class AdvancedEngine {
 public readonly AdvancedData Data;public bool Dirty;
 public AdvancedEngine(AdvancedData data){Data=data;}
 public SkillTask AddTask(string title,DateTime? due,int priority){
  if(String.IsNullOrWhiteSpace(title)||title.Trim().Length>160)throw new ArgumentException("待办内容需为1–160字。");
  if(priority<0||priority>2)throw new ArgumentException("请选择有效的优先级。");
  if(Data.Tasks.Count>=300)throw new ArgumentException("最多保存300条待办，请清理已完成项目。");
  var task=new SkillTask{Id=Guid.NewGuid().ToString("N"),Title=title.Trim(),DueUtc=due,Priority=priority,CreatedUtc=DateTime.UtcNow};Data.Tasks.Add(task);Dirty=true;return task;
 }
 public void ToggleTask(string id){var task=Data.Tasks.First(x=>x.Id==id);task.Done=!task.Done;Dirty=true;}
 public void DeleteTask(string id){Data.Tasks.RemoveAll(x=>x.Id==id);Dirty=true;}
 public List<SkillAlert> Tick(DateTime now){var alerts=new List<SkillAlert>();foreach(var task in Data.Tasks.Where(t=>t!=null&&!t.Done&&t.DueUtc.HasValue&&t.DueUtc.Value>now)){if(task.ReminderCycle!=task.DueUtc.Value.Ticks){task.ReminderCycle=task.DueUtc.Value.Ticks;task.FiredOffsets=new List<int>();Dirty=true;}if(task.FiredOffsets==null)task.FiredOffsets=new List<int>();int before=(int)Math.Ceiling((task.DueUtc.Value-now).TotalMinutes);if(before>0&&(Data.TaskAdvanceMinutes??new List<int>()).Contains(before)&&!task.FiredOffsets.Contains(before)){task.FiredOffsets.Add(before);Dirty=true;alerts.Add(new SkillAlert{Key="task:"+task.Id+":"+before,Text="待办还有 "+before+" 分钟到期："+task.Title,Row=6});}}foreach(var task in Data.Tasks.Where(t=>!t.Done&&!t.Notified&&t.DueUtc.HasValue&&t.DueUtc.Value<=now)){task.Notified=true;Dirty=true;alerts.Add(new SkillAlert{Key="task:"+task.Id,Text="待办到期："+task.Title,Row=6});}return alerts;}
}
public class ClipboardHistory {
 public readonly List<string> Items=new List<string>();public bool Enabled;
 public void SetEnabled(bool enabled){Enabled=enabled;if(!enabled)Items.Clear();}
 public void Capture(string text){if(!Enabled||String.IsNullOrWhiteSpace(text)||text.Length>10000)return;if(Items.Count>0&&Items[0]==text)return;Items.Remove(text);Items.Insert(0,text);if(Items.Count>30)Items.RemoveAt(Items.Count-1);}
}
