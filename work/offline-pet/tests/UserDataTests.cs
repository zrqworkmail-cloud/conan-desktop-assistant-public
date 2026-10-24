using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
class UserDataTests {
 static int checks,failures;
 static void Check(string name,bool ok){checks++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;}
 static T Find<T>(DependencyObject root,Func<T,bool> match)where T:DependencyObject{var value=root as T;if(value!=null&&match(value))return value;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var found=Find<T>(VisualTreeHelper.GetChild(root,i),match);if(found!=null)return found;}return null;}
 [STAThread]static int Main(string[] args){Directory.CreateDirectory(args[0]);string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"migration-tests");Directory.CreateDirectory(root);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Window pet=null;try{
 string old=Path.Combine(root,"old"),profile=Path.Combine(root,"profile"),newApp=Path.Combine(root,"new-version");Directory.CreateDirectory(old);Directory.CreateDirectory(profile);Directory.CreateDirectory(newApp);
 File.WriteAllText(Path.Combine(old,"settings.ini"),"scale=1.5\nterminalTheme=kid");
 var skill=new SkillRuntime(old);skill.Engine.Data.Notes.Add(new SkillNote{Id="note",Title="保留便签",Text="内容"});skill.Engine.Data.EndOfWork.Slots=new System.Collections.Generic.List<WorkEndSlot>{new WorkEndSlot{Label="午休",Enabled=true,MinuteOfDay=720}};skill.Save();
 File.WriteAllText(Path.Combine(profile,"settings.ini"),"scale=1.1");
 Check("import missing copies only absent settings",UserData.ImportMissing(old,profile)==1&&File.ReadAllText(Path.Combine(profile,"settings.ini"))=="scale=1.1");
 Check("legacy source retained",File.ReadAllText(Path.Combine(old,"settings.ini")).Contains("scale=1.5"));
 var first=new SkillRuntime(AppDomain.CurrentDomain.BaseDirectory,profile);Check("bundled holiday calendar still loads",first.Engine.Calendars.ContainsKey(2026));first.Engine.Data.WaterMinutes=33;first.Save();var second=new SkillRuntime(newApp,profile);
 Check("new executable directory restores notes",second.Engine.Data.Notes.Count==1&&second.Engine.Data.Notes[0].Text=="内容");Check("new directory restores reminders and preferences",second.Engine.Data.WaterMinutes==33&&second.Engine.Data.EndOfWork.Slots[0].MinuteOfDay==720);
 using(var voice=new VoiceRuntime(AppDomain.CurrentDomain.BaseDirectory,profile)){voice.SetVolume(.13);voice.Apply();}using(var voice=new VoiceRuntime(newApp,profile))Check("volume survives different program directory",Math.Abs(voice.Settings.Volume-.13)<.001);
 var assistant=new AssistantRuntime(old,profile);assistant.Config.Bubbles=false;assistant.Save();Check("notification preferences survive update",!new AssistantRuntime(newApp,profile).Config.Bubbles);
 var advanced=new AdvancedRuntime(profile);advanced.Engine.AddTask("更新后保留",null,1);advanced.Save();Check("task data survives reload",new AdvancedRuntime(profile).Engine.Data.Tasks.Count==1);
 UserData.QueueImport(old,profile);Check("queued import does not overwrite live settings",File.ReadAllText(Path.Combine(profile,"settings.ini"))=="scale=1.1");File.WriteAllText(Path.Combine(profile,"settings.ini"),"scale=1.2");string backup=UserData.ApplyPending(profile);
 Check("restart applies queued settings",File.ReadAllText(Path.Combine(profile,"settings.ini")).Contains("scale=1.5"));Check("latest live settings backed up",File.ReadAllText(Path.Combine(backup,"settings.ini"))=="scale=1.2");Check("pending removed after successful import",!Directory.Exists(Path.Combine(profile,"pending-import")));Check("unimported files preserved",File.Exists(Path.Combine(profile,"voice-settings.json")));
 UserData.QueueImport(old,profile);UserData.CancelPending(profile);Check("pending import can be cancelled",UserData.ApplyPending(profile)==null);
 string invalid=Path.Combine(root,"invalid");Directory.CreateDirectory(invalid);File.WriteAllText(Path.Combine(invalid,"settings.ini"),"scale=1");File.WriteAllText(Path.Combine(invalid,"skills.json"),"broken");bool rejected=false;try{UserData.QueueImport(invalid,profile);}catch(ArgumentException){rejected=true;}Check("invalid import rejected before staging",rejected&&!Directory.Exists(Path.Combine(profile,"pending-import")));Check("rejected import preserves current data",File.ReadAllText(Path.Combine(profile,"settings.ini")).Contains("scale=1.5"));
 Environment.SetEnvironmentVariable("CONAN_PET_TEST_MODE",null);Check("normal releases share one stable profile",UserData.DirectoryFor(old)==UserData.DirectoryFor(newApp)&&UserData.DirectoryFor(old)==Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ConanDesktopAssistant"));Environment.SetEnvironmentVariable("CONAN_PET_TEST_MODE","1");
 Check("test mode stays isolated",UserData.DirectoryFor(old)==old);
 pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);pet.Show();var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(ConanPet).GetMethod("OpenTerminal",flags).Invoke(pet,null);var terminal=(TerminalWindow)typeof(ConanPet).GetField("terminal",flags).GetValue(pet);terminal.ShowPage("效率工具");terminal.UpdateLayout();Check("data route discoverable",Find<Button>(terminal,b=>Convert.ToString(b.Content)=="数据与备份")!=null);terminal.ShowPage("数据与备份");terminal.UpdateLayout();Check("data page has import control",Find<Button>(terminal,b=>Convert.ToString(b.Content)=="导入旧版数据")!=null);Check("data path is selectable",Find<TextBox>(terminal,b=>b.IsReadOnly&&b.Text==AppDomain.CurrentDomain.BaseDirectory)!=null);terminal.BackPage();Check("back navigation returns to tools",Find<Button>(terminal,b=>Convert.ToString(b.Content)=="数据与备份")!=null);
 }catch(Exception e){Console.WriteLine(e);failures++;}finally{if(pet!=null)pet.Close();app.Shutdown();}File.WriteAllText(Path.Combine(args[0],"user-data-results.json"),"{\"checks\":"+checks+",\"failures\":"+failures+"}");Console.WriteLine("Checks: "+checks+" Failures: "+failures);return failures==0?0:1;}
}
