using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Input;
partial class TerminalWindow {
 sealed class SettingsSearchEntry {public string Title,Path,Route,Tab,Keywords;public override string ToString(){return Title+"    ·    "+Path;}}
 static readonly SettingsSearchEntry[] SettingsEntries={
  new SettingsSearchEntry{Title="下班倒计时",Path="提醒与值班 / 下班倒计时",Route="下班倒计时",Keywords="下班 时间 休息 工作日 调休 节假日"},
  new SettingsSearchEntry{Title="提醒方式与原声音量",Path="提醒与值班 / 提醒方式",Route="提醒方式",Keywords="提醒 声音 音量 气泡 原声 通知"},
  new SettingsSearchEntry{Title="资源与进程预警",Path="提醒与值班 / 资源预警",Route="资源预警",Keywords="cpu gpu 温度 内存 进程 阈值 报警 top"},
  new SettingsSearchEntry{Title="网络与代理提醒",Path="提醒与值班 / 网络与代理",Route="网络与代理",Keywords="网络 代理 vpn 超时 ip 提醒"},
  new SettingsSearchEntry{Title="系统通知",Path="提醒与值班 / 系统通知",Route="系统通知",Keywords="通知 授权 提醒 windows"},
  new SettingsSearchEntry{Title="Codex 额度查询与提醒",Path="Codex 控制台 / 额度与提醒",Route="Codex 控制台",Tab="额度与提醒",Keywords="codex 五小时 周 额度 查询 重置 预警 阈值 唤醒"},
  new SettingsSearchEntry{Title="Codex 重置与唤醒记录",Path="Codex 控制台 / 重置记录",Route="Codex 控制台",Tab="重置记录",Keywords="codex 历史 时间轴 重置 token 唤醒 记录"},
  new SettingsSearchEntry{Title="Codex 连接与微信",Path="Codex 控制台 / 连接与微信",Route="Codex 控制台",Tab="连接与微信",Keywords="codex 登录 授权 连接 测试 微信 server酱"},
  new SettingsSearchEntry{Title="Codex 订阅到期",Path="Codex 控制台 / 订阅",Route="Codex 控制台",Tab="订阅",Keywords="codex 订阅 套餐 到期 日期"},
  new SettingsSearchEntry{Title="任务栏显示项目与排序",Path="任务栏外观 / 显示内容",Route="任务栏外观",Tab="显示内容",Keywords="任务栏 项目 顺序 单位 标签 数值 额度"},
  new SettingsSearchEntry{Title="任务栏位置与屏幕",Path="任务栏外观 / 布局与屏幕",Route="任务栏外观",Tab="布局与屏幕",Keywords="任务栏 屏幕 位置 间距 双行 多显示器 列宽"},
  new SettingsSearchEntry{Title="任务栏配色与字体",Path="任务栏外观 / 配色与文字",Route="任务栏外观",Tab="配色与文字",Keywords="任务栏 颜色 配色 渐变 字体 字号 文字 高对比"},
  new SettingsSearchEntry{Title="任务栏占用条",Path="任务栏外观 / 占用条效果",Route="任务栏外观",Tab="占用条效果",Keywords="任务栏 占用条 背景 圆角 羽化 马赛克"},
  new SettingsSearchEntry{Title="历史趋势与流量",Path="系统与通知 / 线索档案馆",Route="线索档案馆",Keywords="历史 图表 曲线 统计 流量 cpu gpu 额度 token"},
  new SettingsSearchEntry{Title="Codex 额度历史曲线",Path="线索档案馆 / 额度轨迹",Route="线索档案馆",Tab="额度轨迹",Keywords="codex 额度 历史 曲线 五小时 周 重置"},
  new SettingsSearchEntry{Title="CPU / GPU 历史曲线",Path="线索档案馆 / 资源占用",Route="线索档案馆",Tab="资源占用",Keywords="cpu gpu 内存 性能 历史 曲线"},
  new SettingsSearchEntry{Title="流量历史",Path="线索档案馆 / 网络流量",Route="线索档案馆",Tab="网络流量",Keywords="网络 流量 下载 上传 历史 统计"},
  new SettingsSearchEntry{Title="对话气泡外观",Path="桌宠与声音 / 气泡外观",Route="气泡外观",Keywords="气泡 位置 文字 对话"},
  new SettingsSearchEntry{Title="柯南原声与音量",Path="桌宠与声音 / 原声配音",Route="原声配音",Keywords="声音 配音 音量 试听 语音"},
  new SettingsSearchEntry{Title="代理与出口信息",Path="系统与通知 / 系统与网络",Route="系统与网络",Keywords="ip vpn 代理 出口 网络 地区"},
  new SettingsSearchEntry{Title="下班与健康提醒",Path="提醒与值班 / 提醒",Route="提醒",Keywords="提醒 闹钟 日程 时间 倒计时"},
  new SettingsSearchEntry{Title="桌宠动作与移动",Path="桌宠与声音 / 动作与移动",Route="动作与移动",Keywords="柯南 走路 移动 大小 动画"},
  new SettingsSearchEntry{Title="开机启动与窗口",Path="系统与通知 / 启动与窗口",Route="启动与窗口",Keywords="开机 自启 快捷方式 窗口 锁定"}
 };
 static SettingsSearchEntry[] FindSettings(string query){query=(query??"").Trim();if(query.Length==0)return new SettingsSearchEntry[0];var entries=SettingsEntries.Concat(NavigationGroups.SelectMany(g=>g.Skip(1).Select(r=>new SettingsSearchEntry{Title=r,Path=g[0],Route=r,Keywords=r}))).Concat(new[]{new SettingsSearchEntry{Title="插件市场",Path="工作台",Route="插件市场",Keywords="插件 安装 下载 更新"}});return entries.Where(x=>(x.Title+" "+x.Path+" "+x.Keywords).Replace(" ","").IndexOf(query.Replace(" ",""),StringComparison.OrdinalIgnoreCase)>=0).OrderBy(x=>x.Title.Equals(query,StringComparison.OrdinalIgnoreCase)?0:x.Title.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0?1:2).GroupBy(x=>x.Route+"|"+(x.Tab??""),StringComparer.Ordinal).Select(g=>g.First()).Take(10).ToArray();}
 void OpenSetting(SettingsSearchEntry entry){if(entry==null)return;if(entry.Route=="Codex 控制台"&&entry.Tab!=null)codexTab=entry.Tab;if(entry.Route=="任务栏外观"&&entry.Tab!=null)taskbarTab=entry.Tab;if(entry.Route=="线索档案馆"&&entry.Tab!=null)archiveTab=entry.Tab;ShowPage(entry.Route);}
 void InitializeSettingsSearch(TextBox search,FrameworkElement anchor){var list=new ListBox{MaxHeight=330,MinWidth=350,HorizontalContentAlignment=HorizontalAlignment.Stretch};System.Windows.Automation.AutomationProperties.SetName(list,"设置搜索结果");var panel=new Border{Child=list,Padding=new Thickness(5),CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(1)};panel.SetResourceReference(Border.BackgroundProperty,"DeskSurface");panel.SetResourceReference(Border.BorderBrushProperty,"DeskLine");var popup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,AllowsTransparency=true,StaysOpen=false,Child=panel};Action open=()=>{var selected=list.SelectedItem as SettingsSearchEntry;if(selected==null)return;popup.IsOpen=false;search.Clear();OpenSetting(selected);};search.TextChanged+=(s,e)=>{list.ItemsSource=FindSettings(search.Text);if(list.Items.Count>0)list.SelectedIndex=0;popup.IsOpen=search.IsKeyboardFocusWithin&&list.Items.Count>0;search.ToolTip=search.Text.Length>0&&list.Items.Count==0?"没有找到设置，试试其他关键词":null;};search.GotKeyboardFocus+=(s,e)=>{if(list.Items.Count>0)popup.IsOpen=true;};search.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){open();e.Handled=true;}else if(e.Key==Key.Down&&list.Items.Count>0){list.SelectedIndex=Math.Min(list.Items.Count-1,list.SelectedIndex+1);e.Handled=true;}else if(e.Key==Key.Up&&list.Items.Count>0){list.SelectedIndex=Math.Max(0,list.SelectedIndex-1);e.Handled=true;}else if(e.Key==Key.Escape){popup.IsOpen=false;e.Handled=true;}};list.MouseDoubleClick+=(s,e)=>open();list.MouseLeftButtonUp+=(s,e)=>{if(list.SelectedItem!=null)open();};}
}
