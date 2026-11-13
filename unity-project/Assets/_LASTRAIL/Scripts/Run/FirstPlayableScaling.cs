using System;
using System.Linq;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Verified arrival values, already scaled. Execution also requires the retained START and initialization receipt.
 public sealed class FirstPlayableScaling {
  readonly JObject original;
  public string BindingHash=>(string)original["bindingHash"];
  public string EncounterId=>(string)original["encounterId"];
  public System.Collections.Generic.IReadOnlyList<string> IssuedGunnerIds{get;}
  public string IssuedHazardId{get;}
  public string IssuedEnemyTrainId=>(string)original["enemySpatial"]?["physicalInstanceId"];
  public CombatExactValue GunnerHp=>Value("gunnerHp");
  public CombatExactValue LauncherHp=>Value("launcherHp");
  public CombatExactValue GunnerRawDamage=>Value("gunnerRawDamage");
  public CombatExactValue ArtilleryRawDamage=>Value("artilleryRawDamage");
  CombatExactValue Value(string key)=>CombatExactValue.From((JObject)original["applied"][key]);
  public JObject Capture()=>(JObject)original.DeepClone();
  FirstPlayableScaling(JObject p){
   original=(JObject)p.DeepClone();
   if((string)p["profileVersion"]=="combat.first-playable-profile.v3.20261028"||(string)p["profileVersion"]==FirstPlayableDefinitions.ProfileVersion){
    string Issue(string role)=>BDirectAttackInput.Digest(new JObject{["version"]="combat.first-objective-identity.v1.20261028",["accountId"]=p["accountId"].DeepClone(),["runId"]=p["runId"].DeepClone(),["encounterId"]=p["encounterId"].DeepClone(),["sourceHash"]=p["sourceHash"].DeepClone(),["role"]=role}).Substring(7,32);
    IssuedGunnerIds=Array.AsReadOnly(new[]{Issue("gunner-0"),Issue("gunner-1")});IssuedHazardId=Issue("single-artillery");
   }
  }
  internal static FirstPlayableScaling Read(JToken raw,BRouteCommand command,JObject receipt){
   var p=raw as JObject;ValidateFields(p);
   Need(command.Kind=="ARRIVE"&&!command.IsWave);
   var route=command.Binding.Capture();var node=route["pendingNode"] as JObject;var state=receipt["state"] as JObject;
   Need(node!=null&&(string)node["eventKind"]=="COMBAT"&&(string)state?["phase"]=="ACTIVE"&&((JArray)state["holdIds"]).Count==0);
   Need((string)p["accountId"]==command.Binding.AccountId&&(string)p["runId"]==command.Binding.RunId&&(string)p["encounterId"]==command.EventId);
   Need(JToken.DeepEquals(p["nodeId"],node["nodeId"])&&JToken.DeepEquals(p["visitDepth"],node["visitDepth"])&&JToken.DeepEquals(p["progressRevision"],receipt["revision"]));
   Need((string)p["progressStateHash"]==SafeTransitionWire.Digest(state)&&JToken.DeepEquals(p["startSnapshotHash"],route["startSnapshotHash"]));
   Need(JToken.DeepEquals(p["profileId"],node["encounterProfileRef"])&&JToken.DeepEquals(p["profileVersion"],node["encounterVersion"]));
   return ReadDefinition(p,OrdinaryObstaclePin.CombatProfile(node["encounterDefinition"] as JObject),(string)route["selectedRisk"]);
  }
  // The map authority supplies these persisted values; START and initialization still bind their source.
  internal static FirstPlayableScaling ReadTrusted(JObject p,OrdinaryFiveStartInput start,string initializationResponse,JObject node){
   ValidateFields(p);Need(start!=null&&node!=null);
   Need((string)p["accountId"]==start.AccountId&&(string)p["runId"]==start.RunId&&(string)p["nodeId"]==(string)node["nodeId"]&&JToken.DeepEquals(p["progressRevision"],node["entryMapRevision"]));
   SafeTransitionWire.Id(p,"encounterId");OrdinaryNodeStart.Revision(p,"progressRevision");
   Need(System.Text.RegularExpressions.Regex.IsMatch((string)p["progressStateHash"]??"","\\A[a-f0-9]{64}\\z"));
   var result=ReadDefinition(p,start.FirstBattle?.Definition,start.FirstBattle?.SelectedRisk);result.RequireOriginal(start,initializationResponse);return result;
  }
  static void ValidateFields(JObject p){
   bool referenced=(string)p?["version"]=="combat.first-playable-scaling.v2.20261028";
   var fields=new[]{"version","accountId","runId","encounterId","nodeId","visitDepth","progressRevision","progressStateHash","startSnapshotHash","initializationResponseHash","sourceHash","profileId","profileVersion","profileHash","riskDefinitionVersion","riskDefinitionHash","applied","hpRiskApplications","damageRiskApplications","damageProgressApplications","bindingHash"};
   BDirectAttackInput.Fields(p,referenced?fields.Concat(new[]{"enemySpatial"}).ToArray():fields);
   Need(referenced||(string)p["version"]=="combat.first-playable-scaling.v1.20261027");
   Need(referenced==((string)p["profileVersion"]==FirstPlayableDefinitions.ProfileVersion));
   SafeTransitionWire.Pin(p,"bindingHash",(string)p["bindingHash"]);
  }
  static FirstPlayableScaling ReadDefinition(JObject p,JObject definition,string selectedRisk){
   bool referenced=(string)p["version"]=="combat.first-playable-scaling.v2.20261028";
   long depth=BDirectAttackInput.Nat(p,"visitDepth",1,11);string band=depth<=4?"EARLY":depth<=8?"MIDDLE":"LATE";
   foreach(var key in new[]{"initializationResponseHash","sourceHash"})Need(System.Text.RegularExpressions.Regex.IsMatch((string)p[key]??"","\\A[a-f0-9]{64}\\z"));
   foreach(var key in new[]{"hpRiskApplications","damageRiskApplications","damageProgressApplications"})Need(p[key]?.Type==JTokenType.Integer&&(long)p[key]==1);
   var applied=p["applied"] as JObject;
   Need(definition!=null&&applied!=null&&(string)p["profileId"]=="first_battle_forward_gunners_single_artillery"&&new[]{"e890b2bdbbf388048d8a2c77d82eb4ecca37c60b4883cf6317a7aaf0a7c1a1ae","d8db615dfb8beef8aaca82a76544d3e2427a52970da15df9166f74af784ea4f3","86b2aff8721ab355501da88011b922703a1b5723d5e01c5b456f942b1c110e74",FirstPlayableDefinitions.ProfileHash}.Contains((string)p["profileHash"]));
   var body=(JObject)definition.DeepClone();body.Remove("definitionHash");Need(SafeTransitionWire.Digest(body)==(string)p["profileHash"]);
   Need(JToken.DeepEquals(p["profileId"],definition["profileId"])&&JToken.DeepEquals(p["profileVersion"],definition["version"])&&JToken.DeepEquals(p["profileHash"],definition["definitionHash"]));
   Need(JToken.DeepEquals(p["riskDefinitionVersion"],definition["scaling"]?["definitionVersion"])&&JToken.DeepEquals(p["riskDefinitionHash"],definition["scaling"]?["definitionHash"]));
   Need((string)applied["risk"]==selectedRisk&&(string)applied["progress"]==band);
   var variant=referenced?FirstPlayableDefinitions.Variant(definition,(string)applied["risk"],band):((JArray)definition["scaling"]["variants"]).SingleOrDefault(v=>JToken.DeepEquals(v["risk"],applied["risk"])&&(string)v["progress"]==band);Need(variant!=null&&JToken.DeepEquals(variant,applied));
   if(referenced)FirstPlayableDefinitions.RequireEnemyBinding(p["enemySpatial"] as JObject,p);
   return new FirstPlayableScaling(p);
  }
  public void RequireOriginal(OrdinaryFiveStartInput start,string initializationResponse){
   Need(start?.FirstBattle?.SelectedRisk!=null&&(string)original["accountId"]==start.AccountId&&(string)original["runId"]==start.RunId);
   Need((string)original["startSnapshotHash"]==start.StartSnapshotHash&&(string)original["sourceHash"]==start.FirstBattle.SourceHash&&(string)original["profileHash"]==start.FirstBattle.ProfileHash&&(string)original["applied"]["risk"]==start.FirstBattle.SelectedRisk);
   Need(OrdinaryInitializationReceipt.Validate(initializationResponse,start)!=null);
   var receipt=GameApiJson.ReadObject(initializationResponse)["initializationReceipt"];
   Need(receipt["firstBattleEncounterBinding"] is JObject&&(string)original["initializationResponseHash"]==SafeTransitionWire.Digest(receipt));
  }
  static void Need(bool value){if(!value)throw new ArgumentException("first_playable_scaling_conflict");}
 }
}
