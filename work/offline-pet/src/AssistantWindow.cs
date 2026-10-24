using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms=System.Windows.Forms;

class ProcessChoice {public int Id;public string Name;public override string ToString(){return Name+" ["+Id+"]";}}
class AssistantWindow:Window {
 readonly AssistantRuntime runtime;readonly string home;readonly TabControl tabs=new TabControl();
 readonly ListBox shortcuts=new ListBox(),processes=new ListBox(),watched=new ListBox(),history=new ListBox();
 readonly TextBlock system=new TextBlock(),network=new TextBlock(),codex=new TextBlock();
 readonly TextBlock copyIpStatus=new TextBlock();System.Windows.Controls.Button copyIpButton;bool copyingIp;AssistantSnapshot snapshot;readonly TextBox shortcutSearch=new TextBox{Margin=new Thickness(0,0,0,8)};
 public AssistantWindow(AssistantRuntime data,string folder){
  runtime=data;home=folder;Title="柯南 · 桌面助手";Width=720;Height=540;MinWidth=620;MinHeight=460;
  WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=new FontFamily("Microsoft YaHei UI");FontSize=13;
  WorkbenchTheme.Apply(this,tabs,"快捷入口、系统状态与任务通知，集中查看。");
  var quick=Page(tabs,"快捷入口");quick.Children.Add(new TextBlock{Text="双击打开常用文件夹、文件或程序。",Margin=new Thickness(0,0,0,10)});
  quick.Children.Add(new TextBlock{Text="搜索快捷入口"});quick.Children.Add(shortcutSearch);shortcutSearch.TextChanged+=(s,e)=>RefreshShortcuts();shortcutSearch.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter){if(shortcuts.SelectedItem==null&&shortcuts.Items.Count>0)shortcuts.SelectedIndex=0;OpenShortcut();e.Handled=true;}};
  shortcuts.Height=260;quick.Children.Add(shortcuts);shortcuts.MouseDoubleClick+=(s,e)=>OpenShortcut();
  var actions=new WrapPanel();quick.Children.Add(actions);
  Button(actions,"打开",OpenShortcut);Button(actions,"添加文件夹",()=>PickShortcut(true));Button(actions,"添加文件 / 程序",()=>PickShortcut(false));Button(actions,"添加网页",PickWebsite);
  Button(actions,"移除",()=>{var item=shortcuts.SelectedItem as Shortcut;if(item!=null){runtime.Config.Shortcuts.Remove(item);Save();RefreshShortcuts();}});RefreshShortcuts();
  var info=Page(tabs,"系统与网络");system.Margin=new Thickness(0,0,0,18);info.Children.Add(system);network.TextWrapping=TextWrapping.Wrap;info.Children.Add(network);
  info.Children.Add(new TextBlock{Text="显示本机活动网卡的IPv4地址；不代表互联网连通性，不查询公网IP。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,10)});
  copyIpButton=Button(info,"复制IP地址",CopyIp);copyIpStatus.TextWrapping=TextWrapping.Wrap;info.Children.Add(copyIpStatus);
  var monitor=Page(tabs,"进程监控");monitor.Children.Add(new TextBlock{Text="选择一个正在运行的进程进行观察；不会结束或修改进程。"});
  processes.Height=140;monitor.Children.Add(processes);var tools=new WrapPanel();monitor.Children.Add(tools);
  Button(tools,"刷新进程",RefreshProcesses);Button(tools,"监控选中进程",()=>{
   var item=processes.SelectedItem as ProcessChoice;if(item==null)return;
   try{lock(runtime)runtime.Watch(Process.GetProcessById(item.Id));}catch(Exception ex){Error(ex.Message);}
  });
  watched.Height=95;monitor.Children.Add(watched);Button(monitor,"移除选中监控",()=>{var item=watched.SelectedItem as WatchedProcess;if(item!=null){lock(runtime)runtime.Unwatch(item.Id);}});
  monitor.Children.Add(new TextBlock{Text="在本页选择需要观察的进程；多级 CPU/内存提醒点在侦探备忘录上方设置。连续3次超阈值提醒，恢复至该点92%以下后重新布防。进程选择仅保留本次运行。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)});
  var tasks=Page(tabs,"Codex状态");codex.TextWrapping=TextWrapping.Wrap;tasks.Children.Add(codex);
  tasks.Children.Add(new TextBlock{Text="只显示事件提供的状态，不估算完成百分比。超过15分钟没有事件显示“等待更新”，不据此推断需要审批。\n接入只接收状态，不改变Codex的审批或执行行为。安全版更新后，请重新安装接入以更新旧钩子的路径转义。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,12)});
  Button(tasks,"安装 Codex 接入",()=>Install(false));Button(tasks,"移除 Codex 接入",()=>Install(true));
  var notices=Page(tabs,"通知记录");var enabled=new CheckBox{Content="显示通知气泡（独立于随机台词开关）",IsChecked=runtime.Config.Bubbles,Margin=new Thickness(0,0,0,10)};
  enabled.Click+=(s,e)=>{runtime.Config.Bubbles=enabled.IsChecked==true;Save();};notices.Children.Add(enabled);history.Height=350;notices.Children.Add(history);
  RefreshProcesses();
 }
 static StackPanel Page(TabControl tabs,string name){var panel=new StackPanel{Margin=new Thickness(25)};panel.SetValue(TextBlock.ForegroundProperty,WorkbenchTheme.Ink);panel.SetValue(TextBlock.FontWeightProperty,FontWeights.Normal);WorkbenchTheme.PageHeading(panel,name,"");tabs.Items.Add(new TabItem{Header=name,Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}});return panel;}
 static System.Windows.Controls.Button Button(Panel parent,string name,Action action){var b=new System.Windows.Controls.Button{Content=name,Padding=new Thickness(12,6,12,6),Margin=new Thickness(0,6,8,2),HorizontalAlignment=HorizontalAlignment.Left};b.Click+=(s,e)=>action();parent.Children.Add(b);return b;}
 async void CopyIp(){if(copyingIp)return;string text=snapshot==null?null:snapshot.IPText;if(String.IsNullOrWhiteSpace(text)){copyIpStatus.Text="暂无可复制的IP地址。";return;}copyingIp=true;copyIpButton.IsEnabled=false;copyIpStatus.Text="正在复制…";try{bool ok=await ClipboardCopy.WriteText(text);copyIpStatus.Text=ok?"IP地址已复制。":"剪贴板正被占用，复制未完成，请稍后重试。";}finally{copyingIp=false;copyIpButton.IsEnabled=true;}}
 void Error(string text){MessageBox.Show(text,"柯南桌面助手",MessageBoxButton.OK,MessageBoxImage.Information);}
 void Save(){try{lock(runtime)runtime.Save();}catch(IOException e){Error(e.Message);}catch(UnauthorizedAccessException e){Error(e.Message);}}
 void RefreshShortcuts(){string query=shortcutSearch.Text.Trim();shortcuts.ItemsSource=runtime.Config.Shortcuts.Where(s=>query.Length==0||s.Name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||s.Path.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();}
 void OpenShortcut(){var s=shortcuts.SelectedItem as Shortcut;if(s==null)return;if(!AssistantLogic.LocalTarget(s.Path)&&!SkillLogic.WebTarget(s.Path)){Error("目标不存在或不是本地文件路径，请重新添加。");return;}try{Process.Start(new ProcessStartInfo(s.Path){UseShellExecute=true});}catch(Exception e){Error(e.Message);}}
 void PickWebsite(){
  var dialog=new Window{Title="添加网页快捷入口",Owner=Application.Current.Windows.OfType<TerminalWindow>().FirstOrDefault(),Width=420,Height=220,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner};
  var panel=new StackPanel{Margin=new Thickness(16)};dialog.Content=panel;panel.Children.Add(new TextBlock{Text="名称"});var name=new TextBox{Margin=new Thickness(0,4,0,8)};panel.Children.Add(name);panel.Children.Add(new TextBlock{Text="网址（http / https）"});var url=new TextBox{Text="https://",Margin=new Thickness(0,4,0,8)};panel.Children.Add(url);
  Button(panel,"添加",()=>{if(String.IsNullOrWhiteSpace(name.Text)||!SkillLogic.WebTarget(url.Text)){Error("请输入名称和有效的http(s)网址。");return;}runtime.Config.Shortcuts.Add(new Shortcut{Name=name.Text.Trim(),Path=url.Text.Trim()});Save();RefreshShortcuts();dialog.Close();});dialog.ShowDialog();
 }
 void PickShortcut(bool folder){
  string path=null;
  if(folder){using(var picker=new Forms.FolderBrowserDialog{Description="选择快捷打开的文件夹"})if(picker.ShowDialog()==Forms.DialogResult.OK)path=picker.SelectedPath;}
  else{using(var picker=new Forms.OpenFileDialog{Title="选择文件或程序"})if(picker.ShowDialog()==Forms.DialogResult.OK)path=picker.FileName;}
  if(path==null||!AssistantLogic.LocalTarget(path))return;
  runtime.Config.Shortcuts.Add(new Shortcut{Name=Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)),Path=path});Save();RefreshShortcuts();
 }
 void RefreshProcesses(){
  var list=new System.Collections.Generic.List<ProcessChoice>();
  foreach(var p in Process.GetProcesses()){using(p){try{list.Add(new ProcessChoice{Id=p.Id,Name=p.ProcessName});}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}}
  processes.ItemsSource=list.OrderBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToArray();
 }
 void Install(bool uninstall){
  using(var picker=new Forms.FolderBrowserDialog{Description="选择Codex配置目录（包含config.toml，例如.codex或codex-home）"}){
   string known=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Codex","codex-home");picker.SelectedPath=Directory.Exists(known)?known:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");
   if(picker.ShowDialog()!=Forms.DialogResult.OK)return;
   try{
    var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8};
    start.Arguments="-NoProfile -ExecutionPolicy Bypass -File \""+Path.Combine(home,"bridge","install-codex.ps1")+"\" -CodexHome \""+picker.SelectedPath+"\""+(uninstall?" -Uninstall":"");
    var process=Process.Start(start);string output=process.StandardOutput.ReadToEnd();string error=process.StandardError.ReadToEnd();process.WaitForExit();
    Error(process.ExitCode==0?output:error);process.Dispose();
   }catch(Exception e){Error(e.Message);}
  }
 }
 public void FocusSearch(){ShowPage("快捷入口");shortcutSearch.Focus();shortcutSearch.SelectAll();}
 public void ShowProcess(int id){ShowPage("进程监控");if(snapshot!=null)watched.SelectedItem=snapshot.Watches.FirstOrDefault(p=>p.Id==id);}
 readonly System.Collections.Generic.Dictionary<string,FrameworkElement> embedded=new System.Collections.Generic.Dictionary<string,FrameworkElement>();
 public FrameworkElement TakePage(string name){FrameworkElement element;if(embedded.TryGetValue(name,out element)){var decorator=element.Parent as Decorator;if(decorator!=null)decorator.Child=null;var owner=element.Parent as ContentControl;if(owner!=null)owner.Content=null;var panel=element.Parent as Panel;if(panel!=null)panel.Children.Remove(element);return element;}foreach(TabItem tab in tabs.Items)if(Convert.ToString(tab.Header)==name){element=(FrameworkElement)tab.Content;tab.Content=null;embedded[name]=element;return element;}throw new ArgumentException("页面不存在："+name);}
 public void ShowPage(string name){foreach(TabItem tab in tabs.Items)if(Convert.ToString(tab.Header)==name){tabs.SelectedItem=tab;break;}}
 public void Update(AssistantSnapshot value){
  snapshot=value;system.Text=value.SystemText;network.Text=value.NetworkText;codex.Text=value.CodexText;
  var selected=watched.SelectedItem as WatchedProcess;watched.ItemsSource=value.Watches;
  if(selected!=null)watched.SelectedItem=value.Watches.FirstOrDefault(p=>p.Id==selected.Id);
  history.ItemsSource=value.Notices;
 }
}

internal static class ClipboardCopy {
 internal static async System.Threading.Tasks.Task<bool> WriteText(string text,Action<string> write=null){if(String.IsNullOrWhiteSpace(text))return false;write=write??(value=>Forms.Clipboard.SetDataObject(value,true,0,0));for(int attempt=0;attempt<4;attempt++){try{write(text);return true;}catch(System.Runtime.InteropServices.ExternalException){}if(attempt<3)await System.Threading.Tasks.Task.Delay(120);}return false;}
}
