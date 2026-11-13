using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using LastRail.Tests.EditMode;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.EventSystems;

public sealed class OrdinaryPlayerEntryLifetimeTests {
 sealed class Journal:IRunOperationJournal {
  public int Reads;
  public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account){Reads++;return Task.FromResult<IReadOnlyList<PendingRunOperation>>(Array.Empty<PendingRunOperation>());}
  public Task<bool> PutAsync(PendingRunOperation operation)=>throw new AssertionException("Prepared node must not create another run request.");
  public Task<bool> ConfirmAsync(PendingRunOperation operation,string response)=>throw new AssertionException("Prepared node must not confirm another request.");
  public Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key)=>throw new AssertionException("Prepared node must not remove recovery.");
 }
 static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
 [TestCase(true,false)][TestCase(false,false)][TestCase(false,true)] public async Task PreparedDriverLifetimeFollowsCurrentPresentation(bool stale,bool overlap){
  var journal=new Journal();
  var playerType=typeof(PlayerEntryRuntimeTests).GetNestedType("Fixture",BindingFlags.NonPublic);
  using var player=(IDisposable)Activator.CreateInstance(playerType,new object[]{journal});
  var runtime=(PlayerEntryRuntime)Field(player,"Runtime");var wire=Field(player,"Wire");
  wire.GetType().GetField("ProfileAccount").SetValue(wire,"a");
  wire.GetType().GetField("Named").SetValue(wire,true);
  runtime.LobbyPreparation=_=>Task.FromResult(true);
  await runtime.Owner.LoginAsync(()=>Task.FromResult(SupportInquiryTests.Session("a")));
  Assert.IsTrue(runtime.Owner.Ready,"The authenticated fixture must finish profile preparation.");
  runtime.AccountScreens.SynchronizeSession();
  var nodeType=typeof(OrdinaryNodePreparationOwnerTests).GetNestedType("Fixture",BindingFlags.NonPublic);
  using var node=(IDisposable)Activator.CreateInstance(nodeType,new object[]{false,null});
  var owner=(OrdinaryNodePreparationOwner)Field(node,"Owner");var input=(OrdinaryFiveStartInput)Field(node,"Start");var driver=await owner.PrepareAsync();
  var raw=FirstPlayableScalingTests.Fixture(staticSource:true).Start;
  Assert.AreEqual(input.StartSnapshotHash,OrdinaryFiveStartInput.Read(raw,input.AccountId,input.RunId).StartSnapshotHash);
  var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=input.RunId,["runStatus"]="ACTIVE",["startResponse"]=raw,["errorCode"]=null};
  typeof(AccountScreenRuntime).GetProperty("Pending").SetValue(runtime.AccountScreens,new PendingAccountAction("a","departure-start",(string)raw["operationPreviewId"],0,"entry-test",new JObject{["operationPreviewId"]=raw["operationPreviewId"]}.ToString(Newtonsoft.Json.Formatting.None),response.ToString(Newtonsoft.Json.Formatting.None)));
  typeof(AccountScreenRuntime).GetField("ordinaryNodePreparation",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(runtime.AccountScreens,owner);
  Assert.AreEqual(input.AccountId,runtime.Owner.Session.AccountId);
  Assert.AreEqual(LobbyRunSelectionKind.Recovery,runtime.AccountScreens.RunSelection.Kind);
  Assert.AreEqual(input.RunId,runtime.AccountScreens.RunSelection.RunId);
  int reads=journal.Reads;int checks=0;Func<bool> current=()=>!stale||++checks==1;
  var disabledSystems=new List<EventSystem>();
  while(EventSystem.current!=null){var system=EventSystem.current;disabledSystems.Add(system);system.enabled=false;}
  try{
   var show=typeof(PlayerEntryRuntime).GetMethod("PresentPreparedOrdinaryCombatAsync",BindingFlags.Instance|BindingFlags.NonPublic);
   if(overlap){
     var pending=new TaskCompletionSource<CombatSceneDriver>();
     typeof(OrdinaryNodePreparationOwner).GetField("preparation",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,pending.Task);
     var first=(Task)show.Invoke(runtime,new object[]{input.RunId,(Func<bool>)(()=>true)});
     int secondChecks=0;bool? retiredBeforeCurrentCancellation=null;
     bool SecondCurrent(){if(++secondChecks==1)return true;retiredBeforeCurrentCancellation=driver.IsRetired;return false;}
     var second=(Task)show.Invoke(runtime,new object[]{input.RunId,(Func<bool>)SecondCurrent});
     Assert.IsFalse(first.IsCompleted);Assert.IsFalse(second.IsCompleted);
     pending.SetResult(driver);
     var staleError=Assert.ThrowsAsync<InvalidOperationException>(async()=>await first);
     Assert.AreEqual("Ordinary combat owner changed during preparation.",staleError.Message);
     await second;
     Assert.AreEqual(false,retiredBeforeCurrentCancellation,"An older presentation must not retire the current presentation's shared driver.");
     Assert.IsTrue(driver.IsRetired,"The current presentation must retire its driver when it cancels itself.");
     Assert.AreEqual(reads,journal.Reads);
    return;
   }
   var presentation=(Task)show.Invoke(runtime,new object[]{input.RunId,current});
   if(stale)await presentation;else{
    var error=Assert.ThrowsAsync<InvalidOperationException>(async()=>await presentation);
    Assert.AreEqual("The existing input system is unavailable.",error.Message);
   }
   Assert.IsTrue(driver.IsRetired,"The unpublished driver must lose eligibility on presentation failure.");
   Assert.AreEqual(reads,journal.Reads,"Valid preparation must be reused before journal recovery.");
  }finally{foreach(var system in disabledSystems)if(system!=null)system.enabled=true;}
 }
}
