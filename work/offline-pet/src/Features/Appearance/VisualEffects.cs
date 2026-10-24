using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
public static class VisualEffects {
 public static string Mode="original";static bool initialized;
 class Saved {public Brush Original,Applied;}
 static readonly DependencyProperty SourceProperty=DependencyProperty.RegisterAttached("SourceColor",typeof(Color),typeof(VisualEffects));
 static readonly DependencyProperty SavedProperty=DependencyProperty.RegisterAttached("SavedBrushes",typeof(Dictionary<DependencyProperty,Saved>),typeof(VisualEffects));
 public static bool Valid(string mode){return mode=="original"||mode=="soft"||mode=="neon";}
 public static void Initialize(){if(initialized)return;initialized=true;EventManager.RegisterClassHandler(typeof(FrameworkElement),FrameworkElement.LoadedEvent,new RoutedEventHandler((s,e)=>{if(Mode!="original")ApplyOne((DependencyObject)s);}),true);}
 static byte Shift(byte value,int delta){return (byte)Math.Max(0,Math.Min(255,value+delta));}
 public static Brush Paint(Color color,bool text){if(Mode=="original"||color.A==0)return new SolidColorBrush(color);int change=text?3:Mode=="neon"?23:9;var first=Color.FromArgb(color.A,Shift(color.R,change),Shift(color.G,change),Shift(color.B,change));var last=Color.FromArgb(color.A,Shift(color.R,-change),Shift(color.G,-change/2),Shift(color.B,Mode=="neon"?change/2:-change));var brush=new LinearGradientBrush(first,last,Mode=="neon"?35:90);brush.SetValue(SourceProperty,color);brush.Freeze();return brush;}
 static void Convert(DependencyObject item,DependencyProperty property,bool text){var current=item.GetValue(property) as Brush;if(current==null)return;var values=(Dictionary<DependencyProperty,Saved>)item.GetValue(SavedProperty);Saved saved=null;if(values!=null)values.TryGetValue(property,out saved);Brush original=saved!=null&&Object.ReferenceEquals(saved.Applied,current)?saved.Original:current;
  object tagged=original.ReadLocalValue(SourceProperty);bool generated=tagged is Color;if(generated)original=new SolidColorBrush((Color)tagged);
  // WPF text services cast the editing foreground to SolidColorBrush during IME composition.
  if(text&&(item is System.Windows.Controls.Primitives.TextBoxBase||item is PasswordBox)){if(original is SolidColorBrush)item.SetCurrentValue(property,original);if(values!=null)values.Remove(property);return;}
  if(Mode=="original"){if(saved!=null){if(Object.ReferenceEquals(saved.Applied,current))item.SetCurrentValue(property,original);values.Remove(property);}else if(generated)item.SetCurrentValue(property,original);return;}
  var solid=original as SolidColorBrush;if(solid==null||solid.Color.A==0)return;if(values==null){values=new Dictionary<DependencyProperty,Saved>();item.SetValue(SavedProperty,values);}var paint=Paint(solid.Color,text);values[property]=new Saved{Original=original,Applied=paint};item.SetCurrentValue(property,paint);
 }
 static void ApplyOne(DependencyObject item){if(item is SpriteView||item is Image)return;if(item is Border){Convert(item,Border.BackgroundProperty,false);Convert(item,Border.BorderBrushProperty,false);}if(item is Panel)Convert(item,Panel.BackgroundProperty,false);if(item is Control){Convert(item,Control.BackgroundProperty,false);Convert(item,Control.BorderBrushProperty,false);Convert(item,Control.ForegroundProperty,true);}if(item is TextBlock)Convert(item,TextBlock.ForegroundProperty,true);if(item is Shape){Convert(item,Shape.FillProperty,false);Convert(item,Shape.StrokeProperty,false);}}
 public static void Apply(DependencyObject root){Apply(root,new HashSet<DependencyObject>());}
 static void Apply(DependencyObject root,HashSet<DependencyObject> seen){if(root==null||!seen.Add(root))return;ApplyOne(root);if(root is Visual)for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)Apply(VisualTreeHelper.GetChild(root,i),seen);foreach(var child in LogicalTreeHelper.GetChildren(root))if(child is DependencyObject)Apply((DependencyObject)child,seen);}
 public static void SetMode(string mode){if(!Valid(mode))throw new ArgumentException("界面风格无效。");Initialize();Mode=mode;if(Application.Current!=null)foreach(Window window in Application.Current.Windows)Apply(window);}
}
