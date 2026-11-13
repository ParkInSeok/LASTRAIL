using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // Owns one durable local boundary clock. The route controller owns lifetime and
 // exclusivity. Saved originals remain unverified by the server.
 public sealed class BArrivalBoundarySession : IDisposable {
  public const string Version="unity.b-arrival-boundary-session.v1.20261020";
  readonly JObject source,binding;
  readonly string sourceHash,context;
  Func<JObject> load;
  Action<JObject,long> save;
  BArrivalBoundaryTravel runtime;
  JObject retained,pendingRecovery;
  long revision;
  bool busy,uncertain,disposed;
  public bool RecoveryRequired {get{Alive();return uncertain;}}
  BArrivalBoundarySession(JObject source,JObject binding,string context,Func<JObject> load,Action<JObject,long> save){
   this.source=(JObject)source.DeepClone();this.binding=(JObject)binding.DeepClone();sourceHash=(string)source["sourceHash"];
   this.context=context;this.load=load;this.save=save;runtime=BArrivalBoundaryTravel.Create(this.source,sourceHash,this.binding);
  }
  public static BArrivalBoundarySession Open(BDepartureSourceBundle source,BRouteBinding route,BOperationDestinationReceipt destination,string context,Func<JObject> load,Action<JObject,long> save){
   if(source==null||route==null||destination==null||load==null||save==null)throw new ArgumentNullException();
   if(string.IsNullOrWhiteSpace(context)||context.Length>200||context.Trim()!=context)throw new ArgumentException("b_arrival_session_context");
   source.RequireSameDeparture(route);var r=route.Capture();var original=source.Capture();var d=destination.Capture();
   if((string)r["runStatus"]!="ACTIVE"||(string)r["phase"]!="TRAVELLING"||!(r["pendingNode"] is JObject pending)
    ||destination.Intent.AccountId!=route.AccountId||destination.Intent.RunId!=route.RunId
    ||destination.Intent.ExpectedRevision!=route.MapRevision-1
    ||destination.Intent.EdgeId!=(string)r["pendingEdgeId"]||(long)d["response"]["revision"]!=route.MapRevision
    ||(string)d["response"]["pendingEdgeId"]!=(string)r["pendingEdgeId"]
    ||!JToken.DeepEquals(original["originalOrigin"],r["originalNativeOrigin"]))
    throw new ArgumentException("b_arrival_session_destination_binding");
   var boundary=source.OriginalCombatSource["profile"]?["definition"]?["bArrivalBoundarySource"] as JObject;
   if(boundary==null||!JToken.DeepEquals(boundary["origin"],original["originalOrigin"]))throw new ArgumentException("b_arrival_session_boundary_source_missing");
   var binding=new JObject{["accountId"]=route.AccountId,["runId"]=route.RunId,["nodeId"]=pending["nodeId"].DeepClone(),
    ["nativeContextId"]=r["nativeContextId"].DeepClone(),["departurePinHash"]=r["departurePinHash"].DeepClone(),
    ["mapRevision"]=route.MapRevision,["destinationEventId"]=destination.Intent.CommandId,
    ["edgeId"]=r["pendingEdgeId"].DeepClone(),["origin"]=original["originalOrigin"].DeepClone()};
   var owner=new BArrivalBoundarySession(boundary,binding,context,load,save);
   try {
    owner.busy=true;var saved=owner.ReadStored();
    if(saved!=null)owner.AdoptValidated(saved);
    else {
     var initial=owner.Envelope(owner.runtime,1);
     save((JObject)initial.DeepClone(),0);
     owner.Alive();owner.retained=initial;owner.revision=1;
    }
    return owner;
   }catch{owner.Dispose();throw;}finally{owner.busy=false;}
  }
  JObject ReadStored(){var value=load();Alive();return value==null?null:(JObject)value.DeepClone();}
  JObject Envelope(BArrivalBoundaryTravel candidate,long nextRevision){
   var state=candidate.Capture();return new JObject{["version"]=Version,["contextId"]=context,["revision"]=nextRevision,["runtime"]=state,["runtimeHash"]=BDirectAttackInput.Digest(state)};
  }
  void AdoptValidated(JObject saved){
   BDirectAttackInput.Fields(saved,"version","contextId","revision","runtime","runtimeHash");
   if((string)saved["version"]!=Version||(string)saved["contextId"]!=context)throw new ArgumentException("b_arrival_session_checkpoint_context");
   long next=BDirectAttackInput.Nat(saved,"revision",1);
   if(next<revision)throw new ArgumentException("b_arrival_session_checkpoint_regression");
   var original=saved["runtime"] as JObject;
   var restored=BArrivalBoundaryTravel.Restore(source,sourceHash,binding,original,(string)saved["runtimeHash"]);
   if(next!=((JArray)original["travel"]["commands"]).Count+1L)throw new ArgumentException("b_arrival_session_revision_journal_conflict");
   if(retained!=null&&!JToken.DeepEquals(saved,retained)&&
    (pendingRecovery==null||!JToken.DeepEquals(saved,pendingRecovery)))throw new ArgumentException("b_arrival_session_unrelated_checkpoint");
   if(revision!=0&&next==revision&&!JToken.DeepEquals(retained,saved))throw new ArgumentException("b_arrival_session_checkpoint_fork");
   if(retained!=null){
    var prior=(JArray)retained["runtime"]["travel"]["commands"];var current=(JArray)original["travel"]["commands"];
    if(current.Count<prior.Count)throw new ArgumentException("b_arrival_session_journal_regression");
    for(int i=0;i<prior.Count;i++)if(!JToken.DeepEquals(prior[i],current[i]))throw new ArgumentException("b_arrival_session_journal_fork");
   }
   Alive();runtime=restored;retained=(JObject)saved.DeepClone();revision=next;
  }
  void Alive(){if(disposed)throw new ObjectDisposedException(nameof(BArrivalBoundarySession));}
  void Ready(){Alive();if(busy||uncertain)throw new InvalidOperationException("b_arrival_session_original_recovery_required");}
  public JObject Capture(){Alive();return (JObject)retained.DeepClone();}
  public void Execute(JObject command){
   Ready();var original=runtime.Capture();
   var candidate=BArrivalBoundaryTravel.Restore(source,sourceHash,binding,original,BDirectAttackInput.Digest(original));
   candidate.Execute(command==null?null:(JObject)command.DeepClone());
   if(JToken.DeepEquals(original,candidate.Capture()))return;
   if(revision>=9007199254740991)throw new InvalidOperationException("b_arrival_session_revision_exhausted");
   var staged=Envelope(candidate,revision+1);pendingRecovery=staged;busy=true;
   try {
    save((JObject)staged.DeepClone(),revision);
    Alive();runtime=candidate;retained=staged;revision++;pendingRecovery=null;
   }catch{uncertain=true;throw;}finally{busy=false;}
  }
  public JObject CaptureExport(string requestId){Ready();return runtime.CaptureExport(requestId);}
  public void Recover(){
   Alive();if(busy)throw new InvalidOperationException("b_arrival_session_busy");
   busy=true;uncertain=true;
   try{var saved=ReadStored();if(saved==null)throw new InvalidOperationException("b_arrival_session_original_checkpoint_missing");AdoptValidated(saved);pendingRecovery=null;uncertain=false;}
   finally{busy=false;}
  }
  public void Dispose(){if(disposed)return;disposed=true;load=null;save=null;runtime=null;}
 }
}
