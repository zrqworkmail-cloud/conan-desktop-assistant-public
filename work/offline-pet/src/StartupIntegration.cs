using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
// Only explicit UI actions write startup registration or a desktop shortcut.
public sealed class StartupIntegration {
 public const string ValueName="ConanDesktopAssistant";
 readonly string executable,registryPath,desktop;
 const string Marker="ConanDesktopAssistant desktop shortcut";
 public StartupIntegration(string exe):this(exe,@"Software\Microsoft\Windows\CurrentVersion\Run",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)){}
 internal StartupIntegration(string exe,string key,string folder){executable=Path.GetFullPath(exe);registryPath=key;desktop=folder;}
 public string Command{get{return "\""+executable+"\"";}}
 public string RegisteredCommand{get{using(var key=Registry.CurrentUser.OpenSubKey(registryPath))return key==null?"":Convert.ToString(key.GetValue(ValueName,""));}}
 public bool Enabled{get{return RegisteredCommand.Length>0;}}
 public bool CurrentVersion{get{return String.Equals(Command,RegisteredCommand,StringComparison.OrdinalIgnoreCase);}}
 public void SetEnabled(bool enabled){using(var key=Registry.CurrentUser.CreateSubKey(registryPath)){if(enabled){if(!File.Exists(executable))throw new FileNotFoundException("当前桌宠程序不存在。",executable);key.SetValue(ValueName,Command,RegistryValueKind.String);}else key.DeleteValue(ValueName,false);}}
 public string ShortcutPath{get{return Path.Combine(desktop,"柯南桌面助手.lnk");}}
 static object Call(object target,string name,params object[] args){return target.GetType().InvokeMember(name,System.Reflection.BindingFlags.InvokeMethod,null,target,args);}
 static object Get(object target,string name){return target.GetType().InvokeMember(name,System.Reflection.BindingFlags.GetProperty,null,target,null);}
 static void Set(object target,string name,object value){target.GetType().InvokeMember(name,System.Reflection.BindingFlags.SetProperty,null,target,new[]{value});}
 public void CreateShortcut(){if(!File.Exists(executable))throw new FileNotFoundException("当前桌宠程序不存在。",executable);Directory.CreateDirectory(desktop);object shell=null,shortcut=null;try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell",true));shortcut=Call(shell,"CreateShortcut",ShortcutPath);if(File.Exists(ShortcutPath)&&!String.Equals(Convert.ToString(Get(shortcut,"Description")),Marker,StringComparison.Ordinal))throw new IOException("桌面已有同名的其他快捷方式，请先重命名，避免覆盖。");Set(shortcut,"TargetPath",executable);Set(shortcut,"WorkingDirectory",Path.GetDirectoryName(executable));Set(shortcut,"Description",Marker);string icon=Path.Combine(Path.GetDirectoryName(executable),"conan.ico");Set(shortcut,"IconLocation",File.Exists(icon)?icon:executable);Call(shortcut,"Save");}finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);if(shell!=null)Marshal.FinalReleaseComObject(shell);}}
}
