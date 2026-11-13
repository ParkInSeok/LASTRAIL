using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.RoofBattle {
 // Explicit single B-native LOCAL composition. Store delegates are CAS/atomic ports, never a second simulation.
 public sealed class RoofBattleLocalOwner:IDisposable {
  public const string Version="unity.roof-battle-local-checkpoint.v1.20261017";
  public const string Contract="roof-battle-presentation.v1.20261017";
  readonly BDirectAttackBasicLoopBinding binding;readonly int capacity;readonly string context;
  readonly Func<JObject> load;readonly Action<JObject,long> save;readonly int thread;
  readonly List<Subscription> subscriptions=new List<Subscription>();
  BDirectAttackBasicLoop current;JArray journal=new JArray();long revision;bool recovery,busy,publishing,disposed;
  public bool RecoveryRequired=>recovery;
  public RoofBattleLocalOwner(BDirectAttackBasicLoopBinding binding,int capacity,string context,Func<JObject> load,Action<JObject,long> save){
   this.binding=binding??throw new ArgumentNullException(nameof(binding));this.load=load??throw new ArgumentNullException(nameof(load));this.save=save??throw new ArgumentNullException(nameof(save));
   if(capacity<1||capacity>10000||string.IsNullOrWhiteSpace(context))throw new ArgumentException("Bounded explicit LOCAL context required.");
   var input=binding.Composition.Attack;var targets=(JArray)binding.Capture()["targets"];
   if((string)input.Capture()["version"]!=BDirectAttackInput.EveVersion||(string)binding.Capture()["version"]!=BDirectAttackBasicLoopBinding.Version||input.Formation.Mode!=BCombatMode.Solo||input.Formation.Battlefield!=BCombatBattlefield.ExternalVertical||input.Formation.Slots.Count!=1||input.Formation.Slots[0].OperatorId!="op_eve"||targets.Count!=1||(string)targets[0]["sourceId"]!="op_eve")throw new ArgumentException("This first roof owner supports only the existing Eve single-actor LOCAL basic profile. No fallback or admission.");
   this.capacity=capacity;this.context=context;thread=Thread.CurrentThread.ManagedThreadId;
  }
  void ThreadCheck(){if(disposed)throw new ObjectDisposedException(nameof(RoofBattleLocalOwner));if(Thread.CurrentThread.ManagedThreadId!=thread)throw new InvalidOperationException("Use the original owner thread.");}
  void Writable(){ThreadCheck();if(busy||publishing)throw new InvalidOperationException("No reentrant combat command or binding.");}
  public void Open(){
   Writable();recovery=true;try{var cache=load();BDirectAttackBasicLoop restored;JArray originalJournal;long nextRevision;
   if(cache==null){if(current!=null)throw new System.IO.IOException("Original LOCAL checkpoint is missing; do not restart this context.");restored=new BDirectAttackBasicLoop(binding,capacity);originalJournal=new JArray();nextRevision=1;save(Envelope(restored,originalJournal,nextRevision),0);}
   else{
    BDirectAttackInput.Fields(cache,"version","contract","contextId","bindingHash","revision","cursor","native","journal","hash");
    var body=(JObject)cache.DeepClone();body.Remove("hash");
    if((string)cache["version"]!=Version||(string)cache["contract"]!=Contract||(string)cache["contextId"]!=context||(string)cache["bindingHash"]!=binding.OriginalHash||BDirectAttackInput.Digest(body)!=(string)cache["hash"])throw new ArgumentException("Roof checkpoint source/context/hash mismatch.");
    restored=BDirectAttackBasicLoop.Restore((JObject)cache["native"],binding,capacity);originalJournal=RebuildJournal(restored);
    nextRevision=(long)cache["revision"];
    if(nextRevision!=((JArray)restored.Capture()["commands"]).Count+1||(long)cache["cursor"]!=originalJournal.Count||!JToken.DeepEquals(cache["journal"],originalJournal))throw new ArgumentException("Roof checkpoint original event/cursor replay mismatch.");
   }
   current=restored;journal=originalJournal;revision=nextRevision;recovery=false;
   }catch(Exception error){PublishFailure(error);throw;}
   Publish("SNAPSHOT",new JArray());
  }
  JObject Envelope(BDirectAttackBasicLoop native,JArray events,long rev){var root=new JObject{["version"]=Version,["contract"]=Contract,["contextId"]=context,["bindingHash"]=binding.OriginalHash,["revision"]=rev,["cursor"]=events.Count,["native"]=native.Capture(),["journal"]=events.DeepClone()};root["hash"]=BDirectAttackInput.Digest(root);return root;}
  JArray Events(BDirectAttackBasicLoopResult result,long startCursor){
   var rows=new JArray();
   foreach(var execution in result.ExecutedBasics.OrderBy(e=>e.Sequence)){
    var fact=execution.Capture();var hit=result.Hits.Single(h=>h.Attack.EventId==execution.ActionId);
    foreach(string phase in new[]{"EXECUTED_INSTANT","HIT"}){
     var row=new JObject{["eventId"]=execution.ActionId+":"+phase,["actionId"]=execution.ActionId,["phase"]=phase,["contextId"]=context,["bindingHash"]=binding.OriginalHash,["cursor"]=startCursor+rows.Count+1,["sequence"]=execution.Sequence,["tick"]=execution.Tick,["stepTimeSpanTicks"]=binding.Composition.Attack.StepTimeSpanTicks,["sourceId"]=execution.SourceId,["sourceDefinitionId"]=binding.Composition.Attack.Entities[execution.SourceId].Definition,["sourceInputVersion"]=(string)binding.Composition.Attack.Capture()["version"],["targetId"]=execution.TargetId,["sourceGeneration"]=JValue.CreateNull(),["targetGeneration"]=JValue.CreateNull(),["generationReason"]="STATIC_LOCAL_PROFILE_HAS_NO_GENERATION_SOURCE",["projectileId"]=JValue.CreateNull(),["worldPoint"]=JValue.CreateNull(),["spatialStatus"]="DISTANCE_ONLY_LOCAL_INPUT_NO_WORLD_CONTACT"};
     if(phase=="EXECUTED_INSTANT")row["nativeExecution"]=fact.DeepClone();else row["nativeReceipt"]=hit.Snapshot();rows.Add(row);
    }
   }
   return rows;
  }
  BDirectAttackBasicLoopResult Apply(BDirectAttackBasicLoop native,JObject command){string id=(string)command["eventId"];switch((string)command["kind"]){case "ADVANCE":return native.Advance(id,(long)command["elapsed"],(int)command["maximumSteps"]);case "PAUSE":return native.SetPaused(id,(bool)command["value"]);case "SPEED":return native.SetSpeed(id,(int)command["value"]);default:throw new ArgumentException("Unsupported command in this roof LOCAL profile.");}}
  JArray RebuildJournal(BDirectAttackBasicLoop restored){var rebuilt=new JArray();foreach(JObject command in (JArray)restored.Capture()["commands"]){var result=Apply(restored,command);foreach(var e in Events(result,rebuilt.Count))rebuilt.Add(e.DeepClone());}return rebuilt;}
  public JObject Advance(string id,long elapsed,int maximumSteps)=>Commit(new JObject{["kind"]="ADVANCE",["eventId"]=id,["elapsed"]=elapsed,["maximumSteps"]=maximumSteps});
  public JObject SetPaused(string id,bool value)=>Commit(new JObject{["kind"]="PAUSE",["eventId"]=id,["value"]=value});
  public JObject SetSpeed(string id,int halfUnits)=>Commit(new JObject{["kind"]="SPEED",["eventId"]=id,["value"]=halfUnits});
  JObject Commit(JObject command){
   Writable();if(current==null||recovery)throw new InvalidOperationException("Restore the original roof context first.");busy=true;JArray added;
   try{
    var before=current.Capture();var candidate=BDirectAttackBasicLoop.Restore(before,binding,capacity);var result=Apply(candidate,command);
    if(JToken.DeepEquals(before,candidate.Capture()))return Snapshot();
    added=Events(result,journal.Count);var all=(JArray)journal.DeepClone();foreach(var e in added)all.Add(e.DeepClone());
    var envelope=Envelope(candidate,all,checked(revision+1));
    try{save((JObject)envelope.DeepClone(),revision);}catch(Exception error){PublishFailure(error);throw;}
    current=candidate;journal=all;revision++;
   }finally{busy=false;}
   Publish("COMMIT",added);return Snapshot();
  }
  public JObject Snapshot(){ThreadCheck();if(current==null)throw new InvalidOperationException("Open the original roof context first.");return Frame("SNAPSHOT",new JArray());}
  JObject Frame(string kind,JArray events){return new JObject{["version"]="unity.roof-battle-presentation-frame.v1.20261017",["contract"]=Contract,["kind"]=kind,["contextId"]=context,["bindingHash"]=binding.OriginalHash,["revision"]=revision,["cursor"]=journal.Count,["tick"]=current.Tick,["stepTimeSpanTicks"]=binding.Composition.Attack.StepTimeSpanTicks,["paused"]=recovery||current.Paused,["nativePaused"]=current.Paused,["recoveryRequired"]=recovery,["outcome"]=current.Outcome,["hp"]=new JObject(binding.Composition.Attack.Entities.Keys.Select(id=>new JProperty(id,current.Hp(id)))),["nativeSnapshot"]=current.Capture(),["events"]=events.DeepClone()};}
  public IDisposable Subscribe(Action<JObject> consumer){Writable();if(consumer==null)throw new ArgumentNullException(nameof(consumer));var subscription=new Subscription(this,consumer);subscriptions.Add(subscription);try{publishing=true;try{consumer(Snapshot());}finally{publishing=false;}return subscription;}catch{subscription.Dispose();throw;}}
  void PublishFailure(Exception original){recovery=true;busy=false;try{Publish("HOLD",new JArray());}catch(Exception notification){throw new AggregateException("Original LOCAL persistence/recovery failure and presentation notification failure.",original,notification);}}
  void Publish(string kind,JArray events){if(current==null)return;publishing=true;var failures=new List<Exception>();try{foreach(var subscription in subscriptions.ToArray())if(subscription.Active)try{subscription.Consumer(Frame(kind,events));}catch(Exception e){failures.Add(e);}}finally{publishing=false;}if(failures.Count>0)throw new AggregateException("Presentation failed after the original committed state was published.",failures);}
  sealed class Subscription:IDisposable{readonly RoofBattleLocalOwner owner;internal readonly Action<JObject> Consumer;internal bool Active=true;internal Subscription(RoofBattleLocalOwner owner,Action<JObject> consumer){this.owner=owner;Consumer=consumer;}public void Dispose(){if(!Active)return;owner.ThreadCheck();Active=false;owner.subscriptions.Remove(this);}}
  public void Dispose(){Writable();foreach(var s in subscriptions.ToArray())s.Dispose();current=null;journal.Clear();disposed=true;}
 }
}
