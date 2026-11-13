using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public enum FirstPlayableArtilleryPhase { Preview, Cancelled, Launched, Resolved }
 public readonly struct FirstPlayableArtilleryImpact {
  readonly JObject result;
  public bool Applied{get;}
  public bool TrainDefeatedNow=>Applied&&(bool)result["trainDefeatedNow"];
  public JObject Result=>(JObject)result.DeepClone();
  internal FirstPlayableArtilleryImpact(JObject value,bool applied){result=(JObject)value.DeepClone();Applied=applied;}
 }
 // Host must use the existing ordered combat executor, never UI animation callbacks.
 // Borrows both HP owners. The host checkpoints this aggregate with its clock and other actors.
 public sealed class FirstPlayableArtillery {
  const string Version="unity.first-playable-artillery.v1.20261027";
  readonly CombatVitals train,launcher;
  readonly BCombatLocalModel nodeHealth;
  readonly string nodeTrainId,nodeImpactId;
  readonly JObject definition;
  readonly string configurationHash;
  readonly long stepMs,previewTick;
  RailArtilleryHazard hazard;
  JObject impact;
  long? cancelledAt;
  public FirstPlayableArtilleryPhase Phase{get;private set;}
  public long LaunchTick{get;}
  public long ImpactTick{get;}
  public bool WarningVisible=>Phase==FirstPlayableArtilleryPhase.Preview||Phase==FirstPlayableArtilleryPhase.Launched;
  public bool ThreatResolved=>Phase==FirstPlayableArtilleryPhase.Cancelled||Phase==FirstPlayableArtilleryPhase.Resolved;
  public bool TrainDefeated=>train.Hp==0;
  // Trusted host supplies the two original gunner owners. This is not node/run completion authority.
  public bool IsEncounterObjectiveSatisfied(CombatVitals firstGunner,CombatVitals secondGunner){
   Need(firstGunner!=null&&secondGunner!=null&&!ReferenceEquals(firstGunner,secondGunner)
    &&!ReferenceEquals(firstGunner,train)&&!ReferenceEquals(secondGunner,train)
    &&!ReferenceEquals(firstGunner,launcher)&&!ReferenceEquals(secondGunner,launcher));
   return firstGunner.Hp==0&&secondGunner.Hp==0&&ThreatResolved&&!TrainDefeated;
  }
  public FirstPlayableArtillery(FirstPlayableScaling scaling,OrdinaryFiveStartInput start,string initializationResponse,JObject issuedHazard,string trainId,CombatVitals trainOwner,CombatVitals launcherOwner,long logicalStepMs){
   if(scaling==null||trainOwner==null||launcherOwner==null||ReferenceEquals(trainOwner,launcherOwner))throw new ArgumentException("Distinct original HP owners required.");
   scaling.RequireOriginal(start,initializationResponse);
   Need(start.Train!=null&&start.Train.TrainId==trainId&&JObject.Parse(initializationResponse)["initializationReceipt"]?["ordinaryTrainBinding"] is JObject);
   var profile=start.FirstBattle.Definition;var artillery=profile["artillery"];
   Need((string)artillery["hazardVersion"]==RailArtilleryHazard.ExactVersion&&logicalStepMs>0);
   _=new RailArtilleryHazard(issuedHazard);
   Need((string)issuedHazard["version"]==RailArtilleryHazard.ExactVersion&&(string)issuedHazard["runId"]==start.RunId&&(string)issuedHazard["trainId"]==trainId);
   if(scaling.IssuedHazardId!=null)Need((string)issuedHazard["eventId"]==scaling.IssuedHazardId);
   Need(JToken.DeepEquals(issuedHazard["damage"],scaling.ArtilleryRawDamage.Snapshot())&&scaling.LauncherHp.Denominator==1&&scaling.LauncherHp.Numerator==launcherOwner.MaxHp&&launcherOwner.Hp>0);
   long preview=(long)issuedHazard["previewAtMs"],launch=checked(preview+(long)artillery["launchAfterPreviewMs"]),at=(long)issuedHazard["timing"]["impactAtMs"];
   Need(at==checked(preview+(long)artillery["impactAfterPreviewMs"])&&(long)issuedHazard["endMm"]-(long)issuedHazard["startMm"]==(long)artillery["dangerLengthMm"]);
   Need(preview%logicalStepMs==0&&launch%logicalStepMs==0&&at%logicalStepMs==0);
   definition=(JObject)issuedHazard.DeepClone();train=trainOwner;launcher=launcherOwner;stepMs=logicalStepMs;previewTick=preview/stepMs;LaunchTick=launch/stepMs;ImpactTick=at/stepMs;
   configurationHash=BDirectAttackInput.Digest(new JObject{["version"]=Version,["bindingHash"]=scaling.BindingHash,["hazard"]=definition,["stepMs"]=stepMs,["trainMaxHp"]=train.MaxHp,["launcherMaxHp"]=launcher.MaxHp});
  }
  // Node source validation belongs to OrdinaryNodeArtillery. This shares the timing/phase
  // machine while the existing kernel alone owns train mutations and their receipts.
  internal FirstPlayableArtillery(JObject artillery,JObject issuedHazard,long logicalStepMs,string identity,
   BCombatLocalModel health,string trainId,CombatVitals launcherOwner){
   Need(health!=null&&launcherOwner!=null&&logicalStepMs>0);
   _=new RailArtilleryHazard(issuedHazard);
   Need((string)artillery["hazardVersion"]==RailArtilleryHazard.ExactVersion&&(string)issuedHazard["version"]==RailArtilleryHazard.ExactVersion);
   long preview=(long)issuedHazard["previewAtMs"],launch=checked(preview+(long)artillery["launchAfterPreviewMs"]),at=(long)issuedHazard["timing"]["impactAtMs"];
   Need(at==checked(preview+(long)artillery["impactAfterPreviewMs"])&&(long)issuedHazard["endMm"]-(long)issuedHazard["startMm"]==(long)artillery["dangerLengthMm"]);
   Need(preview%logicalStepMs==0&&launch%logicalStepMs==0&&at%logicalStepMs==0);
   definition=(JObject)issuedHazard.DeepClone();stepMs=logicalStepMs;previewTick=preview/stepMs;LaunchTick=launch/stepMs;ImpactTick=at/stepMs;
   nodeHealth=health;nodeTrainId=trainId;train=health.BorrowVitals(trainId);launcher=launcherOwner;
   configurationHash=identity;nodeImpactId="node-artillery:"+BDirectAttackInput.Digest(new JObject{["configuration"]=identity,["impactIndex"]=0}).Substring(7);
  }
  internal FirstPlayableArtillery CloneOwnedNode(BCombatLocalModel health,CombatVitals launcherOwner){
   Need(nodeHealth!=null);
   return new FirstPlayableArtillery(this,health,launcherOwner);
  }
  FirstPlayableArtillery(FirstPlayableArtillery source,BCombatLocalModel health,CombatVitals launcherOwner){
   definition=(JObject)source.definition.DeepClone();configurationHash=source.configurationHash;stepMs=source.stepMs;previewTick=source.previewTick;LaunchTick=source.LaunchTick;ImpactTick=source.ImpactTick;
   nodeHealth=health;nodeTrainId=source.nodeTrainId;nodeImpactId=source.nodeImpactId;train=health.BorrowVitals(nodeTrainId);launcher=launcherOwner;
   Phase=source.Phase;cancelledAt=source.cancelledAt;impact=source.impact==null?null:(JObject)source.impact.DeepClone();hazard=source.hazard==null?null:RailArtilleryHazard.Restore(source.hazard.Snapshot(),definition);
  }
  public void ObserveLauncherDefeat(long tick){
   Need(tick>=previewTick&&launcher.Hp==0);
   if(Phase!=FirstPlayableArtilleryPhase.Preview)return;
   Need(tick<=LaunchTick);cancelledAt=tick;Phase=FirstPlayableArtilleryPhase.Cancelled;
  }
  public void CommitLaunch(long tick){
   Need(tick==LaunchTick);
   if(Phase!=FirstPlayableArtilleryPhase.Preview)return;
   if(launcher.Hp==0){ObserveLauncherDefeat(tick);return;}
   hazard=new RailArtilleryHazard(definition);Phase=FirstPlayableArtilleryPhase.Launched;
  }
  public FirstPlayableArtilleryImpact ResolveImpact(JObject frame,JObject numeric,long tick,bool protectedFromDamage){
   Need(tick==ImpactTick&&(long?)frame?["atMs"]==checked(tick*stepMs));
   var request=new JObject{["frame"]=frame?.DeepClone(),["numeric"]=numeric?.DeepClone(),["tick"]=tick,["protected"]=protectedFromDamage};
   if(Phase==FirstPlayableArtilleryPhase.Resolved){Need(JToken.DeepEquals(impact["request"],request));return new FirstPlayableArtilleryImpact((JObject)impact["result"],false);}
   Need(Phase==FirstPlayableArtilleryPhase.Launched);
   var candidate=RailArtilleryHazard.Restore(hazard.Snapshot(),definition);var collision=candidate.Resolve(frame);
   Need(numeric!=null&&JToken.DeepEquals(numeric["base"],definition["damage"]));
   var calculation=CombatNumericDefenseBudget.Resolve(numeric);
   if(nodeHealth!=null){
    // All collision/arithmetic validation precedes the kernel command. The enclosing
    // native executor stages this owner together with that kernel before publication.
    int magnitude=calculation.ToVitalsMagnitude();bool wasAlive=!nodeHealth.TrainDefeated;
    BCombatSingleTargetReceipt receipt=null;
    if((string)collision["outcome"]=="HIT")receipt=nodeHealth.ApplyResolvedDamage(nodeImpactId,nodeTrainId,magnitude,tick,protectedFromDamage);
    var nodeResult=new JObject{["collision"]=collision,["calculation"]=calculation.Snapshot(),["hpLoss"]=receipt?.HpLoss??0,["absorbed"]=receipt?.Absorbed??0,["hpAfter"]=receipt?.HpAfter??nodeHealth.Hp(nodeTrainId),["trainDefeatedNow"]=wasAlive&&nodeHealth.TrainDefeated};
    impact=new JObject{["request"]=request,["result"]=nodeResult};hazard=candidate;Phase=FirstPlayableArtilleryPhase.Resolved;
    return new FirstPlayableArtilleryImpact(nodeResult,true);
   }
   var nextTrain=train.Copy();var damage=new CombatDamageResult(0,0);bool alive=nextTrain.Hp>0;
   if((string)collision["outcome"]=="HIT")damage=nextTrain.Damage(calculation.ToVitalsMagnitude(),tick,protectedFromDamage);
   var result=new JObject{["collision"]=collision,["calculation"]=calculation.Snapshot(),["hpLoss"]=damage.HpLoss,["absorbed"]=damage.Absorbed,["hpAfter"]=nextTrain.Hp,["trainDefeatedNow"]=alive&&nextTrain.Hp==0};
   var nextImpact=new JObject{["request"]=request,["result"]=result};var trainState=nextTrain.CaptureLocal();
   // No callbacks or IO between the two publications; all validation used transient candidates.
   train.RestoreLocal(trainState,tick);hazard=candidate;impact=nextImpact;Phase=FirstPlayableArtilleryPhase.Resolved;
   return new FirstPlayableArtilleryImpact(result,true);
  }
  public JObject Capture(){
   Need(Phase!=FirstPlayableArtilleryPhase.Preview||launcher.Hp>0);
   if(nodeHealth!=null)return new JObject{["version"]="unity.node-artillery-phase.v1.20261102",["configurationHash"]=configurationHash,["phase"]=Phase.ToString(),["cancelledAt"]=cancelledAt.HasValue?new JValue(cancelledAt.Value):JValue.CreateNull(),["hazard"]=hazard?.Snapshot()??(JToken)JValue.CreateNull(),["impact"]=impact?.DeepClone()??JValue.CreateNull()};
   return new JObject{["version"]=Version,["configurationHash"]=configurationHash,["phase"]=Phase.ToString(),["cancelledAt"]=cancelledAt.HasValue?new JValue(cancelledAt.Value):JValue.CreateNull(),["hazard"]=hazard?.Snapshot()??(JToken)JValue.CreateNull(),["impact"]=impact?.DeepClone()??JValue.CreateNull(),["train"]=train.CaptureLocal(),["launcher"]=launcher.CaptureLocal()};
  }
  public void Restore(JObject saved,string trustedCheckpointHash,long logicalTick){
   if(nodeHealth!=null)throw new InvalidOperationException("Node artillery is restart-only; external restore is unsupported.");
   BDirectAttackInput.Fields(saved,"version","configurationHash","phase","cancelledAt","hazard","impact","train","launcher");
   Need(BDirectAttackInput.Digest(saved)==trustedCheckpointHash&&(string)saved["version"]==Version&&(string)saved["configurationHash"]==configurationHash&&logicalTick>=previewTick);
   Need(Enum.TryParse((string)saved["phase"],out FirstPlayableArtilleryPhase phase)&&Enum.IsDefined(typeof(FirstPlayableArtilleryPhase),phase)&&(string)saved["phase"]==phase.ToString());
   var nextTrain=train.Copy();nextTrain.RestoreLocal(saved["train"] as JObject,logicalTick);var nextLauncher=launcher.Copy();nextLauncher.RestoreLocal(saved["launcher"] as JObject,logicalTick);
   long? cancellation=saved["cancelledAt"].Type==JTokenType.Null?(long?)null:BDirectAttackInput.Nat(saved,"cancelledAt");
   Need((phase==FirstPlayableArtilleryPhase.Cancelled)==cancellation.HasValue);
   if(cancellation.HasValue)Need(nextLauncher.Hp==0&&cancellation.Value>=previewTick&&cancellation.Value<=LaunchTick&&cancellation.Value<=logicalTick);
   bool launched=phase==FirstPlayableArtilleryPhase.Launched||phase==FirstPlayableArtilleryPhase.Resolved;
   Need(launched==(saved["hazard"] is JObject));var nextHazard=launched?RailArtilleryHazard.Restore((JObject)saved["hazard"],definition):null;
   var nextImpact=saved["impact"] as JObject;Need((phase==FirstPlayableArtilleryPhase.Resolved)==(nextImpact!=null));Need(nextImpact!=null||saved["impact"].Type==JTokenType.Null);
   if(!launched)Need(saved["hazard"].Type==JTokenType.Null&&saved["impact"].Type==JTokenType.Null);
   if(phase==FirstPlayableArtilleryPhase.Preview)Need(nextLauncher.Hp>0&&logicalTick<=LaunchTick);
   if(launched)Need(logicalTick>=LaunchTick&&(phase==FirstPlayableArtilleryPhase.Resolved)==!nextHazard.NextImpactAtMs.HasValue);
   if(phase==FirstPlayableArtilleryPhase.Launched)Need(logicalTick<=ImpactTick);
   if(nextImpact!=null){
    BDirectAttackInput.Fields(nextImpact,"request","result");var request=nextImpact["request"] as JObject;BDirectAttackInput.Fields(request,"frame","numeric","tick","protected");
    Need(logicalTick>=ImpactTick&&(long?)request["tick"]==ImpactTick&&request["protected"]?.Type==JTokenType.Boolean);
    var result=nextImpact["result"] as JObject;BDirectAttackInput.Fields(result,"collision","calculation","hpLoss","absorbed","hpAfter","trainDefeatedNow");
    Need(JToken.DeepEquals(nextHazard.Resolve(request["frame"] as JObject),result["collision"])&&JToken.DeepEquals(request["numeric"]?["base"],definition["damage"]));
    Need(JToken.DeepEquals(CombatNumericDefenseBudget.Resolve(request["numeric"] as JObject).Snapshot(),result["calculation"]));
    BDirectAttackInput.Nat(result,"hpLoss",0,int.MaxValue);BDirectAttackInput.Nat(result,"absorbed",0,int.MaxValue);BDirectAttackInput.Nat(result,"hpAfter",0,int.MaxValue);Need(result["trainDefeatedNow"]?.Type==JTokenType.Boolean);
   }
   train.RestoreLocal(nextTrain.CaptureLocal(),logicalTick);launcher.RestoreLocal(nextLauncher.CaptureLocal(),logicalTick);
   Phase=phase;cancelledAt=cancellation;hazard=nextHazard;impact=nextImpact==null?null:(JObject)nextImpact.DeepClone();
  }
  static void Need(bool value){if(!value)throw new ArgumentException("first_playable_artillery_conflict");}
 }
}
