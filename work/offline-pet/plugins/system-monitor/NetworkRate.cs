namespace Conan.Plugins.SystemMonitor {
using Conan.PluginApi;
using System;
public struct NetworkSpeed {public double Download,Upload;}
public class NetworkRate {
 string previousId;long received,sent;double time;
 public NetworkSpeed? Sample(string id,long rx,long tx,double seconds){
  bool valid=previousId==id&&seconds>time&&seconds-time<=15&&rx>=received&&tx>=sent;NetworkSpeed? result=valid?(NetworkSpeed?)new NetworkSpeed{Download=(rx-received)/(seconds-time),Upload=(tx-sent)/(seconds-time)}:null;
  previousId=id;received=rx;sent=tx;time=seconds;return result;
 }
}

}
