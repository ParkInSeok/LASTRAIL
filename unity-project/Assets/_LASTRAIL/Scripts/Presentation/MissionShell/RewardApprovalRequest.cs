using System;
using System.Linq;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 public interface IAccountRewardApprovalJournal { bool ReplaceApproval(PendingAccountAction expected,PendingAccountAction replacement); }
 // Durable request metadata, not a purchase receipt or client-issued entitlement.
 internal sealed class RewardApprovalRequest {
  readonly JObject value;
  internal string Kind=>(string)value["opportunity"]["kind"];
  internal string Run=>(string)value["opportunity"]["runId"];
  internal string Preview=>(string)value["opportunity"]["rewardPreviewId"];
  internal string Intent=>(string)value["opportunity"]["intentId"];
  internal string Target=>Kind=="OFFLINE"?"OFFLINE":Run;
  internal string Body=>new JObject{["opportunity"]=value["opportunity"].DeepClone()}.ToString(Newtonsoft.Json.Formatting.None);
  internal string Json=>value.ToString(Newtonsoft.Json.Formatting.None);
  internal PendingAccountAction Prior {get;}
  RewardApprovalRequest(JObject value,PendingAccountAction prior){this.value=value;Prior=prior;}
  internal static RewardApprovalRequest Create(string account,string kind,string run,string preview,PendingAccountAction prior){
   var op=new JObject{["kind"]=kind};if(kind=="COMBAT"){op["runId"]=run;op["rewardPreviewId"]=preview;}
   JObject saved=null;
   if(prior!=null){
    if(kind=="OFFLINE"&&(prior.Kind=="ad-intent"||prior.Kind=="ad-presented"))op["intentId"]=OfflineAdIntent.Parse(prior.Payload).Id;
    else if(kind=="COMBAT"&&(prior.Kind=="combat-ad-intent"||prior.Kind=="combat-ad-presented")){var old=CombatAdIntent.Parse(prior.Payload,run,preview);op["intentId"]=old.Id;}
    else throw new InvalidOperationException("Previous request requires recovery");
    saved=new JObject{["kind"]=prior.Kind,["target"]=prior.Target,["key"]=prior.Key,["payload"]=prior.Payload};
   }
   return Parse(account,new JObject{["opportunity"]=op,["prior"]=saved}.ToString());
  }
  internal static RewardApprovalRequest Parse(string account,string json){
   var root=GameApiJson.ReadObject(json);if(root.Properties().Any(p=>p.Name!="opportunity"&&p.Name!="prior")||root["opportunity"] is not JObject op)throw new FormatException("approval request");
   var kind=OfflineAdIntent.Text(op,"kind");if(kind!="OFFLINE"&&kind!="COMBAT")throw new FormatException("approval kind");
   var keys=kind=="OFFLINE"?new[]{"kind","intentId"}:new[]{"kind","intentId","runId","rewardPreviewId"};if(op.Properties().Any(p=>!keys.Contains(p.Name)))throw new FormatException("approval fields");
   if(kind=="COMBAT"){OfflineAdIntent.IdText(op,"runId");OfflineAdIntent.IdText(op,"rewardPreviewId");}if(op.Property("intentId")!=null)OfflineAdIntent.IdText(op,"intentId");
   PendingAccountAction prior=null;
   if(root["prior"] is JObject p){
    string pk=OfflineAdIntent.Text(p,"kind");if(!(kind=="OFFLINE"&&(pk=="ad-intent"||pk=="ad-presented")||kind=="COMBAT"&&(pk=="combat-ad-intent"||pk=="combat-ad-presented")))throw new FormatException("approval prior kind");
    prior=new PendingAccountAction(account,pk,(string)p["target"],0,(string)p["key"],(string)p["payload"]);
    var old=GameApiJson.ReadObject(prior.Payload);if((string)old["intentId"]!=(string)op["intentId"]||kind=="COMBAT"&&((string)old["runId"]!=(string)op["runId"]||(string)old["rewardPreviewId"]!=(string)op["rewardPreviewId"]))throw new FormatException("approval prior binding");
   }else if(root["prior"]?.Type!=JTokenType.Null||op.Property("intentId")!=null)throw new FormatException("approval prior");
   return new RewardApprovalRequest(root,prior);
  }
  internal static bool Same(PendingAccountAction a,PendingAccountAction b)=>a!=null&&b!=null&&a.Account==b.Account&&a.Kind==b.Kind&&a.Target==b.Target&&a.Key==b.Key&&a.Count==b.Count&&a.Payload==b.Payload;
  internal void ValidateApproved(PendingAccountAction next){
   if(Kind=="OFFLINE"){
    if(next.Kind!="ad-intent")throw new FormatException("approval destination");var parsed=OfflineAdIntent.Parse(next.Payload);
    if(Intent!=null&&parsed.Id!=Intent)throw new FormatException("approval identity");if(Prior!=null)parsed.RequireSame(OfflineAdIntent.Parse(Prior.Payload));
   }else{
    if(next.Kind!="combat-ad-intent")throw new FormatException("approval destination");var parsed=CombatAdIntent.Parse(next.Payload,Run,Preview);
    if(Intent!=null&&parsed.Id!=Intent)throw new FormatException("approval identity");if(Prior!=null)parsed.RequireSame(CombatAdIntent.Parse(Prior.Payload,Run,Preview));
   }
   if((string)GameApiJson.ReadObject(next.Payload)["contractVersion"]!=RewardOpportunityContract.Version)throw new FormatException("approval response version");
  }
 }
}
