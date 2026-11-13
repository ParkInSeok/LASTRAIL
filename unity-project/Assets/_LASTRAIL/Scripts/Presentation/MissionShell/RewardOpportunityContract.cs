using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // v2 keeps purchased approval separate from the optional advertising challenge.
 internal static class RewardOpportunityContract {
  internal const string Version="reward-opportunity-v2";
  internal static bool IsV2(JObject o)=>o.Property("contractVersion")!=null;
  internal static long Number(JObject o,string key){if(o[key]?.Type!=JTokenType.Integer)throw new FormatException(key);var n=(long)o[key];if(n<0||n>9007199254740991L)throw new FormatException(key);return n;}
  internal static string AdText(JObject o,string key)=>(string)(IsV2(o)?(o["adContext"] as JObject)?[key]:o[key]);
  internal static void Validate(JObject o,bool combat){
   if((string)o["contractVersion"]!=Version)throw new FormatException("reward contract");
   var created=Number(o,"createdAt");
   if(o["approval"] is JObject approval){var cause=OfflineAdIntent.Text(approval,"cause");if(cause!="AD_SSV"&&cause!="PURCHASE_ENTITLEMENT")throw new FormatException("approval cause");if(Number(approval,"approvedAt")<created)throw new FormatException("approval time");}
   else if(o["approval"]?.Type!=JTokenType.Null)throw new FormatException("approval");
   if(o["adContext"] is JObject ad){
    foreach(var k in new[]{"userId","customData","adUnit"})OfflineAdIntent.Text(ad,k);
    var expires=Number(ad,"expiresAt");if(expires<=created||Number(ad,"callbackDeadline")<expires||!JToken.DeepEquals(o["expiresAt"],ad["expiresAt"])||combat&&!JToken.DeepEquals(o["callbackDeadline"],ad["callbackDeadline"]))throw new FormatException("ad clock");
   }else if(o["adContext"]?.Type!=JTokenType.Null||o["expiresAt"]?.Type!=JTokenType.Null||combat&&o["callbackDeadline"]?.Type!=JTokenType.Null||o["approval"] is not JObject paid||(string)paid["cause"]!="PURCHASE_ENTITLEMENT")throw new FormatException("purchased context");
   if(((bool?)o["canSpin"]==true||(string)o["status"]=="CLAIMED")&&o["approval"] is not JObject)throw new FormatException("missing approval");
   if((string)o["status"]=="AD_PENDING"&&o["approval"] is JObject)throw new FormatException("pending approval");
   if(!combat){
    if(o["baseRewardLines"] is not JArray lines||lines.Count==0||lines.Count>4)throw new FormatException("base rewards");
    var ids=new System.Collections.Generic.HashSet<string>();foreach(var token in lines){var row=token as JObject??throw new FormatException("base reward");var id=OfflineAdIntent.IdText(row,"id");if(!new[]{"cur_credits","cur_machine_parts","cur_tactical_data","cur_operator_exp"}.Contains(id)||!ids.Add(id))throw new FormatException("base identity");Number(row,"quantity");if(row.Property("kind")!=null&&row["kind"].Type!=JTokenType.Null&&OfflineAdIntent.Text(row,"kind")!="CURRENCY")throw new FormatException("base kind");if(row.Property("source")!=null&&row["source"].Type!=JTokenType.Null)OfflineAdIntent.Text(row,"source");}
   }
  }
  internal static void RequireSame(JObject next,JObject prior){
   foreach(var key in new[]{"userId","customData","adUnit"})if(AdText(next,key)!=AdText(prior,key))throw new FormatException("ad context changed");
   if(IsV2(prior)){
    if(!IsV2(next)||!JToken.DeepEquals(next["adContext"],prior["adContext"])||prior["approval"] is JObject&&!JToken.DeepEquals(next["approval"],prior["approval"])||!JToken.DeepEquals(next["baseRewardLines"],prior["baseRewardLines"]))throw new FormatException("reward binding changed");
   }
  }
 }
}
