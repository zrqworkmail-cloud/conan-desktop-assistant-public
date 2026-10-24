using System;using System.Windows;
partial class ConanPet {
 ConnectionWindow connections;
 void CheckStartupConnections(){if(Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1"||!assistant.Config.Monitor.StartupAuthorizationPrompt)return;System.Threading.Tasks.Task.Run(()=>{try{ConnectionStatus state;lock(assistant)state=ConnectionStatus.Inspect(assistant);if(state.NeedsAttention)Dispatcher.BeginInvoke(new Action(()=>{if(!shuttingDown)ShowConnections(state);}));}catch(Exception e){if(!(e is System.IO.IOException)&&!(e is UnauthorizedAccessException)&&!(e is ArgumentException))throw;}});}
 void ShowConnections(ConnectionStatus state){if(connections!=null){connections.Activate();return;}connections=new ConnectionWindow(state,dataHome,terminalTheme,assistant.Config.Monitor.StartupAuthorizationPrompt,value=>{lock(assistant){assistant.Config.Monitor.StartupAuthorizationPrompt=value;assistant.Save();}},()=>{OpenTerminal();terminal.ShowQuotaPage();},()=>{OpenTerminal();terminal.ShowSensorPage();}){Owner=this};connections.Closed+=(s,e)=>connections=null;connections.Show();}
}
