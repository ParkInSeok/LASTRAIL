using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Detached presentation of original results. No damage/healing computation, aggregation or ledger.
 public static class CombatResultLabels {
  public const string Version="combat-result-labels.v1.20261019";
  static long Amount(JObject facts,string name) {
   var value=facts[name];if(value==null||value.Type!=JTokenType.Integer)throw new ArgumentException("Original integer "+name+" required.");
   long n=(long)value;if(n<0)throw new ArgumentException("Negative original "+name);return n;
  }
  static string Text(long n)=>n==0?"":n.ToString(CultureInfo.InvariantCulture);
  public static JObject Read(JObject row) {
   if(row==null||row["actualResult"] is not JObject actual)throw new ArgumentException("Original resolved result required.");
   string kind=(string)row["kind"],critical=(string)actual["criticalState"]??"UNKNOWN";
   if(critical!="UNKNOWN"&&critical!="NOT_APPLICABLE"&&critical!="CRITICAL"&&critical!="NON_CRITICAL")throw new ArgumentException("Original critical status required.");
   long hp=0,shield=0,heal=0;bool contact=false,miss=false;
   switch(kind){
    case "DAMAGE":hp=Amount(actual,"hpLoss");shield=Amount(actual,"absorbed");contact=true;break;
    case "HEAL":case "REPAIR":heal=Amount(actual,"actualHpGain");break;
    case "MISS":miss=true;break;
    default:throw new ArgumentException("Unsupported original result kind.");
   }
   var rejected=actual["rejectedEffects"] as JArray;
   if(actual["rejectedEffects"]!=null&&rejected==null)throw new ArgumentException("Original rejected effects must be an array.");
   if(rejected!=null)foreach(var effect in rejected)if(effect is not JObject||effect["effectId"]?.Type!=JTokenType.String||effect["reason"]?.Type!=JTokenType.String)throw new ArgumentException("Original effect rejection identity/reason required.");
   return new JObject{["version"]=Version,["hpText"]=Text(hp),["shieldText"]=Text(shield),["healText"]=heal==0?"":"+"+Text(heal),["criticalState"]=critical,["critical"]=critical=="CRITICAL",["contact"]=contact,["miss"]=miss,["rejectedEffects"]=rejected?.DeepClone()??new JArray()};
  }
 }
}
