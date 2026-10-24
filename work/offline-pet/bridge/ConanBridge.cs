using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
class ConanBridge {
 static int Main(){
  try{
   var json=new JavaScriptSerializer();var e=json.Deserialize<Dictionary<string,object>>(Console.In.ReadToEnd());object name,id;
   if(e.TryGetValue("hook_event_name",out name)&&e.TryGetValue("session_id",out id)&&AssistantLogic.State(Convert.ToString(name))!=null){
    string session=Convert.ToString(id);if(String.IsNullOrWhiteSpace(session)||session.Length>128)return 0;
    string folder=AssistantLogic.EventDirectory(AppDomain.CurrentDomain.BaseDirectory);Directory.CreateDirectory(folder);
    object titleValue;if(!e.TryGetValue("session_title",out titleValue)&&!e.TryGetValue("thread_title",out titleValue))e.TryGetValue("title",out titleValue);string title=new CodexSessionTitles().Resolve(session,titleValue,Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName);
    var output=new{ title=title,protocol="conan-event-v1",@event=Convert.ToString(name),session=session,utc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture)};
    string dest=Path.Combine(folder,"conan-event-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(dest+".tmp",json.Serialize(output));File.Move(dest+".tmp",dest);
   }
  }catch(Exception){}
  finally{Console.Write("{}");}
  return 0;
 }
}
