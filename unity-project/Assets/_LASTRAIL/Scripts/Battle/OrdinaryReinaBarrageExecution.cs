using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Trusted read-only ports; adapters must supply original targeting, rigid frame and solid geometry.
 internal sealed class OrdinaryReinaBarrageSources {
  internal readonly Func<string,long,JObject> Observe,CaptureAttack,CaptureDefense;
  internal readonly ICombatSkillLifestealAuthority Lifesteal;
  internal readonly Func<string,long,JObject,JObject> ObserveCurrent;
  internal OrdinaryReinaBarrageSources(Func<string,long,JObject> observe,Func<string,long,JObject> captureAttack,Func<string,long,JObject> captureDefense,ICombatSkillLifestealAuthority lifesteal):this(observe,captureAttack,captureDefense,lifesteal,null){}
  internal OrdinaryReinaBarrageSources(Func<string,long,JObject> observe,Func<string,long,JObject> captureAttack,Func<string,long,JObject> captureDefense,ICombatSkillLifestealAuthority lifesteal,Func<string,long,JObject,JObject> observeCurrent){
   ObserveCurrent=observeCurrent;
   Observe=observe??throw new ArgumentNullException(nameof(observe));CaptureAttack=captureAttack??throw new ArgumentNullException(nameof(captureAttack));CaptureDefense=captureDefense??throw new ArgumentNullException(nameof(captureDefense));
   Lifesteal=lifesteal??throw new ArgumentNullException(nameof(lifesteal));if(string.IsNullOrWhiteSpace(lifesteal.BindingId))throw new ArgumentException("Original lifesteal binding required.");
  }
 }
 // Explicit new-definition component only. No legacy factory installation or catalog substitution.
 internal sealed class OrdinaryReinaBarrageExecution {
  const string ActorId="op_reina";
  const int Capacity=10000;
  readonly OrdinaryFiveCombatFactory factory;readonly BCombatLocalModel health;readonly OrdinaryReinaBarrageSources sources;
  readonly int cost;readonly long stepUs,windup,recovery;readonly string lifestealBinding;
  OrdinaryNodeSkillLifesteal lifesteal;
  readonly HashSet<string> commands=new HashSet<string>(StringComparer.Ordinal);
  readonly List<JObject> actions=new List<JObject>();
  // Standalone input order is local; the host overload supplies its existing command owner order.
  long lastTick=-1,recoveryEnd=-1,sequence;bool retired;
  internal OrdinaryReinaBarrageExecution(OrdinaryFiveCombatFactory factory,BCombatLocalModel health,OrdinaryReinaBarrageSources sources,int cost){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));this.sources=sources??throw new ArgumentNullException(nameof(sources));
   Need(factory.NodeInput!=null&&factory.Attempt!=null&&cost>0&&factory.Step>0&&factory.Step%10==0);
   this.cost=cost;stepUs=factory.Step/10;windup=Ticks(800);recovery=Ticks(300);lifestealBinding=sources.Lifesteal.BindingId;
   Need(factory.Original.Actors.Any(a=>a.OperatorId==ActorId));RequireHealth(health);lifesteal=new OrdinaryNodeSkillLifesteal(factory,health,sources.Lifesteal);
  }
  static void Need(bool valid){if(!valid)throw new ArgumentException("Explicit original Reina barrage source differs.");}
  long Ticks(long ms)=>checked((ms*1000+stepUs-1)/stepUs);
  void RequireHealth(BCombatLocalModel candidate){
   var state=candidate.CaptureIdentity();var d=state["definition"];var a=factory.Attempt;
   Need((string)state["schema"]=="unity.combat-b.node-health-local.v1.20261102"&&d!=null&&(string)d["accountId"]==a.AccountId&&(string)d["runId"]==a.RunId&&(string)d["nodeId"]==a.NodeId&&(string)d["snapshotHash"]==a.SnapshotHash&&(long?)d["generation"]==a.Generation&&(long?)d["attemptRevision"]==a.Revision);
  }
  static long Integer(JToken token,long minimum,long maximum){Need(token?.Type==JTokenType.Integer);long value=(long)token;Need(value>=minimum&&value<=maximum);return value;}
  static bool Boolean(JToken token){Need(token?.Type==JTokenType.Boolean);return (bool)token;}
  static string Identity(JToken token){Need(token?.Type==JTokenType.String);string value=(string)token;BCombatFormation.Identity(value);return value;}
  // shortcut: exact integer-mm projections only; support fractional projections when original frame/unit authority is supplied, never quantize silently.
  static long[] Point(JToken value){Need(value is JArray a&&a.Count==3);return value.Select(x=>Integer(x,-1000000000,1000000000)).ToArray();}
  static BigInteger Square(JToken from,JToken to){var a=Point(from);var b=Point(to);BigInteger sum=0;for(int i=0;i<3;i++){BigInteger d=(BigInteger)a[i]-b[i];sum+=d*d;}return sum;}
  static double[][] Frame(JToken frame){
   Need(frame is JObject);Identity(frame["id"]);Integer(frame["generation"],1,long.MaxValue);Boolean(frame["alive"]);Point(frame["originMm"]);
   Need(frame["basis"] is JArray rows&&rows.Count==3);
   var basis=new double[3][];
   for(int i=0;i<3;i++){
    Need(frame["basis"][i] is JArray row&&row.Count==3);basis[i]=new double[3];
    for(int j=0;j<3;j++){var n=frame["basis"][i][j];Need(n.Type==JTokenType.Integer||n.Type==JTokenType.Float);basis[i][j]=(double)n;Need(!double.IsNaN(basis[i][j])&&!double.IsInfinity(basis[i][j])&&Math.Abs(basis[i][j])<=1);}
   }
   for(int i=0;i<3;i++)for(int j=0;j<3;j++)Need(Math.Abs(basis[i].Zip(basis[j],(x,y)=>x*y).Sum()-(i==j?1:0))<=1e-9);
   double determinant=basis[0][0]*(basis[1][1]*basis[2][2]-basis[1][2]*basis[2][1])-basis[0][1]*(basis[1][0]*basis[2][2]-basis[1][2]*basis[2][0])+basis[0][2]*(basis[1][0]*basis[2][1]-basis[1][1]*basis[2][0]);
   Need(Math.Abs(determinant-1)<=1e-9);return basis;
  }
  JObject Observe(string command,long tick,JObject prior=null){
   var supplied=sources.ObserveCurrent==null?sources.Observe(command,tick):sources.ObserveCurrent(command,tick,prior);Need(supplied!=null);var o=(JObject)supplied.DeepClone();
   Need(Integer(o["tick"],0,long.MaxValue)==tick&&Identity(o["nodeId"])==factory.Attempt.NodeId&&Identity(o["snapshotHash"])==factory.Attempt.SnapshotHash&&Integer(o["attemptGeneration"],1,long.MaxValue)==factory.Attempt.Generation);
   long revision=Integer(o["revision"],0,long.MaxValue);if(prior!=null){Need(revision>=(long)prior["revision"]);if((long)prior["tick"]==tick)Need(JToken.DeepEquals(o,prior));}
   var s=o["source"];Need(s is JObject&&Identity(s["id"])==ActorId);Integer(s["generation"],1,long.MaxValue);Identity(s["executorId"]);Need(Identity(s["abilityId"])=="op_reina.ultimate");Point(s["muzzleMm"]);Boolean(s["actionEligible"]);
   if(o["primaryTargetId"]?.Type!=JTokenType.Null)Identity(o["primaryTargetId"]);
   var basis=Frame(o["frame"]);var origin=Point(o["frame"]["originMm"]);string frameId=(string)o["frame"]["id"];
   Need(o["targets"] is JArray targets&&targets.Count<=Capacity&&o["solidOccluders"] is JArray solids&&solids.Count<=Capacity);
   var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(var target in (JArray)o["targets"]){
    Need(target is JObject);string id=Identity(target["id"]);Need(ids.Add(id)&&factory.EnemyIds.Contains(id));
    Integer(target["generation"],1,long.MaxValue);Integer(target["battleOrder"],0,long.MaxValue);Integer(target["carDelta"],-1000000,1000000);Boolean(target["eligible"]);Boolean(target["castLineOfSight"]);Boolean(target["protected"]);
    // frameId identifies the supplied projection frame, not the target's owning train/car frame.
    Need(Identity(target["frameId"])==frameId);var local=Point(target["localAnchorMm"]);var world=Point(target["worldAnchorMm"]);
    for(int i=0;i<3;i++){double expected=origin[i];for(int j=0;j<3;j++)expected+=basis[j][i]*local[j];Need(Math.Abs(expected-world[i])<=1e-6);}
   }
   ids.Clear();foreach(var solid in (JArray)o["solidOccluders"]){Need(solid is JObject&&ids.Add(Identity(solid["id"])));Point(solid["centerMm"]);Need(Point(solid["sizeMm"]).All(x=>x>0));}
   Need(sources.Lifesteal.BindingId==lifestealBinding);return o;
  }
  static bool Clear(JObject o,JToken center,JToken anchor){
   var from=Point(center);var to=Point(anchor);
   return !((JArray)o["solidOccluders"]).Any(b=>MiraSweepKernel.Box(from,to,Point(b["centerMm"]),Point(b["centerMm"]),Point(b["sizeMm"]),0).HasValue);
  }
  static bool CastEligible(JObject o,JToken t,Func<string,bool> alive)=>t!=null&&(bool)t["eligible"]&&alive((string)t["id"])&&(bool)t["castLineOfSight"]&&Math.Abs((long)t["carDelta"])<=1&&Square(o["source"]["muzzleMm"],t["worldAnchorMm"])<=(BigInteger)24000*24000;
  static bool AreaEligible(JObject o,JToken t,JToken center)=>t!=null&&(bool)t["eligible"]&&(string)t["frameId"]==(string)o["frame"]["id"]&&Square(center,t["localAnchorMm"])<=(BigInteger)3000*3000&&Clear(o,center,t["localAnchorMm"]);
  // Pure selection also supports four-target geometry tests without creating a second HP ledger.
  internal static JArray SelectCommitTargets(JObject observation,string primaryId,Func<string,bool> alive){
   var targets=(JArray)observation["targets"];var primary=targets.SingleOrDefault(t=>(string)t["id"]==primaryId);var result=new JArray();
   if(!(bool)observation["frame"]["alive"]||!CastEligible(observation,primary,alive)||!AreaEligible(observation,primary,primary["localAnchorMm"]))return result;
   result.Add(primary.DeepClone());foreach(var extra in targets.Where(t=>(string)t["id"]!=primaryId&&CastEligible(observation,t,alive)&&AreaEligible(observation,t,primary["localAnchorMm"]))
    .OrderBy(t=>Square(primary["localAnchorMm"],t["localAnchorMm"])).ThenBy(t=>(long)t["battleOrder"]).ThenBy(t=>(string)t["id"],StringComparer.Ordinal).Take(3))result.Add(extra.DeepClone());return result;
  }
  bool Alive(string id)=>health.Hp(id)>0;
  bool ImpactEligible(JObject observation,JToken frozen,JToken live,JToken center)=>live!=null&&(long)live["generation"]==(long)frozen["generation"]&&Alive((string)frozen["id"])&&AreaEligible(observation,live,center);
  // Borrow the last validated logical observation; opening or refreshing a view never reobserves geometry.
  internal JArray ReadFeedback(){
   var rows=new JArray();if(retired)return rows;
   foreach(var action in actions.Where(a=>(string)a["phase"]=="COMMITTED")){
    var observation=(JObject)action["observation"];var targets=new JArray();
    foreach(var frozen in (JArray)action["targets"]){
     var live=((JArray)observation["targets"]).SingleOrDefault(t=>(string)t["id"]==(string)frozen["id"]);
     bool sameGeneration=live!=null&&(long)live["generation"]==(long)frozen["generation"];
     targets.Add(new JObject{["id"]=frozen["id"].DeepClone(),["generation"]=frozen["generation"].DeepClone(),
      ["eligible"]=ImpactEligible(observation,frozen,live,action["centerMm"]),["protected"]=sameGeneration?live["protected"].DeepClone():JValue.CreateNull()});
    }
    rows.Add(new JObject{["commandId"]=action["commandId"].DeepClone(),["frame"]=observation["frame"].DeepClone(),
     ["centerMm"]=action["centerMm"].DeepClone(),["radiusMm"]=3000,["nextPulse"]=action["nextPulse"].DeepClone(),["targets"]=targets});
   }
   return rows;
  }
  internal string Request(string commandId,long tick,int availableCharge){
   Identity(new JValue(commandId));Need(tick>=0&&(tick==lastTick||tick==lastTick+1)&&availableCharge>=0);
   if(retired)return "RETIRED";if(commands.Contains(commandId))return "DUPLICATE";
   if(actions.Any(a=>(string)a["phase"]=="WINDUP")||tick<recoveryEnd)return "BUSY";
   if(availableCharge<cost)return "INSUFFICIENT_CHARGE";if(!Alive(ActorId))return "DOWN";
   Need(commands.Count<Capacity);var o=Observe(commandId,tick);var s=o["source"];
   if(!(bool)s["actionEligible"])return "INELIGIBLE";
   var selected=SelectCommitTargets(o,(string)o["primaryTargetId"],Alive);if(selected.Count==0)return "NO_TARGET";
   commands.Add(commandId);actions.Add(new JObject{["commandId"]=commandId,["inputSequence"]=checked(++sequence),["phase"]="WINDUP",["requestedTick"]=tick,["commitTick"]=checked(tick+windup),["source"]=s.DeepClone(),["primaryId"]=selected[0]["id"].DeepClone(),["primaryGeneration"]=selected[0]["generation"].DeepClone(),["observation"]=o,["frame"]=o["frame"].DeepClone(),["nextPulse"]=0});return "ACCEPTED";
  }
  internal bool HasPending=>!retired&&actions.Count>0;
  internal bool IsPreparing(string commandId)=>actions.Any(a=>(string)a["commandId"]==commandId&&(string)a["phase"]=="WINDUP");
  internal JObject ReadActor(string id){Need(id==ActorId);string phase=retired?"RETIRED":!Alive(id)?"DOWN":actions.Any(a=>(string)a["phase"]=="WINDUP")?"WINDUP":lastTick<recoveryEnd?"RECOVERY":"IDLE";return new JObject{["actorId"]=id,["phase"]=phase,["reserved"]=actions.Any(a=>(string)a["phase"]=="WINDUP"),["recoveryEnd"]=recoveryEnd};}
  internal IReadOnlyList<JObject> Step(long tick,int availableCharge)=>StepCore(tick,availableCharge,null);
  internal IReadOnlyList<JObject> Step(long tick,int availableCharge,Func<string,long> commandSequence){if(commandSequence==null)throw new ArgumentNullException(nameof(commandSequence));return StepCore(tick,availableCharge,commandSequence);}
  IReadOnlyList<JObject> StepCore(long tick,int availableCharge,Func<string,long> commandSequence){
   Need(tick>=0&&availableCharge>=0);if(tick==lastTick)return Array.Empty<JObject>();Need(tick==lastTick+1);
   var results=new List<JObject>();if(retired){lastTick=tick;return results.AsReadOnly();}
   foreach(var action in actions.ToArray()){
    string command=(string)action["commandId"];long order=commandSequence==null?(long)action["inputSequence"]:commandSequence(command);Need(order>0);if(action["globalSequence"]!=null)Need((long)action["globalSequence"]==order);else action["globalSequence"]=order;
    var o=Observe(command,tick,(JObject)action["observation"]);var source=o["source"];var frame=o["frame"];
    if((string)action["phase"]=="WINDUP"){
     var primary=((JArray)o["targets"]).SingleOrDefault(t=>(string)t["id"]==(string)action["primaryId"]);
     bool valid=Alive(ActorId)&&(bool)source["actionEligible"]&&(long)source["generation"]==(long)action["source"]["generation"]&&(string)source["abilityId"]==(string)action["source"]["abilityId"]&&(string)source["executorId"]==(string)action["source"]["executorId"]
      &&CastEligible(o,primary,Alive)&&(long?)primary?["generation"]==(long?)action["primaryGeneration"]&&(bool)frame["alive"];
     if(!valid){actions.Remove(action);continue;}
     if(tick>=(long)action["commitTick"]){
      if(availableCharge<cost){actions.Remove(action);continue;}
      var selected=SelectCommitTargets(o,(string)action["primaryId"],Alive);if(selected.Count==0){actions.Remove(action);continue;}
      var supplied=sources.CaptureAttack(ActorId,tick);Need(supplied!=null);var attack=(JObject)supplied.DeepClone();_=CombatProjectileAttackSnapshot.Capture(attack);
      action["attack"]=attack;action["targets"]=selected;action["centerMm"]=selected[0]["localAnchorMm"].DeepClone();action["frame"]=frame.DeepClone();action["source"]=source.DeepClone();action["phase"]="COMMITTED";
      action["schedule"]=new JArray(new long[]{1400,2200,3000,3800}.Select(ms=>checked(tick+Ticks(ms))));recoveryEnd=checked(tick+recovery);
      results.Add(new JObject{["kind"]="COMMIT",["commandId"]=command,["actorId"]=ActorId,["abilityId"]=source["abilityId"].DeepClone(),["executorId"]=source["executorId"].DeepClone(),["tick"]=tick,["cost"]=cost});
     }
    }else if(!(bool)frame["alive"]||(string)frame["id"]!=(string)action["frame"]["id"]||(long)frame["generation"]!=(long)action["frame"]["generation"]){
     results.Add(new JObject{["kind"]="CANCEL",["id"]="reina-cancel:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,factory.Attempt.Generation,command)).Substring(7),["commandId"]=command,["nodeId"]=factory.Attempt.NodeId,["attemptGeneration"]=factory.Attempt.Generation,["tick"]=tick,["reason"]="FRAME_LOST",["resolvedPulses"]=action["nextPulse"].DeepClone(),["cancelledPulses"]=4-(int)action["nextPulse"]});
     actions.Remove(action);continue;
    }
    action["observation"]=o;
    if((string)action["phase"]!="COMMITTED")continue;
    while((int)action["nextPulse"]<4&&tick>=(long)action["schedule"][(int)action["nextPulse"]]){
    int pulse=(int)action["nextPulse"];
    results.Add(new JObject{["kind"]="IMPACT",["id"]="reina-impact:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,factory.Attempt.Generation,command,pulse)).Substring(7),["commandId"]=command,["abilityId"]="op_reina.ultimate",["nodeId"]=factory.Attempt.NodeId,["attemptGeneration"]=factory.Attempt.Generation,["pulseIndex"]=pulse,["tick"]=tick,["frame"]=frame.DeepClone(),["centerMm"]=action["centerMm"].DeepClone()});
    var eligible=((JArray)action["targets"]).Select((t,i)=>new{Frozen=t,Index=i,Live=((JArray)o["targets"]).SingleOrDefault(l=>(string)l["id"]==(string)t["id"])})
     .Where(t=>ImpactEligible(o,t.Frozen,t.Live,action["centerMm"])).ToArray();
    // Freeze identities together; defense remains current after preceding targets' confirmed effects.
    foreach(var t in eligible){
     var attack=(JObject)action["attack"].DeepClone();attack["completedAttack"]=CombatExactValue.From((JObject)attack["completedAttack"]).Multiply(CombatExactValue.Create(t.Index==0?250:75,100)).Snapshot();
     var defense=sources.CaptureDefense((string)t.Frozen["id"],tick);Need(defense!=null);int magnitude=CombatProjectileAttackSnapshot.Capture(attack).Evaluate((JObject)defense.DeepClone()).ScaleAndFloor(10000);
     string target=(string)t.Frozen["id"];long generation=(long)t.Frozen["generation"];
     string eventId="reina-barrage:"+BDirectAttackInput.Digest(new JArray(factory.ConfigurationVersion,factory.Attempt.Generation,command,pulse,generation,target)).Substring(7);
     var receipt=health.ApplyResolvedDamage(eventId+":hp",target,magnitude,tick,(bool)t.Live["protected"]);var hp=new JObject{["eventId"]=receipt.EventId,["targetId"]=receipt.TargetId,["tick"]=receipt.Tick,["hpLoss"]=receipt.HpLoss,["hpAfter"]=receipt.HpAfter,["absorbed"]=receipt.Absorbed,["trainDefeated"]=receipt.TrainDefeated};
     var row=new JObject{["kind"]="DAMAGE",["eventId"]=eventId,["id"]=eventId,["commandId"]=command,["actorId"]=ActorId,["sourceId"]=ActorId,["sourceKind"]="SKILL",["abilityId"]=action["source"]["abilityId"].DeepClone(),["executorId"]=action["source"]["executorId"].DeepClone(),["targetId"]=target,["targetGeneration"]=generation,["pulseIndex"]=pulse,["tick"]=tick,["globalSequence"]=action["globalSequence"].DeepClone(),
      ["runId"]=factory.Attempt.RunId,["nodeId"]=factory.Attempt.NodeId,["attemptGeneration"]=factory.Attempt.Generation,["profileSha256"]=BasicCombatSimulation.Digest(factory.ConfigurationVersion),["actualResult"]=new JObject{["hpLoss"]=receipt.HpLoss,["absorbed"]=receipt.Absorbed},["healthReceipt"]=hp.DeepClone(),["receipt"]=hp};
     lifesteal.Apply(row,receipt);results.Add(row);
    }
    action["nextPulse"]=pulse+1;if(pulse==3)actions.Remove(action);
    }
   }
   lastTick=tick;return results.AsReadOnly();
  }
  internal void Retire(){retired=true;actions.Clear();}
  internal bool Cancel(string commandId){BCombatFormation.Identity(commandId);var action=actions.SingleOrDefault(a=>(string)a["commandId"]==commandId&&(string)a["phase"]=="WINDUP");return action!=null&&actions.Remove(action);}
  internal OrdinaryReinaBarrageExecution CloneOwned(BCombatLocalModel newHealth)=>CloneOwned(newHealth,null);
  internal OrdinaryReinaBarrageExecution CloneOwned(BCombatLocalModel newHealth,OrdinaryReinaBarrageSources rebound){
   Need(newHealth!=null&&!ReferenceEquals(health,newHealth)&&health.HasSameState(newHealth));
   var copy=new OrdinaryReinaBarrageExecution(factory,newHealth,rebound??sources,cost){lastTick=lastTick,recoveryEnd=recoveryEnd,sequence=sequence,retired=retired};copy.lifesteal=lifesteal.CloneOwned(newHealth,rebound?.Lifesteal);foreach(string command in commands)copy.commands.Add(command);foreach(var action in actions)copy.actions.Add((JObject)action.DeepClone());return copy;
  }
  internal JObject Capture()=>new JObject{["version"]="ordinary.reina-barrage-execution.v1.20261102",["configuration"]=factory.ConfigurationVersion,["nodeId"]=factory.Attempt.NodeId,["attemptGeneration"]=factory.Attempt.Generation,["lastTick"]=lastTick,["recoveryEnd"]=recoveryEnd,["sequence"]=sequence,["retired"]=retired,["cost"]=cost,["lifestealBinding"]=lifestealBinding,["lifesteal"]=lifesteal.Capture(),["commands"]=new JArray(commands.OrderBy(x=>x,StringComparer.Ordinal)),["actions"]=new JArray(actions.Select(a=>a.DeepClone()))};
 }
}
