using System;
using System.Windows;
partial class ConanPet {
 bool footballKeep=true,footballSeek=true,footballDrag=true,footballToss=true;double nextBallSearch;
 readonly FootballWorld football=new FootballWorld();FootballWindow footballWindow;string footballMode;
 double footballStart,footballUntil,footballKickStart,footballDistance,footballReboundAt,footballGroundTop;
 int footballRepeatCount=1,footballShotsLeft;bool footballLevel, footballRebounded,footballReleased,footballManual,footballCombo,footballHit;
 BubbleMessage footballStartingNotice;
 string FootballSprite(string action){return action=="flick"?"flick-up":action=="bicycle"?"bicycle":"kick-play";}
 Point FootballContact(string action){double x=FootballWorld.ContactX(action);if(direction<0)x=192-x;return new Point(Left+(Width-192*scale)/2+x*scale,Top+90+FootballWorld.ContactY(action)*scale);}
 void StartFootball(string mode){
  if(mode=="stop"){StopFootball();return;}
  bool idle=mode.StartsWith("idle-",StringComparison.Ordinal);string request=idle?mode.Substring(5):mode;if(idle&&football.Active&&footballMode=="rest"){if(footballSeek)nextBallSearch=clock.Elapsed.TotalSeconds;return;}
  if(!signature.Has("juggle")||!signature.Has("flick-up")||!signature.Has("kick-play")||!signature.Has("bicycle")){Say("足球素材尚未准备好。");return;}
  StopFootball();footballManual=!idle;footballStartingNotice=footballManual?messages.Current:null;
  if(footballManual&&!IsVisible){Show();ClampWindow();visibilityItem.Text="隐藏桌宠";}
  if(voices.Playing)voices.Stop();messages.ClearCasual(clock.Elapsed.TotalSeconds);signatureUntil=actionUntil=skateDemoUntil=walkUntil=0;
  footballStart=clock.Elapsed.TotalSeconds;footballGroundTop=Top;footballCombo=request=="throw";
  footballShotsLeft=idle?1:footballRepeatCount;footballUntil=footballStart+(request=="juggle"?6.4*footballShotsLeft:30*footballShotsLeft);
  awakeUntil=footballStart+15;footballDistance=0;footballReleased=footballHit=footballRebounded=false;
  if(footballWindow==null){footballWindow=new FootballWindow(football){Owner=this,AllowDrag=()=>footballDrag,ThrowOnRelease=()=>footballToss};footballWindow.Released+=()=>{if(footballKeep){footballMode="rest";footballUntil=Double.PositiveInfinity;nextBallSearch=clock.Elapsed.TotalSeconds+12;return;}footballMode="chase";footballCombo=true;footballReleased=false;footballStart=clock.Elapsed.TotalSeconds;footballRebounded=false;footballGroundTop=Top;};footballWindow.Dismissed+=StopFootball;}
  string action=request=="throw"?"flick":request;footballMode=action;footballKickStart=footballStart;
  if(action=="juggle"){var p=FootballContact("flick");football.Throw(p.X,p.Y,0,0,12*scale);}
  else if(action=="flick"){var p=FootballContact(action);football.Throw(p.X,p.Y,0,0,12*scale);}
  else{
   var p=FootballContact(action);double sim=FootballWorld.ContactTime(action)*FootballWorld.MotionRate;
   double vx=(action=="bicycle"?200:30)*scale*direction,vy=(action=="bicycle"?-350:-200)*scale;
   football.Throw(p.X-vx*sim,p.Y-vy*sim-400*scale*sim*sim,vx,vy,12*scale);
  }
  footballWindow.RenderBall();footballWindow.Show();
 }
 void StepFootball(double now,double dt,Rect area){double before=football.VX;football.Step(dt,area.Left,area.Top,area.Right,area.Bottom);if(before*football.VX<0&&!footballRebounded){footballRebounded=true;footballReboundAt=now;}}
 FootballWorld ForecastFootball(double seconds,Rect area){var future=new FootballWorld{Active=true,X=football.X,Y=football.Y,VX=football.VX,VY=football.VY,Radius=football.Radius};if(football.HorizontalFlight)future.Strike(football.VX,football.VY,true);for(double left=seconds;left>.000001;){double step=Math.Min(left,1.0/120);future.Step(step,area.Left,area.Top,area.Right,area.Bottom);left-=step;}return future;}
 void FinishFootball(){if(!football.Active||footballMode=="rest")return;if(!footballKeep){StopFootball();return;}desktopMovement=null;footballMode="rest";footballUntil=Double.PositiveInfinity;nextBallSearch=clock.Elapsed.TotalSeconds+20;walkUntil=0;nextIdleGesture=clock.Elapsed.TotalSeconds+8;}
 Rect BallWorkArea(){var source=PresentationSource.FromVisual(this);var from=source==null?System.Windows.Media.Matrix.Identity:source.CompositionTarget.TransformFromDevice;var to=source==null?System.Windows.Media.Matrix.Identity:source.CompositionTarget.TransformToDevice;var center=to.Transform(new Point(football.X,football.Y));var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)center.X,(int)center.Y)).WorkingArea;return new Rect(from.Transform(new Point(screen.Left,screen.Top)),from.Transform(new Point(screen.Right,screen.Bottom)));}
 bool TickRestingFootball(double now,double dt,bool work){if(!IsVisible||menuOpen||DesktopActions.FullScreenForeground()){footballWindow.Hide();return false;}if(!footballWindow.IsVisible)footballWindow.Show();if(!football.Dragging){var area=BallWorkArea();football.Step(dt,area.Left,area.Top,area.Right,area.Bottom);footballWindow.RenderBall();}if(footballSeek&&!work&&!dragging&&!football.Dragging&&!sprite.IsMouseOver&&messages.Current==null&&now>=nextBallSearch&&now>=walkUntil&&now>=signatureUntil){desktopMovement=null;footballMode="retrieve";footballStart=now;footballUntil=now+120;footballManual=false;footballDistance=0;return true;}return false;}
 bool RetrieveFootball(double now,double dt){var area=BallWorkArea();StepFootball(now,dt,area);footballWindow.RenderBall();if(now-footballStart<1.5){DisplayExtra(8,now-footballStart,ActionTiming.SearchFrameMilliseconds);return true;}double pad=(Width-192*scale)/2;double targetLeft=football.X-pad-143*scale,targetTop=football.Y-90-151*scale;double x=Left,y=Top,oldX=x,oldY=y;direction=football.X>=Left+Width/2?1:-1;bool arrived=MovementSpace().Step(ref x,ref y,new Point(targetLeft,targetTop),ActionTiming.RetrieveSpeed(scale)*Math.Min(.1,Math.Max(0,dt)));if(arrived&&(new Vector(x-targetLeft,y-targetTop)).Length>100*scale){FinishFootball();return true;}Left=x;Top=y;footballDistance+=Math.Sqrt((x-oldX)*(x-oldX)+(y-oldY)*(y-oldY));DisplayFrame(direction>0?1:2,ActionTiming.WalkFrame(footballDistance,scale),now,footballDistance);if(Math.Abs(Left-targetLeft)<24*scale&&Math.Abs(Top-targetTop)<90*scale){StartFootball("idle-juggle");}return true;}
 void StopFootball(){if(!football.Active)return;if(voices!=null&&voices.Playing&&(voices.CurrentId=="finish1"||voices.CurrentId=="finish2"))voices.Stop();football.Stop();footballMode=null;if(footballWindow!=null)footballWindow.Hide();nextWalk=clock.Elapsed.TotalSeconds+4;nextIdleGesture=clock.Elapsed.TotalSeconds+20;}
 bool TickFootball(double now,double dt){
  if(!football.Active)return false;bool work=searching||skills.Engine.Data.Focus.Running||assistantSnapshot!=null&&assistantSnapshot.CodexRow>=0;
  if(footballMode=="rest")return TickRestingFootball(now,dt,work);
  if(dragging||work&&!footballManual||messages.Current!=null&&messages.Current.Notification&&!Object.ReferenceEquals(messages.Current,footballStartingNotice)||now>=footballUntil){FinishFootball();return false;}
  if(menuOpen||DesktopActions.FullScreenForeground()){footballStart+=dt;footballKickStart+=dt;footballReboundAt+=dt;footballUntil+=dt;footballWindow.Hide();return false;}
  if(!footballWindow.IsVisible)footballWindow.Show();bodyMove.Y=0;awakeUntil=now+8;Rect area=WorkArea();
  if(football.Dragging){sprite.Display(frames[0,0]);return true;}
  if(footballMode=="retrieve")return RetrieveFootball(now,dt);
  if(footballMode=="juggle"){
   double elapsed=now-footballStart;int frame=(int)(elapsed/.1)%8;sprite.Display(signature.Frame("juggle",(frame+.01)/8));double pad=(Width-192*scale)/2;
   football.X=Left+pad+143*scale;football.Y=Top+90+(151-FootballWorld.JuggleHeight(elapsed-.3))*scale;football.Angle=elapsed*120;footballWindow.RenderBall();return true;
  }
  if(footballMode=="flick"||footballMode=="volley"||footballMode=="bicycle"){
   string action=footballMode;double elapsed=now-footballKickStart,contact=FootballWorld.ContactTime(action);int frame=FootballWorld.ActionFrame(action,elapsed);
   bool mirror=direction<0;
   sprite.Display(signature.Frame(FootballSprite(action),(frame+.01)/signature.Count(FootballSprite(action))),mirror);
   if(action=="flick"&&!footballReleased){var p=FootballContact(action);football.X=p.X;football.Y=p.Y;}
   else StepFootball(now,dt,area);
   if(!footballReleased&&elapsed+.0000001>=contact){
    var p=FootballContact(action);double dx=football.X-p.X,dy=football.Y-p.Y;footballReleased=true;
    footballHit=action=="flick"||dx*dx+dy*dy<=20*scale*20*scale;
    if(footballHit){if(action!="flick"||!footballCombo)footballShotsLeft--;footballRebounded=false;footballReboundAt=Double.PositiveInfinity;int shotDirection=action=="bicycle"&&Math.Abs(football.VX)>.001?-Math.Sign(football.VX):direction;football.Strike(shotDirection*FootballWorld.ShotVX(action)*scale,FootballWorld.ShotVY(action)*scale,footballLevel&&action!="flick");if(action!="flick")voices.PlayAmbient("kick",!IsVisible||DesktopActions.FullScreenForeground(),!footballManual);}
   }
   if(elapsed>=FootballWorld.ActionDuration(action)){
    footballMode=action=="flick"&&footballCombo||!footballHit?"chase":"flight";footballStart=now;
   }
   footballWindow.RenderBall();return true;
  }
  StepFootball(now,dt,area);footballWindow.RenderBall();
  if(footballMode=="flight"){
   if(now-footballStart>=.6&&(footballRebounded&&now-footballReboundAt>=.45||Math.Abs(football.VX)<2*scale&&football.VY==0)){
    if(footballShotsLeft<=0){FinishFootball();return false;}
    footballMode="flick";footballCombo=true;footballGroundTop=Top;footballKickStart=now;footballReleased=footballHit=false;return true;
   }
   sprite.Display(frames[0,0]);return true;
  }
  // Move toward a predicted contact point while keeping a grounded starting height.
  string shot=(footballRepeatCount-footballShotsLeft)%2==1?"bicycle":"volley";
  var predicted=ForecastFootball(FootballWorld.ContactTime(shot),area);direction=predicted.X>=Left+Width/2?1:-1;
  int approachDirection=shot=="bicycle"&&Math.Abs(predicted.VX)>.001?Math.Sign(predicted.VX):direction;
  double px=FootballWorld.ContactX(shot);if(approachDirection<0)px=192-px;double padX=(Width-192*scale)/2;
  double targetLeft=predicted.X-padX-px*scale,targetTop=Math.Max(footballGroundTop,predicted.Y-90-FootballWorld.ContactY(shot)*scale);
  double x=Left,y=Top,oldX=x,oldY=y;
  MovementLogic.Approach(ref x,ref y,targetLeft,targetTop,170*scale*dt*FootballWorld.MotionRate,area.Left-padX-40*scale,area.Top-90-40*scale,area.Right-padX-152*scale,area.Bottom-90-202*scale);
  Left=x;Top=y;footballDistance+=Math.Sqrt((x-oldX)*(x-oldX)+(y-oldY)*(y-oldY));DisplayFrame(direction>0?1:2,ActionTiming.WalkFrame(footballDistance,scale),now,footballDistance);
  var foot=new Point(Left+padX+px*scale,Top+90+FootballWorld.ContactY(shot)*scale);double fx=predicted.X-foot.X,fy=predicted.Y-foot.Y;
  if(fx*fx+fy*fy<=20*scale*20*scale){direction=approachDirection;footballMode=shot;footballKickStart=now;footballReleased=footballHit=false;}
  return true;
 }
}
