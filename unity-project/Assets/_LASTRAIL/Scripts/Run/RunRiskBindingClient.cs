using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Owns one metadata projection and request generations. Auth and HTTP transport are borrowed.
 public sealed class RunRiskBindingClient:IDisposable {
  readonly IGameApiAuthenticatedHttpTransport transport;readonly Func<GameApiAuthSessionSnapshot> session;readonly Func<long> generation;
  RunRiskBindingSnapshot snapshot;JObject frozen;GameApiAuthSessionSnapshot publishedOwner;long publishedGeneration,request;bool disposed;CancellationTokenSource activeWait;
  public event Action Changed;public string Status{get;private set;}="EMPTY";public string ErrorCode{get;private set;}
  public RunRiskBindingSnapshot Snapshot=>Current(publishedOwner,publishedGeneration)?snapshot:null;
  public RunRiskBindingClient(IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<long> generation){this.transport=transport??throw new ArgumentNullException(nameof(transport));this.session=session??throw new ArgumentNullException(nameof(session));this.generation=generation??throw new ArgumentNullException(nameof(generation));}
  bool Current(GameApiAuthSessionSnapshot owner,long stamp)=>!disposed&&owner!=null&&owner.AccountStatus=="ACTIVE"&&ReferenceEquals(owner,session())&&stamp==generation()&&owner.AccessTokenExpiresAt>DateTime.UtcNow;
  public void Invalidate(){request++;activeWait?.Cancel();snapshot=null;frozen=null;publishedOwner=null;Status="EMPTY";ErrorCode=null;if(!disposed)Changed?.Invoke();}
  void CheckFrozen(RunRiskBindingSnapshot value){if(frozen!=null&&(string)frozen["runId"]==value.RunId)RunRiskBindingSnapshot.Need(JToken.DeepEquals(frozen,value.Frozen));}
  void Accept(RunRiskBindingSnapshot value,GameApiAuthSessionSnapshot owner,long stamp){CheckFrozen(value);frozen=value.Frozen;snapshot=value;publishedOwner=owner;publishedGeneration=stamp;Status="READY";ErrorCode=null;}
  public void RestoreVerified(JObject cache,JObject independentTrust){if(disposed)throw new ObjectDisposedException(nameof(RunRiskBindingClient));var owner=session();long stamp=generation();if(!Current(owner,stamp))throw new InvalidOperationException("auth_session_invalid");var t=RunRiskBindingSnapshot.TrustedCopy(independentTrust);RunRiskBindingSnapshot.Need((string)t["accountId"]==owner.AccountId);var verified=RunRiskBindingSnapshot.Read(cache,t);CheckFrozen(verified);request++;activeWait?.Cancel();Accept(verified,owner,stamp);Changed?.Invoke();}
  public async Task RefreshAsync(JObject independentTrust,string ifMatch=null,CancellationToken stop=default){
   if(disposed)throw new ObjectDisposedException(nameof(RunRiskBindingClient));var trust=RunRiskBindingSnapshot.TrustedCopy(independentTrust);var owner=session();long stamp=generation();long id=++request;activeWait?.Cancel();snapshot=null;ErrorCode=null;if(!Current(owner,stamp)||(string)trust["accountId"]!=owner.AccountId){Status="AUTH_REQUIRED";ErrorCode="auth_session_invalid";Changed?.Invoke();return;}if(publishedOwner!=null&&!Current(publishedOwner,publishedGeneration))frozen=null;using var ownedWait=CancellationTokenSource.CreateLinkedTokenSource(stop);activeWait=ownedWait;stop=ownedWait.Token;Status="LOADING";
   try{
    Changed?.Invoke();
    stop.ThrowIfCancellationRequested();var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("X-Combat-B-Risk-Binding-Version",RunRiskBindingSnapshot.Version)};if(ifMatch!=null){RunRiskBindingSnapshot.Need(System.Text.RegularExpressions.Regex.IsMatch(ifMatch,"\\A[a-f0-9]{64}\\z"));headers.Add(new RunApiHttpHeader("If-Match",ifMatch));}
    var pending=transport.SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString((string)trust["runId"])+"/operation-risk-binding",null,headers),owner.AccessToken);var response=await GameApiBorrowedTaskAwait.Wait(pending,stop);if(!Current(owner,stamp)||id!=request)return;stop.ThrowIfCancellationRequested();
    if(response.Outcome==GameApiAuthenticatedHttpOutcome.AuthRequired){Status="AUTH_REQUIRED";ErrorCode=response.ErrorCode;return;}if(response.Outcome==GameApiAuthenticatedHttpOutcome.NetworkError){Status="NETWORK_ERROR";ErrorCode="network_error";return;}var r=response.Response;
    if(r.StatusCode!=200){ErrorCode=RunApiErrorJsonAdapter.OptionalString(r.BodyJson,"errorCode")??"combat_b_risk_run_rejected";Status=ErrorCode=="combat_b_risk_run_stale"?"STALE":"REJECTED";return;}
    RunRiskBindingSnapshot.Need(r.Header("Cache-Control")=="no-store"&&r.Header("ETag")== (string)trust["packetHash"]);var verified=RunRiskBindingSnapshot.Read(GameApiJson.ReadObject(r.BodyJson),trust);Accept(verified,owner,stamp);
   }catch(OperationCanceledException){if(Current(owner,stamp)&&id==request){Status="CANCELLED";ErrorCode="cancelled";}}catch(ArgumentException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode="combat_b_risk_run_pinned_conflict";}}catch(Newtonsoft.Json.JsonException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode="combat_b_risk_run_packet_invalid";}}catch(FormatException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode="combat_b_risk_run_packet_invalid";}}
   catch{if(Current(owner,stamp)&&id==request){Status="FAILED";ErrorCode="risk_metadata_read_failed";}throw;}
   finally{if(ReferenceEquals(activeWait,ownedWait))activeWait=null;if(Current(owner,stamp)&&id==request)Changed?.Invoke();}
  }
  public void Dispose(){if(disposed)return;disposed=true;Invalidate();Changed=null;}
 }
}
