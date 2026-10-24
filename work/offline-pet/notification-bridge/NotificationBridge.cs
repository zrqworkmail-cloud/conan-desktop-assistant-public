using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
class BridgeConfig {public bool Enabled=false,TitleOnly=true;public List<string> Blocked=new List<string>();}
class NotificationBridge:Window {
 readonly string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".conan-assistant","notifications");readonly JavaScriptSerializer json=new JavaScriptSerializer();readonly TextBlock state=new TextBlock{Text="尚未授权。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)};readonly DispatcherTimer timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};HashSet<string> seen=new HashSet<string>();bool baseline,busy;
 [STAThread]static void Main(){var app=new Application();app.Run(new NotificationBridge());}
 NotificationBridge(){Title="柯南 · 系统通知桥接";Width=540;Height=330;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=new System.Windows.Media.FontFamily("Microsoft YaHei UI");FontSize=14;Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.Combine(folder,"inbox"));var panel=new StackPanel{Margin=new Thickness(24)};Content=panel;panel.Children.Add(new TextBlock{Text="可选系统通知组件",FontSize=23,FontWeight=FontWeights.SemiBold});panel.Children.Add(new TextBlock{Text="授权后在本机读取应用Toast通知，并转交给桌宠。不会删除系统通知，也不会上传通知内容。首次连接先建立基线，不重播旧通知。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,15,0,8)});panel.Children.Add(state);var allow=new Button{Content="请求Windows通知读取授权",Padding=new Thickness(12,8,12,8)};allow.Click+=async(s,e)=>{try{var access=await UserNotificationListener.Current.RequestAccessAsync();state.Text=access==UserNotificationListenerAccessStatus.Allowed?"Windows已授权。请在桌宠通知中心启用接收。":"未获授权；其他桌宠功能可继续使用。";baseline=false;await Poll();}catch(Exception ex){state.Text="授权失败："+ex.Message+"\n请通过安装的应用入口启动，不要直接运行解包目录中的EXE。";WriteStatus("授权失败");}};panel.Children.Add(allow);panel.Children.Add(new TextBlock{Text="保持此组件运行才能持续接收。关闭窗口即停止监听。",FontSize=12,Margin=new Thickness(0,12,0,0)});timer.Tick+=async(s,e)=>await Poll();Loaded+=async(s,e)=>{timer.Start();await Poll();};Closed+=(s,e)=>{timer.Stop();WriteStatus("组件已停止");};}
 void Atomic(string path,string text){string temp=path+".tmp";File.WriteAllText(temp,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
 void WriteStatus(string text){try{Atomic(Path.Combine(folder,"status.json"),json.Serialize(new Dictionary<string,string>{{"state",text},{"utc",DateTime.UtcNow.ToString("o")}}));}catch(IOException){}catch(UnauthorizedAccessException){}}
 async System.Threading.Tasks.Task Poll(){if(busy)return;busy=true;try{
  var listener=UserNotificationListener.Current;if(listener.GetAccessStatus()!=UserNotificationListenerAccessStatus.Allowed){baseline=false;WriteStatus("未授权或授权已撤销");return;}
  BridgeConfig config=null;try{config=json.Deserialize<BridgeConfig>(File.ReadAllText(Path.Combine(folder,"config.json")));}catch(IOException){}catch(ArgumentException){}if(config==null||!config.Enabled){baseline=false;WriteStatus("已授权，接收尚未开启");return;}
  var all=await listener.GetNotificationsAsync(NotificationKinds.Toast);var current=new HashSet<string>();int count=0;
  foreach(var notification in all){try{
   string appId=notification.AppInfo.AppUserModelId;string id=notification.Id+"-"+notification.CreationTime.UtcDateTime.Ticks;current.Add(id);if(!baseline||seen.Contains(id)||count>=30||DateTime.UtcNow-notification.CreationTime.UtcDateTime>TimeSpan.FromMinutes(2))continue;string app=notification.AppInfo.DisplayInfo.DisplayName;if(config.Blocked!=null&&config.Blocked.Contains(app))continue;var binding=notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);if(binding==null)continue;var texts=binding.GetTextElements();string title=texts.Count>0?texts[0].Text:"通知";string body=config.TitleOnly?"":String.Join("\n",texts.Skip(1).Select(t=>t.Text).ToArray());if(title.Length>500)title=title.Substring(0,500);if(body.Length>2000)body=body.Substring(0,2000);if(Directory.GetFiles(Path.Combine(folder,"inbox"),"*.json").Length>=100)continue;
   Atomic(Path.Combine(folder,"inbox",Guid.NewGuid().ToString("N")+".json"),json.Serialize(new {Id=id,App=app,AppId=appId,Title=title,Body=body,Utc=DateTime.UtcNow}));count++;
  }catch(Exception){}}
  seen=current;baseline=true;WriteStatus("已连接 · Windows已授权");state.Text="已连接。来源筛选、只显示标题和勿扰请在桌宠通知中心设置。";
 }catch(Exception ex){baseline=false;WriteStatus("读取失败");state.Text="读取失败："+ex.Message;}finally{busy=false;}}
}
