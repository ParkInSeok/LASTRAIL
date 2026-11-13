using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Consumes a trusted server rule-port read; this is not authentication,
 // persisted selection, a B START definition or an admission grant.
 public sealed class BCombatPlacementReplica {
  public const string Version="combat.b-profile-rules.v1.20261002";
  readonly JObject replica;
  public bool StartReady=>false;
  public JObject Capture()=>(JObject)replica.DeepClone();
  public BCombatPlacementReplica(JObject result,JObject trustedSource,string expectedAccount){
   WorldBossRailSnapshot.Fields(result,"version","accountId","camera","interior","placements","operatorCount","mountedTurrets","startReady","unbound");WorldBossRailSnapshot.Fields(trustedSource,"accountId","selectedCarriages","ownedOperatorIds");
   Check((string)result["version"]==Version&&WorldBossRailSnapshot.Text(result,"accountId",128)==expectedAccount&&WorldBossRailSnapshot.Text(trustedSource,"accountId",128)==expectedAccount&&!string.IsNullOrWhiteSpace(expectedAccount));
   Check((string)result["camera"]=="VERTICAL_FIXED"&&(string)result["interior"]=="SPECIFIC_NODES_RETAINED"&&result["startReady"].Type==JTokenType.Boolean&&!(bool)result["startReady"]&&result["mountedTurrets"] is JArray turrets&&turrets.Count==0);
   Check(JToken.DeepEquals(result["unbound"],new JArray("B_PERSISTED_SELECTION_AND_START_BINDING","B_DEFINITIONS_AND_EXECUTION")));
   Check(result["placements"] is JArray&&trustedSource["selectedCarriages"] is JArray&&trustedSource["ownedOperatorIds"] is JArray);
   var rows=(JArray)result["placements"];var selected=(JArray)trustedSource["selectedCarriages"];var owned=(JArray)trustedSource["ownedOperatorIds"];
   // The frozen rule port currently accepts128 placements. The later approved
   // continuation requires TOTAL deployment<=5 and API slot0..4. Reject the
   // incompatible envelope instead of claiming the supplier enforces it.
   Check(rows.Count>=1&&rows.Count<=5&&rows.Count==selected.Count&&owned.Count<=1000);var sourceSlots=new HashSet<long>();foreach(var row in selected){WorldBossRailSnapshot.Fields(row as JObject,"slot","carriageId");Check(sourceSlots.Add(Slot(row)));WorldBossRailSnapshot.Text(row,"carriageId",128);}
   var owners=new HashSet<string>(StringComparer.Ordinal);foreach(var id in owned){Check(id.Type==JTokenType.String&&!string.IsNullOrEmpty((string)id)&&((string)id).Length<=128&&owners.Add((string)id));}
   var slots=new HashSet<long>();var operators=new HashSet<string>(StringComparer.Ordinal);long previous=-1;
   foreach(var row in rows){WorldBossRailSnapshot.Fields(row as JObject,"slot","carriageId","occupant");long slot=Slot(row);string car=WorldBossRailSnapshot.Text(row,"carriageId",128);Check(slots.Add(slot)&&slot>previous&&selected.Any(s=>(long)s["slot"]==slot&&(string)s["carriageId"]==car));previous=slot;var occupant=row["occupant"] as JObject;Check(occupant!=null);string kind=WorldBossRailSnapshot.Text(occupant,"kind");if(kind=="OPERATOR"){WorldBossRailSnapshot.Fields(occupant,"kind","operatorId");string id=WorldBossRailSnapshot.Text(occupant,"operatorId",128);Check(operators.Add(id)&&owners.Contains(id));}else{WorldBossRailSnapshot.Fields(occupant,"kind");Check(kind=="CARGO");}}
   Check(operators.Count>=1&&operators.Count<=5&&WorldBossRailSnapshot.Nat(result,"operatorCount")==operators.Count);replica=(JObject)result.DeepClone();
  }
  internal static void Check(bool value){if(!value)throw new ArgumentException("combat_b_replica_invalid");}
  static long Slot(JToken row){long slot=WorldBossRailSnapshot.Nat(row,"slot");Check(slot<=4);return slot;}
  // Catalog type IDs may repeat across slots. Physical instance IDs must be
  // supplied explicitly; never synthesize inventory objects from a rule fixture.
  public BCombatFormation BuildLocalTestFormation(string trainInstanceId,BCombatMode mode,IReadOnlyDictionary<int,string> actualInstanceBySlot){
   var rows=(JArray)replica["placements"];Check(actualInstanceBySlot!=null&&actualInstanceBySlot.Count==rows.Count);var slots=new List<BCombatSlot>();for(int i=0;i<rows.Count;i++){var row=rows[i];Check((int)row["slot"]==i&&actualInstanceBySlot.ContainsKey(i));slots.Add(new BCombatSlot(actualInstanceBySlot[i],(string)row["occupant"]["operatorId"]));}return new BCombatFormation(trainInstanceId,mode,BCombatBattlefield.ExternalVertical,slots);
  }
 }
 public sealed class BCombatPolicyClassification {
  readonly JObject original;
  public bool StartReady=>false;
  public bool AuthorizesHistoricalRestore=>false;
  public string Classification=>(string)original["classification"];
  public string OriginalPolicyVersion=>(string)original["policyVersion"];
  public JObject Capture()=>(JObject)original.DeepClone();
  public BCombatPolicyClassification(JObject request,JObject response){
   var fields=request?.Properties().Select(p=>p.Name).ToArray();BCombatPlacementReplica.Check(fields!=null&&fields.Contains("purpose")&&fields.All(p=>p=="purpose"||p=="snapshotPolicyVersion"));string purpose=WorldBossRailSnapshot.Text(request,"purpose");BCombatPlacementReplica.Check(purpose=="NEW_B"||purpose=="RESTORE");string version=request["snapshotPolicyVersion"]==null?null:WorldBossRailSnapshot.Text(request,"snapshotPolicyVersion",128);
   WorldBossRailSnapshot.Fields(response,"classification","policyVersion","startReady");BCombatPlacementReplica.Check(response["startReady"].Type==JTokenType.Boolean&&!(bool)response["startReady"]);BCombatPlacementReplica.Check(response["policyVersion"].Type==JTokenType.Null||response["policyVersion"].Type==JTokenType.String);BCombatPlacementReplica.Check(version==(string)response["policyVersion"]&&(string)response["classification"]==(version==BCombatPlacementReplica.Version?"B_RULES_ONLY":"HISTORICAL_PINNED")&&(purpose!="NEW_B"||version==BCombatPlacementReplica.Version));original=(JObject)response.DeepClone();
  }
 }
 public sealed class BCombatRuleError {
  public int Status {get;}public string Code {get;}
  static readonly IReadOnlyDictionary<string,int> codes=new Dictionary<string,int>{["combat_b_request_invalid"]=400,["combat_b_boundary_invalid"]=400,["combat_b_operator_not_owned"]=403,["combat_b_version_conflict"]=409,["combat_b_mounted_turret_forbidden"]=409,["combat_b_placement_duplicate"]=409,["combat_b_operator_limit"]=409,["combat_b_selected_carriage_mismatch"]=409,["combat_b_source_invalid"]=503};
  public BCombatRuleError(JObject source){WorldBossRailSnapshot.Fields(source,"status","errorCode");string code=WorldBossRailSnapshot.Text(source,"errorCode");long status=WorldBossRailSnapshot.Nat(source,"status");BCombatPlacementReplica.Check(codes.TryGetValue(code,out int expected)&&status==expected);Status=expected;Code=code;}
 }
}
