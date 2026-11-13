using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Immutable original inputs, not current HP, an execution capability, or a replacement START.
 public sealed class OrdinaryFiveStartInput {
  public const string KitVersion="combat.ordinary-five-kit.v1.20261023";
  public const string KitV2Version="combat.ordinary-five-kit.v2.20261102";
  public const string KitV3Version="combat.ordinary-five-kit.v3.20261107";
  const string NyxPolicyHash="f8b1b0a9474ccd7300cb88cd76e1e7256f1ddc86ec8604faa16ee52412f5a25c";
  const string ReinaBarrageKitHash="46e3879f5cda29c32ae302cf279047a9a1e3195962db34827d05113af1cc068c";
  const string KyleTauntV2KitHash="ba76bacc1052176533104b6ed28164e4df6735b7163b284b43ffa96cbb8b5b11";
  const string PolicyHash="a4ce53b746fa5d42eaaf0481f3c9a1ccc931a0ae50a176bd1bc192f9a60d875b";
  // Per-operator projections of the existing approved CombatDefinitionIntake publication (slot excluded).
  static readonly Dictionary<string,string> Kits=new Dictionary<string,string>(StringComparer.Ordinal){
   {"op_kyle","bfbfaa0e6cf3c4daac60eda11c182d00c6c871b6a3124b58bad50003b1f700f5"},
   {"op_reina","350442fb652d4311b455b156b5daefcfbe011f5cd6bd9e8619255fe95692f118"},
   {"op_eve","3e1c18482989d93d7c9e709c3dbbf3723b3eb20b2d5154c21387d9446f4b222d"},
   {"op_sera","ac5cb8c7122e4732309b43e4d9fb53d2a2c8f0498411c4539eee45ff2168f19e"},
   {"op_mira","41b145e605948a33e4b7c545e3a9bba07697fa162cbe068ceaf7acfc61bf319f"},
   {"op_nyx","12b1a6f91e685e3808fdfd1291820ff8c45b31c8690a6822c666f2c69c771dc3"}};
  public sealed class Actor {
   readonly JObject kit;
   public string OperatorId{get;} public int Slot{get;} public long PermanentMaxHp{get;} public long PermanentDefense{get;} public long BasicIntervalMs{get;}
   public CombatExactValue PermanentAttack{get;} public CombatExactValue SkillMagnitude{get;}
   internal Actor(JObject row,JObject definition){
    Fields(row,"operatorId","slot","permanentStats","skillMagnitude");OperatorId=Text(row,"operatorId");Slot=(int)Nat(row,"slot",0,4);
    var stats=Object(row["permanentStats"]);Fields(stats,"maxHp","attack","defense","basicIntervalMs");
    PermanentMaxHp=Nat(stats,"maxHp",1);PermanentDefense=Nat(stats,"defense");BasicIntervalMs=Nat(stats,"basicIntervalMs",1);
    PermanentAttack=CombatExactValue.From(Object(stats["attack"]));SkillMagnitude=CombatExactValue.From(Object(row["skillMagnitude"]));kit=(JObject)definition.DeepClone();
   }
   public JObject Ability(string id){var ability=((JArray)kit["abilities"]).SingleOrDefault(a=>(string)a["id"]==id);return ability is JObject o?(JObject)o.DeepClone():throw new ArgumentException("ordinary_ability_missing");}
   public JObject Passive=>(JObject)kit["passive"].DeepClone();
  }
  public string AccountId{get;} public string RunId{get;} public string OriginHash{get;} public string KitHash{get;}
  public string KitSourceVersion{get;}
  public string StatOriginVersion{get;}
  public string DisplayOperationId{get;}
  public JObject ReinaBarrage=>KitSourceVersion==KitV2Version||KitSourceVersion==KitV3Version?Actors.Single(a=>a.OperatorId=="op_reina").Ability("op_reina.ultimate"):null;
  public long FormationRevision{get;} public long LoadoutRevision{get;}
  public IReadOnlyList<Actor> Actors{get;}
  public OrdinaryBasicSource Basic{get;}
  public OrdinaryGeometryReference GeometryReference{get;}
  public OrdinaryRelocationSource Relocation{get;}
  public FirstBattleSource FirstBattle{get;}
  public OrdinaryTrainSource Train{get;}
  public OrdinaryRailSource Rail{get;}
  public OrdinaryRoofSource Roof{get;}
  public string StartSnapshotHash{get;}
  OrdinaryFiveStartInput(JObject start,string account,string run){
   Need(start!=null&&start["accepted"]?.Type==JTokenType.Boolean&&(bool)start["accepted"]&&Text(start,"startStatus")=="STARTED"&&Text(start,"runId")==run);
   DisplayOperationId = (start["rewardReference"] as JObject)?["operationId"]?.Type == JTokenType.String ? (string)start["rewardReference"]["operationId"] : null;
   var stats=Object(start["ordinaryStatSource"]);var source=Object(start["ordinaryKitSource"]);Fields(stats,"origin","originHash");Fields(source,"packet","kitHash");
   var origin=Object(stats["origin"]);var packet=Object(source["packet"]);
   Need(Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(origin))<=131072&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(packet))<=131072);
   Fields(origin,"version","accountId","runId","policy","ownedSourceHash","definitionVersion","definitionHash","formationRevision","loadoutRevision","applicability","actors");
   Fields(packet,"version","accountId","runId","originHash","policyVersion","policyHash","authority","execution","operators","requiredEffectKinds");
   AccountId=Text(origin,"accountId");RunId=Text(origin,"runId");Need(AccountId==account&&RunId==run&&Text(packet,"accountId")==account&&Text(packet,"runId")==run);
   OriginHash=Text(stats,"originHash");KitHash=Text(source,"kitHash");Need(OriginHash==Hash(origin)&&KitHash==Hash(packet)&&Text(packet,"originHash")==OriginHash);
   KitSourceVersion=Text(packet,"version");
   bool nyx=KitSourceVersion==KitV3Version;StatOriginVersion=Text(origin,"version");
   Need(StatOriginVersion==(nyx?OrdinaryFiveCoreStatSnapshot.NyxOriginVersion:OrdinaryFiveCoreStatSnapshot.Version)&&(nyx||KitSourceVersion==KitVersion||KitSourceVersion==KitV2Version)&&Text(packet,"execution")=="REQUIRES_MATCHING_UNITY_HANDLERS");
   string policyHash=nyx?NyxPolicyHash:PolicyHash;
   Need(Text(packet,"policyVersion")== (nyx?"combat.policy-catalog.nyx.2026-11-07.1":CombatDefinitionIntake.DefinitionVersion)&&Text(packet,"policyHash")==policyHash&&Hash(origin["policy"])==policyHash);
   Need(Regex.IsMatch(Text(origin,"ownedSourceHash"),"\\Asha256:[a-f0-9]{64}\\z")&&Regex.IsMatch(Text(origin,"definitionHash"),"\\Asha256:[a-f0-9]{64}\\z"));Text(origin,"definitionVersion");Object(origin["applicability"]);
   FormationRevision=Nat(origin,"formationRevision",1);LoadoutRevision=Nat(origin,"loadoutRevision",1);
   var admission=Object(start["admission"]);Need(Text(admission,"version")=="departure-v4"&&Nat(admission,"formationRevision",1)==FormationRevision&&Nat(admission,"loadoutRevision",1)==LoadoutRevision&&JToken.DeepEquals(admission["combatPolicyRequirements"],origin["policy"]));
   var authority=Object(packet["authority"]);Fields(authority,"version","mode","combatOwner","combatClock","transactionOwner","resultVerificationOwner","clientReportProvesCombat");
   Need(Text(authority,"version")=="combat.authority-split.v1"&&Text(authority,"mode")=="ORDINARY_SOLO"&&Text(authority,"combatOwner")=="UNITY_LOCAL"&&Text(authority,"combatClock")=="LOCAL_LOGICAL"&&Text(authority,"transactionOwner")=="SERVER"&&Text(authority,"resultVerificationOwner")=="SERVER"&&authority["clientReportProvesCombat"]?.Type==JTokenType.Boolean&&!(bool)authority["clientReportProvesCombat"]&&JToken.DeepEquals(authority,admission["combatAuthority"]));
   var rows=Five(origin["actors"]);var definitions=Five(packet["operators"]);var formation=Five(admission["formation"]?["operators"]);var actors=new Actor[5];var seen=new HashSet<string>();var effects=new HashSet<string>(StringComparer.Ordinal);
   foreach(var token in definitions){var definition=Object(token);Fields(definition,"operatorId","slot","role","passive","abilities");var id=Text(definition,"operatorId");var slot=(int)Nat(definition,"slot",0,4);Need(Kits.ContainsKey(id)&&(nyx?id!="op_mira":id!="op_nyx")&&seen.Add(id)&&actors[slot]==null);
    var pinned=(JObject)definition.DeepClone();pinned.Remove("slot");var pinnedHash=Hash(pinned);Need(pinnedHash==((KitSourceVersion==KitV2Version||nyx)&&id=="op_reina"?ReinaBarrageKitHash:Kits[id])||(id=="op_kyle"&&pinnedHash==KyleTauntV2KitHash));
    var original=rows.Where(r=>(string)r["operatorId"]==id).ToArray();var selected=formation.Where(r=>(string)r["operatorId"]==id).ToArray();Need(original.Length==1&&selected.Length==1&&Nat(original[0],"slot",0,4)==slot&&Nat(selected[0],"slot",0,4)==slot);
    actors[slot]=new Actor(Object(original[0]),pinned);foreach(var ability in (JArray)pinned["abilities"])effects.Add(Text(ability,"effect"));
   }
   Need(JToken.DeepEquals(packet["requiredEffectKinds"],new JArray(effects.OrderBy(x=>x,StringComparer.Ordinal))));Actors=Array.AsReadOnly(actors);
   if(nyx)Need(actors.Select(a=>a.OperatorId).SequenceEqual(OrdinaryFiveCoreStatSnapshot.NyxOrder));
   if(start.Property("ordinaryBasicSource")!=null)Basic=new OrdinaryBasicSource(start["ordinaryBasicSource"],this);
   if(start.Property("ordinaryRelocationSource")!=null)Relocation=new OrdinaryRelocationSource(start["ordinaryRelocationSource"],this,definitions.Select(d=>Text(d,"operatorId")));
   if(start.Property("firstBattleEncounterSource")!=null)FirstBattle=new FirstBattleSource(start["firstBattleEncounterSource"],this,(string)start["risk"]);
   if(start.Property("ordinaryTrainSource")!=null)Train=new OrdinaryTrainSource(start["ordinaryTrainSource"],this,Text(origin,"ownedSourceHash"));
   if(start.Property("ordinaryRailSource")!=null)Rail=new OrdinaryRailSource(start["ordinaryRailSource"],this,admission,Text(origin,"ownedSourceHash"));
   if(start.Property("ordinaryRoofSource")!=null)Roof=new OrdinaryRoofSource(start["ordinaryRoofSource"],this);
   if(start.Property("ordinaryGeometryReference")!=null)GeometryReference=new OrdinaryGeometryReference(start["ordinaryGeometryReference"]);
   StartSnapshotHash=Hash(start);
  }
  public static OrdinaryFiveStartInput Read(JObject originalStart,string expectedAccount,string expectedRun)=>new OrdinaryFiveStartInput(originalStart,expectedAccount,expectedRun);
  static JObject Object(JToken t)=>t as JObject??throw new ArgumentException("ordinary_start_object_missing");
  static JArray Five(JToken t){Need(t is JArray a&&a.Count==5);return (JArray)t;}
  static void Fields(JObject o,params string[] names)=>BDirectAttackInput.Fields(o,names);
  static string Text(JToken o,string key)=>BDirectAttackInput.Text(o,key);
  static long Nat(JToken o,string key,long min=0,long max=9007199254740991)=>BDirectAttackInput.Nat(o,key,min,max);
  static string Hash(JToken t)=>BDirectAttackInput.Digest(t).Substring(7);
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_start_source_conflict");}
 }
}
