using System;using System.Reflection;
[assembly:AssemblyVersion("1.0.0.0")]
namespace Conan.PluginApi {
 public sealed class PluginContext {public string InstallDirectory,DataDirectory;}
 public interface IConanPlugin:IDisposable {string Id{get;}void Initialize(PluginContext context);}
 public sealed class FileSearchResult {public System.Collections.Generic.List<string> Files=new System.Collections.Generic.List<string>();public int Scanned,Skipped;public bool Limited;}
 public interface IFileSearchBackend:IConanPlugin {FileSearchResult Search(string root,string query,System.Threading.CancellationToken token);}
 public interface ITextToolsBackend:IConanPlugin {string Process(string operation,string text);}
 public interface ITelemetryBackend:IConanPlugin {
  bool ProcessReportingEnabled{get;set;}int RefreshMilliseconds{get;set;}string SelectedNetworkId{get;set;}
  string ProcessSnapshotJson{get;}
  string Sample(double? cpu,double used,double total);
 }
}
