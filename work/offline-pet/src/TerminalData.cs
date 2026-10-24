using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Forms=System.Windows.Forms;

partial class TerminalWindow {
 void BuildData(){
  string folder=UserData.DirectoryFor(home);StackPanel body;content.Children.Add(Panel("数据与备份","tasks",out body));
  SettingDescription(body,"设置、便签、提醒、快捷入口与音量统一保存在当前 Windows 用户的数据目录。以后更新时直接解压新版即可沿用。授权记录绑定本机和Windows账户并加密保存，不随程序包或设置导入复制。");
  body.Children.Add(new TextBox{Text=folder,IsReadOnly=true,Margin=new Thickness(0,12,0,12)});
  var actions=new WrapPanel();body.Children.Add(actions);
  Button(actions,"打开数据目录",()=>OpenDataFolder(folder));
  Button(actions,"查看导入备份",()=>OpenDataFolder(Path.Combine(folder,"backups")));
  var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,12)};body.Children.Add(status);
  Action refresh=()=>{status.Text=Directory.Exists(Path.Combine(folder,"pending-import"))?"已准备导入：请先退出桌宠，再重新打开。生效前会自动备份被替换的数据。":"可以从旧版程序目录或导入备份中恢复设置。原文件会保留；同名设置会在下次启动时替换。外部程序路径与授权不随备份导入，额度监测需重新确认。";};refresh();
  Button(actions,"导入旧版数据",()=>{try{using(var picker=new Forms.FolderBrowserDialog{Description="选择旧版 ConanPet.exe 所在的文件夹，或导入备份目录"})if(picker.ShowDialog()==Forms.DialogResult.OK){UserData.QueueImport(picker.SelectedPath,folder);refresh();}}catch(Exception e){if(!(e is IOException)&&!(e is UnauthorizedAccessException)&&!(e is ArgumentException)&&!(e is InvalidOperationException))throw;MessageBox.Show(this,"未导入，当前数据保持不变。\n"+e.Message,"导入数据",MessageBoxButton.OK,MessageBoxImage.Warning);}});
  Button(actions,"取消待导入",()=>{try{UserData.CancelPending(folder);refresh();}catch(IOException e){MessageBox.Show(this,e.Message,"取消导入");}catch(UnauthorizedAccessException e){MessageBox.Show(this,e.Message,"取消导入");}});
 }
 void OpenDataFolder(string folder){try{Directory.CreateDirectory(folder);Process.Start(new ProcessStartInfo(folder){UseShellExecute=true});}catch(IOException e){MessageBox.Show(this,e.Message,"打开数据目录");}catch(UnauthorizedAccessException e){MessageBox.Show(this,e.Message,"打开数据目录");}}
}
