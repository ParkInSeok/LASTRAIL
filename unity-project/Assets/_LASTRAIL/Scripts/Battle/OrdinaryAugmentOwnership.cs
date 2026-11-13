using System;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Trusted server response at the current run boundary, not a client-selected flag.
 internal sealed class OrdinaryAugmentOwnership {
  internal readonly string Hash;
  internal readonly CombatO22ApplicationOriginal O22;
  internal OrdinaryAugmentOwnership(JObject packet,OrdinaryFiveStartInput original){
   if(packet==null)throw new ArgumentException("Original augment ownership required.");
   BDirectAttackInput.Fields(packet,"version","accountId","runId","startSnapshotHash","selected","o22Original","packetHash");
   var body=(JObject)packet.DeepClone();body.Remove("packetHash");Hash=(string)packet["packetHash"];
   if((string)packet["version"]!="ordinary.augment-ownership.v1.20261109"||(string)packet["accountId"]!=original.AccountId||(string)packet["runId"]!=original.RunId||(string)packet["startSnapshotHash"]!=original.StartSnapshotHash||BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(body))!=Hash)throw new ArgumentException("Augment ownership origin differs.");
   if(!(packet["selected"] is JArray selected)||selected.Count>1000)throw new ArgumentException("Complete selected augment list required.");
   var events=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
   foreach(JObject item in selected){
    BDirectAttackInput.Fields(item,"effectId","eventId","revision","checkpointHash");BCombatFormation.Identity((string)item["eventId"]);
    if(!events.Add((string)item["eventId"])||item["revision"]?.Type!=JTokenType.Integer||(long)item["revision"]<1||!System.Text.RegularExpressions.Regex.IsMatch((string)item["checkpointHash"]??"","^[a-f0-9]{64}$"))throw new ArgumentException("Selection proof differs.");
    if((string)item["effectId"]!="O22")throw new NotSupportedException("Selected augment needs its current ordinary effect owner: "+item["effectId"]);
   }
   if(selected.Count==0){if(packet["o22Original"]?.Type!=JTokenType.Null)throw new ArgumentException("Unexpected acquisition original.");return;}
   if(selected.Count!=1||!(packet["o22Original"] is JObject raw)||(string)raw["grantEventId"]!=(string)selected[0]["eventId"])throw new ArgumentException("One matching O22 acquisition required.");
   O22=CombatO22ApplicationOriginal.Read(raw.ToString(),original.AccountId,original.RunId,(string)raw["grantEventId"],(string)raw["environment"],original.StartSnapshotHash);
  }
 }
 internal sealed class OrdinaryLiveSkillLifesteal : ICombatSkillLifestealAuthority {
  readonly OrdinaryFiveCombatFactory factory;readonly Func<long,int> healing;
  public string BindingId {get;}
  internal OrdinaryLiveSkillLifesteal(OrdinaryFiveCombatFactory factory,Func<long,int> healing){this.factory=factory;this.healing=healing;BindingId="ordinary-o22:"+BasicCombatSimulation.Digest(factory.ConfigurationVersion);}
  public CombatSkillLifestealProof ResolveOriginal(ConfirmedSkillDamage damage,long tick){
   if(damage.RunId!=factory.Original.RunId||damage.NodeId!=factory.Attempt.NodeId||damage.AttemptGeneration!=factory.Attempt.Generation||damage.ProfileHash!=BasicCombatSimulation.Digest(factory.ConfigurationVersion)||damage.OperatorId!="op_reina"||damage.AbilityId!="op_reina.ultimate"||!factory.EnemyIds.Contains(damage.TargetId))throw new InvalidOperationException("Original skill attribution differs.");
   var original=factory.AugmentOwnership.O22;
   return new CombatSkillLifestealProof(original?.ApplicationId??factory.AugmentOwnership.Hash,original?.DefinitionPacketHash??"ordinary.augment-ownership.v1.20261109",original!=null,true,true,healing(tick));
  }
 }
}
