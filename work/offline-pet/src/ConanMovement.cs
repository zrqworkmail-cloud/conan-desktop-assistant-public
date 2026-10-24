using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Forms=System.Windows.Forms;
partial class ConanPet {
 bool crossScreens=true;DesktopMovement desktopMovement;
 DesktopMovement MovementSpace(){if(desktopMovement!=null)return desktopMovement;var source=PresentationSource.FromVisual(this);Matrix from=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;var displays=Forms.Screen.AllScreens.AsEnumerable();if(!crossScreens){Rect current=WorkArea();return desktopMovement=new DesktopMovement(new[]{new PetScreenArea{Bounds=current,Work=current}},Width,Height);}return desktopMovement=new DesktopMovement(displays.Select(s=>new PetScreenArea{Bounds=new Rect(from.Transform(new Point(s.Bounds.Left,s.Bounds.Top)),from.Transform(new Point(s.Bounds.Right,s.Bounds.Bottom))),Work=new Rect(from.Transform(new Point(s.WorkingArea.Left,s.WorkingArea.Top)),from.Transform(new Point(s.WorkingArea.Right,s.WorkingArea.Bottom)))}),Width,Height);}
 Point RandomDestination(){var areas=MovementSpace().Destinations(new Point(Left,Top));if(areas.Length==0)return new Point(Left,Top);Rect area=areas[random.Next(areas.Length)];return new Point(area.Left+random.NextDouble()*area.Width,area.Top+random.NextDouble()*area.Height);}
 void BeginDesktopTravel(){desktopMovement=null;moveX=Left;moveY=Top;Point destination=RandomDestination();if(moveMode=="patrol"){var areas=MovementSpace().Destinations(new Point(Left,Top));if(areas.Length>0){patrolIndex=(patrolIndex+1)%(4*areas.Length);Rect area=areas[patrolIndex/4];int corner=patrolIndex%4;destination=new Point(corner==0||corner==3?area.Left+Math.Min(20,area.Width/2):area.Right-Math.Min(20,area.Width/2),corner<2?area.Top+Math.Min(20,area.Height/2):area.Bottom-Math.Min(20,area.Height/2));}}else if(moveMode=="skate"){double farthest=(destination-new Point(moveX,moveY)).LengthSquared;for(int i=0;i<7;i++){Point candidate=RandomDestination();double d=(candidate-new Point(moveX,moveY)).LengthSquared;if(d>farthest){destination=candidate;farthest=d;}}}targetX=destination.X;targetY=destination.Y;}
 bool StepDesktopTravel(double distance){double before=moveX;bool arrived=MovementSpace().Step(ref moveX,ref moveY,new Point(targetX,targetY),distance);if(Math.Abs(moveX-before)>.0001)direction=moveX>before?1:-1;return arrived;}
}
