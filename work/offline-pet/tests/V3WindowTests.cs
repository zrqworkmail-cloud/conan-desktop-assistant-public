using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
class V3WindowTests {
 static int failed;static void Check(string n,bool v){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)failed++;}
 static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
 static Button Find(DependencyObject root,string name){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);var b=child as Button;if(b!=null&&Convert.ToString(b.Content)==name)return b;var nested=Find(child,name);if(nested!=null)return nested;}return null;}
 static TextBox FindSearch(DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);var text=c as TextBox;if(text!=null&&System.Windows.Automation.AutomationProperties.GetName(text)=="搜索技能")return text;var found=FindSearch(c);if(found!=null)return found;}return null;}
 static void Click(Window w,string n){w.UpdateLayout();var b=Find(w,n);if(b==null)throw new Exception("Missing button: "+n);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));w.UpdateLayout();}
 [STAThread]static int Main(string[] args){var app=new Application();var runtime=new SkillRuntime(AppDomain.CurrentDomain.BaseDirectory);var w=new SkillsWindow(runtime);w.Show();w.ShowPage("待办计划");w.UpdateLayout();
  Check("workbench exposes task creation",Find(w,"添加待办")!=null);if(failed>0){w.Close();return 1;}
  try{
   ((TextBox)Field(w,"taskTitle")).Text="准备论文";Click(w,"添加待办");var advanced=(AdvancedRuntime)Field(w,"advanced");Check("task creation saves actual data",advanced.Engine.Data.Tasks.Any(t=>t.Title=="准备论文"));
   var list=(ListBox)Field(w,"taskList");list.SelectedIndex=0;Click(w,"完成 / 重新打开");Check("complete action changes task state",advanced.Engine.Data.Tasks[0].Done);
   var restore=new AdvancedRuntime(AppDomain.CurrentDomain.BaseDirectory);Check("completed tasks survive restart",restore.Engine.Data.Tasks[0].Done);restore.Dispose();
   w.ShowPage("剪贴板");w.UpdateLayout();Check("clipboard defaults to disabled",!advanced.Engine.Data.ClipboardEnabled);var check=(CheckBox)Field(w,"captureClipboard");check.IsChecked=true;check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));advanced.Clipboard.Capture("界面剪贴板测试");w.Update();Check("clipboard history appears in UI",((ListBox)Field(w,"clipboardList")).Items.Count==1);Click(w,"清空历史");Check("clear action erases in memory history",advanced.Clipboard.Items.Count==0);
   w.ShowPage("提醒");w.UpdateLayout();var timer=runtime.Engine.AddCountdown("循环提醒",1,DateTime.UtcNow);w.Update();w.SelectReminder(timer.Id);((TextBox)Field(w,"repeatMinutes")).Text="60";Click(w,"设置重复间隔");Check("repeat action saves selected reminder",timer.RepeatMinutes==60);
   w.ShowPage("文件观察");w.UpdateLayout();string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"observed.txt");File.WriteAllText(target,"before");advanced.AddFile(target);advanced.Tick(DateTime.UtcNow);File.WriteAllText(target,"changed content");var alerts=advanced.Tick(DateTime.UtcNow.AddSeconds(4));Check("real file changes create an actionable alert",alerts.Any(a=>a.Key.StartsWith("file:")));Check("unchanged file does not repeatedly alert",!advanced.Tick(DateTime.UtcNow.AddSeconds(8)).Any(a=>a.Key.StartsWith("file:")));
   File.Delete(target);Check("file deletion is reported",advanced.Tick(DateTime.UtcNow.AddSeconds(12)).Any(a=>a.Text.Contains("已删除")));File.WriteAllText(target,"restored");Check("file restoration is reported",advanced.Tick(DateTime.UtcNow.AddSeconds(16)).Any(a=>a.Text.Contains("已恢复")));
   w.ShowPage("语音与快捷键");w.UpdateLayout();Check("local speech and hotkey controls are present",Find(w,"朗读文本")!=null&&Find(w,"停止朗读")!=null&&Find(w,"保存快捷键设置")!=null);
   var search=FindSearch(w);Check("skill search is available",search!=null);if(search!=null){search.Text="剪贴";search.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),0,System.Windows.Input.Key.Enter){RoutedEvent=System.Windows.Input.Keyboard.KeyDownEvent});Check("skill search navigates to matching page",Convert.ToString(((TabItem)((TabControl)Field(w,"tabs")).SelectedItem).Header)=="剪贴板");}
   w.ShowPage("磁盘与电池");w.UpdateLayout();Check("system storage reads actual disk data",AdvancedRuntime.StorageStatus().Contains("GiB"));
   w.ShowPage("总览");w.UpdateLayout();var overview=(TextBlock)Field(w,"overviewNext");var ink=overview.Foreground as SolidColorBrush;Check("overview text remains readable when its tab is selected",ink!=null&&ink.Color.R<100);
   Directory.CreateDirectory(args[0]);foreach(var page in new[]{"总览","待办计划","专注","剪贴板","文件观察"}){w.ShowPage(page);w.UpdateLayout();var bmp=new RenderTargetBitmap((int)w.ActualWidth,(int)w.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(w);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var f=File.Create(Path.Combine(args[0],"v3-"+page+".png")))png.Save(f);}
   Check("workbench uses side navigation",((TabControl)Field(w,"tabs")).TabStripPlacement==Dock.Left);
  }catch(Exception e){Console.WriteLine(e);failed++;}finally{w.Close();}Console.WriteLine("Failures: "+failed);return failed==0?0:1;
 }
}
