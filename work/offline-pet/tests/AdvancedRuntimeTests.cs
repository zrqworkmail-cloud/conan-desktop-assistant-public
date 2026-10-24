using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
class AdvancedRuntimeTests {
 static int failures;static void Check(string n,bool v){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)failures++;}
 [STAThread]static int Main(string[] args){
  string home=args[0];Directory.CreateDirectory(home);var runtime=new AdvancedRuntime(home);var due=DateTime.UtcNow.AddMinutes(20);var task=runtime.Engine.AddTask("持久化任务 😀",due,2);runtime.Save();
  var restored=new AdvancedRuntime(home);Check("task deadline survives restart in UTC",restored.Engine.Data.Tasks[0].DueUtc.Value.Kind==DateTimeKind.Utc&&Math.Abs((restored.Engine.Data.Tasks[0].DueUtc.Value-due).TotalMilliseconds)<1);
  Check("Unicode task title survives restart",restored.Engine.Data.Tasks[0].Title==task.Title);
  byte[] bytes=File.ReadAllBytes(Path.Combine(home,"advanced-skills.json"));Check("advanced JSON has no BOM",bytes.Length>0&&bytes[0]==123);
  var next=restored.Tick(due.AddMinutes(1));Check("missed task deadline emits once after restart",next.Count==1&&restored.Tick(due.AddMinutes(2)).Count==0);var again=new AdvancedRuntime(home);Check("task notification acknowledgement persists",again.Engine.Data.Tasks[0].Notified);
  string file=Path.Combine(home,"sample.txt");File.WriteAllText(file,"file");runtime.AddFile(file);runtime.AddFile(file);Check("duplicate file watches are ignored",runtime.Engine.Data.Files.Count==1);Check("file watches survive restart",new AdvancedRuntime(home).Engine.Data.Files.Count==1);
  runtime.RemoveFile(runtime.Engine.Data.Files[0].Id);Check("file watch deletion persists",new AdvancedRuntime(home).Engine.Data.Files.Count==0);
  string bad=Path.Combine(home,"bad");Directory.CreateDirectory(bad);File.WriteAllText(Path.Combine(bad,"advanced-skills.json"),"{invalid");var broken=new AdvancedRuntime(bad);bool blocked=false;try{broken.Save();}catch(IOException){blocked=true;}Check("corrupt advanced file is preserved and writes are blocked",blocked&&File.ReadAllText(Path.Combine(bad,"advanced-skills.json"))=="{invalid");Check("corrupt configuration does not enable background capture",broken.Tick(DateTime.UtcNow).Count==0&&!broken.Clipboard.Enabled);
  string empty=Path.Combine(home,"empty");Directory.CreateDirectory(empty);File.WriteAllText(Path.Combine(empty,"advanced-skills.json"),"{}");var migration=new AdvancedRuntime(empty);Check("missing optional fields migrate to safe defaults",migration.LoadError==null&&migration.Engine.Data.Tasks.Count==0&&!migration.Engine.Data.Hotkeys);
  var voiceType=Type.GetTypeFromProgID("SAPI.SpVoice");Check("Windows offline speech engine is installed",voiceType!=null);if(voiceType!=null){object voice=Activator.CreateInstance(voiceType);object voices=voiceType.InvokeMember("GetVoices",BindingFlags.InvokeMethod,null,voice,new object[]{"",""});int count=(int)voices.GetType().InvokeMember("Count",BindingFlags.GetProperty,null,voices,null);Check("offline speech has at least one installed voice",count>0);Marshal.FinalReleaseComObject(voices);Marshal.FinalReleaseComObject(voice);}
  runtime.Dispose();restored.Dispose();again.Dispose();broken.Dispose();migration.Dispose();Console.WriteLine("Failures: "+failures);return failures==0?0:1;
 }
}
