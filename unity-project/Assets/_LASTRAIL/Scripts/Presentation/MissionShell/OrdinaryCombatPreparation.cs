using System;
using System.Threading.Tasks;
using System.Linq;
using LastRail.Battle;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Auth;

namespace LastRail.Presentation.MissionShell {
 public enum OrdinaryPreparationFailure { SourceUnavailable, OutcomeUnknown, NodeResultAvailable }
 public sealed class OrdinaryNodeStartUnavailableException : InvalidOperationException {
  public bool RunResultAvailable{get;}
  public OrdinaryPreparationFailure Failure{get;}
  public OrdinaryNodeStartUnavailableException(string message="이 전투 노드의 시작 상태가 아직 제공되지 않았습니다. 중단 기록은 보존했으며, 이전 상태로 전투를 재개하지 않았습니다.",bool runResultAvailable=false,OrdinaryPreparationFailure failure=OrdinaryPreparationFailure.SourceUnavailable):base(message){RunResultAvailable=runResultAvailable;Failure=failure;}
 }
 public sealed partial class AccountScreenRuntime {
  OrdinaryNodePreparationOwner ordinaryNodePreparation;
  Task<CombatSceneDriver> ordinaryPreparation;
  Func<bool> ordinaryPreparationCurrent;
  CombatCheckpointStore ordinaryPreparationStore;
  PendingAccountAction ordinaryPreparationSource;
  string ordinaryPreparationBuild;
  long ordinaryPreparationStep,ordinaryPreparationGeneration;
  void ResetOrdinaryPreparation(){
   ordinaryNodePreparation?.CancelPreparation();
   ordinaryPreparationGeneration++;ordinaryPreparation=null;ordinaryPreparationCurrent=null;
   ordinaryPreparationStore=null;ordinaryPreparationSource=null;ordinaryPreparationBuild=null;
  }
  public void ReleaseOrdinaryCombatPreparation(){ResetOrdinaryPreparation();}
  public void CancelPreparedOrdinaryCombat(CombatSceneDriver expected){if(ordinaryNodePreparation?.Owns(expected)==true)ResetOrdinaryPreparation();}
  public bool HasUnconfirmedOrdinaryResult(string runId)=>runRecovery?.Operations.Any(o=>o.TargetId==runId&&o.AcceptedResponseJson==null&&(o.Kind==PendingRunKind.CompleteOrdinaryNode||o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim))==true;
  public async Task<CombatSceneDriver> PrepareOrdinaryNodeCombatAsync(string runId,long? stepTimeSpanTicks,
   Func<OrdinaryNodeStart,Func<OrdinaryNodeRestartScope>,OrdinaryNodeRestartDelivery> restartDelivery){
   SynchronizeSession();var input=ReadOrdinaryCombatInput(runId);var retained=Pending;var client=runRecovery;var sessionOwner=owner;var stamp=ownerGeneration;long epoch=ordinaryPreparationGeneration;
   if(restartDelivery==null||DepartureReceiptNeedsSave||retained?.Kind!="departure-start"||retained.Account!=input.AccountId||retained.StartResultJson==null||client==null)
    throw new InvalidOperationException("Durably retained departure and restart delivery required.");
   if(!client.Operations.Any(o=>o.TargetId==runId&&(o.Kind==PendingRunKind.CompleteOrdinaryNode||o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim))){
    var reused=ordinaryNodePreparation?.ReusePrepared(input,stepTimeSpanTicks);if(reused!=null)return await reused;
   }
   bool CurrentNodePreparation()=>epoch==ordinaryPreparationGeneration&&Current(sessionOwner,stamp)&&ReferenceEquals(client,runRecovery)&&RunSelection.Kind==LobbyRunSelectionKind.Recovery&&RunSelection.RunId==runId;
   await RecoverOrdinaryResultsBeforeEntryAsync(runId,client,CurrentNodePreparation);
   // An accepted live attempt already owns its newer metadata. Re-entry must not
   // replace it with an older cached map read or issue a confirmation GET.
   var arrival=ordinaryNodePreparation!=null&&ordinaryTravel?.MapArrival!=null?ordinaryTravel.MapArrival:await PrepareOrdinaryTravelAsync(runId);
   if(!CurrentNodePreparation())throw new InvalidOperationException("Node owner changed during preparation.");
   if(arrival?.NodeStart==null||arrival.Scaling==null)throw new OrdinaryNodeStartUnavailableException();
   long originalStep=arrival.NodeStart.ReadActions(input).StepTimeSpanTicks;
   if(stepTimeSpanTicks.HasValue&&stepTimeSpanTicks.Value!=originalStep)throw new InvalidOperationException("Requested logical step differs from the original node.");
   if(ordinaryNodePreparation!=null&&!ordinaryNodePreparation.Matches(input,arrival,originalStep)){ordinaryNodePreparation.Dispose();ordinaryNodePreparation=null;}
   // Owner lifetime spans local re-entry; each attempt separately captures its retirement epoch.
   bool SameOwner()=>!disposed&&Current(sessionOwner,stamp)&&ReferenceEquals(client,runRecovery)&&RunSelection.Kind==LobbyRunSelectionKind.Recovery&&RunSelection.RunId==runId&&ordinaryTravel?.MapArrival?.NodeStart?.NodeId==arrival.NodeStart.NodeId&&ordinaryTravel.MapArrival.NodeStart.SnapshotHash==arrival.NodeStart.SnapshotHash;
   if(ordinaryNodePreparation==null){
    if(ordinaryTravel.MapArrivalRecovered){
     var map=await ReadOrdinaryMapAsync(runId);
     if(!CurrentNodePreparation())throw new InvalidOperationException("Node owner changed during current attempt read.");
     // Another caller may have published an owner while this shared read was
     // pending. Keep its accepted attempt and share its in-flight preparation.
     if(ordinaryNodePreparation!=null){
      if(!ordinaryNodePreparation.Matches(input,arrival,originalStep))throw new InvalidOperationException("Node preparation was replaced during current attempt read.");
     }else{
      if(map.RunStatus!="ACTIVE"||map.Phase!="ACTIVE"||map.BlockedByPendingTransactions||map.CurrentAttempt==null)
       throw new OrdinaryNodeStartUnavailableException("현재 전투 시도를 확인하지 못했습니다. 작전과 원래 요청은 보존했습니다.",map.RunStatus!="ACTIVE");
      var attempt=OrdinaryNodeAttempt.FromCurrent(arrival.NodeStart,map.CurrentAttempt);
      ordinaryNodePreparation=new OrdinaryNodePreparationOwner(input,arrival,originalStep,true,SameOwner,()=>owner.AccessToken,restartDelivery,attempt);
     }
    }else ordinaryNodePreparation=new OrdinaryNodePreparationOwner(input,arrival,originalStep,false,SameOwner,()=>owner.AccessToken,restartDelivery);
   }
   var prepared=await ordinaryNodePreparation.PrepareAsync();
   if(!CurrentNodePreparation())throw new InvalidOperationException("Node owner changed before publication.");
   return prepared;
  }
  async Task RecoverOrdinaryResultsBeforeEntryAsync(string runId,DurableRunClient client,Func<bool> current){
   await client.RestoreAsync();if(!current())throw new InvalidOperationException("Combat owner changed during recovery.");
   var pending=client.Operations.Where(o=>o.TargetId==runId&&(o.Kind==PendingRunKind.CompleteOrdinaryNode||o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim)).ToArray();
   foreach(var operation in pending.Where(o=>o.AcceptedResponseJson==null)){
    if(!current())throw new InvalidOperationException("Combat owner changed during recovery.");
    var recovered=await client.RetryAsync(operation);
    if(!current())throw new InvalidOperationException("Combat owner changed during recovery.");
    if(recovered?.AcceptedResponseJson==null)throw new OrdinaryNodeStartUnavailableException("요청 결과를 확인하지 못했습니다. 원래 요청을 보존했으며 전투를 재시작하지 않았습니다.",false,OrdinaryPreparationFailure.OutcomeUnknown);
   }
   if(pending.Length>0)throw new OrdinaryNodeStartUnavailableException("이전 완료·보상 기록이 확인됐습니다. 해당 결과를 먼저 확인해야 하며 전투를 재시작하지 않았습니다.",pending.Any(o=>o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim),OrdinaryPreparationFailure.NodeResultAvailable);
  }
  // Retained component consumers keep the historical Sera execution identity.
  public Task<CombatSceneDriver> PrepareOrdinaryCombatAsync(string runId,CombatCheckpointStore store,AppBuildSnapshot build,long stepTimeSpanTicks)
   =>PrepareOrdinaryOwnerAsync(runId,store,build,stepTimeSpanTicks,false);
  public Task<CombatSceneDriver> PrepareOrdinaryFiveCombatAsync(string runId,CombatCheckpointStore store,AppBuildSnapshot build,long stepTimeSpanTicks)
   =>PrepareOrdinaryOwnerAsync(runId,store,build,stepTimeSpanTicks,true);
  Task<CombatSceneDriver> PrepareOrdinaryOwnerAsync(string runId,CombatCheckpointStore store,AppBuildSnapshot build,long stepTimeSpanTicks,bool fullFive){
   SynchronizeSession();
   if(store==null||build?.AppVersion==null||stepTimeSpanTicks<=0||stepTimeSpanTicks%10000!=0)throw new ArgumentException("Explicit checkpoint, build and logical step required.");
   var input=ReadOrdinaryCombatInput(runId);var original=Pending;
   if(DepartureReceiptNeedsSave||original?.Kind!="departure-start"||original.Account!=input.AccountId||original.StartResultJson==null||runRecovery==null)
    throw new InvalidOperationException("Durably retained departure request required.");
   string buildPin=build.AppVersion+"/"+build.AppVersionCode+"/"+(fullFive?OrdinaryFiveCombatFactory.Version:OrdinaryFiveSeraFactory.Version);
   if(ordinaryPreparationCurrent!=null&&!ordinaryPreparationCurrent())ResetOrdinaryPreparation();
   if(ordinaryPreparation!=null&&!ordinaryPreparation.IsFaulted&&!ordinaryPreparation.IsCanceled){
    if(!ReferenceEquals(store,ordinaryPreparationStore)||original.StartResultJson!=ordinaryPreparationSource.StartResultJson||original.Key!=ordinaryPreparationSource.Key||original.Payload!=ordinaryPreparationSource.Payload||stepTimeSpanTicks!=ordinaryPreparationStep||buildPin!=ordinaryPreparationBuild)
     throw new InvalidOperationException("Prepared combat source or storage changed.");
    if(ordinaryPreparation.IsCompleted&&ordinaryPreparation.Result.RecoveryRequired)ordinaryPreparation.Result.Open();
    return ordinaryPreparation;
   }
   var sessionOwner=owner;var stamp=ownerGeneration;var client=runRecovery;long epoch=++ordinaryPreparationGeneration;
   bool CurrentOwner()=>epoch==ordinaryPreparationGeneration&&Current(sessionOwner,stamp)&&ReferenceEquals(client,runRecovery);
   bool CurrentPreparation(){if(!CurrentOwner())return false;var selection=RunSelection;return selection.Kind==LobbyRunSelectionKind.Recovery&&selection.RunId==runId;}
   ordinaryPreparationCurrent=CurrentPreparation;ordinaryPreparationStore=store;
   ordinaryPreparationSource=original;ordinaryPreparationBuild=buildPin;ordinaryPreparationStep=stepTimeSpanTicks;
   // Publish the pending operation before initialization notifies display subscribers.
   var completion=new TaskCompletionSource<CombatSceneDriver>();ordinaryPreparation=completion.Task;
   _=Complete();return completion.Task;
   async Task Complete(){try{completion.SetResult(await Prepare());}catch(Exception error){completion.SetException(error);}}
   async Task<CombatSceneDriver> Prepare(){
    if(fullFive)await RecoverOrdinaryResultsBeforeEntryAsync(runId,client,CurrentOwner);
    var initialization=await InitializeOrdinaryCombatAsync(runId);
    if(initialization?.AcceptedResponseJson==null||!CurrentPreparation())throw new InvalidOperationException("Original initialization is not confirmed for this owner.");
    CombatSceneDriver driver;
    if(fullFive){
     var factory=OrdinaryFiveCombatFactory.FromInitialization(input,initialization.AcceptedResponseJson,stepTimeSpanTicks);
     var checkpoint=new OrdinaryFiveCombatCheckpoint(store,original,factory,build,initialization);
     // shortcut: old checkpoints are not node-start snapshots; replace this gate when the server entry producer supplies the original snapshot and generation.
     if(checkpoint.Restore()!=null)throw new OrdinaryNodeStartUnavailableException();
     driver=await checkpoint.OpenAfterFirstCommitAsync(client,CurrentPreparation);
    }else{
     var factory=OrdinaryFiveSeraFactory.FromInitialization(input,initialization.AcceptedResponseJson,stepTimeSpanTicks);
     var checkpoint=new OrdinaryFiveSeraCheckpoint(store,original,factory,build,initialization);
     driver=await checkpoint.OpenAfterFirstCommitAsync(client,CurrentPreparation);
    }
    if(!CurrentPreparation())throw new InvalidOperationException("Combat owner changed before publication.");
    return driver;
   }
  }
 }
}
