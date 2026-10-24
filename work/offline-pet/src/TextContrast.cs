using System;using System.Drawing;
static class TextContrast {
 static double Channel(byte c){double v=c/255.0;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
 public static double Luminance(Color c){return .2126*Channel(c.R)+.7152*Channel(c.G)+.0722*Channel(c.B);}
 public static double Ratio(Color a,Color b){double x=Luminance(a),y=Luminance(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
 static Color Mix(Color a,Color b,double t){return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
 public static Color Readable(Color seed,Color background){if(Ratio(seed,background)>=4.5)return seed;for(int i=1;i<=20;i++){var light=Mix(seed,Color.White,i/20.0);var dark=Mix(seed,Color.Black,i/20.0);double l=Ratio(light,background),d=Ratio(dark,background);if(l>=4.5||d>=4.5)return l>=d?light:dark;}return Ratio(Color.White,background)>=Ratio(Color.Black,background)?Color.White:Color.Black;}
 public static Color Fallback(TaskbarAppearance a){return a.Transparent||a.EffectiveBackgroundOpacity<1?(TaskbarAppearance.LightTheme()?Color.FromArgb(236,238,243):Color.FromArgb(32,37,46)):TaskbarAppearance.ParseColor(a.Background);}
 public static Color[] Background(Bitmap bitmap,Rectangle rect,TaskbarAppearance a){var result=new Color[Math.Max(0,rect.Width)];for(int x=0;x<result.Length;x++){int px=rect.X+x,py=rect.Y+rect.Height/2;result[x]=bitmap!=null&&px>=0&&py>=0&&px<bitmap.Width&&py<bitmap.Height?bitmap.GetPixel(px,py):Fallback(a);}return result;}
}
