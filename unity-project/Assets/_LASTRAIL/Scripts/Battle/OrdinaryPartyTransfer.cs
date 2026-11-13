using System;
using System.Linq;
using System.Collections.Generic;

namespace LastRail.Battle {
 public readonly struct OrdinaryPartyAnchor {
  public readonly int Slot; public readonly long X,Y,Z;
  public OrdinaryPartyAnchor(int slot,long x,long y,long z){if(slot<0||slot>5||x< -1000000000||x>1000000000||y< -1000000000||y>1000000000||z< -1000000000||z>1000000000)throw new ArgumentOutOfRangeException();Slot=slot;X=x;Y=y;Z=z;}
 }
 public readonly struct OrdinaryPartyPose {
  public readonly OrdinaryPartyAnchor Anchor;public readonly long Generation;public readonly bool Alive;
  public OrdinaryPartyPose(OrdinaryPartyAnchor anchor,long generation,bool alive){if(generation<1)throw new ArgumentOutOfRangeException(nameof(generation));Anchor=anchor;Generation=generation;Alive=alive;}
 }
 // Position, movement speed and time remain owned by the executing spatial model.
 public interface IOrdinaryPartyMotion {
  string GeometryHash{get;}
  OrdinaryPartyPose Read(string actor);
  long MovementRevision(string actor);
  bool CanMove(string actor);
  void ValidateLeg(string actor,OrdinaryPartyAnchor from,OrdinaryPartyAnchor to);
  long Move(string actor,OrdinaryPartyAnchor target);
  bool Cancel(string actor,long movementRevision);
 }
 public sealed class OrdinaryPartyTransfer:IDisposable {
  public enum Phase {Merge,Traverse,Deploy,Complete,Cancelled}
  public sealed class Plan {
   public string Actor{get;} public OrdinaryPartyAnchor Merge{get;} public OrdinaryPartyAnchor Arrival{get;} public OrdinaryPartyAnchor Deployment{get;} public bool NearRow{get;}
   public Plan(string actor,OrdinaryPartyAnchor merge,OrdinaryPartyAnchor arrival,OrdinaryPartyAnchor deployment,bool nearRow){Actor=actor;Merge=merge;Arrival=arrival;Deployment=deployment;NearRow=nearRow;}
  }
  readonly IOrdinaryPartyMotion motion;readonly Plan[] plans;readonly long[] generations;readonly string geometryHash;
  int index;long pendingRevision;bool pending;
  public Phase State{get;private set;}=Phase.Merge;
  public int DestinationSlot{get;}
  public bool IsMoving(string actor)=>pending&&plans[index].Actor==actor&&State!=Phase.Cancelled&&State!=Phase.Complete;
  OrdinaryPartyTransfer(OrdinaryPartyTransfer other,IOrdinaryPartyMotion motion){
   this.motion=motion??throw new ArgumentNullException(nameof(motion));Need(motion.GeometryHash==other.geometryHash,"Transfer clone geometry differs");
   plans=other.plans;generations=(long[])other.generations.Clone();geometryHash=other.geometryHash;index=other.index;pendingRevision=other.pendingRevision;pending=other.pending;State=other.State;DestinationSlot=other.DestinationSlot;
  }
  public OrdinaryPartyTransfer Clone(IOrdinaryPartyMotion newMotion)=>new OrdinaryPartyTransfer(this,newMotion);
  // shortcut: one traveller owns the whole passage; allow overlapping travellers only after measured clearance/reservation policy is supplied.
  public OrdinaryPartyTransfer(IOrdinaryPartyMotion motion,IReadOnlyList<string> orderedPhysicalHosts,bool threatOnLeft,IEnumerable<Plan> plans){
   this.motion=motion??throw new ArgumentNullException(nameof(motion));this.plans=plans?.ToArray()??throw new ArgumentNullException(nameof(plans));
   Need(orderedPhysicalHosts!=null&&orderedPhysicalHosts.Count==6&&orderedPhysicalHosts.All(x=>!string.IsNullOrWhiteSpace(x))&&orderedPhysicalHosts.Distinct().Count()==6,"Six bound physical hosts, engine first, required");
   Need(this.plans.Length==5&&this.plans.All(p=>p!=null&&!string.IsNullOrWhiteSpace(p.Actor))&&this.plans.Select(p=>p.Actor).Distinct().Count()==5,"Five unique numbered actors required");
   geometryHash=motion.GeometryHash;Need(!string.IsNullOrWhiteSpace(geometryHash),"Bound geometry required");DestinationSlot=threatOnLeft?5:1;generations=new long[5];
   for(int i=0;i<5;i++){
    var p=this.plans[i];var pose=motion.Read(p.Actor);generations[i]=pose.Generation;
    Need(pose.Alive&&pose.Anchor.Slot>0&&p.Merge.Slot==pose.Anchor.Slot&&p.Arrival.Slot==DestinationSlot&&p.Deployment.Slot==DestinationSlot&&p.NearRow==(i%2==0),"Bound non-engine merge and numbered end-car positions required");
    if(i>0)Need(threatOnLeft?p.Deployment.X>this.plans[i-1].Deployment.X:p.Deployment.X<this.plans[i-1].Deployment.X,"Threat-relative battle numbers must retain order");
    motion.ValidateLeg(p.Actor,pose.Anchor,p.Merge);motion.ValidateLeg(p.Actor,p.Merge,p.Arrival);motion.ValidateLeg(p.Actor,p.Arrival,p.Deployment);
   }
   Need(this.plans.Select(p=>p.Merge).Distinct().Count()==5&&this.plans.Select(p=>p.Arrival).Distinct().Count()==5&&this.plans.Select(p=>p.Deployment).Distinct().Count()==5,"Distinct reserved waiting and combat anchors required");
  }
  static void Need(bool ok,string message){if(!ok)throw new ArgumentException(message);}
  OrdinaryPartyAnchor Target(Plan p)=>State==Phase.Merge?p.Merge:State==Phase.Traverse?p.Arrival:p.Deployment;
  public void Step(){
   if(State==Phase.Complete||State==Phase.Cancelled)return;
   if(motion.GeometryHash!=geometryHash){Dispose();return;}
   for(int i=0;i<plans.Length;i++){var pose=motion.Read(plans[i].Actor);if(!pose.Alive||pose.Generation!=generations[i]){Dispose();return;}}
   var actor=plans[index];var target=Target(actor);
   if(pending){
    if(motion.MovementRevision(actor.Actor)!=pendingRevision){pending=false;State=Phase.Cancelled;return;}
    if(!motion.Read(actor.Actor).Anchor.Equals(target))return;
    pending=false;
    if(State==Phase.Merge)State=Phase.Traverse;
    else if(State==Phase.Traverse)State=Phase.Deploy;
    else {index++;State=index==plans.Length?Phase.Complete:Phase.Merge;}
    return;
   }
   if(!motion.CanMove(actor.Actor))return;
   try{pendingRevision=motion.Move(actor.Actor,target);pending=true;}catch{State=Phase.Cancelled;throw;}
  }
  public void Dispose(){if(State==Phase.Complete||State==Phase.Cancelled)return;if(pending)motion.Cancel(plans[index].Actor,pendingRevision);pending=false;State=Phase.Cancelled;}
 }
 // Borrows the existing spatial owner; action readiness is supplied by the node's action arbiter.
 internal sealed class NativePartyMotion:IOrdinaryPartyMotion {
  readonly BOrdinarySpatialOwner spatial;readonly BOrdinarySpatialGeometry geometry;readonly IBBoardingActors actors;readonly Func<string,bool> canMove;
  internal NativePartyMotion(BOrdinarySpatialOwner spatial,BOrdinarySpatialGeometry geometry,IBBoardingActors actors,Func<string,bool> canMove){this.spatial=spatial??throw new ArgumentNullException(nameof(spatial));this.geometry=geometry??throw new ArgumentNullException(nameof(geometry));this.actors=actors??throw new ArgumentNullException(nameof(actors));this.canMove=canMove??throw new ArgumentNullException(nameof(canMove));if(spatial.Hash!=geometry.Hash)throw new ArgumentException("Party spatial/geometry binding differs");}
  public string GeometryHash=>geometry.Hash;
  public long MovementRevision(string actor)=>spatial.OperatorMovementRevision(actor);
  static BMmPoint Point(OrdinaryPartyAnchor a)=>new BMmPoint(a.X,a.Y,a.Z);
  public OrdinaryPartyPose Read(string actor){spatial.ObserveOperators(actors);int slot=spatial.Slot(actor);var p=spatial.Position(actor);var origin=geometry.World(slot,new BMmPoint(0,0,0));return new OrdinaryPartyPose(new OrdinaryPartyAnchor(slot,p.X-origin.X,p.Y-origin.Y,p.Z-origin.Z),actors.ProjectileTargetGeneration(actor),actors.Hp(actor)>0);}
  public bool CanMove(string actor)=>canMove(actor);
  public void ValidateLeg(string actor,OrdinaryPartyAnchor from,OrdinaryPartyAnchor to){
   if(from.Slot<=0||to.Slot<=0||from.Slot>=geometry.Count||to.Slot>=geometry.Count||geometry.OperatorSpeed(actor)<=0||!geometry.ContainsLocalRoof(from.Slot,Point(from))||!geometry.ContainsLocalRoof(to.Slot,Point(to)))throw new ArgumentException("Party movement requires supplied speed and legal roof anchors");
   var start=geometry.World(from.Slot,Point(from));var end=geometry.World(to.Slot,Point(to));var route=geometry.Route(start,from.Slot,to.Slot,end);
   if(route==null||route.Length<2||!route[0].Equals(start)||!route[route.Length-1].Equals(end)||!Enumerable.Range(1,route.Length-1).All(i=>geometry.Los(route[i-1],route[i])))throw new ArgumentException("Supplied connected unobstructed party route required");
  }
  public long Move(string actor,OrdinaryPartyAnchor target){spatial.Move(actor,target.Slot,Point(target),GeometryHash,actors);return spatial.OperatorMovementRevision(actor);}
  public bool Cancel(string actor,long movementRevision)=>spatial.CancelOperatorMove(actor,movementRevision,GeometryHash,actors);
 }
}
