using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
class SkillRuntimeTests {
 static int failed;static void Check(string n,bool ok){Console.WriteLine((ok?"PASS ":"FAIL ")+n);if(!ok)failed++;}
 [STAThread]static int Main(string[] args){
  string home=args[0];Directory.CreateDirectory(home);var store=new SkillRuntime(home);DateTime now=DateTime.UtcNow;
  store.Engine.StartFocus(now);var reminder=store.Engine.AddCountdown("持久提醒",2,now);store.Engine.SaveNote(null,"中文便签","测试正文 😀");store.Save();
  var restored=new SkillRuntime(home);Check("focus state survives restart",restored.Engine.Data.Focus.Running&&Math.Abs((restored.Engine.Data.Focus.EndUtc-store.Engine.Data.Focus.EndUtc).TotalMilliseconds)<1&&restored.Engine.Data.Focus.EndUtc.Kind==DateTimeKind.Utc);
  Check("reminder deadline survives restart",Math.Abs((restored.Engine.Data.Reminders.Single().DueUtc-reminder.DueUtc).TotalMilliseconds)<1);
  Check("Unicode note survives restart",restored.Engine.Data.Notes.Single().Text=="测试正文 😀");
  byte[] bytes=File.ReadAllBytes(Path.Combine(home,"skills.json"));Check("skills JSON is written without BOM",!(bytes[0]==239&&bytes[1]==187&&bytes[2]==191));
  var alerts=restored.Tick(now.AddMinutes(26));Check("restart reconciles overdue reminder once",alerts.Any(a=>a.Key=="timer:"+reminder.Id)&&restored.Tick(now.AddMinutes(26)).Count==0);
  var next=new SkillRuntime(home);Check("completed reminder persists",!next.Engine.Data.Reminders.Single().Enabled);
  string bad=Path.Combine(home,"bad");Directory.CreateDirectory(bad);File.WriteAllText(Path.Combine(bad,"skills.json"),"{invalid");var invalid=new SkillRuntime(bad);bool blocked=false;try{invalid.Save();}catch(IOException){blocked=true;}Check("corrupt file is preserved instead of overwritten",blocked&&File.ReadAllText(Path.Combine(bad,"skills.json"))=="{invalid");
  Check("desktop action rejects arbitrary external URLs",!DesktopActions.OpenUri("https://example.com"));
  Check("desktop action rejects file URI",!DesktopActions.OpenUri("file:///C:/Windows/System32/cmd.exe"));
  using(var process=Process.GetCurrentProcess())Check("PID reuse guard rejects different process start",!DesktopActions.FocusProcess(process.Id,DateTime.MinValue));
  using(var form=new Form{Text="Conan action verification",Width=320,Height=110,ShowInTaskbar=true}){
   form.Show();Application.DoEvents();using(var process=Process.GetCurrentProcess()){Check("real process window can be brought forward",DesktopActions.FocusProcess(process.Id,process.StartTime.ToUniversalTime()));form.WindowState=FormWindowState.Minimized;Application.DoEvents();bool focused=DesktopActions.FocusProcess(process.Id,process.StartTime.ToUniversalTime());Application.DoEvents();Check("minimized application window is restored",focused&&form.WindowState==FormWindowState.Normal);}form.Close();
  }
  Console.WriteLine("Protocol registration: codex="+DesktopActions.Registered("codex")+", settings="+DesktopActions.Registered("ms-settings")+", screenshot="+DesktopActions.Registered("ms-screenclip"));
  Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
