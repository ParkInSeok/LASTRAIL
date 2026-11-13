using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Trusted producer intake, not authentication. The expected digest comes from the original owner, not this packet.
 public sealed class CombatSeraInput {
  internal sealed class Entity {
   internal readonly string Id,Definition;internal readonly int Slot,MaxHp,InitialHp;
   internal readonly CombatExactValue Attack,Rank;
   internal readonly CombatExactValue Defense;internal readonly long ConditionalDefenseFlat;
   internal Entity(JToken e){Id=(string)e["entityId"];Definition=(string)e["definitionId"];Slot=(int)e["placement"]["formationSlot"];MaxHp=(int)e["permanentStats"]["maxHp"];InitialHp=MaxHp;Attack=CombatExactValue.From((JObject)e["permanentStats"]["attack"]);Rank=CombatExactValue.From((JObject)e["skillMagnitude"]);}
   internal Entity(OrdinaryFiveCoreStatSnapshot.ActorStats stats){Id=Definition=stats.OperatorId;Slot=stats.Slot;MaxHp=checked((int)stats.MaxHp);InitialHp=checked((int)stats.CurrentHp);Attack=stats.Attack;Rank=stats.SkillMagnitude;Defense=stats.Defense;ConditionalDefenseFlat=stats.ConditionalDefenseFlat;}
  }
  readonly JObject original;internal readonly IReadOnlyList<Entity> Entities;
  internal readonly OrdinaryTrainInitialState Train;
  internal readonly long TrainDefense;
  internal string SimulationId=>(string)original["scope"]["simulationId"];
  internal string Hash=>(string)original["inputHash"];
  internal string OrdinaryAccount=>(string)(original["binding"] as JObject)?["accountId"];
  internal string InputVersion=>OrdinaryAccount==null?"combat.sera-heal-input.v1":OrdinaryFiveSeraFactory.Version;
  internal Entity Source=>Entities.Single(e=>e.Definition=="op_sera");
  internal JObject Capture()=>(JObject)original.DeepClone();
  CombatSeraInput(JObject packet){original=(JObject)packet.DeepClone();Entities=Array.AsReadOnly(((JArray)original["entities"]).Select(e=>new Entity(e)).ToArray());}
  internal CombatSeraInput(OrdinaryFiveStartInput start,OrdinaryFiveCoreStatSnapshot stats,OrdinaryTrainInitialState train=null){
   Train=train;
   TrainDefense=start?.Train?.PermanentDefense??0;
   Need(start!=null&&stats!=null&&start.AccountId==stats.AccountId&&start.RunId==stats.RunId&&start.OriginHash==stats.OriginHash,"ordinary_sera_stat_origin_conflict");
   var rows=start.Actors.Select(a=>{var s=stats.Actor(a.OperatorId);Need(s.Slot==a.Slot,"ordinary_sera_stat_slot_conflict");return new Entity(s);}).ToArray();Entities=Array.AsReadOnly(rows);
   original=new JObject{["version"]=OrdinaryFiveSeraFactory.Version,["scope"]=new JObject{["simulationId"]=start.RunId},
    ["binding"]=new JObject{["accountId"]=start.AccountId,["runId"]=start.RunId,["originHash"]=start.OriginHash,["kitHash"]=start.KitHash,["statPacketHash"]=stats.PacketHash},
    ["entities"]=new JArray(rows.Select(e=>new JObject{["id"]=e.Id,["slot"]=e.Slot,["hp"]=e.InitialHp,["maxHp"]=e.MaxHp,["attack"]=e.Attack.Snapshot(),["skillMagnitude"]=e.Rank.Snapshot()}))};
   if(start.Basic!=null)original["binding"]["basicHash"]=start.Basic.BasicHash;
   if(start.Relocation!=null)original["binding"]["relocationHash"]=start.Relocation.RelocationHash;
   if(start.Rail?.GeometryBound==true)original["binding"]["railSourceHash"]=start.Rail.SourceHash;
   if(start.Roof!=null)original["binding"]["roofSourceHash"]=start.Roof.SourceHash;
   if(train!=null)original["train"]=new JObject{["binding"]=train.Binding.DeepClone(),["hp"]=train.Hp,["maxHp"]=train.MaxHp};
   original["inputHash"]=BDirectAttackInput.Digest(original);
  }
  internal static void Need(bool condition,string reason="combat_sera_input_invalid"){if(!condition)throw new ArgumentException(reason);}
  internal static void Fields(JObject o,params string[] keys)=>Need(o!=null&&o.Properties().Select(p=>p.Name).OrderBy(s=>s,StringComparer.Ordinal).SequenceEqual(keys.OrderBy(s=>s,StringComparer.Ordinal)));
  internal static long Nat(JToken o,string key,long min=0,long max=9007199254740991L){Need(o?[key]?.Type==JTokenType.Integer,"combat_sera_command_invalid");long n=(long)o[key];Need(n>=min&&n<=max,"combat_sera_command_invalid");return n;}
  internal static string Text(JToken o,string key){Need(o?[key]?.Type==JTokenType.String&&!string.IsNullOrEmpty((string)o[key]),"combat_sera_command_invalid");return (string)o[key];}
  public static CombatSeraInput Read(JObject packet,string trustedOriginalHash,string expectedAccount){
   Fields(packet,"version","scope","binding","canExecute","authority","profileHash","sourceHash","definitionVersion","definitionHash","policyVersion","policyHash","supportedAbility","passive","entities","unsupported","inputHash");
   var body=(JObject)packet.DeepClone();body.Remove("inputHash");
   Need(Text(packet,"inputHash")==trustedOriginalHash&&"sha256:"+BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(body))==trustedOriginalHash);
   Need(Text(packet,"version")=="combat.sera-heal-input.v1"&&Text(packet,"binding")=="LOCAL_CANDIDATE_NOT_ATOMIC_START"&&packet["canExecute"]?.Type==JTokenType.Boolean&&!(bool)packet["canExecute"]);
   Fields(packet["scope"] as JObject,"simulationId","mode");var sim=Text(packet["scope"],"simulationId");Need(System.Text.RegularExpressions.Regex.IsMatch(sim,"\\A[A-Za-z0-9_-]{1,64}\\z"));
   var mode=Text(packet["scope"],"mode");Need(mode=="ORDINARY_SOLO"||mode=="ORDINARY_COOP");
   var authority=new JObject{["version"]="combat.authority-split.v1",["mode"]=mode,["combatOwner"]=mode=="ORDINARY_SOLO"?"UNITY_LOCAL":"UNITY_HOST",["combatClock"]=mode=="ORDINARY_SOLO"?"LOCAL_LOGICAL":"HOST_LOGICAL",["transactionOwner"]="SERVER",["resultVerificationOwner"]="SERVER",["clientReportProvesCombat"]=false};
   Need(JToken.DeepEquals(packet["authority"],authority));
   Need(Text(packet,"definitionVersion")=="combat.owned-base.2026-10-01.1"&&Text(packet,"definitionHash")=="sha256:9d25a88d64239127cbecb7816a4504244947fe55957cd9eb50ec9e4e75bce515");
   Need(Text(packet,"policyVersion")==CombatDefinitionIntake.DefinitionVersion&&Text(packet,"policyHash")=="sha256:a4ce53b746fa5d42eaaf0481f3c9a1ccc931a0ae50a176bd1bc192f9a60d875b");
   foreach(var key in new[]{"profileHash","sourceHash"})Need(System.Text.RegularExpressions.Regex.IsMatch(Text(packet,key),"\\Asha256:[a-f0-9]{64}\\z"));
   var ability=JObject.Parse("{'id':'op_sera.active','effect':'HEAL_ONE','healBps':16000,'cooldownMs':12000,'windupMs':400,'recoveryMs':300,'target':'LIVING_INJURED_LOWEST_HP_RATIO_SLOT_ID_INCLUDING_SELF','lockTargetAt':'ACCEPTANCE','retarget':false,'overheal':'DISCARD'}");
   var passive=JObject.Parse("{'operatorId':'op_sera','source':'OWN_HEAL','group':'HEAL_MAGNITUDE','valueBps':1000}");
   Need(JToken.DeepEquals(packet["supportedAbility"],ability)&&JToken.DeepEquals(packet["passive"],passive));
   Need(packet["entities"] is JArray a&&a.Count==5&&!string.IsNullOrWhiteSpace(expectedAccount));
   var ids=new HashSet<string>();var slots=new HashSet<long>();var defs=new HashSet<string>();
   foreach(var e in (JArray)packet["entities"]){
    Fields(e as JObject,"entityId","definitionId","ownerAccountId","role","placement","capability","permanentStats","skillMagnitude","sources");
    var def=Text(e,"definitionId");Need(new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}.Contains(def)&&defs.Add(def)&&Text(e,"ownerAccountId")==expectedAccount);
    Need(Text(e,"entityId")==BasicCombatSimulation.StableJson(new JArray(sim,expectedAccount,def))&&ids.Add(Text(e,"entityId")));
    Need(Text(e,"role")=="OPERATOR"&&Text(e,"capability")=="LIVING_OPERATOR_HEAL");Fields(e["placement"] as JObject,"formationSlot");Need(slots.Add(Nat(e["placement"],"formationSlot",0,4)));
    Fields(e["permanentStats"] as JObject,"maxHp","attack","defense","basicIntervalMs");Nat(e["permanentStats"],"maxHp",1,int.MaxValue);Nat(e["permanentStats"],"defense");Nat(e["permanentStats"],"basicIntervalMs",1);
    CombatExactValue.From(e["permanentStats"]["attack"] as JObject);var rank=CombatExactValue.From(e["skillMagnitude"] as JObject);Need(rank.Floor()>0);
    var sources=e["sources"];Fields(sources as JObject,"operator","training");Fields(sources["operator"] as JObject,"sourceId","revision");
    Need(Text(sources["operator"],"sourceId")=="account_operators:"+expectedAccount+":"+def);Nat(sources["operator"],"revision",1);
    Need(sources["training"] is JArray training&&training.Count==5);
    var trainingIds=new HashSet<string>();foreach(var t in (JArray)sources["training"]){Fields(t as JObject,"sourceId","revision","operatorId","level");var tid=Text(t,"sourceId");Need(tid.StartsWith("operator_training_core_slots:"+expectedAccount+":",StringComparison.Ordinal)&&trainingIds.Add(tid));Nat(t,"revision",1);Nat(t,"level",1);Need(new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"}.Contains(Text(t,"operatorId")));}
   }
   Need(JToken.DeepEquals(packet["unsupported"],new JArray("OTHER_ABILITIES","RUN_STAT_MODIFIERS","GENERAL_SHARED_ACTION_QUEUE","PUBLIC_START_AND_CHECKPOINT_COMMIT")));
   return new CombatSeraInput(packet);
  }
 }
}
