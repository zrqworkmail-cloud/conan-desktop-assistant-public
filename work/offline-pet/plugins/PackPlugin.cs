using System;using System.IO;using System.IO.Packaging;using System.Collections.Generic;using System.Web.Script.Serialization;using System.Text;using System.Security.Cryptography;
class PackageManifest {
 public int ApiVersion=1;public string Id,Version,Name,Description,EntryAssembly,EntryType;
 public List<string> Capabilities=new List<string>();public Dictionary<string,string> Files=new Dictionary<string,string>();
}
class PackPlugin {
 static string Hash(byte[] value){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(value)).Replace("-","").ToLowerInvariant();}
 static void Put(Package package,string path,byte[] bytes){var part=package.CreatePart(new Uri("/"+path,UriKind.Relative),"application/octet-stream",CompressionOption.Normal);using(var stream=part.GetStream(FileMode.Create,FileAccess.Write))stream.Write(bytes,0,bytes.Length);}
 static int Main(string[] args){
  if(args.Length==3&&args[2]=="--text-tools"){
   byte[] payload=File.ReadAllBytes(args[0]);var m=new PackageManifest{Id="text-tools",Version="1.0.0",Name="文本处理",Description="文本清理、逐行去重、大小写转换与 JSON 格式化。",EntryAssembly="lib/Conan.TextTools.dll",EntryType="Conan.Plugins.TextTools.TextToolsBackend",Capabilities=new List<string>{"处理用户在文本页面输入的内容"}};m.Files[m.EntryAssembly]=Hash(payload);
   using(var package=Package.Open(args[1],FileMode.Create)){Put(package,"manifest.json",Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(m)));Put(package,m.EntryAssembly,payload);}return 0;
  }
  if(args.Length==3&&args[2]=="--file-search"){
   byte[] payload=File.ReadAllBytes(args[0]);var m=new PackageManifest{Id="file-search",Version="1.0.0",Name="文件搜索",Description="在所选文件夹中按名称搜索，停用时取消搜索。",EntryAssembly="lib/Conan.FileSearch.dll",EntryType="Conan.Plugins.FileSearch.FileSearchBackend",Capabilities=new List<string>{"读取所选目录的文件名"}};m.Files[m.EntryAssembly]=Hash(payload);
   using(var package=Package.Open(args[1],FileMode.Create)){Put(package,"manifest.json",Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(m)));Put(package,m.EntryAssembly,payload);}return 0;
  }
  byte[] dll=File.ReadAllBytes(args[0]),bridge=File.ReadAllBytes(args[2]);
  var manifest=new PackageManifest{Id="system-monitor",Version="1.0.1",Name="系统监控",Description="CPU、GPU、温度、磁盘、内存和网速采集。停用后停止该包的采集，不修改任务栏配色。",EntryAssembly="lib/Conan.SystemMonitor.dll",EntryType="Conan.Plugins.SystemMonitor.NativeTelemetry",Capabilities=new List<string>{"读取系统性能","读取网卡计数器","读取本机温度日志","调用本机显卡驱动工具"}};
  manifest.Files[manifest.EntryAssembly]=Hash(dll);manifest.Files["bridge/read-hardware.ps1"]=Hash(bridge);
  using(var package=Package.Open(args[1],FileMode.Create)){Put(package,"manifest.json",Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(manifest)));Put(package,manifest.EntryAssembly,dll);Put(package,"bridge/read-hardware.ps1",bridge);}return 0;
 }
}
