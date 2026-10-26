using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

public static class CodexDesktopLauncher {
 public static bool IsRunning(){foreach(string name in new[]{"Codex","OpenAI.Codex"})foreach(var process in Process.GetProcessesByName(name)){using(process){try{if(process.MainWindowHandle!=IntPtr.Zero)return true;}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}}return false;}
 public static string FindShortcut(){foreach(string root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)}){if(String.IsNullOrEmpty(root)||!Directory.Exists(root))continue;try{var match=Directory.GetFiles(Path.Combine(root,"Programs"),"*.lnk",SearchOption.AllDirectories).Where(path=>Path.GetFileNameWithoutExtension(path).IndexOf("codex",StringComparison.OrdinalIgnoreCase)>=0).OrderBy(path=>Path.GetFileNameWithoutExtension(path).Equals("Codex",StringComparison.OrdinalIgnoreCase)?0:1).FirstOrDefault();if(match!=null)return match;}catch(IOException){}catch(UnauthorizedAccessException){}}
  string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);foreach(string path in new[]{Path.Combine(local,"Programs","Codex","Codex.exe"),Path.Combine(local,"Programs","OpenAI Codex","Codex.exe")})if(File.Exists(path))return path;return null;
 }
 public static void Start(){if(IsRunning())return;string target=FindShortcut();if(String.IsNullOrEmpty(target))throw new IOException("未找到 Codex 桌面版快捷方式。请从开始菜单手动打开 Codex；额度 CLI 查询仍可独立使用。");Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}
 public static bool WaitForMainWindow(TimeSpan timeout){var until=DateTime.UtcNow+timeout;while(DateTime.UtcNow<until){if(IsRunning())return true;Thread.Sleep(250);}return IsRunning();}
}
