using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  readonly JArray effectResults=new JArray();
  string effectRunId="LOCAL_DIAGNOSTIC",effectProfile;
  internal void BindEffectRun(string run,string profile){if(Tick!=0||effectResults.Count!=0||string.IsNullOrWhiteSpace(run)||string.IsNullOrWhiteSpace(profile))throw new ArgumentException();effectRunId=run;effectProfile=profile;}
  string EffectProfile=>effectProfile??ConfigurationDigest();
  public ConfirmedSkillDamage ReadConfirmedSkillDamage(string eventId){
   if(string.IsNullOrWhiteSpace(eventId))throw new ArgumentException("Damage event ID required");
   var row=effectResults.Cast<JObject>().SingleOrDefault(r=>(string)r["id"]==eventId);
   if(row==null||(string)row["kind"]!="DAMAGE")throw new InvalidOperationException("Confirmed skill damage not found");
   if(!commands.TryGetValue((string)row["commandId"],out var command)||command.Status!="APPLIED"||command.Skill.Caster.Config.Id!=(string)row["sourceId"])
    throw new InvalidOperationException("Original damage command attribution missing");
   if(command.Skill.Caster.Device)throw new InvalidOperationException("Device damage requires registered operator attribution");
   return new ConfirmedSkillDamage(row,command.Skill.Caster.Config.Train);
  }
  static string EffectIdentity(JObject row)=>Digest(StableJson(new JArray(row["runId"],row["profileSha256"],row["globalSequence"],row["commandId"],row["abilityId"],row["effectIndex"],row["targetId"],row["pulseIndex"],row["kind"])));
  void RecordEffect(Command command,int index,Actor target,string kind,JObject result){
   var row=new JObject{["runId"]=effectRunId,["profileSha256"]=EffectProfile,["configurationSha256"]=ConfigurationDigest(),["journalSequence"]=effectResults.Count+1,["globalSequence"]=command.Seq,["commandId"]=command.Id,["abilityId"]=Text(command.Skill.Data,"id"),["effectIndex"]=index,["targetId"]=target.Config.Id,["pulseIndex"]=0,["kind"]=kind,["tick"]=Tick,["sourceId"]=command.Skill.Caster.Config.Id,["sourceKind"]="SKILL",["actualResult"]=result};
   row["id"]=EffectIdentity(row);if(kind=="DAMAGE")ResolveSkillLifesteal(command,row);effectResults.Add(row);
  }
  JObject CaptureEffectJournal()=>new JObject{["version"]="combat-effect-results-v1",["runId"]=effectRunId,["profileSha256"]=EffectProfile,["rows"]=effectResults.DeepClone()};
  void RestoreEffectJournal(JObject journal){
   if(journal==null)return;Fields(journal,"version","runId","profileSha256","rows");if(Text(journal,"version")!="combat-effect-results-v1"||Text(journal,"runId")!=effectRunId||Text(journal,"profileSha256")!=EffectProfile)throw new ArgumentException("Effect journal binding mismatch");
   var ids=new System.Collections.Generic.HashSet<string>();var targets=allies.Select(a=>a.Config.Id).Concat(waves.SelectMany(w=>w).Select(a=>a.Id)).ToHashSet();
   foreach(var token in Rows(journal,"rows")){
    var r=token as JObject;var keys=new[]{"runId","profileSha256","configurationSha256","journalSequence","globalSequence","commandId","abilityId","effectIndex","targetId","pulseIndex","kind","tick","sourceId","sourceKind","actualResult","id"};Fields(r,r?["lifesteal"]==null?keys:keys.Concat(new[]{"lifesteal"}).ToArray());
    if(Text(r,"runId")!=effectRunId||Text(r,"profileSha256")!=EffectProfile||Text(r,"configurationSha256")!=ConfigurationDigest()||Nat(r,"journalSequence")!=effectResults.Count+1||Nat(r,"pulseIndex")!=0||Text(r,"sourceKind")!="SKILL"||Text(r,"id")!=EffectIdentity(r)||!ids.Add(Text(r,"id"))||!targets.Contains(Text(r,"targetId")))throw new ArgumentException("Effect result identity mismatch");
    Nat(r,"tick",Tick);if(!commands.TryGetValue(Text(r,"commandId"),out var cmd)||cmd.Status!="APPLIED"||Nat(r,"globalSequence",sequence)!=cmd.Seq||Text(r,"abilityId")!=Text(cmd.Skill.Data,"id")||Text(r,"sourceId")!=cmd.Skill.Caster.Config.Id)throw new ArgumentException("Effect command mismatch");
    var authored=(JArray)cmd.Skill.Data["effects"];var index=(int)Nat(r,"effectIndex",authored.Count-1);string type=Text(authored[index],"type"),kind=Text(r,"kind");
    if(!(kind==type.ToUpperInvariant()||kind=="SHIELD_ISSUE"&&(type=="shield"||type=="train_shield")||kind.StartsWith("STATUS_",StringComparison.Ordinal)&&new[]{"taunt","mark","train_damage_buff"}.Contains(type)))throw new ArgumentException("Effect kind mismatch");
    ValidateActualResult(kind,r["actualResult"] as JObject);ValidateSkillLifesteal(cmd,r);effectResults.Add(r.DeepClone());
   }
  }
  internal static void ValidateActualResult(string kind,JObject r){
   switch(kind){
    case "DAMAGE":Fields(r,"hpLoss","absorbed");Nat(r,"hpLoss",int.MaxValue);Nat(r,"absorbed",int.MaxValue);break;
    case "HEAL":case "REPAIR":Fields(r,"actualHpGain");Nat(r,"actualHpGain",int.MaxValue);break;
    case "SHIELD_ISSUE":Fields(r,"previousRemaining","resultingRemaining","addedAmount","previousExpiry","expiry","outcome");var previous=Nat(r,"previousRemaining",int.MaxValue);var current=Nat(r,"resultingRemaining",int.MaxValue);if(Nat(r,"addedAmount",int.MaxValue)!=Math.Max(0,current-previous))throw new ArgumentException();Nat(r,"previousExpiry");Nat(r,"expiry");if(!new[]{"ADD","REFRESH","UNCHANGED"}.Contains(Text(r,"outcome")))throw new ArgumentException();break;
    case "INTERRUPT":Fields(r,"cancelledActionIds");var actions=Rows(r,"cancelledActionIds");if(actions.Any(x=>x.Type!=JTokenType.Integer||(long)x<=0)||actions.Select(x=>(long)x).Distinct().Count()!=actions.Count)throw new ArgumentException();break;
    case "STATUS_ADD":case "STATUS_REFRESH":case "STATUS_IGNORED":Fields(r,"category","magnitude","previousExpiry","expiry","accepted");if(!new[]{"taunt","mark","train_damage_buff"}.Contains(Text(r,"category")))throw new ArgumentException();Nat(r,"magnitude",10000);Nat(r,"previousExpiry");Nat(r,"expiry");if(Boolean(r,"accepted")!=(kind!="STATUS_IGNORED"))throw new ArgumentException();break;
    default:throw new ArgumentException("Unknown typed effect result");
   }
  }
 }
}
