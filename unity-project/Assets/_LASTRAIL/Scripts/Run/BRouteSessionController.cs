using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Composes the existing node controller. No route clock, HP, RNG or proof broker.
 public sealed class BRouteSessionController:IDisposable {
  public const string Version="unity.combat-b.route-controller-local.v1.20261005";
  readonly string account,run;BRouteBinding route;BNodeSessionController node;BRouteCommand command;BRouteCommandResult result;
  readonly BRuntimeObservationDelivery delivery;readonly BResultReportDelivery reportDelivery;readonly BDepartureAssignmentIntake assignmentIntake;
  long generation,operationGeneration,minimumRequiredMapRevision,settlementEpoch;int mutationsInFlight;bool reconciliationRequired,disposed;Action<JObject> changed;
  BArrivalBoundarySession boundaryTravel;bool attachingBoundary,composingRoute;
  public BRouteBinding Route{get{Alive();return route;}}public bool CanStart=>false;
  public BRouteSessionController(string accountId,string runId){if(string.IsNullOrEmpty(accountId)||string.IsNullOrEmpty(runId))throw new ArgumentException("b_route_controller_identity_missing");account=accountId;run=runId;node=new BNodeSessionController(account,run);delivery=new BRuntimeObservationDelivery(CaptureOriginalRuntimeObservation);reportDelivery=new BResultReportDelivery(CaptureOriginalRuntimeObservation);assignmentIntake=new BDepartureAssignmentIntake(account,run,()=>route);}
  public void Bind(Action<JObject> callback){Alive();if(callback==null)throw new ArgumentNullException(nameof(callback));InvalidateDeliveries();changed=callback;generation=checked(generation+1);node.Bind(_=>{if(mutationsInFlight==0)Notify();});}
  public void Unbind(){Alive();try{InvalidateDeliveries();}finally{changed=null;generation=checked(generation+1);node.Unbind();}}
  public async Task<bool> RefreshAsync(Func<Task<BRouteBinding>> capture,BBoardingEventsNodeSnapshot boarding=null,BTravelRuntimeConfiguration motion=null,Func<BBoardingEventsNodeSnapshot,BTravelRuntimeConfiguration,BDirectAttackBasicLoop> create=null){
   Alive();if(composingRoute)throw new InvalidOperationException("b_route_composition_reentry");if(capture==null)throw new ArgumentNullException(nameof(capture));InvalidateDeliveries();node.InvalidatePending();long ticket=generation=NextOperationGeneration(),readEpoch=settlementEpoch;var next=await capture();if(disposed||ticket!=generation)return false;if(next==null||next.AccountId!=account||next.RunId!=run)throw new ArgumentException("b_route_controller_identity_conflict");assignmentIntake.RequireRoute(next);route?.RequireSameDeparture(next);
   if(route!=null&&route.Hash==next.Hash){if(mutationsInFlight==0&&readEpoch==settlementEpoch&&reconciliationRequired){reconciliationRequired=false;Notify();}return true;}
   // A changed route ends the boundary session; its durable original remains in
   // the supplied store. An unchanged route/view rebind keeps the same clock.
   // Same original node across map phases retains native state and observation.
   if(route?.NativeNode!=null&&next.NativeNode!=null&&JToken.DeepEquals(route.NativeNode.Capture(),next.NativeNode.Capture())){boundaryTravel?.Dispose();boundaryTravel=null;route=next;if(mutationsInFlight==0&&readEpoch==settlementEpoch)reconciliationRequired=false;Notify();return true;}
   var candidate=new BNodeSessionController(account,run);composingRoute=true;
   try{
    if(next.NativeNode!=null){var p=next.NativeNode.Capture();if(route?.NativeNode!=null){var old=route.NativeNode.Capture();candidate.BindVerifiedNode(old,BRouteBinding.Trust(old),0);}candidate.BindVerifiedNode(p,BRouteBinding.Trust(p),candidate.Revision);
     if(motion!=null||boarding!=null||create!=null)candidate.AttachLocalTravel(boarding,motion,create,candidate.Revision);
    }else if(motion!=null||boarding!=null||create!=null)throw new InvalidOperationException("b_route_native_node_source_unavailable");
    if(disposed||ticket!=generation)return false;
    if(changed!=null)candidate.Bind(_=>{if(mutationsInFlight==0)Notify();});var oldNode=node;node=candidate;candidate=null;boundaryTravel?.Dispose();boundaryTravel=null;route=next;if(mutationsInFlight==0&&readEpoch==settlementEpoch)reconciliationRequired=false;oldNode.Dispose();composingRoute=false;Notify();return true;
   }finally{composingRoute=false;candidate?.Dispose();}
  }
  public async Task<bool> ConfirmDestinationAsync(BOperationDestinationIntent intent,Func<Task<BOperationDestinationReceipt>> confirm){Alive();InvalidateDeliveries();long ticket=generation=NextOperationGeneration();mutationsInFlight=checked(mutationsInFlight+1);bool reconciled=false;try{bool accepted=await node.ConfirmDestinationAsync(intent,confirm,node.Revision);if(accepted&&!disposed){RememberDestinationRevision();reconciled=ticket==generation;}return accepted;}finally{mutationsInFlight--;if(!disposed){settlementEpoch=checked(settlementEpoch+1);if(!reconciled)reconciliationRequired=true;if(ticket==generation)Notify();}}}
  public void AttachLocalTravel(BBoardingEventsNodeSnapshot boarding,BTravelRuntimeConfiguration motion,Func<BBoardingEventsNodeSnapshot,BTravelRuntimeConfiguration,BDirectAttackBasicLoop> create){Alive();RequireNoBoundaryClock();RequireCurrentRoute();node.AttachLocalTravel(boarding,motion,create,node.Revision);InvalidateDeliveries();generation=NextOperationGeneration();}
  void RequireNoBoundaryClock(){if(boundaryTravel!=null||attachingBoundary||composingRoute)throw new InvalidOperationException("b_route_boundary_clock_already_owned");}
  public void AttachPersistentBoundaryTravel(BOperationDestinationReceipt destination,string context,Func<JObject> load,Action<JObject,long> save){
   Alive();RequireCurrentRoute(false);RequireNoBoundaryClock();
   if(node.HasLocalRuntime)throw new InvalidOperationException("b_route_native_clock_already_owned");
   var source=assignmentIntake.Bundle??throw new InvalidOperationException("b_route_original_departure_bundle_required");
   var originalRoute=route;var originalNode=node;long ticket=generation;BArrivalBoundarySession candidate=null;attachingBoundary=true;
   try{
    candidate=BArrivalBoundarySession.Open(source,originalRoute,destination,context,load,save);
    Alive();if(generation!=ticket||!ReferenceEquals(route,originalRoute)||!ReferenceEquals(node,originalNode))throw new InvalidOperationException("b_route_boundary_owner_changed");
    boundaryTravel=candidate;candidate=null;generation=NextOperationGeneration();
   }finally{attachingBoundary=false;candidate?.Dispose();}
   Notify();
  }
  public JObject CaptureBoundaryTravel(){Alive();RequireCurrentRoute(false);return boundaryTravel?.Capture()??throw new InvalidOperationException("b_route_boundary_clock_missing");}
  public JObject CaptureOriginalBoundaryArrival(string requestId){Alive();RequireCurrentRoute(false);return boundaryTravel?.CaptureExport(requestId)??throw new InvalidOperationException("b_route_boundary_clock_missing");}
  // Publish a new native owner only after restoring its original checkpoint and
  // durably installing the run's acquired effect. Intermediate candidates have
  // no UI subscribers and cannot expose an unpersisted/unbuffed combat state.
  internal void AttachPersistentLocalTravel(BBoardingEventsNodeSnapshot boarding,BTravelRuntimeConfiguration motion,Func<BBoardingEventsNodeSnapshot,BTravelRuntimeConfiguration,BDirectAttackBasicLoop> create,string context,Func<JObject> load,Action<JObject,long> save,Func<JObject,BDirectAttackBasicLoop> restore,CombatO22ApplicationOriginal acquired){
   Alive();RequireNoBoundaryClock();RequireCurrentRoute();
   if(node.HasLocalRuntime)throw new InvalidOperationException("b_node_runtime_already_attached");
   var originalNode=node;var originalRoute=route;var saved=node.Capture();
   InvalidateDeliveries();long ticket=generation=NextOperationGeneration();
   BNodeSessionController candidate=BNodeSessionController.Restore(saved,BDirectAttackInput.Digest(saved),BRouteBinding.Trust(route.NativeNode.Capture()),null,account,run);
   void RequireOwner(){Alive();if(ticket!=generation||!ReferenceEquals(node,originalNode)||!ReferenceEquals(route,originalRoute))throw new InvalidOperationException("Native attachment owner changed");}
   try{
    candidate.AttachLocalTravel(boarding,motion,create,candidate.Revision);
    candidate.ConfigureLocalCheckpoint(context,load,save,restore);RequireOwner();
    if(acquired!=null){candidate.ApplyO22(originalRoute,acquired,candidate.Revision);RequireOwner();}
    if(changed!=null)candidate.Bind(_=>{if(mutationsInFlight==0)Notify();});
    node=candidate;candidate=null;originalNode.Dispose();Notify();
   }finally{candidate?.Dispose();}
  }
  public async Task<bool> SendAsync(BRouteCommand originalCommand,Func<Task<BRouteCommandResult>> send){Alive();if(originalCommand==null||send==null||route==null||originalCommand.Binding.AccountId!=account||originalCommand.Binding.RunId!=run)throw new ArgumentException("b_route_controller_command_conflict");route.RequireOriginalDeparture(originalCommand.Binding);InvalidateDeliveries();node.InvalidatePending();long ticket=generation=NextOperationGeneration(),operationTicket=operationGeneration;mutationsInFlight=checked(mutationsInFlight+1);bool reconciled=false,publishChanged=false;try{var incoming=await send();if(disposed||ticket!=generation&&(originalCommand.Kind!="COMPLETE"||operationTicket!=operationGeneration))return false;if(incoming==null)throw new ArgumentException("b_route_controller_result_missing");var validated=BRouteCommandResult.Read(incoming.StatusCode,incoming.Capture().ToString(),originalCommand);bool same=command!=null&&JToken.DeepEquals(command.Capture(),originalCommand.Capture())&&result.StatusCode==validated.StatusCode&&JToken.DeepEquals(result.Capture(),validated.Capture());command=originalCommand;result=validated;if(validated.Committed&&!originalCommand.IsWave)minimumRequiredMapRevision=Math.Max(minimumRequiredMapRevision,validated.RequiredMapRevision);reconciled=true;publishChanged=!same;return true;}finally{mutationsInFlight--;if(!disposed){settlementEpoch=checked(settlementEpoch+1);if(!reconciled)reconciliationRequired=true;if(ticket==generation&&(!reconciled||publishChanged))Notify();}}}
  public FirstPlayableScaling ReadFirstPlayableScaling(OrdinaryFiveStartInput start,string initializationResponse){
   Alive();if(mutationsInFlight!=0||reconciliationRequired||result?.Committed!=true||result.Scaling==null)throw new InvalidOperationException("Confirmed first playable arrival required.");
   result.Scaling.RequireOriginal(start,initializationResponse);return result.Scaling;
  }
  public OrdinaryNodeStart ReadOrdinaryNodeStart(OrdinaryFiveStartInput start){
   Alive();if(mutationsInFlight!=0||reconciliationRequired||result?.Committed!=true||result.NodeStart==null)throw new InvalidOperationException("Confirmed node-start arrival required.");
   result.NodeStart.RequireOriginal(start);return result.NodeStart;
  }
  public void Advance(string eventId,long elapsed,int steps){Alive();RequireCurrentRoute(boundaryTravel==null);if(boundaryTravel!=null){boundaryTravel.Execute(new JObject{["kind"]="ADVANCE",["eventId"]=eventId,["elapsed"]=elapsed,["maximumSteps"]=steps});Notify();return;}node.Advance(eventId,elapsed,steps,node.Revision);InvalidateChangedDeliveries();}
  public void SetPaused(string eventId,bool paused){Alive();RequireCurrentRoute(boundaryTravel==null);if(boundaryTravel!=null){boundaryTravel.Execute(new JObject{["kind"]="PAUSE",["eventId"]=eventId,["value"]=paused});Notify();return;}node.SetPaused(eventId,paused,node.Revision);InvalidateChangedDeliveries();}
  public void SetHold(string eventId,string owner,bool held){Alive();RequireCurrentRoute();node.SetHold(eventId,owner,held,node.Revision);InvalidateChangedDeliveries();}
  string SelectionHold(string grant)=>"augment:"+BDirectAttackInput.Digest(new JArray(account,run,grant)).Substring(7);
  public void BeginAugmentSelection(string grant){
   Alive();RequireCurrentRoute(false);if(string.IsNullOrWhiteSpace(grant))throw new ArgumentException("Original grant required");
   if(route.NativeNode==null){
    var p=route.Capture();
    if((string)p["phase"]!="ACTIVE"||(string)p["runStatus"]!="ACTIVE"||(string)p["currentNode"]?["eventKind"]!="AUGMENT")
     throw new InvalidOperationException("b_route_active_augment_required");
    return;
   }
   // A route-only screen has no native clock to resume. APPLIED still requires
   // an attached, persistent native owner; this does not grant runtime support.
   if(!node.HasLocalRuntime)return;
   if(node.CheckpointRecoveryRequired)node.RecoverLocalCheckpoint();
   string hold=SelectionHold(grant);SetHold(hold+":begin",hold,true);
  }
  public void CompleteO22Selection(CombatO22ApplicationOriginal original){
   ApplyO22(original);string hold=SelectionHold(original.GrantEventId);
   SetHold(hold+":complete",hold,false);
  }
  public void ConfigureLocalCheckpoint(string context,Func<JObject> load,Action<JObject,long> save,Func<JObject,BDirectAttackBasicLoop> restore){Alive();RequireCurrentRoute();node.ConfigureLocalCheckpoint(context,load,save,restore);}
  public void RecoverLocalCheckpoint(){Alive();if(boundaryTravel!=null){RequireCurrentRoute(false);boundaryTravel.Recover();Notify();return;}node.RecoverLocalCheckpoint();InvalidateChangedDeliveries();}
  public bool CheckpointRecoveryRequired{get{Alive();return boundaryTravel?.RecoveryRequired??node.CheckpointRecoveryRequired;}}
  public string LocalCombatOutcome{get{Alive();return node.LocalCombatOutcome;}}
  public void ApplyO22(CombatO22ApplicationOriginal original){Alive();RequireCurrentRoute();node.ApplyO22(route,original,node.Revision);InvalidateChangedDeliveries();}
  public void MoveOrdinaryOperator(string eventId,string actorId,int slot,JArray localPoint,string geometryHash){Alive();RequireCurrentRoute();node.MoveOrdinaryOperator(eventId,actorId,slot,localPoint,geometryHash,node.Revision);InvalidateChangedDeliveries();}
  public void ApplyWaveBasic(string eventId,string sourceId,string targetId,long targetGeneration){Alive();RequireCurrentRoute();node.ApplyWaveBasic(eventId,sourceId,targetId,targetGeneration,node.Revision);InvalidateChangedDeliveries();}
  public void AcceptInterceptionSource(string eventId,string shotId,BInterceptTimingSnapshot receipt){Alive();RequireCurrentRoute();node.AcceptInterceptionSource(eventId,shotId,receipt,node.Revision);InvalidateChangedDeliveries();}
  public JObject ObserveOrdinaryGeometry(string sourceId,string targetId,long tick,long sourceRevision,string geometryHash){Alive();RequireCurrentRoute();return node.ObserveOrdinaryGeometry(sourceId,targetId,tick,sourceRevision,geometryHash);}
  void RequireCurrentRoute(bool requireNative=true){if(route==null||RequiresRefresh())throw new InvalidOperationException("b_route_authoritative_refresh_required");if(requireNative&&route.NativeNode==null)throw new InvalidOperationException("b_route_native_node_source_unavailable");}
  bool RequiresRefresh(){if(route==null||reconciliationRequired||mutationsInFlight>0||minimumRequiredMapRevision>route.MapRevision)return true;var selected=node.Capture()["destination"];if(selected.Type!=JTokenType.Null&&(long)selected["response"]["revision"]>route.MapRevision)return true;return result?.Committed==true&&!command.IsWave&&(long)result.Capture()["receipt"]["revision"]>route.MapRevision;}
  public JObject Capture(){Alive();return new JObject{["version"]=Version,["accountId"]=account,["runId"]=run,["route"]=route?.Capture()??(JToken)JValue.CreateNull(),["nodeSession"]=node.Capture(),["lastCommand"]=command?.Capture()??(JToken)JValue.CreateNull(),["commandBinding"]=command?.Binding.Capture()??(JToken)JValue.CreateNull(),["lastResult"]=result?.Capture()??(JToken)JValue.CreateNull(),["lastResultStatus"]=result==null?JValue.CreateNull():new JValue(result.StatusCode),["minimumRequiredMapRevision"]=minimumRequiredMapRevision,["mutationReconciliationRequired"]=reconciliationRequired||mutationsInFlight>0,["routeRefreshRequired"]=RequiresRefresh(),["canStart"]=false,["canAuthorizeEquipment"]=false};}
  public BRouteRuntimeObservation CaptureOriginalRuntimeObservation(){Alive();RequireCurrentRoute();return new BRouteRuntimeObservation(route,node.CaptureOriginalRuntimeObservation());}
  public Task<BLocalObservationDeliveryStatus> ForwardOriginalRuntimeObservationAsync(BRouteRuntimeObservation original,Func<BRouteRuntimeObservation,CancellationToken,Task> forward,CancellationToken cancellation=default){Alive();return delivery.ForwardAsync(original,forward,cancellation);}
  public Task<BResultReportDeliveryResult> SubmitOriginalResultReportAsync(BRouteRuntimeObservation original,BResultReportRequest request,BResultReportHttpClient client,LastRail.Auth.GameApiAccessToken token,CancellationToken cancellation=default){Alive();return reportDelivery.SendAsync(original,request,client,token,null,cancellation);}
  public Task<BResultReportDeliveryResult> RecoverOriginalResultReportAsync(BRouteRuntimeObservation original,BResultReportRequest request,string evidenceRef,BResultReportHttpClient client,LastRail.Auth.GameApiAccessToken token,CancellationToken cancellation=default){Alive();if(string.IsNullOrEmpty(evidenceRef))throw new ArgumentException("b_result_recovery_reference_invalid");return reportDelivery.SendAsync(original,request,client,token,evidenceRef,cancellation);}
  void InvalidateDeliveries(){try{delivery.Invalidate();}finally{try{reportDelivery.Invalidate();}finally{assignmentIntake.Invalidate();}}}
  void InvalidateChangedDeliveries(){try{delivery.InvalidateChangedSources();}finally{reportDelivery.InvalidateChangedSources();}}
  public BDepartureAssignment DepartureAssignment{get{Alive();return assignmentIntake.Current;}}
  public BDepartureSourceBundle DepartureSourceBundle{get{Alive();return assignmentIntake.Bundle;}}
  public Task<bool> ReadDepartureSourceBundleAsync(Func<CancellationToken,Task<BDepartureSourceBundle>> read,CancellationToken cancellation=default){Alive();return assignmentIntake.ReadBundleAsync(read,cancellation);}
  public Task<bool> ReadDepartureSourceBundleAsync(BDepartureSourceBundleHttpClient client,LastRail.Auth.GameApiAccessToken token,string expectedHash=null,CancellationToken cancellation=default){Alive();if(client==null)throw new ArgumentNullException(nameof(client));return ReadDepartureSourceBundleAsync(ct=>client.ReadAsync(account,run,token,expectedHash,ct),cancellation);}
  public BDepartureFormationAdapter PrepareDepartureFormation(){Alive();return new BDepartureFormationAdapter(assignmentIntake.Current??throw new InvalidOperationException("b_assignment_source_not_loaded"));}
  public Task<bool> ReadDepartureAssignmentAsync(Func<CancellationToken,Task<BDepartureAssignment>> read,CancellationToken cancellation=default){Alive();return assignmentIntake.ReadAsync(read,cancellation);}
  public Task<bool> ReadDepartureAssignmentAsync(BDepartureAssignmentHttpClient client,LastRail.Auth.GameApiAccessToken token,string expectedHash=null,CancellationToken cancellation=default){Alive();if(client==null)throw new ArgumentNullException(nameof(client));return ReadDepartureAssignmentAsync(ct=>client.ReadAsync(account,run,token,expectedHash,ct),cancellation);}
  public JObject CaptureLocalDeliveryCursor()=>delivery.CaptureCursor();
  public void RestoreLocalDeliveryCursor(JObject cursor,string independentHash)=>delivery.RestoreCursor(cursor,independentHash);
  public JObject DescribeRuntimeSources(){Alive();var p=node.Capture();var observation=p["observation"];return new JObject{["provenance"]="LOCAL_ORIGINAL_NATIVE_OBSERVATION_NOT_ACCEPTED_ROUTE_PROOF",["nativeContextId"]=route?.Capture()["nativeContextId"]??(JToken)JValue.CreateNull(),["mapRevision"]=route==null?JValue.CreateNull():new JValue(route.MapRevision),["nodeSession"]=p,["acceptedRouteEvidenceSupported"]=false,["checkpointHash"]=p["runtime"].Type==JTokenType.Null?JValue.CreateNull():new JValue(BDirectAttackInput.Digest(p["runtime"])),["originalEventId"]=observation.Type==JTokenType.Null?JValue.CreateNull():observation["originalObservation"]["acceptedEventId"].DeepClone(),["sourceRevision"]=observation.Type==JTokenType.Null?JValue.CreateNull():observation["originalObservation"]["sourceRevision"].DeepClone(),["missing"]=new JArray("SERVER_ACCEPTED_TRAVEL_ARRIVAL_EVENT","NODE_OBJECTIVE_AND_ALL_HAZARD_SETTLEMENT","SAFE_EVENT_PERMISSION","ORIGINAL_HOLD_TRANSACTION_SOURCE","SAMEBATCH_ACCEPTED_CHECKPOINT_FENCE")};}
  public static BRouteSessionController Restore(JObject saved,string independentHash,Func<JObject,BDirectAttackBasicLoop> restoreNative,string expectedAccount,string expectedRun){
   if(saved==null||BDirectAttackInput.Digest(saved)!=independentHash||(string)saved["version"]!=Version||(string)saved["accountId"]!=expectedAccount||(string)saved["runId"]!=expectedRun)throw new ArgumentException("b_route_controller_restore_pin_conflict");var owner=new BRouteSessionController(expectedAccount,expectedRun);
   try{if(saved["route"].Type!=JTokenType.Null){var r=(JObject)saved["route"];owner.route=BRouteBinding.Read(r.ToString(),(string)r["packetHash"],expectedAccount,expectedRun);}owner.minimumRequiredMapRevision=BDirectAttackInput.Nat(saved,"minimumRequiredMapRevision",0);if(saved["mutationReconciliationRequired"]?.Type!=JTokenType.Boolean)throw new ArgumentException("b_route_restore_reconciliation_invalid");owner.reconciliationRequired=(bool)saved["mutationReconciliationRequired"];var n=(JObject)saved["nodeSession"];owner.node.Dispose();owner.node=BNodeSessionController.Restore(n,BDirectAttackInput.Digest(n),owner.route?.NativeNode==null?null:BRouteBinding.Trust(owner.route.NativeNode.Capture()),restoreNative,expectedAccount,expectedRun);if(owner.route?.NativeNode==null?owner.node.Node!=null:owner.node.Node==null||!JToken.DeepEquals(owner.node.Node.Capture(),owner.route.NativeNode.Capture()))throw new ArgumentException("b_route_controller_restored_node_conflict");if(saved["lastCommand"].Type!=JTokenType.Null){var b=(JObject)saved["commandBinding"];var original=BRouteBinding.Read(b.ToString(),(string)b["packetHash"],expectedAccount,expectedRun);owner.route?.RequireOriginalDeparture(original);var c=(JObject)saved["lastCommand"];BDirectAttackInput.Fields(c,"version","command");if((string)c["version"]!=BRouteBinding.Version)throw new ArgumentException("b_route_restore_command_version");owner.command=BRouteCommand.FromOriginal(original,(JObject)c["command"],c["command"]["kind"]==null);owner.result=BRouteCommandResult.Read((int)BDirectAttackInput.Nat(saved,"lastResultStatus",200,599),saved["lastResult"].ToString(),owner.command);}if(owner.result?.Committed==true&&!owner.command.IsWave&&owner.minimumRequiredMapRevision<owner.result.RequiredMapRevision)throw new ArgumentException("b_route_restore_ack_revision_conflict");owner.RememberDestinationRevision();if(!JToken.DeepEquals(owner.Capture(),saved))throw new ArgumentException("b_route_controller_restore_conflict");return owner;}catch{owner.Dispose();throw;}
  }
  // View rebinding invalidates callbacks; owner operations also invalidate outstanding completion results.
  long NextOperationGeneration(){operationGeneration=checked(operationGeneration+1);return checked(generation+1);}
  void RememberDestinationRevision(){var d=node.Capture()["destination"];if(d.Type!=JTokenType.Null)minimumRequiredMapRevision=Math.Max(minimumRequiredMapRevision,(long)d["response"]["revision"]);}void Notify(){InvalidateChangedDeliveries();RememberDestinationRevision();changed?.Invoke(Capture());}void Alive(){if(disposed)throw new ObjectDisposedException(nameof(BRouteSessionController));}
  public void Dispose(){if(disposed)return;try{try{delivery.Dispose();}finally{try{reportDelivery.Dispose();}finally{assignmentIntake.Dispose();}}}finally{disposed=true;generation=NextOperationGeneration();changed=null;boundaryTravel?.Dispose();boundaryTravel=null;node.Dispose();route=null;command=null;result=null;}}
 }
}
