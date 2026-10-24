using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
public static class EventProtocol {
 public static bool Named(string path){string name=Path.GetFileNameWithoutExtension(path);Guid id;if(name.StartsWith("conan-event-",StringComparison.Ordinal))name=name.Substring(12);return Guid.TryParseExact(name,"N",out id);}
 public static bool Read(string path,Dictionary<string,object> data,out string name,out string session,out DateTime utc){name=session=null;utc=default(DateTime);if(data==null||!Named(path))return false;object protocol,eventName,id,time;bool modern=Path.GetFileName(path).StartsWith("conan-event-",StringComparison.Ordinal);if(modern){if(!data.TryGetValue("protocol",out protocol)||!(protocol is string)||(string)protocol!="conan-event-v1")return false;}else if(data.Count!=3||!data.Keys.All(k=>k=="event"||k=="session"||k=="utc"))return false;if(!data.TryGetValue("event",out eventName)||!(eventName is string)||AssistantLogic.State((string)eventName)==null||!data.TryGetValue("session",out id)||!(id is string)||String.IsNullOrWhiteSpace((string)id)||((string)id).Length>128||!data.TryGetValue("utc",out time)||!(time is string)||!DateTime.TryParse((string)time,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out utc))return false;name=(string)eventName;session=(string)id;utc=utc.ToUniversalTime();return true;}
}
