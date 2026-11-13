using System;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Composes the existing entry, boarding and spatial owners with the ordinary node's sole health owner.
 internal sealed partial class OrdinaryBoardingExecution:IBBoardingActors {
  readonly OrdinaryFiveCombatFactory factory;readonly BCombatLocalModel health;readonly BBoardingRoster roster;
  readonly OrdinaryBoardingGeometry geometry;BEnemyBoardingOwner boarding;BWaveEntryOwner entry;
  long tick,sequence,lastStep=-1;bool started,retired;
  internal OrdinaryBoardingExecution(OrdinaryFiveCombatFactory factory,BCombatLocalModel health){
   this.factory=factory;this.health=health;var source=factory.BoardingSource??throw new ArgumentException("Original boarding source required.");
   roster=new BBoardingRoster(factory.Original.Roof.Slots.Select(s=>new BBoardingRoster.Entity(s.OperatorId,"OPERATOR",s.OperatorId,s.PhysicalOrder)).Concat(source.Boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Select(i=>new BBoardingRoster.Entity(i.InstanceId,"LOCAL_ENEMY",i.DefinitionId,-1))),source.PhysicalHostIds,factory.Step/10);
   geometry=new OrdinaryBoardingGeometry(factory,roster);var policy=BWaveBoardingPolicy.FromReceived(source.Boarding.OriginalSource.VisitDepth,source.Boarding.Definition);
   boarding=new BEnemyBoardingOwner(source.Boarding.OriginalSource,roster,policy,geometry.Paths()){Spatial=new BOrdinarySpatialOwner(geometry,source.Boarding.OriginalSource,roster)};
   entry=new BWaveEntryOwner(source.Boarding.OriginalSource,policy,roster.StepMicroseconds);InitializeAttacks();
  }
  OrdinaryBoardingExecution(OrdinaryBoardingExecution other,BCombatLocalModel health){factory=other.factory;this.health=health;roster=other.roster;geometry=other.geometry;boarding=other.boarding.Clone();entry=other.entry.Clone();tick=other.tick;sequence=other.sequence;lastStep=other.lastStep;started=other.started;retired=other.retired;CloneAttacks(other);CloneParty(other);}
  internal OrdinaryBoardingExecution CloneOwned(BCombatLocalModel newHealth)=>new OrdinaryBoardingExecution(this,newHealth);
  public long Tick=>tick;
  public int Hp(string id)=>health.Hp(id);
  public long ProjectileTargetGeneration(string id)=>Contains(id)?boarding.Generation(id):health.Hp(id)>0?1:2;
  internal bool Contains(string id)=>roster.Entities.TryGetValue(id,out var e)&&e.Kind=="LOCAL_ENEMY";
  long Next()=>checked(++sequence);
  void Active(){if(retired)throw new InvalidOperationException("Boarding node retired.");}
  internal void Start(long at){Active();if(started||at<0)throw new InvalidOperationException("Boarding starts once at the original combat boundary.");started=true;BeginStep(at);}
  internal void BeginStep(long at){Active();if(!started)return;if(at<tick)throw new ArgumentException("Boarding clock reversed.");tick=at;if(at==lastStep)return;lastStep=at;StepPartyTransfer();entry.BeforeStep(boarding,this,Next);boarding.Step(this,Next);boarding.ObserveSpatial(this);}
  internal void EndStep(long at,bool flying){Active();if(!started)return;if(at!=tick)throw new ArgumentException("Boarding observation tick differs.");boarding.ObserveDeaths(this,Next);boarding.ObserveSpatial(this);entry.AfterStep(boarding,this,Next,flying||HasFlying);}
  internal bool Registered(string id)=>!retired&&started&&boarding.Registered(id);
  internal bool Targetable(string id)=>Registered(id)&&Hp(id)>0&&boarding.Targetable(id);
  internal int Slot(string id)=>boarding.Slot(id);
  internal string Category(string id)=>boarding.CategoryAtImpact(id);
  internal long HitRadiusMm(string id)=>(long)geometry.HitCapsule(id)["radiusMm"];
  internal long HitHeightMm(string id)=>(long)geometry.HitCapsule(id)["heightMm"];
  internal bool Complete=>started&&entry.Victory;
  internal int CompletedWaves=>entry.CompletedWaves;
  internal JObject Target(string id,long at,long currentFront){
   Active();if(at!=tick||!Registered(id))throw new ArgumentException("Current registered boarding target required.");
   var foot=boarding.Spatial.Position(id);var sourceFoot=geometry.LocalPoint(id,"FOOT");var hit=geometry.LocalPoint(id,"HIT");var capsule=geometry.HitCapsule(id);var center=BMmPoint.Read(capsule["centerMm"]);long delta=currentFront-(factory.Original.Rail.InitialFrontArcMm??throw new ArgumentException("Initial rail front required."));
   long[] Point(BMmPoint p)=>new[]{checked(foot.X+p.X-sourceFoot.X+delta),checked(foot.Y+p.Y-sourceFoot.Y),checked(foot.Z+p.Z-sourceFoot.Z)};
   var feet=Point(center);feet[1]-=(long)capsule["heightMm"]/2;
   return new JObject{["id"]=id,["generation"]=boarding.Generation(id),["slot"]=checked(Slot(id)+1),["domain"]="RAIL",["eligible"]=Targetable(id),["anchorMm"]=new JArray(Point(hit)),["feetMm"]=new JArray(feet),["heightMm"]=capsule["heightMm"].DeepClone(),["radiusMm"]=capsule["radiusMm"].DeepClone()};
  }
  internal JObject Capture()=>new JObject{["sourceHash"]=factory.BoardingSource.SourceHash,["tick"]=tick,["sequence"]=sequence,["lastStep"]=lastStep,["started"]=started,["retired"]=retired,["boarding"]=boarding.Capture(),["entry"]=entry.Capture(),["spatial"]=boarding.Spatial.Capture(),["attacks"]=CaptureAttacks()};
  internal void Retire(){if(retired)return;RetireParty();retired=true;RetireAttacks();boarding.Terminal(tick,Next);}
 }
}
