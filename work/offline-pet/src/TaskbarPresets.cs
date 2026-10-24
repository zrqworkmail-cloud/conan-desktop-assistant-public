using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;
public class TaskbarPreset {public string Name;public TaskbarAppearance Appearance;public List<string> TaskbarItems;}
public static class TaskbarPresets {
 public static readonly string[] Builtins={"透明文字","紧凑双行","极简单行","柯南蓝","基德银"};
 public static bool Builtin(string name){return Builtins.Contains(name);}
 public static TaskbarPreset Find(MonitorConfig config,string name){return (config.TaskbarPresets??new List<TaskbarPreset>()).FirstOrDefault(p=>p!=null&&String.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase));}
 public static string[] Names(MonitorConfig config){return Builtins.Concat((config.TaskbarPresets??new List<TaskbarPreset>()).Where(p=>p!=null&&!String.IsNullOrWhiteSpace(p.Name)).Select(p=>p.Name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
 public static TaskbarPreset Capture(string name,MonitorConfig config){name=(name??"").Trim();if(name.Length==0||name.Length>24||name.Any(Char.IsControl))throw new ArgumentException("预设名称需为1–24个字，不能包含控制字符。");if(config.TaskbarItems==null||config.TaskbarItems.Count==0||config.TaskbarItems.Any(k=>!TaskbarAppearance.Keys.Contains(k))||config.TaskbarItems.Distinct().Count()!=config.TaskbarItems.Count)throw new ArgumentException("请至少选择一个有效显示项，不要重复。");var json=new JavaScriptSerializer();var appearance=json.Deserialize<TaskbarAppearance>(json.Serialize(config.Appearance));if(appearance==null)throw new ArgumentException("预设外观不能为空。");appearance.Validate();return new TaskbarPreset{Name=name,Appearance=appearance,TaskbarItems=config.TaskbarItems.ToList()};}
 public static TaskbarPreset Load(MonitorConfig config,string name){var saved=Find(config,name);if(saved!=null)return Capture(saved.Name,new MonitorConfig{Appearance=saved.Appearance,TaskbarItems=saved.TaskbarItems});if(!Builtin(name))throw new ArgumentException("请先选择一个已保存的预设。");return new TaskbarPreset{Name=name,Appearance=TaskbarAppearance.Preset(name),TaskbarItems=config.TaskbarItems.ToList()};}
 public static List<TaskbarPreset> Save(MonitorConfig library,string name,MonitorConfig draft,bool overwrite){var captured=Capture(name,draft);bool exists=Find(library,captured.Name)!=null||Builtin(captured.Name);if(!overwrite&&exists)throw new ArgumentException("同名预设已存在，请换个名称或使用“覆盖所选预设”。");if(overwrite&&!exists)throw new ArgumentException("请先选择一个已保存的预设。");var next=(library.TaskbarPresets??new List<TaskbarPreset>()).Where(p=>p!=null&&!String.Equals(p.Name,captured.Name,StringComparison.OrdinalIgnoreCase)).ToList();next.Add(captured);return next;}
 public static List<TaskbarPreset> Reset(MonitorConfig config,string name){if(!Builtin(name))throw new ArgumentException("只有内置预设可以恢复默认；自定义预设可重新载入或删除。");return Without(config,name);}
 public static List<TaskbarPreset> Delete(MonitorConfig config,string name){if(Builtin(name)||Find(config,name)==null)throw new ArgumentException("请选择一个自定义预设；内置预设不能删除。");return Without(config,name);}
 static List<TaskbarPreset> Without(MonitorConfig config,string name){return (config.TaskbarPresets??new List<TaskbarPreset>()).Where(p=>p!=null&&!String.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase)).ToList();}
}
