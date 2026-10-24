using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
public class SystemNotificationConfig {public bool Enabled,Quiet;public bool TitleOnly=true;public List<string> Blocked=new List<string>();}
public class SystemNotice {public string Id,App,AppId,Title,Body;public DateTime Utc;}
public class SystemNotificationInbox {
 readonly string folder;readonly JavaScriptSerializer json=new JavaScriptSerializer();public readonly List<SystemNotice> Items=new List<SystemNotice>();public SystemNotificationConfig Config;public string Status="系统通知：尚未启用";
 public SystemNotificationInbox(){folder=Environment.GetEnvironmentVariable("CONAN_NOTIFICATION_DIR");if(String.IsNullOrEmpty(folder)&&Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1")folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"notification-test");if(String.IsNullOrEmpty(folder))folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".conan-assistant","notifications");Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.Combine(folder,"inbox"));try{Config=json.Deserialize<SystemNotificationConfig>(File.ReadAllText(Path.Combine(folder,"config.json")));}catch(IOException){}catch(ArgumentException){}if(Config==null)Config=new SystemNotificationConfig();if(Config.Blocked==null)Config.Blocked=new List<string>();}
 public void Save(){File.WriteAllText(Path.Combine(folder,"config.json"),json.Serialize(Config),Encoding.UTF8);}
 public void Block(string app){if(String.IsNullOrWhiteSpace(app)||app.Length>200)throw new ArgumentException("请输入通知来源应用名称。");if(!Config.Blocked.Contains(app.Trim()))Config.Blocked.Add(app.Trim());Save();}
 public List<SystemNotice> Poll(){
  var fresh=new List<SystemNotice>();Status=Config.Enabled?"系统通知：组件尚未连接":"系统通知：尚未启用";
  try{string statusFile=Path.Combine(folder,"status.json");if(Config.Enabled&&File.Exists(statusFile)&&DateTime.UtcNow-File.GetLastWriteTimeUtc(statusFile)<TimeSpan.FromSeconds(15)){var data=json.Deserialize<Dictionary<string,string>>(File.ReadAllText(statusFile));string state;if(data.TryGetValue("state",out state))Status="系统通知："+state;}}
  catch(IOException){}catch(ArgumentException){}
  foreach(var file in Directory.GetFiles(Path.Combine(folder,"inbox"),"*.json").Take(100)){
   try{if(!Config.Enabled||new FileInfo(file).Length>16384)continue;var n=json.Deserialize<SystemNotice>(File.ReadAllText(file));if(n==null||String.IsNullOrEmpty(n.Id)||n.Id.Length>240||String.IsNullOrEmpty(n.App)||n.App.Length>200||n.Title==null||n.Title.Length>500||n.Body!=null&&n.Body.Length>2000||n.Utc<DateTime.UtcNow.AddMinutes(-2)||n.Utc>DateTime.UtcNow.AddMinutes(1)||Config.Blocked.Contains(n.App)||Items.Any(i=>i.Id==n.Id))continue;if(Config.TitleOnly)n.Body="";Items.Insert(0,n);if(Items.Count>100)Items.RemoveAt(100);fresh.Add(n);
   }catch(IOException){}catch(ArgumentException){}
   finally{try{File.Delete(file);}catch(IOException){}catch(UnauthorizedAccessException){}}
  }return fresh;
 }
 public SystemNotice Find(string key){return Items.FirstOrDefault(n=>"system:"+n.Id==key);}
}
