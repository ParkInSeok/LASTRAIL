using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 // Owns authenticated definition publication and waits; transport/session remain borrowed.
 internal sealed class ImmutableCombatDefinitionClient<T>:IDisposable where T:class {
  readonly IGameApiAuthenticatedHttpTransport transport;readonly Func<GameApiAuthSessionSnapshot> session;readonly Func<long> generation;
  readonly string endpoint,version,approvedHash,errorPrefix,ownerName;readonly Func<JObject,string,T> read;readonly Func<string,string,T> readJson;
  T snapshot;GameApiAuthSessionSnapshot publishedOwner;long publishedGeneration,request;CancellationTokenSource activeWait;bool disposed;
  public event Action Changed;public string Status {get;private set;}="EMPTY";public string ErrorCode {get;private set;}
  bool Current(GameApiAuthSessionSnapshot owner,long stamp)=>!disposed&&owner!=null&&owner.AccountStatus=="ACTIVE"&&ReferenceEquals(owner,session())&&stamp==generation()&&owner.AccessTokenExpiresAt>DateTime.UtcNow;
  public T Snapshot=>Current(publishedOwner,publishedGeneration)?snapshot:null;
  internal ImmutableCombatDefinitionClient(IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<long> generation,string endpoint,string version,string approvedHash,string errorPrefix,Func<JObject,string,T> read,Func<string,string,T> readJson,string ownerName){this.endpoint=endpoint;this.version=version;this.approvedHash=approvedHash;this.errorPrefix=errorPrefix;this.read=read;this.readJson=readJson;this.ownerName=ownerName;this.transport=transport??throw new ArgumentNullException(nameof(transport));this.session=session??throw new ArgumentNullException(nameof(session));this.generation=generation??throw new ArgumentNullException(nameof(generation));}
  public void Invalidate(){request++;activeWait?.Cancel();snapshot=null;publishedOwner=null;Status="EMPTY";ErrorCode=null;if(!disposed)Changed?.Invoke();}
  void Accept(T value,GameApiAuthSessionSnapshot owner,long stamp){snapshot=value;publishedOwner=owner;publishedGeneration=stamp;Status="READY";ErrorCode=null;}
  public void RestoreVerified(JObject cache,string independentlyExpectedPacketHash){if(disposed)throw new ObjectDisposedException(ownerName);var owner=session();long stamp=generation();if(!Current(owner,stamp))throw new InvalidOperationException("auth_session_invalid");var verified=read(cache,independentlyExpectedPacketHash);request++;activeWait?.Cancel();Accept(verified,owner,stamp);Changed?.Invoke();}
  public async Task RefreshAsync(string independentlyExpectedPacketHash,string ifMatch=null,CancellationToken stop=default){
   if(disposed)throw new ObjectDisposedException(ownerName);Need(Hash(new JValue(independentlyExpectedPacketHash))==approvedHash);if(ifMatch!=null)Hash(new JValue(ifMatch));var owner=session();long stamp=generation(),id=++request;activeWait?.Cancel();if(!Current(owner,stamp)){snapshot=null;publishedOwner=null;Status="AUTH_REQUIRED";ErrorCode="auth_session_invalid";Changed?.Invoke();return;}if(publishedOwner!=null&&!Current(publishedOwner,publishedGeneration)){snapshot=null;publishedOwner=null;}using var wait=CancellationTokenSource.CreateLinkedTokenSource(stop);activeWait=wait;Status="LOADING";ErrorCode=null;
   try{Changed?.Invoke();wait.Token.ThrowIfCancellationRequested();var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("X-Combat-B-Scout-Gunner-Version",version)};if(ifMatch!=null)headers.Add(new RunApiHttpHeader("If-Match",ifMatch));var response=await GameApiBorrowedTaskAwait.Wait(transport.SendAsync(new RunApiHttpRequest("GET",endpoint,null,headers),owner.AccessToken),wait.Token);if(!Current(owner,stamp)||request!=id)return;wait.Token.ThrowIfCancellationRequested();if(response.Outcome==GameApiAuthenticatedHttpOutcome.AuthRequired){snapshot=null;publishedOwner=null;Status="AUTH_REQUIRED";ErrorCode=response.ErrorCode;return;}if(response.Outcome==GameApiAuthenticatedHttpOutcome.NetworkError){Status="NETWORK_ERROR";ErrorCode="network_error";return;}var r=response.Response;if(r.StatusCode!=200){ErrorCode=RunApiErrorJsonAdapter.OptionalString(r.BodyJson,"errorCode")??errorPrefix+"_rejected";Status=ErrorCode==errorPrefix+"_stale"?"STALE":"REJECTED";return;}Need(r.Header("Cache-Control")=="no-store"&&r.Header("ETag")==independentlyExpectedPacketHash);Accept(readJson(r.BodyJson,independentlyExpectedPacketHash),owner,stamp);
   }catch(OperationCanceledException){if(Current(owner,stamp)&&id==request){Status="CANCELLED";ErrorCode="cancelled";}}catch(ArgumentException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode=errorPrefix+"_source_conflict";}}catch(Newtonsoft.Json.JsonException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode=errorPrefix+"_source_invalid";}}catch(FormatException){if(Current(owner,stamp)&&id==request){Status="CONTRACT_MISMATCH";ErrorCode=errorPrefix+"_source_invalid";}}catch{if(Current(owner,stamp)&&id==request){Status="FAILED";ErrorCode="definition_metadata_read_failed";}throw;}
   finally{if(ReferenceEquals(activeWait,wait))activeWait=null;if(Current(owner,stamp)&&id==request)Changed?.Invoke();}
  }
  public void Dispose(){if(disposed)return;disposed=true;Invalidate();Changed=null;}
 }
}
