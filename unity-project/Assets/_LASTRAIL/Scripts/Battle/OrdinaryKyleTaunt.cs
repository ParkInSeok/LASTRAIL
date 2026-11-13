using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  JObject KyleAbility=>ordinaryFactory?.Original.Actors.FirstOrDefault(a=>a.OperatorId=="op_kyle")?.Ability("op_kyle.active");
  bool IndependentOrdinaryGuard=>!ordinaryNativeRetired&&ordinaryExecutionStarted&&ordinaryHealth!=null&&ordinaryLiveSources!=null
   &&(ordinaryBoarding!=null||ordinaryGunners!=null)&&(string)KyleAbility?["tauntPolicyVersion"]==KyleTauntPolicy.Version;
  IReadOnlyList<KyleTauntPolicy.Candidate> ObserveKyleTargets(string caster){
   ordinaryFactory.SceneMotion?.Invoke(Tick,ordinaryFront);
   var rows=new List<KyleTauntPolicy.Candidate>();
   if(ordinaryBoarding!=null){ordinaryBoarding.BindTauntTargets(OrdinaryTauntTarget);ordinaryBoarding.BeginStep(Tick);rows.AddRange(ordinaryBoarding.ObserveTaunt(caster));}
   if(ordinaryGunners!=null)foreach(var enemy in ordinaryFactory.NodeInput.Source.Enemies.Gunners){
    if(ordinaryHealth.Hp(enemy.InstanceId)<=0)continue;
    rows.Add(ordinaryLiveSources.ObserveTaunt(caster,enemy.InstanceId,(string)ordinaryGunners.ReadActor(enemy.InstanceId)["targetId"],Tick,rows.Count));
   }
   return rows;
  }
  IReadOnlyList<(string Id,long Generation)> KyleTargets(string caster,JObject ability){
   if(!IndependentOrdinaryGuard)return ordinaryFactory.Geometry.KyleTauntCandidates(caster,Tick);
   return KyleTauntPolicy.Select(ObserveKyleTargets(caster),(int)ability["maximumFrontTargets"],(long)ability["tauntRangeMm"]).Select(r=>(r.Id,r.Generation)).ToArray();
  }
  bool KyleTargetStillEligible(string caster,string target,long generation,JObject ability){
   if(!IndependentOrdinaryGuard)return ordinaryFactory.Geometry.KyleTauntStillEligible(caster,target,generation,Tick);
   var row=ObserveKyleTargets(caster).SingleOrDefault(r=>r.Id==target&&r.Generation==generation);
   return row!=null&&KyleTauntPolicy.Select(new[]{row},1,(long)ability["tauntRangeMm"]).Count==1;
  }
  string OrdinaryTauntTarget(string enemy,long generation){
   if(ordinaryNativeRetired||Outcome!=BasicCombatOutcome.Running)return null;
   return ordinaryTaunts.Where(t=>(string)t["target"]==enemy&&(long)t["generation"]==generation&&(long)t["expiry"]>Tick&&GetHp((string)t["source"])>0)
    .OrderByDescending(t=>(long)t["expiry"]).ThenBy(t=>(string)t["source"],StringComparer.Ordinal).Select(t=>(string)t["source"]).FirstOrDefault();
  }
 }
 internal sealed partial class OrdinaryBoardingExecution:IBBoardingTauntTargets {
  Func<string,long,string> tauntTarget;
  internal void BindTauntTargets(Func<string,long,string> source){tauntTarget=source;}
  public string ForcedTarget(string enemy)=>tauntTarget?.Invoke(enemy,boarding.Generation(enemy));
  internal IReadOnlyList<KyleTauntPolicy.Candidate> ObserveTaunt(string caster){
   var result=new List<KyleTauntPolicy.Candidate>();int order=0;
   foreach(string id in boarding.TargetableOrder()){
    if(Hp(id)<=0)continue;
    // Only these two original non-boss enemy definitions have a supported taunt contract.
    bool immune=roster.Entities[id].Definition!="enemy_raider_scout"&&roster.Entities[id].Definition!="enemy_spike_cart_gunner";
    result.Add(boarding.Spatial.ObserveTaunt(caster,id,boarding.Generation(id),order++,immune));
   }
   return result;
  }
 }
}
