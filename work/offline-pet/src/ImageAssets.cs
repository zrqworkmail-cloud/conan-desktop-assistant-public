using System.IO;
internal static class ImageAssets {
 public static string Resolve(string home,string name){string path=Path.Combine(home,"images",name);return File.Exists(path)?path:Path.Combine(home,name);}
}
