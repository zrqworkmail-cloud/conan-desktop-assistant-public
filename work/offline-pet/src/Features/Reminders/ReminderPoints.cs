using System;
using System.Linq;
using System.Collections.Generic;
public static class ReminderPoints {
 public static readonly int[] WorkDefaults={15,10,8,6,5,4,3,2,1};
 public static List<int> Parse(string text){var parts=(text??"").Split(new[]{',','，',';','；',' ','\r','\n'},StringSplitOptions.RemoveEmptyEntries);var result=new List<int>();foreach(var part in parts){int n;if(!Int32.TryParse(part,out n)||n<0||n>1440)throw new ArgumentException("提醒点请输入0–1440的整数分钟，0代表到点提醒。");result.Add(n);}result=result.Distinct().OrderByDescending(n=>n).ToList();if(result.Count>30)throw new ArgumentException("最多设置30个提醒点。");return result;}
 public static string Format(IEnumerable<int> points){return String.Join("、",points.OrderByDescending(x=>x).Select(x=>x==0?"到点":x+"分钟前"));}
}
