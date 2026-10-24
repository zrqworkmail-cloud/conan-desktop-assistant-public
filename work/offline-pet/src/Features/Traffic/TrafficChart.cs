using System;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
class TrafficChart:FrameworkElement {
 public TrafficPoint[] Points=new TrafficPoint[0];public bool Lines,Bars=true,ShowDownload=true,ShowUpload=true;public TrafficChart(){Height=260;MinWidth=600;MouseMove+=Hover;}
 void Hover(object sender,MouseEventArgs e){if(Points.Length==0)return;double slot=(ActualWidth-70)/Points.Length;int i=Math.Max(0,Math.Min(Points.Length-1,(int)((e.GetPosition(this).X-56)/slot)));var p=Points[i];ToolTip=p.Date.ToString("yyyy-MM-dd")+(p.Recorded?"\n下载 "+Size(p.Download)+"\n上传 "+Size(p.Upload):"\n无采样记录");}
 static string Size(double bytes){return bytes>=1073741824?(bytes/1073741824).ToString("F2")+" GiB":(bytes/1048576).ToString("F1")+" MiB";}
 void Label(DrawingContext dc,string text,double x,double y,Brush brush){dc.DrawText(new FormattedText(text,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),10,brush,VisualTreeHelper.GetDpi(this).PixelsPerDip),new Point(x,y));}
 protected override void OnRender(DrawingContext dc){base.OnRender(dc);double width=ActualWidth;if(width<100||Points.Length==0)return;double max=Math.Max(1048576,Points.Max(p=>Math.Max(p.Download,p.Upload))),slot=(width-70)/Points.Length;var ink=TryFindResource("DeskMuted") as Brush??Brushes.SlateGray;var grid=TryFindResource("DeskLine") as Brush??Brushes.Gray;Brush down=VisualEffects.Paint(Color.FromRgb(69,174,214),false),up=VisualEffects.Paint(Color.FromRgb(155,128,225),false);double top=25,height=180;
  for(int i=0;i<=4;i++){double y=top+height*i/4;dc.DrawLine(new Pen(grid,.5),new Point(54,y),new Point(width-10,y));Label(dc,(max*(4-i)/4/1048576).ToString("F0")+" M",2,y-7,ink);}
  for(int i=0;i<Points.Length;i++){var p=Points[i];double x=56+slot*(i+.5);Label(dc,p.Date.ToString("MM/dd"),x-14,214,ink);if(!p.Recorded){Label(dc,"—",x-4,top+height-17,ink);continue;}double d=p.Download/max*height,u=p.Upload/max*height;if(Bars){double bar=Math.Min(13,slot*.3);if(ShowDownload)dc.DrawRoundedRectangle(down,null,new Rect(x-bar-1,top+height-d,bar,Math.Max(1,d)),2,2);if(ShowUpload)dc.DrawRoundedRectangle(up,null,new Rect(x+1,top+height-u,bar,Math.Max(1,u)),2,2);}if(Lines){for(int series=0;series<2;series++){if(series==0&&!ShowDownload||series==1&&!ShowUpload)continue;var brush=series==0?down:up;double y=top+height-(series==0?p.Download:p.Upload)/max*height;if(i>0&&Points[i-1].Recorded){var previous=Points[i-1];double py=top+height-(series==0?previous.Download:previous.Upload)/max*height;dc.DrawLine(new Pen(brush,2),new Point(x-slot,py),new Point(x,y));}dc.DrawEllipse(brush,null,new Point(x,y),3,3);}}}
  Label(dc,"↓ 下载",56,243,down);Label(dc,"↑ 上传",140,243,up);Label(dc,"— 无记录（折线在缺失处断开）",230,243,ink);
 }
}
