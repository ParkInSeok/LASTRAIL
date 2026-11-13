using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.RoofBattle {
 // Serialized, independently pinned LOCAL authoring input; no account START or network source.
 public sealed class RoofBattleLocalSource {
  public BDirectAttackBasicLoopBinding Binding{get;}public string SourceHash{get;}
  RoofBattleLocalSource(BDirectAttackBasicLoopBinding binding,string hash){Binding=binding;SourceHash=hash;}
  public static RoofBattleLocalSource Read(JObject source,string trustedHash){
   if(source==null||BDirectAttackInput.Digest(source)!=trustedHash)throw new ArgumentException("Original LOCAL source hash mismatch.");
   BDirectAttackInput.Fields(source,"version","classification","attack","formation","ownedProfile","definitions","buffDefinitions","composition","loop");
   if((string)source["version"]!="unity.roof-local-source.v1.20261017"||(string)source["classification"]!="EXISTING_EVE_BASIC_LOCAL_NOT_OPERATIONAL")throw new ArgumentException("Unsupported roof LOCAL authoring source.");
   var f=(JObject)source["formation"];BDirectAttackInput.Fields(f,"version","trainId","mode","battlefield","internalNodeId","slots");
   if((string)f["version"]!=BCombatFormation.LocalVersion)throw new ArgumentException("Original formation version required.");
   var formation=new BCombatFormation((string)f["trainId"],(BCombatMode)Enum.Parse(typeof(BCombatMode),(string)f["mode"]),(BCombatBattlefield)Enum.Parse(typeof(BCombatBattlefield),(string)f["battlefield"]),((JArray)f["slots"]).Select(s=>new BCombatSlot((string)s["carriageId"],(string)s["operatorId"])),(string)f["internalNodeId"]);
   var packet=(JObject)source["attack"];var owned=(JObject)source["ownedProfile"];
   var input=new BDirectAttackInput(packet,BDirectAttackInput.Digest(packet),formation,owned,BDirectAttackInput.Digest(owned),CombatDefinitionIntake.Read((JObject)source["definitions"]));
   var defs=(JObject)source["buffDefinitions"];var definitions=new BCarriageBuffDefinitions(BasicCombatSimulation.StableJson(defs),BDirectAttackInput.Digest(defs).Substring(7));
   var composition=new BDirectAttackBuffBinding(input,definitions,(JObject)source["composition"],BDirectAttackInput.Digest(source["composition"]));
   var binding=new BDirectAttackBasicLoopBinding(composition,(JObject)source["loop"],BDirectAttackInput.Digest(source["loop"]));
   return new RoofBattleLocalSource(binding,trustedHash);
  }
 }
}
