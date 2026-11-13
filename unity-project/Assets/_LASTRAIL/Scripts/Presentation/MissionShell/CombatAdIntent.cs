using System;
using LastRail.Auth;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // Immutable boundary object. The server alone authorizes and calculates rewards.
 public sealed class CombatAdIntent {
  readonly JObject data;
  CombatAdIntent(JObject data){this.data=(JObject)data.DeepClone();}
  public string Id=>(string)data["intentId"];public string RunId=>(string)data["runId"];public string PreviewId=>(string)data["rewardPreviewId"];
  public string Status=>(string)data["status"];public string Json=>data.ToString();
  public bool CanSpin=>(bool)data["canSpin"];public bool CanClaimBasic=>(bool)data["canClaimBasic"];
  public bool Terminal=>new[]{"CLAIMED","EXPIRED","REWARD_CONSUMED","UNAVAILABLE","PREVIEW_CHANGED"}.Contains(Status);
  public string UserId=>RewardOpportunityContract.AdText(data,"userId");public string CustomData=>RewardOpportunityContract.AdText(data,"customData");public string AdUnit=>RewardOpportunityContract.AdText(data,"adUnit");
  public JObject Result=>data["result"] is JObject o?(JObject)o.DeepClone():null;
  public JArray BaseLines=>(JArray)data["eligibleBaseRewardLines"].DeepClone();
  public JArray ExcludedLines=>(JArray)data["excludedBaseRewardLines"].DeepClone();
  static string ReadId(JObject o,string k)=>OfflineAdIntent.IdText(o,k);
  static string Text(JObject o,string k)=>OfflineAdIntent.Text(o,k);
  static long Number(JObject o,string k){if(o[k]?.Type!=JTokenType.Integer)throw new FormatException(k);var n=(long)o[k];if(n<0||n>9007199254740991L)throw new FormatException(k);return n;}
  static void Lines(JToken token,bool balances){if(token is not JArray a||a.Count>128)throw new FormatException("reward lines");foreach(var t in a){var row=t as JObject??throw new FormatException();ReadId(row,"id");Text(row,"kind");Text(row,"source");Number(row,"quantity");if(balances)Number(row,"balanceAfter");}}
  public static CombatAdIntent Parse(string json,string run,string preview){
   var o=GameApiJson.ReadObject(json);if(o["accepted"]?.Type!=JTokenType.Boolean||!(bool)o["accepted"]||ReadId(o,"runId")!=run||ReadId(o,"rewardPreviewId")!=preview)throw new FormatException("ad owner");
   ReadId(o,"intentId");Text(o,"policyVersion");
   if(RewardOpportunityContract.IsV2(o))RewardOpportunityContract.Validate(o,true);
   else {Text(o,"userId");Text(o,"customData");Text(o,"adUnit");if(Number(o,"expiresAt")<=Number(o,"createdAt")||Number(o,"callbackDeadline")<Number(o,"expiresAt"))throw new FormatException("ad expiry");}
   var state=Text(o,"status");if(!new[]{"AD_PENDING","AD_VERIFIED","RESULT_FIXED","CLAIMED","EXPIRED","REWARD_CONSUMED","UNAVAILABLE","PREVIEW_CHANGED"}.Contains(state))throw new FormatException("ad status");
   if(o["canSpin"]?.Type!=JTokenType.Boolean||(bool)o["canSpin"]!=(state=="AD_VERIFIED"||state=="RESULT_FIXED")||o["canClaimBasic"]?.Type!=JTokenType.Boolean)throw new FormatException("ad actions");
   Number(o,"serverNow");
   if(o["probabilities"] is not JArray p||p.Count!=3)throw new FormatException("probabilities");var weights=new[]{6000,3000,1000};for(int i=0;i<3;i++){var row=p[i] as JObject??throw new FormatException();if(Number(row,"multiplier")!=i+2||Number(row,"weightBps")!=weights[i])throw new FormatException("probabilities");}
   Lines(o["eligibleBaseRewardLines"],false);Lines(o["excludedBaseRewardLines"],false);
   if(state=="CLAIMED")ValidateResult(o["result"] as JObject,o);else if(o["result"]?.Type!=JTokenType.Null)throw new FormatException("unexpected result");
   return new CombatAdIntent(o);
  }
  static void ValidateResult(JObject result,JObject pinned){
   if(result==null||result["accepted"]?.Type!=JTokenType.Boolean||!(bool)result["accepted"]||Text(result,"status")!="CLAIMED")throw new FormatException("ad result");
   foreach(var key in new[]{"intentId","runId","rewardPreviewId","policyVersion"})if(Text(result,key)!=(string)pinned[key])throw new FormatException("result binding");
   var multiplier=Number(result,"multiplier");if(multiplier<2||multiplier>4)throw new FormatException("multiplier");ReadId(result,"rewardClaimId");RecruitmentJson.Utc(result,"serverClaimedAt");Lines(result["rewardLines"],true);
  }
  public CombatAdIntent WithResult(string json){var result=GameApiJson.ReadObject(json);ValidateResult(result,data);var next=(JObject)data.DeepClone();next["status"]="CLAIMED";next["canSpin"]=false;next["canClaimBasic"]=false;next["result"]=result;return Parse(next.ToString(),RunId,PreviewId);}
  public void RequireSame(CombatAdIntent prior){
   RewardOpportunityContract.RequireSame(data,prior.data);
   foreach(var key in new[]{"intentId","runId","rewardPreviewId","policyVersion","createdAt","expiresAt","callbackDeadline","probabilities","eligibleBaseRewardLines","excludedBaseRewardLines"})if(!JToken.DeepEquals(data[key],prior.data[key]))throw new FormatException("ad binding changed");
   if(prior.Status=="CLAIMED"&&(Status!="CLAIMED"||!JToken.DeepEquals(Result,prior.Result))||prior.CanSpin&&Status=="AD_PENDING"||prior.Status=="RESULT_FIXED"&&Status=="AD_VERIFIED")throw new FormatException("ad state regressed");
  }
  public static void ValidatePending(string kind,string target,string payload){var o=JObject.Parse(payload);var run=ReadId(o,"runId");var preview=ReadId(o,"rewardPreviewId");if(target!=run)throw new FormatException();if(kind!="combat-ad-create")Parse(payload,run,preview);}
 }
}
