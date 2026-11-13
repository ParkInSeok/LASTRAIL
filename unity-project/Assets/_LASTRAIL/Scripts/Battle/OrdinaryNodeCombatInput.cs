using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Preparation values only; the retained node still owns unsupported effects and spatial contracts.
 internal sealed class OrdinaryNodeCombatInput {
  internal OrdinaryNodeStart Source {get;}
  internal OrdinaryNodeActions Actions {get;}
  internal OrdinaryNodeAttempt Attempt {get;}
  internal string TrainId {get;}
  internal int TrainHp {get;} internal int TrainMaxHp {get;}
  readonly Dictionary<string,OrdinaryFiveCoreStatSnapshot.ActorStats> actors=new Dictionary<string,OrdinaryFiveCoreStatSnapshot.ActorStats>(StringComparer.Ordinal);
  internal OrdinaryFiveCoreStatSnapshot.ActorStats Actor(string id)=>actors[id];
  internal OrdinaryNodeCombatInput(OrdinaryFiveStartInput original,OrdinaryNodeStart node,FirstPlayableScaling encounter,long step,OrdinaryNodeAttempt attempt) {
   if(original==null||node==null||encounter==null)throw new ArgumentException("Original node and encounter required.");
   node.RequireOriginal(original);
   if(node.ExecutionOrigin==null||node.Enemies==null||node.Enemies.ScalingBindingHash!=encounter.BindingHash)throw new ArgumentException("Original node execution inputs required.");
   Actions=node.ReadActions(original);if(Actions.StepTimeSpanTicks!=step)throw new ArgumentException("Node logical step differs.");
   Attempt=attempt??throw new ArgumentNullException(nameof(attempt));Attempt.RequireNode(node);
   Source=node;var snapshot=node.Capture()["snapshot"];var instances=new HashSet<string>(StringComparer.Ordinal);
   foreach(var row in snapshot["actors"]){
    string id=(string)row["operatorId"];var action=Actions.Actors.Single(a=>a.OperatorId==id);
    var stats=new OrdinaryFiveCoreStatSnapshot.ActorStats(new JObject{["operatorId"]=id,["slot"]=row["slot"].DeepClone(),["currentHp"]=row["hp"].DeepClone(),["equipment"]=row["slots"].DeepClone(),["effectiveStats"]=row["effectiveStats"].DeepClone(),["conditionalDefenseFlat"]=action.ConditionalDefenseFlat,["skillMagnitude"]=action.SkillMagnitude.Snapshot()},instances);
    checked { _=(int)stats.MaxHp;_=(int)stats.CurrentHp;_=action.ActiveCooldownRemainingTicks*(step/10000); }
    actors.Add(id,stats);
   }
   var train=snapshot["train"];TrainId=(string)train["trainId"];TrainHp=checked((int)train["hp"]);TrainMaxHp=checked((int)train["maxHp"]);
   foreach(var enemy in node.Enemies.Gunners){
    if(enemy.Hp.Denominator!=1||enemy.MaxHp.Denominator!=1)throw new ArgumentException("Integral node enemy HP required.");
    checked{_=(int)enemy.Hp.Floor();_=(int)enemy.MaxHp.Floor();}
   }
  }
 }
}
