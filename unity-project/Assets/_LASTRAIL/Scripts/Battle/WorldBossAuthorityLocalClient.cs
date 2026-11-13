using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Nonvisual transport coordinator. No socket, token issuance, movement or health producer.
 public sealed class WorldBossAuthorityLocalClient:IDisposable {
  readonly JObject definition,profile;readonly string attempt,account,run,target;
  WorldBossAuthoritySnapshot state;WorldBossRailController controller;WorldBossAuthorityRequest pending;
  JObject acknowledgement;readonly JArray history=new JArray();WorldBossAuthorityError lastError;bool refreshRequired,authBlocked,dispatching,disposed;int pendingFailure;
  Action<WorldBossAuthorityRequest> requestSink;Action changedSink;
  public WorldBossAuthoritySnapshot State=>state;
  public WorldBossAuthorityRequest PendingRequest=>pending;
  public WorldBossAuthorityError LastError=>lastError;
  public JObject OriginalAcknowledgement=>acknowledgement==null?null:(JObject)acknowledgement.DeepClone();
  public bool AwaitingRead=>refreshRequired;
  public bool AuthenticationBlocked=>authBlocked;
  public WorldBossAuthorityLocalClient(JObject first,JObject trustedDefinition,JObject trustedProfile,string expectedAttempt,string owner,string expectedRun,string expectedTarget){
   definition=(JObject)trustedDefinition.DeepClone();profile=(JObject)trustedProfile.DeepClone();attempt=expectedAttempt;account=owner;run=expectedRun;target=expectedTarget;
   state=new WorldBossAuthoritySnapshot(first,definition,profile,attempt,run,target);controller=new WorldBossRailController(state.Rail,account);Attach(controller);
  }
  void Live(){if(disposed)throw new ObjectDisposedException(nameof(WorldBossAuthorityLocalClient));}
  void Mutable(){Live();if(dispatching)throw new InvalidOperationException("Authority request dispatch in progress");}
  void Attach(WorldBossRailController c){c.CommandRequested+=PrepareOriginal;}
  void Detach(WorldBossRailController c){c.CommandRequested-=PrepareOriginal;}
  void Notify(){if(!disposed)changedSink?.Invoke();}
  void Emit(WorldBossAuthorityRequest request){dispatching=true;try{if(!disposed)requestSink?.Invoke(request);}finally{dispatching=false;}}
  public void Bind(Action<WorldBossAuthorityRequest> requests,Action changed){Live();requestSink=requests;changedSink=changed;}
  public void Unbind(){requestSink=null;changedSink=null;}
  public bool Unbind(Action<WorldBossAuthorityRequest> owner){if(owner==null)throw new ArgumentNullException(nameof(owner));if(!ReferenceEquals(requestSink,owner))return false;Unbind();return true;}
  public WorldBossAuthorityRequest RequestRead(){Mutable();var request=new WorldBossAuthorityRequest(attempt,(string)definition["contentVersion"]);Emit(request);return request;}
  void PrepareOriginal(JObject original){pending=new WorldBossAuthorityRequest(attempt,(string)definition["contentVersion"],original,state.JournalRevision);acknowledgement=null;pendingFailure=0;Emit(pending);}
  public void PreviewRoute(string round,string side){Mutable();if(refreshRequired||authBlocked)throw new InvalidOperationException("Fresh authenticated read required");controller.PreviewRoute(round,side);Notify();}
  public WorldBossAuthorityRequest ConfirmPreview(string key){Mutable();if(refreshRequired||authBlocked)throw new InvalidOperationException("Fresh authenticated read required");ValidateNewKey(key);long ownerRevision=state.JournalRevision;var body=controller.ConfirmPreview(key);var original=new WorldBossAuthorityRequest(attempt,(string)definition["contentVersion"],body,ownerRevision);Notify();return original;}
  public WorldBossAuthorityRequest Internal(string round,string action,string key){Mutable();if(refreshRequired||authBlocked)throw new InvalidOperationException("Fresh authenticated read required");ValidateNewKey(key);long ownerRevision=state.JournalRevision;var body=controller.Internal(round,action,key);var original=new WorldBossAuthorityRequest(attempt,(string)definition["contentVersion"],body,ownerRevision);Notify();return original;}
  public WorldBossAuthorityRequest OriginalForRetry(){Live();if(pending==null)throw new InvalidOperationException("No unresolved original command");return pending;}
  public WorldBossAuthorityRequest RetryOriginal(){Mutable();if(authBlocked||pendingFailure==409)throw new InvalidOperationException("Authenticated current read required");var original=OriginalForRetry();Emit(original);return original;}
  public WorldBossAuthorityRequest ReplayOriginal(string key){Mutable();if(authBlocked)throw new InvalidOperationException("Authentication required");var item=history.SingleOrDefault(x=>(string)x["request"]["body"]["commandId"]==key);if(item==null)throw new ArgumentException("Original key not retained");var request=WorldBossAuthorityRequest.Restore(item["request"] as JObject,attempt,(string)definition["contentVersion"]);Emit(request);return request;}
  static void ResponseHeaders(string cache,string transport){WorldBossAuthoritySnapshot.Need(cache=="no-store"&&transport==WorldBossAuthorityRequest.Version);}
  public void AcceptAcknowledgement(WorldBossAuthorityRequest original,JObject response,string cacheControl,string responseTransport){
   Mutable();ResponseHeaders(cacheControl,responseTransport);WorldBossAuthoritySnapshot.Need(original!=null&&original.Method=="POST");
   var stored=history.SingleOrDefault(x=>WorldBossAuthoritySnapshot.Same(x["request"],original.Capture()));bool active=pending!=null&&WorldBossAuthoritySnapshot.Same(pending.Capture(),original.Capture());WorldBossAuthoritySnapshot.Need(active||stored!=null);
   WorldBossRailSnapshot.Fields(response,"accepted","transportVersion","status","requestJournalRevision","receipt");WorldBossAuthoritySnapshot.Need(response["accepted"].Type==JTokenType.Boolean&&(bool)response["accepted"]&&(string)response["transportVersion"]==WorldBossAuthorityRequest.Version&&(string)response["status"]=="LOCAL_TEST_PROFILE"&&WorldBossRailSnapshot.Nat(response,"requestJournalRevision")==original.OriginalJournalRevision);WorldBossRailSnapshot.ValidateReceipt(response["receipt"] as JObject,original.Body);
   var previous=active?acknowledgement:stored["acknowledgement"] as JObject;if(previous!=null)WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(previous,response));
   if(active){acknowledgement=(JObject)response.DeepClone();refreshRequired=true;pendingFailure=0;}else stored["acknowledgement"]=response.DeepClone();Notify();
  }
  public void AcceptFailure(WorldBossAuthorityRequest original,int status,JObject body,string cacheControl,string responseTransport){
   Mutable();ResponseHeaders(cacheControl,responseTransport);WorldBossAuthoritySnapshot.Need(original!=null);if(original.Method=="POST")WorldBossAuthoritySnapshot.Need(pending!=null&&WorldBossAuthoritySnapshot.Same(original.Capture(),pending.Capture())||history.Any(x=>WorldBossAuthoritySnapshot.Same(x["request"],original.Capture())));else WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(original.Capture(),new WorldBossAuthorityRequest(attempt,(string)definition["contentVersion"]).Capture()));
   var error=new WorldBossAuthorityError(status,body);lastError=error;if(status==401||status==403)authBlocked=true;if(status==409)refreshRequired=true;if(pending!=null&&original.Method=="POST"&&WorldBossAuthoritySnapshot.Same(original.Capture(),pending.Capture()))pendingFailure=status;Notify();
  }
  public void ApplyRead(JObject read,string cacheControl,string responseTransport){
   Mutable();ResponseHeaders(cacheControl,responseTransport);var candidate=new WorldBossAuthoritySnapshot(read,definition,profile,attempt,run,target);WorldBossAuthoritySnapshot.Need(candidate.JournalRevision>=state.JournalRevision&&candidate.Rail.Revision>=state.Rail.Revision&&candidate.HealthRevision>=state.HealthRevision&&candidate.ShieldRevision>=state.ShieldRevision);
   var old=state.Capture();var next=candidate.Capture();var oldEntries=(JArray)old["journal"]["entries"];var newEntries=(JArray)next["journal"]["entries"];for(int i=0;i<oldEntries.Count;i++)WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(oldEntries[i],newEntries[i]));
   if(old["health"]["receipt"].Type!=JTokenType.Null)WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(old["health"]["receipt"],next["health"]["receipt"]));
   var c=WorldBossRailController.RestoreLocal(controller.CaptureLocal(),definition,attempt,account);
   try{c.ApplyAuthoritativeSnapshot(candidate.Rail.Capture());bool resolved=pending!=null&&c.Pending==null,definitivelyAbsent=pending!=null&&pendingFailure==409&&!((JArray)candidate.Rail.Capture()["receipts"]).Any(r=>(string)r["accountId"]==account&&(string)r["commandId"]==(string)pending.Body["commandId"]);
    if(resolved&&acknowledgement!=null)WorldBossAuthoritySnapshot.Need(((JArray)candidate.Rail.Capture()["receipts"]).Any(r=>WorldBossAuthoritySnapshot.Same(r["receipt"],acknowledgement["receipt"])));
    if(definitivelyAbsent){c.Dispose();c=new WorldBossRailController(candidate.Rail,account);}
    if(resolved||definitivelyAbsent){WorldBossAuthoritySnapshot.Need(history.Count<12);history.Add(new JObject{["request"]=pending.Capture(),["acknowledgement"]=acknowledgement==null?JValue.CreateNull():(JToken)acknowledgement.DeepClone(),["disposition"]=resolved?"ORIGINAL_DOMAIN_RESULT":"DEFINITIVE_409_ABSENT"});}
    var previous=controller;Detach(previous);controller=c;Attach(c);state=candidate;if(resolved||definitivelyAbsent){pending=null;acknowledgement=null;pendingFailure=0;}refreshRequired=false;authBlocked=false;lastError=null;previous.Dispose();Notify();
   }catch{if(!ReferenceEquals(controller,c))c.Dispose();throw;}
  }
  public string CaptureLocal(){Live();return new JObject{["schema"]="world-boss.authority-client.v1",["state"]=state.Capture(),["controller"]=JObject.Parse(controller.CaptureLocal()),["pending"]=pending==null?JValue.CreateNull():(JToken)pending.Capture(),["acknowledgement"]=acknowledgement==null?JValue.CreateNull():(JToken)acknowledgement.DeepClone(),["history"]=history.DeepClone(),["pendingFailure"]=pendingFailure}.ToString(Newtonsoft.Json.Formatting.None);}
  public static WorldBossAuthorityLocalClient RestoreLocal(string json,JObject definition,JObject profile,string attempt,string account,string run,string target){
   if(json==null||json.Length>20971520)throw new ArgumentException("Authority client cache limit");var x=JObject.Parse(json);WorldBossRailSnapshot.Fields(x,"schema","state","controller","pending","acknowledgement","history","pendingFailure");WorldBossAuthoritySnapshot.Need((string)x["schema"]=="world-boss.authority-client.v1"&&x["history"] is JArray entries&&entries.Count<=12);
   var c=new WorldBossAuthorityLocalClient(x["state"] as JObject,definition,profile,attempt,account,run,target);
   try{var restored=WorldBossRailController.RestoreLocal(x["controller"].ToString(Newtonsoft.Json.Formatting.None),definition,attempt,account);WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(restored.State.Capture(),c.state.Rail.Capture()));c.Detach(c.controller);c.controller.Dispose();c.controller=restored;c.Attach(restored);
    foreach(JObject entry in (JArray)x["history"]){WorldBossRailSnapshot.Fields(entry,"request","acknowledgement","disposition");var request=WorldBossAuthorityRequest.Restore(entry["request"] as JObject,attempt,(string)definition["contentVersion"]);WorldBossAuthoritySnapshot.Need(request.Method=="POST"&&new[]{"ORIGINAL_DOMAIN_RESULT","DEFINITIVE_409_ABSENT"}.Contains((string)entry["disposition"]));WorldBossAuthoritySnapshot.Need(!c.history.Any(h=>(string)h["request"]["body"]["commandId"]==(string)request.Body["commandId"]));c.ValidateRestoredOriginal(request,(string)entry["disposition"],entry["acknowledgement"]);c.history.Add(entry.DeepClone());if(entry["acknowledgement"].Type!=JTokenType.Null)c.AcceptAcknowledgement(request,entry["acknowledgement"] as JObject,"no-store",WorldBossAuthorityRequest.Version);}
    if(x["pending"].Type!=JTokenType.Null){c.pending=WorldBossAuthorityRequest.Restore(x["pending"] as JObject,attempt,(string)definition["contentVersion"]);WorldBossAuthoritySnapshot.Need(c.pending.Method=="POST"&&c.controller.Pending!=null&&WorldBossAuthoritySnapshot.Same(c.pending.Body,c.controller.Pending));c.ValidateRestoredOriginal(c.pending,null,x["acknowledgement"]);WorldBossAuthoritySnapshot.Need(!c.history.Any(h=>(string)h["request"]["body"]["commandId"]==(string)c.pending.Body["commandId"]));if(x["acknowledgement"].Type!=JTokenType.Null)c.AcceptAcknowledgement(c.pending,x["acknowledgement"] as JObject,"no-store",WorldBossAuthorityRequest.Version);}else WorldBossAuthoritySnapshot.Need(c.controller.Pending==null&&x["acknowledgement"].Type==JTokenType.Null);
    int failure=checked((int)WorldBossRailSnapshot.Nat(x,"pendingFailure"));WorldBossAuthoritySnapshot.Need(new[]{0,400,401,403,404,409,503}.Contains(failure));WorldBossAuthoritySnapshot.Need(c.pending!=null||failure==0);c.pendingFailure=failure;c.refreshRequired=true;c.authBlocked=failure==401||failure==403;return c;
   }catch{c.Dispose();throw;}
  }
  void ValidateNewKey(string key){WorldBossAuthorityRequest.ValidateKey(key);if(history.Any(x=>(string)x["request"]["body"]["commandId"]==key))throw new ArgumentException("Retained original key requires replay");}
  void ValidateRestoredOriginal(WorldBossAuthorityRequest request,string disposition,JToken ack){
   WorldBossAuthoritySnapshot.Need(request.OriginalJournalRevision<=state.JournalRevision&&(long)request.Body["expectedRevision"]<=state.Rail.Revision);
   var records=(JArray)state.Rail.Capture()["receipts"];var record=records.SingleOrDefault(r=>(string)r["accountId"]==account&&(string)r["commandId"]==(string)request.Body["commandId"]);
   if(disposition=="ORIGINAL_DOMAIN_RESULT"){
    WorldBossAuthoritySnapshot.Need(record!=null&&(string)record["digest"]==WorldBossRailSnapshot.Canonical(request.Body));
    var entry=((JArray)state.Capture()["journal"]["entries"]).SingleOrDefault(e=>(string)e["kind"]=="COMMAND"&&(string)e["key"]==new JArray("COMMAND",account,(string)request.Body["commandId"]).ToString(Newtonsoft.Json.Formatting.None));
    WorldBossAuthoritySnapshot.Need(entry!=null&&(long)entry["ownerRevision"]==request.OriginalJournalRevision+1&&(string)entry["requestDigest"]==WorldBossRailSnapshot.Canonical(request.Body)&&WorldBossAuthoritySnapshot.Same(entry["receipt"],record["receipt"]));
    if(ack.Type!=JTokenType.Null)WorldBossAuthoritySnapshot.Need(WorldBossAuthoritySnapshot.Same(ack["receipt"],record["receipt"]));
   }else{WorldBossAuthoritySnapshot.Need(record==null);if(disposition=="DEFINITIVE_409_ABSENT")WorldBossAuthoritySnapshot.Need(ack.Type==JTokenType.Null);}
  }
  public void Dispose(){if(disposed)return;disposed=true;Unbind();Detach(controller);controller.Dispose();}
 }
}

