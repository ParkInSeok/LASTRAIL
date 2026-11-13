using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using LastRail.Auth;
using LastRail.Battle;
namespace LastRail.Run {
 public sealed class BNamedOperatorSourceHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BNamedOperatorSourceHttpClient(IGameApiAuthenticatedHttpTransport value){transport=value??throw new ArgumentNullException(nameof(value));}
  public async Task<BNamedOperatorSource> CaptureAsync(string[] operatorIds,long formationRevision,long loadoutRevision,GameApiAccessToken token){
   var response=await BNamedSourceHttpBoundary.Fetch(transport,operatorIds,formationRevision,loadoutRevision,token,"/v1/combat/b-named-operator-source","X-B-Named-Operator-Version",BNamedOperatorSource.Version,512*1024);
   var packet=response.packet;string hash=response.hash;
   var captured=BNamedOperatorSource.Read(packet,packet,hash);
   if((long)packet["formationRevision"]!=formationRevision||(long)packet["loadoutRevision"]!=loadoutRevision||!operatorIds.OrderBy(id=>id).SequenceEqual(((JArray)packet["actors"]).Select(a=>(string)a["operatorId"]).OrderBy(id=>id)))throw new ArgumentException("b_named_http_selection_conflict");
   return captured;
  }
 }
 internal static class BNamedSourceHttpBoundary {
  internal static async Task<(JObject packet,string hash)> Fetch(IGameApiAuthenticatedHttpTransport transport,string[] ids,long formation,long loadout,GameApiAccessToken token,string endpoint,string header,string version,int maximumBytes,string expectedEffectHash=null){
   if(ids==null||ids.Length<1||ids.Length>2||ids.Distinct().Count()!=ids.Length||ids.Any(id=>id!="op_kyle"&&id!="op_sable")||formation<1||loadout<1||formation>9007199254740991||loadout>9007199254740991||token==null)throw new ArgumentException("b_named_request_invalid");
   var body=new JObject{["operatorIds"]=new JArray(ids),["expectedFormationRevision"]=formation,["expectedLoadoutRevision"]=loadout};
   if(expectedEffectHash!=null){if(!System.Text.RegularExpressions.Regex.IsMatch(expectedEffectHash,"^[a-f0-9]{64}$"))throw new ArgumentException("b_named_effect_hash_invalid");body["expectedEffectSourceHash"]=expectedEffectHash;}
   var result=await transport.SendAsync(new RunApiHttpRequest("POST",endpoint,body.ToString(Newtonsoft.Json.Formatting.None),new[]{new RunApiHttpHeader("Content-Type","application/json"),new RunApiHttpHeader(header,version)}),token);
   if(result.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded)throw new InvalidOperationException("b_named_http:"+result.Outcome+":"+result.ErrorCode);
   if(result.Response.StatusCode!=200)throw new InvalidOperationException("b_named_http_status:"+result.Response.StatusCode+":"+RunApiErrorJsonAdapter.OptionalString(result.Response.BodyJson,"errorCode"));
   if(System.Text.Encoding.UTF8.GetByteCount(result.Response.BodyJson)>maximumBytes)throw new ArgumentException("b_named_http_capacity");
   var packet=JObject.Parse(result.Response.BodyJson,new Newtonsoft.Json.Linq.JsonLoadSettings{DuplicatePropertyNameHandling=Newtonsoft.Json.Linq.DuplicatePropertyNameHandling.Error});
   return (packet,result.Response.Header("ETag")?.Trim('"'));
  }
 }
}
