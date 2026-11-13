using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // Supplied by the authoritative session owner, never inferred from a retained receipt.
 public sealed class OrdinaryNodeRestartScope {
  public string AccountId{get;}public string RunId{get;}public string NodeId{get;}public long Revision{get;}public long Generation{get;}
  public OrdinaryNodeRestartScope(string account,string run,string node,long revision,long generation){AccountId=account;RunId=run;NodeId=node;Revision=revision;Generation=generation;}
  internal bool Matches(OrdinaryNodeStart start,long revision,long generation)=>AccountId==start.AccountId&&RunId==start.RunId&&NodeId==start.NodeId&&Revision==revision&&Generation==generation;
 }

 // One retained request/response per node origin. A receipt is recovery data, not host authorization.
 public sealed class OrdinaryNodeRestartDelivery {
  static readonly SemaphoreSlim Gate=new SemaphoreSlim(1,1);
  const string Version="ordinary.node-restart-delivery.v1";
  const int MaximumBytes=1024*1024;
  readonly OrdinaryNodeStart start;
  readonly IAndroidKeystoreNativeStoragePort storage;
  readonly IGameApiAuthenticatedHttpTransport transport;
  readonly Func<OrdinaryNodeRestartScope> current;
  bool invalidated;
  string currentProjectionRequestKey;
  public GameApiAuthenticatedHttpResult LastTransportResult{get;private set;}
  public OrdinaryNodeRestartDelivery(OrdinaryNodeStart start,IAndroidKeystoreNativeStoragePort storage,IGameApiAuthenticatedHttpTransport transport,Func<OrdinaryNodeRestartScope> current){
   this.start=start??throw new ArgumentNullException(nameof(start));this.storage=storage??throw new ArgumentNullException(nameof(storage));this.transport=transport??throw new ArgumentNullException(nameof(transport));this.current=current??throw new ArgumentNullException(nameof(current));
  }
  // Use a dedicated file, never the existing operations.enc file. Its schema rejects cross-use.
  public static OrdinaryNodeRestartDelivery CreateEncryptedFile(OrdinaryNodeStart start,IAndroidKeystoreNativeStoragePort crypto,string absolutePath,IGameApiAuthenticatedHttpTransport transport,Func<OrdinaryNodeRestartScope> current)=>
   new OrdinaryNodeRestartDelivery(start,new AndroidEncryptedRunFilePort(crypto,absolutePath),transport,current);
  public Task<OrdinaryNodeRestartResult> DeliverAsync(OrdinaryNodeRestart request,GameApiAccessToken token,CancellationToken publicationCancellation=default){
   if(request==null)throw new ArgumentNullException(nameof(request));return ExecuteAsync(request,token,publicationCancellation);
  }
  public Task<OrdinaryNodeRestartResult> RecoverAsync(GameApiAccessToken token,CancellationToken publicationCancellation=default)=>ExecuteAsync(null,token,publicationCancellation);
  // Explicit current metadata permits retiring historical accepted receipts. An
  // uncertain request is always resolved with its original key before any new one.
  public Task<OrdinaryNodeRestartResult> RestartFromCurrentAsync(OrdinaryNodeAttempt projection,GameApiAccessToken token)=>RestartFromCurrentAsync(projection,token,null);
  internal async Task<OrdinaryNodeRestartResult> RestartFromCurrentAsync(OrdinaryNodeAttempt projection,GameApiAccessToken token,Func<bool> preparationCurrent){
   if(token==null)throw new ArgumentNullException(nameof(token));
   projection?.RequireNode(start);if(projection==null)throw new ArgumentNullException(nameof(projection));
   void RequireProjection(){var scope=current();if(scope==null||!scope.Matches(start,projection.Revision,projection.Generation))throw Conflict();}
   await Gate.WaitAsync();
   try{
    RequirePreparation(preparationCurrent);RequireProjection();LastTransportResult=null;
    var row=Load();OrdinaryNodeRestart request=null;OrdinaryNodeRestartResult accepted=null;
    if(row!=null){
     request=Decode(row);accepted=ReadAccepted(row,request);
     if(accepted==null){
      if((bool)row["invalidated"]||invalidated)throw Conflict();
      if(request.ExpectedGeneration>projection.Generation||request.ExpectedRevision>projection.Revision)throw Conflict();
      accepted=await SendCurrent(row,request,token,preparationCurrent);RequireProjection();
      if(accepted==null)return null;
      if(invalidated||(bool)row["invalidated"])throw Conflict();
      if(accepted.ResultAvailable)return accepted;
      if(request.ExpectedGeneration==projection.Generation&&request.ExpectedRevision==projection.Revision){currentProjectionRequestKey=request.Key;return accepted;}
      // A prior uncertain request may have produced exactly the current metadata.
      // A contradictory successful reply cannot supersede that current read.
      if(accepted.Generation!=projection.Generation||accepted.Revision!=projection.Revision)throw Conflict();
     }else{
      if(accepted.ResultAvailable)return accepted;
      if(accepted.Generation>projection.Generation||accepted.Revision>projection.Revision){
       if(request.Key==currentProjectionRequestKey&&request.ExpectedGeneration==projection.Generation&&request.ExpectedRevision==projection.Revision)return accepted;
       throw Conflict();
      }
     }
    }
    RequirePreparation(preparationCurrent);RequireProjection();
    request=new OrdinaryNodeRestart(start,Guid.NewGuid().ToString("N"),projection.Revision,projection.Generation);
    row=Encode(request);Save(row);invalidated=false;currentProjectionRequestKey=request.Key;
    accepted=await SendCurrent(row,request,token,preparationCurrent);RequireProjection();if(accepted!=null&&(invalidated||(bool)row["invalidated"]))throw Conflict();return accepted;
   }finally{Gate.Release();}
  }
  async Task<OrdinaryNodeRestartResult> SendCurrent(JObject row,OrdinaryNodeRestart request,GameApiAccessToken token,Func<bool> preparationCurrent=null){
   RequirePreparation(preparationCurrent);
   var response=await transport.SendAsync(new RunApiHttpRequest("POST",request.EndpointPath,request.BodyJson,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("Idempotency-Key",request.Key)}),token);
   LastTransportResult=response;
   if(response==null)throw new InvalidOperationException("ordinary_node_restart_transport_missing");
   if(response.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded||response.Response.StatusCode!=200){
    if(response.Response!=null&&RunApiErrorJsonAdapter.OptionalString(response.Response.BodyJson,"errorCode")=="ordinary_node_restart_superseded"){row["invalidated"]=true;Save(row);invalidated=true;}
    return null;
   }
   var accepted=request.ReadResponse(response.Response.BodyJson);row["responseJson"]=response.Response.BodyJson;if(invalidated)row["invalidated"]=true;Save(row);return accepted;
  }
  public async Task InvalidateAsync(){
   invalidated=true;
   await Gate.WaitAsync();
   try{var row=Load();if(row!=null){row["invalidated"]=true;Save(row);}}
   finally{Gate.Release();}
  }
  // Cancellation suppresses an unsent request only. Once sent, its receipt is
  // persisted and the caller separately decides whether a screen may publish it.
  static void RequirePreparation(Func<bool> current){if(current!=null&&!current())throw new InvalidOperationException("Node preparation ended before transport.");}
  internal Task<OrdinaryNodeRestartResult> DeliverAsync(OrdinaryNodeRestart request,GameApiAccessToken token,Func<bool> preparationCurrent)=>ExecuteAsync(request,token,default,preparationCurrent);
  internal Task<OrdinaryNodeRestartResult> RecoverAsync(GameApiAccessToken token,Func<bool> preparationCurrent)=>ExecuteAsync(null,token,default,preparationCurrent);
  async Task<OrdinaryNodeRestartResult> ExecuteAsync(OrdinaryNodeRestart supplied,GameApiAccessToken token,CancellationToken cancellation,Func<bool> preparationCurrent=null){
   if(token==null)throw new ArgumentNullException(nameof(token));
   await Gate.WaitAsync(cancellation);
   try{
    cancellation.ThrowIfCancellationRequested();RequirePreparation(preparationCurrent);LastTransportResult=null;
    var row=Load();OrdinaryNodeRestart request;
    if(row==null){
     if(supplied==null)return null;
     request=supplied;RequireOrigin(request);RequireCurrent(request,null);
     if(invalidated)throw Conflict();row=Encode(request);Save(row);
    }else{
     request=Decode(row);
     if(supplied!=null){
      RequireOrigin(supplied);
      if(supplied.Key!=request.Key||supplied.BodyJson!=request.BodyJson){
       if(supplied.Key==request.Key)throw Conflict();
       // Only an explicitly retired, accepted attempt may be replaced by its exact successor.
       var previous=ReadAccepted(row,request);
       if(!(bool)row["invalidated"]||previous==null||previous.ResultAvailable||supplied.ExpectedRevision!=previous.Revision||supplied.ExpectedGeneration!=previous.Generation)throw Conflict();
       RequireCurrent(supplied,null);request=supplied;row=Encode(request);Save(row);invalidated=false;
      }
     }
    }
    if(invalidated||(bool)row["invalidated"])throw Conflict();
    var accepted=ReadAccepted(row,request);
    RequireCurrent(request,accepted);
    if(accepted==null){
     // Screen cancellation stops publication, not the already durable delivery's confirmation.
     accepted=await SendCurrent(row,request,token,preparationCurrent);
     if(accepted==null){cancellation.ThrowIfCancellationRequested();return null;}
    }
    cancellation.ThrowIfCancellationRequested();
    if(invalidated||(bool)row["invalidated"])throw Conflict();
    RequireCurrent(request,accepted);return accepted;
   }finally{Gate.Release();}
  }
  void RequireOrigin(OrdinaryNodeRestart request){var source=request.Original;if(source.AccountId!=start.AccountId||source.RunId!=start.RunId||source.NodeId!=start.NodeId||source.SnapshotHash!=start.SnapshotHash||source.EntryMapRevision!=start.EntryMapRevision)throw Conflict();}
  void RequireCurrent(OrdinaryNodeRestart request,OrdinaryNodeRestartResult accepted){
   var scope=current();
   if(scope==null||!(scope.Matches(start,request.ExpectedRevision,request.ExpectedGeneration)||accepted!=null&&scope.Matches(start,accepted.Revision,accepted.ResultAvailable?scope.Generation:accepted.Generation.Value)))throw Conflict();
  }
  static InvalidOperationException Conflict()=>new InvalidOperationException("ordinary_node_restart_stale_or_conflicting");
  void Available(){if(!storage.IsAndroidRuntime||!storage.IsKeystoreAvailable||storage.UsedPlayerPrefsFallback)throw new InvalidOperationException("ordinary_node_restart_secure_storage_unavailable");}
  JObject Encode(OrdinaryNodeRestart request)=>new JObject{["version"]=Version,["accountId"]=start.AccountId,["runId"]=start.RunId,["nodeId"]=start.NodeId,["snapshotHash"]=start.SnapshotHash,["entryMapRevision"]=start.EntryMapRevision,["key"]=request.Key,["bodyJson"]=request.BodyJson,["responseJson"]=null,["invalidated"]=false};
  JObject Load(){
   Available();var encrypted=storage.Load();if(encrypted==null)return null;
   var row=SafeTransitionWire.Parse(storage.Decrypt(encrypted),MaximumBytes);
   BDirectAttackInput.Fields(row,"version","accountId","runId","nodeId","snapshotHash","entryMapRevision","key","bodyJson","responseJson","invalidated");
   if((string)row["version"]!=Version||(string)row["accountId"]!=start.AccountId||(string)row["runId"]!=start.RunId||(string)row["nodeId"]!=start.NodeId||(string)row["snapshotHash"]!=start.SnapshotHash||OrdinaryNodeStart.Revision(row,"entryMapRevision")!=start.EntryMapRevision||row["invalidated"].Type!=JTokenType.Boolean||row["responseJson"].Type!=JTokenType.Null&&row["responseJson"].Type!=JTokenType.String)throw Conflict();
   var request=Decode(row);ReadAccepted(row,request);return row;
  }
  OrdinaryNodeRestart Decode(JObject row){
   if(row["bodyJson"].Type!=JTokenType.String||row["key"].Type!=JTokenType.String)throw Conflict();
   var body=SafeTransitionWire.Parse((string)row["bodyJson"],4096);BDirectAttackInput.Fields(body,"nodeId","expectedRevision","expectedGeneration");
   var request=new OrdinaryNodeRestart(start,(string)row["key"],OrdinaryNodeStart.Revision(body,"expectedRevision"),OrdinaryNodeStart.Revision(body,"expectedGeneration"));
   if(request.BodyJson!=(string)row["bodyJson"])throw Conflict();return request;
  }
  static OrdinaryNodeRestartResult ReadAccepted(JObject row,OrdinaryNodeRestart request)=>row["responseJson"].Type==JTokenType.Null?null:request.ReadResponse((string)row["responseJson"]);
  void Save(JObject row){
   Available();var json=row.ToString(Formatting.None);
   // The wire reader limits a response to 256 KiB; reserve its escaped representation before HTTP.
   if(Encoding.UTF8.GetByteCount(json)+(row["responseJson"].Type==JTokenType.Null?768*1024:0)>MaximumBytes)throw new InvalidOperationException("ordinary_node_restart_storage_capacity");
   var encrypted=storage.Encrypt(json);if(encrypted==null||!storage.Save(encrypted))throw new InvalidOperationException("ordinary_node_restart_storage_write_failed");
  }
 }
}
