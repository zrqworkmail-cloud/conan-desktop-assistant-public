using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
public class SpriteActionSpec {public string Id,File,Label;public int Frames;}
public class SpriteActionCatalog {public List<SpriteActionSpec> Actions=new List<SpriteActionSpec>();}
class SpriteActions {
 readonly Dictionary<string,BitmapSource[]> animations=new Dictionary<string,BitmapSource[]>();
 public SpriteActions(string home){string path=Path.Combine(home,"signature-actions.json");if(!File.Exists(path))return;var catalog=new JavaScriptSerializer().Deserialize<SpriteActionCatalog>(File.ReadAllText(path));foreach(var action in catalog.Actions){if(Path.GetFileName(action.File)!=action.File||action.Frames<2||action.Frames>32)throw new InvalidDataException("动作图集配置无效");var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri(Path.Combine(home,action.File));image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();if(image.PixelWidth%action.Frames!=0)throw new InvalidDataException("动作图集帧宽无效");var frames=new BitmapSource[action.Frames];for(int i=0;i<frames.Length;i++){var crop=new CroppedBitmap(image,new Int32Rect(i*image.PixelWidth/action.Frames,0,image.PixelWidth/action.Frames,image.PixelHeight));var frame=new FormatConvertedBitmap(crop,PixelFormats.Bgra32,null,0);frame.Freeze();frames[i]=frame;}animations.Add(action.Id,frames);}}
 public bool Has(string id){return id!=null&&animations.ContainsKey(id);}
 public int Count(string id){return animations.ContainsKey(id)?animations[id].Length:0;}
 public BitmapSource MovingFrame(double elapsed){var frames=animations["skate"];return frames[Math.Min(frames.Length-1,ActionTiming.MovingSkate(elapsed))];}
 public BitmapSource PlaybackFrame(string id,double elapsed,double duration,int count,string emotion){var frames=animations[id];if(id=="count"&&count>=1&&count<=20)return frames[elapsed<.16?0:elapsed>=duration-.16?frames.Length-1:Math.Min(frames.Length-1,ActionTiming.NumberPose(count))];if(id=="alert"&&emotion!=null){int[] sequence=emotion=="懊恼"?new[]{0,3,4,5}:new[]{0,1,2,1,0};return frames[Math.Min(frames.Length-1,sequence[ActionTiming.OneShot(sequence.Length,elapsed,duration)])];}return frames[ActionTiming.OneShot(frames.Length,elapsed,duration)];}
 public BitmapSource Frame(string id,double progress,int count=0,string emotion=null){var frames=animations[id];if(id=="count"&&count>=1&&count<=20)return frames[Math.Min(frames.Length-1,ActionTiming.NumberPose(count))];if(id=="alert"&&emotion=="懊恼")return frames[3+Math.Min(2,Math.Max(0,(int)(progress*3)))];if(id=="alert"&&emotion=="警觉")return frames[Math.Min(2,Math.Max(0,(int)(progress*3)))];return frames[Math.Min(frames.Length-1,Math.Max(0,(int)(progress*frames.Length)))];}
}
