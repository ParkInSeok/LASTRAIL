using System;
using System.Linq;
using System.Text;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // Read-only authenticated projection. sourceHash refers to the Server's original
 // source, so it deliberately cannot authenticate these projection bytes.
 public sealed class EquipmentMultishotModel {
  public const string Version="equipment.multishot-presentation.v1.20261016";
  readonly JObject original;
  public string AccountId=>(string)original["accountId"];
  public string RunId=>(string)original["runId"];
  public string Basis=>(string)original["basis"];
  public JObject Capture()=>(JObject)original.DeepClone();
  EquipmentMultishotModel(JObject p){original=(JObject)p.DeepClone();}
  static void Need(bool yes,string code){if(!yes)throw new ArgumentException("equipment_multishot_display_"+code);}
  static bool Null(JToken t)=>t?.Type==JTokenType.Null;
  static void Hash(JToken t)=>Need(t?.Type==JTokenType.String&&System.Text.RegularExpressions.Regex.IsMatch((string)t,"^[a-f0-9]{64}$"),"hash");
  static void False(JToken t)=>Need(t?.Type==JTokenType.Boolean&&!(bool)t,"operating_scope");
  static int Star(JObject p,string key)=>(int)BDirectAttackInput.Nat(p,key,1,5);
  static void Procedure(JObject p){if(Null(p["appliedStar"])){Need(Null(p["coefficientBps"]),"absent_procedure");return;}int star=Star(p,"appliedStar");Need((int)BDirectAttackInput.Nat(p,"coefficientBps",1,10000)==new[]{1500,2500,4000,6000,8500}[star-1]&&star<=Star(p,"currentStar")&&(string)p["effectId"]=="EN17"&&(string)p["definitionVersion"]=="equipment.multishot-catalog.v1.20261015","procedure");}
  public static EquipmentMultishotModel Read(JObject p,JObject trustedOriginal,string independentlyPinnedProjectionHash,string accountId,string runId)=>ReadCore(p,trustedOriginal,independentlyPinnedProjectionHash,accountId,runId,false);
  internal static EquipmentMultishotModel ReadPrepared(JObject p,JObject trustedOriginal,string independentlyPinnedProjectionHash,string accountId,string runId)=>ReadCore(p,trustedOriginal,independentlyPinnedProjectionHash,accountId,runId,true);
  static EquipmentMultishotModel ReadCore(JObject p,JObject trustedOriginal,string independentlyPinnedProjectionHash,string accountId,string runId,bool prepared){
   Need(p!=null&&trustedOriginal!=null&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(p))<=1024*1024,"capacity");
   Need(JToken.DeepEquals(p,trustedOriginal)&&BDirectAttackInput.Digest(p)==independentlyPinnedProjectionHash,"original_projection_conflict");
   Need((string)p["version"]==Version&&(string)p["accountId"]==accountId&&(string)p["runId"]==runId,"identity");BDirectAttackInput.Text(p,"accountId");BDirectAttackInput.Text(p,"runId");False(p["runtimeSupported"]);False(p["canStart"]);Hash(p["sourceHash"]);
   string basis=(string)p["basis"],status=(string)p["status"];Need(prepared?basis=="PROPOSED_PLACEMENT_NOT_COMMITTED":basis!="PROPOSED_PLACEMENT_NOT_COMMITTED","prepared_root_required");
   if(basis=="CURRENT_VERIFIED_STORED_ITEM"){
    BDirectAttackInput.Fields(p,"version","basis","accountId","runId","instanceId","locationKind","status","currentStar","appliedStar","coefficientBps","effectId","definitionVersion","instanceRevision","effectRevision","inventoryRevision","sourceHash","hashBasis","selectionStatus","runtimeSupported","canStart");
    Need((string)p["locationKind"]=="STORED"&&(string)p["hashBasis"]=="VERIFIED_STORED_INVENTORY_AND_EFFECT_READ"&&(string)p["selectionStatus"]=="DETERMINED_ONLY_AFTER_VERIFIED_WEARER_CAPTURE"&&new[]{"NOT_EQUIPPED","NO_MULTISHOT","SOURCE_UNAVAILABLE"}.Contains(status),"stored_scope");Star(p,"currentStar");Procedure(p);if(status=="NOT_EQUIPPED")Need(!Null(p["coefficientBps"]),"stored_effect");
   }else{
    BDirectAttackInput.Fields(p,"version","basis","accountId","runId","wearerId","profile","environment","status","selectedInstanceId","coefficientBps","slots","reasons","sourceHash","startHash","inventoryRevision","inventoryHash","originalMiraHash","departurePinHash","effectiveStats","attackScope","supportScope","runtimeSupported","canStart");
    Need(new[]{"CURRENT_VERIFIED_EQUIPMENT","ORIGINAL_LOCAL_TRIAL_START","PROPOSED_PLACEMENT_NOT_COMMITTED"}.Contains(basis)&&(string)p["wearerId"]=="op_mira"&&(string)p["profile"]==BMiraEquippedTrialContext.Profile&&(string)p["environment"]=="LOCAL"&&(string)p["supportScope"]=="ISOLATED_LOCAL_TRIAL_ONLY"&&(string)p["attackScope"]=="VERIFIED_MIRA_PROJECTILE_BASIC_ONLY","basis_scope");
    Need(new[]{"VERIFIED_SELECTED","VERIFIED_NO_ENCHANTMENT","SOURCE_UNAVAILABLE"}.Contains(status),"status");if(basis=="ORIGINAL_LOCAL_TRIAL_START")Hash(p["startHash"]);else Need(Null(p["startHash"]),"current_is_not_departure");
    foreach(var key in new[]{"inventoryHash","originalMiraHash","departurePinHash"})Hash(p[key]);BDirectAttackInput.Nat(p,"inventoryRevision",1);
    var slots=p["slots"] as JArray;Need(slots!=null&&slots.Count==3&&p["reasons"] is JArray,"slots");var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);int applied=0;
    for(int i=0;i<3;i++){if(Null(slots[i]))continue;var row=slots[i] as JObject;Need(row!=null,"row");BDirectAttackInput.Fields(row,"slotIndex","instanceId","currentStar","appliedStar","coefficientBps","effectId","definitionVersion","instanceRevision","effectRevision","originalHash","payloadHash","sourceRef","status","basicStatsRemainEquipped");
     Need(BDirectAttackInput.Nat(row,"slotIndex",0,2)==i&&ids.Add(BDirectAttackInput.Text(row,"instanceId")),"slot_identity");Star(row,"currentStar");Procedure(row);BDirectAttackInput.Nat(row,"instanceRevision",1);if(!Null(row["effectRevision"]))BDirectAttackInput.Nat(row,"effectRevision",1);foreach(var key in new[]{"originalHash","payloadHash"})if(!Null(row[key]))Hash(row[key]);
     string state=(string)row["status"];Need(new[]{"APPLIED","LOWER_EFFECT","DUPLICATE_HIGHEST","NO_ENCHANTMENT","SOURCE_UNAVAILABLE"}.Contains(state),"row_status");
     if(status=="SOURCE_UNAVAILABLE")Need(state=="SOURCE_UNAVAILABLE"&&Null(row["basicStatsRemainEquipped"]),"unknown_is_not_zero");else{
      Need(row["basicStatsRemainEquipped"]?.Type==JTokenType.Boolean&&(bool)row["basicStatsRemainEquipped"],"base_still_equipped");
      if(state=="APPLIED"){applied++;Need((string)row["instanceId"]==(string)p["selectedInstanceId"]&&JToken.DeepEquals(row["coefficientBps"],p["coefficientBps"]),"selected");}
      else if(state=="DUPLICATE_HIGHEST")Need(!Null(row["coefficientBps"])&&JToken.DeepEquals(row["coefficientBps"],p["coefficientBps"]),"duplicate");
      else if(state=="LOWER_EFFECT")Need(!Null(row["coefficientBps"])&&!Null(p["coefficientBps"])&&(int)row["coefficientBps"]<(int)p["coefficientBps"],"lower");
      else Need(state=="NO_ENCHANTMENT"&&Null(row["appliedStar"]),"no_enchantment");
     }
    }
    Need(status=="VERIFIED_SELECTED"?applied==1&&!Null(p["coefficientBps"]):applied==0&&Null(p["selectedInstanceId"])&&Null(p["coefficientBps"]),"selection_status");
   }
   return new EquipmentMultishotModel(p);
  }
  static string Percent(JToken bps)=>Null(bps)?"":((decimal)(int)bps/100).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"%";
  static string Status(string s)=>s switch{"APPLIED"=>"적용 중","LOWER_EFFECT"=>"더 높은 효과 적용 중","DUPLICATE_HIGHEST"=>"동일 효과 중복 · 1개 적용","NO_ENCHANTMENT"=>"인챈트 없음","NOT_EQUIPPED"=>"장착 시 적용 여부 결정","NO_MULTISHOT"=>"다중 발사 없음",_=>"효과 상태 확인 불가"};
  internal static JObject RenderSlot(JObject p,int index)=>Row(p,index);
  static JObject Row(JObject p,int index){if(p==null)return new JObject{["empty"]=true,["slotLabel"]="슬롯 "+(index+1),["statusLabel"]="빈 슬롯",["currentStar"]="",["appliedStar"]="",["coefficient"]=""};return new JObject{["empty"]=false,["slotLabel"]="슬롯 "+(index+1),["currentStar"]="장비 ★"+(int)p["currentStar"],["appliedStar"]=Null(p["appliedStar"])?"":"시술 당시 ★"+(int)p["appliedStar"],["coefficient"]=Null(p["coefficientBps"])?"":"추가 피해"+Percent(p["coefficientBps"]),["statusLabel"]=Status((string)p["status"])};}
  public JObject Render(){bool stored=Basis=="CURRENT_VERIFIED_STORED_ITEM";string status=(string)original["status"];return new JObject{["basisLabel"]=stored?"미장착 장비":Basis=="ORIGINAL_LOCAL_TRIAL_START"?"출발 시 적용":Basis=="PROPOSED_PLACEMENT_NOT_COMMITTED"?"장착 후 예상":"현재 장착",["summary"]=stored?Status(status):status=="VERIFIED_SELECTED"?"다중 발사 · 추가 피해"+Percent(original["coefficientBps"]):status=="VERIFIED_NO_ENCHANTMENT"?"다중 발사 없음":"효과 상태 확인 불가",["slots"]=stored?new JArray(Row(original,0)):new JArray(((JArray)original["slots"]).Select((t,i)=>Row(t as JObject,i))),["replacementPreviewAvailable"]=false};}
 }
}
