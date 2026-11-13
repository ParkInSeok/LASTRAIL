using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LastRail.Auth;
namespace LastRail.Run {
 public sealed class BNativeNodeSourceHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BNativeNodeSourceHttpClient(IGameApiAuthenticatedHttpTransport value){transport=value??throw new ArgumentNullException(nameof(value));}
  public async Task<BNativeNodeSourceSnapshot> ReadAsync(string account,string run,GameApiAccessToken token,string expectedHash=null){
   if(string.IsNullOrEmpty(run)||run.Length>200)throw new ArgumentException("b_native_run_invalid");var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("X-B-Native-Node-Version",BNativeNodeSourceSnapshot.Version)};if(expectedHash!=null)headers.Add(new RunApiHttpHeader("If-Match",expectedHash));
   var r=await transport.SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(run)+"/b-native-node-source",null,headers),token);
   if(r.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded)throw new InvalidOperationException("b_native_http:"+r.Outcome+":"+r.ErrorCode);if(r.Response.StatusCode!=200)throw new InvalidOperationException("b_native_http_status:"+r.Response.StatusCode+":"+RunApiErrorJsonAdapter.OptionalString(r.Response.BodyJson,"errorCode"));
   return BNativeNodeSourceSnapshot.Read(r.Response.BodyJson,r.Response.Header("ETag")?.Trim('"'),account,run);
  }
 }
}
