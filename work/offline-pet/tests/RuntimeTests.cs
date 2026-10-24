using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Web.Script.Serialization;
class RuntimeTests {
 static int failed;static void Check(string n,bool pass){Console.WriteLine((pass?"PASS ":"FAIL ")+n);if(!pass)failed++;}
 static int Main(string[] args){
  string home=args[0];Directory.CreateDirectory(home);Environment.SetEnvironmentVariable("CONAN_PET_TEST_MODE","1");Environment.SetEnvironmentVariable("CONAN_PET_EVENT_DIR",Path.Combine(home,"events"));
  var r=new AssistantRuntime(home);var first=r.Poll();
  Check("real Windows memory and CPU snapshot available",first.SystemText.Contains("GiB")&&first.SystemText.Contains("CPU"));
  Check("network snapshot is labeled even without interfaces",!String.IsNullOrWhiteSpace(first.NetworkText));
  Check("empty inbox does not claim hooks are uninstalled",first.CodexText.Contains("不等于未安装")&&!first.CodexText.Contains("请在接入设置中安装钩子"));
  string target=Path.Combine(home,"shortcut with spaces.txt");File.WriteAllText(target,"test");
  r.Config.Shortcuts.Clear();r.Config.Shortcuts.Add(new Shortcut{Name="测试文件",Path=target});r.Save();
  var loaded=new AssistantRuntime(home);Check("Unicode shortcuts persist without shell quoting",loaded.Config.Shortcuts.Count==1&&loaded.Config.Shortcuts[0].Path==target);
  var json=new JavaScriptSerializer();
  File.WriteAllText(Path.Combine(home,"events","conan-event-0123456789abcdef0123456789abcdef.json"),json.Serialize(new{protocol="conan-event-v1",@event="PermissionRequest",session="integration",utc=DateTime.UtcNow.ToString("o"),prompt="PRIVATE_TEST_MARKER"}));
  var waiting=r.Poll();Check("real inbox event becomes waiting state",waiting.CodexRow==6&&waiting.CodexText.Contains("等待你确认"));
  Check("notifications exclude prompt content",!String.Join(" ",waiting.Notices).Contains("PRIVATE_TEST_MARKER"));
  Check("processed event file is consumed",!File.Exists(Path.Combine(home,"events","conan-event-0123456789abcdef0123456789abcdef.json")));
  using(var p=Process.Start(new ProcessStartInfo("powershell.exe","-NoProfile -Command Start-Sleep -Milliseconds 1400"){UseShellExecute=false,CreateNoWindow=true})){
   int pid=p.Id;r.Watch(Process.GetProcessById(pid));var active=r.Poll();Check("selected real process is monitored",active.Watches.Any(w=>w.Id==pid&&!w.Ended));
   p.WaitForExit(6000);var ended=r.Poll();Check("natural process exit produces a notification",ended.Watches.Any(w=>w.Id==pid&&w.Ended)&&ended.Notices.Any(n=>n.Contains("已退出")));
   var again=r.Poll();Check("same exit does not flood notifications",again.Notices.Length==ended.Notices.Length);
  }
  File.WriteAllText(Path.Combine(home,"events","malformed.json"),"{bad json");
  r.Poll();Check("malformed bridge event cannot stop monitoring",true);
  Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
