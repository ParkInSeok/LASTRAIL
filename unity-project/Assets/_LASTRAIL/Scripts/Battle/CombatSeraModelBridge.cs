using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  CombatSeraInput seraInput;CombatSeraCommands seraCommands;
  // Existing Actor/Vitals/clock remain the only state owners. No fake train/wave/basic profile is constructed.
  BasicCombatSimulation(CombatSeraInput input,long step){
   if(step<=0||step%10000!=0)throw new ArgumentException("Explicit integer-ms logical step required.");
   seraInput=input;ConfigurationVersion=input.InputVersion+"/"+input.Hash+(input.OrdinaryAccount==null?"":"/"+step);LocalOwnerAccount=input.OrdinaryAccount;clock=new FixedStepDebt(step);random=new RunRandom(1);configuredRandomState=random.ExportState();approvedVitals=true;
   waves=Array.Empty<BasicCombatActor[]>();allies=input.Entities.Select(e=>new Actor(e.Id,e.MaxHp)).ToList();
   foreach(var entity in input.Entities)if(entity.InitialHp!=entity.MaxHp)allies.Single(a=>a.Identity==entity.Id).Vitals.ApplyTransferredHpLoss(entity.MaxHp-entity.InitialHp,0);
   var operators=allies.ToDictionary(a=>a.Identity,a=>a.Vitals,StringComparer.Ordinal);
   if(input.Train!=null)allies.Add(new Actor(input.Train.TrainId,input.Train.MaxHp));
   var targets=input.Train==null?null:new OrdinaryDamageTargets(input,allies.ToDictionary(a=>a.Identity,a=>a.Vitals,StringComparer.Ordinal),step/10000);
   seraCommands=new CombatSeraCommands(input,operators,targets);
  }
  // Local validation of the real producer packet and one native effect, never public admission or whole-run support.
  public static BasicCombatSimulation FromTrustedSeraHealInputForLocalValidation(JObject original,string trustedOriginalHash,string expectedAccount,long stepTimeSpanTicks)
   =>new BasicCombatSimulation(CombatSeraInput.Read(original,trustedOriginalHash,expectedAccount),stepTimeSpanTicks);
  internal static BasicCombatSimulation CreateOrdinarySera(CombatSeraInput input,long step)=>new BasicCombatSimulation(input,step);
  BasicCombatSimulation CloneSera()=>RestoreSeraOwner(JObject.Parse(CaptureSeraOwner()),()=>new BasicCombatSimulation(seraInput,clock.Capture().StepTicks));
  void AdoptSera(BasicCombatSimulation candidate){
   // Reject before publication so every accepted owner remains compatible with the existing restore budget.
   if(candidate.CaptureSeraOwner().Length>8*1024*1024)throw new InvalidOperationException("combat_sera_snapshot_limit");
   clock=candidate.clock;allies.Clear();allies.AddRange(candidate.allies);seraCommands=candidate.seraCommands;Outcome=seraCommands.Terminal?BasicCombatOutcome.Failure:BasicCombatOutcome.Running;
  }
  public JObject ScheduleResolvedSeraCommand(JObject request){
   if(seraCommands==null)throw new InvalidOperationException("Sera input owner not configured.");
   if(seraInput.OrdinaryAccount!=null&&(string)request?["command"]?["kind"]!="CAST"&&(string)request?["command"]?["kind"]!="CANCEL")throw new InvalidOperationException("Ordinary execution cannot use local HP observations.");
   var candidate=CloneSera();var receipt=candidate.seraCommands.Schedule(request);AdoptSera(candidate);return receipt;
  }
  public JArray ReadSeraEmissions()=>seraCommands?.Emissions??throw new InvalidOperationException("Sera input owner not configured.");
  // Only the trusted native effect producer calls this; UI commands retain their narrower entry point.
  public JObject ScheduleResolvedOrdinaryEffect(JObject request){
   if(seraInput?.Train==null||!new[]{"ORDINARY_DAMAGE","ORDINARY_SHIELD"}.Contains((string)request?["command"]?["kind"]))throw new ArgumentException("Original ordinary resolved effect required.");
   var candidate=CloneSera();var result=candidate.seraCommands.Schedule(request);AdoptSera(candidate);return result;
  }
  void AdvanceSera(long elapsed,int budget){var candidate=CloneSera();candidate.seraCommands.Advance(candidate.clock,elapsed,budget);AdoptSera(candidate);}
  void ControlSera(string kind,JToken value){var candidate=CloneSera();candidate.seraCommands.RecordControl(kind,value);if(kind=="PAUSE")candidate.clock.SetPaused((bool)value);else candidate.clock.SetSpeed((int)value);AdoptSera(candidate);}
  string CaptureSeraOwner(){
   var c=clock.Capture();var state=new JObject{["schema"]=seraInput.OrdinaryAccount==null?"combat.sera-unity-owner.v1":OrdinaryFiveSeraFactory.CheckpointVersion,["input"]=seraInput.Capture(),
    ["clock"]=new JObject{["step"]=c.StepTicks,["debt"]=c.DebtHalfTicks,["completed"]=c.CompletedSteps,["speed"]=c.SpeedHalfUnits,["paused"]=c.IsPaused},
    ["actors"]=new JArray(allies.Select(a=>new JObject{["id"]=a.Identity,["vitals"]=a.Vitals.CaptureLocal()})),["commands"]=seraCommands.Capture()};
   if(seraInput.OrdinaryAccount!=null)state["configurationSha256"]=Digest(ConfigurationVersion);return StableJson(state);
  }
  static BasicCombatSimulation RestoreSeraOwner(JObject saved,Func<BasicCombatSimulation> factory){
   var candidate=factory();if(candidate==null||candidate.seraCommands==null||candidate.Tick!=0)throw new ArgumentException("Original Sera factory required.");
   var ops=saved["commands"]?["operations"] as JArray;if(ops==null||ops.Count>10000)throw new ArgumentException("Invalid Sera operations.");
   foreach(var token in ops){
    var op=token as JObject;string kind=Text(op,"kind");
    if(kind=="SCHEDULE"){Fields(op,"kind","request");if(candidate.seraInput.OrdinaryAccount!=null&&!new[]{"CAST","CANCEL","ORDINARY_DAMAGE","ORDINARY_SHIELD"}.Contains((string)op["request"]?["command"]?["kind"]))throw new ArgumentException("Unexpected ordinary HP observation.");candidate.seraCommands.Schedule(op["request"] as JObject);}
    else if(kind=="ADVANCE"){Fields(op,"kind","elapsedTicks","maximumSteps");long elapsed=Nat(op,"elapsedTicks");int budget=checked((int)Nat(op,"maximumSteps",int.MaxValue));candidate.seraCommands.Advance(candidate.clock,elapsed,budget);}
    else if(kind=="PAUSE"||kind=="SPEED"){Fields(op,"kind","value");if(kind=="PAUSE"){if(op["value"]?.Type!=JTokenType.Boolean)throw new ArgumentException();candidate.clock.SetPaused((bool)op["value"]);}else {if(op["value"]?.Type!=JTokenType.Integer)throw new ArgumentException();candidate.clock.SetSpeed((int)op["value"]);}candidate.seraCommands.RecordControl(kind,op["value"]);}
    else throw new ArgumentException("Unknown Sera owner operation.");
   }
   if(StableJson(saved)!=candidate.CaptureSeraOwner())throw new ArgumentException("combat_sera_queue_state_invalid");
   candidate.Outcome=candidate.seraCommands.Terminal?BasicCombatOutcome.Failure:BasicCombatOutcome.Running;
   return candidate;
  }
  internal static IEnumerable<DueAction> OrderDueActions(IEnumerable<DueAction> actions)=>actions.OrderBy(a=>a.Due).ThenBy(a=>a.Sequence);
 }
}
