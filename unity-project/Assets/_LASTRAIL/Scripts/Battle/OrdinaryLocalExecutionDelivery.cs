using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Session-owned pending input only. HTTP envelope/authentication belongs to its transport adapter.
 public sealed class OrdinaryLocalExecutionDelivery {
  public sealed class Batch {
   public string Key {get;}
   public string BodyJson {get;}
   public long ExpectedSequence {get;}
   public long LastSequence {get;}
   public long Tick {get;}
   internal readonly JObject Checkpoint;
   internal readonly int Count;
   internal Batch(string key,OrdinaryNodeAttempt attempt,long sequence,IReadOnlyList<JObject> commands,JObject checkpoint){
    Key=key;ExpectedSequence=sequence;LastSequence=sequence+commands.Count;Count=commands.Count;
    Checkpoint=(JObject)checkpoint.DeepClone();Tick=(long)checkpoint["clock"]["completed"];
    BodyJson=new JObject{["runId"]=attempt.RunId,["nodeId"]=attempt.NodeId,["attemptGeneration"]=attempt.Generation,["snapshotHash"]=attempt.SnapshotHash,["expectedSequence"]=sequence,["inputs"]=new JArray(commands.Select(c=>c.DeepClone()))}.ToString(Formatting.None);
   }
  }
  const int MaximumBatch=128,MaximumSequence=10000;
  readonly OrdinaryNodeAttempt attempt;
  readonly Func<bool> current;
  readonly List<JObject> queued=new List<JObject>();
  readonly HashSet<string> usedKeys=new HashSet<string>(StringComparer.Ordinal);
  string acceptedKey,acceptedDigest;
  long observedTick,acceptedTick;
  bool retired,opened;
  public Batch Pending {get;private set;}
  public long AcceptedSequence {get;private set;}
  public int QueuedCount=>queued.Count;
  public OrdinaryLocalExecutionDelivery(OrdinaryNodeAttempt attempt,Func<bool> currentAttempt){
   this.attempt=attempt??throw new ArgumentNullException(nameof(attempt));current=currentAttempt??throw new ArgumentNullException(nameof(currentAttempt));
  }
  void Active(){if(retired||!current())throw new InvalidOperationException("ordinary_local_execution_stale_attempt");}
  public void Retire(){retired=true;}
  // Called from the committed model operation observer, never from a parallel UI command implementation.
  public void Queue(long actualBeforeTick,JObject actualCommand){
   Active();Need(actualBeforeTick>=observedTick&&actualBeforeTick>=acceptedTick&&actualBeforeTick<=OrdinaryNodeStart.SafeMaximum);
   Need(queued.Count<MaximumBatch&&AcceptedSequence+queued.Count<MaximumSequence);ValidateCommand(actualCommand);
   queued.Add(new JObject{["sequence"]=AcceptedSequence+queued.Count+1,["tick"]=actualBeforeTick,["command"]=actualCommand.DeepClone()});observedTick=actualBeforeTick;
  }
  public Batch Prepare(string key,JObject committedCheckpoint){
   Active();Need(RunRequestKey.IsSupported(key)&&!key.Contains(" "));
   if(Pending!=null){Need(Pending.Key==key);return Pending;}
   Need(!opened||queued.Count>0);RequireCheckpoint(committedCheckpoint);Need(usedKeys.Add(key));
   Pending=new Batch(key,attempt,AcceptedSequence,queued,committedCheckpoint);return Pending;
  }
  void RequireCheckpoint(JObject checkpoint){
   Need(checkpoint!=null&&(string)checkpoint["schema"]==OrdinaryFiveCombatFactory.CheckpointVersion&&checkpoint["clock"] is JObject);
   long tick=BDirectAttackInput.Nat(checkpoint["clock"],"completed",0,OrdinaryNodeStart.SafeMaximum);Need(tick>=observedTick&&tick>=acceptedTick);
   var owner=checkpoint["health"]?["definition"];
   Need(owner!=null&&(string)owner["accountId"]==attempt.AccountId&&(string)owner["runId"]==attempt.RunId&&(string)owner["nodeId"]==attempt.NodeId&&(long?)owner["generation"]==attempt.Generation&&(long?)owner["attemptRevision"]==attempt.Revision&&(string)owner["snapshotHash"]==attempt.SnapshotHash);
  }
  // A failed check leaves the frozen key, body and checkpoint available for original-key recovery.
  public bool Accept(string key,JObject engineResult){
   Active();Need(engineResult!=null);
   string digest=BDirectAttackInput.Digest(engineResult);
   if(key==acceptedKey){Need(digest==acceptedDigest);return false;}
   Need(Pending!=null&&key==Pending.Key);
   BDirectAttackInput.Fields(engineResult,"version","accountId","runId","nodeId","attemptGeneration","snapshotHash","lastSequence","tick","checkpoint","obstacleResolution","evidence","healthChanges");
   Need((string)engineResult["version"]==OrdinaryLocalExecutionReplay.ResultVersion&&(string)engineResult["accountId"]==attempt.AccountId&&(string)engineResult["runId"]==attempt.RunId&&(string)engineResult["nodeId"]==attempt.NodeId&&(long?)engineResult["attemptGeneration"]==attempt.Generation&&(string)engineResult["snapshotHash"]==attempt.SnapshotHash);
   Need(engineResult["lastSequence"]?.Type==JTokenType.Integer&&(long)engineResult["lastSequence"]==Pending.LastSequence&&engineResult["tick"]?.Type==JTokenType.Integer&&(long)engineResult["tick"]==Pending.Tick);
   Need(engineResult["healthChanges"] is JArray&&JToken.DeepEquals(WireNumbers(engineResult["checkpoint"]),WireNumbers(Pending.Checkpoint)));
   Need(JToken.DeepEquals(engineResult["obstacleResolution"],Pending.Checkpoint["obstacleSequence"]?["resolution"]??JValue.CreateNull()));
   Need(engineResult["evidence"]?.Type==JTokenType.Null||engineResult["evidence"] is JObject);
   queued.RemoveRange(0,Pending.Count);AcceptedSequence=Pending.LastSequence;acceptedTick=Pending.Tick;
   acceptedKey=key;acceptedDigest=digest;opened=true;Pending=null;return true;
  }
  static void ValidateCommand(JObject command){
   string kind=BDirectAttackInput.Text(command,"kind",40);
   if(kind=="ADVANCE"){
    BDirectAttackInput.Fields(command,"kind","elapsed","budget");BDirectAttackInput.Nat(command,"elapsed",0,10000000);BDirectAttackInput.Nat(command,"budget",0,1000);return;
   }
   BDirectAttackInput.Fields(command,"kind","value");
   if(kind=="PAUSE"||kind=="ULTIMATE_AUTO"){Need(command["value"]?.Type==JTokenType.Boolean);return;}
   if(kind=="SPEED"){long value=BDirectAttackInput.Nat(command,"value",2,4);Need(value==2||value==3||value==4);return;}
   if(kind=="THROTTLE"){BDirectAttackInput.Nat(command,"value",7000,13000);return;}
   throw new ArgumentException("ordinary_local_execution_command_unsupported");
  }
  static JToken WireNumbers(JToken source){
   var copy=source.DeepClone();
   // JavaScript JSON preserves these exact integers but drops their trailing .0.
   foreach(var value in ((JContainer)copy).Descendants().OfType<JValue>().ToArray()){
    if(value.Type!=JTokenType.Float)continue;double number=(double)value;
    if(Math.Abs(number)<=9007199254740991d&&number==Math.Truncate(number))value.Replace(new JValue((long)number));
   }
   return copy;
  }
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_local_execution_delivery_conflict");}
 }
}
