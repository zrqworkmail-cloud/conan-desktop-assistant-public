using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
class SpeechBubble:Window {
 readonly Canvas surface=new Canvas();readonly Border card=new Border();readonly TextBlock heading=new TextBlock(),body=new TextBlock(),hint=new TextBlock();
 readonly WrapPanel footer=new WrapPanel();readonly Polygon tail=new Polygon();
 readonly Brush background=new SolidColorBrush(Color.FromRgb(255,253,248)),edge=new SolidColorBrush(Color.FromRgb(211,220,230));
 readonly Button details=new Button(),expand=new Button();readonly ScrollViewer bodyScroll=new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};bool expanded;string lastAppearance="",lastSide;public BubbleAppearance Options=new BubbleAppearance();public Rect? AnchorBody;BubbleMessage shown;public event Action Dismissed,Details;
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool SetWindowText(IntPtr h,string text);
 [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int n);
 [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr h,int n,int value);
 public SpeechBubble(){
  Title="柯南 · 消息气泡";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;
  ShowInTaskbar=false;ShowActivated=false;Topmost=true;Focusable=false;UseLayoutRounding=true;SnapsToDevicePixels=true;
  SourceInitialized+=(s,e)=>{var h=new WindowInteropHelper(this).Handle;SetWindowLong(h,-20,GetWindowLong(h,-20)|0x08000000);HwndSource.FromHwnd(h).AddHook(NoActivate);};
  Loaded+=(s,e)=>SetWindowText(new WindowInteropHelper(this).Handle,Title);
  var content=new StackPanel();card.Child=content;card.Background=background;card.BorderBrush=edge;card.BorderThickness=new Thickness(1);card.CornerRadius=new CornerRadius(18);card.Padding=new Thickness(17,13,17,13);
  card.Effect=new DropShadowEffect{BlurRadius=18,ShadowDepth=3,Opacity=0.18,Color=Colors.Black};
  var header=new DockPanel{LastChildFill=true};content.Children.Add(header);
  var close=new Button{Content="×",Width=24,Height=24,FontSize=18,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Foreground=Brushes.SlateGray,Focusable=false,Cursor=System.Windows.Input.Cursors.Hand,ToolTip="关闭这条消息"};
  System.Windows.Automation.AutomationProperties.SetName(close,"关闭气泡");DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);close.Click+=(s,e)=>{if(Dismissed!=null)Dismissed();};
  var badge=new Border{Child=TerminalStyle.Icon("bow",14),Width=24,Height=24,CornerRadius=new CornerRadius(7),Margin=new Thickness(0,0,8,0),Background=TerminalStyle.BrushOf("#E9EEF7")};DockPanel.SetDock(badge,Dock.Left);header.Children.Add(badge);heading.FontSize=12;heading.FontWeight=FontWeights.SemiBold;heading.VerticalAlignment=VerticalAlignment.Center;header.Children.Add(heading);
  body.FontSize=14;body.LineHeight=22;body.Foreground=new SolidColorBrush(Color.FromRgb(34,46,66));body.TextWrapping=TextWrapping.Wrap;body.TextTrimming=TextTrimming.CharacterEllipsis;body.Margin=new Thickness(0,4,0,0);bodyScroll.Content=body;bodyScroll.Margin=new Thickness(0,5,0,0);content.Children.Add(bodyScroll);
  footer.Margin=new Thickness(0,9,0,0);content.Children.Add(footer);hint.FontSize=10;hint.Foreground=Brushes.SlateGray;hint.VerticalAlignment=VerticalAlignment.Center;footer.Children.Add(hint);
  details.Content="查看详情";details.FontSize=11;details.Padding=new Thickness(7,1,7,1);details.Margin=new Thickness(10,0,0,0);details.Background=Brushes.Transparent;details.BorderThickness=new Thickness(0);details.Foreground=new SolidColorBrush(Color.FromRgb(42,94,148));details.Focusable=false;details.Cursor=System.Windows.Input.Cursors.Hand;System.Windows.Automation.AutomationProperties.SetName(details,"查看详情");footer.Children.Add(details);expand.Content="展开全文";expand.FontSize=11;expand.Margin=new Thickness(8,0,0,0);expand.Padding=new Thickness(7,2,7,2);expand.Focusable=false;expand.Click+=(s,e)=>{expanded=!expanded;lastAppearance="";};footer.Children.Add(expand);details.Click+=(s,e)=>{if(Details!=null)Details();};
  FontFamily=new FontFamily("Microsoft YaHei UI");tail.Fill=background;tail.Stroke=edge;tail.StrokeThickness=1;tail.IsHitTestVisible=false;
  surface.Children.Add(card);surface.Children.Add(tail);Content=surface;
 }
 IntPtr NoActivate(IntPtr h,int message,IntPtr w,IntPtr l,ref bool handled){if(message==0x21){handled=true;return new IntPtr(3);}return IntPtr.Zero;}
 public void Render(BubbleMessage message,int pending,double anchorX,double headY,double feetY,Rect area){
  if(message==null){shown=null;if(IsVisible)Hide();return;}
  bool newMessage=shown!=message;if(newMessage)expanded=false;shown=message;var options=Options??new BubbleAppearance();int font=Math.Max(12,Math.Min(20,options.FontSize));string appearance=options.Style+":"+font+":"+options.Width+":"+expanded+(message.CountdownUtc.HasValue?":"+(DateTime.UtcNow.Ticks/TimeSpan.TicksPerSecond):"");bool changed=newMessage||lastAppearance!=appearance;lastAppearance=appearance;
  string text=BubbleLogic.Readable(message.DisplayText(DateTime.UtcNow));if(changed){
   bool dark=options.Style=="blue",kid=options.Style=="kid";Brush fill=TerminalStyle.BrushOf(dark?"#14263D":kid?"#F5F7FF":"#FFFDF6"),line=TerminalStyle.BrushOf(dark?"#416483":kid?"#C6D0EB":"#D5DCE4"),ink=TerminalStyle.BrushOf(dark?"#F0F5FC":"#24354B"),muted=TerminalStyle.BrushOf(dark?"#A7BBD1":"#64758A"),accent=TerminalStyle.BrushOf(dark?"#89D8ED":kid?"#6574BD":"#AE4857");card.Background=fill;card.BorderBrush=line;tail.Fill=fill;tail.Stroke=line;heading.Foreground=accent;body.Foreground=ink;hint.Foreground=muted;details.Foreground=expand.Foreground=accent;expand.Background=TerminalStyle.BrushOf(dark?"#203D55":"#EAF0F7");expand.BorderThickness=new Thickness(0);
   heading.Text=BubbleLogic.Heading(message);body.FontSize=font;body.LineHeight=Math.Round(font*1.6);body.TextTrimming=TextTrimming.None;body.Text=expanded?text:BubbleLogic.Preview(text);body.MaxHeight=Double.PositiveInfinity;
   details.Visibility=message.Notification?Visibility.Visible:Visibility.Collapsed;details.Content=message.Key!=null&&message.Key.StartsWith("codex:")?"打开 Codex →":message.Key!=null&&message.Key.StartsWith("network")?"网络设置 →":"查看详情 →";
  }
  hint.Text=pending>0?"另有 "+pending+" 条消息":"悬停暂停";footer.Visibility=Visibility.Visible;
  Width=Math.Max(80,Math.Min(Math.Max(240,Math.Min(460,options.Width)),area.Width-16));card.Width=Width-24;body.Width=Math.Max(20,card.Width-36);bodyScroll.MaxHeight=Math.Max(font*1.6,Math.Min(expanded?360:font*1.6*6,area.Height-150));bodyScroll.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;body.Measure(new Size(body.Width,Double.PositiveInfinity));expand.Visibility=expanded||text!=BubbleLogic.Preview(text)||body.DesiredSize.Height>bodyScroll.MaxHeight?Visibility.Visible:Visibility.Collapsed;expand.Content=expanded?"收起":"展开全文";
  card.Measure(new Size(card.Width,Double.PositiveInfinity));double cardHeight=card.DesiredSize.Height;Height=cardHeight+24;var anchor=AnchorBody??new Rect(anchorX-50,headY,100,Math.Max(20,feetY-headY));
  var p=BubbleLogic.Arrange(anchor.Left,anchor.Top,anchor.Width,anchor.Height,Width,Height,area.Left,area.Top,area.Right,area.Bottom,options.Position,lastSide);lastSide=p.Side;Left=p.X;Top=p.Y;Canvas.SetLeft(card,12);Canvas.SetTop(card,12);
  if(p.Side.EndsWith("right")){double y=Math.Max(32,Math.Min(Height-32,headY+20-Top));tail.Points=new PointCollection{new Point(13,y-7),new Point(2,y),new Point(13,y+7)};}
  else if(p.Side.EndsWith("left")){double y=Math.Max(32,Math.Min(Height-32,headY+20-Top));tail.Points=new PointCollection{new Point(Width-13,y-7),new Point(Width-2,y),new Point(Width-13,y+7)};}
  else{double x=Math.Max(32,Math.Min(Width-32,anchorX-Left)),y=p.Side=="top"?12+cardHeight-1:13;tail.Points=new PointCollection{new Point(x-7,y),new Point(x,y+(p.Side=="top"?10:-10)),new Point(x+7,y)};}
  if(!IsVisible)Show();if(newMessage)BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(160)));
 }
}
