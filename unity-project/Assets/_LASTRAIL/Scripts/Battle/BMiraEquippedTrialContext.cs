using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Immutable LOCAL trial input. The caller must retain the independently authenticated
 // original START, original Mira and run snapshot pins; a self-rehashed client packet is not authority.
 public sealed class BMiraEquippedTrialContext {
  public const string Version="mira.equipped-trial-context.v1.20261016",Profile="mira-equipped-multishot-local.v1.20261015",Policy="mira-equipped-controlled-trial-approved-20261015";
  const string Catalog="equipment.multishot-catalog.v1.20261015";
  readonly JObject original;
  public string Hash{get;}
  public string AccountId=>(string)original["start"]["accountId"];
  public string RunId=>(string)original["start"]["runId"];
  internal JObject Source=>(JObject)original["start"]["source"].DeepClone();
  internal JObject Mira=>(JObject)Source["originalComponent"]["originalMira"].DeepClone();
  internal JObject Stats=>(JObject)Source["effectiveStats"].DeepClone();
  internal JObject Critical=>(JObject)Source["critical"].DeepClone();
  internal int CoefficientBps=>Source["selection"].Type==JTokenType.Null?0:(int)Source["selection"]["definitionSnapshot"]["coefficientBps"];
  public JObject Capture()=>(JObject)original.DeepClone();
  BMiraEquippedTrialContext(JObject start){original=new JObject{["version"]=Version,["profile"]=Profile,["accountId"]=start["accountId"].DeepClone(),["runId"]=start["runId"].DeepClone(),["critical"]=start["source"]["critical"].DeepClone(),["start"]=start.DeepClone()};Hash=BDirectAttackInput.Digest(original);}
  static void Need(bool v,string code){if(!v)throw new ArgumentException("mira_equipped_"+code);}
  static JObject Obj(JToken t)=>t as JObject??throw new ArgumentException("mira_equipped_object");
  static string Digest(JToken t)=>BDirectAttackInput.Digest(t).Substring(7);
  static string Body(JObject o,string key){var b=(JObject)o.DeepClone();b.Remove(key);return Digest(b);}
  static void Empty(JToken t)=>Need(t is JArray a&&a.Count==0,"unsupported");
  static void Fields(JObject o,params string[] names)=>BDirectAttackInput.Fields(o,names);
  static void Pin(JObject o,string field)=>Need((string)o[field]==Body(o,field),"hash");
  static void False(JToken t)=>Need(t?.Type==JTokenType.Boolean&&!(bool)t,"operating_flags");
  static void Null(JToken t)=>Need(t?.Type==JTokenType.Null,"not_applicable");
  static void Definition(JObject d,int maxStar=2){Fields(d,"version","appliedStar","coefficientBps","maxSecondaryTargets","radiusL","attackEvaluation","targetDefenseEvaluation");int star=(int)BDirectAttackInput.Nat(d,"appliedStar",1,maxStar);Need((string)d["version"]=="multishot-enchant-v1.20261015"&&(int)d["coefficientBps"]==new[]{1500,2500,4000,6000,8500}[star-1]&&(int)d["maxSecondaryTargets"]==2&&(string)d["attackEvaluation"]=="AT_FIRE"&&(string)d["targetDefenseEvaluation"]=="AT_IMPACT"&&JToken.DeepEquals(d["radiusL"],new JObject{["numerator"]=1,["denominator"]=2}),"definition");}
  public static BMiraEquippedTrialContext Read(JObject input,JObject trustedOriginal,string startHash,string accountId,string runId,JObject originalMira,string miraHash,string runSnapshotHash){
   Need(input!=null&&trustedOriginal!=null&&originalMira!=null,"missing");Need(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(input))<=1024*1024,"capacity");
   Fields(input,"version","accountId","runId","trialId","profile","environment","requestHash","originalMiraHash","sourceHash","originalRunSnapshotHash","startHash","source");
   Need(JToken.DeepEquals(input,trustedOriginal)&&(string)input["startHash"]==startHash,"original_conflict");
   var start=(JObject)input.DeepClone();start.Remove("source");Pin(start,"startHash");
   Need((string)input["version"]=="equipment.multishot-local-trial-start.v1.20261015"&&(string)input["accountId"]==accountId&&(string)input["runId"]==runId&&(string)input["profile"]==Profile&&(string)input["environment"]=="LOCAL"&&(string)input["originalMiraHash"]==miraHash&&(string)input["originalRunSnapshotHash"]==runSnapshotHash,"start_binding");
   BDirectAttackInput.Text(input,"trialId");BDirectAttackInput.Text(input,"requestHash");
   var s=Obj(input["source"]);Fields(s,"version","accountId","runId","wearerId","originalComponent","originalComponentHash","departurePinHash","inventoryRevision","inventoryHash","catalogBinding","slots","selection","blocked","status","operatingCatalogBound","effectiveStats","startBinding","missing","runtimeSupported","canStart","profile","environment","catalogBound","research","critical","coreStats","staticStatsReady","unsupported","passive","packetHash");Pin(s,"packetHash");
   Need((string)s["version"]=="equipment.multishot-equipped-12-source.v2.20261015"&&(string)s["packetHash"]==(string)input["sourceHash"]&&(string)s["accountId"]==accountId&&(string)s["runId"]==runId&&(string)s["wearerId"]=="op_mira"&&(string)s["profile"]==Profile&&(string)s["environment"]=="LOCAL"&&(string)s["startBinding"]=="IMMUTABLE_LOCAL_TRIAL_START"&&s["staticStatsReady"]?.Type==JTokenType.Boolean&&(bool)s["staticStatsReady"]&&s["catalogBound"]?.Type==JTokenType.Boolean&&(bool)s["catalogBound"],"source_binding");
   False(s["runtimeSupported"]);False(s["canStart"]);False(s["operatingCatalogBound"]);Empty(s["blocked"]);Empty(s["unsupported"]);
   foreach(var key in new[]{"research","critical"}){var p=Obj(s[key]);if(key=="research")Fields(p,"policyVersion","policySource","status","modifiers","ledgerEvidence");else Fields(p,"policyVersion","policySource","status","chanceBps","rngDraws");Need((string)p["policyVersion"]==Policy&&(string)p["policySource"]=="44_COMBAT_DETAIL_CONTRACTS.md#"+Policy&&(string)p["status"]=="NOT_APPLICABLE","policy");if(key=="research"){Null(p["modifiers"]);Null(p["ledgerEvidence"]);}else{Null(p["chanceBps"]);Need(BDirectAttackInput.Nat(p,"rngDraws")==0,"rng");}}
   var c=Obj(s["originalComponent"]);Pin(c,"packetHash");Need((string)c["packetHash"]==(string)s["originalComponentHash"]&&(string)c["version"]=="combat.mira-effective-source.v1.20261015"&&(string)c["accountId"]==accountId&&(string)c["runId"]==runId&&(string)c["originalMiraHash"]==miraHash&&(string)c["runSnapshotHash"]==runSnapshotHash&&JToken.DeepEquals(c["departurePinHash"],s["departurePinHash"]),"component");False(c["runtimeSupported"]);False(c["canStart"]);Null(c["effectiveStats"]);
   var mira=BMiraBasicSource.Read(Obj(c["originalMira"]),originalMira,miraHash,accountId).Capture();
   var commander=c["commander"]["snapshot"];if(commander.Type!=JTokenType.Null)Need(Obj(commander["ranks"]).Count==0,"commander");
   var passive=Obj(s["passive"]);Need(JToken.DeepEquals(passive,c["passive"])&&(string)passive["applicationOwner"]=="ORIGINAL_MODE_COMBAT_FIRE_OWNER_ONCE"&&(int)passive["nonEquipmentOutgoingCapBps"]==3000&&(string)passive["effect"]["sourceId"]=="passive:op_mira:OWN_BASIC"&&(string)passive["effect"]["group"]=="NON_EQUIPMENT_OUTGOING"&&(int)passive["effect"]["valueBps"]==1000,"passive");False(passive["appliedToStaticStats"]);
   var catalog=Obj(s["catalogBinding"]);Pin(catalog,"bindingHash");Need((string)catalog["version"]=="equipment.multishot-formal-binding.v1.20261015"&&(string)catalog["sourceStatus"]=="APPROVED_CODE_CATALOG_NOT_PUBLISHED"&&(string)catalog["effectId"]=="EN17"&&(string)catalog["definitionVersion"]==Catalog&&(string)catalog["catalogVersion"]==Catalog&&(string)catalog["logicalDefinitionVersion"]=="multishot-enchant-v1.20261015","catalog");var rows=(JArray)catalog["rows"];Need(rows.Count==5,"catalog_rows");for(int i=0;i<5;i++){Need((int)rows[i]["appliedStar"]==i+1&&(string)rows[i]["effectId"]=="EN17"&&(string)rows[i]["definitionVersion"]==Catalog,"catalog_row");Definition(Obj(rows[i]["definitionSnapshot"]),5);}
   var slots=s["slots"] as JArray;Need(slots!=null&&slots.Count==3,"slots");var identities=new HashSet<string>(StringComparer.Ordinal);var tags=new Dictionary<string,int>();var groups=new Dictionary<string,int>{{"HP",0},{"ATK",0},{"DEF",0}};var candidates=new List<JObject>();
   for(int index=0;index<3;index++){if(slots[index].Type==JTokenType.Null)continue;var slot=Obj(slots[index]);var item=Obj(slot["instance"]);var effect=Obj(slot["effect"]);var snap=Obj(effect["snapshot"]);string id=(string)slot["instanceId"],tag=(string)item["set_tag"],category=(string)item["category_id"];int star=(int)BDirectAttackInput.Nat(item,"star",1,2);
    Need(identities.Add(id)&&id==(string)item["instance_id"]&&(string)item["location_kind"]=="EQUIPPED"&&(string)item["wearer_kind"]=="operator"&&(string)item["wearer_id"]=="op_mira"&&(int)item["slot_index"]==index&&(string)item["definition_version"]==RunEquipmentStatSource.Version&&groups.ContainsKey(category)&&(int)slot["currentStar"]==star,"ownership");
    BDirectAttackInput.Nat(item,"locked",0,1);BDirectAttackInput.Nat(effect,"effectRevision",1);Need((long)effect["instanceRevision"]==(long)item["revision"]&&(long)effect["inventoryRevision"]<=(long)s["inventoryRevision"],"revision");
    Need((string)snap["version"]=="equipment.effect-state.v1"||(string)snap["version"]=="equipment.effect-state.v2.20261015","envelope");Empty(snap["uniqueEffects"]);Need((string)snap["equipment"]["definitionId"]==(string)item["definition_id"]&&(string)snap["equipment"]["definitionVersion"]==(string)item["definition_version"]&&(int)snap["equipment"]["star"]==star,"equipment");
    foreach(var h in new[]{"originalHash","payloadHash"})Need(System.Text.RegularExpressions.Regex.IsMatch((string)slot[h]??"","^[a-f0-9]{64}$"),"effect_hash");BDirectAttackInput.Text(slot,"sourceRef");
    var enchant=snap["enchantment"];if(enchant.Type!=JTokenType.Null){Need((string)snap["version"]=="equipment.effect-state.v2.20261015"&&(string)enchant["effectId"]=="EN17"&&(string)enchant["definitionVersion"]==Catalog,"enchantment");var d=Obj(enchant["definitionSnapshot"]);Definition(d);Need((int)enchant["appliedStar"]==(int)d["appliedStar"]&&(int)d["appliedStar"]<=star,"procedure_star");candidates.Add(new JObject{["instanceId"]=id,["definitionSnapshot"]=d.DeepClone()});}
    tags.TryGetValue(tag,out var count);tags[tag]=count+1;groups[category]+=star==1?1000:2500;
   }
   Need(tags.All(t=>t.Key=="SET01"?t.Value<=1:t.Key=="SET04"&&t.Value<=2),"set");if(tags.ContainsKey("SET01"))groups["ATK"]+=400;if(tags.ContainsKey("SET04")){groups["HP"]+=600;if(tags["SET04"]>=2)groups["DEF"]+=1000;}
   var selected=candidates.OrderByDescending(x=>(int)x["definitionSnapshot"]["coefficientBps"]).ThenBy(x=>(string)x["instanceId"],StringComparer.Ordinal).FirstOrDefault();Need(JToken.DeepEquals(s["selection"],selected==null?JValue.CreateNull():(JToken)selected)&&(string)s["status"]==(selected==null?"VERIFIED_NO_ENCHANTMENT":"FORMAL_CATALOG_SELECTED"),"selection");
   var permanent=mira["actor"]["owned"]["permanentStats"];var stats=Obj(s["effectiveStats"]);Fields(stats,"maxHp","attack","defense","basicIntervalMs");var core=Obj(s["coreStats"]);var basis=Obj(c["base"]);Need((long)basis["maxHp"]==(long)permanent["maxHp"]&&JToken.DeepEquals(basis["attack"],permanent["attack"])&&JToken.DeepEquals(basis["defense"],CombatExactValue.Create((long)permanent["defense"],1).Snapshot()),"permanent");
   foreach(var pair in groups){Need((int)core["additiveBps"][pair.Key]==pair.Value,"group");string key=pair.Key=="HP"?"maxHp":pair.Key=="ATK"?"attack":"defense";var b=key=="maxHp"?CombatExactValue.Create((long)basis[key],1):CombatExactValue.From(Obj(basis[key]));var expected=b.Multiply(CombatExactValue.Create(10000+pair.Value,10000));if(key=="maxHp")Need((long)stats[key]==expected.Floor()&&JToken.DeepEquals(core["exactMaxHp"],expected.Snapshot()),"hp");else Need(JToken.DeepEquals(stats[key],expected.Snapshot()),"stats");Need(JToken.DeepEquals(stats[key],core[key]),"core");}
   Need((long)stats["basicIntervalMs"]==(long)permanent["basicIntervalMs"],"period");return new BMiraEquippedTrialContext(input);
  }
 }
 // Shared native flight owner consumes either explicit context through this small immutable adapter.
 internal sealed class BMiraNativeTrialBinding {
  readonly BMiraFirstTrialContext first;readonly BMiraEquippedTrialContext equipped;
  internal BMiraNativeTrialBinding(BMiraFirstTrialContext v){first=v??throw new ArgumentNullException(nameof(v));}
  internal BMiraNativeTrialBinding(BMiraEquippedTrialContext v){equipped=v??throw new ArgumentNullException(nameof(v));}
  internal string AccountId=>equipped?.AccountId??first.Source.AccountId;
  internal string RunId=>equipped?.RunId??first.Source.RunId;
  internal string Hash=>equipped?.Hash??first.Hash;
  internal bool Equipped=>equipped!=null;
  internal int CoefficientBps=>equipped?.CoefficientBps??0;
  internal JObject Mira=>equipped?.Mira??(JObject)first.Source.Capture()["originalComponent"]["originalMira"];
  internal JObject Stats=>equipped?.Stats??first.Source.StaticStats();
  internal JObject Critical=>equipped?.Critical??(JObject)first.Capture()["critical"];
  internal JObject Capture()=>equipped?.Capture()??first.Capture();
 }
}
