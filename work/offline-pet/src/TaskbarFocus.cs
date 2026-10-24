using System;
using System.Runtime.InteropServices;
using System.Text;
using Forms=System.Windows.Forms;
sealed partial class TaskbarMonitor {
 delegate void ShellEvent(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int childId,uint thread,uint time);
 [DllImport("user32.dll")]static extern IntPtr SetWinEventHook(uint first,uint last,IntPtr module,ShellEvent callback,uint process,uint thread,uint flags);
 [DllImport("user32.dll")]static extern bool UnhookWinEvent(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr hwnd,uint kind);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int size);
 ShellEvent shellEvent;IntPtr foregroundHook,reorderHook;bool shellRefreshQueued;
 internal bool ShellHooksActive {get{return foregroundHook!=IntPtr.Zero&&reorderHook!=IntPtr.Zero;}}
 internal static bool IsShellTaskbar(IntPtr hwnd){if(hwnd==IntPtr.Zero)return false;var name=new StringBuilder(128);GetClassName(hwnd,name,name.Capacity);if(name.ToString()=="Shell_TrayWnd"||name.ToString()=="Shell_SecondaryTrayWnd")return true;var root=GetAncestor(hwnd,2);if(root==hwnd||root==IntPtr.Zero)return false;name.Clear();GetClassName(root,name,name.Capacity);return name.ToString()=="Shell_TrayWnd"||name.ToString()=="Shell_SecondaryTrayWnd";}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);shellEvent=OnShellEvent;foregroundHook=SetWinEventHook(3,3,IntPtr.Zero,shellEvent,0,0,2);reorderHook=SetWinEventHook(0x8004,0x8004,IntPtr.Zero,shellEvent,0,0,2);}
 protected override void OnHandleDestroyed(EventArgs e){ReleaseShellHooks();base.OnHandleDestroyed(e);}
 void ReleaseShellHooks(){if(foregroundHook!=IntPtr.Zero)UnhookWinEvent(foregroundHook);if(reorderHook!=IntPtr.Zero)UnhookWinEvent(reorderHook);foregroundHook=reorderHook=IntPtr.Zero;shellRefreshQueued=false;}
 void OnShellEvent(IntPtr hook,uint kind,IntPtr hwnd,int objectId,int childId,uint thread,uint time){if(kind!=3&&kind!=0x8004||!IsShellTaskbar(hwnd)||kind==0x8004&&!IsShellTaskbar(GetForegroundWindow()))return;QueueShellRefresh();}
 internal void QueueShellRefresh(){if(IsDisposed||Disposing||!IsHandleCreated||Config==null||!Config.Taskbar||ContextMenuOpen||positionDragging||shellRefreshQueued)return;shellRefreshQueued=true;try{BeginInvoke(new Forms.MethodInvoker(()=>{shellRefreshQueued=false;if(IsDisposed||Disposing||Config==null||!Config.Taskbar||ContextMenuOpen||positionDragging)return;ApplyAppearance();Position();RestoreShellLayer();}));}catch(InvalidOperationException){shellRefreshQueued=false;}}
 internal void RestoreShellLayer(){if(IsDisposed||Disposing||!IsHandleCreated||Config==null||!Config.Taskbar||ContextMenuOpen||positionDragging||!IsWindowVisible(Handle))return;
  // Reapply the z-order, not just WS_EX_TOPMOST: Explorer can raise its own topmost surface.
  const uint flags=0x1|0x2|0x8|0x10;SetWindowPos(Handle,new IntPtr(-2),0,0,0,0,flags);SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,flags);Invalidate();Update();RenderLayeredFrame();
 }
}
