using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Presentation.Battle;

public sealed class ReinaBarragePresentationTests {
 [Test] public void ActualCommitHoldsUntilFilmEndsThenEachPulsePresentsOnceAndDisposeUnsubscribes(){
  var method=typeof(OrdinaryReinaBarrageHostTests).GetMethod("Driver",BindingFlags.Static|BindingFlags.NonPublic);
  var driver=(CombatSceneDriver)method.Invoke(null,new object[]{false});
  var type=typeof(CombatSceneDriver).Assembly.GetType("LastRail.Presentation.Battle.ReinaBarragePresentation");
  Assert.NotNull(type,"Missing cinematic-to-native-impact binding");
  Action finish=null;int starts=0,stops=0,impacts=0,remaining=-1;
  var owner=(IDisposable)Activator.CreateInstance(type,new object[]{driver,(Func<bool>)(()=>true),
   (Func<Action,IDisposable>)(end=>{starts++;finish=end;return new Lease(()=>stops++);}),
   (Action<JObject>)(_=>impacts++),(Action<int>)(n=>remaining=n)});
  driver.RequestSkill("film","op_reina","op_reina.ultimate");driver.Advance(170000L*48,48);
  Assert.AreEqual(1,starts);Assert.True(driver.HasPresentationHold);Assert.AreEqual(4,remaining);
  driver.Advance(170000L*300,300);Assert.AreEqual(0,impacts);
  finish();finish();Assert.False(driver.HasPresentationHold);Assert.AreEqual(1,stops);
  driver.Advance(170000L*224,224);Assert.AreEqual(4,impacts);Assert.AreEqual(0,remaining);
  owner.Dispose();owner.Dispose();Assert.False(driver.HasPresentationHold);driver.Retire();
 }
 static CombatSceneDriver Driver()=>(CombatSceneDriver)typeof(OrdinaryReinaBarrageHostTests).GetMethod("Driver",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{false});
 [TestCase(false)][TestCase(true)] public void SkipOrOwnerDisposalReleasesOnlyOwnHoldAndNeverReplaysImpact(bool dispose){
  var d=Driver();Action finish=null;int impacts=0;IDisposable other=null;
  d.BarragePresentationEvent+=r=>{if((string)r["kind"]=="COMMIT")other=d.AcquirePresentationHold();};
  var owner=new ReinaBarragePresentation(d,()=>true,end=>{finish=end;return new Lease(()=>{});},r=>impacts++,_=>{});
  d.RequestSkill("skip","op_reina","op_reina.ultimate");d.Advance(170000L*48,48);
  if(dispose)owner.Dispose();else finish();
  Assert.True(d.HasPresentationHold);other.Dispose();Assert.False(d.HasPresentationHold);finish();
  d.Advance(170000L*224,224);Assert.AreEqual(dispose?0:4,impacts);owner.Dispose();d.Retire();
 }
 [Test] public void UnreadyMovieFallsThroughWithoutHoldingBattle(){
  var d=Driver();int sounds=0;
  using(var owner=new ReinaBarragePresentation(d,()=>false,_=>throw new Exception("Must not play"),_=>sounds++,_=>{})){
   d.RequestSkill("fallback","op_reina","op_reina.ultimate");d.Advance(170000L*272,272);d.Advance(0,224);Assert.False(d.HasPresentationHold);Assert.AreEqual(4,sounds);
  }d.Retire();
 }
 [Test] public void PlaybackFailureCannotLeakHoldOrSuppressNativeImpacts(){
  var d=Driver();int impacts=0;
  using(var owner=new ReinaBarragePresentation(d,()=>true,_=>throw new InvalidOperationException("synthetic decoder failure"),_=>impacts++,_=>{})){
   d.RequestSkill("fail","op_reina","op_reina.ultimate");d.Advance(170000L*48,48);Assert.False(d.HasPresentationHold);Assert.NotNull(d.LastPresentationFailure);
   d.Advance(170000L*224,224);Assert.AreEqual(4,impacts);
  }d.Retire();
 }
 [Test] public void FullSelectionUsesExistingViewingMarkerAndSimplifiedDoesNotConsumeIt(){
  int marks=0;var policy=new CombatPresentationPolicy(()=>false,()=>{marks++;return true;});policy.Mode=CombatPresentationMode.Simplified;
  Assert.False(policy.TryReserveFullCinematic());Assert.AreEqual(0,marks);policy.Mode=CombatPresentationMode.FirstUseFull;
  Assert.True(policy.TryReserveFullCinematic());Assert.False(policy.TryReserveFullCinematic());Assert.AreEqual(1,marks);
 }
 [Test] public void RebindingConsumesCurrentCommittedProgressWithoutReplayingFilmOrPastImpact(){
  var d=Driver();d.RequestSkill("already-running","op_reina","op_reina.ultimate");d.Advance(170000L*131,131);int count=0,remaining=-1;
  using(var owner=new ReinaBarragePresentation(d,()=>true,_=>throw new Exception("No late film"),_=>count++,n=>remaining=n)){
   Assert.AreEqual(3,remaining);d.Advance(170000L*141,141);Assert.AreEqual(3,count);Assert.AreEqual(0,remaining);
  }d.Retire();
 }
 sealed class Lease:IDisposable {Action end;public Lease(Action end){this.end=end;}public void Dispose(){var old=end;end=null;old?.Invoke();}}
}
