using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Borrows the node kernel; original effect ownership is resolved for each real damage event.
 internal sealed class OrdinaryNodeSkillLifesteal {
  readonly OrdinaryFiveCombatFactory factory;readonly BCombatLocalModel health;
  readonly ICombatSkillLifestealAuthority authority;readonly string binding;
  readonly Dictionary<string,JObject> decisions=new Dictionary<string,JObject>(StringComparer.Ordinal);
  internal OrdinaryNodeSkillLifesteal(OrdinaryFiveCombatFactory factory,BCombatLocalModel health,ICombatSkillLifestealAuthority authority){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));this.authority=authority??throw new ArgumentNullException(nameof(authority));
   binding=authority.BindingId;Require(!string.IsNullOrWhiteSpace(binding)&&factory.Attempt!=null);
   var state=health.CaptureIdentity();var d=state["definition"];var a=factory.Attempt;
   Require((string)state["schema"]=="unity.combat-b.node-health-local.v1.20261102"&&d!=null&&(string)d["accountId"]==a.AccountId&&(string)d["runId"]==a.RunId&&(string)d["nodeId"]==a.NodeId&&(string)d["snapshotHash"]==a.SnapshotHash&&(long?)d["generation"]==a.Generation&&(long?)d["attemptRevision"]==a.Revision);
  }
  internal JObject Apply(JObject row,BCombatSingleTargetReceipt damage){
   Require(row!=null&&damage!=null&&authority.BindingId==binding);
   string actor=(string)row["sourceId"],ability=(string)row["abilityId"];
   Require(factory.Original.Actors.Any(a=>a.OperatorId==actor)&&(ability==actor+".active"||ability==actor+".ultimate")&&(string)row["kind"]=="DAMAGE"&&(string)row["sourceKind"]=="SKILL");
   Require((string)row["runId"]==factory.Attempt.RunId&&(string)row["nodeId"]==factory.Attempt.NodeId&&(long?)row["attemptGeneration"]==factory.Attempt.Generation&&(string)row["profileSha256"]==BasicCombatSimulation.Digest(factory.ConfigurationVersion));
   Require((string)row["targetId"]==damage.TargetId&&(long?)row["tick"]==damage.Tick&&(int?)row["actualResult"]?["hpLoss"]==damage.HpLoss&&(int?)row["actualResult"]?["absorbed"]==damage.Absorbed&&health.Hp(damage.TargetId)==damage.HpAfter);
   Require(!string.IsNullOrWhiteSpace((string)row["id"])&&!string.IsNullOrWhiteSpace((string)row["commandId"])&&(long?)row["globalSequence"]>0);
   Require(row["pulseIndex"]?.Type==JTokenType.Integer&&(int)row["pulseIndex"]>=0&&row["targetGeneration"]?.Type==JTokenType.Integer&&(long)row["targetGeneration"]>=1);
   var input=(JObject)row.DeepClone();input.Remove("lifesteal");string fingerprint=BDirectAttackInput.Digest(input);
   if(decisions.TryGetValue(damage.EventId,out var prior)){
    Require((string)prior["inputHash"]==fingerprint&&(row["lifesteal"]==null||JToken.DeepEquals(row["lifesteal"],prior["result"])));
    row["lifesteal"]=prior["result"].DeepClone();return (JObject)prior["result"].DeepClone();
   }
   Require(row["lifesteal"]==null&&decisions.Count<10000);
   var proof=authority.ResolveOriginal(new ConfirmedSkillDamage(row,false),damage.Tick)??throw new InvalidOperationException("Original node skill lifesteal proof missing.");
   int before=health.Hp(actor),maximum=health.MaximumHp(actor);string outcome=proof.Outcome(before,maximum,damage.HpLoss);
   int request=health.TrainDefeated?0:proof.Recovery(before,maximum,damage.HpLoss);
   var receipt=request==0?null:health.ApplyLifesteal("node-skill-heal:"+BasicCombatSimulation.Digest(damage.EventId),actor,request,damage.Tick);
   var result=new JObject{["evidenceId"]=proof.EvidenceId,["definitionVersion"]=proof.DefinitionVersion,["hpBefore"]=before,["maximumHp"]=maximum,["hpAfter"]=health.Hp(actor),["actualHpGain"]=receipt?.HpGain??0,["derivedExcess"]=0,["outcome"]=health.TrainDefeated?"TRAIN_DOWN":outcome};
   if(receipt!=null)result["healthEventId"]=receipt.EventId;
   decisions.Add(damage.EventId,new JObject{["inputHash"]=fingerprint,["result"]=result.DeepClone()});row["lifesteal"]=result.DeepClone();return result;
  }
  internal OrdinaryNodeSkillLifesteal CloneOwned(BCombatLocalModel newHealth)=>CloneOwned(newHealth,null);
  internal OrdinaryNodeSkillLifesteal CloneOwned(BCombatLocalModel newHealth,ICombatSkillLifestealAuthority rebound){
   Require(newHealth!=null&&!ReferenceEquals(health,newHealth)&&health.HasSameState(newHealth));
   if(rebound!=null)Require(rebound.BindingId==binding);var copy=new OrdinaryNodeSkillLifesteal(factory,newHealth,rebound??authority);foreach(var entry in decisions)copy.decisions.Add(entry.Key,(JObject)entry.Value.DeepClone());return copy;
  }
  internal JObject Capture()=>new JObject{["binding"]=binding,["decisions"]=new JObject(decisions.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new JProperty(p.Key,p.Value.DeepClone())))};
  static void Require(bool valid){if(!valid)throw new InvalidOperationException("Original node skill damage attribution differs.");}
 }
}
