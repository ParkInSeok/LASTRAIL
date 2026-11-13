using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed class BDirectAttackBuffReceipt {
  readonly JObject source;public BDirectAttackReceipt Attack {get;}
  public JObject BuffSource=>source==null?null:(JObject)source.DeepClone();
  internal BDirectAttackBuffReceipt(BDirectAttackReceipt attack,JObject accepted){Attack=attack;source=accepted==null?null:(JObject)accepted.DeepClone();}
  internal JObject Snapshot(){var snapshot=new JObject{["eventId"]=Attack.EventId,["sourceId"]=Attack.SourceId,["tick"]=Attack.Tick,["beforeShield"]=Attack.BeforeShield,["calculation"]=Attack.Calculation,["buffSource"]=source==null?JValue.CreateNull():(JToken)source.DeepClone(),
   ["hp"]=new JObject{["targetId"]=Attack.Hp.TargetId,["hpLoss"]=Attack.Hp.HpLoss,["hpAfter"]=Attack.Hp.HpAfter,["absorbed"]=Attack.Hp.Absorbed,["transfer"]=Attack.Hp.TransferToTrain,["trainLoss"]=Attack.Hp.TrainHpLoss,["carriageDown"]=Attack.Hp.CarriageDown,["trainDefeated"]=Attack.Hp.TrainDefeated}};if(Attack.Lifesteal!=null)snapshot["lifesteal"]=Attack.Lifesteal;return snapshot;}
 }
 // One bounded LOCAL coordinating owner. Copy its private state for staging; publish only on success.
 // The existing direct child exclusively owns HP/clock/period, buff child owns only operating source state.
 public sealed partial class BDirectAttackBuffSession:IBEnemyBasicTimingContext {
  public const string Version="unity.combat-b.direct-buff-local.v1";
  readonly BDirectAttackBuffBinding binding;readonly int eventCapacity;BDirectAttackSession direct;BCarriageBuffModel buffs;
  readonly JArray events=new JArray();readonly Dictionary<string,(JObject command,BDirectAttackBuffReceipt receipt)> receipts=new Dictionary<string,(JObject,BDirectAttackBuffReceipt)>(StringComparer.Ordinal);
  public BDirectAttackBuffSession(BDirectAttackBuffBinding originalBinding,int boundedEventCapacity){
   binding=originalBinding??throw new ArgumentNullException(nameof(originalBinding));BuffWire.Need(boundedEventCapacity>=1&&boundedEventCapacity<=10000);eventCapacity=boundedEventCapacity;
   direct=BDirectAttackSession.CreateModified(binding.Attack,eventCapacity);buffs=new BCarriageBuffModel(binding.Definitions,binding.Departure,binding.Initial);
  }
  public long Tick=>direct.Tick;public bool Paused=>direct.Paused;public bool TrainDefeated=>direct.TrainDefeated;public int Hp(string id)=>direct.Hp(id);
  public JObject BuffSnapshot()=>buffs.Snapshot();public JObject Capacity()=>buffs.Capacity();
  public void Advance(long elapsedTicks,int maximumSteps)=>Commit(new JObject{["kind"]="ADVANCE",["elapsed"]=elapsedTicks,["maximumSteps"]=maximumSteps});
  public void SetPaused(bool value)=>Commit(new JObject{["kind"]="PAUSE",["value"]=value});
  public void SetSpeed(int halfUnits)=>Commit(new JObject{["kind"]="SPEED",["value"]=halfUnits});
  public BDirectAttackBuffReceipt ApplyBasicImpact(string id,string source,string target,long tick,string ability="BASIC")=>Commit(AttackCommand("BASIC",id,source,target,tick,ability));
  public BDirectAttackBuffReceipt AdvanceAndApplyBasicImpact(string id,string source,string target,long tick,long elapsedTicks,int maximumSteps,string ability="BASIC"){
   var c=AttackCommand("ADVANCE_BASIC",id,source,target,tick,ability);c["elapsed"]=elapsedTicks;c["maximumSteps"]=maximumSteps;return Commit(c);
  }
  static JObject AttackCommand(string kind,string id,string source,string target,long tick,string ability){BCombatFormation.Identity(id);BCombatFormation.Identity(source);BCombatFormation.Identity(target);return new JObject{["kind"]=kind,["eventId"]=id,["sourceId"]=source,["targetId"]=target,["tick"]=tick,["ability"]=ability};}
  BDirectAttackBuffSession Stage(){
   var r=new BDirectAttackBuffSession(binding,eventCapacity);r.direct=direct.CloneOwnedModified();r.buffs=buffs.CloneOwned();
   foreach(var row in events)r.events.Add(row.DeepClone());
   // Receipts are sealed immutable values; their JObject properties return copies.
   foreach(var pair in receipts)r.receipts.Add(pair.Key,((JObject)pair.Value.command.DeepClone(),pair.Value.receipt));return r;
  }
  BDirectAttackBuffReceipt Commit(JObject command){
   string id=(string)command["eventId"];if(id!=null&&receipts.TryGetValue(id,out var old)){BuffWire.Need(JToken.DeepEquals(old.command,command),"b_attack_buff_event_conflict");return old.receipt;}
   BuffWire.Need(events.Count<eventCapacity,"b_attack_buff_capacity");var staged=Stage();var receipt=staged.Execute(command);
   // No await, external observer or arbitrary callback occurs during this synchronous ownership transfer.
   direct=staged.direct;buffs=staged.buffs;events.Add(command.DeepClone());if(id!=null)receipts.Add(id,((JObject)command.DeepClone(),receipt));return receipt;
  }
  internal BDirectAttackBuffSession CloneOwnedForBasicLoop()=>Stage();
  internal void ApplyO22(JObject application)=>direct.ApplyO22(application);
  internal void BindWaveEnemies(IBWaveEnemyPerception value)=>direct.BindWaveEnemies(value);
  internal bool TryGeometry(string source,string target,out JObject frame)=>direct.TryGeometry(source,target,out frame);
  internal string SpatialTarget(string source)=>direct.SpatialTarget(source);
  internal bool HasSpatial=>direct.HasSpatial;
  internal bool SpatialProtection(string actor,string target)=>direct.SpatialProtection(actor,target);
  internal bool LoopTargetEligible(string source,string target)=>direct.LoopTargetEligible(source,target);
  internal void ScheduleEnemyBasic(string source,long due)=>direct.ScheduleEnemyBasic(source,due);
  internal bool EnemyBasicTargetEligible(string source,string target)=>direct.EnemyBasicTargetEligible(source,target);
  internal void ConfigureActiveTarget(JObject target)=>direct.ConfigureActiveTarget(target);
  internal void RecordTargetLifetime(JObject originalEvent,JObject target)=>direct.RecordTargetLifetime(originalEvent,target);
  internal JObject CurrentActiveTarget=>direct.ActiveTarget;
  internal bool ActiveEligible(JObject target,bool emitting)=>direct.ActiveTargetEligible(target,emitting);
  internal CombatExactValue CurrentActiveAttack=>direct.ActiveAttack;
  internal long OriginalDebtHalfTicks=>direct.DebtHalfTicks;
  internal JObject CaptureActiveModifier(string id,bool timed,int slot)=>buffs.Accept(new JObject{["eventId"]=id,["expectedRevision"]=buffs.Revision,["slot"]=slot,["kind"]="ACTIVE",["ability"]="ACTIVE",["timing"]=timed?"TIME_COOLDOWN":"NONE",["target"]=timed?"NONE":"ENEMY",["direct"]=!timed,["originalEligible"]=true});
  internal long ActiveCooldownTicks(string id,long duration)=>new BCarriageBuffCalculator(buffs).ActiveCooldownTicks(id,duration,binding.Attack.StepTimeSpanTicks/10,Array.Empty<CombatTimingSource>(),Array.Empty<CombatTimingSource>(),null);
  internal BDirectAttackBuffReceipt ResolveActiveImpact(string id,string shotId,string target,CombatExactValue attack){var receipt=new BDirectAttackBuffReceipt(direct.ApplyActiveImpact(id,target,attack,buffs,shotId),buffs.Source(shotId));var c=new JObject{["kind"]="ACTIVE_IMPACT",["eventId"]=id,["shotId"]=shotId,["targetId"]=target,["tick"]=Tick};BuffWire.Need(events.Count<eventCapacity,"b_attack_buff_capacity");events.Add(c);receipts.Add(id,(c,receipt));return receipt;}
  internal void ApplyActiveObservation(JObject command){var result=direct.ApplyActiveObservation(command);if(result!=null&&result.HpAfter==0){var e=binding.Attack.Entities[result.TargetId];if(e.Kind=="CARRIAGE"||e.Kind=="OPERATOR")buffs.Transition(new JObject{["eventId"]=StateEvent("active-observation",result.EventId),["expectedRevision"]=buffs.Revision,["kind"]=e.Kind=="CARRIAGE"?"BODY":"ACTOR",["slot"]=e.Slot,["value"]=e.Kind=="CARRIAGE"?"BROKEN":"DOWN"});}}
  internal long NextBasicDue(string source)=>direct.NextBasicDue(source);
  internal void DriveBasicLoop(long elapsed,int maximum,Func<bool> running,Action step)=>direct.AdvanceDriven(elapsed,maximum,running,step);
  internal BDirectAttackBuffReceipt ResolveLoopBasic(string id,string source,string target)=>Execute(AttackCommand("BASIC",id,source,target,Tick,"BASIC"));
  internal void BindProjectileTargetLifetimes()=>direct.BindProjectileTargetLifetimes();
  internal long ProjectileTargetGeneration(string id)=>direct.ProjectileTargetGeneration(id);
  long IBBoardingActors.ProjectileTargetGeneration(string id)=>ProjectileTargetGeneration(id);
  long IBEnemyBasicTimingContext.NextBasicDue(string id)=>NextBasicDue(id);
  void IBEnemyBasicTimingContext.ScheduleEnemyBasic(string id,long due)=>ScheduleEnemyBasic(id,due);
  bool IBEnemyBasicTimingContext.EnemyBasicTargetEligible(string source,string target)=>EnemyBasicTargetEligible(source,target);
  internal void BeginBasicOccupation(string actor,long duration)=>direct.BeginBasicOccupation(actor,duration);
  internal void AdjustBasicOccupation(string actor,long delta)=>direct.AdjustBasicOccupation(actor,delta);
  JObject InterceptionCommand(string id,int slot)=>new JObject{["eventId"]=id,["expectedRevision"]=buffs.Revision,["slot"]=slot,["kind"]="INTERCEPT",["ability"]="INTERCEPT",["timing"]="INTERCEPTION_PHASE",["target"]="NONE",["direct"]=false,["originalEligible"]=true};
  internal JObject PreviewInterception(string id,int slot)=>buffs.CloneOwned().Accept(InterceptionCommand(id,slot));
  internal JObject CaptureInterception(string id,int slot)=>buffs.Accept(InterceptionCommand(id,slot));
  internal void AcceptReceivedInterception(LastRail.Run.BInterceptTimingSnapshot receipt){
   var captured=receipt.BuffSnapshot;var accepted=receipt.AcceptedAction;var command=(JObject)accepted["input"];var before=buffs.Snapshot();
   BuffWire.Need(JToken.DeepEquals(before["departure"],captured["departure"])&&JToken.DeepEquals(before["initial"],captured["initial"]),"b_intercept_native_buff_origin");
   int revision=checked((int)(long)command["expectedRevision"]);var journal=(JArray)captured["journal"];
   BuffWire.Need(revision==buffs.Revision&&journal.Count>revision&&JToken.DeepEquals(before["journal"],new JArray(journal.Take(revision).Select(x=>x.DeepClone())))&&JToken.DeepEquals(journal[revision]["input"],command),"b_intercept_native_journal_prefix");
   BuffWire.Need(JToken.DeepEquals(buffs.Accept(command),accepted["source"]),"b_intercept_native_action_conflict");
  }
  internal BDirectAttackBuffReceipt ResolveGunnerImpact(string id,string source,string target,JObject attack){
   BuffWire.Need(events.Count<eventCapacity,"b_attack_buff_capacity");var budget=new Budget(binding,buffs,id,direct.WaveCategory,direct.Hp);int before=direct.Hp(target);var hit=direct.ApplyLaunchedGunnerImpact(id,source,target,attack,budget);
   if(before>0&&hit.Hp.HpAfter==0)buffs.Transition(new JObject{["eventId"]=StateEvent("actor",id),["expectedRevision"]=buffs.Revision,["kind"]="ACTOR",["slot"]=binding.Attack.Entities[target].Slot,["value"]="DOWN"});
   var receipt=new BDirectAttackBuffReceipt(hit,budget.Accepted);var command=new JObject{["kind"]="GUNNER_PROJECTILE_IMPACT",["eventId"]=id,["sourceId"]=source,["targetId"]=target,["tick"]=Tick,["attack"]=attack.DeepClone()};events.Add(command);receipts.Add(id,(command,receipt));return receipt;
  }
  sealed class Budget:IBDirectAttackBudget {
   readonly BDirectAttackBuffBinding binding;readonly BCarriageBuffModel buffs;readonly string eventId;readonly Func<string,string> category;readonly Func<string,int> hp;
   internal JObject Accepted {get;private set;}
   internal Budget(BDirectAttackBuffBinding b,BCarriageBuffModel m,string id,Func<string,string> observedCategory,Func<string,int> currentHp){binding=b;buffs=m;eventId=id;category=observedCategory;hp=currentHp;}
   public CombatNumericDefenseResult Resolve(BDirectAttackInput.Entity source,BDirectAttackInput.Entity target,JObject numeric){
    if(target.Kind=="CARRIAGE"||target.Kind=="OPERATOR"){
     var original=target.Original;numeric["defense"]=BCarriageBuffCalculator.DefenseStat(target.Id,CombatExactValue.From((JObject)original["defense"]),(JArray)original["defenseSources"],buffs.Defense(target.Slot,target.Kind=="CARRIAGE"?"BODY":"OPERATOR")).Snapshot();
     long conditional=binding.Attack.ConditionalDefenseFlat(target.Id,hp(target.Id));if(conditional!=0){var value=CombatExactValue.From((JObject)numeric["defense"]);numeric["defense"]=CombatExactValue.Create(value.Numerator+conditional*value.Denominator,value.Denominator).Snapshot();}}
    if(source.Kind!="OPERATOR")return CombatNumericDefenseBudget.Resolve(numeric);
    // Eligibility was established by the original action/geometry owner before this pure calculation seam.
    Accepted=buffs.Accept(new JObject{["eventId"]=eventId,["expectedRevision"]=buffs.Revision,["slot"]=source.Slot,["kind"]="BASIC",["ability"]="BASIC",["timing"]="NONE",["target"]=category(target.Id)??binding.Category(target.Id),["direct"]=true,["originalEligible"]=true});
    return new BCarriageBuffCalculator(buffs).Damage(eventId,numeric);
   }
  }
  static string StateEvent(string kind,string id){using var sha=SHA256.Create();return "attack-buff-"+kind+":"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(BuffWire.ServerSourceId(kind,id)))).Replace("-","").ToLowerInvariant();}
  BDirectAttackBuffReceipt Execute(JObject c){
   string kind=BuffWire.Text(c["kind"]);BDirectAttackBuffReceipt receipt=null;
   if(kind=="ADVANCE"){BuffWire.Fields(c,"kind","elapsed","maximumSteps");direct.Advance(BuffWire.Number(c["elapsed"]),(int)BuffWire.Number(c["maximumSteps"],0,10000));}
   else if(kind=="PAUSE"){BuffWire.Fields(c,"kind","value");direct.SetPaused(BuffWire.Flag(c["value"]));}
   else if(kind=="SPEED"){BuffWire.Fields(c,"kind","value");direct.SetSpeed((int)BuffWire.Number(c["value"],0,4));}
   else{
    BuffWire.Need(kind=="BASIC"||kind=="ADVANCE_BASIC");if(kind=="BASIC")BuffWire.Fields(c,"kind","eventId","sourceId","targetId","tick","ability");else{BuffWire.Fields(c,"kind","eventId","sourceId","targetId","tick","ability","elapsed","maximumSteps");direct.Advance(BuffWire.Number(c["elapsed"]),(int)BuffWire.Number(c["maximumSteps"],0,10000));}
    string id=BuffWire.Text(c["eventId"]),source=BuffWire.Text(c["sourceId"]),target=BuffWire.Text(c["targetId"]);BuffWire.Need(!receipts.ContainsKey(id),"b_attack_buff_stored_duplicate");
    var budget=new Budget(binding,buffs,id,direct.WaveCategory,direct.Hp);int before=direct.Hp(target);var attack=direct.ApplyBasicImpactWithBudget(id,source,target,BuffWire.Number(c["tick"]),BuffWire.Text(c["ability"]),budget);
    var e=binding.Attack.Entities[target];if(before>0&&attack.Hp.HpAfter==0&&(e.Kind=="CARRIAGE"||e.Kind=="OPERATOR")){
     bool body=e.Kind=="CARRIAGE";buffs.Transition(new JObject{["eventId"]=StateEvent(body?"body":"actor",id),["expectedRevision"]=buffs.Revision,["kind"]=body?"BODY":"ACTOR",["slot"]=e.Slot,["value"]=body?"BROKEN":"DOWN"});}
    receipt=new BDirectAttackBuffReceipt(attack,budget.Accepted);receipts.Add(id,((JObject)c.DeepClone(),receipt));
   }
   BuffWire.Need(events.Count<eventCapacity,"b_attack_buff_capacity");events.Add(c.DeepClone());return receipt;
  }
  public JObject Capture()=>new JObject{["version"]=Version,["bindingHash"]=binding.OriginalHash,["capacity"]=eventCapacity,["events"]=events.DeepClone(),["direct"]=JObject.Parse(direct.CaptureLocal()),["buffs"]=buffs.Snapshot(),["receipts"]=new JArray(events.Where(e=>e["eventId"]!=null).Select(e=>receipts[(string)e["eventId"]].receipt.Snapshot()))};
  public static BDirectAttackBuffSession Restore(JObject cache,BDirectAttackBuffBinding binding,int capacity){
   BuffWire.Fields(cache,"version","bindingHash","capacity","events","direct","buffs","receipts");BuffWire.Need((string)cache["version"]==Version&&(string)cache["bindingHash"]==binding.OriginalHash&&BuffWire.Number(cache["capacity"],1,10000)==capacity,"b_attack_buff_cache_binding");
   BuffWire.Array(cache["receipts"],0,capacity);var r=new BDirectAttackBuffSession(binding,capacity);foreach(var row in BuffWire.Array(cache["events"],0,capacity))r.Execute(BuffWire.Object(row));
   BuffWire.Need(JToken.DeepEquals(r.Capture(),cache),"b_attack_buff_cache_replay");return r;
  }
 }
}




