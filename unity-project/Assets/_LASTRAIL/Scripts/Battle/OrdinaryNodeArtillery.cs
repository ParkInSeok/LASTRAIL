using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Owns only the distinct ground launcher's HP and the issued single-shot phase.
 // The host stages this with its eight-target HP kernel and publishes them together.
 internal sealed class OrdinaryNodeArtillery {
  readonly OrdinaryFiveCombatFactory factory;
  readonly BCombatLocalModel health;
  readonly JObject definition;
  readonly JArray launcherEvents=new JArray();
  CombatVitals launcher;
  FirstPlayableArtillery artillery;
  public FirstPlayableArtilleryPhase Phase=>artillery.Phase;
  public long LaunchTick=>artillery.LaunchTick;
  public long ImpactTick=>artillery.ImpactTick;
  public bool ThreatResolved=>artillery.ThreatResolved;
  public bool WarningVisible=>artillery.WarningVisible;
  public int LauncherHp=>launcher.Hp;
  public OrdinaryNodeArtillery(OrdinaryFiveCombatFactory factory,JObject issuedHazard,BCombatLocalModel health){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.health=health??throw new ArgumentNullException(nameof(health));
   var node=factory.NodeInput;Need(node!=null&&factory.Attempt!=null);RequireKernel(health);
   var source=node.Source.Enemies.Artillery;
   _=new RailArtilleryHazard(issuedHazard);
   Need((string)issuedHazard["runId"]==factory.Original.RunId&&(string)issuedHazard["trainId"]==node.TrainId&&(string)issuedHazard["eventId"]==source.HazardId&&JToken.DeepEquals(issuedHazard["damage"],source.RawDamage.Snapshot()));
   Need(source.Hp.Denominator==1&&source.MaxHp.Denominator==1&&source.Hp.Numerator>0&&source.Hp.Numerator<=source.MaxHp.Numerator);
   launcher=new CombatVitals(checked((int)source.MaxHp.Numerator),0,true);
   if(source.Hp.Numerator!=source.MaxHp.Numerator){var initial=launcher.CaptureLocal();initial["hp"]=checked((int)source.Hp.Numerator);initial["anchorHp"]=initial["hp"].DeepClone();launcher.RestoreLocal(initial,0);}
   definition=new JObject{["version"]="unity.ordinary-node-artillery.v1.20261102",["factory"]=factory.ConfigurationVersion,["hazard"]=issuedHazard.DeepClone(),["launcherHp"]=source.Hp.Snapshot(),["launcherMaxHp"]=source.MaxHp.Snapshot()};
   artillery=new FirstPlayableArtillery(source.CaptureDefinition(),issuedHazard,factory.Step/10000,BDirectAttackInput.Digest(definition),health,node.TrainId,launcher);
  }
  void RequireKernel(BCombatLocalModel candidate){
   Need(candidate!=null);var state=candidate.CaptureIdentity();var d=state["definition"];var attempt=factory.Attempt;
   Need((string)state["schema"]=="unity.combat-b.node-health-local.v1.20261102"&&d!=null&&(string)d["accountId"]==attempt.AccountId&&(string)d["runId"]==attempt.RunId&&(string)d["nodeId"]==attempt.NodeId&&(string)d["snapshotHash"]==attempt.SnapshotHash&&(long?)d["generation"]==attempt.Generation&&(long?)d["attemptRevision"]==attempt.Revision);
  }
  OrdinaryNodeArtillery(OrdinaryNodeArtillery source,BCombatLocalModel newHealth){
   factory=source.factory;health=newHealth;RequireKernel(newHealth);
   Need(!ReferenceEquals(source.health,newHealth)&&source.health.HasSameState(newHealth));
   definition=(JObject)source.definition.DeepClone();launcher=source.launcher.Copy();artillery=source.artillery.CloneOwnedNode(newHealth,launcher);
   foreach(var row in source.launcherEvents)launcherEvents.Add(row.DeepClone());
  }
  public OrdinaryNodeArtillery CloneOwned(BCombatLocalModel newHealth)=>new OrdinaryNodeArtillery(this,newHealth);
  public void CommitLaunch(long tick)=>artillery.CommitLaunch(tick);
  public FirstPlayableArtilleryImpact ResolveImpact(JObject frame,JObject numeric,long tick,bool protectedFromDamage)=>artillery.ResolveImpact(frame,numeric,tick,protectedFromDamage);
  // Only the original train attack executor may call this after resolving its damage.
  // No operator/drone target registration is exposed by this ground hazard owner.
  public void ApplyResolvedTrainDamageToLauncher(string eventId,int magnitude,long tick){
   BCombatFormation.Identity(eventId);Need(magnitude>=0&&tick>=0&&tick<=9007199254740991);
   var command=new JObject{["eventId"]=eventId,["magnitude"]=magnitude,["tick"]=tick};
   var old=launcherEvents.FirstOrDefault(e=>(string)e["command"]["eventId"]==eventId);
   if(old!=null){Need(JToken.DeepEquals(old["command"],command));return;}
   Need(launcherEvents.Count<10000);
   var next=launcher.Copy();var loss=health.TrainDefeated?new CombatDamageResult(0,0):next.Damage(magnitude,tick,false);
   // Validate the same-tick cancellation before publishing the private launcher HP.
   var nextPhase=artillery.CloneOwnedNode(health,next);
   if(next.Hp==0)nextPhase.ObserveLauncherDefeat(tick);
   var receipt=new JObject{["command"]=command,["hpLoss"]=loss.HpLoss,["hpAfter"]=next.Hp};
   launcher=next;artillery=nextPhase;launcherEvents.Add(receipt);
  }
  public JObject Capture()=>new JObject{["definition"]=definition.DeepClone(),["phase"]=artillery.Capture(),["launcher"]=launcher.CaptureLocal(),["launcherEvents"]=launcherEvents.DeepClone()};
  static void Need(bool value){if(!value)throw new ArgumentException("ordinary_node_artillery_conflict");}
 }
}
