using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;

public sealed class OrdinaryPartyTransferTests {
 sealed class Motion:IOrdinaryPartyMotion {
  internal readonly Dictionary<string,OrdinaryPartyPose> Poses=new Dictionary<string,OrdinaryPartyPose>();
  internal readonly List<string> Commands=new List<string>();
  internal OrdinaryPartyAnchor Pending; internal string Actor; internal long Revision; internal int Cancels;
  internal bool Ready=true;
  public string GeometryHash=>"test-geometry";
  public OrdinaryPartyPose Read(string actor)=>Poses[actor];
  public long MovementRevision(string actor)=>Revision;
  public bool CanMove(string actor)=>Ready;
  public void ValidateLeg(string actor,OrdinaryPartyAnchor from,OrdinaryPartyAnchor to) {if(to.Slot==0)throw new ArgumentException();}
  public long Move(string actor,OrdinaryPartyAnchor target) {Assert.IsNull(Actor,"Only one traveller may own the passage");Actor=actor;Pending=target;Commands.Add(actor);return ++Revision;}
  public bool Cancel(string actor,long revision) {if(Actor!=actor||Revision!=revision)return false;Actor=null;Cancels++;return true;}
  internal void Arrive(){var p=Poses[Actor];Poses[Actor]=new OrdinaryPartyPose(Pending,p.Generation,true);Actor=null;}
 }
 static (Motion motion,OrdinaryPartyTransfer transfer) Create(bool left) {
  var m=new Motion();int end=left?5:1;
  var plans=Enumerable.Range(0,5).Select(i=>{
   string id="actor"+i;var start=new OrdinaryPartyAnchor(3,100+i,0,0);m.Poses[id]=new OrdinaryPartyPose(start,1,true);
   return new OrdinaryPartyTransfer.Plan(id,new OrdinaryPartyAnchor(3,10+i,0,0),new OrdinaryPartyAnchor(end,20+i,0,0),new OrdinaryPartyAnchor(end,left?i:-i,0,i%2==0?1:-1),i%2==0);
  }).ToArray();
  return (m,new OrdinaryPartyTransfer(m,new[]{"engine","a","b","c","d","e"},left,plans));
 }
 [TestCase(true)][TestCase(false)] public void EachActorDeploysOffThePassageBeforeNextActorMerges(bool left){
  var f=Create(left);Assert.AreEqual(left?5:1,f.transfer.DestinationSlot);
  for(int actor=0;actor<5;actor++)for(int phase=0;phase<3;phase++){
   Assert.AreEqual((OrdinaryPartyTransfer.Phase)phase,f.transfer.State);
   f.transfer.Step();Assert.AreEqual("actor"+actor,f.motion.Actor);int count=f.motion.Commands.Count;
   f.transfer.Step();Assert.AreEqual(count,f.motion.Commands.Count,"Repeated observations cannot resubmit movement");
   f.motion.Arrive();f.transfer.Step();
  }
  Assert.AreEqual(OrdinaryPartyTransfer.Phase.Complete,f.transfer.State);Assert.AreEqual(15,f.motion.Commands.Count);
  f.transfer.Step();Assert.AreEqual(15,f.motion.Commands.Count);
 }
 [TestCase(false)][TestCase(true)] public void DownOrGenerationChangeCancelsOnlyOwnedMoveAndNeverResumes(bool generation){
  var f=Create(true);f.transfer.Step();var p=f.motion.Poses["actor3"];
  f.motion.Poses["actor3"]=new OrdinaryPartyPose(p.Anchor,generation?2:1,generation);
  f.transfer.Step();Assert.AreEqual(OrdinaryPartyTransfer.Phase.Cancelled,f.transfer.State);Assert.AreEqual(1,f.motion.Cancels);
  f.motion.Poses["actor3"]=p;f.transfer.Step();Assert.AreEqual(1,f.motion.Commands.Count);
 }
 [Test] public void ActionOwnerCanDeferDepartureAndDisposeIsIdempotent(){
  var f=Create(true);f.motion.Ready=false;f.transfer.Step();Assert.IsEmpty(f.motion.Commands);
  f.motion.Ready=true;f.transfer.Step();f.transfer.Dispose();f.transfer.Dispose();f.transfer.Step();Assert.AreEqual(1,f.motion.Cancels);
 }
 [Test] public void ReplacedMovementIsNotCancelledByOldTransfer(){
  var f=Create(true);f.transfer.Step();f.motion.Revision++;f.transfer.Dispose();Assert.AreEqual(0,f.motion.Cancels);
 }
 [Test] public void ObservingReplacementEndsTransferWithoutCancellingTheNewMovement(){
  var f=Create(true);f.transfer.Step();f.motion.Revision++;f.transfer.Step();
  Assert.AreEqual(OrdinaryPartyTransfer.Phase.Cancelled,f.transfer.State);Assert.AreEqual(0,f.motion.Cancels);Assert.AreEqual("actor0",f.motion.Actor);
  f.transfer.Step();f.transfer.Dispose();Assert.AreEqual(1,f.motion.Commands.Count);Assert.AreEqual(0,f.motion.Cancels);
 }
 [Test] public void ClonePreservesPendingReservationWithoutResubmittingAndUsesNewMotionOwner(){
  var f=Create(true);f.transfer.Step();var replacement=new Motion();foreach(var p in f.motion.Poses)replacement.Poses.Add(p.Key,p.Value);
  replacement.Actor=f.motion.Actor;replacement.Pending=f.motion.Pending;replacement.Revision=f.motion.Revision;
  var copy=f.transfer.Clone(replacement);copy.Step();Assert.IsEmpty(replacement.Commands);replacement.Arrive();copy.Step();copy.Step();
  Assert.AreEqual("actor0",replacement.Actor);Assert.AreEqual(OrdinaryPartyTransfer.Phase.Traverse,copy.State);Assert.AreEqual("actor0",f.motion.Actor);Assert.AreEqual(1,f.motion.Commands.Count);
 }
 [TestCase("engine")][TestCase("duplicate")][TestCase("order")][TestCase("row")]
 public void InvalidReservationsRejectBeforeAnyMovement(string fault){
  var m=new Motion();var plans=Enumerable.Range(0,5).Select(i=>{string id="actor"+i;var a=new OrdinaryPartyAnchor(3,i,0,0);m.Poses[id]=new OrdinaryPartyPose(a,1,true);return new OrdinaryPartyTransfer.Plan(fault=="duplicate"?"actor0":id,a,new OrdinaryPartyAnchor(fault=="engine"?0:5,i,0,0),new OrdinaryPartyAnchor(5,fault=="order"?-i:i,0,0),fault=="row"?true:i%2==0);}).ToArray();
  Assert.Throws<ArgumentException>(()=>new OrdinaryPartyTransfer(m,new[]{"engine","a","b","c","d","e"},true,plans));Assert.IsEmpty(m.Commands);
 }
}
