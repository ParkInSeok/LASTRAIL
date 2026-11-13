using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public sealed class OfflineAdIntent {
  readonly JObject value;
  OfflineAdIntent(JObject value){this.value=(JObject)value.DeepClone();}
  public string Id=>(string)value["intentId"];
  public string Status=>(string)value["status"];
  public string Policy=>(string)value["policyVersion"];
  public string UserId=>RewardOpportunityContract.AdText(value,"userId");
  public string CustomData=>RewardOpportunityContract.AdText(value,"customData");
  public string AdUnit=>RewardOpportunityContract.AdText(value,"adUnit");
  public JArray BaseLines=>value["baseRewardLines"] is JArray rows?(JArray)rows.DeepClone():null;
  public int[] Multipliers=>value["displayMultipliers"].Values<int>().ToArray();
  public string ProbabilityText=>value["probabilities"] is JArray probabilities
   ?string.Join("\n",probabilities.Select(row=>((int)row["multiplier"]).ToString(CultureInfo.InvariantCulture)+"배  "+((int)row["weightBps"]/100m).ToString("0.##",CultureInfo.InvariantCulture)+"%"))
   :"상세 확률 정보는 현재 제공되지 않습니다.";
  public bool CanSpin=>Status=="AD_VERIFIED";
  public JObject Result=>value["result"] as JObject ==null?null:(JObject)value["result"].DeepClone();
  public string Json=>value.ToString();
  internal static string Text(JObject o,string key){if(o[key]?.Type!=JTokenType.String)throw new FormatException(key);var s=(string)o[key];if(string.IsNullOrWhiteSpace(s)||s.Length>256||s.Any(char.IsControl))throw new FormatException(key);return s;}
  internal static string IdText(JObject o,string key){var s=Text(o,key);if(!Regex.IsMatch(s,@"\A[A-Za-z0-9_-]{1,128}\z"))throw new FormatException(key);return s;}
  static long Number(JObject o,string key){if(o[key]?.Type!=JTokenType.Integer)throw new FormatException(key);var n=(long)o[key];if(n<0||n>9007199254740991L)throw new FormatException(key);return n;}
  public static OfflineAdIntent Parse(string json){
   var o=JObject.Parse(json);if(o["accepted"]?.Type!=JTokenType.Boolean||!(bool)o["accepted"])throw new FormatException();
   IdText(o,"intentId");Text(o,"policyVersion");
   if(RewardOpportunityContract.IsV2(o))RewardOpportunityContract.Validate(o,false);
   else {Text(o,"userId");Text(o,"customData");Text(o,"adUnit");if(Number(o,"expiresAt")<=Number(o,"createdAt"))throw new FormatException();}
   var status=Text(o,"status");if(!new[]{"AD_PENDING","AD_VERIFIED","CLAIMED","EXPIRED","SUPPLY_CONSUMED"}.Contains(status))throw new FormatException();
   if(o["canSpin"]?.Type!=JTokenType.Boolean||(bool)o["canSpin"]!=(status=="AD_VERIFIED"))throw new FormatException();
   if(o["displayMultipliers"] is not JArray values||values.Count==0||values.Count>3||values.Any(x=>x.Type!=JTokenType.Integer||(int)x<2||(int)x>4))throw new FormatException();
   var ints=values.Values<int>().ToArray();if(!ints.SequenceEqual(ints.Distinct().OrderBy(x=>x)))throw new FormatException();
   ValidateProbabilities(o,ints);
   if(status=="CLAIMED")ValidateResult(o["result"] as JObject,(string)o["intentId"],(string)o["policyVersion"],ints);
   else if(o["result"]?.Type!=JTokenType.Null)throw new FormatException();
   return new OfflineAdIntent(o);
  }
  public OfflineAdIntent WithResult(string json){var result=JObject.Parse(json);ValidateResult(result,Id,Policy,Multipliers);var copy=(JObject)value.DeepClone();copy["status"]="CLAIMED";copy["canSpin"]=false;copy["result"]=result;return Parse(copy.ToString());}
  public void RequireSame(OfflineAdIntent previous){if(previous!=null)RewardOpportunityContract.RequireSame(value,previous.value);if(previous!=null&&(previous.Status=="CLAIMED"&&(Status!="CLAIMED"||!JToken.DeepEquals(Result,previous.Result))||previous.Status=="AD_VERIFIED"&&Status=="AD_PENDING"))throw new FormatException("Intent state regressed");if(previous!=null&&(Id!=previous.Id||Policy!=previous.Policy||UserId!=previous.UserId||CustomData!=previous.CustomData||AdUnit!=previous.AdUnit||!Multipliers.SequenceEqual(previous.Multipliers)||(long)value["createdAt"]!=(long)previous.value["createdAt"]||!JToken.DeepEquals(value["expiresAt"],previous.value["expiresAt"])||previous.value["probabilities"]!=null&&!JToken.DeepEquals(value["probabilities"],previous.value["probabilities"])))throw new FormatException("Intent changed");}
  static void ValidateProbabilities(JObject o,int[] displayed){
   // Older saved intents predate this response field; a present value must be complete and valid.
   if(o.Property("probabilities")==null)return;
   if(o["probabilities"] is not JArray rows||rows.Count!=3)throw new FormatException("probabilities");
   var seen=new System.Collections.Generic.HashSet<int>();var positive=new System.Collections.Generic.List<int>();long sum=0;
   foreach(var token in rows){
    var row=token as JObject??throw new FormatException("probabilities");var multiplier=Number(row,"multiplier");var weight=Number(row,"weightBps");
    if(multiplier<2||multiplier>4||weight>10000||!seen.Add((int)multiplier))throw new FormatException("probabilities");
    sum+=weight;if(weight>0)positive.Add((int)multiplier);
   }
   if(sum!=10000||!positive.OrderBy(x=>x).SequenceEqual(displayed))throw new FormatException("probabilities");
   o["probabilities"]=new JArray(rows.OrderBy(row=>(int)row["multiplier"]));
  }
  static void ValidateResult(JObject o,string id,string policy,int[] allowed){
   if(o==null||o["accepted"]?.Type!=JTokenType.Boolean||!(bool)o["accepted"]||Text(o,"status")!="CLAIMED"||IdText(o,"intentId")!=id||Text(o,"policyVersion")!=policy||o["multiplier"]?.Type!=JTokenType.Integer||!allowed.Contains((int)o["multiplier"]))throw new FormatException();
   IdText(o,"rewardTransactionId");if(o["rewardLines"] is not JArray rows||rows.Count==0||rows.Count>4)throw new FormatException();
   var ids=new System.Collections.Generic.HashSet<string>();foreach(var token in rows){var r=token as JObject??throw new FormatException();var currency=Text(r,"id");if(Text(r,"kind")!="CURRENCY"||!new[]{"cur_credits","cur_machine_parts","cur_tactical_data","cur_operator_exp"}.Contains(currency)||!ids.Add(currency))throw new FormatException();var quantity=Number(r,"quantity");if(quantity<=0||Number(r,"balanceAfter")<quantity)throw new FormatException();}
  }
 }
 public interface IRewardAdPresentation { bool Available {get;} System.Threading.Tasks.Task ShowAsync(string userId,string customData,string adUnit); }
 public interface IAccountAdActionJournal { bool ReplaceAd(PendingAccountAction expected,PendingAccountAction replacement); }
}

