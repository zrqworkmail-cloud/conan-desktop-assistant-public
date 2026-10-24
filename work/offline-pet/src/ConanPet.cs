using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
using Drawing=System.Drawing;

class SpriteView:FrameworkElement {
 public BitmapSource Frame;bool mirrored;
 public SpriteView(){SnapsToDevicePixels=true;UseLayoutRounding=true;RenderOptions.SetBitmapScalingMode(this,BitmapScalingMode.NearestNeighbor);}
 Rect ImageRect(){if(Frame==null)return Rect.Empty;double factor=Math.Min(ActualWidth/Frame.PixelWidth,ActualHeight/Frame.PixelHeight);double w=Frame.PixelWidth*factor,h=Frame.PixelHeight*factor;return new Rect((ActualWidth-w)/2,ActualHeight-h,w,h);}
 protected override void OnRender(DrawingContext dc){if(Frame==null)return;Rect rect=ImageRect();if(mirrored)dc.PushTransform(new ScaleTransform(-1,1,rect.Left+rect.Width/2,0));dc.DrawImage(Frame,rect);if(mirrored)dc.Pop();}
 protected override HitTestResult HitTestCore(PointHitTestParameters p){
  if(Frame==null||ActualWidth==0||ActualHeight==0)return null;
  Rect rect=ImageRect();if(!rect.Contains(p.HitPoint))return null;int x=(int)((p.HitPoint.X-rect.Left)/rect.Width*Frame.PixelWidth),y=(int)((p.HitPoint.Y-rect.Top)/rect.Height*Frame.PixelHeight);
  if(x<0||x>=Frame.PixelWidth||y<0||y>=Frame.PixelHeight)return null;
  if(mirrored)x=Frame.PixelWidth-1-x;byte[] pixel=new byte[4];Frame.CopyPixels(new Int32Rect(x,y,1,1),pixel,4,0);
  return pixel[3]>20?new PointHitTestResult(this,p.HitPoint):null;
 }
 public Rect VisibleBounds(){if(Frame==null)return Rect.Empty;int w=Frame.PixelWidth,h=Frame.PixelHeight,left=w,top=h,right=0,bottom=0;var pixels=new byte[w*h*4];Frame.CopyPixels(pixels,w*4,0);for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[(y*w+x)*4+3]>20){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x+1);bottom=Math.Max(bottom,y+1);}if(left>=right)return Rect.Empty;if(mirrored){int old=left;left=w-right;right=w-old;}var rect=ImageRect();return new Rect(rect.X+left*rect.Width/w,rect.Y+top*rect.Height/h,(right-left)*rect.Width/w,(bottom-top)*rect.Height/h);}
 public void Display(BitmapSource frame,bool mirror=false){if(Frame==frame&&mirrored==mirror)return;Frame=frame;mirrored=mirror;InvalidateVisual();}
}

partial class ConanPet:Window {
 static readonly bool TestMode=Environment.GetEnvironmentVariable("CONAN_PET_TEST_MODE")=="1";
 static readonly string WindowTitle="柯南桌宠 · 离线版"+(TestMode?" [Test]":"");
 readonly string home=AppDomain.CurrentDomain.BaseDirectory;
 readonly string dataHome=UserData.DirectoryFor(AppDomain.CurrentDomain.BaseDirectory);
 readonly Random random=new Random();
 readonly Stopwatch clock=Stopwatch.StartNew();
 readonly SpriteView sprite=new SpriteView();
 readonly Canvas canvas=new Canvas();
 readonly SpeechBubble bubble=new SpeechBubble();
 readonly BubbleQueue messages=new BubbleQueue();
 readonly BitmapSource[,] frames=new BitmapSource[11,8];
 SpriteActions signature;string signatureAction;double signatureStart,signatureUntil;bool signatureVoice;double skateDemoUntil,motionStart,motionDistance;int lastIdle=-1;readonly List<int> idleDeck=new List<int>();int signatureCount;string signatureEmotion;
 readonly BitmapSource[] extraFrames=new BitmapSource[16];double awakeUntil;
 readonly DispatcherTimer ticker=new DispatcherTimer();
 readonly Forms.NotifyIcon tray=new Forms.NotifyIcon();
 AssistantRuntime assistant;AssistantWindow assistantWindow;AssistantSnapshot assistantSnapshot;TaskbarMonitor taskbarMonitor;MonitorCard monitorCard;readonly DispatcherTimer monitorPositionTimer=new DispatcherTimer();
 AdvancedRuntime advanced;TerminalWindow terminal;VoiceRuntime voices;
 readonly TranslateTransform bodyMove=new TranslateTransform();
 SkillRuntime skills;SkillsWindow skillsWindow;BubbleMessage lastTrayMessage;readonly DispatcherTimer skillTicker=new DispatcherTimer();
 System.Threading.Timer assistantTimer;int sampling;volatile bool shuttingDown;

