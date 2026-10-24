using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
class SkillsWindowTests {
 static int failed;static void Check(string n,bool ok){Console.WriteLine((ok?"PASS ":"FAIL ")+n);if(!ok)failed++;}
 static object Field(object target,string name){return target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 [STAThread]static int Main(string[] args){
  var app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
  var pet=(Window)Activator.CreateInstance(typeof(ConanPet),true);var runtime=(SkillRuntime)Field(pet,"skills");
  var reminder=runtime.Engine.AddCountdown("集成测试提醒",1,DateTime.UtcNow);reminder.DueUtc=DateTime.UtcNow.AddSeconds(1);runtime.Save();
  var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};timer.Tick+=(s,e)=>{timer.Stop();
   try{
    var queue=(BubbleQueue)Field(pet,"messages");Check("real pet ticker emits a due skill bubble",queue.Current!=null&&queue.Current.Key=="timer:"+reminder.Id);
    var bubble=(SpeechBubble)Field(pet,"bubble");Click(bubble,"查看详情");var window=(SkillsWindow)Field(pet,"skillsWindow");
    Check("bubble details opens the matching skill window",window!=null&&window.IsVisible);
    Check("reminder navigation selects its record",((ListBox)Field(window,"reminders")).SelectedItem==reminder);
    window.ShowPage("专注");window.UpdateLayout();Click(window,"开始专注");Check("focus start button begins real state",runtime.Engine.Data.Focus.Running);
    Click(window,"暂停");Check("focus pause button updates state",runtime.Engine.Data.Focus.Paused);Click(window,"继续");Check("focus resume button updates state",runtime.Engine.Data.Focus.Running);Click(window,"停止");
    window.ShowPage("提醒");window.UpdateLayout();((TextBox)Field(window,"reminderLabel")).Text="界面倒计时";((TextBox)Field(window,"countdown")).Text="2";Click(window,"添加倒计时");Check("countdown button saves a reminder",runtime.Engine.Data.Reminders.Any(r=>r.Label=="界面倒计时"));
    window.SelectReminder(reminder.Id);window.UpdateLayout();Click(window,"稍后5分钟");Check("snooze button reactivates reminder",reminder.Enabled&&reminder.DueUtc>DateTime.UtcNow.AddMinutes(4));
    window.ShowPage("健康");window.UpdateLayout();((CheckBox)Field(window,"water")).IsChecked=true;((TextBox)Field(window,"waterMinutes")).Text="30";Click(window,"保存健康提醒");Check("wellness settings save from the UI",runtime.Engine.Data.Reminders.Any(r=>r.Kind=="water"&&r.Enabled));
    window.ShowPage("便签");window.UpdateLayout();((TextBox)Field(window,"noteTitle")).Text="界面便签";((TextBox)Field(window,"noteText")).Text="这是一张本地便签 😀";Click(window,"保存便签");
    var restored=new SkillRuntime(AppDomain.CurrentDomain.BaseDirectory);Check("note save button persists Unicode",restored.Engine.Data.Notes.Any(n=>n.Title=="界面便签"&&n.Text.Contains("😀")));
    window.ShowPage("文本");window.UpdateLayout();var text=(TextBox)Field(window,"text");text.Text="A\n\nA\nB";Click(window,"逐行去重");Check("text tool button transforms content",text.Text=="A"+Environment.NewLine+"B");
    text.Text="{\"name\":\"柯南\",\"items\":[1,2]}";Click(window,"格式化JSON");Check("JSON tool button formats content",text.Text.Contains("\n")&&text.Text.Contains("柯南"));
    Directory.CreateDirectory(args[0]);window.ShowPage("专注");window.UpdateLayout();Save(window,Path.Combine(args[0],"skills-focus.png"));window.ShowPage("提醒");window.UpdateLayout();Save(window,Path.Combine(args[0],"skills-reminders.png"));window.ShowPage("便签");window.UpdateLayout();Save(window,Path.Combine(args[0],"skills-notes.png"));
    window.ShowPage("桌面工具");window.UpdateLayout();Check("desktop tool controls are available",Find(window,"截图 / 框选")!=null&&Find(window,"播放 / 暂停")!=null&&Find(window,"网络设置")!=null);
    var assistant=(AssistantRuntime)Field(pet,"assistant");assistant.Config.Shortcuts.Clear();assistant.Config.Shortcuts.Add(new Shortcut{Name="测试文件",Path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"conan.png")});assistant.Config.Shortcuts.Add(new Shortcut{Name="文档入口",Path=AppDomain.CurrentDomain.BaseDirectory});
    typeof(ConanPet).GetMethod("OpenAssistant",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(pet,null);var panel=(AssistantWindow)Field(pet,"assistantWindow");((TextBox)Field(panel,"shortcutSearch")).Text="测试文件";
    Check("launcher search filters the real shortcut list",((ListBox)Field(panel,"shortcuts")).Items.Count==1);
    typeof(ConanPet).GetMethod("OpenDetails",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(pet,new object[]{new BubbleMessage{Key="codex:invalid-session",Notification=true,Text="测试"}});
    Check("unavailable Codex thread falls back to the status page",Convert.ToString(((TabItem)((TabControl)Field(panel,"tabs")).SelectedItem).Header)=="Codex状态");
   }catch(Exception error){Console.WriteLine(error.ToString());failed++;}
   finally{pet.Close();app.Shutdown();}
  };timer.Start();app.Run(pet);Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
 static Button Find(DependencyObject root,string name){
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);var b=child as Button;if(b!=null&&(Convert.ToString(b.Content)==name||System.Windows.Automation.AutomationProperties.GetName(b)==name))return b;var nested=Find(child,name);if(nested!=null)return nested;}return null;
 }
 static void Click(Window root,string name){root.UpdateLayout();var button=Find(root,name);if(button==null)throw new Exception("Button missing: "+name);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();}
 static void Save(Window root,string path){root.UpdateLayout();var image=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create(path))encoder.Save(file);}
}
