using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell {
 // Owns one node's local attempt lifetime; the delivery retains uncertain server requests separately.
 public sealed class OrdinaryNodePreparationOwner : IDisposable {
  readonly OrdinaryFiveStartInput original;readonly OrdinaryNodeStart node;readonly FirstPlayableScaling scaling;readonly long step;readonly string localGeometry;
  readonly Func<bool> current;readonly Func<GameApiAccessToken> token;
  readonly Func<OrdinaryNodeStart,Func<OrdinaryNodeRestartScope>,OrdinaryNodeRestartDelivery> createDelivery;
  OrdinaryNodeAttempt attempt;OrdinaryNodeRestartDelivery delivery;OrdinaryNodeRestartResult acceptedRestart;OrdinaryNodeRestart pendingRestart;
  CombatSceneDriver driver;Task<CombatSceneDriver> preparation;Task confirmation;long epoch;bool restart,disposed;
  bool useCurrentProjection;
  readonly Newtonsoft.Json.Linq.JObject boardingSource,augmentOwnership;
  public OrdinaryNodeAttempt CurrentAttempt=>attempt;
  public OrdinaryNodePreparationOwner(OrdinaryFiveStartInput original,BRouteCommandResult arrival,long step,bool recovered,
   Func<bool> current,Func<GameApiAccessToken> token,Func<OrdinaryNodeStart,Func<OrdinaryNodeRestartScope>,OrdinaryNodeRestartDelivery> createDelivery){
   this.original=original??throw new ArgumentNullException(nameof(original));if(arrival==null)throw new ArgumentNullException(nameof(arrival));node=arrival.NodeStart;scaling=arrival.Scaling;
   if(!arrival.Committed||node==null||scaling==null)throw new OrdinaryNodeStartUnavailableException();
   node.RequireOriginal(original);this.step=step;this.current=current??throw new ArgumentNullException(nameof(current));
   this.token=token??throw new ArgumentNullException(nameof(token));this.createDelivery=createDelivery??throw new ArgumentNullException(nameof(createDelivery));
   attempt=OrdinaryNodeAttempt.FromEntry(node);restart=recovered;
   _=OrdinaryFiveCombatFactory.FromNodeStart(original,node,scaling,step,attempt);
  }
  public OrdinaryNodePreparationOwner(OrdinaryFiveStartInput original,BRouteCommandResult arrival,long step,bool recovered,
   Func<bool> current,Func<GameApiAccessToken> token,Func<OrdinaryNodeStart,Func<OrdinaryNodeRestartScope>,OrdinaryNodeRestartDelivery> createDelivery,OrdinaryNodeAttempt currentAttempt)
   :this(original,arrival,step,recovered,current,token,createDelivery){
   if(currentAttempt==null)throw new ArgumentNullException(nameof(currentAttempt));currentAttempt.RequireNode(node);
   attempt=currentAttempt;useCurrentProjection=true;
  }
  internal OrdinaryNodePreparationOwner(OrdinaryLocalExecutionReplay.Input source,Func<bool> current){
   if(source==null)throw new ArgumentNullException(nameof(source));
   original=source.Original;node=source.Node;scaling=source.Scaling;attempt=source.Attempt;
   step=node.ReadActions(original).StepTimeSpanTicks;localGeometry=source.GeometryJson;boardingSource=source.BoardingSource;augmentOwnership=source.AugmentOwnership;
   this.current=current??throw new ArgumentNullException(nameof(current));
  }
  public bool Matches(OrdinaryFiveStartInput input,BRouteCommandResult source,long logicalStep)=>input.AccountId==original.AccountId&&input.RunId==original.RunId&&input.StartSnapshotHash==original.StartSnapshotHash&&source.NodeStart?.NodeId==node.NodeId&&source.NodeStart.SnapshotHash==node.SnapshotHash&&step==logicalStep;
  public Task<CombatSceneDriver> ReusePrepared(OrdinaryFiveStartInput input,long? requestedStep){
   if(!Current()||input.AccountId!=original.AccountId||input.RunId!=original.RunId||input.StartSnapshotHash!=original.StartSnapshotHash||requestedStep.HasValue&&requestedStep.Value!=step||driver?.IsRetired==true||driver?.RecoveryRequired==true||preparation==null||preparation.IsFaulted||preparation.IsCanceled)return null;
   return preparation;
  }
  bool Current()=>!disposed&&current();
  OrdinaryNodeRestartScope Scope()=>Current()?new OrdinaryNodeRestartScope(original.AccountId,original.RunId,attempt.NodeId,attempt.Revision,attempt.Generation):null;
  public Task<CombatSceneDriver> PrepareAsync(){
   if(!Current())throw new InvalidOperationException("Node preparation owner changed.");
   if(driver?.IsRetired==true||driver?.RecoveryRequired==true)CancelPreparation();
   if(preparation!=null&&!preparation.IsFaulted&&!preparation.IsCanceled)return preparation;
   long generation=++epoch;var completion=new TaskCompletionSource<CombatSceneDriver>();preparation=completion.Task;_=Complete();return completion.Task;
   async Task Complete(){try{completion.SetResult(await Prepare());}catch(Exception error){completion.SetException(error);}}
   async Task<CombatSceneDriver> Prepare(){
    if(acceptedRestart?.ResultAvailable==true)throw ResultAvailable();
    if(restart)await ConfirmRestart();
    if(!Current()||epoch!=generation)throw new InvalidOperationException("Node preparation ended before publication.");
    restart=false;
    var factory=OrdinaryFiveCombatFactory.FromNodeStart(original,node,scaling,step,attempt);
    if(localGeometry!=null){factory=factory.WithLiveSources(localGeometry);if(boardingSource!=null)factory=factory.WithBoardingSource(boardingSource,node.BoardingSourceHash);}
    else if(original.GeometryReference?.DefinitionId=="ordinary-live-obstacle-and-five-v2"){
     using var geometry=await LastRail.Resources.PreparedContent.PrepareAsync(new[]{"CombatPresentation/OrdinaryLiveGeometryV02"});
     if(!Current()||epoch!=generation)throw new InvalidOperationException("Node preparation ended before geometry publication.");
     var measured=LastRail.Resources.PreparedContent.Load<UnityEngine.TextAsset>("CombatPresentation/OrdinaryLiveGeometryV02");
     if(measured==null)throw new OrdinaryNodeStartUnavailableException("현재 전투의 판정 공간 자료가 없습니다.");
     factory=factory.WithLiveSources(measured.text);
    }
    if(augmentOwnership!=null)factory=factory.WithAugmentOwnership(augmentOwnership);
    var prepared=CombatSceneDriver.ForOrdinaryNode(factory,()=>Current()&&epoch==generation);
    prepared.Open();driver=prepared;restart=false;return prepared;
   }
  }
  Task ConfirmRestart(){
   if(localGeometry!=null)throw new InvalidOperationException("A new server-confirmed local attempt is required for reentry.");
   if(confirmation!=null&&!confirmation.IsFaulted&&!confirmation.IsCanceled)return confirmation;
   var completion=new TaskCompletionSource<bool>();confirmation=completion.Task;_=Complete();return completion.Task;
   async Task Complete(){try{await Confirm();completion.SetResult(true);}catch(Exception error){completion.SetException(error);}}
   bool PreparationCurrent()=>Current()&&preparation!=null;
   async Task Confirm(){
     delivery=delivery??createDelivery(node,Scope);
     OrdinaryNodeRestartResult result;
     if(useCurrentProjection&&acceptedRestart==null)result=await delivery.RestartFromCurrentAsync(attempt,token(),PreparationCurrent);
     else if(pendingRestart!=null)result=await delivery.DeliverAsync(pendingRestart,token(),PreparationCurrent);
     else if(acceptedRestart!=null){
      if(acceptedRestart.ResultAvailable)throw ResultAvailable();
      await delivery.InvalidateAsync();
      if(!Current()||preparation==null)throw new InvalidOperationException("Node preparation ended before restart.");
      pendingRestart=new OrdinaryNodeRestart(node,Guid.NewGuid().ToString("N"),attempt.Revision,attempt.Generation);
      result=await delivery.DeliverAsync(pendingRestart,token(),PreparationCurrent);
     }else{
      result=await delivery.RecoverAsync(token(),PreparationCurrent);
      // shortcut: a retained accepted restart is not a current-attempt projection; require that server source before cross-process reuse.
      if(result!=null&&delivery.LastTransportResult==null)throw new OrdinaryNodeStartUnavailableException("현재 전투 시도를 확인하지 못했습니다. 원래 재입장 기록은 보존했습니다.");
      if(result==null&&delivery.LastTransportResult==null){
       if(!Current()||preparation==null)throw new InvalidOperationException("Node preparation ended before restart.");
       pendingRestart=new OrdinaryNodeRestart(node,Guid.NewGuid().ToString("N"),attempt.Revision,attempt.Generation);
       result=await delivery.DeliverAsync(pendingRestart,token(),PreparationCurrent);
      }
     }
     if(result==null)throw new OrdinaryNodeStartUnavailableException("재입장 요청 결과를 확인하지 못했습니다. 원래 요청을 보존했습니다.",false,OrdinaryPreparationFailure.OutcomeUnknown);
     acceptedRestart=result;pendingRestart=null;
     if(result.ResultAvailable)throw ResultAvailable();
     attempt=OrdinaryNodeAttempt.FromRestart(result);restart=false;
   }
  }
  public bool Owns(CombatSceneDriver expected)=>expected!=null&&ReferenceEquals(driver,expected);
  public void CancelPreparation(){
   epoch++;if(driver?.HasBegunExecution==true){restart=true;confirmation=null;}
   driver?.Retire();driver=null;preparation=null;
  }
  OrdinaryNodeStartUnavailableException ResultAvailable()=>new OrdinaryNodeStartUnavailableException("이전 전투 결과를 먼저 확인해 주세요.",(string)acceptedRestart?.Capture()["runStatus"]!="ACTIVE",OrdinaryPreparationFailure.NodeResultAvailable);
  public void Retire(){epoch++;driver?.Retire();driver=null;preparation=null;restart=true;if(confirmation?.IsCompleted==true)confirmation=null;}
  public void Dispose(){if(disposed)return;Retire();disposed=true;}
 }
}
