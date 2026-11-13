using System;
using System.Linq;
using System.Text;
using System.Numerics;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // Authenticated read result for one ORIGINAL placement command. Never an authority
 // token, committed receipt, native equipment context or checkpoint input.
 public sealed class EquipmentMultishotPlacementPreviewModel {
  public const string Version="equipment.multishot-placement-preview.v1.20261016";
  readonly JObject original,command;
  readonly EquipmentMultishotModel before,after;
  public string AccountId{get;}
  public string RunId{get;}
  public bool AlreadyCommitted=>(string)original["status"]=="ALREADY_COMMITTED";
  public bool CanApplyToCombat=>false;
  internal JObject BindingSnapshot=>before?.Capture();
  public JObject Capture()=>(JObject)original.DeepClone();
  public JObject OriginalCommand()=>(JObject)command.DeepClone();
  EquipmentMultishotPlacementPreviewModel(JObject p,JObject c,string account,string run,EquipmentMultishotModel b,EquipmentMultishotModel a){original=(JObject)p.DeepClone();command=(JObject)c.DeepClone();AccountId=account;RunId=run;before=b;after=a;}
  static void Need(bool v,string code){if(!v)throw new ArgumentException("equipment_preview_"+code);}
  static JObject Obj(JToken t)=>t as JObject??throw new ArgumentException("equipment_preview_object");
  static bool Null(JToken t)=>t?.Type==JTokenType.Null;
  static void Hash(JToken t)=>Need(t?.Type==JTokenType.String&&System.Text.RegularExpressions.Regex.IsMatch((string)t,"^[a-f0-9]{64}$"),"hash");
  static void Fields(JObject p,params string[] fields)=>BDirectAttackInput.Fields(p,fields);
  static long Nat(JObject p,string key,long min=1,long max=9007199254740990)=>BDirectAttackInput.Nat(p,key,min,max);
  static void Target(JToken t){if(Null(t))return;var p=Obj(t);Fields(p,"kind","wearerId","slotIndex");Need((string)p["kind"]=="operator"&&(string)p["wearerId"]=="op_mira","wearer");Nat(p,"slotIndex",0,2);}
  static void Command(JObject c){Fields(c,"commandId","instanceId","expectedInventoryRevision","expectedInstanceRevision","target","expectedTargetInstanceId");BDirectAttackInput.Text(c,"commandId");BDirectAttackInput.Text(c,"instanceId");Nat(c,"expectedInventoryRevision");Nat(c,"expectedInstanceRevision");Target(c["target"]);if(!Null(c["expectedTargetInstanceId"]))BDirectAttackInput.Text(c,"expectedTargetInstanceId");}
  static void Receipt(JObject r,JObject c){Fields(r,"version","commandId","instanceId","target","displacedInstanceId","inventoryRevision","instanceRevision","displacedInstanceRevision");Need((string)r["version"]=="equipment.placement.v1"&&JToken.DeepEquals(r["commandId"],c["commandId"])&&JToken.DeepEquals(r["instanceId"],c["instanceId"])&&JToken.DeepEquals(r["target"],c["target"])&&JToken.DeepEquals(r["displacedInstanceId"],c["expectedTargetInstanceId"]),"original_command");Target(r["target"]);Need(Nat(r,"inventoryRevision",2,9007199254740991)==Nat(c,"expectedInventoryRevision")+1&&(Nat(r,"instanceRevision",2,9007199254740991)==Nat(c,"expectedInstanceRevision")+1||Nat(r,"instanceRevision",2,9007199254740991)==Nat(c,"expectedInstanceRevision")+2),"receipt_revision");if(Null(r["displacedInstanceId"]))Need(Null(r["displacedInstanceRevision"]),"displacement");else Nat(r,"displacedInstanceRevision",2,9007199254740991);}
  public static EquipmentMultishotPlacementPreviewModel Read(JObject p,JObject trustedOriginal,string independentlyPinnedProjectionHash,string accountId,string runId,JObject originalCommand){
   Need(p!=null&&trustedOriginal!=null&&originalCommand!=null,"missing");Need(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(p))<=1024*1024,"capacity");Need(JToken.DeepEquals(p,trustedOriginal)&&BDirectAttackInput.Digest(p)==independentlyPinnedProjectionHash,"original_pin");Need(!string.IsNullOrWhiteSpace(accountId)&&!string.IsNullOrWhiteSpace(runId)&&(string)p["version"]==Version,"scope");Command(originalCommand);
   if((string)p["status"]=="ALREADY_COMMITTED"){
    Fields(p,"version","status","committed","receipt","before","after","effectChange");Need(p["committed"]?.Type==JTokenType.Boolean&&(bool)p["committed"]&&Null(p["before"])&&Null(p["after"])&&Null(p["effectChange"]),"replay");Receipt(Obj(p["receipt"]),originalCommand);return new EquipmentMultishotPlacementPreviewModel(p,originalCommand,accountId,runId,null,null);
   }
   Fields(p,"version","status","committed","receipt","placementProposal","proof","before","after","effectChange","proposedInventoryRevision","boundInventoryRevision","candidateSourceHash","proposedAssignments");Need((string)p["status"]=="PREVIEW"&&p["committed"]?.Type==JTokenType.Boolean&&!(bool)p["committed"]&&Null(p["receipt"]),"not_committed");
   Receipt(Obj(p["placementProposal"]),originalCommand);var proof=Obj(p["proof"]);Fields(proof,"sourceVersion","sourceRevision","sourceHash","allowed");BDirectAttackInput.Text(proof,"sourceVersion");Nat(proof,"sourceRevision",1,9007199254740991);Hash(proof["sourceHash"]);Need(proof["allowed"]?.Type==JTokenType.Boolean&&(bool)proof["allowed"],"readonly_authority");
   long bound=Nat(p,"boundInventoryRevision"),proposed=Nat(p,"proposedInventoryRevision",2,9007199254740991);Need(bound==Nat(originalCommand,"expectedInventoryRevision")&&proposed==bound+1&&(long)p["placementProposal"]["inventoryRevision"]==proposed,"bound_revision");Hash(p["candidateSourceHash"]);
   var b=Obj(p["before"]);var a=Obj(p["after"]);Need((string)b["basis"]=="CURRENT_VERIFIED_EQUIPMENT"&&(string)a["basis"]=="PROPOSED_PLACEMENT_NOT_COMMITTED"&&(long)b["inventoryRevision"]==bound&&(long)a["inventoryRevision"]==bound&&(string)a["sourceHash"]==(string)p["candidateSourceHash"]&&JToken.DeepEquals(a["originalMiraHash"],b["originalMiraHash"])&&JToken.DeepEquals(a["departurePinHash"],b["departurePinHash"]),"candidate_binding");
   // Existing placement owner unstages an equipped source before assigning a new
   // non-null slot, so that original transaction legitimately advances it twice.
   bool originallyEquipped=((JArray)b["slots"]).Any(row=>!Null(row)&&(string)row["instanceId"]==(string)originalCommand["instanceId"]);
   long sourceIncrement=originallyEquipped&&!Null(originalCommand["target"])?2:1;
   Need((long)p["placementProposal"]["instanceRevision"]==Nat(originalCommand,"expectedInstanceRevision")+sourceIncrement,"original_source_revision");
   if(!Null(originalCommand["expectedTargetInstanceId"])){var displaced=((JArray)b["slots"]).SingleOrDefault(row=>!Null(row)&&(string)row["instanceId"]==(string)originalCommand["expectedTargetInstanceId"]);Need(displaced!=null&&(long)p["placementProposal"]["displacedInstanceRevision"]==(long)displaced["instanceRevision"]+1,"displaced_revision");}
   var before=EquipmentMultishotModel.Read(b,Obj(trustedOriginal["before"]),BDirectAttackInput.Digest(trustedOriginal["before"]),accountId,runId);
   var after=EquipmentMultishotModel.ReadPrepared(a,Obj(trustedOriginal["after"]),BDirectAttackInput.Digest(trustedOriginal["after"]),accountId,runId);
   var assignments=p["proposedAssignments"] as JArray;Need(assignments!=null&&assignments.Count==1,"assignments");var assignment=Obj(assignments[0]);Fields(assignment,"wearerKind","wearerId","slots");Need((string)assignment["wearerKind"]=="operator"&&(string)assignment["wearerId"]=="op_mira"&&assignment["slots"] is JArray slots&&slots.Count==3,"assignment_owner");
   for(int i=0;i<3;i++){var slot=assignment["slots"][i];var row=a["slots"][i];if(Null(slot)){Need(Null(row),"empty_slot");continue;}var item=Obj(slot);Fields(item,"instanceId","definitionId","definitionVersion","categoryId","setTag","star");Need(!Null(row)&&(string)item["instanceId"]==(string)row["instanceId"]&&(long)item["star"]==(long)row["currentStar"],"assignment_row");BDirectAttackInput.Text(item,"definitionId");BDirectAttackInput.Text(item,"definitionVersion");BDirectAttackInput.Text(item,"categoryId");BDirectAttackInput.Text(item,"setTag");}
   string expected=(string)b["status"]=="SOURCE_UNAVAILABLE"||(string)a["status"]=="SOURCE_UNAVAILABLE"?"SOURCE_UNAVAILABLE":JToken.DeepEquals(b["coefficientBps"],a["coefficientBps"])?"UNCHANGED":Null(b["coefficientBps"])?"GAINED":Null(a["coefficientBps"])?"REMOVED":(int)a["coefficientBps"]>(int)b["coefficientBps"]?"INCREASED":"DECREASED";Need((string)p["effectChange"]==expected,"effect_change");
   return new EquipmentMultishotPlacementPreviewModel(p,originalCommand,accountId,runId,before,after);
  }
  static string Exact(JToken value){var p=CombatExactValue.From(Obj(value)).Snapshot();var n=BigInteger.Parse((string)p["numerator"]);var d=BigInteger.Parse((string)p["denominator"]);var whole=BigInteger.DivRem(n,d,out var remainder);if(remainder==0)return whole.ToString(System.Globalization.CultureInfo.InvariantCulture);var digits=BigInteger.DivRem(remainder*1000000,d,out var tail).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(6,'0').TrimEnd('0');return (tail==0?"":"약 ")+whole.ToString(System.Globalization.CultureInfo.InvariantCulture)+(digits.Length==0?"":"."+digits);}
  static JObject Stats(EquipmentMultishotModel model){var s=model?.Capture()["effectiveStats"];if(s==null||Null(s))return new JObject{["hp"]="최대 HP —",["attack"]="ATK —",["defense"]="DEF —"};return new JObject{["hp"]="최대 HP "+s["maxHp"].ToString(),["attack"]="ATK "+Exact(s["attack"]),["defense"]="DEF "+Exact(s["defense"])};}
  public JObject Render(){var b=before?.Render();if(b!=null)b["basisLabel"]="장착 전";return new JObject{["version"]="equipment.multishot-placement-preview-display.v1.20261016",["status"]=original["status"].DeepClone(),["statusLabel"]=AlreadyCommitted?"이미 처리된 장착 요청":"확정 전 미리보기",["appliesToCombat"]=false,["before"]=b==null?JValue.CreateNull():(JToken)b,["after"]=after==null?JValue.CreateNull():(JToken)after.Render(),["beforeStats"]=Stats(before),["afterStats"]=Stats(after)};}
 }
}
