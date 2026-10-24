using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
class FileEvidenceView:StackPanel {
 readonly TextBlock info=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,15,0,15)},status=new TextBlock{TextWrapping=TextWrapping.Wrap};string selected,hash;int revision;
 public FileEvidenceView(){Children.Add(new TextBlock{Text="把一个文件拖到柯南身上，或在此选择文件。所有核验在本机完成。",TextWrapping=TextWrapping.Wrap});var row=new WrapPanel();Children.Add(row);Add(row,"选择文件",()=>{var picker=new Microsoft.Win32.OpenFileDialog();if(picker.ShowDialog()==true)LoadPath(picker.FileName);});Add(row,"定位文件",()=>{if(selected!=null)Process.Start("explorer.exe","/select,\""+selected+"\"");});Add(row,"复制SHA-256",()=>{if(hash!=null)Clipboard.SetText(hash);});Children.Add(info);Children.Add(status);}
 static void Add(Panel p,string label,Action action){var b=new Button{Content=label};b.Click+=(s,e)=>action();p.Children.Add(b);}
 public async void LoadPath(string path){int current=++revision;hash=null;selected=null;try{var file=new FileInfo(path);if(!file.Exists){status.Text="文件不存在。";return;}selected=file.FullName;info.Text=file.Name+"\n"+file.FullName+"\n大小："+file.Length.ToString("N0")+" 字节\n修改时间："+file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");status.Text="正在核验SHA-256……";string digest=await Task.Run(()=>{using(var stream=new FileStream(file.FullName,FileMode.Open,FileAccess.Read,FileShare.Read))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();});if(current!=revision)return;hash=digest;status.Text="SHA-256\n"+digest+"\n校验值用于比较文件是否一致，不能据此判断文件安全。";}catch(Exception e){if(!(e is IOException)&&!(e is UnauthorizedAccessException)&&!(e is ArgumentException))throw;if(current==revision){selected=null;hash=null;status.Text="无法核验："+e.Message;}}}
}
