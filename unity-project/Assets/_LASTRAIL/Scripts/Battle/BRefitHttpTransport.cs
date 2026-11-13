using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
namespace LastRail.Battle {
 public sealed class BRefitPrivateIntake : IDisposable {
  string authorization;bool disposed;public string Origin {get;}public string AccountId {get;}public DateTimeOffset ExpiresAt {get;}
  public override string ToString()=>"Private B refit intake";
  public BRefitPrivateIntake(JObject x){
   BuffWire.Fields(x,"origin","accountId","accessExpiresAt","headers");var origin=BuffWire.Text(x["origin"]);BuffWire.Need(Uri.TryCreate(origin,UriKind.Absolute,out var uri)&&uri.Scheme=="http"&&uri.Host=="127.0.0.1"&&uri.Port>0&&origin=="http://127.0.0.1:"+uri.Port.ToString(CultureInfo.InvariantCulture));Origin=origin;AccountId=BuffWire.Text(x["accountId"]);
   long expiry=BuffWire.Number(x["accessExpiresAt"],0,253402300799999L);ExpiresAt=DateTimeOffset.FromUnixTimeMilliseconds(expiry);BuffWire.Need(ExpiresAt>DateTimeOffset.UtcNow);
   var h=BuffWire.Object(x["headers"]);BuffWire.Fields(h,"Authorization","X-Combat-B-Refit-Version");BuffWire.Need((string)h["X-Combat-B-Refit-Version"]==BRefitRequest.Version&&h["Authorization"]?.Type==JTokenType.String&&((string)h["Authorization"]).Length<=8192&&!((string)h["Authorization"]).Contains("\r")&&!((string)h["Authorization"]).Contains("\n")&&Regex.IsMatch((string)h["Authorization"],@"^Bearer [A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$"));authorization=(string)h["Authorization"];
  }
  internal void Authorize(UnityWebRequest web){if(disposed)throw new ObjectDisposedException(nameof(BRefitPrivateIntake));if(DateTimeOffset.UtcNow>=ExpiresAt)throw new InvalidOperationException("refit_auth_expired");web.SetRequestHeader("Authorization",authorization);}
  public void Dispose(){disposed=true;authorization=null;}
 }
 public sealed class BRefitHttpTransport : IDisposable {
  readonly OwnedUnityJsonExchange exchange;public string AccountId {get;}public int InFlightCount=>exchange.InFlightCount;
  public BRefitHttpTransport(BRefitPrivateIntake owned){if(owned==null)throw new ArgumentNullException(nameof(owned));AccountId=owned.AccountId;exchange=new OwnedUnityJsonExchange(owned.Origin,"X-Combat-B-Refit-Version",owned.Authorize,owned);}
  public async Task<BRefitHttpResponse> SendAsync(BRefitRequest request,CancellationToken cancellation=default){
   if(request==null)throw new ArgumentNullException(nameof(request));if(request.Method=="POST"&&request.AccountId!=AccountId)throw new InvalidOperationException("refit_foreign_request");var h=new Dictionary<string,string>{{"X-Combat-B-Refit-Version",BRefitRequest.Version}};if(request.Method=="POST"){h.Add("Content-Type","application/json");h.Add("Idempotency-Key",request.Key);}var response=await exchange.SendAsync(request.Method,BRefitRequest.Path,h,request.Body,cancellation);return new BRefitHttpResponse(response.Status,response.CacheControl,response.Version,response.Body);
  }
  public void Dispose()=>exchange.Dispose();
 }
 public sealed class BRefitConnection : IDisposable {
  readonly BRefitController controller;readonly BRefitHttpTransport transport;readonly CancellationTokenSource stop=new CancellationTokenSource();readonly Action<BRefitRequest> binding;bool disposed;public Task LastExchange{get;private set;}=Task.CompletedTask;
  public BRefitConnection(BRefitController borrowed,BRefitHttpTransport owned,Action updated=null){controller=borrowed??throw new ArgumentNullException(nameof(borrowed));transport=owned??throw new ArgumentNullException(nameof(owned));controller.RequireAccount(transport.AccountId);binding=request=>LastExchange=Exchange(request);controller.Bind(binding,updated);}
  async Task Exchange(BRefitRequest request){try{var response=await transport.SendAsync(request,stop.Token);if(!disposed&&!stop.IsCancellationRequested)controller.AcceptResponse(request,response);}catch{if(!disposed)controller.TransportFailed(request);throw;}}
  public void Dispose(){if(disposed)return;disposed=true;controller.Unbind(binding);stop.Cancel();transport.Dispose();stop.Dispose();}
 }
}