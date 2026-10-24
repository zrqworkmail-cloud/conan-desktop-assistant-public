using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
public class PetScreenArea {public Rect Bounds,Work;}
// Positions describe the window's top-left corner. Bridge regions keep the whole
// window on the union of two physically adjacent screens while crossing a seam.
public sealed class DesktopMovement {
 readonly List<Rect> regions=new List<Rect>();readonly int screens;
 List<Point> route;int waypoint;Point requested;
 public DesktopMovement(IEnumerable<PetScreenArea> displays,double width,double height){var list=displays.Where(s=>s.Work.Width>=width&&s.Work.Height>=height).ToArray();foreach(var s in list)regions.Add(new Rect(s.Work.Left,s.Work.Top,s.Work.Width-width,s.Work.Height-height));screens=regions.Count;
  for(int i=0;i<list.Length;i++)for(int j=i+1;j<list.Length;j++){
   var a=list[i];var b=list[j];if(Near(a.Bounds.Right,b.Bounds.Left))Horizontal(a,b,width,height);else if(Near(b.Bounds.Right,a.Bounds.Left))Horizontal(b,a,width,height);
   if(Near(a.Bounds.Bottom,b.Bounds.Top))Vertical(a,b,width,height);else if(Near(b.Bounds.Bottom,a.Bounds.Top))Vertical(b,a,width,height);
  }
 }
 static bool Near(double a,double b){return Math.Abs(a-b)<.01;}
 void Horizontal(PetScreenArea left,PetScreenArea right,double w,double h){double top=Math.Max(left.Work.Top,right.Work.Top),bottom=Math.Min(left.Work.Bottom,right.Work.Bottom)-h;if(bottom>=top)regions.Add(new Rect(left.Work.Right-w,top,right.Work.Left-(left.Work.Right-w),bottom-top));}
 void Vertical(PetScreenArea upper,PetScreenArea lower,double w,double h){double left=Math.Max(upper.Work.Left,lower.Work.Left),right=Math.Min(upper.Work.Right,lower.Work.Right)-w;if(right>=left)regions.Add(new Rect(left,upper.Work.Bottom-h,right-left,lower.Work.Top-(upper.Work.Bottom-h)));}
 static Point Project(Point p,Rect r){return new Point(Math.Max(r.Left,Math.Min(r.Right,p.X)),Math.Max(r.Top,Math.Min(r.Bottom,p.Y)));}
 static bool Touch(Rect a,Rect b){return Math.Max(a.Left,b.Left)<=Math.Min(a.Right,b.Right)+.001&&Math.Max(a.Top,b.Top)<=Math.Min(a.Bottom,b.Bottom)+.001;}
 int Nearest(Point p,int limit){int index=-1;double distance=Double.PositiveInfinity;for(int i=0;i<limit;i++){double d=(Project(p,regions[i])-p).LengthSquared;if(d<distance){index=i;distance=d;}}return index;}
 int[] Connected(Point start,out Point origin){int first=Nearest(start,regions.Count);origin=first<0?start:Project(start,regions[first]);var previous=Enumerable.Repeat(-1,regions.Count).ToArray();if(first<0)return previous;previous[first]=first;var queue=new Queue<int>();queue.Enqueue(first);while(queue.Count>0){int current=queue.Dequeue();for(int i=0;i<regions.Count;i++)if(previous[i]<0&&Touch(regions[current],regions[i])){previous[i]=current;queue.Enqueue(i);}}return previous;}
 public Point ClampToScreen(Point point){int i=Nearest(point,screens);return i<0?point:Project(point,regions[i]);}
 public Rect[] Destinations(Point start){Point origin;var previous=Connected(start,out origin);return regions.Take(screens).Where((r,i)=>previous[i]>=0).ToArray();}
 public void Reset(){route=null;}
 void Plan(Point start,Point goal){requested=goal;route=new List<Point>();waypoint=0;Point origin;var previous=Connected(start,out origin);int end=-1;double nearest=Double.PositiveInfinity;for(int i=0;i<screens;i++){if(previous[i]<0)continue;double d=(Project(goal,regions[i])-goal).LengthSquared;if(d<nearest){nearest=d;end=i;}}if(end<0)return;route.Add(origin);var chain=new List<int>();for(int current=end;;current=previous[current]){chain.Add(current);if(previous[current]==current)break;}chain.Reverse();for(int i=1;i<chain.Count;i++){Rect a=regions[chain[i-1]],b=regions[chain[i]];double left=Math.Max(a.Left,b.Left),top=Math.Max(a.Top,b.Top);route.Add(Project(goal,new Rect(left,top,Math.Max(0,Math.Min(a.Right,b.Right)-left),Math.Max(0,Math.Min(a.Bottom,b.Bottom)-top))));}route.Add(Project(goal,regions[end]));}
 public bool Step(ref double x,ref double y,Point target,double distance){var position=new Point(x,y);if(route==null||(target-requested).LengthSquared>1)Plan(position,target);double remaining=Math.Max(0,distance);while(waypoint<route.Count){Vector delta=route[waypoint]-position;double length=delta.Length;if(length>remaining&&length>.000001){position+=delta/length*remaining;break;}position=route[waypoint++];remaining-=length;}x=position.X;y=position.Y;return waypoint>=route.Count;}
}
