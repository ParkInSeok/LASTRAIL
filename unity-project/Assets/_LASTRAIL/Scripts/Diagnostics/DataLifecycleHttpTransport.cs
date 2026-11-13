using System;
using System.Threading.Tasks;
using LastRail.Run;
namespace LastRail.Diagnostics {
 // Counts actual transport attempts. Logical requests/cache reuse belong to owners.
 public sealed class DataLifecycleHttpTransport:IRunApiHttpTransport {
  readonly IRunApiHttpTransport inner;public DataLifecycleTrace Trace {get;}
  public DataLifecycleHttpTransport(IRunApiHttpTransport inner,DataLifecycleTrace trace){this.inner=inner??throw new ArgumentNullException(nameof(inner));Trace=trace??throw new ArgumentNullException(nameof(trace));}
  public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){
   var key=Key(request.EndpointPath);Trace.Record(key,LifecycleEvent.Sent,LifecycleReason.Response);
   try{var response=await inner.SendAsync(request);Trace.Record(key,LifecycleEvent.Received,response?.IsNetworkError!=false?LifecycleReason.Failure:LifecycleReason.Response);return response;}
   catch{Trace.Record(key,LifecycleEvent.Received,LifecycleReason.Failure);throw;}
  }
  static LifecycleData Key(string path){
   switch(path){case "/v1/profile":return LifecycleData.Profile;case "/v1/story-progress":return LifecycleData.Story;case "/v1/account/state":return LifecycleData.Account;case "/v1/wallet/crystals":return LifecycleData.Wallet;case "/v1/growth/config":return LifecycleData.Growth;case "/v1/trains/loadout":return LifecycleData.Train;case "/v1/formation/ultimate":return LifecycleData.Ultimate;}
   if(path.StartsWith("/v1/gacha/banners",StringComparison.Ordinal))return LifecycleData.Banners;
   if(path.StartsWith("/v1/runs/",StringComparison.Ordinal)){if(path.EndsWith("/select",StringComparison.Ordinal)||path.EndsWith("/reroll",StringComparison.Ordinal))return LifecycleData.RunSelect;if(path.EndsWith("/claim",StringComparison.Ordinal))return LifecycleData.RunClaim;return LifecycleData.RunResult;}
   return LifecycleData.Other;
  }
 }
}
