using System;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // No numeric defaults or inferred operating speed/acceleration authority.
 public sealed class BTravelRuntimeConfiguration {
  readonly JObject original;public string Hash{get;}public bool OperatingSupported=>false;
  BTravelRuntimeConfiguration(JObject source,string hash){if(source==null||BDirectAttackInput.Digest(source)!=hash)throw new ArgumentException("b_travel_configuration_pin_conflict");original=(JObject)source.DeepClone();Hash=hash;}
  public static BTravelRuntimeConfiguration LocalValidation(JObject original,string independentHash)=>new BTravelRuntimeConfiguration(original,independentHash);
  public JObject Capture()=>(JObject)original.DeepClone();
  public void RequireOperating()=>throw new InvalidOperationException("b_operating_travel_speed_acceleration_source_unavailable");
 }
 // Owns node replacement/publication and created runtime lifetime; original native
 // owners alone execute clock, HP, travel and hazards. No map proof is synthesized.
 public sealed partial class BNodeSessionController:IDisposable {
  public const string Version="unity.combat-b.node-controller-local.v1.20261005";
  readonly string account,run;BScoutGunnerNodeOwner node=new BScoutGunnerNodeOwner();
  BDirectAttackBasicLoop runtime;BTravelMaintenanceObservationSource observation;BOperationDestinationReceipt destination;
  long generation,revision;bool disposed;Action<JObject> changed;
  public BScoutGunnerNodeSnapshot Node{get{Alive();return node.Snapshot;}}
  public long Revision{get{Alive();return revision;}}public bool CanStart=>false;
  public bool HasLocalRuntime{get{Alive();return runtime!=null;}}
  public string LocalCombatOutcome{get{Alive();return runtime?.Outcome;}}
  public BNodeSessionController(string accountId,string runId){if(string.IsNullOrEmpty(accountId)||string.IsNullOrEmpty(runId))throw new ArgumentException("b_node_controller_identity_missing");account=accountId;run=runId;}
  public void Bind(Action<JObject> callback){Alive();changed=callback??throw new ArgumentNullException(nameof(callback));generation=checked(generation+1);}
  public void Unbind(){Alive();changed=null;generation=checked(generation+1);}
  internal void InvalidatePending(){Alive();generation=checked(generation+1);}
  void CheckRevision(long expected){Alive();if(checkpointUncertain||checkpointBusy)throw new InvalidOperationException("Restore the original node checkpoint before another command");if(expected!=revision)throw new InvalidOperationException("b_node_controller_revision_conflict");}
  BScoutGunnerNodeOwner StageNode(JObject packet,JObject trusted){
   var staged=new BScoutGunnerNodeOwner();staged.RestoreVerified(packet,trusted);var p=staged.Snapshot.Capture();
   if((string)p["origin"]["accountId"]!=account||staged.Snapshot.RunId!=run)throw new ArgumentException("b_node_controller_identity_conflict");
   if(node.Snapshot!=null){var previous=node.Snapshot.Capture();foreach(var key in new[]{"origin","nativeContextId","seedHash"})if(!JToken.DeepEquals(previous[key],p[key]))throw new ArgumentException("b_node_controller_original_context_conflict");if(!JToken.DeepEquals(previous,p)&&(staged.Snapshot.VisitDepth<=node.Snapshot.VisitDepth||(string)p["priorSourceHash"]!=(string)previous["sourceHash"]))throw new ArgumentException("b_node_controller_prior_source_conflict");}
   return staged;
  }
  public void BindVerifiedNode(JObject packet,JObject trusted,long expectedRevision){CheckRevision(expectedRevision);var staged=StageNode(packet,trusted);if(node.Snapshot!=null&&JToken.DeepEquals(node.Snapshot.Capture(),staged.Snapshot.Capture()))return;if(runtime!=null)throw new InvalidOperationException("b_node_live_replacement_requires_runtime");generation=checked(generation+1);Replace(staged);}
  // Borrowed capture task cannot publish after a new request/rebind/disposal.
  public async Task<bool> BindVerifiedNodeAsync(Func<Task<JObject>> capture,JObject independentlyTrustedOriginal,long expectedRevision){CheckRevision(expectedRevision);if(capture==null||independentlyTrustedOriginal==null)throw new ArgumentNullException();var trust=(JObject)independentlyTrustedOriginal.DeepClone();long ticket=generation=checked(generation+1);var packet=await capture();if(disposed||ticket!=generation)return false;var staged=StageNode(packet,trust);if(node.Snapshot!=null&&JToken.DeepEquals(node.Snapshot.Capture(),staged.Snapshot.Capture()))return true;if(runtime!=null)throw new InvalidOperationException("b_node_live_replacement_requires_runtime");Replace(staged);return true;}
  void Replace(BScoutGunnerNodeOwner staged){long nextRevision=checked(revision+1);var old=observation;node=staged;runtime=null;observation=null;destination=null;revision=nextRevision;old?.Dispose();Notify();}
  public void AttachLocalTravel(BBoardingEventsNodeSnapshot source,BTravelRuntimeConfiguration configuration,Func<BBoardingEventsNodeSnapshot,BTravelRuntimeConfiguration,BDirectAttackBasicLoop> create,long expectedRevision){
   CheckRevision(expectedRevision);if(runtime!=null)throw new InvalidOperationException("b_node_runtime_already_attached");if(configuration==null)throw new InvalidOperationException("b_travel_typed_configuration_required");if(source==null||node.Snapshot==null||!JToken.DeepEquals(source.OriginalSource.Capture(),node.Snapshot.Capture())||create==null)throw new ArgumentException("b_node_runtime_source_conflict");
   var candidate=create(source,configuration)??throw new InvalidOperationException("b_node_runtime_missing");var p=candidate.Capture();var travel=p["travel"];
   if(travel==null||(string)travel["configurationHash"]!=configuration.Hash||(string)travel["nodeSourceHash"]!=(string)node.Snapshot.Capture()["sourceHash"]||!JToken.DeepEquals(travel["origin"],node.Snapshot.Capture()["origin"])||(string)travel["nativeContextId"]!=node.Snapshot.NativeContextId||(string)travel["nodeId"]!=node.Snapshot.NodeId)throw new ArgumentException("b_node_runtime_original_binding_conflict");
   long nextRevision=checked(revision+1),nextGeneration=checked(generation+1);var next=new BTravelMaintenanceObservationSource(candidate);var old=observation;runtime=candidate;observation=next;revision=nextRevision;generation=nextGeneration;old?.Dispose();Notify();
  }
  public void ReplaceVerifiedLocalSession(JObject packet,JObject trusted,BBoardingEventsNodeSnapshot source,BTravelRuntimeConfiguration configuration,Func<BBoardingEventsNodeSnapshot,BTravelRuntimeConfiguration,BDirectAttackBasicLoop> create,long expectedRevision){
   if(checkpointSave!=null)throw new InvalidOperationException("A persistent node must finish its original checkpoint lifetime before replacement");
   CheckRevision(expectedRevision);var staged=StageNode(packet,trusted);
   if(node.Snapshot!=null&&JToken.DeepEquals(node.Snapshot.Capture(),staged.Snapshot.Capture()))throw new InvalidOperationException("b_node_same_source_runtime_reset_forbidden");
   using(var candidate=new BNodeSessionController(account,run)){
    candidate.node=staged;candidate.AttachLocalTravel(source,configuration,create,0);
    long nextRevision=checked(revision+1),nextGeneration=checked(generation+1);var old=observation;
    node=staged;runtime=candidate.runtime;observation=candidate.observation;candidate.node=new BScoutGunnerNodeOwner();candidate.runtime=null;candidate.observation=null;destination=null;revision=nextRevision;generation=nextGeneration;old?.Dispose();Notify();
   }
  }
  void RequireNativeUnchanged(){if(checkpointUncertain||checkpointBusy)throw new InvalidOperationException("Original checkpoint recovery required before publishing runtime evidence");if(runtime==null)throw new InvalidOperationException("b_node_runtime_missing");var p=observation.Capture();observation.RequireUnchanged(observation.Revision,(string)p["originalObservation"]["nativeHash"]);}
  void ExecuteOriginal(string eventId,long expectedRevision,Action<BDirectAttackBasicLoop> execute){CheckRevision(expectedRevision);RequireNativeUnchanged();if(checkpointSave!=null){ExecutePersisted(eventId,execute);return;}var before=runtime.Capture();execute(runtime);if(JToken.DeepEquals(before,runtime.Capture()))return;observation.ObserveAccepted(eventId,observation.Revision);Notify();}
  public void ApplyO22(BRouteBinding route,CombatO22ApplicationOriginal original,long expectedRevision){if(checkpointSave==null)throw new InvalidOperationException("Install original node persistence before APPLIED");ExecuteOriginal(original?.ApplicationId??throw new ArgumentNullException(nameof(original)),expectedRevision,n=>n.ApplyO22(route,original));}
  public void Advance(string eventId,long elapsed,int maxSteps,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.Advance(eventId,elapsed,maxSteps));
  public void AdvanceMiraFirstFrame(string eventId,long elapsed,IMiraFirstGeometrySource geometry,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.AdvanceMiraFirstFrame(eventId,elapsed,geometry));
  public void SetPaused(string eventId,bool paused,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.SetPaused(eventId,paused));
  public void SetHold(string eventId,string owner,bool held,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.SetHold(eventId,owner,held));
  public void MoveOrdinaryOperator(string eventId,string actorId,int slot,JArray localPoint,string geometryHash,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.MoveOrdinaryOperator(eventId,actorId,slot,localPoint,geometryHash));
  public void ApplyWaveBasic(string eventId,string sourceId,string targetId,long targetGeneration,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.ApplyWaveBasic(eventId,sourceId,targetId,targetGeneration));
  public void AcceptInterceptionSource(string eventId,string shotId,BInterceptTimingSnapshot receipt,long expectedRevision)=>ExecuteOriginal(eventId,expectedRevision,n=>n.AcceptInterceptionSource(eventId,shotId,receipt));
  public JObject ObserveOrdinaryGeometry(string sourceId,string targetId,long tick,long sourceRevision,string geometryHash){Alive();RequireNativeUnchanged();return runtime.ObserveOrdinaryGeometry(sourceId,targetId,tick,sourceRevision,geometryHash);}
  public async Task<bool> ConfirmDestinationAsync(BOperationDestinationIntent intent,Func<Task<BOperationDestinationReceipt>> confirm,long expectedRevision){CheckRevision(expectedRevision);if(intent==null||intent.AccountId!=account||intent.RunId!=run||confirm==null)throw new ArgumentException("b_node_destination_identity_conflict");long ticket=generation=checked(generation+1);var receipt=await confirm();if(disposed||ticket!=generation)return false;if(receipt==null||!JToken.DeepEquals(receipt.Intent.Capture(),intent.Capture()))throw new ArgumentException("b_node_destination_response_conflict");destination=receipt;Notify();return true;}
  public void RequireAcceptedArrivalOrCompletion(){Alive();throw new InvalidOperationException("b_node_accepted_arrival_completion_adapter_unavailable");}
  internal JObject CaptureOriginalRuntimeObservation(){Alive();RequireNativeUnchanged();return Capture();}
  public JObject Capture(){Alive();return new JObject{["version"]=Version,["accountId"]=account,["runId"]=run,["localRevision"]=revision,["node"]=node.Snapshot?.Capture()??(JToken)JValue.CreateNull(),["runtime"]=runtime?.Capture()??(JToken)JValue.CreateNull(),["observation"]=observation?.Capture()??(JToken)JValue.CreateNull(),["destination"]=destination?.Capture()??(JToken)JValue.CreateNull(),["canStart"]=false,["acceptedMapTransitionSupported"]=false};}
  public static BNodeSessionController Restore(JObject saved,string independentHash,JObject trustedNode,Func<JObject,BDirectAttackBasicLoop> restoreNative,string expectedAccount,string expectedRun){
   if(saved==null||BDirectAttackInput.Digest(saved)!=independentHash||(string)saved["version"]!=Version||(string)saved["accountId"]!=expectedAccount||(string)saved["runId"]!=expectedRun)throw new ArgumentException("b_node_controller_restore_pin_conflict");var owner=new BNodeSessionController(expectedAccount,expectedRun);
   try{if(saved["node"]?.Type!=JTokenType.Null)owner.node=owner.StageNode((JObject)saved["node"],trustedNode);owner.revision=BDirectAttackInput.Nat(saved,"localRevision");if(saved["runtime"]?.Type!=JTokenType.Null){if(restoreNative==null||owner.node.Snapshot==null)throw new ArgumentException("b_node_controller_restore_source_missing");owner.runtime=restoreNative((JObject)saved["runtime"].DeepClone());if(!JToken.DeepEquals(owner.runtime?.Capture(),saved["runtime"])||!JToken.DeepEquals(saved["runtime"]["travel"]["origin"],owner.node.Snapshot.Capture()["origin"])||(string)saved["runtime"]["travel"]["nodeSourceHash"]!=(string)owner.node.Snapshot.Capture()["sourceHash"])throw new ArgumentException("b_node_controller_restore_runtime_conflict");var o=(JObject)saved["observation"];owner.observation=BTravelMaintenanceObservationSource.Restore(owner.runtime,o,BDirectAttackInput.Digest(o));}if(saved["destination"]?.Type!=JTokenType.Null){owner.destination=BOperationDestinationReceipt.Restore((JObject)saved["destination"]);if(owner.destination.Intent.AccountId!=expectedAccount||owner.destination.Intent.RunId!=expectedRun)throw new ArgumentException("b_node_destination_identity_conflict");}if(!JToken.DeepEquals(owner.Capture(),saved))throw new ArgumentException("b_node_controller_restore_conflict");return owner;}catch{owner.Dispose();throw;}
  }
  void Notify(){changed?.Invoke(Capture());}void Alive(){if(disposed)throw new ObjectDisposedException(nameof(BNodeSessionController));}
  public void Dispose(){if(disposed)return;disposed=true;generation=checked(generation+1);changed=null;observation?.Dispose();observation=null;runtime=null;node.Invalidate();destination=null;checkpointLoad=null;checkpointSave=null;checkpointRestore=null;}
 }
}
