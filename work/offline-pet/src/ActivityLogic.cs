using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
public static class ActivityLogic {
 public static string State(bool searching,bool busy,bool interacting,double idleSeconds){if(searching)return "search";if(busy||interacting)return "awake";return idleSeconds>=90?"sleep":"awake";}
}
