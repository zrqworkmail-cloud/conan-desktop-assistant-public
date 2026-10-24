using System;
using System.Collections.Generic;
public class VoiceClip {
 public bool Candidate;public string Id,File,Japanese,Chinese,Note,Action,Emotion;public int Count;public string[] Events=new string[0];public double Seconds;
 public override string ToString(){return (String.IsNullOrEmpty(Chinese)?"仅试听":Chinese)+"  ·  "+Seconds.ToString("F1")+"秒"+(Events.Length>0?"  ·  自动配音":"  ·  手动试听");}
}
public class VoiceCatalog {public List<VoiceClip> Clips=new List<VoiceClip>();}
public class VoiceSettings {public bool Enabled=true,Interactions=true,Workflows=true,Reminders=false,ClickToPreview=true,Ambient=true;public double Volume=.20;}
public class VoicePolicy {
 readonly Dictionary<string,double> kinds=new Dictionary<string,double>(),keys=new Dictionary<string,double>();double next=Double.NegativeInfinity;
 public int KeyCount{get{return keys.Count;}}
 public bool CanPlay(string kind,string key,double now,double duration,bool playing,bool suppressed){
  if(suppressed)return false;if(kind=="preview")return true;if(kind=="monitor-alert"){double recent;return !playing&&(!kinds.TryGetValue(kind,out recent)||now-recent>=30);}if(kind=="work-end")return !playing;if(playing||now<next)return false;double previous;if(kinds.TryGetValue(kind,out previous)&&now-previous<(kind=="ambient"?45:kind=="click"?30:20))return false;return String.IsNullOrEmpty(key)||!keys.TryGetValue(key,out previous)||now-previous>=120;
 }
 public void Mark(string kind,string key,double now,double duration){if(kind=="preview"){next=now+Math.Max(8,duration+1);return;}next=now+Math.Max(8,duration+1);kinds[kind]=now;if(!String.IsNullOrEmpty(key)){if(keys.Count>=256){string oldest=null;double time=Double.MaxValue;foreach(var pair in keys)if(pair.Value<time){oldest=pair.Key;time=pair.Value;}if(oldest!=null)keys.Remove(oldest);}keys[key]=now;}}
 public static string NotificationEvent(string key,int row){if(key.StartsWith("system:")||key.StartsWith("file:")||key.StartsWith("resource:")||key.StartsWith("process:")||key.StartsWith("exit:")||key.StartsWith("high:")||key.StartsWith("network:"))return null;if(key=="focus")return "focus-complete";if(key.StartsWith("timer:")||key.StartsWith("wellness:")||key.StartsWith("task:")||key.StartsWith("codex-reset-soon:"))return "reminder";if(key.StartsWith("codex:"))return null;return null;}
}
