using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

partial class TerminalWindow {
 readonly Stack<string> routeHistory=new Stack<string>();bool returning;
 readonly Button backButton=new Button{Margin=new Thickness(0,0,0,16),HorizontalAlignment=HorizontalAlignment.Left};
 readonly TextBlock breadcrumb=new TextBlock{FontSize=12,VerticalAlignment=VerticalAlignment.Center};
 readonly Border brandIcon=new Border(),previewArtwork=new Border();
 readonly Dictionary<string,Button> themeButtons=new Dictionary<string,Button>();
 readonly Dictionary<string,CheckBox> companionSwitches=new Dictionary<string,CheckBox>();
 CheckBox quickIdleVoice;Slider quickVoiceVolume;TextBlock quickVoiceLevel;VoiceRuntime volumeRuntime;bool syncingQuickVoice;
 Expander utilityNav;ScrollViewer pageScroll;string displayedTheme="a";
 public string Theme{get{return displayedTheme;}}
 public Action<string> ThemeChanged;
 void ScrollSettingsWheel(object sender,MouseWheelEventArgs e){if(e.Delta==0)return;var node=e.OriginalSource as DependencyObject;while(node!=null&&node!=pageScroll){var choice=node as ComboBox;if(choice!=null&&choice.IsDropDownOpen)return;var inner=node as ScrollViewer;if(inner!=null&&inner.ScrollableHeight>0&&((e.Delta<0&&inner.VerticalOffset<inner.ScrollableHeight-.5)||(e.Delta>0&&inner.VerticalOffset>.5))){inner.ScrollToVerticalOffset(inner.VerticalOffset-e.Delta*.4);e.Handled=true;return;}node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}pageScroll.ScrollToVerticalOffset(pageScroll.VerticalOffset-e.Delta*.4);e.Handled=true;}
 void InitializeShell(){
  TerminalAppearance.Palette(Resources,"a");SetResourceReference(BackgroundProperty,"DeskBase");SetResourceReference(ForegroundProperty,"DeskText");
  var root=new Grid();TerminalAppearance.Bind(root,System.Windows.Controls.Panel.BackgroundProperty,"Base");root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(210)});root.ColumnDefinitions.Add(new ColumnDefinition());Content=root;
  var side=new DockPanel();TerminalAppearance.Bind(side,System.Windows.Controls.Panel.BackgroundProperty,"Sidebar");root.Children.Add(side);
  var brand=new StackPanel{Margin=new Thickness(22,28,16,24)};DockPanel.SetDock(brand,Dock.Top);side.Children.Add(brand);var line=new StackPanel{Orientation=Orientation.Horizontal};brand.Children.Add(line);line.Children.Add(brandIcon);line.Children.Add(new TextBlock{Text="侦探控制台",FontSize=17,FontWeight=FontWeights.SemiBold,Margin=new Thickness(10,0,0,0)});brand.Children.Add(Text("YOUR DESKTOP COMPANION",9));
  var footer=new StackPanel{Margin=new Thickness(20)};DockPanel.SetDock(footer,Dock.Bottom);side.Children.Add(footer);string atlas=Path.Combine(home,"conan.png");if(File.Exists(atlas)){var bm=new BitmapImage(new Uri(atlas));var avatar=new Image{Source=new CroppedBitmap(bm,new Int32Rect(43,47,106,101)),Width=40,Height=40,HorizontalAlignment=HorizontalAlignment.Left};RenderOptions.SetBitmapScalingMode(avatar,BitmapScalingMode.NearestNeighbor);footer.Children.Add(avatar);}footer.Children.Add(Text("你的桌面小侦探",12));footer.Children.Add(Text("本地运行 · v5.30.6",10));
  var navScroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};side.Children.Add(navScroll);var nav=new StackPanel{Margin=new Thickness(12,0,10,12)};navScroll.Content=nav;
  BuildSortableNavigation(nav,navScroll);

  var main=new DockPanel();Grid.SetColumn(main,1);root.Children.Add(main);var top=new Border{Padding=new Thickness(30,15,30,15),BorderThickness=new Thickness(0,0,0,1)};TerminalAppearance.Bind(top,Border.BorderBrushProperty,"Line");DockPanel.SetDock(top,Dock.Top);main.Children.Add(top);var tools=new DockPanel();top.Child=tools;
  var themes=new StackPanel{Orientation=Orientation.Horizontal};DockPanel.SetDock(themes,Dock.Right);tools.Children.Add(themes);var styles=new ComboBox{Width=112,Margin=new Thickness(0,4,8,4),MinHeight=32,ToolTip="全局界面画刷风格；不改变原版像素人物颜色"};System.Windows.Automation.AutomationProperties.SetName(styles,"全局色彩风格");foreach(string label in new[]{"原样配色","柔光渐变","霓虹渐变"})styles.Items.Add(label);styles.SelectedIndex=Array.IndexOf(new[]{"original","soft","neon"},assistant.Config.Monitor.VisualStyle);themes.Children.Add(styles);styles.SelectionChanged+=(s,e)=>{if(styles.SelectedIndex<0)return;string mode=new[]{"original","soft","neon"}[styles.SelectedIndex];SaveMonitor(()=>assistant.Config.Monitor.VisualStyle=mode);VisualEffects.SetMode(mode);SetTheme(displayedTheme,false);VisualEffects.Apply(this);};foreach(var choice in new[]{new[]{"a","A · 柯南"},new[]{"b","B · 月下基德"}}){string key=choice[0];var b=Button(themes,choice[1],()=>SetTheme(key,true));b.MinHeight=32;b.Padding=new Thickness(13,6,22,6);b.FontSize=12;System.Windows.Automation.AutomationProperties.SetAutomationId(b,"Theme"+key.ToUpperInvariant());themeButtons[key]=b;}
  var searchHost=new Grid{Width=180,Margin=new Thickness(0,4,14,4)};DockPanel.SetDock(searchHost,Dock.Right);tools.Children.Add(searchHost);var search=new TextBox{MinHeight=32,Padding=new Thickness(9,6,9,6),FontSize=12};searchHost.Children.Add(search);var placeholder=Text("搜索功能 · Enter",11);placeholder.Margin=new Thickness(10,0,0,0);placeholder.VerticalAlignment=VerticalAlignment.Center;placeholder.IsHitTestVisible=false;searchHost.Children.Add(placeholder);search.TextChanged+=(s,e)=>placeholder.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;System.Windows.Automation.AutomationProperties.SetName(search,"搜索控制台");
  search.KeyDown+=(s,e)=>{if(e.Key!=Key.Enter)return;string query=search.Text.Trim();if(query.Length==0)return;string found=Routes.Concat(MemoAliases).Concat(new[]{"侦探备忘录","启动与窗口","监控与预警","任务栏外观","下班倒计时","原声配音","原声候选","动作演示","数据与备份","证物卡","文件搜索","快捷入口","文件观察","磁盘与电池","便签","文本","剪贴板","桌面工具","语音与快捷键","待办计划","专注","提醒","健康","Codex状态","进程监控","系统与网络","通知记录"}).FirstOrDefault(n=>n.Contains(query));if(found!=null){ShowPage(found);search.Clear();}else search.ToolTip="未找到匹配页面";e.Handled=true;};
  TerminalAppearance.Bind(breadcrumb,TextBlock.ForegroundProperty,"Muted");tools.Children.Add(breadcrumb);
  pinnedSettingsTop.Margin=new Thickness(22,10,18,0);pinnedSettingsBottom.Margin=new Thickness(22,0,18,8);pinnedSettingsTop.Visibility=pinnedSettingsBottom.Visibility=Visibility.Collapsed;System.Windows.Automation.AutomationProperties.SetName(pinnedSettingsTop,"固定预览与导航");System.Windows.Automation.AutomationProperties.SetName(pinnedSettingsBottom,"固定应用栏");DockPanel.SetDock(pinnedSettingsTop,Dock.Top);main.Children.Add(pinnedSettingsTop);DockPanel.SetDock(pinnedSettingsBottom,Dock.Bottom);main.Children.Add(pinnedSettingsBottom);
  pageScroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};main.Children.Add(pageScroll);content.Margin=new Thickness(22,16,18,40);pageScroll.Content=content;pageScroll.PreviewMouseWheel+=ScrollSettingsWheel;
  backButton.Click+=(s,e)=>BackPage();System.Windows.Automation.AutomationProperties.SetAutomationId(backButton,"ConsoleBack");
  PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Left&&(Keyboard.Modifiers&ModifierKeys.Alt)!=0){BackPage();e.Handled=true;}};
  SetTheme("a",false);
  Closed+=(s,e)=>{if(volumeRuntime!=null)volumeRuntime.Changed-=SyncQuickVoice;};
 }
 Grid AddNavigation(Panel nav,string route,string icon){
  var row=new Grid();nav.Children.Add(row);var button=Button(row,route,()=>ShowPage(route));button.HorizontalAlignment=HorizontalAlignment.Stretch;button.HorizontalContentAlignment=HorizontalAlignment.Left;button.MinHeight=34;button.Padding=new Thickness(36,6,12,6);button.Margin=new Thickness(0,2,0,2);button.FontSize=12;button.BorderBrush=Brushes.Transparent;button.SetResourceReference(Control.BackgroundProperty,"DeskSidebar");button.ToolTip="打开"+route;System.Windows.Automation.AutomationProperties.SetAutomationId(button,"Route-"+route);
  var glyph=TerminalStyle.Icon(icon,15);glyph.HorizontalAlignment=HorizontalAlignment.Left;glyph.Margin=new Thickness(12,0,0,0);glyph.IsHitTestVisible=false;TerminalAppearance.BindTree(glyph);row.Children.Add(glyph);navigation[route]=button;return row;
 }
 public void SetTheme(string theme,bool persist){
  displayedTheme=theme=="b"?"b":"a";TerminalAppearance.Palette(Resources,displayedTheme);
  brandIcon.Child=TerminalStyle.Icon(displayedTheme=="b"?"hat":"bow",26);TerminalAppearance.BindTree(brandIcon.Child);
  foreach(var pair in themeButtons){bool selected=pair.Key==displayedTheme;pair.Value.Tag=selected?"selected":null;System.Windows.Automation.AutomationProperties.SetItemStatus(pair.Value,selected?"已选中":"未选中");}
  UpdateArtwork();VisualEffects.Apply(this);trend.InvalidateVisual();ring.InvalidateVisual();if(persist&&ThemeChanged!=null)ThemeChanged(displayedTheme);
 }
 public void BackPage(){if(page=="总览"&&routeHistory.Count==0)return;string route=routeHistory.Count>0?routeHistory.Pop():"总览";returning=true;try{ShowPage(route);}finally{returning=false;}}
 void BeginNavigation(string name){if(page!=name&&!returning)routeHistory.Push(page);breadcrumb.Text="工作空间  /  "+Category(name)+ (Category(name)==name?"":"  /  "+name);backButton.Content="← 返回"+(routeHistory.Count>0?routeHistory.Peek():"总览");backButton.Visibility=name=="总览"?Visibility.Collapsed:Visibility.Visible;}
 void AddPageHeading(string name){
  bool compact=name!="总览"&&name!="性能监控"&&name!="动作与移动";backButton.Margin=new Thickness(0,0,0,compact?8:16);var heading=new StackPanel{Margin=new Thickness(0,0,0,compact?12:20)};if(!compact)heading.Children.Add(Text("COMPANION / YOUR DESKTOP WORKSPACE",10));heading.Children.Add(new TextBlock{Text=name=="总览"?"今天的线索，尽在掌握。":name=="动作与移动"?"动作与陪伴":name,FontSize=compact?22:27,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,4,0,compact?0:8)});if(!compact)heading.Children.Add(Text(name=="动作与移动"?"让你的桌面搭档，拥有刚刚好的存在感。":"留意桌面的变化，也照顾好你的工作节奏。",12));content.Children.Add(heading);
 }
 void UpdateArtwork(){
  if(displayedTheme=="b")previewArtwork.Child=new KidEmblem();
  else{string path=Path.Combine(home,"conan.png");if(File.Exists(path)){var bitmap=new BitmapImage(new Uri(path));var pet=new Image{Source=new CroppedBitmap(bitmap,new Int32Rect(0,0,192,208)),Width=150,Height=163};RenderOptions.SetBitmapScalingMode(pet,BitmapScalingMode.NearestNeighbor);previewArtwork.Child=pet;}}
 }
 static void SettingDescription(Panel parent,string text){var hint=Text(text,11);hint.Margin=new Thickness(0,0,0,10);parent.Children.Add(hint);}
 void AddQuickVoice(Panel parent){
  var row=new Grid{Margin=new Thickness(0,8,0,18)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(190)});row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(82)});parent.Children.Add(row);
  row.Children.Add(new TextBlock{Text="柯南原声音量",FontSize=13,VerticalAlignment=VerticalAlignment.Center});
  quickVoiceVolume=new Slider{Minimum=0,Maximum=100,TickFrequency=5,IsSnapToTickEnabled=true,SmallChange=5,LargeChange=10,Margin=new Thickness(0,0,12,0),ToolTip="只调整柯南配音；0%为静音，调整后自动保存。"};Grid.SetColumn(quickVoiceVolume,1);row.Children.Add(quickVoiceVolume);System.Windows.Automation.AutomationProperties.SetName(quickVoiceVolume,"柯南原声音量");
  quickVoiceLevel=Text("",12);quickVoiceLevel.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(quickVoiceLevel,2);row.Children.Add(quickVoiceLevel);System.Windows.Automation.AutomationProperties.SetName(quickVoiceLevel,"柯南原声音量值");
  if(volumeRuntime!=Voices){if(volumeRuntime!=null)volumeRuntime.Changed-=SyncQuickVoice;volumeRuntime=Voices;if(volumeRuntime!=null)volumeRuntime.Changed+=SyncQuickVoice;}
  quickIdleVoice=new CheckBox{Content="待机与漫游语音",IsChecked=Voices!=null&&Voices.Settings.Ambient,ToolTip="点击切换，立即保存；关闭后不影响点击配音、手动试听和下班提醒。"};parent.Children.Add(quickIdleVoice);quickIdleVoice.Click+=(s,e)=>{if(Voices==null)return;Voices.Settings.Ambient=quickIdleVoice.IsChecked==true;try{Voices.Apply();}catch(IOException ex){MessageBox.Show(ex.Message,"待机语音");}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message,"待机语音");}};SettingDescription(parent,"控制空闲动作、漫游出发及自动踢球的声音。下方“随机台词”仅控制文字气泡。");
  SyncQuickVoice();quickVoiceVolume.ValueChanged+=(s,e)=>{if(syncingQuickVoice||Voices==null)return;Voices.SetVolume(quickVoiceVolume.Value/100);try{Voices.Apply();}catch(IOException ex){MessageBox.Show(ex.Message,"原声音量");}catch(UnauthorizedAccessException ex){MessageBox.Show(ex.Message,"原声音量");}SyncQuickVoice();};
 }
 void SyncQuickVoice(){if(quickVoiceVolume==null)return;syncingQuickVoice=true;try{if(quickIdleVoice!=null)quickIdleVoice.IsChecked=Voices!=null&&Voices.Settings.Ambient;quickVoiceVolume.IsEnabled=Voices!=null&&Voices.Settings.Enabled;double value=Voices==null?0:Voices.Settings.Volume*100;quickVoiceVolume.Value=value;quickVoiceLevel.Text=Voices==null?"未连接":!Voices.Settings.Enabled?"配音已关闭":value<=0?"0% · 静音":value.ToString("F0")+"%";}finally{syncingQuickVoice=false;}}
 void AddFootballSwitch(Panel parent){
  foreach(var option in new[]{new[]{"footballKeep","表演结束后保留桌面足球"},new[]{"footballSeek","空闲时寻找足球并颠球"},new[]{"footballDrag","允许手动拖动足球"},new[]{"footballToss","松手时按拖动速度抛球"}}){string key=option[0];var toggle=new CheckBox{Content=option[1],IsChecked=PetFlag!=null&&PetFlag(key),ToolTip="点击切换并保存"};System.Windows.Automation.AutomationProperties.SetName(toggle,key);toggle.Click+=(s,e)=>{if(PetSetting!=null&&PetFlag(key)!=(toggle.IsChecked==true))PetSetting(key);};parent.Children.Add(toggle);companionSwitches[key]=toggle;}
  var check=new CheckBox{Content="足球保持人物所在高度",IsChecked=PetFlag!=null&&PetFlag("footballLevel"),ToolTip="点击切换，立即保存。切换时结束当前足球表演，下次触球使用新规则。"};System.Windows.Automation.AutomationProperties.SetAutomationId(check,"FootballLevel");check.Click+=(s,e)=>{if(PetFlag!=null&&PetFlag("footballLevel")!=(check.IsChecked==true))PetSetting("footballLevel");};parent.Children.Add(check);companionSwitches["footballLevel"]=check;SettingDescription(parent,"开启：抽射和倒挂金钩出球后在触球高度水平飞行并左右反弹。关闭：使用重力下落。向上挑球、颠球保留原轨迹。");
 }
 void BuildMovement(){
  var oldArt=previewArtwork.Parent as Decorator;if(oldArt!=null)oldArt.Child=null;
  movementButtons.Clear();sizeButtons.Clear();companionSwitches.Clear();var grid=Columns(content,2);grid.ColumnDefinitions[0].Width=new GridLength(1,GridUnitType.Star);grid.ColumnDefinitions[1].Width=new GridLength(255);
  StackPanel settings;var card=Panel("让柯南按你的节奏活动","move",out settings);Place(grid,card,0);settings.Children.Add(modeLabel);SettingDescription(settings,"单选卡用于活动方式，开关用于持续设置，动作演示独立放置。");
  SettingsLabel(settings,"声音与待机台词");AddQuickVoice(settings);
  SettingsLabel(settings,"活动模式","选择一种移动方式；下面的开关可独立调整。");var primary=new UniformGrid{Columns=3};settings.Children.Add(primary);var secondary=new UniformGrid{Columns=2,Margin=new Thickness(0,4,0,12)};settings.Children.Add(secondary);
  foreach(var item in new[]{new[]{"全屏二维漫游","roam"},new[]{"追随鼠标","follow"},new[]{"定点休息","rest"},new[]{"滑板巡游","skate"},new[]{"四角巡逻","patrol"}}){string mode=item[1];var button=Button(mode=="skate"||mode=="patrol"?(Panel)secondary:primary,item[0],()=>{setMovement(mode);Refresh(last);});button.HorizontalAlignment=HorizontalAlignment.Stretch;button.Padding=new Thickness(12,16,24,16);button.MinHeight=mode=="skate"||mode=="patrol"?46:84;button.FontSize=12;if(mode!="skate"&&mode!="patrol"){primary.Children.Remove(button);var cell=new Grid();primary.Children.Add(cell);cell.Children.Add(button);button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Padding=new Thickness(12,34,22,12);var glyph=TerminalStyle.Icon(mode=="roam"?"move":mode=="follow"?"glasses":"clock",20);glyph.HorizontalAlignment=HorizontalAlignment.Left;glyph.VerticalAlignment=VerticalAlignment.Top;glyph.Margin=new Thickness(13,17,0,0);glyph.IsHitTestVisible=false;cell.Children.Add(glyph);}movementButtons[mode]=button;}
  SettingsLabel(settings,"屏幕与陪伴行为");foreach(var item in new[]{new[]{"跨显示器漫游","crossScreens","开启后可在相邻显示器之间走路、追随鼠标、滑板巡游；关闭后只在当前屏幕活动。"},new[]{"视线跟随","gaze","眼神轻轻跟随鼠标的位置。"},new[]{"随机台词","lines","空闲时偶尔显示一句，不影响通知开关。"}}){string key=item[1];var check=new CheckBox{Content=item[0],IsChecked=PetFlag!=null&&PetFlag(key),HorizontalAlignment=HorizontalAlignment.Stretch};System.Windows.Automation.AutomationProperties.SetAutomationId(check,"Companion"+key);check.Click+=(s,e)=>{if(PetFlag!=null&&PetFlag(key)!=(check.IsChecked==true))PetSetting(key);};settings.Children.Add(check);SettingDescription(settings,item[2]);companionSwitches[key]=check;}
  SettingsLabel(settings,"足球互动");AddFootballSwitch(settings);
  SettingsLabel(settings,"角色大小","保持原版像素比例，最近邻清晰放大。");var sizes=new UniformGrid{Columns=5};settings.Children.Add(sizes);foreach(var size in new[]{new[]{"小","0.85"},new[]{"标准","1.15"},new[]{"大","2"},new[]{"特大","3"},new[]{"超大","5"}}){string value=size[1];var b=Button(sizes,size[0],()=>{PetSetting(value);Refresh(last);});b.HorizontalAlignment=HorizontalAlignment.Stretch;b.FontSize=12;b.Padding=new Thickness(7,8,18,8);sizeButtons[value]=b;}SettingDescription(settings,"原版像素比例 · 最近邻清晰放大");
  var routes=new WrapPanel{Margin=new Thickness(0,12,0,0)};settings.Children.Add(routes);Button(routes,"数据与备份",()=>ShowPage("数据与备份"));Button(routes,"原声配音",()=>ShowPage("原声配音"));Button(routes,"动作演示与足球 →",()=>ShowPage("动作演示"));
  StackPanel rail;var preview=Panel("陪伴预览",displayedTheme=="b"?"hat":"bow",out rail);Place(grid,preview,1);
  var art=new Border{Child=previewArtwork,Tag="theme-art",CornerRadius=new CornerRadius(12),Padding=new Thickness(12),Background=TerminalStyle.Gradient("#253757","#30345C"),Height=205,Margin=new Thickness(0,4,0,12)};rail.Children.Add(art);UpdateArtwork();rail.Children.Add(Text("A / 柯南 · B / 月下基德",12));rail.Children.Add(Text("外观切换保留当前页面与选项。B的礼帽、单片眼镜、月亮和扑克牌属于控制台装饰，桌宠仍使用原版形象。",11));rail.Children.Add(Text("待机时小憩，工作或消息到来时唤醒；保留现有动作与配音调度。",11));
 }
}
