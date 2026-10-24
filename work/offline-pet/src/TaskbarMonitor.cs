using System;
using System.Linq;
using System.Runtime.InteropServices;
using Forms=System.Windows.Forms;
using Drawing=System.Drawing;
// Taskbar placement follows the Win11 strategy documented by TrafficMonitor.
sealed partial class TaskbarMonitor:Forms.Form {
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string cls,string title);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
   [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int cmd);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr hwnd);
 bool contextMenuOpen;
 internal bool ContextMenuOpen{get{return contextMenuOpen||ContextMenuStrip!=null&&ContextMenuStrip.Visible;}}
 readonly Forms.ToolTip readingsTip=new Forms.ToolTip();
 public string ScreenOverride;internal string DisplayScreen{get{return ScreenOverride??(Config==null?null:Config.TaskbarScreen);}}
 public SystemSample Sample;public MonitorConfig Config;bool sampleWasStale;
 internal bool SampleIsStale{get{return Sample!=null&&Sample.Utc!=default(DateTime)&&DateTime.UtcNow-Sample.Utc>TimeSpan.FromMilliseconds(Math.Max(5000,(Config==null?1000:Config.RefreshMilliseconds)*3));}}
 internal SystemSample DisplaySample{get{return SampleIsStale?null:Sample;}}public string Status="任务栏显示已关闭";public event Action OpenCard;public event Action OpenQuota;
 protected override bool ShowWithoutActivation {get{return true;}}
 protected override Forms.CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80|0x08000000|0x80000;return p;}}
 public TaskbarMonitor(){FormBorderStyle=Forms.FormBorderStyle.None;ShowInTaskbar=false;StartPosition=Forms.FormStartPosition.Manual;TopMost=true;Name="ConanTaskbarMonitor";Text="柯南 · 任务栏监控";SetStyle(Forms.ControlStyles.UserPaint|Forms.ControlStyles.AllPaintingInWmPaint|Forms.ControlStyles.OptimizedDoubleBuffer,true);BackColor=Drawing.Color.FromArgb(24,31,48);ForeColor=Drawing.Color.FromArgb(205,225,249);Font=new Drawing.Font("Segoe UI",9);Cursor=Forms.Cursors.Hand;MouseDown+=(s,e)=>{if(e.Button==Forms.MouseButtons.Left)BeginPositionDrag(PointToScreen(e.Location));};MouseMove+=(s,e)=>MovePositionDrag(PointToScreen(e.Location));MouseUp+=(s,e)=>{if(e.Button!=Forms.MouseButtons.Left)return;bool moved=EndPositionDrag();if(!moved&&OpenCard!=null)OpenCard();};MouseCaptureChanged+=(s,e)=>{if(!Capture&&positionDragging)EndPositionDrag();};var menu=new Forms.ContextMenuStrip();menu.Items.Add("打开监控卡片",null,(s,e)=>{if(OpenCard!=null)OpenCard();});menu.Items.Add("Codex额度与授权",null,(s,e)=>{if(OpenQuota!=null)OpenQuota();});menu.Items.Add("任务栏外观设置",null,(s,e)=>{if(OpenAppearance!=null)OpenAppearance();});var lockItem=new Forms.ToolStripMenuItem("锁定任务栏位置"){CheckOnClick=false};lockItem.Click+=(s,e)=>{if(Config==null)return;Config.TaskbarPositionLocked=!Config.TaskbarPositionLocked;if(PositionChanged!=null)PositionChanged();};menu.Items.Add(lockItem);menu.Items.Add("关闭任务栏显示",null,(s,e)=>{if(Disable!=null)Disable();});menu.Opening+=(s,e)=>{lockItem.Checked=Config==null||Config.TaskbarPositionLocked;contextMenuOpen=true;readingsTip.Active=false;};menu.Closed+=(s,e)=>{contextMenuOpen=false;if(!IsDisposed&&!Disposing){readingsTip.Active=true;QueueShellRefresh();}};ContextMenuStrip=menu;}
 public event Action Disable,OpenAppearance;
 public void Update(SystemSample sample,MonitorConfig config){Sample=sample;Config=config;if(!ContextMenuOpen&&!positionDragging){ApplyAppearance();Position();}else if(Config==null||!Config.Taskbar)Position();Invalidate();if(ContextMenuOpen||positionDragging)RenderLayeredFrame();if(!ContextMenuOpen)readingsTip.SetToolTip(this,(sample==null?"等待采样":"采样于 "+sample.Utc.ToLocalTime().ToString("HH:mm:ss.fff"))+" · "+config.RefreshMilliseconds+"ms"+(SampleIsStale?" · 数据已过期，等待更新":"")+(sample==null?"":"\n磁盘："+sample.DiskState+"\nCPU温度："+sample.TemperatureState)+(sample==null||sample.Quota==null?"":"\n"+sample.Quota.State+String.Join("",sample.Quota.Windows.Select(w=>"\nCodex "+(w.Minutes==300?"5h":"周")+" · 重置 "+w.ResetUtc.ToLocalTime().ToString("MM-dd HH:mm")+" · 还剩 "+CodexQuotaReader.Countdown(sample.Quota,w.Minutes,DateTime.UtcNow)))));}
 TaskbarGeometry geometry;double renderDpi=1;
 void ApplyAppearance(){/* Per-pixel composition keeps text and bars independent of background alpha. */}
 public static string ScreenLabel(Forms.Screen s){return (s.Primary?"主屏 · ":"副屏 · ")+s.DeviceName+" · "+s.Bounds.Width+"×"+s.Bounds.Height;}
 public static IntPtr TaskbarFor(string device,out string note){note="";var screen=String.IsNullOrEmpty(device)?Forms.Screen.PrimaryScreen:Forms.Screen.AllScreens.FirstOrDefault(s=>s.DeviceName==device);if(screen==null){screen=Forms.Screen.PrimaryScreen;note=" · 所选屏幕未连接，暂用主屏";}var main=FindWindow("Shell_TrayWnd",null);if(main!=IntPtr.Zero&&Forms.Screen.FromHandle(main).DeviceName==screen.DeviceName)return main;IntPtr next=IntPtr.Zero;while((next=FindWindowEx(IntPtr.Zero,next,"Shell_SecondaryTrayWnd",null))!=IntPtr.Zero)if(Forms.Screen.FromHandle(next).DeviceName==screen.DeviceName)return next;note="该屏幕没有任务栏，请在 Windows 多显示器任务栏设置中启用";return IntPtr.Zero;}
 public static int HeightInDips(string device=null){string note;var taskbar=TaskbarFor(device,out note);Rect area;if(taskbar==IntPtr.Zero||!GetWindowRect(taskbar,out area))return 48;double dpi=1;try{uint value=GetDpiForWindow(taskbar);if(value>=72)dpi=value/96.0;}catch(EntryPointNotFoundException){}return Math.Max(12,(int)((area.Bottom-area.Top)/dpi));}
 public void Position(){
  bool stale=SampleIsStale;if(stale!=sampleWasStale){sampleWasStale=stale;Invalidate();}
  if(Config==null||!Config.Taskbar){positionDragging=positionPressed=false;Capture=false;if(ContextMenuStrip!=null&&ContextMenuStrip.Visible)ContextMenuStrip.Close();if(IsHandleCreated)ShowWindow(Handle,0);Status="任务栏显示已关闭";return;}
  if(ContextMenuOpen||positionDragging)return;
  if(Config.TaskbarItems.Any(k=>k=="codex5h"||k=="codex7d"))Invalidate();
  string screenNote;IntPtr taskbar=TaskbarFor(DisplayScreen,out screenNote);Rect area;if(taskbar==IntPtr.Zero||!GetWindowRect(taskbar,out area)||!IsWindowVisible(taskbar)||DesktopActions.FullScreenForeground()){if(IsHandleCreated)ShowWindow(Handle,0);Status=taskbar==IntPtr.Zero?screenNote:"任务栏隐藏或全屏，暂停显示";return;}
  var screen=Forms.Screen.FromHandle(taskbar).Bounds;int visibleHeight=Math.Min(area.Bottom,screen.Bottom)-Math.Max(area.Top,screen.Top);if(visibleHeight<12){if(IsHandleCreated)ShowWindow(Handle,0);Status="任务栏自动隐藏，暂停显示";return;}
  if(area.Bottom-area.Top>area.Right-area.Left){if(IsHandleCreated)ShowWindow(Handle,0);Status="当前为竖向任务栏，请使用监控卡片";return;}
  if(!IsHandleCreated){var created=Handle;}
  double dpi=1;try{uint value=GetDpiForWindow(taskbar);if(value>=72)dpi=value/96.0;}catch(EntryPointNotFoundException){}
  renderDpi=dpi;geometry=TaskbarPainter.Geometry(Config,dpi,area.Bottom-area.Top,DisplaySample,Math.Max(100,area.Right-area.Left-100));int width=Math.Min(geometry.Size.Width,Math.Max(100,area.Right-area.Left-100)),height=Math.Min(area.Bottom-area.Top,geometry.Size.Height);

  int x=Math.Max(0,Config.TaskbarOffset);if(Config.TaskbarSide=="right"){Rect tray;var notify=FindWindowEx(taskbar,IntPtr.Zero,"TrayNotifyWnd",null);int right=notify!=IntPtr.Zero&&GetWindowRect(notify,out tray)?tray.Left-area.Left:area.Right-area.Left-(int)(180*dpi);x=right-width-x;}x=Math.Max(0,Math.Min(area.Right-area.Left-width,x));
  Size=new Drawing.Size(width,height);if(!Visible)Show();if(!SetWindowPos(Handle,new IntPtr(-1),area.Left+x,area.Top+Math.Max(0,Math.Min(area.Bottom-area.Top-height,(area.Bottom-area.Top-height)/2+(int)(Config.Appearance.OffsetY*dpi))),width,height,0x10|0x40)){Status="任务栏定位失败："+Marshal.GetLastWin32Error();return;}RenderLayeredFrame();if(IsShellTaskbar(GetForegroundWindow()))RestoreShellLayer();Status="已在 "+Forms.Screen.FromHandle(taskbar).DeviceName+" 显示（兼容浮层）· "+(Config.TaskbarPositionLocked?"位置已锁定 · 点击查看详情":"可拖动位置 · 点击查看详情")+screenNote+(geometry.Rows!=Config.Appearance.Rows?" · 高度不足，已使用单行":"")+(geometry.RenderDpi<dpi?" · 已自适应缩放":"")+(geometry.Size.Width>width||geometry.Size.Height>height?" · 超出可用范围，请减少项目或间距":"")+(SampleIsStale?" · 数据已过期，等待更新":"");
 }
 void RenderLayeredFrame(){if(Config==null||!IsHandleCreated||!IsWindowVisible(Handle)||ClientSize.Width<1||ClientSize.Height<1)return;if(geometry==null)geometry=TaskbarPainter.Geometry(Config,renderDpi,Height,DisplaySample);using(var image=TaskbarPainter.Surface(Config,DisplaySample,geometry,ClientSize)){if(!PresentLayered(image))Status="任务栏透明合成失败："+LastLayeredError;}}
 protected override void OnPaint(Forms.PaintEventArgs e){base.OnPaint(e);RenderLayeredFrame();}
 protected override void Dispose(bool disposing){if(disposing){ReleaseShellHooks();readingsTip.Dispose();if(ContextMenuStrip!=null)ContextMenuStrip.Dispose();}base.Dispose(disposing);}
}
