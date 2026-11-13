using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Freezes verified next-selection/refit only. Authentication belongs to the supplied transport owner.
 // Remove this LOCAL bridge when an atomic operating START producer supplies this binding.
 public sealed class BLocalFlowCandidate {
  public const string Version="unity.combat-b.local-flow-candidate.v1";
  readonly JObject original;readonly BCarriageBuffDefinitions definitions;
  public string AccountId {get;}public string OriginalHash {get;}public bool StartReady=>false;
  public JObject Capture()=>(JObject)original.DeepClone();
  static void Need(bool ok,string code){if(!ok)throw new ArgumentException(code);}
  public BLocalFlowCandidate(string selectionAccount,BSelectionSnapshot selection,BRefitSnapshot refit,BRefitRequest request,JObject receipt){
   if(selection==null||refit==null||request==null||receipt==null)throw new ArgumentNullException("saved_sources");
   Need(selectionAccount==refit.AccountId&&request.AccountId==refit.AccountId,"b_local_flow_account");
   Need(selection.Selection!=null&&refit.Refit!=null&&selection.EligibilityError==null&&refit.EligibilityError==null,"b_local_flow_current_eligibility");
   var sel=selection.Capture();var current=refit.Capture();var accepted=request.ValidateReceipt(receipt);var departure=refit.Refit;
   Need(selection.Revision==refit.SelectionRevision,"b_local_flow_selection_revision");
   Need((string)sel["savedCatalog"]["version"]==selection.CatalogVersion&&(long)sel["savedCatalog"]["revision"]==selection.CatalogRevision,"b_local_flow_catalog_revision");
   Need((long)accepted["revision"]==refit.Revision&&(string)accepted["definitionHash"]==refit.DefinitionHash&&(string)accepted["sourceHash"]==refit.SourceHash&&JToken.DeepEquals(accepted["refit"],departure),"b_local_flow_current_receipt");
   var placements=(JArray)selection.Selection.Capture()["placements"];var cars=(JArray)departure["cars"];
   Need(placements.Count==cars.Count,"b_local_flow_placements");
   foreach(var car in cars){var placed=placements.SingleOrDefault(t=>(long)t["slot"]==(long)car["slot"]);
    Need(placed!=null&&(string)placed["carriageId"]==(string)car["carriageId"]&&JToken.DeepEquals(placed["occupant"],car["occupant"]),"b_local_flow_placement_binding");
    Need(((JArray)sel["ownedSource"]["carriages"]).Any(t=>(string)t==(string)car["carriageId"]),"b_local_flow_carriage_ownership");
    var catalog=((JArray)sel["catalog"]["carriages"]).SingleOrDefault(t=>(string)t["carriageId"]==(string)car["carriageId"]);
    Need(catalog!=null&&(long)car["slot"]<(long)sel["catalog"]["unlockedSlotCount"]&&((JArray)catalog["occupants"]).Any(t=>(string)t==(string)car["occupant"]["kind"]),"b_local_flow_catalog_binding");
    if((string)car["occupant"]["kind"]=="OPERATOR")Need(((JArray)sel["ownedSource"]["operators"]).Any(t=>(string)t==(string)car["occupant"]["operatorId"]),"b_local_flow_operator_ownership");
   }
   var wrapper=new JObject{["version"]=current["buffVersion"].DeepClone(),["definitions"]=current["definitions"].DeepClone()};definitions=new BCarriageBuffDefinitions(BasicCombatSimulation.StableJson(wrapper),refit.DefinitionHash);
   AccountId=refit.AccountId;original=new JObject{["version"]=Version,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["accountId"]=AccountId,["selection"]=sel,["refitRead"]=current,["originalRequest"]=request.Body,["receipt"]=accepted,["startReady"]=false};OriginalHash=BDirectAttackInput.Digest(original);
  }
  public BDirectAttackBasicLoopBinding Bind(BDirectAttackInput attack,JObject trustedContext,string trustedContextHash){
   if(attack==null)throw new ArgumentNullException(nameof(attack));
   Need(attack.OriginalAccountId==AccountId,"b_local_flow_attack_account");var departure=(JObject)original["receipt"]["refit"].DeepClone();var cars=(JArray)departure["cars"];var slots=attack.Formation.Slots;
   Need(cars.Count==slots.Count,"b_local_flow_attack_slot_count");
   for(int slot=0;slot<slots.Count;slot++){var car=cars.SingleOrDefault(t=>(long)t["slot"]==slot);Need(car!=null&&(string)car["instanceId"]==slots[slot].CarriageId,"b_local_flow_attack_physical_source_missing");
    Need((string)car["occupant"]["kind"]=="CARGO"?slots[slot].IsCargo:(string)car["occupant"]["operatorId"]==slots[slot].OperatorId,"b_local_flow_attack_actor_source_missing");}
   Need(trustedContext!=null&&BDirectAttackInput.Digest(trustedContext)==trustedContextHash,"b_local_flow_context_hash");BuffWire.Fields(trustedContext,"version","candidateHash","attackInputHash","initial","targetCategories","targets");
   Need((string)trustedContext["version"]==Version&&(string)trustedContext["candidateHash"]==OriginalHash&&(string)trustedContext["attackInputHash"]==attack.OriginalHash,"b_local_flow_context_binding");
   var compositionPacket=new JObject{["version"]=BDirectAttackBuffBinding.Version,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["attackInputHash"]=attack.OriginalHash,["definitionHash"]=definitions.Hash,["departure"]=departure,["initial"]=trustedContext["initial"].DeepClone(),["targetCategories"]=trustedContext["targetCategories"].DeepClone()};
   var composition=new BDirectAttackBuffBinding(attack,definitions,compositionPacket,BDirectAttackInput.Digest(compositionPacket));
   var loop=new JObject{["version"]=BDirectAttackBasicLoopBinding.Version,["provenance"]="BOUNDED_LOCAL_NOT_FULL_RUN",["compositionHash"]=composition.OriginalHash,["targets"]=trustedContext["targets"].DeepClone()};
   return new BDirectAttackBasicLoopBinding(composition,loop,BDirectAttackInput.Digest(loop));
  }
 }
}