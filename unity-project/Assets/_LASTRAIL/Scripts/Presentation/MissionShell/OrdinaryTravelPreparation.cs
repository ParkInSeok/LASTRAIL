using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell {
 // Owns confirmed travel state, while the existing run journal owns every original command.
 public sealed class OrdinaryTravelOwner : IDisposable {
  readonly DurableRunClient client;
  readonly Func<bool> current;
  readonly string account,run;
  string destinationNode;
  Task<BTravelReceipt> active;
  Task<BRouteCommandResult> mapActive;
  bool disposed,restored;
  public BTravelReceipt Latest {get;private set;}
  public BOperationDestinationReceipt DestinationReceipt {get;private set;}
  public bool PauseRequested {get;private set;}
  public BRouteCommandResult MapArrival {get;private set;}
  public bool MapArrivalRecovered {get;private set;}
  public BRouteBinding MapArrivalRoute {get;private set;}
  public bool Busy=>active!=null||mapActive!=null;
  public bool NeedsRecovery=>HasPending||!restored&&(Latest!=null||DestinationReceipt!=null);
  public OrdinaryTravelOwner(DurableRunClient client,string account,string run,Func<bool> current){
   this.client=client??throw new ArgumentNullException(nameof(client));this.account=account??throw new ArgumentNullException(nameof(account));
   this.run=run??throw new ArgumentNullException(nameof(run));this.current=current??throw new ArgumentNullException(nameof(current));
  }
  bool Current()=>!disposed&&current();
  void RequireCurrent(){if(!Current())throw new InvalidOperationException("Travel owner changed.");}
  public bool NeedsMapArrivalRoute {
   get{
    RequireCurrent();if(Latest?.ArrivalEvidenceRef==null||MapArrival!=null||MapArrivalRoute!=null)return false;
    var saved=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.MapArrival);
    return saved==null||!MatchesArrival(saved.ReadMapArrivalCommand());
   }
  }
  bool MatchesArrival(BRouteCommand command)=>Latest?.ArrivalEvidenceRef!=null&&command.Binding.MapRevision==Latest.MapRevision&&
   (string)command.Capture()["command"]["evidenceRef"]==Latest.ArrivalEvidenceRef;
  void RequireArrivalRoute(BRouteBinding route){
   RequireCurrent();if(route==null||Latest?.ArrivalEvidenceRef==null)throw new InvalidOperationException("Confirmed arrival evidence and original route are required.");
   Latest.RequireScope(account,run);var r=route.Capture();var o=Latest.Capture()["arrival"]["observation"];var destination=o["destination"];
   if(route.AccountId!=account||route.RunId!=run||route.MapRevision!=Latest.MapRevision||(string)r["runStatus"]!="ACTIVE"||(string)r["phase"]!="TRAVELLING"||
    (string)r["pendingNode"]?["nodeId"]!=Latest.NodeId||(string)r["pendingEdgeId"]!=(string)destination["edgeId"]||
    (string)destination["eventId"]!=Latest.DestinationEventId||(long)destination["mapRevision"]!=route.MapRevision||
    !JToken.DeepEquals(r["nativeContextId"],o["nativeContextId"])||!JToken.DeepEquals(r["departurePinHash"],o["departurePinHash"])||!JToken.DeepEquals(r["bindingHash"],o["operationBindingHash"]))
    throw new InvalidOperationException("Arrival evidence differs from the original route.");
  }
  public Task<BRouteCommandResult> CommitMapArrivalAsync(BRouteBinding route){
   RequireCurrent();if(mapActive!=null)return mapActive;if(active!=null)return Task.FromResult<BRouteCommandResult>(null);
   var completion=new TaskCompletionSource<BRouteCommandResult>();mapActive=completion.Task;_=Complete();return completion.Task;
   async Task Complete(){try{var result=await Commit();RequireCurrent();mapActive=null;completion.SetResult(result);}catch(Exception error){mapActive=null;completion.SetException(error);}}
   async Task<BRouteCommandResult> Commit(){
    await RestoreAsync();RequireCurrent();if(Latest?.ArrivalEvidenceRef==null||HasPending)return null;
    if(client.Operations.Any(o=>o.AccountId==account&&o.TargetId==run&&(o.Kind==PendingRunKind.CompleteOrdinaryNode||o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim)))return null;
    var saved=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.MapArrival);
    var command=saved?.ReadMapArrivalCommand();
    if(command!=null&&MatchesArrival(command)){
     RequireArrivalRoute(command.Binding);
     if(route!=null&&route.Hash!=command.Binding.Hash)throw new InvalidOperationException("Map arrival recovery requires the original route.");
     MapArrivalRoute=command.Binding;
     if(MapArrival!=null)return MapArrival;
    }else{
     if(saved!=null&&saved.AcceptedResponseJson==null)throw new InvalidOperationException("The previous map arrival must be recovered first.");
     route=route??MapArrivalRoute;if(route==null)return null;RequireArrivalRoute(route);MapArrivalRoute=route;
     command=BRouteCommand.FromOriginal(route,new JObject{["kind"]="ARRIVE",["eventId"]=Guid.NewGuid().ToString("N"),["expectedRevision"]=route.MapRevision,
      ["edgeId"]=route.Capture()["pendingEdgeId"],["evidenceRef"]=Latest.ArrivalEvidenceRef});
    }
    bool recovered=saved?.AcceptedResponseJson!=null;
    var accepted=await client.ArriveMapAsync(command);RequireCurrent();if(accepted?.AcceptedResponseJson==null)return null;
    var result=BRouteCommandResult.Read(200,accepted.AcceptedResponseJson,command);
    if(MapArrival!=null&&result.RequiredMapRevision<MapArrival.RequiredMapRevision)throw new InvalidOperationException("Map arrival revision regressed.");
    MapArrival=result;MapArrivalRecovered=recovered;return result;
   }
  }
  public Task<BTravelReceipt> ConfirmDestinationAsync(BOperationDestinationIntent intent,string nodeId){
   RequireCurrent();if(intent==null||intent.AccountId!=account||intent.RunId!=run||string.IsNullOrEmpty(nodeId))throw new ArgumentException("Original destination scope and node are required.");
   if(active!=null)return active;if(mapActive!=null)return Task.FromResult<BTravelReceipt>(null);
   if(DestinationReceipt!=null){
    var original=DestinationReceipt.Intent;
    if(original.CommandId==intent.CommandId&&!JToken.DeepEquals(original.Capture(),intent.Capture()))throw new InvalidOperationException("Destination intent changed.");
    if(Latest==null||intent.ExpectedRevision<=original.ExpectedRevision)return Task.FromResult(Latest);
   }
   return Exclusive(async()=>{
    await client.RestoreAsync();RequireCurrent();
    if(HasResultOperation)return null;
    var saved=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.ConfirmDestination);
    if(saved!=null&&(saved.AcceptedResponseJson==null||saved.ReadDestinationIntent().ExpectedRevision>=intent.ExpectedRevision))return await RestoreCore();
    if(saved!=null&&BOperationDestinationReceipt.Read(saved.AcceptedResponseJson,saved.ReadDestinationIntent()).TravelInitial==null)return await RestoreCore();
    if(HasPending)return null;
    var accepted=await client.ConfirmDestinationAsync(intent);RequireCurrent();
    if(accepted?.AcceptedResponseJson==null){restored=false;return null;}
    DestinationReceipt=BOperationDestinationReceipt.Read(accepted.AcceptedResponseJson,accepted.ReadDestinationIntent());destinationNode=nodeId;
    var initial=UseDestinationInitial(nodeId);restored=initial!=null;return initial;
   });
  }
  BTravelReceipt UseDestinationInitial(string expectedNode){
   RequireCurrent();var intent=DestinationReceipt.Intent;var revision=intent.ExpectedRevision+1;
   if(Latest!=null&&Latest.MapRevision>revision)return Latest;
   var initial=DestinationReceipt.TravelInitial;
   // Acceptance remains durable even when the travel projection is absent or invalid.
   Latest=null;MapArrival=null;MapArrivalRoute=null;restored=false;
   if(initial==null)return null;
   var receipt=BTravelReceipt.ReadInitial(initial.ToString(Formatting.None),intent.CommandId,expectedNode??destinationNode??(string)initial["nodeId"],revision);
   Apply(receipt);return Latest;
  }
  internal bool HasResultOperation=>client.Operations.Any(o=>o.AccountId==account&&o.TargetId==run&&(o.Kind==PendingRunKind.CompleteOrdinaryNode||o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim));
  public Task<BTravelReceipt> RestoreAsync(){
   RequireCurrent();if(active!=null)return active;if(restored)return Task.FromResult(Latest);
   return Exclusive(RestoreCore);
  }
  async Task<BTravelReceipt> RestoreCore(){
    await client.RestoreAsync();RequireCurrent();
    // The existing result recovery path has priority over travel.
    if(HasResultOperation)return null;
    var destination=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.ConfirmDestination);
    if(destination!=null){
     if(destination.AcceptedResponseJson==null){destination=await client.ConfirmDestinationAsync(destination.ReadDestinationIntent());RequireCurrent();}
     if(destination?.AcceptedResponseJson==null)return null;
     var receipt=BOperationDestinationReceipt.Read(destination.AcceptedResponseJson,destination.ReadDestinationIntent());
     if(DestinationReceipt?.Intent.CommandId!=receipt.Intent.CommandId)destinationNode=null;
     DestinationReceipt=receipt;
    }
    var saved=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.TravelCommand);
    var previous=saved==null?null:BTravelCommand.Read(saved.BodyJson);
    var previousRevision=saved?.AcceptedResponseJson==null?previous?.MapRevision:BTravelReceipt.Read(saved.AcceptedResponseJson,previous).MapRevision;
    if(destination!=null&&(saved==null||DestinationReceipt.Intent.ExpectedRevision+1>previousRevision)){
     if(saved!=null&&saved.AcceptedResponseJson==null)return null;
     if(UseDestinationInitial(null)==null)return null;
     var recovered=await Send("RECOVER");if(recovered==null)return null;
     restored=true;return Latest;
    }
    if(saved==null)return null;
    var command=BTravelCommand.Read(saved.BodyJson);
    if(DestinationReceipt!=null&&command.MapRevision==DestinationReceipt.Intent.ExpectedRevision+1&&command.DestinationEventId!=DestinationReceipt.Intent.CommandId)
     throw new InvalidOperationException("Saved travel differs from its destination receipt.");
    bool recoveredConflict=false;
    if(saved.AcceptedResponseJson==null){
     var original=saved;saved=await client.TravelAsync(run,command);RequireCurrent();
     if(saved==null&&client.LastTravelConflictJson!=null){
      var conflict=BTravelReceipt.ReadConflictCurrent(client.LastTravelConflictJson,command);
      if(conflict?.PauseIntent==true)PauseRequested=true;
      saved=await client.RecoverTravelConflictAsync(original,Guid.NewGuid().ToString("N"));RequireCurrent();
      if(saved!=null){command=BTravelCommand.Read(saved.BodyJson);recoveredConflict=true;}
     }
    }
    RequireCurrent();if(saved?.AcceptedResponseJson==null)return null;
    Apply(BTravelReceipt.Read(saved.AcceptedResponseJson,command));
    // A new owner/process discards unconfirmed elapsed time; opening this same owner again does not.
    if(!Latest.IsCurrentMap&&Latest.ArrivalEvidenceRef==null&&!recoveredConflict){
     var recovered=await Send("RECOVER");if(recovered==null)return null;
    }
    restored=true;return Latest;
  }
  public Task<BTravelReceipt> SetThrottleAsync(long bps){
   RequireCurrent();if(Busy||!CanCommand||PauseRequested||Latest.State.Paused||Latest.State.ThrottleBps==bps)return Task.FromResult<BTravelReceipt>(null);
   return Exclusive(()=>Send("TRAVEL_THROTTLE",new JProperty("bps",bps)));
  }
  public Task<BTravelReceipt> PauseAsync(){
   RequireCurrent();PauseRequested=true;
   if(mapActive!=null)return Task.FromResult(Latest);
   if(active!=null)return active;
   if(!CanCommand||Latest.State.Paused)return Task.FromResult(Latest);
   return Exclusive(()=>Send("PAUSE",new JProperty("value",true)));
  }
  public Task<BTravelReceipt> ResumeAsync(){
   RequireCurrent();if(Busy||!CanCommand||!Latest.State.Paused)return Task.FromResult<BTravelReceipt>(null);
   PauseRequested=false;return Exclusive(()=>Send("PAUSE",new JProperty("value",false)));
  }
  public bool ContinueAfterArrival(){
   RequireCurrent();if(Busy||NeedsRecovery||Latest?.ArrivalEvidenceRef==null||MapArrival?.Committed!=true||MapArrival.NodeStart==null)return false;
   PauseRequested=false;return true;
  }
  public BRouteCommandResult ReadCurrentMapArrival(){
   RequireCurrent();if(Latest?.IsCurrentMap!=true||Latest.RunStatus!="ACTIVE"||(string)Latest.MapState?["phase"]!="ACTIVE")return null;
   var saved=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.MapArrival);
   if(saved?.AcceptedResponseJson==null)return null;
   var result=BRouteCommandResult.Read(200,saved.AcceptedResponseJson,saved.ReadMapArrivalCommand());
   if(result.NodeStart==null||!JToken.DeepEquals(result.NodeStart.Capture(),Latest.MapReceipt?["nodeStart"])||result.RequiredMapRevision!=Latest.MapRevision)return null;
   MapArrival=result;MapArrivalRecovered=true;return result;
  }
  public BRouteCommandResult ReadRetainedMapArrival(OperationMapStatus currentMap){
   RequireCurrent();
   if(currentMap==null||currentMap.RunId!=run||currentMap.RunStatus!="ACTIVE"||currentMap.Phase!="ACTIVE"||currentMap.BlockedByPendingTransactions||currentMap.CurrentAttempt==null)return null;
   if(currentMap.CurrentAttempt.AccountId!=account)throw new InvalidOperationException("Current map attempt account differs.");
   var saved=client.Operations.SingleOrDefault(o=>o.AccountId==account&&o.TargetId==run&&o.Kind==PendingRunKind.MapArrival);
   if(saved?.AcceptedResponseJson==null)return null;
   var result=BRouteCommandResult.Read(200,saved.AcceptedResponseJson,saved.ReadMapArrivalCommand());
   if(result.NodeStart==null)return null;
   _=LastRail.Battle.OrdinaryNodeAttempt.FromCurrent(result.NodeStart,currentMap.CurrentAttempt);
   MapArrival=result;MapArrivalRecovered=true;return result;
  }
  public Task<BTravelReceipt> ArriveAsync(){
   RequireCurrent();if(Busy||!CanCommand)return Task.FromResult<BTravelReceipt>(null);
   return Exclusive(()=>Send("ARRIVE"));
  }
  public Task<BTravelReceipt> RecoverAsync(){
   RequireCurrent();if(active!=null)return active;
   if(mapActive!=null)return Task.FromResult(Latest);
   // The confirmed evidence must survive until the separate durable map ARRIVE consumer accepts it.
   if(Latest?.ArrivalEvidenceRef!=null)return Task.FromResult(Latest);
   if(Latest==null||Latest.IsCurrentMap||HasPending)return RestoreAsync();
   return Exclusive(()=>Send("RECOVER"));
  }
  bool HasPending=>client.Operations.Any(o=>o.AccountId==account&&o.TargetId==run&&(o.Kind==PendingRunKind.TravelCommand||o.Kind==PendingRunKind.ConfirmDestination)&&o.AcceptedResponseJson==null);
  bool CanCommand=>restored&&Latest!=null&&!Latest.IsCurrentMap&&Latest.ArrivalEvidenceRef==null&&
   !HasPending;
  Task<BTravelReceipt> Exclusive(Func<Task<BTravelReceipt>> operation){
   var completion=new TaskCompletionSource<BTravelReceipt>();active=completion.Task;
   _=Complete();return completion.Task;
   async Task Complete(){
    try{
     var result=await operation();RequireCurrent();
     if(result!=null&&PauseRequested&&!result.IsCurrentMap&&result.ArrivalEvidenceRef==null&&!result.State.Paused)
      result=await Send("PAUSE",new JProperty("value",true));
     RequireCurrent();active=null;completion.SetResult(result);
    }catch(Exception error){active=null;completion.SetException(error);}
   }
  }
  async Task<BTravelReceipt> Send(string kind,JProperty value=null){
   RequireCurrent();var source=Latest;if(source==null||source.IsCurrentMap||source.ArrivalEvidenceRef!=null)return null;
   var body=new JObject{["version"]=BTravelCommand.Version,["requestId"]=Guid.NewGuid().ToString("N"),
    ["destinationEventId"]=source.DestinationEventId,["nodeId"]=source.NodeId,["mapRevision"]=source.MapRevision,
    ["expectedRevision"]=source.Revision,["kind"]=kind};
   if(value!=null)body.Add(value);
   var command=BTravelCommand.Read(body.ToString(Formatting.None));var result=await client.TravelAsync(run,command);
   RequireCurrent();if(result?.AcceptedResponseJson==null){restored=false;return null;}
   var receipt=BTravelReceipt.Read(result.AcceptedResponseJson,command);Apply(receipt);return Latest;
  }
  void Apply(BTravelReceipt receipt){
   RequireCurrent();if(Latest?.IsCurrentMap==true&&!receipt.IsCurrentMap)return;
   if(Latest!=null&&receipt.IsCurrentMap&&(receipt.MapRevision<Latest.MapRevision||Latest.IsCurrentMap&&Latest.RunStatus!="ACTIVE"&&receipt.RunStatus=="ACTIVE"))return;
   if(Latest!=null&&!Latest.IsCurrentMap&&!receipt.IsCurrentMap){
    if(receipt.DestinationEventId!=Latest.DestinationEventId||receipt.NodeId!=Latest.NodeId||receipt.MapRevision!=Latest.MapRevision)
     throw new InvalidOperationException("Travel destination changed without an authoritative map transition.");
    if(receipt.Revision<Latest.Revision)return;
    if(receipt.Revision==Latest.Revision&&!JToken.DeepEquals(receipt.Capture(),Latest.Capture()))throw new InvalidOperationException("Travel revision conflict.");
   }
   Latest=receipt;if(receipt.PauseIntent)PauseRequested=true;
  }
  public void Dispose(){disposed=true;Latest=null;DestinationReceipt=null;destinationNode=null;MapArrival=null;MapArrivalRoute=null;}
 }
 public sealed partial class AccountScreenRuntime {
  OrdinaryTravelOwner ordinaryTravel;
  string ordinaryTravelRun;
  Task<BRouteCommandResult> ordinaryMapArrivalRequest;
  void ResetOrdinaryTravel(){ordinaryNodePreparation?.Dispose();ordinaryNodePreparation=null;ordinaryTravel?.Dispose();ordinaryTravel=null;ordinaryTravelRun=null;ordinaryMapArrivalRequest=null;ResetOrdinaryMap();}
  public OrdinaryTravelOwner ReadOrdinaryTravel(string runId){
   SynchronizeSession();if(!Current(owner,ownerGeneration)||runRecovery==null)throw new InvalidOperationException("Travel account owner unavailable.");
   var selected=RunSelection;if(selected.Kind!=LobbyRunSelectionKind.Recovery||selected.RunId!=runId)throw new InvalidOperationException("Travel run is not selected.");
   if(ordinaryTravelRun!=runId){
    ResetOrdinaryTravel();var account=owner.AccountId;var stamp=ownerGeneration;var client=runRecovery;
    ordinaryTravel=new OrdinaryTravelOwner(client,account,runId,()=>!disposed&&session()?.AccountId==account&&generation()==stamp&&ReferenceEquals(client,runRecovery)&&RunSelection.RunId==runId);
    ordinaryTravelRun=runId;
   }
   return ordinaryTravel;
  }
  public Task<BRouteCommandResult> CommitOrdinaryMapArrivalAsync(string runId,BRouteBinding route=null){
   var travel=ReadOrdinaryTravel(runId);if(ordinaryMapArrivalRequest!=null)return ordinaryMapArrivalRequest;
   var original=owner;var stamp=ownerGeneration;var completion=new TaskCompletionSource<BRouteCommandResult>();ordinaryMapArrivalRequest=completion.Task;
   _=Complete();return completion.Task;
   async Task Complete(){
    try{
     await travel.RestoreAsync();
     if(!Current(original,stamp)||!ReferenceEquals(travel,ordinaryTravel))throw new InvalidOperationException("Map arrival owner changed.");
     if(route==null&&travel.NeedsMapArrivalRoute){
      // First-needed route information only; saved requests and successful responses are reused without a confirmation GET.
      route=await new BRouteHttpClient(new GameApiAuthenticatedHttpTransport(wire)).ReadAsync(original.AccountId,runId,original.AccessToken);
      if(!Current(original,stamp)||!ReferenceEquals(travel,ordinaryTravel))throw new InvalidOperationException("Map route owner changed during read.");
     }
     var result=await travel.CommitMapArrivalAsync(route);
     if(!Current(original,stamp)||!ReferenceEquals(travel,ordinaryTravel))throw new InvalidOperationException("Map arrival owner changed before publication.");
     completion.SetResult(result);
    }catch(Exception error){completion.SetException(error);}
    finally{if(ReferenceEquals(ordinaryMapArrivalRequest,completion.Task))ordinaryMapArrivalRequest=null;}
   }
  }
  public async Task<BRouteCommandResult> PrepareOrdinaryTravelAsync(string runId){
   var travel=ReadOrdinaryTravel(runId);
   if(runRecovery.Operations.Any(o=>o.TargetId==runId&&(o.Kind==PendingRunKind.CompleteOrdinaryNode||o.Kind==PendingRunKind.Finish||o.Kind==PendingRunKind.Claim)))return null;
   var receipt=await travel.RestoreAsync();
   if(receipt==null){
    if(travel.Latest!=null||travel.DestinationReceipt!=null||runRecovery.Operations.Any(o=>o.TargetId==runId&&(o.Kind==PendingRunKind.TravelCommand||o.Kind==PendingRunKind.ConfirmDestination)&&o.AcceptedResponseJson==null))
     throw new OrdinaryNodeStartUnavailableException("이동 요청 결과를 확인하지 못했습니다. 원래 요청을 보존했습니다.",false,OrdinaryPreparationFailure.OutcomeUnknown);
    if(travel.PauseRequested)throw new OrdinaryNodeStartUnavailableException("일시정지 중에는 전투를 시작하지 않습니다.");
    if(runRecovery.Operations.Any(o=>o.TargetId==runId&&o.Kind==PendingRunKind.MapArrival&&o.AcceptedResponseJson!=null)){
     var currentMap=await ReadOrdinaryMapAsync(runId);var retainedArrival=travel.ReadRetainedMapArrival(currentMap);
     if(retainedArrival==null)throw new OrdinaryNodeStartUnavailableException("현재 전투 시도의 시작 원본을 확인하지 못했습니다.",currentMap.RunStatus!="ACTIVE");
     retainedArrival.NodeStart.RequireOriginal(ReadOrdinaryCombatInput(runId));return retainedArrival;
    }
    return null; // No initial travel state is manufactured; the existing node-start gate remains authoritative.
   }
   if(receipt.IsCurrentMap&&receipt.RunStatus!="ACTIVE")throw new OrdinaryNodeStartUnavailableException("작전 결과를 먼저 확인해 주세요.",true);
   if(receipt.ArrivalEvidenceRef!=null){
    var map=await CommitOrdinaryMapArrivalAsync(runId);
    if(map==null)throw new OrdinaryNodeStartUnavailableException("도착 요청 결과를 확인하지 못했습니다. 원래 요청과 도착 증거를 보존했습니다.",false,OrdinaryPreparationFailure.OutcomeUnknown);
    map.NodeStart?.RequireOriginal(ReadOrdinaryCombatInput(runId));
    if(travel.PauseRequested)throw new OrdinaryNodeStartUnavailableException("일시정지 의도를 보존했습니다. 전투를 자동으로 시작하지 않습니다.");
    if(map.NodeStart==null)throw new OrdinaryNodeStartUnavailableException("지도 도착은 확인했지만 전투 노드의 시작 상태가 아직 제공되지 않았습니다.");
    return map;
   }
   if(travel.PauseRequested)throw new OrdinaryNodeStartUnavailableException("일시정지 의도를 보존했습니다. 전투를 자동으로 시작하지 않습니다.");
   if(receipt.IsCurrentMap){
    if((string)receipt.MapState?["phase"]!="ACTIVE")
     throw new OrdinaryNodeStartUnavailableException("현재 전투 노드의 시작 상태가 아직 제공되지 않았습니다.");
    var currentMap=await ReadOrdinaryMapAsync(runId);
    var map=travel.ReadRetainedMapArrival(currentMap);
    if(map==null)throw new OrdinaryNodeStartUnavailableException("원래 노드 입장 기록을 확인하지 못했습니다. 작전은 유지됩니다.");
    map.NodeStart.RequireOriginal(ReadOrdinaryCombatInput(runId));return map;
   }
   throw new OrdinaryNodeStartUnavailableException(receipt.ArrivalEvidenceRef!=null?"이동 도착은 확인했지만 전투 노드 입장은 아직 확정되지 않았습니다.":"이동을 다시 준비했어요. 전투 준비에 필요한 정보를 확인하지 못했습니다. 작전은 유지됩니다.");
  }
 }
}
