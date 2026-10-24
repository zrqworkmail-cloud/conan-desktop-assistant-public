using System;
using System.IO;
using System.Windows.Controls;
partial class TerminalWindow {
 void BuildStartup(){
  var integration=new StartupIntegration(Path.Combine(home,"ConanPet.exe"));StackPanel body;content.Children.Add(Panel("启动与桌面入口","tasks",out body));
  SettingsLabel(body,"Windows 登录启动");var state=Text("",12);var toggle=new CheckBox{Content="登录 Windows 时自动启动柯南",IsChecked=integration.Enabled,ToolTip="仅在你点击时更新当前用户启动项，无需管理员权限"};body.Children.Add(toggle);body.Children.Add(state);
  Action refresh=()=>{toggle.IsChecked=integration.Enabled;state.Text=!integration.Enabled?"开机自启：未登记":integration.CurrentVersion?"已登记当前版本；最终启动状态以 Windows 启动应用设置为准。":"启动项指向其他版本。点击下方按钮可更新为当前版本。";};refresh();
  toggle.Click+=(s,e)=>{try{integration.SetEnabled(toggle.IsChecked==true);refresh();}catch(Exception ex){refresh();System.Windows.MessageBox.Show(ex.Message,"开机自启");}};
  SettingDescription(body,"请先把完整解压后的程序放在固定位置。移动目录或换新版后，在新版点击“更新自启到当前版本”，再更新桌面快捷方式。不会在打开新版时悄悄修改现有启动路径。");
  SettingsLabel(body,"桌面入口与版本路径");var row=new WrapPanel();body.Children.Add(row);Button(row,"更新自启到当前版本",()=>{if(!integration.Enabled)throw new InvalidOperationException("请先打开上方自启开关。");integration.SetEnabled(true);refresh();});Button(row,"创建 / 更新桌面快捷方式",()=>{integration.CreateShortcut();state.Text="桌面快捷方式已指向当前版本。";}).Tag="primary";body.Children.Add(Text("当前程序："+Path.Combine(home,"ConanPet.exe"),11));
  StackPanel window;content.Children.Add(Panel("任务栏窗口","cpu",out window));var cfg=assistant.Config.Monitor;SettingsLabel(window,"窗口显示与操作");MonitorToggle(window,"启动时提示尚缺的连接与授权",()=>cfg.StartupAuthorizationPrompt,v=>cfg.StartupAuthorizationPrompt=v);MonitorToggle(window,"显示任务栏监控窗口",()=>cfg.Taskbar,v=>cfg.Taskbar=v);MonitorToggle(window,"锁定任务栏位置",()=>cfg.TaskbarPositionLocked,v=>cfg.TaskbarPositionLocked=v);SettingDescription(window,"解锁后可在所选任务栏内拖动，松手保存位置；锁定后禁止拖动，点击仍可打开详情。屏幕、双行排列、宽高自适应、透明度、文字颜色及四边间距集中在任务栏外观页；修改后点击“应用并保存”。");var actions=new WrapPanel();window.Children.Add(actions);Button(actions,"任务栏外观与排版 →",()=>ShowPage("任务栏外观"));Button(actions,"采样与资源预警 →",()=>ShowPage("监控与预警"));Button(actions,"Codex 额度与重置 →",ShowQuotaPage);
 }
 public void ShowSensorPage(){monitorTab="数据来源";ShowPage("监控与预警");}
 public void ShowQuotaPage(){monitorTab="Codex额度";ShowPage("监控与预警");}
}
