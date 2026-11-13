using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using LastRail.Run;
namespace LastRail.Battle {
 // Explicit LOCAL bridge over original definition/stat snapshots. Not a START or enemy catalog.
 public sealed class BDirectAttackInput {
  public const string Version="unity.combat-b.direct-local.v1",EveVersion="unity.combat-b.direct-local.v2";
  public const string WaveRuntimeVersion="unity.combat-b.wave-runtime-input.v1.20261004";
  public const string GunnerInterceptLocalInputVersion="unity.combat-b.gunner-intercept-local-input.v1.20261004";
  public const string NamedLocalInputVersion="unity.combat-b.named-operator-local-input.v1.20261004";
  public const string EquippedMiraInputVersion="unity.combat-b.mira-equipped-wave-input.v1.20261016";
  internal bool IsWaveRuntime=>SchemaVersion==EquippedMiraInputVersion||SchemaVersion==WaveRuntimeVersion||SchemaVersion==GunnerInterceptLocalInputVersion||SchemaVersion==NamedLocalInputVersion;
  internal const string ActiveRuntimeVersion="combat.b-active-runtime-input.v1.20261003";
  internal sealed class Entity {
   internal readonly string Id,Kind,Definition;
   internal readonly int Slot,MaxHp;internal readonly long Interval;
   internal readonly CombatExactValue Attack,Defense;
   readonly JObject originalEntity;
   internal JObject Original=>(JObject)originalEntity.DeepClone();
   internal Entity(JObject x){
    originalEntity=(JObject)x.DeepClone();Id=Text(x,"id");Kind=Text(x,"kind");Definition=Text(x,"definitionId");
    Slot=(int)Nat(x,"slot",0,4);MaxHp=(int)Nat(x,"maxHp",1,int.MaxValue);Interval=Nat(x,"basicIntervalMicroseconds");
    Attack=x["attack"].Type==JTokenType.Null?null:CombatExactValue.From(x["attack"] as JObject);
    Defense=CombatStatResolver.EquipmentDefense(Id,CombatExactValue.From(x["defense"] as JObject),x["defenseSources"] as JArray);
    if(Attack!=null)CombatStatResolver.Attack(Id,Attack,x["attackSources"] as JArray);
   }
  }
  readonly JObject original;
  readonly Dictionary<string,Entity> entities=new Dictionary<string,Entity>(StringComparer.Ordinal);
  internal IReadOnlyDictionary<string,Entity> Entities=>new System.Collections.ObjectModel.ReadOnlyDictionary<string,Entity>(entities);
  readonly Dictionary<(string,string),JObject> geometry=new Dictionary<(string,string),JObject>();
  internal bool TryGetGeometry(string source,string target,out JObject frame){if(!geometry.TryGetValue((source,target),out var original)){frame=null;return false;}frame=(JObject)original.DeepClone();return true;}
  readonly Dictionary<string,JObject> conditionalDefense=new Dictionary<string,JObject>(StringComparer.Ordinal);
  internal long ConditionalDefenseFlat(string id,int hp)=>conditionalDefense.TryGetValue(id,out var source)&&(long)hp*2<Entities[id].MaxHp?(long)source["valueFlat"]:0;
  readonly Dictionary<string,JObject> basicPassives=new Dictionary<string,JObject>(StringComparer.Ordinal);
  public string SchemaVersion {get;}
  public BCombatFormation Formation {get;}
  public string OriginalHash {get;}
  public string OriginalAccountId {get;}
  public long StepTimeSpanTicks {get;}
  public bool OperationallyExecutable=>false;
  public IReadOnlyList<string> SupportedActions=>System.Array.AsReadOnly((SchemaVersion==GunnerInterceptLocalInputVersion||SchemaVersion==NamedLocalInputVersion)?entities.Values.Where(e=>e.Kind=="OPERATOR").OrderBy(e=>e.Id,StringComparer.Ordinal).Select(e=>e.Id+":BASIC").Concat(new[]{"LOCAL_ENEMY:BASIC_EXPLICIT_IMPACT"}).ToArray():SchemaVersion==EveVersion?new[]{"op_mira:BASIC","op_eve:BASIC","LOCAL_ENEMY:BASIC_EXPLICIT_IMPACT"}:new[]{"op_mira:BASIC","LOCAL_ENEMY:BASIC_EXPLICIT_IMPACT"});
  public JObject Capture()=>(JObject)original.DeepClone();
  public static string Digest(JToken x)=>"sha256:"+BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(x));
  internal static void Fields(JObject x,params string[] keys)=>WorldBossRailSnapshot.Fields(x,keys);
  internal static string Text(JToken x,string key,int max=128)=>WorldBossRailSnapshot.Text(x,key,max);
  internal static long Nat(JToken x,string key,long min=0,long max=9007199254740991){
   if(x?[key]?.Type!=JTokenType.Integer)throw new ArgumentException("b_attack_integer_required");
   long n=(long)x[key];if(n<min||n>max)throw new ArgumentException("b_attack_integer_range");return n;
  }
  internal static void Need(bool yes){if(!yes)throw new ArgumentException("b_attack_original_input_invalid");}
  static JArray Array(JToken x,int max){var a=x as JArray;Need(a!=null&&a.Count<=max);return a;}
  public BDirectAttackInput(JObject packet,string trustedPacketHash,BCombatFormation formation,
    JObject originalOwnedProfile,string trustedOwnedProfileHash,CombatDefinitionIndex originalDefinition):this(packet,trustedPacketHash,formation,originalOwnedProfile,trustedOwnedProfileHash,originalDefinition,false){}
  public static BDirectAttackInput ReadWaveRuntime(JObject packet,string hash,BCombatFormation formation,JObject profile,string profileHash,CombatDefinitionIndex definition,BScoutGunnerNodeSnapshot source)=>new BDirectAttackInput(packet,hash,formation,profile,profileHash,definition,false,0,source??throw new ArgumentNullException(nameof(source)));
  public static BDirectAttackInput ReadGunnerInterceptLocalRuntime(JObject packet,string hash,BCombatFormation formation,JObject profile,string profileHash,CombatDefinitionIndex definition,BScoutGunnerNodeSnapshot source)=>new BDirectAttackInput(packet,hash,formation,profile,profileHash,definition,false,0,source??throw new ArgumentNullException(nameof(source)),true);
  public static BDirectAttackInput ReadEquippedMiraWaveRuntime(JObject packet,string hash,BCombatFormation formation,JObject profile,string profileHash,CombatDefinitionIndex definition,BScoutGunnerNodeSnapshot source,BMiraEquippedTrialContext context)=>new BDirectAttackInput(packet,hash,formation,profile,profileHash,definition,false,0,source??throw new ArgumentNullException(nameof(source)),false,null,context??throw new ArgumentNullException(nameof(context)));
  internal static BDirectAttackInput ReadNamedRuntime(JObject packet,string hash,BCombatFormation formation,CombatDefinitionIndex definition,BScoutGunnerNodeSnapshot wave,BNamedOperatorSource named)=>new BDirectAttackInput(packet,hash,formation,(JObject)named.Capture()["ownedProfile"],"sha256:"+(string)named.Capture()["ownedProfileHash"],definition,false,0,wave,true,named);
  internal static BDirectAttackInput ReadActiveRuntime(JObject packet,string hash,BCombatFormation formation,JObject profile,string profileHash,CombatDefinitionIndex definition,long step)=>new BDirectAttackInput(packet,hash,formation,profile,profileHash,definition,true,step);
  BDirectAttackInput(JObject packet,string trustedPacketHash,BCombatFormation formation,JObject originalOwnedProfile,string trustedOwnedProfileHash,CombatDefinitionIndex originalDefinition,bool activeRuntime,long activeStep=0,BScoutGunnerNodeSnapshot waveSource=null,bool interceptLocal=false,BNamedOperatorSource named=null,BMiraEquippedTrialContext equipped=null){
   bool wave=waveSource!=null;var sourcePacket=waveSource?.Capture();var waveInstances=wave?waveSource.Waves.SelectMany(w=>w.Instances).ToDictionary(i=>i.InstanceId,StringComparer.Ordinal):null;
   Need(packet!=null&&BasicCombatSimulation.StableJson(packet).Length<=2097152&&Digest(packet)==trustedPacketHash);
   Need(originalOwnedProfile!=null&&Digest(originalOwnedProfile)==trustedOwnedProfileHash&&originalDefinition!=null);
   Need((string)originalOwnedProfile["schemaVersion"]=="combat.owned-profile-intake.v1"&&
    (string)originalOwnedProfile["definitionVersion"]=="combat.owned-base.2026-10-01.1"&&
    (string)originalOwnedProfile["definitionHash"]=="sha256:9d25a88d64239127cbecb7816a4504244947fe55957cd9eb50ec9e4e75bce515"&&
    (string)originalOwnedProfile["policyVersion"]==CombatDefinitionIntake.DefinitionVersion&&
    originalOwnedProfile["canExecute"]?.Type==JTokenType.Boolean&&!(bool)originalOwnedProfile["canExecute"]);
   OriginalAccountId=Text(originalOwnedProfile,"accountId");
   Formation=formation??throw new ArgumentNullException(nameof(formation));
   Need(formation.Mode==BCombatMode.Solo&&formation.Battlefield==BCombatBattlefield.ExternalVertical);
   if(activeRuntime)Fields(packet,"version","provenance","formation","entities","geometry","initialShields");else if(wave)Fields(packet,"version","provenance","formation","ownedProfileHash","definitionHash","stepTimeSpanTicks","entities","geometry","initialShields","sourceEvidence","waveSourceHash");else Fields(packet,"version","provenance","formation","ownedProfileHash","definitionHash","stepTimeSpanTicks","entities","geometry","initialShields","sourceEvidence");
   SchemaVersion=Text(packet,"version");Need((activeRuntime?SchemaVersion==ActiveRuntimeVersion:wave?SchemaVersion==(equipped!=null?EquippedMiraInputVersion:named!=null?NamedLocalInputVersion:interceptLocal?GunnerInterceptLocalInputVersion:WaveRuntimeVersion):SchemaVersion==Version||SchemaVersion==EveVersion)&&Text(packet,"provenance")== (activeRuntime?"ISOLATED_LOCAL_FIXTURE":wave?(interceptLocal?"LOCAL_APPROVED_INTERCEPT_NOT_ADMITTED_ABILITY":"LOCAL_VERIFIED_WAVE_SOURCE_NOT_OPERATING_START"):"LOCAL_EXPLICIT_ORIGINAL_SOURCE_NOT_START")&&
    (activeRuntime||Text(packet,"ownedProfileHash")==trustedOwnedProfileHash&&Text(packet,"definitionHash")==originalDefinition.ContentHash)&&
    JToken.DeepEquals(packet["formation"],formation.Capture()));
   StepTimeSpanTicks=activeRuntime?activeStep:Nat(packet,"stepTimeSpanTicks",10,10000000);Need(StepTimeSpanTicks>=10&&StepTimeSpanTicks<=10000000&&StepTimeSpanTicks%10==0);
   if(!activeRuntime){Need(Array(packet["sourceEvidence"],16).Count>0);foreach(var source in (JArray)packet["sourceEvidence"]){Fields(source as JObject,"sourceId","sha256");Text(source,"sourceId",400);Need(System.Text.RegularExpressions.Regex.IsMatch(Text(source,"sha256"),"^[a-f0-9]{64}$"));}}
   if(wave)Need((string)packet["waveSourceHash"]==(string)sourcePacket["sourceHash"]&&OriginalAccountId==(string)sourcePacket["origin"]["accountId"]);
   var canonical=originalDefinition.Capture()["definition"];
   foreach(var row in Array(packet["entities"],wave?43:16)){
    var x=row as JObject;Fields(x,"id","kind","definitionId","slot","maxHp","attack","defense","basicIntervalMicroseconds","attackSources","defenseSources","effects","weakening","penetration","reductions","allowedTargets");
    foreach(var key in new[]{"attackSources","defenseSources","effects","weakening","penetration","allowedTargets"})Array(x[key],64);
    Fields(x["reductions"] as JObject,"equipment","guardStance","independent");
    foreach(var key in new[]{"equipment","guardStance","independent"})Array(x["reductions"][key],64);
    var e=new Entity(x);BCombatFormation.Identity(e.Id);Need(!Entities.ContainsKey(e.Id));entities.Add(e.Id,e);
    Need(new[]{"OPERATOR","CARRIAGE","TRAIN","LOCAL_ENEMY"}.Contains(e.Kind));
    var allowed=((JArray)x["allowedTargets"]).Select(t=>t.Type==JTokenType.String?(string)t:null).ToArray();
    Need(allowed.Distinct().Count()==allowed.Length&&allowed.All(k=>new[]{"OPERATOR","CARRIAGE","TRAIN","LOCAL_ENEMY"}.Contains(k)));
    if(e.Kind=="OPERATOR"){
          // Explicit supported actors and their canonical source scopes; no permissive roster fallback.
     Need(((named!=null&&(e.Definition=="op_kyle"||e.Definition=="op_sable"))||(interceptLocal&&e.Definition=="op_kyle")||e.Definition=="op_mira"||((SchemaVersion==EveVersion||wave)&&e.Definition=="op_eve"))&&e.Id==e.Definition&&e.Attack!=null&&allowed.SequenceEqual(new[]{"LOCAL_ENEMY"}));
     if(named!=null&&e.Definition=="op_sable"){var sable=named.Actor(e.Id);Need(JToken.DeepEquals(sable["permanentStats"]["attack"],x["attack"])&&e.MaxHp==Nat(sable["permanentStats"],"maxHp",1)&&e.Interval==Nat(sable["permanentStats"],"basicIntervalMs",1)*1000&&JToken.DeepEquals(e.Defense.Snapshot(),CombatExactValue.Create(Nat(sable["permanentStats"],"defense"),1).Snapshot()));continue;}
     var actor=((JArray)canonical["operators"]).SingleOrDefault(o=>(string)o["operatorId"]==e.Definition);
     Need(actor!=null&&(string)actor["role"]=="OPERATOR");var passive=actor["passive"] as JObject;
     Need(passive!=null&&(string)passive["operatorId"]==e.Definition&&JToken.DeepEquals(passive,((JArray)canonical["policy"]["passives"]).SingleOrDefault(o=>(string)o["operatorId"]==e.Definition)));
     if(e.Definition=="op_kyle")Need(interceptLocal&&(string)passive["source"]=="SELF_HP_STRICTLY_BELOW_HALF"&&(string)passive["group"]=="CONDITIONAL_FLAT_DEF_AFTER_STAT_MULTIPLIERS"&&(long)passive["valueFlat"]==83);
     else Need((string)passive["group"]=="NON_EQUIPMENT_OUTGOING"&&(string)passive["source"]==(e.Definition=="op_mira"?"OWN_BASIC":"OWN_SENTRY"));
     if(e.Definition=="op_kyle")conditionalDefense.Add(e.Id,(JObject)passive.DeepClone());
     // Eve's sentry passive is deliberately absent from BASIC. Sentry execution remains unsupported.
     if((string)passive["source"]=="OWN_BASIC")basicPassives.Add(e.Id,(JObject)passive.DeepClone());
     if(equipped!=null){Need(e.Definition=="op_mira"&&equipped.AccountId==OriginalAccountId&&equipped.RunId==waveSource.RunId&&Digest(equipped.Mira["ownedProfile"])==trustedOwnedProfileHash);var stat=equipped.Stats;Need(JToken.DeepEquals(stat["attack"],x["attack"])&&e.MaxHp==Nat(stat,"maxHp",1,int.MaxValue)&&JToken.DeepEquals(stat["defense"],x["defense"])&&e.Interval==Nat(stat,"basicIntervalMs",1)*1000);}
     else {
     var stat=named!=null?named.Actor(e.Id)["permanentStats"]:((JArray)originalOwnedProfile["operators"]).SingleOrDefault(t=>(string)t["operatorId"]==e.Definition)?["permanentStats"];
     Need(stat!=null&&JToken.DeepEquals(stat["attack"],x["attack"])&&e.MaxHp==Nat(stat,"maxHp",1,int.MaxValue)&&
      JToken.DeepEquals(CombatExactValue.From(x["defense"] as JObject).Snapshot(),CombatExactValue.Create(Nat(stat,"defense"),1).Snapshot())&&e.Interval==Nat(stat,"basicIntervalMs",1)*1000);
     }
    }else if(e.Kind=="LOCAL_ENEMY"){if(wave){Need(waveInstances.TryGetValue(e.Id,out var instance)&&e.Definition==instance.DefinitionId&&instance.Hp<=int.MaxValue&&e.MaxHp==(int)instance.Hp&&e.Attack!=null&&allowed.SequenceEqual(new[]{"OPERATOR"}));var type=((JArray)sourcePacket["definition"]["types"]).Single(t=>(string)t["definitionId"]==instance.DefinitionId);Need(JToken.DeepEquals(e.Attack.Snapshot(),type["rawAction"]["value"])&&JToken.DeepEquals(e.Defense.Snapshot(),CombatExactValue.Create(53,1).Snapshot())&&e.Interval==(long)type["basicPeriodMs"]*1000);foreach(var key in new[]{"attackSources","defenseSources","effects","weakening","penetration"})Need(((JArray)x[key]).Count==0);foreach(var key in new[]{"equipment","guardStance","independent"})Need(((JArray)x["reductions"][key]).Count==0);}else Need(e.Definition.StartsWith("LOCAL_",StringComparison.Ordinal)&&e.Attack!=null&&e.Interval>0&&allowed.All(k=>k!="LOCAL_ENEMY"));}
    else Need(e.Attack==null&&e.Interval==0&&allowed.Length==0&&e.Definition.StartsWith("LOCAL_",StringComparison.Ordinal));
    // Validate every arithmetic source before any HP owner exists.
    Budget(e,e,CombatExactValue.From(new JObject{["numerator"]="1",["denominator"]="1"}));
   }
   var expected=new HashSet<string>(formation.Slots.Select(s=>s.CarriageId)){formation.TrainId};
   foreach(var s in formation.Slots)if(s.OperatorId!=null)expected.Add(s.OperatorId);
   Need(expected.SetEquals(Entities.Values.Where(e=>e.Kind!="LOCAL_ENEMY").Select(e=>e.Id))&&(wave?new HashSet<string>(waveInstances.Keys).SetEquals(Entities.Values.Where(e=>e.Kind=="LOCAL_ENEMY").Select(e=>e.Id)):Entities.Values.Count(e=>e.Kind=="LOCAL_ENEMY")<=5));
   Need(Entities[formation.TrainId].Kind=="TRAIN");
   for(int i=0;i<formation.Slots.Count;i++){
    var slot=formation.Slots[i];Need(Entities[slot.CarriageId].Kind=="CARRIAGE"&&Entities[slot.CarriageId].Slot==i);
    if(slot.OperatorId!=null)Need(Entities[slot.OperatorId].Kind=="OPERATOR"&&Entities[slot.OperatorId].Slot==i);
   }
   foreach(var token in Array(packet["geometry"],wave?512:64)){
    var g=token as JObject;Fields(g,"sourceId","targetId","sourceSlot","targetSlot","distanceMm","maximumDistanceMm","observedTick","validUntilTick","eligible");
    var source=Text(g,"sourceId");var target=Text(g,"targetId");Need(Entities.ContainsKey(source)&&Entities.ContainsKey(target)&&source!=target);
    Need(Nat(g,"sourceSlot",0,formation.Slots.Count-1)==Entities[source].Slot&&Nat(g,"targetSlot",0,formation.Slots.Count-1)==Entities[target].Slot);
    Nat(g,"distanceMm",0,1000000000);Nat(g,"maximumDistanceMm",1,1000000000);
    if(wave&&Entities[source].Kind=="LOCAL_ENEMY"){var type=((JArray)sourcePacket["definition"]["types"]).Single(t=>(string)t["definitionId"]==Entities[source].Definition);Need(Nat(g,"maximumDistanceMm",1)<=(long)type["spatial"]["maximumDistanceMm"]);}
    Need(Nat(g,"validUntilTick")>=Nat(g,"observedTick")&&g["eligible"].Type==JTokenType.Boolean&&!geometry.ContainsKey((source,target)));
    geometry.Add((source,target),(JObject)g.DeepClone());
   }
   foreach(var token in Array(packet["initialShields"],16)){
    var s=token as JObject;Fields(s,"targetId","sourceEntity","sourceAbility","effectIndex","amount","expiryTick");
    Need(Entities.ContainsKey(Text(s,"targetId")));BCombatFormation.Identity(Text(s,"sourceEntity"));BCombatFormation.Identity(Text(s,"sourceAbility"));
    Nat(s,"effectIndex",0,int.MaxValue);Nat(s,"amount",0,int.MaxValue);Nat(s,"expiryTick",1);
   }
   original=(JObject)packet.DeepClone();OriginalHash=trustedPacketHash;
  }
  internal CombatNumericDefenseResult Budget(Entity source,Entity target,CombatExactValue basis)=>CombatNumericDefenseBudget.Resolve(NumericInput(source,target,basis));
  internal JObject NumericInput(Entity source,Entity target,CombatExactValue basis,bool basicPassive=true){
   var effects=(JArray)source.Original["effects"].DeepClone();
   if(basicPassive&&source.Kind=="OPERATOR"&&basicPassives.TryGetValue(source.Id,out var passive))effects.Add(new JObject{["sourceId"]="passive:"+source.Id+":OWN_BASIC",["group"]=(string)passive["group"],["valueBps"]=passive["valueBps"].DeepClone()});
   return new JObject{["version"]=CombatNumericDefenseBudget.Version,["base"]=basis.Snapshot(),["defense"]=target.Defense.Snapshot(),
    ["effects"]=effects,["weakening"]=source.Original["weakening"].DeepClone(),["penetration"]=source.Original["penetration"].DeepClone(),["reductions"]=target.Original["reductions"].DeepClone()};
  }
 }
}




