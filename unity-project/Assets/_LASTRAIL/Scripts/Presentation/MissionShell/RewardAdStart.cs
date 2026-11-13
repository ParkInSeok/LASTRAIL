using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell
{
 // Server-issued, one-attempt permission. Durable callers mark the attempt before sending.
 internal static class RewardAdStart
 {
  internal const string Version="ad-start-v1";
  internal static async Task<bool> AuthorizeAsync(IRunApiHttpTransport wire,GameApiAuthSessionSnapshot owner,string consumer,string intentJson,string key)
  {
   var intent=GameApiJson.ReadObject(intentJson);string id=OfflineAdIntent.IdText(intent,"intentId");
   var request=new RunApiHttpRequest("POST","/v1/ad-opportunities/"+consumer+"/"+id+"/start",null,new[]{new RunApiHttpHeader("Idempotency-Key",key)});
   var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(request,owner.AccessToken));
   if(response.IsNetworkError||response.StatusCode!=200)return false;
   var result=GameApiJson.ReadObject(response.BodyJson);
   if(!GameApiJson.Boolean(result,"accepted")||GameApiJson.String(result,"contractVersion")!=Version||GameApiJson.String(result,"consumer")!=consumer||GameApiJson.String(result,"intentId")!=id)throw new FormatException("Ad start binding");
   long revision=RewardOpportunityContract.Number(result,"runtimePolicyRevision"),approved=RewardOpportunityContract.Number(result,"approvedAt"),expires=RewardOpportunityContract.Number(result,"expiresAt");
   if(revision<1||approved<RewardOpportunityContract.Number(intent,"createdAt")||expires!=RewardOpportunityContract.Number(intent,"expiresAt")||approved>=expires)throw new FormatException("Ad start clock");
   string status=GameApiJson.String(result,"startStatus");bool show=GameApiJson.Boolean(result,"mayShow");
   if(status=="ALREADY_APPROVED"&&!show)return false;
   if(status!="APPROVED"||!show)throw new FormatException("Ad start state");
   return expires>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
  }
 }
}
