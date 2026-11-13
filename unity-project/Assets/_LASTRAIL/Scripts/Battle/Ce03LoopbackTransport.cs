using System;
using System.Text;
using System.Threading.Tasks;
using LastRail.Run;
using UnityEngine.Networking;
namespace LastRail.Battle {
public sealed class Ce03LoopbackTransport:IRunApiHttpTransport {
 readonly Uri origin;
 public Ce03LoopbackTransport(Uri value){if(value==null||value.Scheme!="http"||value.Host!="127.0.0.1"||value.AbsolutePath!="/"||value.UserInfo!=""||value.Query!=""||value.Fragment!="")throw new ArgumentException();origin=value;}
 public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){
  if(request==null||!(request.EndpointPath.StartsWith("/__local/ce03/",StringComparison.Ordinal)||request.EndpointPath=="/v1/operations/preview"||request.EndpointPath.StartsWith("/v1/runs/",StringComparison.Ordinal)||request.Method=="GET"&&Array.IndexOf(new[]{"/v1/account/state","/v1/wallet/crystals","/v1/growth/config","/v1/trains/loadout","/v1/formation/ultimate"},request.EndpointPath)>=0))throw new ArgumentException("Local endpoint required.");
  var uri=new Uri(origin,request.EndpointPath);if(uri.GetLeftPart(UriPartial.Authority)!=origin.GetLeftPart(UriPartial.Authority))throw new ArgumentException();
  var done=new TaskCompletionSource<RunApiHttpResponse>();var wire=new UnityWebRequest(uri.AbsoluteUri,request.Method);wire.downloadHandler=new DownloadHandlerBuffer();wire.redirectLimit=0;wire.timeout=20;
  if(request.BodyJson!=null){wire.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.BodyJson));wire.SetRequestHeader("Content-Type","application/json");}foreach(var header in request.Headers)wire.SetRequestHeader(header.Name,header.Value);
  try{var operation=wire.SendWebRequest();operation.completed+=_=>{try{done.TrySetResult(wire.result==UnityWebRequest.Result.ConnectionError||wire.responseCode==0?RunApiHttpResponse.NetworkError("Local connection failed."):RunApiHttpResponse.Success((int)wire.responseCode,wire.downloadHandler.text));}catch(Exception e){done.TrySetException(e);}finally{wire.Dispose();}};}catch{wire.Dispose();throw;}return done.Task;
 }
}
}
