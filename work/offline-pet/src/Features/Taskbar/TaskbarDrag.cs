using System;
using Drawing=System.Drawing;
using Forms=System.Windows.Forms;
sealed partial class TaskbarMonitor {
 bool positionPressed,positionDragging;Drawing.Point positionStart,positionOrigin;Rect dragArea;
 public event Action PositionChanged;
 internal void BeginPositionDrag(Drawing.Point point){if(Config==null||Config.TaskbarPositionLocked||!Config.Taskbar||ContextMenuOpen)return;string note;var bar=TaskbarFor(DisplayScreen,out note);if(bar==IntPtr.Zero||!GetWindowRect(bar,out dragArea))return;positionPressed=true;positionStart=point;positionOrigin=Location;Capture=true;}
 internal void MovePositionDrag(Drawing.Point point){if(!positionPressed||Config==null)return;if(Config.TaskbarPositionLocked){EndPositionDrag();return;}int dx=point.X-positionStart.X,dy=point.Y-positionStart.Y;if(!positionDragging&&Math.Abs(dx)<Forms.SystemInformation.DragSize.Width&&Math.Abs(dy)<Forms.SystemInformation.DragSize.Height)return;positionDragging=true;int x=dragArea.Left+Math.Max(0,Math.Min(Math.Min(2000,dragArea.Right-dragArea.Left-Width),positionOrigin.X+dx-dragArea.Left));int y=Math.Max(dragArea.Top,Math.Min(dragArea.Bottom-Height,positionOrigin.Y+dy));SetWindowPos(Handle,new IntPtr(-1),x,y,Width,Height,0x10);}
 internal bool EndPositionDrag(){bool moved=positionDragging;positionDragging=positionPressed=false;Capture=false;if(moved&&Config!=null){Config.TaskbarSide="left";Config.TaskbarOffset=Math.Max(0,Math.Min(2000,Left-dragArea.Left));Config.Appearance.OffsetY=Math.Max(-30,Math.Min(30,(int)Math.Round((Top-dragArea.Top-(dragArea.Bottom-dragArea.Top-Height)/2.0)/renderDpi)));if(PositionChanged!=null)PositionChanged();Position();}return moved;}
}
