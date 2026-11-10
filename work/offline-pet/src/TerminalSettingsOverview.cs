using System;using System.Windows;using System.Windows.Controls;
partial class TerminalWindow {
 void BuildSettingsOverview(){
  StackPanel intro;content.Children.Add(Panel("找到你要调整的设置","tasks",out intro));SettingDescription(intro,"按目的选择下方分类，也可以在右上角输入“下班”“额度”“任务栏颜色”等关键词。开关通常立即保存；任务栏外观和下班时段需点击页面内的保存按钮。");
  var grid=Columns(content,2);
  SettingsOverviewCard(grid,0,"提醒与日程","管理到点、提前提醒、预警与通知。",new[]{"下班倒计时","提醒方式","资源预警","网络与代理","系统通知"});
  SettingsOverviewCard(grid,1,"桌宠与声音","角色移动、动作、气泡和原声。",new[]{"动作与移动","动作演示","气泡外观","原声配音"});
  var system=Columns(content,2);
  SettingsOverviewCard(system,0,"系统监控与任务栏","选择监控、历史和任务栏外观。",new[]{"监控与预警","任务栏外观","性能监控","线索档案馆"});
  SettingsOverviewCard(system,1,"Codex 与连接","额度、会话、记录、订阅和微信。",new[]{"Codex 控制台","系统与网络"});
  var other=Columns(content,2);
  SettingsOverviewCard(other,0,"文件与工具","搜索文件、设置快捷入口和管理工具。",new[]{"文件搜索","快捷入口","效率工具","插件市场"});
  SettingsOverviewCard(other,1,"启动与数据","自启、桌面快捷方式、备份和迁移。",new[]{"启动与窗口","数据与备份"});
 }
 void SettingsOverviewCard(Grid grid,int column,string title,string description,string[] routes){StackPanel body;string icon=title=="提醒与日程"?"bell":title=="桌宠与声音"?"move":title=="系统监控与任务栏"?"cpu":title=="Codex 与连接"?"network":title=="文件与工具"?"folder":"tasks";var frame=Panel(title,icon,out body);frame.Margin=new Thickness(column==0?0:7,0,column==0?7:0,14);SettingDescription(body,description);var row=new WrapPanel();body.Children.Add(row);foreach(string value in routes){string route=value;var button=Button(row,route,()=>ShowPage(route));button.ToolTip="打开 "+route;}Place(grid,frame,column);}
}
