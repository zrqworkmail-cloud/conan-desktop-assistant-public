using System;using System.IO;using System.Collections.Generic;using System.Threading;using Conan.PluginApi;
namespace Conan.Plugins.FileSearch {
public sealed class FileSearchBackend:IFileSearchBackend {
 readonly CancellationTokenSource stopped=new CancellationTokenSource();
 public string Id{get{return "file-search";}}
 public void Initialize(PluginContext context){}
 public void Dispose(){stopped.Cancel();}
 public FileSearchResult Search(string root,string query,CancellationToken token){using(var linked=CancellationTokenSource.CreateLinkedTokenSource(token,stopped.Token)){return Find(root,query,linked.Token);}}
 static FileSearchResult Find(string root,string query,CancellationToken token){
  if(!Directory.Exists(root))throw new ArgumentException("请选择存在的文件夹。");if(String.IsNullOrWhiteSpace(query))throw new ArgumentException("请输入文件名关键词。");
  var result=new FileSearchResult();var pending=new Stack<string>();pending.Push(root);
  while(pending.Count>0){token.ThrowIfCancellationRequested();var directory=pending.Pop();
   try{if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0){result.Skipped++;continue;}
    foreach(string file in Directory.EnumerateFiles(directory)){token.ThrowIfCancellationRequested();result.Scanned++;if(Path.GetFileName(file).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)result.Files.Add(file);if(result.Files.Count>=200||result.Scanned>=50000){result.Limited=true;return result;}}
    foreach(string child in Directory.EnumerateDirectories(directory)){token.ThrowIfCancellationRequested();if(pending.Count>=10000){result.Limited=true;return result;}pending.Push(child);}
   }catch(UnauthorizedAccessException){result.Skipped++;}catch(IOException){result.Skipped++;}
  }return result;
 }
}

}
