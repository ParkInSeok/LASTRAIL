using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // Immutable optional node prefix. No copied enemy list, HP, clock or execution permission.
 public sealed class OrdinaryObstaclePin {
  readonly JObject original;
  OrdinaryObstaclePin(JObject value){original=(JObject)value.DeepClone();}
  public JObject Capture()=>(JObject)original.DeepClone();
  internal JObject NextWave=>(JObject)original["nextWave"].DeepClone();
  static JObject Definition()=>JObject.Parse(@"{""version"":""ordinary.falling-obstacle.v1.20261106"",""hp"":100,""defense"":0,""shotDamage"":25,""launchIntervalMs"":1650,""preparationMs"":150,""rangeMm"":100000,""projectileSpeedMmPerSecond"":300000,""zoneStartOffsetMm"":277000,""zoneEndOffsetMm"":283000,""hazardAtMs"":16000,""horizontalBaseDamage"":600,""fallingBaseDamage"":900,""cruiseSpeedMmPerSecond"":20000,""slowSpeedMmPerSecond"":14000,""fastSpeedMmPerSecond"":26000,""accelerationMmPerSecondSquared"":5000}");
  internal static JObject CombatProfile(JObject encounter){
   if(encounter==null)throw new ArgumentException("ordinary_obstacle_definition_invalid");
   var result=(JObject)encounter.DeepClone();
   if(result.Property("ordinaryObstacle")!=null){
    Need(JToken.DeepEquals(result["ordinaryObstacle"],Definition()),"definition_invalid");result.Remove("ordinaryObstacle");
   }
   return result;
  }
  internal static OrdinaryObstaclePin Read(JObject runtime,JObject encounter){
   bool issued=encounter?.Property("ordinaryObstacle")!=null,present=runtime.Property("sequence")!=null;
   Need(issued==present,"definition_invalid");if(!issued)return null;
   _=CombatProfile(encounter);var raw=runtime["sequence"] as JObject;
   BDirectAttackInput.Fields(raw,"version","obstacle","nextWave");
   Need((string)raw["version"]=="ordinary.obstacle-sequence.v1.20261106"&&JToken.DeepEquals(raw["obstacle"],encounter["ordinaryObstacle"]),"definition_invalid");
   var wave=raw["nextWave"] as JObject;BDirectAttackInput.Fields(wave,"kind","enemiesHash");
   Need((string)wave["kind"]=="ORIGINAL_NODE_ENEMIES"&&(string)wave["enemiesHash"]==BDirectAttackInput.Digest(runtime["enemies"]).Substring(7),"next_wave_unavailable");
   return new OrdinaryObstaclePin(raw);
  }
  internal static void Need(bool value,string reason){if(!value)throw new ArgumentException("ordinary_obstacle_"+reason);}
 }
 // Attempt-local consumer of the trusted execution owner's resolution, never a public upload endpoint.
 internal sealed class OrdinaryObstacleSequence {
  readonly OrdinaryObstaclePin pin;readonly OrdinaryNodeAttempt attempt;
  JObject resolution,receipt;
  internal string Phase=>receipt==null?"OBSTACLE":(string)receipt["phase"];
  internal OrdinaryObstacleSequence(OrdinaryObstaclePin pin,OrdinaryNodeAttempt attempt){this.pin=pin??throw new ArgumentNullException(nameof(pin));this.attempt=attempt??throw new ArgumentNullException(nameof(attempt));}
  JObject Scope()=>new JObject{["accountId"]=attempt.AccountId,["runId"]=attempt.RunId,["nodeId"]=attempt.NodeId,["attemptGeneration"]=attempt.Generation,["snapshotHash"]=attempt.SnapshotHash};
  internal JObject Capture(){if(receipt!=null)return (JObject)receipt.DeepClone();var state=Scope();state["version"]="ordinary.obstacle-transition.v1.20261106";state["phase"]="OBSTACLE";state["revision"]=1;state["nextWave"]=null;return state;}
  internal JObject CaptureLocal()=>new JObject{["transition"]=Capture(),["resolution"]=resolution?.DeepClone()};
  internal OrdinaryObstacleSequence CloneOwned()=>new OrdinaryObstacleSequence(pin,attempt){resolution=(JObject)resolution?.DeepClone(),receipt=(JObject)receipt?.DeepClone()};
  internal JObject Resolve(JObject value){
   BDirectAttackInput.Fields(value,"accountId","runId","nodeId","attemptGeneration","snapshotHash","eventId","atMs","outcome","obstacleHp","trainHp","tailBeyondZone","remainingHazards","unresolvedProjectiles","collisionCommitted");
   foreach(var field in Scope().Properties())Need(JToken.DeepEquals(value[field.Name],field.Value),"attempt_conflict");
   string id=(string)value["eventId"],outcome=(string)value["outcome"];
   Need(value["eventId"]?.Type==JTokenType.String&&!string.IsNullOrEmpty(id)&&id.Length<=200,"resolution_invalid");
   Need(outcome=="DESTROYED"||outcome=="PASSED"||outcome=="COLLISION"||outcome=="DEFEAT","resolution_invalid");
   long at=BDirectAttackInput.Nat(value,"atMs",0,OrdinaryNodeStart.SafeMaximum),hp=BDirectAttackInput.Nat(value,"obstacleHp",0,100),train=BDirectAttackInput.Nat(value,"trainHp",0,int.MaxValue);
   long hazards=BDirectAttackInput.Nat(value,"remainingHazards",0,OrdinaryNodeStart.SafeMaximum),shots=BDirectAttackInput.Nat(value,"unresolvedProjectiles",0,OrdinaryNodeStart.SafeMaximum);
   Need(value["tailBeyondZone"]?.Type==JTokenType.Boolean&&value["collisionCommitted"]?.Type==JTokenType.Boolean,"resolution_invalid");
   if(receipt!=null){Need(id==(string)resolution["eventId"],"already_resolved");Need(JToken.DeepEquals(value,resolution),"event_conflict");return Capture();}
   Need(outcome=="DEFEAT"?train==0:train>0,"survival_conflict");
   if(outcome!="DEFEAT"){
    Need((bool)value["tailBeyondZone"]&&hazards==0&&shots==0,"not_clear");
    Need(outcome=="DESTROYED"?hp==0:hp>0,"outcome_conflict");
    Need((bool)value["collisionCommitted"]==(outcome=="COLLISION"),"collision_conflict");
    Need(outcome!="PASSED"||at>=16000,"not_clear");
   }
   var next=Scope();next["version"]="ordinary.obstacle-transition.v1.20261106";next["eventId"]=id;next["atMs"]=at;next["phase"]=outcome=="DEFEAT"?"DEFEAT":"COMBAT";next["revision"]=2;next["nextWave"]=outcome=="DEFEAT"?null:pin.NextWave;
   resolution=(JObject)value.DeepClone();receipt=next;return Capture();
  }
  static void Need(bool value,string reason)=>OrdinaryObstaclePin.Need(value,reason);
 }
}
