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
 internal sealed class UnityJsonExchangeResponse {
  internal int Status {get;}internal string CacheControl {get;}internal string Version {get;}internal JObject Body {get;}
  internal UnityJsonExchangeResponse(int status,string cache,string version,JObject body){Status=status;CacheControl=cache;Version=version;Body=body;}
 }
 // Shared request lifetime only. Each protocol retains its own private auth and domain validation.
 internal sealed class OwnedUnityJsonExchange : IDisposable {
  readonly string origin,versionHeader;
  readonly Action<UnityWebRequest> authorize;readonly IDisposable authOwner;
  readonly int thread=Thread.CurrentThread.ManagedThreadId;
  readonly SynchronizationContext context=SynchronizationContext.Current;
  readonly HashSet<UnityWebRequest> requests=new HashSet<UnityWebRequest>();bool disposed;
  internal int InFlightCount=>requests.Count;
  internal OwnedUnityJsonExchange(string origin,string responseVersionHeader,Action<UnityWebRequest> authorize,IDisposable ownedAuth){this.origin=origin;versionHeader=responseVersionHeader;this.authorize=authorize;authOwner=ownedAuth;}
  void MainThread(){if(Thread.CurrentThread.ManagedThreadId!=thread)throw new InvalidOperationException("local_http_main_thread_required");}
  internal async Task<UnityJsonExchangeResponse> SendAsync(string method,string path,IReadOnlyDictionary<string,string> headers,JObject body,CancellationToken cancellation){
   MainThread();if(disposed)throw new ObjectDisposedException(nameof(OwnedUnityJsonExchange));cancellation.ThrowIfCancellationRequested();
   string json=body?.ToString(Formatting.None);if(json!=null&&Encoding.UTF8.GetByteCount(json)>18000)throw new ArgumentException("local_http_request_limit");
   using(var web=new UnityWebRequest(origin+path,method)){
    web.redirectLimit=0;web.timeout=5;web.downloadHandler=new DownloadHandlerBuffer();if(json!=null)web.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));foreach(var h in headers)web.SetRequestHeader(h.Key,h.Value);authorize(web);
    requests.Add(web);UnityWebRequestAsyncOperation operation=null;Action<AsyncOperation> completed=null;CancellationTokenRegistration registration=default;
    try{
     operation=web.SendWebRequest();var ready=new TaskCompletionSource<bool>();completed=_=>ready.TrySetResult(true);operation.completed+=completed;if(operation.isDone)ready.TrySetResult(true);
     registration=cancellation.Register(()=>{if(Thread.CurrentThread.ManagedThreadId==thread){if(requests.Contains(web))web.Abort();}else if(context!=null)context.Post(_=>{if(requests.Contains(web))web.Abort();},null);});
     await ready.Task;cancellation.ThrowIfCancellationRequested();if(disposed)throw new ObjectDisposedException(nameof(OwnedUnityJsonExchange));
     if(web.result==UnityWebRequest.Result.ConnectionError||web.result==UnityWebRequest.Result.DataProcessingError)throw new InvalidOperationException("local_http_transport_failed");
     string text=web.downloadHandler.text;if(string.IsNullOrEmpty(text)||text.Length>16777216)throw new InvalidOperationException("local_http_response_limit");JObject response;
     try{response=JObject.Parse(text,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}catch(JsonException){throw new InvalidOperationException("local_http_response_invalid");}
     return new UnityJsonExchangeResponse((int)web.responseCode,web.GetResponseHeader("Cache-Control"),web.GetResponseHeader(versionHeader),response);
    }finally{registration.Dispose();if(operation!=null&&completed!=null)operation.completed-=completed;requests.Remove(web);}
   }
  }
  public void Dispose(){MainThread();if(disposed)return;disposed=true;foreach(var web in requests)web.Abort();authOwner.Dispose();}
 }
}
