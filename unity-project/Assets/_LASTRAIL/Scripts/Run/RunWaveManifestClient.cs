using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Owns one metadata projection and request generations. Auth and HTTP transport are borrowed.
 public sealed class RunWaveManifestClient:IDisposable {
  readonly IGameApiAuthenticatedHttpTransport transport;readonly Func<GameApiAuthSessionSnapshot> session;readonly Func<long> generation;
  RunWaveManifestSnapshot snapshot;JObject frozen;GameApiAuthSessionSnapshot publishedOwner;long publishedGeneration,request;bool disposed;CancellationTokenSource activeWait;
  public event Action Changed;public string Status{get;private set;}="EMPTY";public string ErrorCode{get;private set;}
  public RunWaveManifestSnapshot Snapshot=>Current(publishedOwner,publishedGeneration)?snapshot:null;
  public RunWaveManifestClient(IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<long> generation){this.transport=transport??throw new ArgumentNullException(nameof(transport));this.session=session??throw new ArgumentNullException(nameof(session));this.generation=generation??throw new ArgumentNullException(nameof(generation));}
  bool Current(GameApiAuthSessionSnapshot owner,long stamp)=>!disposed&&owner!=null&&owner.AccountStatus=="ACTIVE"&&ReferenceEquals(owner,session())&&stamp==generation()&&owner.AccessTokenExpiresAt>DateTime.UtcNow;
  public void Invalidate(){request++;activeWait?.Cancel();snapshot=null;frozen=null;publishedOwner=null;Status="EMPTY";ErrorCode=null;if(!disposed)Changed?.Invoke();}
  void CheckFrozen(RunWaveManifestSnapshot value){if(frozen!=null&&(string)frozen["runId"]==value.RunId)RunWaveManifestReader.Need(JToken.DeepEquals(frozen,value.Frozen));}
  void Accept(RunWaveManifestSnapshot value,GameApiAuthSessionSnapshot owner,long stamp){CheckFrozen(value);frozen=value.Frozen;snapshot=value;publishedOwner=owner;publishedGeneration=stamp;Status="READY";ErrorCode=null;}
  public void RestoreVerified(JObject cache,JObject independentTrust){if(disposed)throw new ObjectDisposedException(nameof(RunWaveManifestClient));var owner=session();long stamp=generation();if(!Current(owner,stamp))throw new InvalidOperationException("auth_session_invalid");var t=RunWaveManifestReader.TrustedCopy(independentTrust);RunWaveManifestReader.Need((string)t["accountId"]==owner.AccountId);var verified=RunWaveManifestSnapshot.Read(cache,t);CheckFrozen(verified);request++;activeWait?.Cancel();Accept(verified,owner,stamp);Changed?.Invoke();}
  public async Task RefreshAsync(JObject independentTrust,string ifMatch=null,CancellationToken stop=default){
   if(disposed)throw new ObjectDisposedException(nameof(RunWaveManifestClient));var trust=RunWaveManifestReader.TrustedCopy(independentTrust);var owner=session();long stamp=generation();long id=++request;activeWait?.Cancel();ErrorCode=null;if(!Current(owner,stamp)||(string)trust["accountId"]!=owner.AccountId){snapshot=null;frozen=null;publishedOwner=null;Status="AUTH_REQUIRED";ErrorCode="auth_session_invalid";Changed?.Invoke();return;}if(publishedOwner!=null&&!Current(publishedOwner,publishedGeneration)){frozen=null;snapshot=null;publishedOwner=null;}if(snapshot!=null&&((string)trust["runId"]!=snapshot.RunId||(string)trust["startSnapshotHash"]!=(string)frozen["startSnapshotHash"]||!JToken.DeepEquals(trust["parentPinHash"],frozen["parentPinHash"])||!JToken.DeepEquals(trust["manifestHash"],frozen["manifestHash"])))snapshot=null;using var ownedWait=CancellationTokenSource.CreateLinkedTokenSource(stop);activeWait=ownedWait;stop=ownedWait.Token;Status="LOADING";
   try{
    Changed?.Invoke();
    stop.ThrowIfCancellationRequested();var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("X-Combat-B-Wave-Manifest-Version",RunWaveManifestSnapshot.Version)};if(ifMatch!=null){RunWaveManifestReader.Need(System.Text.RegularExpressions.Regex.IsMatch(ifMatch,"\\A[a-f0-9]{64}\\z"));headers.Add(new RunApiHttpHeader("If-Match",ifMatch));}
    var pending=transport.SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString((string)trust["runId"])+"/b-wave-manifest",null,headers),owner.AccessToken);var response=await GameApiBorrowedTaskAwait.Wait(pending,stop);if(!Current(owner,stamp)||id!=request)return;stop.ThrowIfCancellationRequested();
    if(response.Outcome==GameApiAuthenticatedHttpOutcome.AuthRequired){snapshot=null;frozen=null;publishedOwner=null;Status="AUTH_REQUIRED";ErrorCode=response.ErrorCode;return;}if(response.Outcome==GameApiAuthenticatedHttpOutcome.NetworkError){Status="NETWORK_ERROR";ErrorCode="network_error";return;}var r=response.Response;
    if(r.StatusCode!=200){ErrorCode=RunApiErrorJsonAdapter.OptionalString(r.BodyJson,"errorCode")??"combat_b_wave_rejected";Status=ErrorCode=="combat_b_wave_stale"?"STALE":"REJECTED";return;}
    RunWaveManifestReader.Need(r.Header("Cache-Control")=="no-store"&&r.Header("ETag")== (string)trust["packetHash"]);var verified=RunWaveManifestSnapshot.ReadJson(r.BodyJson,trust);Accept(verified,owner,stamp);
   }catch(OperationCanceledException){if(Current(owner,stamp)&&id==request){Status="CANCELLED";ErrorCode="cancelled";}}catch(ArgumentException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode="combat_b_wave_pinned_conflict";}}catch(Newtonsoft.Json.JsonException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode="combat_b_wave_packet_invalid";}}catch(FormatException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode="combat_b_wave_packet_invalid";}}
   catch{if(Current(owner,stamp)&&id==request){Status="FAILED";ErrorCode="wave_metadata_read_failed";}throw;}
   finally{if(ReferenceEquals(activeWait,ownedWait))activeWait=null;if(Current(owner,stamp)&&id==request)Changed?.Invoke();}
  }
  public void Dispose(){if(disposed)return;disposed=true;Invalidate();Changed=null;}
 }
}
