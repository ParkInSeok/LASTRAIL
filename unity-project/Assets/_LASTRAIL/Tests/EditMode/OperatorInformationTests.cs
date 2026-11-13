#if UNITY_EDITOR
using LastRail.Battle;
using LastRail.Presentation.Battle;
using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class OperatorInformationTests {
  [Test] public async Task MemorySelectionPublishesReadyWithoutLoadingAndDetailDoesNotReadAgain(){
   int reads=0;var states=new System.Collections.Generic.List<string>();
   using(var c=new OperatorInformationController((t,ct)=>{reads++;return Task.FromResult(Snapshot(t));})){
    c.Bind(f=>states.Add(f.Status));var selection=c.SelectAsync(Target());Assert.That(selection.IsCompleted,Is.True);await selection;
    Assert.That(states,Is.EqualTo(new[]{"CLOSED","READY"}));c.OpenDetail(0);c.CloseDetail();Assert.That(reads,Is.EqualTo(1));
   }
  }
  static OperatorInformationTarget Target(string id="op_mira",long generation=1)=>new OperatorInformationTarget("run",id,id,generation);
  static OperatorEquipmentInformation Item(string id="one",long revision=1)=>new OperatorEquipmentInformation(id,revision,"장비",1,null,"확정 효과");
  static OperatorInformationSnapshot Snapshot(OperatorInformationTarget t,long revision=1,string item="one")=>new OperatorInformationSnapshot(t,revision,new[]{Item(item,revision),null,null},null,"");
  [TestCase(true)][TestCase(false)] public async Task UnknownOrAbsentItemDoesNotEraseInstanceRevisionFloor(bool unknown){
   int phase=0;using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(new OperatorInformationSnapshot(x,phase+1,phase==1?(unknown?null:new OperatorEquipmentInformation[3]):new[]{new OperatorEquipmentInformation("same",5,"",1,null,"",phase==0?5:4),null,null},null,"")))){c.Bind(_=>{});await c.SelectAsync(Target());phase=1;await c.RefreshAsync();phase=2;await c.RefreshAsync();Assert.That(c.Frame.Status,Is.EqualTo("FAILED"));}
  }
  [Test] public async Task FailureDoesNotEraseRevisionFloor(){
   int call=0;using(var c=new OperatorInformationController((x,ct)=>++call==2?Task.FromException<OperatorInformationSnapshot>(new IOException()):Task.FromResult(Snapshot(x,call==1?4:3)))){c.Bind(_=>{});await c.SelectAsync(Target());await c.RefreshAsync();await c.RefreshAsync();Assert.That(c.Frame.Status,Is.EqualTo("FAILED"));}
  }
  [Test] public async Task InstanceOrEffectRegressionFailsEvenIfInventoryRevisionIncreases(){
   int revision=1;using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(new OperatorInformationSnapshot(x,revision,new[]{new OperatorEquipmentInformation("same",2,"",1,null,"",revision==1?2:1),null,null},null,"")))){c.Bind(_=>{});await c.SelectAsync(Target());revision=2;await c.RefreshAsync();Assert.That(c.Frame.Status,Is.EqualTo("FAILED"));}
  }
  [Test] public async Task ApprovedNavigationConsumesOnlyFirstWorldClickAndBackMovesOneLevel(){
   var a=Target();var b=Target("op_eve");using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(Snapshot(x)))){c.Bind(_=>{});await c.ClickActorAsync(a);c.OpenDetail(0);await c.ClickActorAsync(b);Assert.That(c.Frame.Target,Is.SameAs(a));Assert.That(c.Frame.DetailSlot,Is.EqualTo(-1));await c.ClickActorAsync(b);Assert.That(c.Frame.Target,Is.SameAs(b));c.OpenDetail(0);c.Back();Assert.That(c.Frame.Visible,Is.True);c.Back();Assert.That(c.Frame.Visible,Is.False);await c.ClickActorAsync(a);await c.ClickActorAsync(a);Assert.That(c.Frame.Visible,Is.False);await c.ClickActorAsync(a);c.OpenDetail(0);c.ClickEmptyWorld();Assert.That(c.Frame.Visible,Is.True);c.ClickEmptyWorld();Assert.That(c.Frame.Visible,Is.False);}
  }
  [Test] public async Task DetailClosePreservesSelectionAndEmptySlotsNeverOpen(){
   var t=Target();using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(Snapshot(x)))){c.Bind(_=>{});await c.SelectAsync(t);c.OpenDetail(0);Assert.That(c.Frame.DetailSlot,Is.EqualTo(0));c.CloseDetail();Assert.That(c.Frame.Target,Is.SameAs(t));Assert.That(c.Frame.Snapshot.Slot(0).InstanceId,Is.EqualTo("one"));c.OpenDetail(1);Assert.That(c.Frame.DetailSlot,Is.EqualTo(-1));await c.SelectAsync(t);Assert.That(c.Frame.Visible,Is.True);}
  }
  [Test] public async Task LateOldActorResponseCannotReplaceNewActor(){
   var a=new TaskCompletionSource<OperatorInformationSnapshot>();var b=new TaskCompletionSource<OperatorInformationSnapshot>();var ta=Target();var tb=Target("op_eve");
   using(var c=new OperatorInformationController((x,ct)=>x.Equals(ta)?a.Task:b.Task)){c.Bind(_=>{});var one=c.SelectAsync(ta);var two=c.SelectAsync(tb);b.SetResult(Snapshot(tb));await two;a.SetResult(Snapshot(ta));await one;Assert.That(c.Frame.Target,Is.SameAs(tb));Assert.That(c.Frame.Snapshot.Target,Is.SameAs(tb));}
  }
  [Test] public async Task ReplacementClosesDetailButSameInstanceRevisionRefreshKeepsIt(){
   long rev=1;string id="one";using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(Snapshot(x,rev,id)))){c.Bind(_=>{});await c.SelectAsync(Target());c.OpenDetail(0);rev=2;await c.RefreshAsync();Assert.That(c.Frame.DetailSlot,Is.EqualTo(0));Assert.That(c.Frame.Snapshot.Slot(0).Revision,Is.EqualTo(2));id="two";rev=3;await c.RefreshAsync();Assert.That(c.Frame.DetailSlot,Is.EqualTo(-1));Assert.That(c.Frame.Visible,Is.True);}
  }
  [Test] public async Task UnknownAndReadFailureAreNotEmpty(){
   bool fail=false;using(var c=new OperatorInformationController((x,ct)=>fail?Task.FromException<OperatorInformationSnapshot>(new IOException("source unavailable")):Task.FromResult(new OperatorInformationSnapshot(x,1,null,null,"")))){c.Bind(_=>{});await c.SelectAsync(Target());Assert.That(c.Frame.Snapshot.EquipmentKnown,Is.False);c.OpenDetail(0);Assert.That(c.Frame.DetailSlot,Is.EqualTo(-1));fail=true;await c.RefreshAsync();Assert.That(c.Frame.Status,Is.EqualTo("FAILED"));Assert.That(c.Frame.Error,Is.TypeOf<IOException>());Assert.That(c.Frame.Snapshot,Is.Null);}
  }
  [Test] public async Task UnbindCancelsAndRejectsLateCompletionWithoutCallingOldView(){
   var pending=new TaskCompletionSource<OperatorInformationSnapshot>();int calls=0;System.Threading.CancellationToken token=default;
   using(var c=new OperatorInformationController((x,ct)=>{token=ct;return pending.Task;})){c.Bind(_=>calls++);var work=c.SelectAsync(Target());c.Unbind();int closed=calls;Assert.That(token.IsCancellationRequested,Is.True);pending.SetResult(Snapshot(Target()));await work;Assert.That(calls,Is.EqualTo(closed));Assert.That(c.Frame.Visible,Is.False);}
  }
  [Test] public async Task RebindAndNewIncarnationInvalidateOldSelection(){
   int old=0,next=0;using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(Snapshot(x)))){c.Bind(_=>old++);await c.SelectAsync(Target());c.OpenDetail(0);c.Bind(_=>next++);int closed=old;await c.SelectAsync(Target(generation:2));Assert.That(old,Is.EqualTo(closed));Assert.That(next,Is.EqualTo(2));Assert.That(c.Frame.DetailSlot,Is.EqualTo(-1));Assert.That(c.Frame.Target.Generation,Is.EqualTo(2));c.Clear();Assert.That(c.Frame.Visible,Is.False);}
  }
  [Test] public async Task MismatchedTargetAndRevisionRegressionFailClosed(){
   bool bad=false;using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(Snapshot(x,bad?1:2)))){c.Bind(_=>{});await c.SelectAsync(Target());bad=true;await c.RefreshAsync();Assert.That(c.Frame.Status,Is.EqualTo("FAILED"));}
   using(var c=new OperatorInformationController((x,ct)=>Task.FromResult(Snapshot(Target("op_eve"))))){c.Bind(_=>{});await c.SelectAsync(Target());Assert.That(c.Frame.Status,Is.EqualTo("FAILED"));}
  }
  [Test] public void SnapshotOwnsSlotsAndRejectsDuplicateInstances(){
   var slots=new[]{Item(),null,null};var s=new OperatorInformationSnapshot(Target(),1,slots,null,"");slots[0]=null;Assert.That(s.Slot(0),Is.Not.Null);
   Assert.Throws<ArgumentException>(()=>new OperatorInformationSnapshot(Target(),1,new[]{Item(),Item(),null},null,""));
  }
  [Test] public void ActualMiraPinnedSourceIsReadOnlyAndDoesNotInventMissingDisplayData(){
   var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/en17-formal-local-trial-packet-20261015.json"));
   var context=BMiraEquippedTrialContext.Read(p,p,(string)p["startHash"],"a",(string)p["runId"],(JObject)p["source"]["originalComponent"]["originalMira"],(string)p["originalMiraHash"],(string)p["originalRunSnapshotHash"]);
   var before=context.Capture().ToString();var target=new OperatorInformationTarget(context.RunId,"mira","op_mira",1);var info=OperatorInformationMiraSource.Capture(context,target);
   Assert.That(info.Slot(0).InstanceId,Is.EqualTo((string)p["source"]["slots"][0]["instanceId"]));Assert.That(info.Slot(0).Description,Does.Contain("15%"));Assert.That(info.Slot(0).Name,Is.Null);Assert.That(info.Slot(0).IconKey,Is.Null);Assert.That(context.Capture().ToString(),Is.EqualTo(before));Assert.That(info.SourceNotice,Does.Contain("LOCAL"));
   Assert.Throws<ArgumentException>(()=>OperatorInformationMiraSource.Capture(context,Target()));
  }
  [Test] public void HasReadOnlySelectionController() {
   Assert.That(typeof(CombatSceneDriver).Assembly.GetType("LastRail.Presentation.Battle.OperatorInformationController"), Is.Not.Null,
    "Approved actor information requires a presentation owner independent of combat commands.");
  }
 }
}
#endif
