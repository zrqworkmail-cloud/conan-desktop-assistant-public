using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
sealed class TaskbarInsertionAdorner:Adorner {
 public bool After;public int Index;public TaskbarInsertionAdorner(UIElement target):base(target){IsHitTestVisible=false;}
 protected override void OnRender(DrawingContext dc){double width=AdornedElement.RenderSize.Width,y=After?AdornedElement.RenderSize.Height:0;var color=new SolidColorBrush(Color.FromRgb(96,214,238));dc.DrawLine(new Pen(color,3),new Point(0,y),new Point(width,y));dc.DrawEllipse(color,null,new Point(4,y),4,4);string label="插入此处 · 第 "+(Index+1)+" 位";var text=new FormattedText(label,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Microsoft YaHei UI"),11,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);double x=Math.Max(12,width-text.Width-20),top=After?y-24:y+2;dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(245,20,61,83)),null,new Rect(x-6,top-2,text.Width+12,22),5,5);dc.DrawText(text,new Point(x,top));}
}
public static class TaskbarOrderLogic {
 public static int Target(int from,int target,bool after,int count){if(from<0||from>=count||target<0||target>=count)return -1;int index=target+(after?1:0);if(from<index)index--;return Math.Max(0,Math.Min(count-1,index));}
}
