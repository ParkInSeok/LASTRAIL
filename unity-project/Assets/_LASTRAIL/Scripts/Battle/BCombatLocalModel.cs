using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed class BCombatHpBinding {
  public string TargetId {get;} public int MaxHp {get;} public int CurrentHp {get;}
  public BCombatHpBinding(string targetId,int maxHp):this(targetId,maxHp,maxHp){}
  public BCombatHpBinding(string targetId,int maxHp,int currentHp){BCombatFormation.Identity(targetId);if(maxHp<=0)throw new ArgumentOutOfRangeException(nameof(maxHp));if(currentHp<0||currentHp>maxHp)throw new ArgumentOutOfRangeException(nameof(currentHp));TargetId=targetId;MaxHp=maxHp;CurrentHp=currentHp;}
 }
 public sealed class BCombatSingleTargetReceipt {
  public string EventId {get;} public string TargetId {get;} public long Tick {get;} public int HpLoss {get;} public int HpAfter {get;}
  public int Absorbed {get;} public int TransferToTrain {get;} public int TrainHpLoss {get;} public bool CarriageDown {get;} public bool TrainDefeated {get;} public int ShieldAdded {get;} public int HpGain {get;} public int DerivedExcess {get;}
  internal BCombatSingleTargetReceipt(string id,string target,long tick,int loss,int hp,int absorbed=0,int transfer=0,int trainLoss=0,bool down=false,bool defeated=false,int shieldAdded=0,int hpGain=0,int derivedExcess=0){EventId=id;TargetId=target;Tick=tick;HpLoss=loss;HpAfter=hp;Absorbed=absorbed;TransferToTrain=transfer;TrainHpLoss=trainLoss;CarriageDown=down;TrainDefeated=defeated;ShieldAdded=shieldAdded;HpGain=hpGain;DerivedExcess=derivedExcess;}
 }
 // LOCAL resolved-HP owner. Original single-target v1 remains explicit and
 // replayable. New v2 composes the existing actual-loss carriage router; no
 // attack/DEF adjudication, admission, UI or fullcheckpoint owner.
 public sealed class BCombatLocalModel {
  const long MaximumLocalTick=9007199254740991;
  const int OriginalCacheCharacters=33554432,MaximumCacheCharacters=67108864;
  const string OriginalSchema="unity.combat-b.kernel-local.v1",DamageSchema="unity.combat-b.kernel-damage-local.v2";
  const string NodeSchema="unity.combat-b.node-health-local.v1.20261102";
  readonly BCombatFormation formation;readonly BCombatHpBinding[] bindings;readonly int capacity;readonly bool integrated;readonly CombatDamageRouter damageRouter;
  readonly string trainId;readonly HashSet<string> operatorIds;readonly OrdinaryNodeCombatInput nodeInput;readonly LastRail.Run.OrdinaryLocalBoardingSource boardingSource;
  readonly Dictionary<string,CombatVitals> vitals=new Dictionary<string,CombatVitals>(StringComparer.Ordinal);
  readonly Dictionary<string,BCombatSingleTargetReceipt> receipts=new Dictionary<string,BCombatSingleTargetReceipt>(StringComparer.Ordinal);
  readonly JArray events=new JArray();long lastTick;
  public bool TrainDefeated=>Hp(trainId)==0;
  public BCombatLocalModel(BCombatFormation profile,IEnumerable<BCombatHpBinding> source,int eventCapacity,BCombatAuthority localAuthority):this(profile,source,eventCapacity,localAuthority,false){}
  BCombatLocalModel(BCombatFormation profile,IEnumerable<BCombatHpBinding> source,int eventCapacity,BCombatAuthority localAuthority,bool withTransfer){
   formation=profile??throw new ArgumentNullException(nameof(profile));if(profile.Battlefield!=BCombatBattlefield.ExternalVertical||profile.Authority==BCombatAuthority.ServerWorld||profile.Authority!=localAuthority||withTransfer&&profile.Mode!=BCombatMode.Solo)throw new InvalidOperationException("Local B kernel is not this profile authority");
   trainId=profile.TrainId;operatorIds=new HashSet<string>(profile.Slots.Where(s=>s.OperatorId!=null).Select(s=>s.OperatorId),StringComparer.Ordinal);
   if(eventCapacity<=0||eventCapacity>10000)throw new ArgumentOutOfRangeException(nameof(eventCapacity));capacity=eventCapacity;integrated=withTransfer;
   bindings=source?.ToArray()??throw new ArgumentNullException(nameof(source));var targets=new HashSet<string>(profile.Slots.Select(s=>s.CarriageId),StringComparer.Ordinal){profile.TrainId};foreach(var s in profile.Slots)if(s.OperatorId!=null)targets.Add(s.OperatorId);
   if(bindings.Any(b=>b==null)||bindings.Select(b=>b.TargetId).Distinct(StringComparer.Ordinal).Count()!=bindings.Length||!targets.SetEquals(bindings.Select(b=>b.TargetId)))throw new ArgumentException("Each B target needs one independent HP binding");
   InitializeVitals();
   if(integrated)damageRouter=new CombatDamageRouter(profile.TrainId,vitals[profile.TrainId],profile.Slots.ToDictionary(s=>s.CarriageId,s=>vitals[s.CarriageId]),eventCapacity);
  }
  // Node HP comes from the committed entry; no fabricated carriage target or transfer policy.
  internal BCombatLocalModel(OrdinaryNodeCombatInput node,int eventCapacity,LastRail.Run.OrdinaryLocalBoardingSource boarding=null){
   boardingSource=boarding;
   nodeInput=node??throw new ArgumentNullException(nameof(node));if(eventCapacity<=0||eventCapacity>10000)throw new ArgumentOutOfRangeException(nameof(eventCapacity));capacity=eventCapacity;integrated=true;trainId=node.TrainId;
   operatorIds=new HashSet<string>(node.Actions.Actors.Select(a=>a.OperatorId),StringComparer.Ordinal);
   bindings=node.Actions.Actors.Select(a=>{var s=node.Actor(a.OperatorId);return new BCombatHpBinding(a.OperatorId,checked((int)s.MaxHp),checked((int)s.CurrentHp));})
    .Concat(new[]{new BCombatHpBinding(trainId,node.TrainMaxHp,node.TrainHp)})
    .Concat(node.Source.Enemies.Gunners.Select(g=>new BCombatHpBinding(g.InstanceId,checked((int)g.MaxHp.Floor()),checked((int)g.Hp.Floor()))))
    .Concat(boarding==null?Array.Empty<BCombatHpBinding>():boarding.Boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Select(i=>new BCombatHpBinding(i.InstanceId,checked((int)i.Hp)))).ToArray();
   if(bindings.Length<8||bindings.Select(b=>b.TargetId).Distinct(StringComparer.Ordinal).Count()!=bindings.Length)throw new ArgumentException("Distinct original node HP targets required.");
   InitializeVitals();
  }
  void InitializeVitals(){foreach(var b in bindings){var v=new CombatVitals(b.MaxHp,0,true);if(b.CurrentHp!=b.MaxHp){var initial=v.CaptureLocal();initial["hp"]=b.CurrentHp;initial["anchorHp"]=b.CurrentHp;v.RestoreLocal(initial,0);}vitals.Add(b.TargetId,v);}}
  // Borrowed only by the owning node's actor projection. Mutations use this kernel's commands.
  internal CombatVitals BorrowVitals(string id){if(nodeInput==null)throw new InvalidOperationException("Node HP owner required.");return vitals.TryGetValue(id,out var v)?v:throw new ArgumentException("Unknown node HP target.");}
  internal BCombatLocalModel CloneOwnedNode(){
   if(nodeInput==null)throw new InvalidOperationException("Node HP owner required.");var copy=new BCombatLocalModel(nodeInput,capacity,boardingSource);
   foreach(var pair in vitals)copy.vitals[pair.Key]=pair.Value.Copy();
   foreach(var row in events)copy.events.Add(row.DeepClone());foreach(var pair in receipts)copy.receipts.Add(pair.Key,pair.Value);copy.lastTick=lastTick;return copy;
  }
  public static BCombatLocalModel CreateWithCarriageTransfer(BCombatFormation profile,IEnumerable<BCombatHpBinding> bindings,int eventCapacity,BCombatAuthority authority)=>new BCombatLocalModel(profile,bindings,eventCapacity,authority,true);
  public int Hp(string target)=>vitals.TryGetValue(target,out var v)?v.Hp:throw new ArgumentException("Unknown HP target");
  public bool OperatorAlive(string operatorId){if(!operatorIds.Contains(operatorId))throw new ArgumentException("Unknown operator");return Hp(operatorId)>0;}
  internal int MaximumHp(string id)=>vitals[id].MaxHp;
  // Only the owning native executor calls this after resolving original damage.
  // The same HP journal owns replay; no new HP mirror or overheal producer.
  internal BCombatSingleTargetReceipt ApplyLifesteal(string id,string target,int magnitude,long tick){
   if(!integrated||!operatorIds.Contains(target))throw new InvalidOperationException("Lifesteal requires original operator HP owner");
   var v=Input(id,target,magnitude,tick);var command=new JObject{["kind"]="LIFESTEAL",["eventId"]=id,["targetId"]=target,["magnitude"]=magnitude,["tick"]=tick};if(Original(command,out var old))return old;
   int gain=v.ResolveLivingHeal(magnitude,false,!TrainDefeated).ActualHpGain;
   return Publish(command,new BCombatSingleTargetReceipt(id,target,tick,0,v.Hp,defeated:TrainDefeated,hpGain:gain));
  }
  public BCombatSingleTargetReceipt ApplyResolvedLivingSkillHeal(string id,string target,int magnitude,long tick){
   if(!integrated||!operatorIds.Contains(target))throw new InvalidOperationException("Living skill heal requires original operator HP owner");
   var v=Input(id,target,magnitude,tick);var command=new JObject{["kind"]="LIVING_SKILL_HEAL",["eventId"]=id,["targetId"]=target,["magnitude"]=magnitude,["tick"]=tick};if(Original(command,out var old))return old;
   var heal=v.ResolveLivingHeal(magnitude,true,!TrainDefeated);
   return Publish(command,new BCombatSingleTargetReceipt(id,target,tick,0,v.Hp,defeated:TrainDefeated,hpGain:heal.ActualHpGain,derivedExcess:heal.Excess));
  }
  CombatVitals Input(string eventId,string target,int magnitude,long tick){BCombatFormation.Identity(eventId);if(!vitals.TryGetValue(target,out var v)||magnitude<0||tick<0||tick>MaximumLocalTick)throw new ArgumentException("Invalid resolved target event");return v;}
  bool Original(JObject command,out BCombatSingleTargetReceipt receipt){if(receipts.TryGetValue((string)command["eventId"],out receipt)){if(!JToken.DeepEquals(events.Single(e=>(string)e["eventId"]==(string)command["eventId"]),command))throw new InvalidOperationException("Original resolved event changed");return true;}if(events.Count>=capacity||(long)command["tick"]<lastTick)throw new InvalidOperationException("Kernel capacity or clock boundary");return false;}
  BCombatSingleTargetReceipt Publish(JObject command,BCombatSingleTargetReceipt receipt){events.Add(command);receipts.Add(receipt.EventId,receipt);lastTick=receipt.Tick;return receipt;}
  internal JArray CaptureOrdinaryHealthChanges(){
   if(nodeInput==null)throw new InvalidOperationException("Original node health journal required.");
   var result=new JArray();foreach(var command in events){string target=(string)command["targetId"];if(target!=trainId&&!operatorIds.Contains(target))continue;
    var receipt=receipts[(string)command["eventId"]];if(receipt.HpLoss==0&&receipt.HpGain==0)continue;
    var attempt=nodeInput.Attempt;string id=BDirectAttackInput.Digest(new JArray(attempt.AccountId,attempt.RunId,attempt.NodeId,attempt.Generation,attempt.SnapshotHash,receipt.EventId));
    result.Add(new JObject{["eventId"]=id,["targetId"]=target,["reason"]=receipt.HpLoss>0?"DAMAGE":"HEAL",["hp"]=receipt.HpAfter});
   }return result;
  }
  public BCombatSingleTargetReceipt ApplyResolvedSingleTargetDamage(string eventId,string target,int magnitude,long tick){
   if(integrated)throw new InvalidOperationException("Integrated B damage requires explicit v2 command");var v=Input(eventId,target,magnitude,tick);var command=new JObject{["eventId"]=eventId,["targetId"]=target,["magnitude"]=magnitude,["tick"]=tick};if(Original(command,out var old))return old;
   var loss=v.Damage(magnitude,tick,false);return Publish(command,new BCombatSingleTargetReceipt(eventId,target,tick,loss.HpLoss,v.Hp));
  }
  public BCombatSingleTargetReceipt ApplyResolvedDamage(string eventId,string target,int magnitude,long tick,bool protectedFromDamage=false){
   if(!integrated)throw new InvalidOperationException("Original kernel does not own B transfer policy");var v=Input(eventId,target,magnitude,tick);var command=new JObject{["kind"]="DAMAGE",["eventId"]=eventId,["targetId"]=target,["magnitude"]=magnitude,["tick"]=tick,["protected"]=protectedFromDamage};if(Original(command,out var old))return old;
   BCombatSingleTargetReceipt receipt;
   if(damageRouter!=null&&(target==trainId||formation.Slots.Any(s=>s.CarriageId==target))){
    var r=target==formation.TrainId?damageRouter.ApplyDirectTrainDamage(eventId,magnitude,tick,protectedFromDamage).Receipt:damageRouter.ApplyCarriageDamage(eventId,target,magnitude,tick,protectedFromDamage).Receipt;
    receipt=new BCombatSingleTargetReceipt(eventId,target,tick,r.TargetHpLoss,v.Hp,r.Absorbed,r.TransferToTrain,r.TrainHpLoss,r.CarriageDown,r.TrainDefeated);
   }else{var loss=TrainDefeated?new CombatDamageResult(0,0):v.Damage(magnitude,tick,protectedFromDamage);receipt=new BCombatSingleTargetReceipt(eventId,target,tick,loss.HpLoss,v.Hp,loss.Absorbed,0,nodeInput!=null&&target==trainId?loss.HpLoss:0,false,TrainDefeated);}
   return Publish(command,receipt);
  }
  public BCombatSingleTargetReceipt ApplyResolvedShield(string eventId,string target,CombatShieldSource source,int magnitude,long expiry,long tick){
   if(!integrated)throw new InvalidOperationException("Original kernel has no shield event schema");var v=Input(eventId,target,magnitude,tick);if(source==null||expiry<=tick||expiry>MaximumLocalTick)throw new ArgumentException("Invalid original shield event");BCombatFormation.Identity(source.EntityId);BCombatFormation.Identity(source.AbilityId);
   var command=new JObject{["kind"]="SHIELD",["eventId"]=eventId,["targetId"]=target,["magnitude"]=magnitude,["tick"]=tick,["sourceEntity"]=source.EntityId,["sourceAbility"]=source.AbilityId,["effectIndex"]=source.EffectIndex,["expiry"]=expiry};if(Original(command,out var old))return old;
   int added=TrainDefeated?0:v.Shield(source,magnitude,expiry,tick);return Publish(command,new BCombatSingleTargetReceipt(eventId,target,tick,0,v.Hp,0,0,0,false,TrainDefeated,added));
  }
  JObject Definition(){var hp=new JArray(bindings.Select(b=>{var row=new JObject{["targetId"]=b.TargetId,["maxHp"]=b.MaxHp};if(b.CurrentHp!=b.MaxHp)row["currentHp"]=b.CurrentHp;return row;}));
   if(nodeInput!=null){var s=nodeInput.Source;return new JObject{["accountId"]=s.AccountId,["runId"]=s.RunId,["nodeId"]=s.NodeId,["generation"]=nodeInput.Attempt.Generation,["attemptRevision"]=nodeInput.Attempt.Revision,["snapshotHash"]=s.SnapshotHash,["hpBindings"]=hp,["eventCapacity"]=capacity};}
   var result=new JObject{["formation"]=formation.Capture(),["hpBindings"]=hp,["eventCapacity"]=capacity};if(integrated)result["damagePolicy"]="ACTUAL_CARRIAGE_LOSS_30_PERCENT_EXISTING_ROUTER";return result;}
  // v2<=10k events with4 bounded128char IDs/event at6escapedchars/unit:
  // <34M chars plus bounded definition/vitals fits64M. v1 retains32M bound.
  internal JObject CaptureIdentity()=>new JObject{["schema"]=nodeInput!=null?NodeSchema:integrated?DamageSchema:OriginalSchema,["definition"]=Definition()};
  public string CaptureLocal()=>CaptureState().ToString(Newtonsoft.Json.Formatting.None);
  internal JObject CaptureState()=>new JObject{["schema"]=nodeInput!=null?NodeSchema:integrated?DamageSchema:OriginalSchema,["definition"]=Definition(),["events"]=events.DeepClone(),["lastTick"]=lastTick,["vitals"]=JObject.FromObject(vitals.ToDictionary(pair=>pair.Key,pair=>pair.Value.CaptureLocal()))};
  internal bool HasSameState(BCombatLocalModel other)=>other!=null&&(nodeInput!=null)==(other.nodeInput!=null)&&integrated==other.integrated&&lastTick==other.lastTick
   &&JToken.DeepEquals(Definition(),other.Definition())&&JToken.DeepEquals(events,other.events)&&vitals.Count==other.vitals.Count
   &&vitals.All(pair=>other.vitals.TryGetValue(pair.Key,out var value)&&JToken.DeepEquals(pair.Value.CaptureLocal(),value.CaptureLocal()));
  public static BCombatLocalModel RestoreLocal(string json,BCombatFormation profile,IEnumerable<BCombatHpBinding> originalBindings,int eventCapacity,BCombatAuthority authority){
   if(json==null||json.Length>MaximumCacheCharacters)throw new ArgumentException("Local B cache limit");var cache=JObject.Parse(json);WorldBossRailSnapshot.Fields(cache,"schema","definition","events","lastTick","vitals");bool withTransfer=(string)cache["schema"]==DamageSchema;if(!withTransfer&&((string)cache["schema"]!=OriginalSchema||json.Length>OriginalCacheCharacters))throw new ArgumentException("Unknown original B kernel schema");var model=new BCombatLocalModel(profile,originalBindings,eventCapacity,authority,withTransfer);
   if(!JToken.DeepEquals(model.Definition(),cache["definition"])||!(cache["events"] is JArray rows)||rows.Count>eventCapacity)throw new ArgumentException("Original B local definition changed");
   foreach(var row in rows){if(model.receipts.ContainsKey((string)row["eventId"]))throw new ArgumentException("Duplicate saved event");var r=row as JObject;string id=WorldBossRailSnapshot.Text(row,"eventId",128),target=WorldBossRailSnapshot.Text(row,"targetId",128);int magnitude=checked((int)WorldBossRailSnapshot.Nat(row,"magnitude"));long tick=WorldBossRailSnapshot.Nat(row,"tick");if(!withTransfer){WorldBossRailSnapshot.Fields(r,"eventId","targetId","magnitude","tick");model.ApplyResolvedSingleTargetDamage(id,target,magnitude,tick);}else if((string)row["kind"]=="DAMAGE"){WorldBossRailSnapshot.Fields(r,"kind","eventId","targetId","magnitude","tick","protected");if(row["protected"].Type!=JTokenType.Boolean)throw new ArgumentException("Invalid protection flag");model.ApplyResolvedDamage(id,target,magnitude,tick,(bool)row["protected"]);}else if((string)row["kind"]=="LIVING_SKILL_HEAL"){WorldBossRailSnapshot.Fields(r,"kind","eventId","targetId","magnitude","tick");model.ApplyResolvedLivingSkillHeal(id,target,magnitude,tick);}else if((string)row["kind"]=="LIFESTEAL"){WorldBossRailSnapshot.Fields(r,"kind","eventId","targetId","magnitude","tick");model.ApplyLifesteal(id,target,magnitude,tick);}else{WorldBossRailSnapshot.Fields(r,"kind","eventId","targetId","magnitude","tick","sourceEntity","sourceAbility","effectIndex","expiry");if((string)row["kind"]!="SHIELD")throw new ArgumentException("Unsupported saved event kind");model.ApplyResolvedShield(id,target,new CombatShieldSource(WorldBossRailSnapshot.Text(row,"sourceEntity",128),WorldBossRailSnapshot.Text(row,"sourceAbility",128),checked((int)WorldBossRailSnapshot.Nat(row,"effectIndex"))),magnitude,WorldBossRailSnapshot.Nat(row,"expiry"),tick);}}
   if(!JToken.DeepEquals(JObject.Parse(model.CaptureLocal()),cache))throw new ArgumentException("Local B state does not match original events");return model;
  }
 }
}
