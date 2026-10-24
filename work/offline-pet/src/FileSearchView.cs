using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
class FileSearchView:StackPanel {
 public Action<string> VoiceEvent;
 readonly TextBox folder=new TextBox{IsReadOnly=true,MinWidth=400},query=new TextBox{MinWidth=260};readonly ListBox results=new ListBox{MinHeight=280};readonly TextBlock status=new TextBlock{Text="选择一个文件夹，按文件名关键词查找。",TextWrapping=TextWrapping.Wrap};readonly Button start=new Button{Content="开始搜索"},cancel=new Button{Content="取消",IsEnabled=false};CancellationTokenSource cancellation;readonly Action<bool> activity;
 public FileSearchView(Action<bool> changed){activity=changed;Children.Add(new TextBlock{Text="仅搜索你选择的目录及子目录；最多200项结果、5万个文件，跳过链接与不可访问目录。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,15)});var pick=new Button{Content="选择搜索文件夹"};pick.Click+=(s,e)=>{using(var dialog=new System.Windows.Forms.FolderBrowserDialog{Description="选择搜索范围"})if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)folder.Text=dialog.SelectedPath;};Children.Add(pick);Children.Add(folder);Children.Add(new TextBlock{Text="文件名关键词",Margin=new Thickness(0,12,0,4)});Children.Add(query);var actions=new WrapPanel();Children.Add(actions);actions.Children.Add(start);actions.Children.Add(cancel);start.Click+=(s,e)=>Begin();query.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter&&cancellation==null)Begin();};cancel.Click+=(s,e)=>Cancel();Children.Add(status);Children.Add(results);var reveal=new Button{Content="打开选中文件所在位置"};Children.Add(reveal);reveal.Click+=(s,e)=>Reveal();results.MouseDoubleClick+=(s,e)=>Reveal();System.Windows.Automation.AutomationProperties.SetName(query,"文件名关键词");}
 void Reveal(){string path=results.SelectedItem as string;if(path!=null&&File.Exists(path))System.Diagnostics.Process.Start("explorer.exe","/select,\""+path+"\"");}
 void Begin(){if(cancellation!=null)return;if(!Directory.Exists(folder.Text)||String.IsNullOrWhiteSpace(query.Text)){status.Text="请先选择文件夹并输入关键词。";return;}string root=folder.Text,term=query.Text;var source=new CancellationTokenSource();cancellation=source;start.IsEnabled=false;cancel.IsEnabled=true;results.ItemsSource=null;status.Text="正在寻找线索…";activity(true);if(VoiceEvent!=null)VoiceEvent("search-start");
  Task.Factory.StartNew(()=>FileFinder.Search(root,term,source.Token),source.Token).ContinueWith(task=>Dispatcher.BeginInvoke(new Action(()=>{try{if(task.IsCanceled)status.Text="搜索已取消。";else if(task.IsFaulted){status.Text="搜索失败："+task.Exception.GetBaseException().Message;if(VoiceEvent!=null)VoiceEvent("search-error");}else{if(task.Result.Files.Count>0&&VoiceEvent!=null)VoiceEvent("search-found");results.ItemsSource=task.Result.Files;status.Text=String.Format("找到{0}项 · 检查{1}个文件 · 跳过{2}个目录{3}",task.Result.Files.Count,task.Result.Scanned,task.Result.Skipped,task.Result.Limited?" · 已达到搜索上限":"");}}finally{cancellation=null;source.Dispose();start.IsEnabled=true;cancel.IsEnabled=false;activity(false);}})));
 }
 public void Cancel(){if(cancellation!=null)cancellation.Cancel();}
}