 bool roam=true,gaze=true,lines=true,dragging,moved,menuOpen;string moveMode="roam";double targetX,targetY,moveX,moveY;int patrolIndex;
 double scale=1.15,lastTick,walkUntil,nextWalk,actionUntil,actionStart,lastCursorMove,dragLeft,dragTop;
 int direction=1,action=-1;
 Point dragCursor,lastCursor;
 string[] phrases;
 Drawing.Icon trayIcon;
 Forms.ToolStripMenuItem roamItem,visibilityItem,voiceMuteItem,idleVoiceItem;

 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string cls,string name);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool SetWindowText(IntPtr hwnd,string title);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr hwnd,int message,IntPtr w,IntPtr l);

 [STAThread]public static int Main(){
  bool owns;using(var single=new Mutex(true,"Local\\ConanOfflinePet-20261013"+(TestMode?"-Test":""),out owns)){
   if(!owns){IntPtr h=FindWindow(null,WindowTitle);if(h!=IntPtr.Zero)PostMessage(h,0x8011,IntPtr.Zero,IntPtr.Zero);return 0;}
   try{UserData.Prepare(AppDomain.CurrentDomain.BaseDirectory);var app=new Application();CrashDiagnostics.Install(app);app.ShutdownMode=ShutdownMode.OnMainWindowClose;app.Run(new ConanPet());return 0;}
   catch(Exception e){string report=CrashDiagnostics.Record(e,"Main exit");MessageBox.Show("运行失败："+e.Message+(String.IsNullOrEmpty(report)?"\n错误日志未能保存。":"\n错误日志已保存：\n"+report)+"\n请重启柯南，并提供本弹窗或错误日志。",WindowTitle,MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
  }
 }
 ConanPet(){
  Directory.CreateDirectory(dataHome);Title=WindowTitle;WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
  SourceInitialized+=(s,e)=>{
   var h=new System.Windows.Interop.WindowInteropHelper(this).Handle;
   System.Windows.Interop.HwndSource.FromHwnd(h).AddHook(WindowMessage);
  };
  ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;ShowActivated=false;WindowStartupLocation=WindowStartupLocation.Manual;
  var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri(Path.Combine(home,"conan.png"));image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();
  if(image.PixelWidth!=1536||image.PixelHeight!=2288)throw new InvalidDataException("conan.png 图集尺寸应为1536×2288。");
  for(int row=0;row<11;row++)for(int col=0;col<8;col++){
   var crop=new CroppedBitmap(image,new Int32Rect(col*192,row*208,192,208));
   var converted=new FormatConvertedBitmap(crop,PixelFormats.Bgra32,null,0);converted.Freeze();frames[row,col]=converted;
  }
  for(int kind=0;kind<2;kind++){string extraPath=Path.Combine(home,kind==0?"conan-sleep-v55.png":"conan-search-v55.png");if(!File.Exists(extraPath))continue;var extra=new BitmapImage();extra.BeginInit();extra.UriSource=new Uri(extraPath);extra.CacheOption=BitmapCacheOption.OnLoad;extra.EndInit();extra.Freeze();if(extra.PixelWidth!=1536||extra.PixelHeight!=208)throw new InvalidDataException("追加动作图集尺寸应为1536×208。");for(int i=0;i<8;i++){var crop=new CroppedBitmap(extra,new Int32Rect(i*192,0,192,208));var converted=new FormatConvertedBitmap(crop,PixelFormats.Bgra32,null,0);converted.Freeze();extraFrames[kind*8+i]=converted;}}
  signature=new SpriteActions(home);
  AllowDrop=true;DragOver+=(s,e)=>{e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};Drop+=(s,e)=>{var paths=e.Data.GetData(DataFormats.FileDrop) as string[];if(paths!=null&&paths.Length>0&&File.Exists(paths[0])){OpenTerminal();terminal.OpenEvidence(paths[0]);PlaySignature("deduction",2,false);}e.Handled=true;};
  sprite.RenderTransform=bodyMove;UseLayoutRounding=true;
  ReadPreferences();
  phrases=File.Exists(Path.Combine(home,"台词.txt"))?Array.FindAll(File.ReadAllLines(Path.Combine(home,"台词.txt")),s=>!String.IsNullOrWhiteSpace(s)&&!s.StartsWith("#")):new[]{"让我再看看线索。"};
  if(phrases.Length==0)phrases=new[]{"让我再看看线索。"};
  canvas.Children.Add(sprite);Content=canvas;ResizePet();
  bubble.Dismissed+=()=>messages.Dismiss(clock.Elapsed.TotalSeconds);
  bubble.Details+=()=>OpenDetails(messages.Current);
  IsVisibleChanged+=(s,e)=>{if(!IsVisible){StopFootball();signatureUntil=0;skateDemoUntil=0;messages.Clear();bubble.Hide();if(voices!=null)voices.Stop();}};
  sprite.MouseLeftButtonDown+=OnDown;sprite.MouseMove+=OnMove;sprite.MouseLeftButtonUp+=OnUp;
  sprite.LostMouseCapture+=(s,e)=>{if(dragging){dragging=false;walkUntil=0;nextWalk=clock.Elapsed.TotalSeconds+8;SavePreferences();}};
  sprite.MouseRightButtonUp+=(s,e)=>{ShowMenu();e.Handled=true;};
  assistant=new AssistantRuntime(home,dataHome);VisualEffects.SetMode(VisualEffects.Valid(assistant.Config.Monitor.VisualStyle)?assistant.Config.Monitor.VisualStyle:"original");
  skills=new SkillRuntime(home,dataHome);voices=new VoiceRuntime(home,dataHome);voices.Started+=clip=>{if(!voices.TravelVoice&&(!(voices.CurrentKind=="monitor-alert"||(voices.CurrentKey??"").StartsWith("codex-reset"))||assistant.Config.Monitor.AlertActions)){PlaySignature(clip.Action,Math.Max(.9,clip.Seconds+.25),true);signatureCount=clip.Count;signatureEmotion=clip.Emotion;}if(lines&&!voices.TravelVoice&&(messages.Current==null||!messages.Current.Notification)&&!String.IsNullOrEmpty(clip.Japanese))messages.Add(new BubbleMessage{Key="voice",Text=clip.Japanese+"\n"+clip.Chinese,Notification=false},clock.Elapsed.TotalSeconds);};voices.Completed+=()=>signatureVoice=false;voices.Changed+=()=>{if(!voices.Playing&&signatureVoice){signatureUntil=0;signatureVoice=false;}};advanced=new AdvancedRuntime(dataHome);advanced.SettingsChanged+=SyncHotkeys;
  assistant.Notification+=(key,text,row)=>{if(!shuttingDown)Dispatcher.BeginInvoke(new Action(()=>NotifyUser(key,text,row)));};
  skillTicker.Interval=TimeSpan.FromSeconds(1);skillTicker.Tick+=(s,e)=>{
   try{foreach(var alert in SkillAlerts()){lock(assistant)assistant.Notices.Add(alert.Key,alert.Text,DateTime.UtcNow);if(alert.Key.StartsWith("work-end:"))NotifyWorkEnd(alert);else NotifyUser(alert.Key,alert.Text,alert.Row);}if(skillsWindow!=null)skillsWindow.Update();if(terminal!=null)terminal.Refresh(assistantSnapshot);}
   catch(IOException error){skillTicker.Stop();Say("技能数据保存失败："+error.Message);}
   catch(UnauthorizedAccessException error){skillTicker.Stop();Say("技能数据无法写入："+error.Message);}
  };
  CreateTray();InitializeMonitorWindows();
  Loaded+=(s,e)=>{bubble.Owner=this;SetWindowText(new System.Windows.Interop.WindowInteropHelper(this).Handle,WindowTitle);ClampWindow();sprite.Display(frames[0,0]);Say("小侦探就位。点击我听台词，右键打开菜单。");nextWalk=8;SyncHotkeys();ticker.Start();skillTicker.Start();appliedMonitorInterval=Math.Max(500,Math.Min(5000,assistant.Config.Monitor.RefreshMilliseconds));assistantTimer=new System.Threading.Timer(PollAssistant,null,0,appliedMonitorInterval);CheckStartupConnections();};
  ticker.Interval=TimeSpan.FromMilliseconds(1000.0/60);ticker.Tick+=Tick;
  Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=OnDisplayChanged;
  Closed+=(s,e)=>{shuttingDown=true;monitorPositionTimer.Stop();DisposeTaskbarViews();if(monitorCard!=null)monitorCard.Close();lock(assistant){if(assistant.Monitor.Dirty||assistant.QuotaReader.Dirty)assistant.Save();assistant.Archive.Flush();assistant.Telemetry.Dispose();assistant.Limiter.Dispose();}StopFootball();if(footballWindow!=null)footballWindow.Close();ReleaseHotkeys();if(voices!=null)voices.Dispose();if(advanced!=null)advanced.Dispose();skillTicker.Stop();if(terminal!=null)terminal.Close();if(skillsWindow!=null)skillsWindow.Close();bubble.Close();if(assistantTimer!=null)assistantTimer.Dispose();if(assistantWindow!=null)assistantWindow.Close();ticker.Stop();Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=OnDisplayChanged;SavePreferences();tray.Visible=false;tray.Dispose();if(trayIcon!=null)trayIcon.Dispose();};
 }
 IntPtr WindowMessage(IntPtr hwnd,int message,IntPtr w,IntPtr l,ref bool handled){
  if(message==0x0312){if(w.ToInt32()==0x4311){OpenAssistant();assistantWindow.FocusSearch();}if(w.ToInt32()==0x4312)OpenTerminal();handled=true;}
  if(message==0x8011){Dispatcher.BeginInvoke(new Action(()=>{Show();visibilityItem.Text="隐藏桌宠";ClampWindow();}));handled=true;}
  return IntPtr.Zero;
 }
 [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr hwnd,int id);
 void ReleaseHotkeys(){var handle=new System.Windows.Interop.WindowInteropHelper(this).Handle;if(handle!=IntPtr.Zero){UnregisterHotKey(handle,0x4311);UnregisterHotKey(handle,0x4312);}}
 void SyncHotkeys(){if(!IsLoaded||advanced==null)return;ReleaseHotkeys();if(!advanced.Engine.Data.Hotkeys){advanced.HotkeyStatus="全局快捷键未启用";return;}var handle=new System.Windows.Interop.WindowInteropHelper(this).Handle;bool search=RegisterHotKey(handle,0x4311,0x4003,0x20),center=RegisterHotKey(handle,0x4312,0x4003,0x4B);if(search&&center)advanced.HotkeyStatus="已启用：Ctrl+Alt+Space 快捷入口；Ctrl+Alt+K 侦探控制台";else{ReleaseHotkeys();advanced.HotkeyStatus="组合键被其他程序占用，未启用。关闭后重新启用可重试。";}}
 List<SkillAlert> SkillAlerts(){var alerts=skills.Tick(DateTime.UtcNow);alerts.AddRange(advanced.Tick(DateTime.UtcNow));return alerts;}
 void PollAssistant(object state){
  if(shuttingDown||System.Threading.Interlocked.Exchange(ref sampling,1)!=0)return;
  try{
   AssistantSnapshot value;lock(assistant)value=assistant.Poll();
   if(!shuttingDown)Dispatcher.BeginInvoke(new Action(()=>{assistantSnapshot=value;RefreshMonitorWindows();if(assistantWindow!=null)assistantWindow.Update(value);if(terminal!=null)terminal.Refresh(value);}));
  }catch(Exception e){if(!shuttingDown)Dispatcher.BeginInvoke(new Action(()=>{lock(assistant)assistant.Notices.Add("poll-error","刷新失败："+e.Message,DateTime.UtcNow);ClearMonitorSamples();}));}
  finally{System.Threading.Interlocked.Exchange(ref sampling,0);}
 }
 void EnsureAssistant(){if(assistantWindow==null){assistantWindow=new AssistantWindow(assistant,home);if(assistantSnapshot!=null)assistantWindow.Update(assistantSnapshot);}}
 void OpenAssistant(){EnsureAssistant();OpenTerminal();terminal.ShowPage("快捷入口");}
 FrameworkElement ToolPage(string page){if(page=="快捷入口"||page=="系统与网络"||page=="进程监控"||page=="Codex状态"||page=="通知记录"){EnsureAssistant();return assistantWindow.TakePage(page);}EnsureSkills();return skillsWindow.TakePage(page);}
 void EnsureSkills(){if(skillsWindow==null){skillsWindow=new SkillsWindow(skills,advanced);skillsWindow.Emotion+=row=>{Animate(row);PlayVoice("task-complete",null);};skillsWindow.VoiceEvent+=kind=>PlayVoice(kind,null);}}
 void NotifyWorkEnd(SkillAlert alert){if(shuttingDown||DesktopActions.FullScreenForeground())return;if(assistant.Config.Bubbles){var message=new BubbleMessage{Key="work-end",Text=alert.Text,Row=3,Notification=true};if(IsVisible)messages.Add(message,clock.Elapsed.TotalSeconds);else{lastTrayMessage=message;tray.ShowBalloonTip(6000,"柯南 · 下班倒计时",alert.Text,Forms.ToolTipIcon.Info);}}if(!IsVisible)return;var options=skills.Engine.Data.EndOfWork;if(options.Gestures){PlaySignature(alert.Count>=1&&alert.Count<=20?"count":"deduction",2.4,false);signatureCount=alert.Count;}if(options.Sound&&alert.Count>=1&&alert.Count<=20)voices.PlayCountReminder(alert.Count,false);}
 void NotifyUser(string key,string text,int row){
  if(shuttingDown||DesktopActions.FullScreenForeground())return;bool warning=key.StartsWith("monitor:")||key.StartsWith("codex-reset");var cfg=assistant.Config.Monitor;
  if(key.StartsWith("monitor:")){if(cfg.AlertActions)PlaySignature("alert",2.4,false);if(cfg.AlertSound){voices.PlayMonitorAlert(!IsVisible);}}
  string voiceEvent=VoicePolicy.NotificationEvent(key,row);if(key=="focus"&&skills.Engine.Data.Focus.Mode=="work"||warning&&!cfg.AlertSound)voiceEvent=null;if(voiceEvent!=null){PlayVoice(voiceEvent,key);}
  if(!assistant.Config.Bubbles||warning&&!cfg.AlertBubbles)return;var message=new BubbleMessage{Key=key,Text=text,Row=row,Notification=true};if(key.StartsWith("codex-reset-soon:")){var parts=key.Split(':');int minutes;long ticks;if((parts.Length==3||parts.Length==4)&&Int32.TryParse(parts[1],out minutes)&&(minutes==300||minutes==10080)&&Int64.TryParse(parts[2],out ticks)&&ticks>=DateTime.MinValue.Ticks&&ticks<=DateTime.MaxValue.Ticks){message.CountdownUtc=new DateTime(ticks,DateTimeKind.Utc);message.CountdownLabel="Codex "+(minutes==300?"五小时":"周")+"额度";}}
  if(key.StartsWith("codex:")&&key.EndsWith(":waiting")){string session=key.Substring(6,key.Length-14);message.StillValid=()=>{lock(assistant)return assistant.Codex.State(session,DateTime.UtcNow)=="waiting";};if(!message.StillValid())return;}
  if(IsVisible){messages.Add(message,clock.Elapsed.TotalSeconds);if(!warning||cfg.AlertActions){Animate(row);if(!voices.Playing)PlaySignature(key.StartsWith("system:")?"bowtie":row==6?"alert":"deduction",2,false);}}else{message.Key=BubbleLogic.Group(message.Key);lastTrayMessage=message;tray.ShowBalloonTip(6000,"柯南桌面助手",text,Forms.ToolTipIcon.Info);}
 }
 string terminalTheme="a";
 void OpenTerminal(){if(terminal==null){terminal=new TerminalWindow(assistant,skills,advanced,home,OpenSkills,OpenAssistant,SetMovement,()=>roam?moveMode:"rest");terminal.SetTheme(terminalTheme,false);terminal.ThemeChanged=value=>{terminalTheme=value;SavePreferences();};terminal.Voices=voices;terminal.ToolPage=ToolPage;terminal.PlayAction=Animate;terminal.SignatureAction=id=>PlaySignature(id,2.4,false);terminal.FootballAction=StartFootball;terminal.FootballState=()=>football.Active?(footballMode=="rest"?"足球在桌面 · 可以拖动":footballMode=="retrieve"?"正在寻找足球":footballMode=="juggle"?"正在颠球":footballMode=="volley"?"正在抽射":footballMode=="bicycle"?"倒挂金钩":footballMode=="flick"?"向上挑球":footballMode=="flight"?"等待足球反弹":"正在追球"):"表演已结束";terminal.GetFootballRepeats=()=>footballRepeatCount;terminal.SetFootballRepeats=value=>{footballRepeatCount=Math.Max(1,Math.Min(3,value));SavePreferences();};terminal.PetScale=()=>scale;terminal.PetSetting=PetSetting;terminal.PetFlag=key=>key=="crossScreens"?crossScreens:key=="gaze"?gaze:key=="footballKeep"?footballKeep:key=="footballSeek"?footballSeek:key=="footballDrag"?footballDrag:key=="footballToss"?footballToss:key=="footballLevel"?footballLevel:lines;terminal.MonitorSettingsChanged=RefreshMonitorWindows;terminal.TaskbarState=TaskbarViewsStatus;terminal.ToolVoice=kind=>PlayVoice(kind,null);var owner=terminal;terminal.SearchActivity=busy=>{if(terminal!=owner)return;searching=busy;awakeUntil=clock.Elapsed.TotalSeconds+8;};terminal.Closed+=(s,e)=>{searching=false;terminal=null;};}terminal.Refresh(assistantSnapshot);terminal.Show();terminal.Activate();}
 void SetMovement(string mode){FinishFootball();skateDemoUntil=0;roam=mode!="rest";if(roam)moveMode=mode;roamItem.Checked=!roam;walkUntil=0;nextWalk=clock.Elapsed.TotalSeconds+3;SavePreferences();}
 void OpenSkills(string page){EnsureSkills();skillsWindow.Update();OpenTerminal();terminal.ShowPage(page=="总览"?"计划与专注":page);}
 bool searching;double nextIdleGesture=18;
 void PetSetting(string key){if(key=="footballKeep"){footballKeep=!footballKeep;if(!footballKeep&&footballMode=="rest")StopFootball();}else if(key=="footballSeek")footballSeek=!footballSeek;else if(key=="footballDrag")footballDrag=!footballDrag;else if(key=="footballToss")footballToss=!footballToss;else if(key=="crossScreens"){crossScreens=!crossScreens;walkUntil=skateDemoUntil=0;desktopMovement=null;nextWalk=clock.Elapsed.TotalSeconds+3;ClampWindow();}else if(key=="footballLevel"){footballLevel=!footballLevel;FinishFootball();}else if(key=="gaze")gaze=!gaze;else if(key=="lines"){lines=!lines;if(!lines)messages.ClearCasual(clock.Elapsed.TotalSeconds);}else{double value;if(Double.TryParse(key,NumberStyles.Float,CultureInfo.InvariantCulture,out value)){scale=value;ResizePet();}}SavePreferences();}
 [StructLayout(LayoutKind.Sequential)]struct LastInput {public uint Size,Time;}
 [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref LastInput input);
 double IdleSeconds(){var input=new LastInput{Size=(uint)Marshal.SizeOf(typeof(LastInput))};return GetLastInputInfo(ref input)?unchecked((uint)Environment.TickCount-input.Time)/1000.0:0;}
 void DisplayExtra(int start,double now,int ms){if(extraFrames[start]==null){DisplayFrame(start==0?0:7,PetLogic.Frame(start==0?0:7,(int)(now*1000)),now);return;}bodyMove.Y=0;sprite.Display(extraFrames[start+((int)(now*1000)/ms)%8]);}
 void OpenDetails(BubbleMessage message){
  if(message==null)return;if(message.Key.StartsWith("codex-reset")){OpenTerminal();terminal.ShowQuotaPage();return;}if(message.Key.StartsWith("monitor:")){OpenTerminal();terminal.ShowPage("监控与预警");return;}if(message.Key.StartsWith("system:")){var notice=assistant.SystemNotifications.Find(message.Key);if(notice!=null&&DesktopActions.OpenNotificationApp(notice.AppId))return;OpenTerminal();terminal.ShowPage("通知中心");return;}var route=NotificationRoute.Resolve(message.Key);
  try{
   if(route.Kind=="codex"&&route.Value!=null&&DesktopActions.OpenUri(route.Value))return;
   if(route.Kind=="network"&&DesktopActions.OpenUri(route.Value))return;
   if(route.Kind=="process"){
    int id=Int32.Parse(route.Value);WatchedProcess watch=null;if(assistantSnapshot!=null)watch=Array.Find(assistantSnapshot.Watches,p=>p.Id==id);
    if(watch!=null&&!watch.Ended&&DesktopActions.FocusProcess(id,watch.Start))return;
    OpenAssistant();terminal.ShowPage("进程监控");assistantWindow.ShowProcess(id);return;
   }
  }catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}
  if(route.Kind=="tasks"){OpenSkills("待办计划");skillsWindow.SelectTask(route.Value);return;}
  if(route.Kind=="files"){OpenSkills("文件观察");skillsWindow.SelectFile(route.Value);return;}
  if(route.Kind=="focus"){OpenSkills("专注");return;}
  if(route.Kind=="reminders"){OpenSkills("提醒");skillsWindow.SelectReminder(route.Value);return;}
  if(route.Kind=="wellness"){OpenSkills("健康");return;}
  OpenAssistant();terminal.ShowPage(route.Kind=="codex"?"Codex状态":route.Kind=="network"?"系统与网络":"通知记录");
 }
 void ReadPreferences(){
  Left=Double.NaN;Top=Double.NaN;
  string path=Path.Combine(dataHome,"settings.ini");if(!File.Exists(path))return;
  foreach(string line in File.ReadAllLines(path)){
   var pair=line.Split('=');if(pair.Length!=2)continue;double value;
   if(pair[0]=="x"&&Double.TryParse(pair[1],NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!Double.IsNaN(value)&&!Double.IsInfinity(value))Left=value;
   if(pair[0]=="y"&&Double.TryParse(pair[1],NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!Double.IsNaN(value)&&!Double.IsInfinity(value))Top=value;
   if(pair[0]=="scale"&&Double.TryParse(pair[1],NumberStyles.Float,CultureInfo.InvariantCulture,out value))scale=PetLogic.Clamp(value,0.75,5.0);
   if(pair[0]=="roam")roam=pair[1]=="true";if(pair[0]=="moveMode"&&(pair[1]=="roam"||pair[1]=="follow"||pair[1]=="patrol"||pair[1]=="skate"))moveMode=pair[1];
   if(pair[0]=="footballRepeats"){int count;if(Int32.TryParse(pair[1],out count)&&count>=1&&count<=3)footballRepeatCount=count;}
   if(pair[0]=="terminalTheme"&&(pair[1]=="a"||pair[1]=="b"))terminalTheme=pair[1];
   if(pair[0]=="crossScreens")crossScreens=pair[1]=="true";
   if(pair[0]=="gaze")gaze=pair[1]=="true";
   if(pair[0]=="footballKeep")footballKeep=pair[1]=="true";if(pair[0]=="footballSeek")footballSeek=pair[1]=="true";if(pair[0]=="footballDrag")footballDrag=pair[1]=="true";if(pair[0]=="footballToss")footballToss=pair[1]=="true";
   if(pair[0]=="footballLevel")footballLevel=pair[1]=="true";
   if(pair[0]=="lines")lines=pair[1]=="true";
  }
 }
 void SavePreferences(){
  try{File.WriteAllLines(Path.Combine(dataHome,"settings.ini"),new[]{"x="+Left.ToString(CultureInfo.InvariantCulture),"y="+Top.ToString(CultureInfo.InvariantCulture),"scale="+scale.ToString(CultureInfo.InvariantCulture),"roam="+roam.ToString().ToLowerInvariant(),"moveMode="+moveMode,"crossScreens="+crossScreens.ToString().ToLowerInvariant(),"terminalTheme="+terminalTheme,"footballRepeats="+footballRepeatCount,"footballLevel="+footballLevel.ToString().ToLowerInvariant(),"footballKeep="+footballKeep.ToString().ToLowerInvariant(),"footballSeek="+footballSeek.ToString().ToLowerInvariant(),"footballDrag="+footballDrag.ToString().ToLowerInvariant(),"footballToss="+footballToss.ToString().ToLowerInvariant(),"gaze="+gaze.ToString().ToLowerInvariant(),"lines="+lines.ToString().ToLowerInvariant()});}
  catch(IOException){}catch(UnauthorizedAccessException){}
 }
 Point CursorPoint(){var p=Forms.Cursor.Position;var src=PresentationSource.FromVisual(this);return src==null?new Point(p.X,p.Y):src.CompositionTarget.TransformFromDevice.Transform(new Point(p.X,p.Y));}
 Rect WorkArea(){
  var src=PresentationSource.FromVisual(this);Matrix from=src==null?Matrix.Identity:src.CompositionTarget.TransformFromDevice;
  Matrix to=src==null?Matrix.Identity:src.CompositionTarget.TransformToDevice;
  Point center=Double.IsNaN(Left)||Double.IsNaN(Top)?new Point(0,0):to.Transform(new Point(Left+Width/2,Top+Height/2));
  var screen=Forms.Screen.FromPoint(new Drawing.Point((int)center.X,(int)center.Y));var area=screen.WorkingArea;
  Point start=from.Transform(new Point(area.Left,area.Top)),end=from.Transform(new Point(area.Right,area.Bottom));
  return new Rect(start,end);
 }
 void ClampWindow(){
  desktopMovement=null;Rect area=WorkArea();if(Double.IsNaN(Left)||Double.IsNaN(Top)){Left=area.Right-Width-32;Top=area.Bottom-Height-8;}
  if(crossScreens){Point safe=MovementSpace().ClampToScreen(new Point(Left,Top));Left=safe.X;Top=safe.Y;}else{Left=PetLogic.Clamp(Left,area.Left,area.Right-Width);Top=PetLogic.Clamp(Top,area.Top,area.Bottom-Height);}
 }
 void ResizePet(){
  desktopMovement=null;walkUntil=skateDemoUntil=0;nextWalk=clock.Elapsed.TotalSeconds+3;
  double available=IsLoaded?WorkArea().Height:SystemParameters.WorkArea.Height;var source=PresentationSource.FromVisual(this);double dpi=source==null?1:source.CompositionTarget.TransformToDevice.M22;scale=PetLogic.FitPixelScale(scale,available,dpi);double oldHeight=Height;Width=Math.Max(280,192*scale);Height=90+208*scale;
  sprite.Width=192*scale;sprite.Height=208*scale;Canvas.SetLeft(sprite,(Width-sprite.Width)/2);Canvas.SetTop(sprite,90);
  if(IsLoaded&&!Double.IsNaN(oldHeight)){Top+=oldHeight-Height;ClampWindow();}SavePreferences();
 }
 void OnDisplayChanged(object sender,EventArgs e){Dispatcher.BeginInvoke(new Action(ResizePet));}
 bool detailDoubleClick;
 void OnDown(object sender,MouseButtonEventArgs e){
  detailDoubleClick=e.ClickCount==2;
  dragging=true;moved=false;dragCursor=CursorPoint();dragLeft=Left;dragTop=Top;walkUntil=0;actionUntil=0;signatureUntil=0;skateDemoUntil=0;
  sprite.CaptureMouse();e.Handled=true;
 }
 void OnMove(object sender,MouseEventArgs e){
  if(!dragging)return;Point p=CursorPoint();Vector d=p-dragCursor;if(d.Length>4)moved=true;
  if(moved){Left=dragLeft+d.X;Top=dragTop+d.Y;}e.Handled=true;
 }
 void OnUp(object sender,MouseButtonEventArgs e){
  if(!dragging)return;dragging=false;sprite.ReleaseMouseCapture();ClampWindow();SavePreferences();nextWalk=clock.Elapsed.TotalSeconds+8;
  if(!moved){if(assistant.Config.Monitor.ClickCard&&detailDoubleClick)OpenMonitorCard();Animate(3);if(!PlayVoice("click",null)&&!voices.Playing)Say(phrases[random.Next(phrases.Length)]);}e.Handled=true;
 }
 bool PlayVoice(string kind,string key){return voices!=null&&voices.PlayEvent(kind,key,!IsVisible||DesktopActions.FullScreenForeground());}
 void PlaySignature(string id,double seconds,bool voice){if(football.Active&&id=="kick"&&voice)return;FinishFootball();if(!signature.Has(id)||!voice&&voices!=null&&voices.Playing)return;if(id=="skate"){BeginSkateDemo(seconds);return;}skateDemoUntil=0;seconds=ActionTiming.Duration(id,signature.Count(id),seconds);signatureCount=id=="count"&&!voice?random.Next(1,21):0;signatureEmotion=null;signatureAction=id;signatureStart=clock.Elapsed.TotalSeconds;signatureUntil=signatureStart+seconds;signatureVoice=voice;awakeUntil=signatureUntil+8;walkUntil=0;nextWalk=signatureUntil+7;}
 void BeginSkateDemo(double seconds){double now=clock.Elapsed.TotalSeconds;signatureUntil=0;actionUntil=0;signatureVoice=false;skateDemoUntil=now+Math.Max(7.2,seconds*3);motionStart=now;motionDistance=0;awakeUntil=skateDemoUntil+8;desktopMovement=null;moveX=Left;moveY=Top;var destinations=MovementSpace().Destinations(new Point(Left,Top));Rect area=destinations.Length==0?new Rect(Left,Top,0,0):destinations.OrderByDescending(r=>Math.Pow(r.Left+r.Width/2-Left,2)+Math.Pow(r.Top+r.Height/2-Top,2)).First();targetX=destinations.Length>1?area.Left+area.Width/2:PetLogic.Clamp(Left+(Left+420<=area.Right?420:-420),area.Left,area.Right);targetY=PetLogic.Clamp(Top+(Top+180<=area.Bottom?180:-180),area.Top,area.Bottom);walkUntil=skateDemoUntil;nextWalk=skateDemoUntil+7;}
 void RandomIdle(){if(idleDeck.Count==0){for(int i=0;i<ActionTiming.IdleActions.Length;i++)idleDeck.Add(i);for(int i=idleDeck.Count-1;i>0;i--){int j=random.Next(i+1),value=idleDeck[i];idleDeck[i]=idleDeck[j];idleDeck[j]=value;}if(idleDeck[0]==lastIdle){int value=idleDeck[0];idleDeck[0]=idleDeck[1];idleDeck[1]=value;}}int index=idleDeck[0];idleDeck.RemoveAt(0);lastIdle=index;PlayIdle(ActionTiming.IdleActions[index]);}
 void PlayIdle(string id){if(id=="kick"||id=="bicycle"||id=="flick"){StartFootball("idle-"+(id=="kick"?"volley":id));return;}if(voices!=null&&voices.PlayAmbient(id,!IsVisible||DesktopActions.FullScreenForeground()))return;switch(id){case "juggle":StartFootball("idle-juggle");break;case "wave":Animate(3);break;case "jump":Animate(4);break;case "bow":Animate(5);break;case "surprise":Animate(6);break;case "think":Animate(7);break;case "glasses":Animate(8);break;case "sleep":case "search":signatureAction=id;signatureStart=clock.Elapsed.TotalSeconds;signatureUntil=signatureStart+2.4;signatureVoice=false;walkUntil=0;nextWalk=signatureUntil+7;break;case "skate":if(roam)PlaySignature(id,2.4,false);else Animate(8);break;case "count":PlaySignature(id,1.5,false);break;default:PlaySignature(id,2.4,false);break;}}
 void Animate(int row){FinishFootball();if(voices==null||!voices.Playing){signatureUntil=0;skateDemoUntil=0;}awakeUntil=clock.Elapsed.TotalSeconds+8;action=row;actionStart=clock.Elapsed.TotalSeconds;actionUntil=actionStart+PetLogic.Duration(row)/1000.0;walkUntil=0;nextWalk=actionUntil+7;}
 void DisplayFrame(int row,int column,double now,double walkDistance=0){
  if(row==0&&skills!=null&&skills.Engine.Data.Focus.Running&&skills.Engine.Data.Focus.Mode=="work"){row=7;column=PetLogic.Frame(row,(int)(now*1000));}
  sprite.Display(frames[row,column]);
  double offset=row==4?-Math.Abs(Math.Sin((now-actionStart)*Math.PI*2))*18*scale:row==1||row==2?-Math.Abs(Math.Sin(ActionTiming.WalkPhase(walkDistance,scale)*Math.PI*2))*2*scale:0;
  var source=PresentationSource.FromVisual(this);double dpi=source==null?1:source.CompositionTarget.TransformToDevice.M22;bodyMove.Y=Math.Round(offset*dpi)/dpi;
 }
 void Say(string text){if(lines&&assistant.Config.Bubbles)messages.Add(new BubbleMessage{Key="casual",Text=text,Row=0,Notification=false},clock.Elapsed.TotalSeconds);}
 void Tick(object sender,EventArgs e){
  double now=clock.Elapsed.TotalSeconds,dt=Math.Min(0.1,now-lastTick);lastTick=now;
  if(!IsVisible)return;
  if(voices!=null&&voices.Playing&&DesktopActions.FullScreenForeground())voices.Stop();
  if(!assistant.Config.Bubbles){messages.Clear();bubble.Hide();}else{
  messages.Tick(now,bubble.IsMouseOver);
  bubble.Options=assistant.Config.BubbleAppearance;bubble.AnchorBody=new Rect(Left+(Width-116*scale)/2,Top+90+40*scale,116*scale,168*scale);
  bubble.Render(messages.Current,messages.Pending,Left+Width/2,Top+90+40*scale,Top+Height,WorkArea());}
  Point cursor=CursorPoint();if((cursor-lastCursor).Length>2){lastCursorMove=now;lastCursor=cursor;}
  if(football.Active&&TickFootball(now,dt))return;
  if(dragging){DisplayFrame(direction>0?1:2,PetLogic.Frame(1,(int)(now*1000)),now);return;}
  if(now<signatureUntil){if(signatureAction=="search"&&extraFrames[8]!=null){bodyMove.Y=0;sprite.Display(extraFrames[8+ActionTiming.SearchFrame(now-signatureStart,false)]);return;}if(signatureAction=="sleep"||signatureAction=="search"){DisplayExtra(signatureAction=="sleep"?0:8,now,signatureAction=="sleep"?220:ActionTiming.SearchFrameMilliseconds);return;}bodyMove.Y=0;sprite.Display(signature.PlaybackFrame(signatureAction,now-signatureStart,signatureUntil-signatureStart,signatureCount,signatureEmotion));return;}
  if(now<actionUntil){DisplayFrame(action,PetLogic.Frame(action,(int)((now-actionStart)*1000)),now);return;}
  bool hover=sprite.IsMouseOver||bubble.IsMouseOver||menuOpen||DesktopActions.FullScreenForeground();
  if(hover||messages.Current!=null){walkUntil=0;nextWalk=Math.Max(nextWalk,now+4);}
  bool busy=skills.Engine.Data.Focus.Running||assistantSnapshot!=null&&assistantSnapshot.CodexRow>=0;
  string state=ActivityLogic.State(searching,busy,hover||messages.Current!=null||now<awakeUntil,IdleSeconds());
  if(state=="search"||state=="sleep"){walkUntil=0;nextWalk=now+3;DisplayExtra(state=="sleep"?0:8,now,state=="sleep"?220:ActionTiming.SearchFrameMilliseconds);return;}
  if(busy){walkUntil=0;nextWalk=now+3;int row=assistantSnapshot!=null&&assistantSnapshot.CodexRow>=0?assistantSnapshot.CodexRow:7;DisplayFrame(row,PetLogic.Frame(row,(int)(now*1000)),now);return;}
  if(!hover&&messages.Current==null&&now>=nextIdleGesture&&ActionTiming.CanIdle(now,walkUntil)){nextIdleGesture=now+20+random.NextDouble()*15;RandomIdle();return;}
  if(roam&&!hover&&now>=nextWalk&&messages.Current==null&&now>=skateDemoUntil){motionStart=now;motionDistance=0;
   BeginDesktopTravel();
   walkUntil=now+60;nextWalk=walkUntil+6;voices.PlayAmbient("travel",!IsVisible||DesktopActions.FullScreenForeground());
  }
  if((roam||now<skateDemoUntil)&&now<walkUntil&&!hover&&messages.Current==null){
   if(moveMode=="follow"&&now>=skateDemoUntil){targetX=cursor.X-Width/2;targetY=cursor.Y-Height/2;}
   double oldX=moveX,oldY=moveY;bool skating=moveMode=="skate"||now<skateDemoUntil;bool arrived=StepDesktopTravel(ActionTiming.TravelSpeed(scale,skating)*dt);motionDistance+=Math.Sqrt(Math.Pow(moveX-oldX,2)+Math.Pow(moveY-oldY,2));
   var source=PresentationSource.FromVisual(this);Matrix pixel=source==null?Matrix.Identity:source.CompositionTarget.TransformToDevice;Left=Math.Round(moveX*pixel.M11)/pixel.M11;Top=Math.Round(moveY*pixel.M22)/pixel.M22;
   if(arrived){walkUntil=now;nextIdleGesture=Math.Min(nextIdleGesture,now+.4);nextWalk=now+(moveMode=="follow"?1:4+random.NextDouble()*5);}

   if((moveMode=="skate"||now<skateDemoUntil)&&signature.Has("skate")){bodyMove.Y=0;sprite.Display(arrived?signature.Frame("skate",0):signature.MovingFrame(now-motionStart),direction<0);return;}int row=direction>0?1:2;DisplayFrame(row,ActionTiming.WalkFrame(motionDistance,scale),now,motionDistance);return;
  }
  if(walkUntil>0){walkUntil=0;SavePreferences();}
  if(assistantSnapshot!=null&&assistantSnapshot.CodexRow>=0){int row=assistantSnapshot.CodexRow;DisplayFrame(row,PetLogic.Frame(row,(int)(now*1000)),now);return;}
  if(gaze&&now-lastCursorMove<1.2){
   double anchorX=Left+Width/2,anchorY=Top+90+sprite.Height/2;
   int sector=PetLogic.Gaze(cursor.X-anchorX,cursor.Y-anchorY);
   if(sector>=0){DisplayFrame(9+sector/8,sector%8,now);return;}
  }
  DisplayFrame(0,PetLogic.Frame(0,(int)(now*1000)),now);
 }
 Forms.ToolStripMenuItem Item(string text,Action handler){var item=new Forms.ToolStripMenuItem(text);item.Click+=(s,e)=>handler();return item;}
 [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
 void CreateTray(){
  using(var atlas=new Drawing.Bitmap(Path.Combine(home,"conan.png")))
  using(var portrait=new Drawing.Bitmap(32,32)){
   using(var graphics=Drawing.Graphics.FromImage(portrait)){graphics.InterpolationMode=Drawing.Drawing2D.InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=Drawing.Drawing2D.PixelOffsetMode.Half;graphics.DrawImage(atlas,new Drawing.Rectangle(0,4,32,24),new Drawing.Rectangle(42,47,108,77),Drawing.GraphicsUnit.Pixel);}
   IntPtr handle=portrait.GetHicon();try{using(var icon=Drawing.Icon.FromHandle(handle))trayIcon=(Drawing.Icon)icon.Clone();}finally{DestroyIcon(handle);}
  }
  tray.Icon=trayIcon;tray.Text="柯南桌宠 · 离线版";
  var menu=new Forms.ContextMenuStrip{Font=new Drawing.Font("Microsoft YaHei UI",9),BackColor=Drawing.Color.FromArgb(255,253,248),ForeColor=Drawing.Color.FromArgb(32,55,79),ShowImageMargin=false};
  menu.Items.Add(Item("侦探工作台",OpenTerminal));menu.Items.Add(Item("启动与窗口设置",()=>{OpenTerminal();terminal.ShowPage("启动与窗口");}));menu.Items.Add(Item("搜索文件",()=>{OpenTerminal();terminal.ShowPage("文件搜索");}));
  menu.Items.Add(Item("下班倒计时",()=>OpenSkills("下班倒计时")));
  menu.Items.Add(Item("专注计时",()=>OpenSkills("专注")));menu.Items.Add(Item("添加提醒",()=>OpenSkills("提醒")));
  var actionsMenu=new Forms.ToolStripMenuItem("动作选择");
  foreach(var entry in new[]{new[]{"挥手","3"},new[]{"跳跃","4"},new[]{"致意","5"},new[]{"惊讶","6"},new[]{"思考","7"},new[]{"整理眼镜","8"}}){int row=Int32.Parse(entry[1]);actionsMenu.DropDownItems.Add(Item(entry[0]+" · 一次",()=>{ShowForAction();Animate(row);}));}
  foreach(var entry in new[]{new[]{"放大镜寻找","search"},new[]{"打个盹","sleep"},new[]{"指认推理","deduction"},new[]{"麻醉手表","watch"},new[]{"变声领结","bowtie"},new[]{"涡轮滑板","skate"},new[]{"数字手势","count"},new[]{"点赞鼓励","praise"},new[]{"警觉观察","alert"}}){string id=entry[1];actionsMenu.DropDownItems.Add(Item(entry[0]+" · 一次",()=>{ShowForAction();FinishFootball();if(id=="search"||id=="sleep")PlayIdle(id);else PlaySignature(id,2.4,false);}));}
  foreach(var entry in new[]{new[]{"颠足球","juggle"},new[]{"凌空抽射","volley"},new[]{"向上挑球","flick"},new[]{"倒挂金钩","bicycle"},new[]{"抛球追球","throw"}}){string id=entry[1];actionsMenu.DropDownItems.Add(Item(entry[0]+" · 限次表演",()=>StartFootball(id)));}
  actionsMenu.DropDownItems.Add(Item("停止当前表演",StopPerformance));menu.Items.Add(actionsMenu);
  menu.Items.Add(new Forms.ToolStripSeparator());
  roamItem=Item("定点休息",()=>SetMovement(roam?"rest":"roam"));roamItem.Checked=!roam;menu.Items.Add(roamItem);
  idleVoiceItem=Item("待机与漫游语音",()=>{voices.Settings.Ambient=!voices.Settings.Ambient;try{voices.Apply();}catch(IOException e){Say(e.Message);}catch(UnauthorizedAccessException e){Say(e.Message);}});idleVoiceItem.Checked=voices.Settings.Ambient;voices.Changed+=()=>idleVoiceItem.Checked=voices.Settings.Ambient;menu.Items.Add(idleVoiceItem);
  voiceMuteItem=Item("原声静音",()=>{voices.Settings.Enabled=!voices.Settings.Enabled;try{voices.Apply();}catch(IOException e){Say(e.Message);}catch(UnauthorizedAccessException e){Say(e.Message);}});voiceMuteItem.Checked=!voices.Settings.Enabled;voices.Changed+=()=>voiceMuteItem.Checked=!voices.Settings.Enabled;menu.Items.Add(voiceMuteItem);
  menu.Items.Add(Item("找回桌宠",()=>{Show();Left=Double.NaN;Top=Double.NaN;ClampWindow();SavePreferences();}));
  visibilityItem=Item("隐藏桌宠",()=>{if(IsVisible){Hide();visibilityItem.Text="显示桌宠";}else{Show();visibilityItem.Text="隐藏桌宠";ClampWindow();}});menu.Items.Add(visibilityItem);
  menu.Items.Add(Item("退出",()=>Close()));
  menu.Opening+=(s,e)=>{menuOpen=true;walkUntil=0;};menu.Closed+=(s,e)=>{menuOpen=false;nextWalk=clock.Elapsed.TotalSeconds+3;};tray.ContextMenuStrip=menu;tray.BalloonTipClicked+=(s,e)=>OpenDetails(lastTrayMessage);tray.DoubleClick+=(s,e)=>{Show();visibilityItem.Text="隐藏桌宠";ClampWindow();};tray.Visible=true;
 }
 void ShowForAction(){if(!IsVisible){Show();visibilityItem.Text="隐藏桌宠";ClampWindow();}if(voices.Playing)voices.Stop();}
 void StopPerformance(){StopFootball();signatureUntil=actionUntil=skateDemoUntil=walkUntil=0;nextWalk=clock.Elapsed.TotalSeconds+5;if(voices.Playing)voices.Stop();}
 MenuItem NativeMenuItem(Forms.ToolStripMenuItem source){
  var item=new MenuItem{Header=source.Text,IsChecked=source.Checked,IsEnabled=source.Enabled};
  item.IsCheckable=source==roamItem||source==voiceMuteItem||source==idleVoiceItem;
  if(source.HasDropDownItems){foreach(Forms.ToolStripMenuItem child in source.DropDownItems)item.Items.Add(NativeMenuItem(child));}
  else item.Click+=(s,e)=>{source.PerformClick();e.Handled=true;};
  return item;
 }
 void ShowMenu(){
  var menu=new ContextMenu();menu.Resources.MergedDictionaries.Add(WorkbenchTheme.Resources());
  foreach(Forms.ToolStripItem source in tray.ContextMenuStrip.Items){
   if(source is Forms.ToolStripSeparator)menu.Items.Add(new Separator());
   else menu.Items.Add(NativeMenuItem((Forms.ToolStripMenuItem)source));
  }
  menuOpen=true;walkUntil=0;menu.Closed+=(s,e)=>{menuOpen=false;nextWalk=clock.Elapsed.TotalSeconds+3;};menu.PlacementTarget=sprite;menu.Placement=System.Windows.Controls.Primitives.PlacementMode.MousePoint;menu.IsOpen=true;
 }
}
