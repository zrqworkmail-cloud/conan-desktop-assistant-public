using System;using System.Collections;using System.Collections.Generic;using System.Linq;using System.Web.Script.Serialization;using Conan.PluginApi;
namespace Conan.Plugins.TextTools {
 public sealed class TextToolsBackend:ITextToolsBackend {
  bool disposed;public string Id{get{return "text-tools";}}public void Initialize(PluginContext context){}public void Dispose(){disposed=true;}
  public string Process(string operation,string text){if(disposed)throw new ObjectDisposedException("text-tools");if(text==null)text="";if(text.Length>100000)throw new ArgumentException("文本工具最多处理10万字符。");switch(operation){case "clean":return Lines(text,false);case "unique":return Lines(text,true);case "upper":return text.ToUpperInvariant();case "lower":return text.ToLowerInvariant();case "json":var json=new JavaScriptSerializer();return Format(json.DeserializeObject(text),0,json);default:throw new ArgumentException("不支持的文本操作。");}}
  static string Lines(string text,bool unique){var lines=text.Replace("\r\n","\n").Replace('\r','\n').Split('\n').Select(x=>x.Trim()).Where(x=>x.Length>0);return String.Join(Environment.NewLine,(unique?lines.Distinct(StringComparer.Ordinal):lines).ToArray());}
  static string Format(object value,int level,JavaScriptSerializer json){string pad=new string(' ',level*2),inner=pad+"  ";var map=value as IDictionary<string,object>;if(map!=null)return map.Count==0?"{}":"{\n"+String.Join(",\n",map.Select(p=>inner+json.Serialize(p.Key)+": "+Format(p.Value,level+1,json)).ToArray())+"\n"+pad+"}";var list=value as IList;if(list!=null)return list.Count==0?"[]":"[\n"+String.Join(",\n",list.Cast<object>().Select(x=>inner+Format(x,level+1,json)).ToArray())+"\n"+pad+"]";return json.Serialize(value);}
 }
}
