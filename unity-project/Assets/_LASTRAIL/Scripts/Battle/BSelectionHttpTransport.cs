using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
namespace LastRail.Battle {
 // Selection authentication is not converted into world-authority headers.
 public sealed class BSelectionPrivateIntake : IDisposable {
  string authorization;bool disposed;
  public string Origin {get;}public string AccountId {get;}public DateTimeOffset ExpiresAt {get;}
  public override string ToString()=>"Private B selection intake";
  public BSelectionPrivateIntake(JObject input){
   BSelectionContract.Fields(input,"origin","accountId","accessExpiresAt","headers");string origin=BSelectionContract.Text(input,"origin");
   BSelectionContract.Check(Uri.TryCreate(origin,UriKind.Absolute,out var uri)&&uri.Scheme=="http"&&uri.Host=="127.0.0.1"&&uri.Port>0&&origin=="http://127.0.0.1:"+uri.Port.ToString(CultureInfo.InvariantCulture));Origin=origin;AccountId=BSelectionContract.Text(input,"accountId");
   long expiry=BSelectionContract.Number(input,"accessExpiresAt");BSelectionContract.Check(expiry<=253402300799999L);ExpiresAt=DateTimeOffset.FromUnixTimeMilliseconds(expiry);BSelectionContract.Check(ExpiresAt>DateTimeOffset.UtcNow);
   var h=input["headers"] as JObject;BSelectionContract.Fields(h,"Authorization","X-Combat-B-Selection-Version");BSelectionContract.Check(BSelectionContract.Text(h,"X-Combat-B-Selection-Version")==BSelectionRequest.Version);
   var token=h["Authorization"];BSelectionContract.Check(token?.Type==JTokenType.String&&((string)token).Length<=8192&&Regex.IsMatch((string)token,@"^Bearer [A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$"));authorization=(string)token;
  }
  internal void Authorize(UnityWebRequest web){if(disposed)throw new ObjectDisposedException(nameof(BSelectionPrivateIntake));if(DateTimeOffset.UtcNow>=ExpiresAt)throw new InvalidOperationException("selection_auth_expired");web.SetRequestHeader("Authorization",authorization);}
  public void Dispose(){disposed=true;authorization=null;}
 }
 public sealed class BSelectionHttpResponse {
  readonly JObject body;public int Status {get;}public string Version {get;}public string CacheControl {get;}public JObject Body=>(JObject)body.DeepClone();
  public BSelectionHttpResponse(int status,string cache,string version,JObject input){if(input==null)throw new ArgumentNullException(nameof(input));Status=status;CacheControl=cache;Version=version;body=(JObject)input.DeepClone();}
 }
 public sealed class BSelectionHttpTransport : IDisposable {
  readonly OwnedUnityJsonExchange exchange;
  public int InFlightCount=>exchange.InFlightCount;
  public string AccountId {get;}
  public BSelectionHttpTransport(BSelectionPrivateIntake ownedIntake){
   if(ownedIntake==null)throw new ArgumentNullException(nameof(ownedIntake));AccountId=ownedIntake.AccountId;
   exchange=new OwnedUnityJsonExchange(ownedIntake.Origin,"X-Combat-B-Selection-Version",ownedIntake.Authorize,ownedIntake);
  }
  public async Task<BSelectionHttpResponse> SendAsync(BSelectionRequest request,CancellationToken cancellation=default){
   if(request==null)throw new ArgumentNullException(nameof(request));
   var headers=new Dictionary<string,string>{{"X-Combat-B-Selection-Version",BSelectionRequest.Version}};
   if(request.Method=="POST"){headers.Add("Content-Type","application/json");headers.Add("Idempotency-Key",request.Key);}
   var response=await exchange.SendAsync(request.Method,BSelectionRequest.Path,headers,request.Body,cancellation);
   return new BSelectionHttpResponse(response.Status,response.CacheControl,response.Version,response.Body);
  }
  public void Dispose()=>exchange.Dispose();
 }
 public sealed class BSelectionConnection : IDisposable {
  readonly BSelectionController controller;readonly BSelectionHttpTransport transport;readonly CancellationTokenSource stop=new CancellationTokenSource();readonly Action<BSelectionRequest> binding;bool disposed;
  public Task LastExchange {get;private set;}=Task.CompletedTask;
  public BSelectionConnection(BSelectionController borrowedController,BSelectionHttpTransport ownedTransport,Action changed=null){controller=borrowedController??throw new ArgumentNullException(nameof(borrowedController));transport=ownedTransport??throw new ArgumentNullException(nameof(ownedTransport));binding=request=>LastExchange=Exchange(request);controller.RequireAccount(transport.AccountId);controller.Bind(binding,changed);}
  async Task Exchange(BSelectionRequest request){try{var response=await transport.SendAsync(request,stop.Token);if(!disposed&&!stop.IsCancellationRequested)controller.AcceptResponse(request,response);}catch{if(!disposed)controller.TransportFailed(request);throw;}}
  public void Dispose(){if(disposed)return;disposed=true;controller.Unbind(binding);stop.Cancel();transport.Dispose();stop.Dispose();}
 }
}
