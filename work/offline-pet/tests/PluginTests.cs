using Conan.PluginApi;
using System;using System.IO;using System.IO.Packaging;using System.Linq;using System.Reflection;using System.Text;using System.Web.Script.Serialization;using System.Collections.Generic;
class PluginTests {
 static int checks,failures;static void Check(bool ok,string text){checks++;if(!ok)failures++;Console.WriteLine((ok?"PASS ":"FAIL ")+text);}
 static void Put(Package p,string path,byte[] data){var part=p.CreatePart(new Uri("/"+path,UriKind.Relative),"application/octet-stream");using(var stream=part.GetStream(FileMode.Create,FileAccess.Write))stream.Write(data,0,data.Length);}
 static void Make(string file,PluginManifest m,byte[] dll){using(var p=Package.Open(file,FileMode.Create)){Put(p,"manifest.json",Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(m)));if(dll!=null){Put(p,m.EntryAssembly,dll);if(m.Files.ContainsKey("bridge/read-hardware.ps1"))Put(p,"bridge/read-hardware.ps1",new byte[0]);}}}
 static bool Reject(Action action){try{action();return false;}catch(InvalidDataException){return true;}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}}
 [STAThread]static int Main(string[] args){
  string folder=args[0],package=args[1];Directory.CreateDirectory(folder);var manager=new PluginManager(folder);var proxy=new Telemetry(manager);var json=new JavaScriptSerializer();
  try{
   var empty=proxy.Sample(36,8,16);Check(!empty.Cpu.HasValue&&empty.MemoryTotal==0,"absent package reports unavailable, never fake CPU or memory");
   Check(manager.List().Length==0&&!proxy.Available,"core starts without collector assembly");
   var descriptor=PluginManager.Inspect(package);Check(descriptor.Id=="system-monitor"&&descriptor.ApiVersion==1,"standalone package manifest inspected");
   string legacyPackage=Path.Combine(folder,"legacy-monitor.conanplugin");File.Copy(package,legacyPackage,true);using(var legacy=Package.Open(legacyPackage,FileMode.Open,FileAccess.ReadWrite)){var part=legacy.GetPart(new Uri("/manifest.json",UriKind.Relative));var older=json.Deserialize<PluginManifest>(json.Serialize(descriptor));older.Version="1.0.0";using(var stream=part.GetStream(FileMode.Create,FileAccess.Write)){byte[] bytes=Encoding.UTF8.GetBytes(json.Serialize(older));stream.Write(bytes,0,bytes.Length);}}foreach(bool enabled in new[]{false,true}){string migrationFolder=Path.Combine(folder,"migration-"+enabled);var migrationProbe=new PluginManager(migrationFolder);migrationProbe.Install(legacyPackage);if(enabled)migrationProbe.SetEnabled("system-monitor",true);string userData=Path.Combine(migrationFolder,"plugins","data","system-monitor");Directory.CreateDirectory(userData);File.WriteAllText(Path.Combine(userData,"fixture.txt"),"keep");Check(migrationProbe.UpgradeBundledMonitor(package,PluginManager.Hash(File.ReadAllBytes(package))),"verified bundled monitor upgrade succeeds "+enabled);Check(migrationProbe.List().Single().Manifest.Version=="1.0.1"&&migrationProbe.Enabled("system-monitor")==enabled,"monitor upgrade preserves enabled state "+enabled);Check(File.ReadAllText(Path.Combine(userData,"fixture.txt"))=="keep"&&Directory.Exists(Path.Combine(migrationFolder,"plugins","installed","system-monitor","1.0.0")),"monitor upgrade preserves data and previous version "+enabled);migrationProbe.Dispose();}
   manager.Install(package);Check(manager.List().Length==1&&!manager.Enabled("system-monitor"),"installation stays disabled until user enables it");
   Check(manager.List()[0].Running==false,"install does not instantiate collector");
   manager.SetEnabled("system-monitor",true);var backend=manager.Service<ITelemetryBackend>("system-monitor");
   Check(backend!=null&&backend.GetType().Assembly!=typeof(Telemetry).Assembly,"collector code loads from a separate DLL");
   Check(!backend.GetType().Assembly.GetReferencedAssemblies().Any(a=>a.Name=="ConanPet"),"plugin references the SDK rather than the host executable");Check(manager.List()[0].Running&&proxy.Available,"enabled plugin is available to host");
   var reading=json.Deserialize<SystemSample>(backend.Sample(37,8,16));Check(reading.Cpu==37&&reading.MemoryUsed==8&&reading.MemoryTotal==16,"external collector returns data through the host contract");System.Threading.Thread.Sleep(1800);var hardware=json.Deserialize<SystemSample>(backend.Sample(37,8,16));Console.WriteLine("HARDWARE "+json.Serialize(new{hardware.Gpu,hardware.GpuName,hardware.GpuState,hardware.DiskBusy,hardware.CpuTemperature}));var oldType=backend.GetType();manager.SetEnabled("system-monitor",false);
   Check((bool)oldType.GetField("disposed",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(backend),"disable disposes original collector");
   Check(!proxy.Available&&!manager.List()[0].Running&&!proxy.Sample(90,8,16).Cpu.HasValue,"disabled plugin cannot continue sampling through host");
   manager.SetEnabled("system-monitor",true);var resumed=manager.Service<ITelemetryBackend>("system-monitor");
   Check(resumed!=null&&!Object.ReferenceEquals(backend,resumed)&&resumed.GetType()==oldType,"re-enable creates fresh collector without repeatedly loading assemblies");
   var restart=new PluginManager(folder);Check(restart.Enabled("system-monitor"),"enabled choice survives profile reload");restart.Dispose();
   manager.SetEnabled("system-monitor",false);
   string installed=Path.Combine(folder,"plugins","installed","system-monitor","1.0.1","lib","Conan.SystemMonitor.dll");byte[] original=File.ReadAllBytes(installed);File.AppendAllText(installed,"tamper");
   manager.SetEnabled("system-monitor",true);Check(manager.Service<ITelemetryBackend>("system-monitor")==null&&manager.List()[0].Error.Contains("修改"),"tampered installed DLL rejected before activation");
   manager.SetEnabled("system-monitor",false);File.WriteAllBytes(installed,original);
   var invalid=json.Deserialize<PluginManifest>(json.Serialize(descriptor));invalid.Files.Clear();invalid.Files["../escape.dll"]=new string('0',64);string path=Path.Combine(folder,"traversal.conanplugin");Make(path,invalid,null);
   Check(Reject(()=>manager.Install(path)),"traversal manifest refused");
   Check(!File.Exists(Path.Combine(folder,"escape.dll")),"installer never writes outside its plugin directory");
   invalid=json.Deserialize<PluginManifest>(json.Serialize(descriptor));invalid.ApiVersion=99;path=Path.Combine(folder,"api.conanplugin");Make(path,invalid,original);
   Check(Reject(()=>manager.Install(path)),"incompatible API refused");
   invalid=json.Deserialize<PluginManifest>(json.Serialize(descriptor));invalid.Files[invalid.EntryAssembly]=new string('0',64);path=Path.Combine(folder,"hash.conanplugin");Make(path,invalid,original);
   Check(Reject(()=>manager.Install(path)),"package hash mismatch refused");
   Check(!Directory.GetDirectories(Path.Combine(folder,"plugins"),"stage-*").Any(),"failed installs leave no staged payloads");
   Check(Reject(()=>manager.Install(package)),"duplicate version cannot silently overwrite installation");
   Check(Reject(()=>manager.Uninstall("../escape")),"uninstall rejects invalid identifiers");
   string settings=Path.Combine(folder,"assistant.json");File.WriteAllText(settings,"retained-user-settings");manager.Uninstall("system-monitor");
   Check(manager.List().Length==0&&!Directory.Exists(Path.Combine(folder,"plugins","installed","system-monitor")),"uninstall removes independent payload and registration");
   Check(File.ReadAllText(settings)=="retained-user-settings","uninstall preserves unrelated user settings");
   Check(Reject(()=>PluginManager.Download("http://example.invalid/plugin",Path.Combine(folder,"download"))),"insecure download URL refused before request");
   manager.Install(package);manager.SetEnabled("system-monitor",true);Check(manager.Service<ITelemetryBackend>("system-monitor")!=null,"reinstall restores functional package");
   var available=PluginCatalog.Bundled(args[4]);Check(available.Length==3&&PluginCatalog.Search(available,"文件搜索").Single().Id=="file-search","bundled catalog verified and searchable");var official=available.Single(x=>x.Id=="file-search");PluginCatalog.VerifyDownloaded(official,official.Package);Check(true,"catalog package identity and hash verified");var bad=new CatalogEntry{Id=official.Id,Version=official.Version,Sha256=new string('0',64)};Check(Reject(()=>PluginCatalog.VerifyDownloaded(bad,official.Package)),"catalog hash mismatch rejects install");var remoteJson=json.Serialize(new CatalogDocument{Plugins=new List<CatalogEntry>{new CatalogEntry{Id="file-search",Name="Search",Version="2.0.0",Description="test",Package="https://example.com/search.conanplugin",Sha256=new string('a',64)}}});var remote=PluginCatalog.ParseRemote(remoteJson);Check(remote.Length==1&&!remote[0].Local&&PluginCatalog.UpdateAvailable(remote[0],new[]{new PluginRow{Manifest=new PluginManifest{Id="file-search",Version="1.0.0"}}}),"remote catalog detects version update");Check(Reject(()=>PluginCatalog.ParseRemote(remoteJson.Replace("https://","http://"))),"remote catalog rejects insecure package links");
   string searchPackage=args[3];var searchDescriptor=PluginManager.Inspect(searchPackage);Check(searchDescriptor.Id=="file-search","file search independent package inspected");
   manager.Install(searchPackage);Check(!manager.Enabled("file-search"),"file search installs disabled");manager.SetEnabled("file-search",true);
   var search=manager.Service<IFileSearchBackend>("file-search");string searchFixture=Path.Combine(folder,"fixture");Directory.CreateDirectory(Path.Combine(searchFixture,"nested"));File.WriteAllText(Path.Combine(searchFixture,"nested","Report.txt"),"test");
   Check(search!=null&&search.GetType().Assembly!=typeof(Telemetry).Assembly,"search engine loaded from independent DLL");Check(search.Search(searchFixture,"report",System.Threading.CancellationToken.None).Files.Count==1,"search finds filenames recursively");
   manager.SetEnabled("file-search",false);bool searchStopped=false;try{search.Search(searchFixture,"report",System.Threading.CancellationToken.None);}catch(OperationCanceledException){searchStopped=true;}Check(searchStopped&&manager.Service<IFileSearchBackend>("file-search")==null,"disabling search cancels backend and prevents new searches");var updated=json.Deserialize<PluginManifest>(json.Serialize(searchDescriptor));updated.Version="1.0.1";string updatedPackage=Path.Combine(folder,"search-update.conanplugin");byte[] searchCode=File.ReadAllBytes(Path.Combine(folder,"plugins","installed","file-search","1.0.0","lib","Conan.FileSearch.dll"));Make(updatedPackage,updated,searchCode);manager.Install(updatedPackage);var updatedRow=manager.List().Single(x=>x.Manifest.Id=="file-search");Check(!updatedRow.RestartRequired&&!updatedRow.Enabled&&updatedRow.Manifest.Version=="1.0.1","installing update preserves profile");manager.SetEnabled("file-search",true);Check(manager.Service<IFileSearchBackend>("file-search")!=null,"updated plugin can be enabled without restart");manager.Uninstall("file-search");Check(!Directory.Exists(Path.Combine(folder,"plugins","installed","file-search")),"search package uninstalls independently");
   string migratedSearch=Path.Combine(folder,"search-migration");var migration=new PluginManager(migratedSearch);Check(migration.BootstrapLegacyPackage(searchPackage,PluginManager.Hash(File.ReadAllBytes(searchPackage)),"file-search",true)&&migration.Enabled("file-search"),"legacy file search restores from verified package");migration.Uninstall("file-search");Check(!migration.BootstrapLegacyPackage(searchPackage,PluginManager.Hash(File.ReadAllBytes(searchPackage)),"file-search",false),"search is not reinstalled after explicit uninstall");migration.Dispose();
   string bootstrapFolder=Path.Combine(folder,"bootstrap");var bootstrap=new PluginManager(bootstrapFolder);string packageHash=PluginManager.Hash(File.ReadAllBytes(package));
   Check(!bootstrap.BootstrapMonitor(package,packageHash,false)&&!bootstrap.HasProfile,"new installs do not silently enable bundled monitoring");
   Check(!bootstrap.BootstrapMonitor(package,new string('0',64),true)&&!bootstrap.HasProfile,"changed bundled package cannot be auto-installed");
   Check(bootstrap.BootstrapMonitor(package,packageHash,true)&&bootstrap.Enabled("system-monitor"),"legacy settings migrate the verified bundled monitor");
   bootstrap.Uninstall("system-monitor");bootstrap.Dispose();var reboot=new PluginManager(bootstrapFolder);
   Check(!reboot.BootstrapMonitor(package,packageHash,true)&&reboot.List().Length==0,"explicit uninstall is not undone at next startup");reboot.Dispose();
   string corrupt=Path.Combine(folder,"corrupt");Directory.CreateDirectory(Path.Combine(corrupt,"plugins"));string corruptFile=Path.Combine(corrupt,"plugins","plugins.json");File.WriteAllText(corruptFile,"{");
   var damaged=new PluginManager(corrupt);Check(!String.IsNullOrEmpty(damaged.ProfileError)&&damaged.List().Length==0,"damaged plugin registry does not crash host startup");
   Check(Reject(()=>damaged.Install(package))&&File.ReadAllText(corruptFile)=="{","damaged registry remains untouched and cannot be silently overwritten");damaged.Dispose();
   string concurrent=Path.Combine(folder,"concurrent");var guard=new PluginManager(concurrent);
   byte[] fixture=File.ReadAllBytes(args[2]);var blocking=new PluginManifest{Id="system-monitor",Version="1.0.0",Name="blocking test",Description="",EntryAssembly="lib/block.dll",EntryType="BlockingMonitorPlugin"};
   blocking.Files[blocking.EntryAssembly]=PluginManager.Hash(fixture);string blockingPackage=Path.Combine(folder,"blocking.conanplugin");Make(blockingPackage,blocking,fixture);
   guard.Install(blockingPackage);guard.SetEnabled("system-monitor",true);var blockingBackend=guard.Service<ITelemetryBackend>("system-monitor");var blockingType=blockingBackend.GetType();
   var entered=(System.Threading.ManualResetEvent)blockingType.GetField("Entered").GetValue(null);var release=(System.Threading.ManualResetEvent)blockingType.GetField("Release").GetValue(null);
   try{
    var guardedProxy=new Telemetry(guard);var sampleTask=System.Threading.Tasks.Task.Run(()=>guardedProxy.Sample(37,8,16));
    Check(entered.WaitOne(3000),"in-flight sampling fixture entered");
    var stopTask=System.Threading.Tasks.Task.Run(()=>guard.SetEnabled("system-monitor",false));System.Threading.Thread.Sleep(150);
    Check(!stopTask.IsCompleted,"disable waits for an in-flight host sampling call");
    release.Set();Check(System.Threading.Tasks.Task.WaitAll(new System.Threading.Tasks.Task[]{sampleTask,stopTask},5000),"sampling and disable finish without deadlock");
    Check((bool)blockingType.GetField("Disposed").GetValue(blockingBackend)&&!guardedProxy.Available,"disable completes disposal before returning");guardedProxy.Dispose();
   }finally{release.Set();guard.Dispose();}

  }catch(Exception error){failures++;Console.WriteLine(error);}
  finally{proxy.Dispose();manager.Dispose();}
  File.WriteAllText(Path.Combine(folder,"plugin-results.json"),json.Serialize(new{checks,failures,realUserSettingsChanged=false}));
  return failures==0?0:1;
 }
}
