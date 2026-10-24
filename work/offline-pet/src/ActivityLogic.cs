using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
public static class ActivityLogic {
 public static string State(bool searching,bool busy,bool interacting,double idleSeconds){if(searching)return "search";if(busy||interacting)return "awake";return idleSeconds>=90?"sleep":"awake";}
}
public class FileSearchResult {public readonly List<string> Files=new List<string>();public int Scanned,Skipped;public bool Limited;}
public static class FileFinder {
 public static FileSearchResult Search(string root,string query,CancellationToken token){
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
