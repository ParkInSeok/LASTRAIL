using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Explicitly flushed by the product owner. Driver and authenticated transport are borrowed.
 public sealed class OrdinaryLocalExecutionSession:IDisposable {
  public const string EndpointPath="/__local/ordinary-execution";
  readonly CombatSceneDriver driver;
  readonly OrdinaryNodeAttempt attempt;
  readonly IGameApiAuthenticatedHttpTransport transport;
  readonly Func<GameApiAccessToken> token;
  readonly Func<bool> current;
  readonly OrdinaryLocalExecutionDelivery delivery;
  readonly Action<OrdinaryLocalExecutionDelivery.Batch> retainBeforeSend;
  long copiedSequence,capturedRevision;
  bool disposed,opened;
  JObject receipt;
  public string Origin {get;}
  public bool InFlight {get;private set;}
  // Each committed driver change contributes at most one external replay input.
  // Revisions also count non-input changes, making this a conservative queue bound.
  public bool CanAcceptMoreInputs=>!disposed&&!driver.IsRetired&&current()&&driver.TryGetCommittedRevision(out var revision)&&revision>=capturedRevision&&revision-capturedRevision<128;
  public GameApiAuthenticatedHttpResult LastTransportResult {get;private set;}
  public OrdinaryLocalExecutionDelivery.Batch Pending=>delivery.Pending;
  public long AcceptedSequence=>delivery.AcceptedSequence;
  public JObject LastReceipt=>receipt==null?null:(JObject)receipt.DeepClone();
  // The composition owner binds the borrowed transport to this same validated loopback origin.
  public OrdinaryLocalExecutionSession(CombatSceneDriver driver,OrdinaryNodeAttempt attempt,IGameApiAuthenticatedHttpTransport transport,string baseUrl,Func<GameApiAccessToken> tokenProvider,Func<bool> currentAttempt,Action<OrdinaryLocalExecutionDelivery.Batch> retainBeforeSend=null){
   if(!Uri.TryCreate(baseUrl,UriKind.Absolute,out var origin)||origin.Scheme!="http"||!origin.IsLoopback||origin.AbsolutePath!="/"||origin.UserInfo.Length!=0||origin.Query.Length!=0||origin.Fragment.Length!=0)throw new ArgumentException("Local execution requires a loopback HTTP origin.");
   this.driver=driver??throw new ArgumentNullException(nameof(driver));this.attempt=attempt??throw new ArgumentNullException(nameof(attempt));this.transport=transport??throw new ArgumentNullException(nameof(transport));
   token=tokenProvider??throw new ArgumentNullException(nameof(tokenProvider));current=currentAttempt??throw new ArgumentNullException(nameof(currentAttempt));Origin=origin.GetLeftPart(UriPartial.Authority);
   delivery=new OrdinaryLocalExecutionDelivery(attempt,()=>!disposed&&!driver.IsRetired&&current());
   this.retainBeforeSend=retainBeforeSend;
  }
  void Active(){if(disposed)throw new ObjectDisposedException(nameof(OrdinaryLocalExecutionSession));if(driver.IsRetired||!current())throw new InvalidOperationException("ordinary_local_execution_stale_attempt");}
  public async Task<JObject> FlushAsync(CancellationToken publicationCancellation=default){
   Active();publicationCancellation.ThrowIfCancellationRequested();if(InFlight)throw new InvalidOperationException("ordinary_local_execution_flush_in_flight");InFlight=true;
   try{
    LastTransportResult=null;
    var batch=delivery.Pending;
    if(batch==null){
     var committed=driver.CaptureOrdinaryReplayInputs();var projection=committed.Inputs;var checkpoint=JObject.Parse(committed.StateJson);
     Need((string)projection["schema"]=="unity.ordinary-replay-input-projection.v2.20261106"&&(string)projection["accountId"]==attempt.AccountId&&(string)projection["runId"]==attempt.RunId&&projection["inputs"] is JArray);
     Need((string)projection["checkpointSha256"]==BasicCombatSimulation.Digest(committed.StateJson)&&(string)projection["configurationSha256"]==(string)checkpoint["configurationSha256"]);
     var inputs=(JArray)projection["inputs"];Need(inputs.Count>=copiedSequence&&inputs.Count-copiedSequence<=128);
     for(int i=checked((int)copiedSequence);i<inputs.Count;i++){
      var row=inputs[i] as JObject;BDirectAttackInput.Fields(row,"sequence","tick","command");Need(BDirectAttackInput.Nat(row,"sequence",1,10000)==i+1);
      delivery.Queue(BDirectAttackInput.Nat(row,"tick",0,OrdinaryNodeStart.SafeMaximum),row["command"] as JObject);copiedSequence++;
     }
     capturedRevision=committed.Revision;
     if(opened&&delivery.QueuedCount==0)return LastReceipt;
     batch=delivery.Prepare(Guid.NewGuid().ToString("N"),checkpoint);
    }
    Active();publicationCancellation.ThrowIfCancellationRequested();var accessToken=token()??throw new InvalidOperationException("Local execution access token unavailable.");
    var request=new RunApiHttpRequest("POST",EndpointPath,batch.BodyJson,new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader("Idempotency-Key",batch.Key)});
    retainBeforeSend?.Invoke(batch);
    var response=await transport.SendAsync(request,accessToken);
    Active();publicationCancellation.ThrowIfCancellationRequested();LastTransportResult=response??throw new InvalidOperationException("ordinary_local_execution_transport_missing");
    if(response.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded||response.Response?.StatusCode!=200)return null;
    var accepted=JObject.Parse(response.Response.BodyJson);BDirectAttackInput.Fields(accepted,"accepted","version","requestId","evidenceRef","result");
    Need(accepted["accepted"]?.Type==JTokenType.Boolean&&(bool)accepted["accepted"]&&(string)accepted["version"]=="ordinary.local-execution-receipt.v1.20261106"&&(string)accepted["requestId"]==batch.Key&&(string)accepted["evidenceRef"]=="local:"+batch.Key&&accepted["result"] is JObject);
    if(delivery.Accept(batch.Key,(JObject)accepted["result"])){receipt=(JObject)accepted.DeepClone();opened=true;}
    return LastReceipt;
   }finally{InFlight=false;}
  }
  public void Dispose(){if(disposed)return;disposed=true;delivery.Retire();}
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_local_execution_session_conflict");}
 }
}
