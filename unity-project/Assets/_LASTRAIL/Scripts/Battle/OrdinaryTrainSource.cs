using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Permanent run-pinned stats only; no current HP, shield, or initialization default.
 public sealed class OrdinaryTrainSource {
  public const string Version="combat.ordinary-train-source.v1.20261027";
  public const string InitialHealthVersion="combat.ordinary-train-source.v2.20261027";
  public string SourceVersion{get;}
  const string DefinitionVersion="combat.owned-train-base.2026-10-01.1",DefinitionHash="8bdf54659581a228e5a2846028a9c56ee31f975cd5e18927bcf071dc9d73245c";
  const long BaseHp=5000,BaseAttack=60,HpGrowthBps=800,AttackGrowthBps=600;
  public string SourceHash{get;}
  public string TrainId{get;}
  public long Level{get;}
  public long Revision{get;}
  public long PermanentMaxHp{get;}
  public long PermanentAttack{get;}
  public long PermanentDefense{get;}
  public long BasicIntervalMs{get;}
  internal OrdinaryTrainSource(JToken raw,OrdinaryFiveStartInput start,string ownedSourceHash){
   var source=raw as JObject;BDirectAttackInput.Fields(source,"packet","sourceHash");var p=source["packet"] as JObject;
   SourceVersion=(string)p?["version"];
   var keys=new[]{"version","accountId","runId","ordinaryOriginHash","ownedSourceHash","loadoutRevision","train","definitionVersion","definitionHash","permanentStats"};
   BDirectAttackInput.Fields(p,SourceVersion==InitialHealthVersion?System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(keys,new[]{"initialization"})):keys);
   Need(SourceVersion==Version||SourceVersion==InitialHealthVersion);
   if(SourceVersion==InitialHealthVersion)Need(JToken.DeepEquals(p["initialization"],new JObject{["policyVersion"]="TRAIN-INITIAL-HEALTH-RESUME-v1.20261027",["departureEffects"]="NO_ACTIVE_DEPARTURE_EFFECTS"}));
   SourceHash=(string)source["sourceHash"];Need(SourceHash==BDirectAttackInput.Digest(p).Substring(7));
   Need((string)p["accountId"]==start.AccountId&&(string)p["runId"]==start.RunId&&(string)p["ordinaryOriginHash"]==start.OriginHash&&(string)p["ownedSourceHash"]==ownedSourceHash&&BDirectAttackInput.Nat(p,"loadoutRevision",1)==start.LoadoutRevision);
   Need((string)p["definitionVersion"]==DefinitionVersion&&(string)p["definitionHash"]==DefinitionHash);
   var train=p["train"] as JObject;BDirectAttackInput.Fields(train,"trainId","level","revision");TrainId=BDirectAttackInput.Text(train,"trainId");Need(TrainId.Length<=128);
   Level=BDirectAttackInput.Nat(train,"level",1,10);Revision=BDirectAttackInput.Nat(train,"revision",1);
   var stats=p["permanentStats"] as JObject;BDirectAttackInput.Fields(stats,"maxHp","attack","defense","basicIntervalMs");
   PermanentMaxHp=BDirectAttackInput.Nat(stats,"maxHp",1);PermanentAttack=BDirectAttackInput.Nat(stats,"attack",1);PermanentDefense=BDirectAttackInput.Nat(stats,"defense");BasicIntervalMs=BDirectAttackInput.Nat(stats,"basicIntervalMs",1);
   Need(PermanentMaxHp==BaseHp*(10000+HpGrowthBps*(Level-1))/10000&&PermanentAttack==BaseAttack*(10000+AttackGrowthBps*(Level-1))/10000&&PermanentDefense==111&&BasicIntervalMs==1000);
  }
  static void Need(bool condition){if(!condition)throw new ArgumentException("ordinary_train_source_invalid");}
 }
}
