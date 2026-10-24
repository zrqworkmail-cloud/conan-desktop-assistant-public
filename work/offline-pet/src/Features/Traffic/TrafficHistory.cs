using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
public class TrafficPoint {public DateTime Date;public bool Recorded;public double Download,Upload;}
public static class TrafficHistory {
 public static DateTime Start(DateTime date,bool month){return month?new DateTime(date.Year,date.Month,1):date.Date.AddDays(-((int)date.DayOfWeek+6)%7);}
 public static TrafficPoint[] Range(IEnumerable<TrafficDay> days,DateTime date,bool month){var start=Start(date,month);int count=month?DateTime.DaysInMonth(start.Year,start.Month):7;return Enumerable.Range(0,count).Select(i=>{var d=start.AddDays(i);var found=days.Where(x=>x!=null&&x.Day==d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)).ToArray();return new TrafficPoint{Date=d,Recorded=found.Length>0,Download=found.Sum(x=>Math.Max(0,x.Download)),Upload=found.Sum(x=>Math.Max(0,x.Upload))};}).ToArray();}
}
