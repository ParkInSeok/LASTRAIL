using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
 // TEST_ONLY negotiation/clock adapter for legacy behavioral fixtures. No runtime authorization.
 internal sealed class AdStartTestProtocol
 {
  readonly long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
  JObject intent;string createKey;
  internal bool FreshAdvertising;
  internal async Task<RunApiHttpResponse> Send(RunApiHttpRequest request,Func<RunApiHttpRequest,Task<RunApiHttpResponse>> legacy)
  {
   if(request.EndpointPath=="/v1/runtime-policy")return RunApiHttpResponse.Success(200,AdRuntimePolicyTests.Policy(true));
   if(request.EndpointPath=="/v1/ad-skip")return RunApiHttpResponse.Success(200,AdSkipUseTests.Sample("03-configured-unowned"));
   if(request.Method=="POST"&&request.EndpointPath.EndsWith("/ad-intents"))
   {
    Assert.That(request.Headers.Single(h=>h.Name=="X-Ad-Contract").Value,Is.EqualTo("ad-start-v1"));
    string key=request.Headers.Single(h=>h.Name=="Idempotency-Key").Value;if(createKey!=null)Assert.That(key,Is.EqualTo(createKey));createKey=key;
   }
   if(request.EndpointPath.EndsWith("/start"))
   {
    Assert.That(intent,Is.Not.Null);Assert.That(request.Method,Is.EqualTo("POST"));Assert.That(request.BodyJson,Is.Null);
    Assert.That(request.Headers.Single(h=>h.Name=="Idempotency-Key").Value,Is.EqualTo(createKey));
    string consumer=request.EndpointPath.Contains("/COMBAT/")?"COMBAT":"OFFLINE";
    return RunApiHttpResponse.Success(200,new JObject{["accepted"]=true,["contractVersion"]="ad-start-v1",["consumer"]=consumer,["intentId"]=intent["intentId"],["startStatus"]="APPROVED",["mayShow"]=true,["runtimePolicyRevision"]=1,["approvedAt"]=FreshAdvertising?now:(long)intent["createdAt"],["expiresAt"]=intent["expiresAt"]}.ToString());
   }
   var response=await legacy(request);
   if(!request.EndpointPath.Contains("/ad-intents")||response.IsNetworkError||response.StatusCode!=200)return response;
   var data=JObject.Parse(response.BodyJson);if(data["createdAt"]==null)return response;
   if(FreshAdvertising){data["createdAt"]=now-1000;data["expiresAt"]=now+300000;if(data["callbackDeadline"]!=null)data["callbackDeadline"]=now+360000;}
   intent=data;
   return FreshAdvertising?RunApiHttpResponse.Success(response.StatusCode,data.ToString()):response;
  }
 }
}
