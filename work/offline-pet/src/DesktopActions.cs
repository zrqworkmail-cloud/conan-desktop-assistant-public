using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
public static class DesktopActions {
 [StructLayout(LayoutKind.Sequential)]struct WindowRect{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out WindowRect rect);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,System.Text.StringBuilder text,int count);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint processId);
 static readonly uint ownProcessId=(uint)Process.GetCurrentProcess().Id;
 static DateTime fullScreenSample;static bool fullScreen;
 public static bool FullScreenForeground(){
  var h=GetForegroundWindow();uint processId;GetWindowThreadProcessId(h,out processId);if(processId==ownProcessId)return false;
  if((DateTime.UtcNow-fullScreenSample).TotalSeconds<1)return fullScreen;fullScreenSample=DateTime.UtcNow;fullScreen=false;if(h==IntPtr.Zero)return false;var cls=new System.Text.StringBuilder(128);GetClassName(h,cls,128);if(cls.ToString()=="Progman"||cls.ToString()=="WorkerW"||cls.ToString()=="Shell_TrayWnd")return false;
  WindowRect r;if(!GetWindowRect(h,out r))return false;var bounds=System.Windows.Forms.Screen.FromHandle(h).Bounds;fullScreen=r.Left<=bounds.Left&&r.Top<=bounds.Top&&r.Right>=bounds.Right&&r.Bottom>=bounds.Bottom;return fullScreen;
 }

 [DllImport("user32.dll")]static extern bool ShowWindowAsync(IntPtr h,int n);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")]static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 public static bool OpenNotificationApp(string appId){if(String.IsNullOrEmpty(appId)||appId.Length>200||!System.Text.RegularExpressions.Regex.IsMatch(appId,@"^[a-zA-Z0-9_.!\-]+$"))return false;try{Process.Start(new ProcessStartInfo("explorer.exe","shell:AppsFolder\\"+appId){UseShellExecute=true});return true;}catch(System.ComponentModel.Win32Exception){return false;}}
 public static bool Registered(string scheme){using(var user=Registry.CurrentUser.OpenSubKey(@"Software\Classes\"+scheme))if(user!=null&&user.GetValue("URL Protocol")!=null)return true;using(var key=Registry.ClassesRoot.OpenSubKey(scheme))return key!=null&&key.GetValue("URL Protocol")!=null;}
 public static bool OpenUri(string uri){Uri value;if(!Uri.TryCreate(uri,UriKind.Absolute,out value))return false;if(value.Scheme!="codex"&&value.Scheme!="ms-settings"&&value.Scheme!="ms-screenclip")return false;if(!Registered(value.Scheme))return false;Process.Start(new ProcessStartInfo(uri){UseShellExecute=true});return true;}
 public static bool FocusProcess(int id,DateTime? expectedStart){
  try{using(var p=Process.GetProcessById(id)){if(expectedStart.HasValue&&p.StartTime.ToUniversalTime()!=expectedStart.Value)return false;p.Refresh();var handle=p.MainWindowHandle;if(handle==IntPtr.Zero)return false;ShowWindowAsync(handle,9);return SetForegroundWindow(handle);}}
  catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch(System.ComponentModel.Win32Exception){return false;}
 }
 public static void Media(string action){
  byte key;switch(action){case "volume-down":key=0xAE;break;case "volume-up":key=0xAF;break;case "mute":key=0xAD;break;case "play-pause":key=0xB3;break;case "previous":key=0xB1;break;case "next":key=0xB0;break;default:throw new ArgumentException("未知媒体操作。");}
  keybd_event(key,0,0,UIntPtr.Zero);keybd_event(key,0,2,UIntPtr.Zero);
 }
}
