using System;
using System.Collections.Generic;
using System.Globalization;
public class BubbleMessage {public Func<bool> StillValid;public string Key,Text;public bool Notification;public int Row;public DateTime? CountdownUtc;public string CountdownLabel;public string DisplayText(DateTime now){return !CountdownUtc.HasValue?Text:CountdownLabel+"\n"+(CountdownUtc.Value>now?"距离预计重置还有 "+BubbleLogic.TimeLeft(CountdownUtc.Value-now):"预计重置时间已到，等待官方核验")+"\n预计重置："+CountdownUtc.Value.ToLocalTime().ToString("MM-dd HH:mm:ss");}}
public struct BubblePlacement {public double X,Y;public bool Above;public string Side;}
public static class BubbleLogic {
 public static string TimeLeft(TimeSpan value){long total=(long)Math.Max(0,Math.Ceiling(value.TotalSeconds));long days=total/86400,hours=total/3600%24,minutes=total/60%60,seconds=total%60;return (days>0?days+"天 ":"")+(hours>0?hours+"小时 ":"")+(minutes>0?minutes+"分 ":"")+seconds+"秒";}
 public static string Heading(BubbleMessage m){if(!m.Notification)return "柯南 · 小侦探";string key=m.Key??"";return key.StartsWith("codex-reset-soon:")?"Codex 额度重置提醒":key.StartsWith("codex-reset:")?"Codex 额度更新":key.StartsWith("monitor:")?"系统资源预警":key.StartsWith("work-end")?"下班倒计时":key.StartsWith("system:")?"系统通知":key.StartsWith("codex:")?"Codex 动态":key.StartsWith("network")?"网络侦察":key=="focus"?"专注计时":key.StartsWith("timer:")?"定时提醒":key.StartsWith("wellness:")?"健康提醒":key.StartsWith("task:")?"待办到期":key.StartsWith("file:")?"文件动态":"进程提醒";}
 public static string Readable(string text){return (text??"").Replace("\r\n","\n").Replace('\r','\n').Replace("\t","  ").Trim();}
 public static BubblePlacement Arrange(double left,double top,double bodyWidth,double bodyHeight,double width,double height,double screenLeft,double screenTop,double screenRight,double screenBottom,string preference,string previous=null){
  string[] sides=preference=="top"?new[]{"top","upper-right","upper-left","bottom"}:preference=="bottom"?new[]{"bottom","right","left","top"}:preference=="right"?new[]{"right","left","top","bottom"}:preference=="left"?new[]{"left","right","top","bottom"}:preference=="upper"?new[]{"upper-right","upper-left","top","right","left","bottom"}:new[]{"upper-right","upper-left","right","left","top","bottom"};
  double best=Double.PositiveInfinity;var result=new BubblePlacement();for(int i=0;i<sides.Length;i++){string side=sides[i];double x=side.EndsWith("right")?left+bodyWidth+14:side.EndsWith("left")?left-width-14:left+bodyWidth/2-width/2;double y=side=="top"?top-height-14:side=="bottom"?top+bodyHeight+14:side.StartsWith("upper")?top-height*.65:top+Math.Min(40,bodyHeight*.2);double cx=Math.Max(screenLeft+8,Math.Min(screenRight-width-8,x)),cy=Math.Max(screenTop+8,Math.Min(screenBottom-height-8,y));double overlap=Math.Max(0,Math.Min(cx+width,left+bodyWidth)-Math.Max(cx,left))*Math.Max(0,Math.Min(cy+height,top+bodyHeight)-Math.Max(cy,top));double score=(Math.Abs(cx-x)+Math.Abs(cy-y))*100+overlap*10+i+(preference=="auto"&&previous==side?-2:0);if(score<best){best=score;result=new BubblePlacement{X=cx,Y=cy,Above=side=="top",Side=side};}}return result;
 }
 public static double Duration(string text,bool notice){return Math.Min(notice?20:14,Math.Max(notice?7:4.5,2+new StringInfo(text).LengthInTextElements/5.0));}
 public static string Preview(string text){int[] starts=StringInfo.ParseCombiningCharacters(text);return starts.Length<=140?text:text.Substring(0,starts[139])+"…";}
 public static string Group(string key){if(key.StartsWith("codex:"))return key.Substring(0,key.LastIndexOf(':'));if(key.StartsWith("high:")||key.StartsWith("exit:"))return "process:"+key.Substring(key.IndexOf(':')+1);if(key.StartsWith("network:"))return "network";return key;}
 public static BubblePlacement Place(double x,double head,double feet,double w,double h,double l,double t,double r,double b){
  bool above=head-h-8>=t+8;double y=above?head-h-8:feet+8;
  return new BubblePlacement{X=Math.Max(l+8,Math.Min(r-w-8,x-w/2)),Y=Math.Max(t+8,Math.Min(b-h-8,y)),Above=above};
 }
}
public class BubbleQueue {
 readonly List<BubbleMessage> pending=new List<BubbleMessage>();
 public BubbleMessage Current{get;private set;}public double Until{get;private set;}double lastTick;
 public int Pending{get{return pending.Count;}}
 void Start(BubbleMessage m,double now){Current=m;Until=m==null?0:now+BubbleLogic.Duration(m.Text,m.Notification);lastTick=now;}
 public void Add(BubbleMessage m,double now){
  if(m.StillValid!=null&&!m.StillValid())return;
  m.Key=BubbleLogic.Group(m.Key);
  if(Current==null||!Current.Notification||Current.Key==m.Key){Start(m,now);return;}
  if(!m.Notification)return;
  int i=pending.FindIndex(x=>x.Key==m.Key);if(i>=0){pending[i]=m;return;}
  if(pending.Count==8)pending.RemoveAt(0);pending.Add(m);
 }
 public void Tick(double now,bool hover){pending.RemoveAll(m=>m.StillValid!=null&&!m.StillValid());if(Current!=null&&Current.StillValid!=null&&!Current.StillValid())Dismiss(now);if(Current!=null&&hover)Until+=Math.Max(0,now-lastTick);lastTick=now;if(Current!=null&&now>=Until)Dismiss(now);}
 public void Dismiss(double now){var next=pending.Count==0?null:pending[0];if(next!=null)pending.RemoveAt(0);Start(next,now);}
 public void ClearCasual(double now){if(Current!=null&&!Current.Notification)Dismiss(now);}
 public void Clear(){pending.Clear();Current=null;Until=0;}
}
