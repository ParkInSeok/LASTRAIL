using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using LastRail.Auth;
using LastRail.Battle;
namespace LastRail.Run {
 public sealed class BNamedEffectiveSourceHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BNamedEffectiveSourceHttpClient(IGameApiAuthenticatedHttpTransport value){transport=value??throw new ArgumentNullException(nameof(value));}
  public async Task<BNamedEffectiveSource> CaptureAsync(string[] ids,long formationRevision,long loadoutRevision,GameApiAccessToken token,string expectedAccount,string expectedEffectSourceHash=null){
   if(string.IsNullOrWhiteSpace(expectedAccount))throw new ArgumentException("b_effective_http_account_missing");
   var response=await BNamedSourceHttpBoundary.Fetch(transport,ids,formationRevision,loadoutRevision,token,"/v1/combat/b-named-effective-source","X-B-Named-Effective-Version",BNamedEffectiveSource.Version,768*1024,expectedEffectSourceHash);
   var source=BNamedEffectiveSource.Read(response.packet,response.packet,response.hash,expectedAccount,expectedEffectSourceHash);var named=source.Capture()["namedSource"];
   if((long)named["formationRevision"]!=formationRevision||(long)named["loadoutRevision"]!=loadoutRevision||!ids.OrderBy(id=>id).SequenceEqual(((JArray)named["actors"]).Select(a=>(string)a["actorId"]).OrderBy(id=>id)))throw new ArgumentException("b_effective_http_selection_conflict");
   return source;
  }
 }
}
