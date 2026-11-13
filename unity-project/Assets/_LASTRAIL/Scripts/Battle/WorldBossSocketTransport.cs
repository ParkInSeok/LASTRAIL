using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace LastRail.Battle {
 // Credential-free response; the existing local client owns replica reconciliation.
 public sealed class WorldBossSocketResponse {
  public int Status {get;}
  public string CacheControl {get;}
  public string TransportVersion {get;}
  readonly JObject body;
  public JObject Body=>(JObject)body.DeepClone();
  internal WorldBossSocketResponse(int status,string cache,string version,JObject value) {
   Status=status;CacheControl=cache;TransportVersion=version;body=(JObject)value.DeepClone();
  }
 }
 // Unity main-thread owner. Abort, completion subscriptions and intake lifetime belong here.
 public sealed class WorldBossSocketTransport : IDisposable {
  readonly OwnedUnityJsonExchange exchange;readonly string contentVersion;
  public int InFlightCount=>exchange.InFlightCount;
  public WorldBossSocketTransport(WorldBossSocketPrivateIntake ownedIntake){
   if(ownedIntake==null)throw new ArgumentNullException(nameof(ownedIntake));contentVersion=ownedIntake.ContentVersion;
   exchange=new OwnedUnityJsonExchange(ownedIntake.Origin,"X-World-Authority-Transport",ownedIntake.Authorize,ownedIntake);
  }
  public async Task<WorldBossSocketResponse> SendAsync(WorldBossAuthorityRequest request,CancellationToken cancellation=default){
   if(request==null)throw new ArgumentNullException(nameof(request));
   if(request.Headers["X-World-Content-Version"]!=contentVersion)throw new ArgumentException("world_socket_content_mismatch");
   var response=await exchange.SendAsync(request.Method,request.Path,request.Headers,request.Body,cancellation);
   return new WorldBossSocketResponse(response.Status,response.CacheControl,response.Version,response.Body);
  }
  public void Dispose()=>exchange.Dispose();
 }
 // Borrows the existing domain client; owns its binding and the supplied transport.
 public sealed class WorldBossSocketConnection : IDisposable {
  readonly WorldBossAuthorityLocalClient client;readonly WorldBossSocketTransport transport;
  readonly CancellationTokenSource stop=new CancellationTokenSource();
  readonly Action<WorldBossAuthorityRequest> binding;
  bool disposed;
  public Task LastExchange {get;private set;}=Task.CompletedTask;
  public WorldBossSocketConnection(WorldBossAuthorityLocalClient borrowedClient,WorldBossSocketTransport ownedTransport,Action changed=null) {
   client=borrowedClient??throw new ArgumentNullException(nameof(borrowedClient));
   transport=ownedTransport??throw new ArgumentNullException(nameof(ownedTransport));
   binding=request=>LastExchange=Exchange(request);client.Bind(binding,changed);
  }
  async Task Exchange(WorldBossAuthorityRequest request) {
   var response=await transport.SendAsync(request,stop.Token);
   if(disposed||stop.IsCancellationRequested)return;
   if(response.Status==200) {
    if(request.Method=="GET")client.ApplyRead(response.Body,response.CacheControl,response.TransportVersion);
    else client.AcceptAcknowledgement(request,response.Body,response.CacheControl,response.TransportVersion);
   }else client.AcceptFailure(request,response.Status,response.Body,response.CacheControl,response.TransportVersion);
  }
  public void Dispose(){if(disposed)return;disposed=true;client.Unbind(binding);stop.Cancel();transport.Dispose();stop.Dispose();}
 }
}

