using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Reads the current model's owners for every request; never promotes entry arrays to ongoing absence.
 internal sealed class OrdinaryLiveNumericAuthority {
  internal const string EntryVersion="ordinary.local-entry-effects.v1.20261106";
  internal const string CurrentVersion="ordinary.local-current-effects.v1.20261106";
  internal const string NyxCurrentVersion="ordinary.local-current-effects.v2.20261107";
  readonly OrdinaryFiveCombatFactory factory;
  readonly Func<string,int> hp;
  readonly Func<long,JObject> currentEffects;
  readonly Func<string,long,string> actualKyleMode;
  readonly JObject context;readonly System.Collections.Generic.Dictionary<string,long> boardingDefense=new System.Collections.Generic.Dictionary<string,long>(StringComparer.Ordinal);
  readonly System.Collections.Generic.Dictionary<string,long> boardingPeriods=new System.Collections.Generic.Dictionary<string,long>(StringComparer.Ordinal);
  internal string EntryEffectsHash {get;}
  internal OrdinaryLiveNumericAuthority(OrdinaryFiveCombatFactory factory,Func<string,int> currentHp,Func<long,JObject> actualCurrentEffects,Func<string,long,string> actualKyleMode){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));hp=currentHp??throw new ArgumentNullException(nameof(currentHp));currentEffects=actualCurrentEffects??throw new ArgumentNullException(nameof(actualCurrentEffects));this.actualKyleMode=actualKyleMode??throw new ArgumentNullException(nameof(actualKyleMode));
   Need(factory.NodeInput!=null&&factory.Attempt!=null,"Original node factory required");
   if(factory.BoardingSource!=null){var source=factory.BoardingSource.Boarding.OriginalSource;var raw=source.Capture();var types=raw["definition"]["types"].ToDictionary(t=>(string)t["definitionId"],t=>(long)t["basicPeriodMs"]);foreach(var i in raw["waves"].SelectMany(w=>w["instances"]))boardingDefense.Add((string)i["instanceId"],(long)i["defense"]);foreach(var i in source.Waves.SelectMany(w=>w.Instances))boardingPeriods.Add(i.InstanceId,types[i.DefinitionId]);}
   context=new JObject{["accountId"]=factory.Attempt.AccountId,["runId"]=factory.Attempt.RunId,["nodeId"]=factory.Attempt.NodeId,["startSnapshotHash"]=factory.Original.StartSnapshotHash,["snapshotHash"]=factory.Attempt.SnapshotHash,["attemptGeneration"]=factory.Attempt.Generation};
   var entry=factory.NodeInput.Source.Capture()["snapshot"]?["runtime"]?["effects"] as JObject;EntryEffectsHash=ValidateEntry(entry,context);context["entryEffectsHash"]=EntryEffectsHash;
   foreach(var actor in factory.Original.Actors){var stats=factory.ActorStats(actor.OperatorId);for(int i=0;i<3;i++)if(stats.EquipmentSlot(i)!=null)throw new NotSupportedException("Live equipment timing/effect authority is not implemented; original equipped stats must not silently become a no-effects loadout.");}
  }
  static void Need(bool ok,string reason){if(!ok)throw new ArgumentException("Ordinary live numeric authority: "+reason);}
  static void Identity(JObject value,JObject expected,params string[] names){foreach(string name in names)Need(value[name]?.Type==JTokenType.String&&!string.IsNullOrWhiteSpace((string)value[name])&&JToken.DeepEquals(value[name],expected[name]),"Original "+name+" differs");}
  internal static string ValidateEntry(JObject entry,JObject expected){
   Need(entry!=null,"Immutable DB entry effect source required");BDirectAttackInput.Fields(entry,"version","accountId","runId","nodeId","startSnapshotHash","originGeneration","revision","atTick","activeEffects","shields","interceptions");
   Need((string)entry["version"]==EntryVersion&&(long?)entry["originGeneration"]==1&&(long?)entry["revision"]==1&&(long?)entry["atTick"]==0,"New isolated original effect entry required");Identity(entry,expected,"accountId","runId","nodeId","startSnapshotHash");
   foreach(string name in new[]{"activeEffects","shields","interceptions"})Need(entry[name] is JArray rows&&rows.Count==0,"Nonempty original entry unsupported, cannot erase "+name);return BDirectAttackInput.Digest(entry);
  }
  internal static JObject ValidateCurrent(JObject value,JObject expected,long tick){
   Need(value!=null&&tick>=0,"Current effect owner required");BDirectAttackInput.Fields(value,"version","accountId","runId","nodeId","snapshotHash","attemptGeneration","entryEffectsHash","revision","atTick","activeEffects","shields","interceptions");
   bool nyx=(string)value["version"]==NyxCurrentVersion;
   Need((nyx||(string)value["version"]==CurrentVersion)&&value["atTick"]?.Type==JTokenType.Integer&&(long)value["atTick"]==tick&&value["revision"]?.Type==JTokenType.Integer&&(long)value["revision"]>=1&&JToken.DeepEquals(value["attemptGeneration"],expected["attemptGeneration"]),"Current tick/revision/attempt proof differs");Identity(value,expected,"accountId","runId","nodeId","snapshotHash","entryEffectsHash");
   foreach(string name in new[]{"activeEffects","shields","interceptions"})Need(value[name] is JArray rows&&rows.Count<=10000,"Complete current "+name+" list required");
   var seen=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);var definitions=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
   foreach(JObject effect in (JArray)value["activeEffects"]){
    string kind=(string)effect["kind"],id=(string)effect["sourceId"],target=(string)effect["targetId"];
    if(kind!="TAUNT"&&!(nyx&&(kind=="MITIGATION_RATE_REDUCTION"||kind=="NEW_BASIC_PERIOD_INCREASE")))throw new NotSupportedException("Live numeric effect kind requires its original eligible-source adapter: "+kind);
    Need(!string.IsNullOrWhiteSpace(id)&&!string.IsNullOrWhiteSpace(target)&&seen.Add(BDirectAttackInput.Digest(new JArray(id,target))),"Duplicate or absent current source identity");
    string definition=BDirectAttackInput.Digest(new JArray(kind,effect["ownerId"],effect["valueBps"]));Need(!definitions.TryGetValue(id,out var prior)||prior==definition,"Source reused across groups or values");definitions[id]=definition;
    if(nyx&&(kind=="MITIGATION_RATE_REDUCTION"||kind=="NEW_BASIC_PERIOD_INCREASE")){
     BDirectAttackInput.Fields(effect,"sourceId","sourceKind","ownerId","targetId","targetGeneration","kind","valueBps","expiresAtTick");
     Need((string)effect["sourceKind"]=="NYX_ZONE"&&(string)effect["ownerId"]=="op_nyx"&&effect["targetGeneration"]?.Type==JTokenType.Integer&&(long)effect["targetGeneration"]>0&&effect["valueBps"]?.Type==JTokenType.Integer&&
      (kind=="MITIGATION_RATE_REDUCTION"?new[]{800,1500}:new[]{1500,2500}).Contains((int)effect["valueBps"])&&effect["expiresAtTick"]?.Type==JTokenType.Integer&&(long)effect["expiresAtTick"]>=tick,"Current Nyx source differs");continue;
    }
    if((string)effect["kind"]!="TAUNT")throw new NotSupportedException("Live numeric effect kind requires its original eligible-source adapter: "+(string)effect["kind"]);
    BDirectAttackInput.Fields(effect,"sourceId","ownerId","targetId","kind","valueBps","expiresAtTick");Need(!string.IsNullOrWhiteSpace((string)effect["sourceId"])&&(string)effect["ownerId"]=="op_kyle"&&!string.IsNullOrWhiteSpace((string)effect["targetId"])&&(long?)effect["valueBps"]==0&&(long?)effect["expiresAtTick"]>tick,"Current original taunt source differs");
   }
   foreach(JObject row in (JArray)value["interceptions"]){BDirectAttackInput.Fields(row,"shotId","tick","eventId","status");Need(!string.IsNullOrWhiteSpace((string)row["shotId"])&&!string.IsNullOrWhiteSpace((string)row["eventId"])&&row["tick"]?.Type==JTokenType.Integer&&(long)row["tick"]<=tick&&(long)row["tick"]>=0&&new[]{"CLEAR","INTERCEPTED"}.Contains((string)row["status"]),"Original interception decision differs");}
   return (JObject)value.DeepClone();
  }
  JObject Current(long tick)=>ValidateCurrent(currentEffects(tick),context,tick);
  OrdinaryFiveStartInput.Actor Actor(string id)=>factory.Original.Actors.SingleOrDefault(a=>a.OperatorId==id)??throw new ArgumentException("Unknown original numeric actor");
  static JObject Reductions()=>new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()};
  static JObject Numeric(CombatExactValue basis,CombatExactValue defense,JArray effects)=>new JObject{["version"]=CombatNumericDefenseBudget.Version,["base"]=basis.Snapshot(),["defense"]=defense.Snapshot(),["effects"]=effects,["penetration"]=new JArray(),["weakening"]=new JArray(),["reductions"]=Reductions()};
  internal CombatExactValue EffectivePeriod(string actor,long tick){
   var current=Current(tick);bool boarding=boardingPeriods.TryGetValue(actor,out long boardingPeriod);bool gunner=factory.NodeInput.Source.Enemies.Gunners.Any(g=>g.InstanceId==actor);if(!gunner&&!boarding)Actor(actor);
   long duration=checked((boarding?boardingPeriod:gunner?(long)factory.NodeInput.Source.Enemies.CaptureActionDefinition()["basicPeriodMs"]:factory.Original.Basic.Profiles.Single(p=>p.OperatorId==actor).OriginalIntervalMs)*1000);
   // Exact microseconds: with the current explicitly empty timing source set no intermediate tick rounding occurs.
   long period=CombatActionTiming.BasicAttackPeriodTicks(duration,1,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>());return CombatActionTiming.BasicPeriodWithNyxDelay(CombatExactValue.Create(period,1),((JArray)current["activeEffects"]).Where(r=>(string)r["targetId"]==actor&&(string)r["kind"]=="NEW_BASIC_PERIOD_INCREASE").Select(r=>new CombatTimingSource((string)r["sourceId"],(int)r["valueBps"])));
  }
  internal JObject NyxZoneAttack(string actor,long tick){Current(tick);Need(actor=="op_nyx","Nyx zone caster required");Actor(actor);var stats=factory.ActorStats(actor);return Launch(stats.Attack.Multiply(stats.SkillMagnitude),new JArray());}
  internal int SkillHealingMultiplier(long tick){Current(tick);return 10000;}
  internal JObject ReinaBarrageAttack(string actor,long tick){Current(tick);Need(actor=="op_reina","Reina barrage caster required");Actor(actor);var stats=factory.ActorStats(actor);return Launch(stats.Attack.Multiply(stats.SkillMagnitude),new JArray());}
  static JObject Launch(CombatExactValue basis,JArray applied){
   var resolved=CombatNumericDefenseBudget.Resolve(Numeric(basis,CombatExactValue.Create(0,1),applied));
   return new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=resolved.Snapshot()["exact"].DeepClone(),["appliedOffense"]=applied,["penetration"]=new JArray()};
  }
  internal JObject BasicAttack(string actor,string target,long tick){
   Current(tick);var original=Actor(actor);Need(factory.EnemyIds.Contains(target),"Original BASIC target required");var passive=original.Passive;var effects=new JArray();
   if(actor=="op_mira"){
    Need((string)passive["source"]=="OWN_BASIC"&&(string)passive["group"]=="NON_EQUIPMENT_OUTGOING"&&passive["valueBps"]?.Type==JTokenType.Integer,"Original Mira passive differs");
    effects.Add(new JObject{["sourceId"]=actor+".passive.OWN_BASIC",["group"]=passive["group"].DeepClone(),["valueBps"]=passive["valueBps"].DeepClone()});
   }
   // Reina's marked-target bonus is ineligible in this supported current view (MARK is rejected above).
   // Eve's OWN_SENTRY and Sera's OWN_HEAL never apply to an ordinary BASIC projectile.
   return Launch(factory.ActorStats(actor).Attack,effects);
  }
  internal JObject GunnerAttack(string gunner,CombatExactValue raw,long tick){
   Current(tick);var source=factory.NodeInput.Source.Enemies.Gunners.SingleOrDefault(g=>g.InstanceId==gunner);Need(source!=null&&raw!=null&&JToken.DeepEquals(source.RawDamage.Snapshot(),raw.Snapshot()),"Original once-scaled gunner raw damage required");return Launch(raw,new JArray());
  }
  internal static CombatExactValue ConditionalDefense(CombatExactValue basis,long flat,int current,long maximum){Need(basis!=null&&flat>=0&&current>=0&&maximum>0&&current<=maximum,"Current shared HP/defense source differs");return (long)current*2<maximum?CombatExactValue.Create(basis.Numerator+flat*basis.Denominator,basis.Denominator):basis;}
  internal JObject Defense(string target,long tick){
   var current=Current(tick);CombatExactValue defense;var enemy=factory.NodeInput.Source.Enemies.Gunners.SingleOrDefault(g=>g.InstanceId==target);
   if(target==factory.TrainId)defense=CombatExactValue.Create(factory.Original.Train.PermanentDefense,1);
   else if(enemy!=null)defense=CombatExactValue.Create(enemy.Defense,1);
   else if(boardingDefense.TryGetValue(target,out var boardingDef))defense=CombatExactValue.Create(boardingDef,1);
   else{Actor(target);var stats=factory.ActorStats(target);defense=ConditionalDefense(stats.Defense,stats.ConditionalDefenseFlat,hp(target),stats.MaxHp);}
   var result=new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=defense.Snapshot(),["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=Reductions()};
   if((string)current["version"]==NyxCurrentVersion){result["version"]=CombatProjectileAttackSnapshot.MitigationArrivalVersion;result["mitigationRateReduction"]=new JArray(((JArray)current["activeEffects"]).Where(r=>(string)r["targetId"]==target&&(string)r["kind"]=="MITIGATION_RATE_REDUCTION").Select(r=>new JObject{["sourceId"]=r["sourceId"].DeepClone(),["valueBps"]=r["valueBps"].DeepClone()}));}return result;
  }
  internal string KyleMode(string target,long tick){Current(tick);Need(factory.EnemyIds.Contains(target),"Original Kyle target required");string mode=actualKyleMode(target,tick);Need(mode=="MELEE"||mode=="RANGED","Actual measured Kyle mode required");return mode;}
  internal JObject Interception(string shot,long tick){
   var view=Current(tick);BCombatFormation.Identity(shot);var rows=((JArray)view["interceptions"]).OfType<JObject>().Where(r=>(string)r["shotId"]==shot).ToArray();Need(rows.Length<=1,"Duplicate original shot decision");
   if(rows.Length==1){var result=(JObject)rows[0].DeepClone();Need((long)result["tick"]==tick,"Current ordered interception decision required");return result;}
   return new JObject{["shotId"]=shot,["tick"]=tick,["status"]="CLEAR",["eventId"]="local-interception:"+BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(new JArray(factory.Attempt.RunId,factory.Attempt.NodeId,factory.Attempt.Generation,shot,tick)))};
  }
 }
}
