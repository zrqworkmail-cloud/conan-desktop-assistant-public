using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;
partial class TerminalWindow {
 readonly StackPanel pinnedSettingsTop=new StackPanel(),pinnedSettingsBottom=new StackPanel();
 void ClearPinnedSettings(){var parent=backButton.Parent as Panel;if(parent!=null)parent.Children.Remove(backButton);pinnedSettingsTop.Children.Clear();pinnedSettingsBottom.Children.Clear();pinnedSettingsTop.Visibility=pinnedSettingsBottom.Visibility=Visibility.Collapsed;}
 void PinTaskbarSettings(StackPanel body,UIElement preview,UIElement tabs,UIElement apply){
  var old=backButton.Parent as Panel;if(old!=null)old.Children.Remove(backButton);
  // Replace the scrolling page title with a compact fixed title and back action.
  if(content.Children.Count>0&&content.Children[0] is StackPanel)content.Children.RemoveAt(0);
  if(body.Children.Count>0)body.Children.RemoveAt(0);
  var heading=new DockPanel();backButton.Margin=new Thickness(0,0,14,4);DockPanel.SetDock(backButton,Dock.Left);heading.Children.Add(backButton);heading.Children.Add(new TextBlock{Text="任务栏外观 · 实时预览",FontSize=18,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});pinnedSettingsTop.Children.Add(heading);
  foreach(var element in new UIElement[]{taskbarPreviewState,preview,tabs}){body.Children.Remove(element);pinnedSettingsTop.Children.Add(element);}body.Children.Remove(apply);pinnedSettingsBottom.Children.Add(apply);pinnedSettingsTop.Visibility=pinnedSettingsBottom.Visibility=Visibility.Visible;TerminalAppearance.BindTree(pinnedSettingsTop);TerminalAppearance.BindTree(pinnedSettingsBottom);VisualEffects.Apply(pinnedSettingsTop);VisualEffects.Apply(pinnedSettingsBottom);
 }
 static void SettingsLabel(Panel parent,string title,string hint=""){var label=new StackPanel();label.Children.Add(new TextBlock{Text=title,FontSize=14,FontWeight=FontWeights.SemiBold});if(!String.IsNullOrEmpty(hint))SettingDescription(label,hint);var mark=new Border{Child=label,BorderThickness=new Thickness(3,0,0,0),Padding=new Thickness(10,1,0,1),Margin=new Thickness(0,14,0,10)};mark.SetResourceReference(Border.BorderBrushProperty,"DeskAccent");parent.Children.Add(mark);}
 // Each section is explicit about its first control; no inference from control captions or translated labels.
 static void GroupSettings(StackPanel body,params Tuple<UIElement,string,string>[] markers){
  var children=body.Children.Cast<UIElement>().ToArray();if(children.Length==0)return;
  var starts=markers.Select(m=>new{Index=m.Item1==null?0:Array.IndexOf(children,m.Item1),Title=m.Item2,Hint=m.Item3}).Where(m=>m.Index>=0).OrderBy(m=>m.Index).ToArray();if(starts.Length==0)return;body.Children.Clear();
  int before=starts[0].Index;for(int i=0;i<before;i++)body.Children.Add(children[i]);
  for(int i=0;i<starts.Length;i++){var marker=starts[i];var inner=new StackPanel();inner.Children.Add(new TextBlock{Text=marker.Title,FontSize=14,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,5)});if(!String.IsNullOrEmpty(marker.Hint))SettingDescription(inner,marker.Hint);int end=i+1<starts.Length?starts[i+1].Index:children.Length;for(int j=marker.Index;j<end;j++)inner.Children.Add(children[j]);var frame=new Border{Child=inner,Padding=new Thickness(14),Margin=new Thickness(0,0,0,12),CornerRadius=new CornerRadius(10),BorderThickness=new Thickness(1),Tag="theme-art"};frame.SetResourceReference(Border.BorderBrushProperty,"DeskLine");frame.SetResourceReference(Border.BackgroundProperty,"DeskBase");body.Children.Add(frame);}
 }
 static Tuple<UIElement,string,string> SettingsGroup(UIElement first,string title,string hint=""){return Tuple.Create(first,title,hint);}
 static UIElement SettingRow(FrameworkElement control){return (UIElement)control.Parent;}
}
