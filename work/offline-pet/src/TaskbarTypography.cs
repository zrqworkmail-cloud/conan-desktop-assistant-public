using System;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using F=System.Windows.Forms;
sealed class TaskbarTypography:IDisposable {
 public readonly Font Cjk,Latin;readonly bool same;
 public TaskbarTypography(TaskbarAppearance a,double dpi){Cjk=TaskbarPainter.Font(a,dpi);Latin=new Font(String.IsNullOrWhiteSpace(a.LatinFontName)?a.FontName:a.LatinFontName,(float)((a.LatinFontSize==0?a.FontSize:a.LatinFontSize)*96/72*dpi),((a.LatinBold??a.Bold)?FontStyle.Bold:FontStyle.Regular)|((a.LatinItalic??a.Italic)?FontStyle.Italic:FontStyle.Regular),GraphicsUnit.Pixel);same=Cjk.Name==Latin.Name&&Cjk.Size==Latin.Size&&Cjk.Style==Latin.Style;}
 IEnumerable<Tuple<string,Font>> Runs(string text){if(same){yield return Tuple.Create(text,Cjk);yield break;}int start=0;for(int i=1;i<=text.Length;i++)if(i==text.Length||(text[i]>=0x2e80)!=(text[start]>=0x2e80)){yield return Tuple.Create(text.Substring(start,i-start),text[start]>=0x2e80?Cjk:Latin);start=i;}}
 public int Measure(Graphics g,string text){return Runs(text??"").Sum(run=>TaskbarPainter.Measure(g,run.Item1,run.Item2));}
 public int Height(Graphics g){var flags=F.TextFormatFlags.NoPadding|F.TextFormatFlags.SingleLine;return Math.Max(F.TextRenderer.MeasureText(g,"国Ag",Cjk,new Size(Int32.MaxValue,Int32.MaxValue),flags).Height,F.TextRenderer.MeasureText(g,"Ag",Latin,new Size(Int32.MaxValue,Int32.MaxValue),flags).Height);}
 public void Draw(Graphics g,string text,Rectangle rect,Color color,F.TextFormatFlags flags,bool blackBackground){
  if(same){if(blackBackground)F.TextRenderer.DrawText(g,text,Cjk,rect,color,Color.Black,flags);else F.TextRenderer.DrawText(g,text,Cjk,rect,color,flags);return;}
  text=text??"";if(Measure(g,text)>rect.Width&&(flags&F.TextFormatFlags.EndEllipsis)!=0){string value=text;while(value.Length>0&&Measure(g,value+"…")>rect.Width){int remove=Char.IsLowSurrogate(value[value.Length-1])&&value.Length>1?2:1;value=value.Substring(0,value.Length-remove);}text=value+"…";}
  int total=Measure(g,text),x=rect.X+((flags&F.TextFormatFlags.HorizontalCenter)!=0?(rect.Width-total)/2:(flags&F.TextFormatFlags.Right)!=0?rect.Width-total:0);var saved=g.Save();try{g.SetClip(rect,CombineMode.Intersect);var runFlags=flags&~(F.TextFormatFlags.HorizontalCenter|F.TextFormatFlags.Right|F.TextFormatFlags.EndEllipsis);foreach(var run in Runs(text)){int width=TaskbarPainter.Measure(g,run.Item1,run.Item2);var box=new Rectangle(x,rect.Y,width,rect.Height);if(blackBackground)F.TextRenderer.DrawText(g,run.Item1,run.Item2,box,color,Color.Black,runFlags);else F.TextRenderer.DrawText(g,run.Item1,run.Item2,box,color,runFlags);x+=width;}}finally{g.Restore(saved);}
 }
 public void Dispose(){Cjk.Dispose();Latin.Dispose();}
}
