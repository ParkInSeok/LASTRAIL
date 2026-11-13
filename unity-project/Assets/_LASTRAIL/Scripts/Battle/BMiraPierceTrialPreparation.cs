using System;
using System.Linq;
using System.Text;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 /// <summary>
 /// Approved trial-policy preparation, not a new authenticated trial START or combat
 /// context. Keeps verified existing equipment and original node intact while the
 /// Server-owned grant/departure executable contract is absent. The supplied logical policy/profile is pinned separately. No runtime accepts this
 /// type, and comparison intent is never evidence of possessed piercing.
 /// </summary>
 public sealed class BMiraPierceTrialPreparation {
  public const string Version="mira.pierce-trial-preparation.v1.20261016";
  public const string Policy="pierce-local-trial-policy-approved-20261016";
  readonly JObject original;
  public string Hash{get;}
  public bool CanStart=>false;
  public bool RuntimeSupported=>false;
  BMiraPierceTrialPreparation(JObject original){this.original=original;Hash=BDirectAttackInput.Digest(original);}
  public JObject Capture()=>(JObject)original.DeepClone();
  public void RequireRuntimeReady()=>throw new InvalidOperationException("mira_pierce_trial_contract_missing");
  public static BMiraPierceTrialPreparation Prepare(string comparisonGroup,BMiraEquippedTrialContext equipment,BBoardingEventsNodeSnapshot node,JObject independentlyTrustedNativeRun,string nativeRunEvidenceHash,BMiraPierceTrialPolicy policy){
   if(equipment==null||node==null||policy==null)throw new ArgumentNullException("Verified original equipment and node required.");
   bool multishot;
   switch(comparisonGroup){case "A_BASELINE":case "B_PIERCE":multishot=false;break;case "C_MULTISHOT":case "D_COMBINED":multishot=true;break;default:throw new ArgumentException("Approved comparison group required.");}
   var originalEquipment=equipment.Capture();var nodePacket=node.Capture();var origin=node.OriginalSource.Capture()["origin"];
   if(equipment.AccountId!=(string)origin["accountId"]||equipment.RunId!=(string)origin["runId"])throw new ArgumentException("Original account/run node conflict.");
   // Run IDs alone are not a same-origin proof (isolated producers may reuse them).
   // This independently pinned native run evidence is additional provenance; the old
   // source-referenced run snapshot/hash below remains byte-for-byte unchanged.
   if(independentlyTrustedNativeRun==null||BDirectAttackInput.Digest(independentlyTrustedNativeRun)!=nativeRunEvidenceHash
    ||(string)independentlyTrustedNativeRun["runId"]!=equipment.RunId
    ||!JToken.DeepEquals(independentlyTrustedNativeRun["bCombatPin"]?["origin"],origin)
    ||(string)independentlyTrustedNativeRun["equipmentDeparturePin"]?["pinHash"]!=(string)equipment.Source["departurePinHash"]
    ||(string)independentlyTrustedNativeRun["equipmentDeparturePin"]?["originHash"]!=(string)equipment.Source["originalComponent"]["departureOriginHash"])
    throw new ArgumentException("Native run origin/equipment departure evidence conflict.");
   var mira=equipment.Mira;var cars=(JArray)mira["refit"]["departure"]["cars"];
   var operators=cars.Where(c=>(string)c["occupant"]["kind"]=="OPERATOR").ToArray();
   if(operators.Length!=1||(string)operators[0]["occupant"]["operatorId"]!="op_mira"||(string)mira["actor"]["abilityId"]!="op_mira.basic")throw new ArgumentException("Approved single Mira BASIC required.");
   if(equipment.CoefficientBps!=(multishot?2500:0))throw new ArgumentException("Original equipment differs from approved comparison group.");
   var source=equipment.Source;var policyPacket=policy.Capture();
   var research=(JObject)policyPacket["research"].DeepClone();research["policyVersion"]=BMiraPierceTrialPolicy.Version;research["modifiers"]=JValue.CreateNull();research["ledgerEvidence"]=JValue.CreateNull();
   var critical=(JObject)policyPacket["critical"].DeepClone();critical["policyVersion"]=BMiraPierceTrialPolicy.Version;
   var result=new JObject{
    ["version"]=Version,["policyVersion"]=BMiraPierceTrialPolicy.Version,["policySource"]="31_AUGMENT_EFFECT_REFERENCE.md#"+Policy,
    ["environment"]="LOCAL",["comparisonGroup"]=comparisonGroup,["profile"]=BMiraPierceTrialPolicy.Profile,["trialPolicy"]=policyPacket,["trialPolicyHash"]=policy.Hash,
    ["status"]="AWAITING_FORMAL_OWNED_DEPARTURE_AND_TRIAL_CONTEXT",["runtimeSupported"]=false,["canStart"]=false,
    ["accountId"]=equipment.AccountId,["runId"]=equipment.RunId,
    ["research"]=research,
    ["critical"]=critical,
    ["originalEquipmentContextHash"]=equipment.Hash,["originalEquipmentContext"]=originalEquipment,
    ["originalNodeSourceHash"]=node.SourceHash,["originalNodeHash"]=BDirectAttackInput.Digest(nodePacket),["originalNode"]=nodePacket,
    ["nativeRunEvidenceHash"]=nativeRunEvidenceHash,["nativeRunEvidence"]=independentlyTrustedNativeRun.DeepClone(),
    ["comparisonEquipment"]=new JObject{["classification"]="VERIFIED_EXISTING_BASELINE_NOT_NEW_TRIAL_START",["coefficientBps"]=equipment.CoefficientBps,["effectiveStats"]=equipment.Stats,["originalPassive"]=source["passive"].DeepClone()},
    ["requiredGroupSource"]=((JArray)policyPacket["groups"]).Single(g=>(string)g["key"]==comparisonGroup.Substring(0,1)).DeepClone(),
    ["effectOwnershipEstablished"]=false,["pierceOwnedSource"]=JValue.CreateNull(),["completedAttack"]=JValue.CreateNull(),
    ["missing"]=new JArray("SERVER_FORMAL_OWNED_DEPARTURE_AND_TRIAL_CONTEXT_WRITER","STRICT_NEW_TRIAL_INTAKE_AND_NATIVE_ATOMIC_ADAPTER")
   };
   if(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(result))>2*1024*1024)throw new ArgumentException("Trial preparation capacity.");
   return new BMiraPierceTrialPreparation(result);
  }
  public static BMiraPierceTrialPreparation Restore(JObject saved,string independentHash,string expectedGroup,BMiraEquippedTrialContext originalEquipment,BBoardingEventsNodeSnapshot originalNode,JObject independentlyTrustedNativeRun,string nativeRunEvidenceHash,BMiraPierceTrialPolicy policy){
   if(saved==null||Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(saved))>2*1024*1024||BDirectAttackInput.Digest(saved)!=independentHash)throw new ArgumentException("Trial preparation independent pin.");
   var restored=Prepare(expectedGroup,originalEquipment,originalNode,independentlyTrustedNativeRun,nativeRunEvidenceHash,policy);
   if(!JToken.DeepEquals(restored.original,saved))throw new ArgumentException("Trial preparation policy/group/original source conflict.");
   return restored;
  }
 }
}