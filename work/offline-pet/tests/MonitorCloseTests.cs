using System;
using System.IO;
using System.Reflection;
using System.Windows;
class MonitorCloseTests {
 [STAThread]static int Main(string[] args){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var card=new MonitorCard();Exception reentrant=null;card.Show();card.Activate();card.Closing+=(s,e)=>{try{typeof(Window).GetMethod("OnDeactivated",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(card,new object[]{EventArgs.Empty});}catch(TargetInvocationException error){reentrant=error.InnerException;}};card.Close();Console.WriteLine((reentrant==null?"PASS":"FAIL")+" deactivation during closing does not close again"+(reentrant==null?"":" : "+reentrant.GetType().Name+" "+reentrant.Message));app.Shutdown();Directory.CreateDirectory(args[0]);File.WriteAllText(Path.Combine(args[0],"close-results.json"),"{\"checks\":1,\"failures\":"+(reentrant==null?0:1)+"}");return reentrant==null?0:1;}
}
