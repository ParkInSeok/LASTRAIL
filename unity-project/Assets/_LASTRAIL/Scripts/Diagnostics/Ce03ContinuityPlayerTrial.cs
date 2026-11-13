#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Diagnostics {
 // Isolated replay of an existing accepted run: no HTTP, admission, rewards or
 // writes to the live run checkpoint. Continuous owner is the comparison oracle.
 public static class Ce03ContinuityPlayerTrial {
  static JObject Hit(BasicCombatHit h)=>new JObject{["tick"]=h.Tick,["source"]=h.Source,["target"]=h.Target,["hpLoss"]=h.HpLoss,["remainingHp"]=h.RemainingHp,["absorbed"]=h.Absorbed};
  public static void Run(CombatPlayerAssetBinding binding,string folder,string output,string sourceHash){
   var config=JObject.Parse(File.ReadAllText(Path.Combine(folder,"config.json")));
   string raw=File.ReadAllText((string)config["original"]);var value=JObject.Parse(raw);
   var original=new Ce03OriginalStart(raw,(string)value["owner"],new Uri((string)value["origin"]+"/"));
   if((string)JObject.Parse((string)value["responseRaw"])["combat"]["profile"]["baseProfile"]["executionEvidence"]["algorithmSourceSha256"]!=sourceHash)throw new InvalidOperationException("Replay source differs from original");
   var factory=original.CreateVerifiedPlayerFactory(binding);string phase=(string)config["phase"];
   if(phase!="continuous"&&phase!="restore")throw new ArgumentException("Explicit continuity phase required");
   var cases=new JArray();
   foreach(string name in new[]{"attack-wait","pending-attack","after-damage","after-kill","terminal"}){
    string snapshot=Path.Combine(folder,name+"-snapshot.json"),expected=Path.Combine(folder,name+"-expected.json");
    BasicCombatSimulation model;long step;int advanceCalls=0,eventsBefore;JToken firstHit=null;
    if(phase=="continuous"){
     model=factory();step=(long)JObject.Parse(model.CaptureLocalState())["clock"]["step"];
     bool found=false;for(int i=0;i<10000;i++){
      model.Advance(step,1);var events=model.Events;
      if(name=="terminal")found=model.Outcome!=BasicCombatOutcome.Running;
      else if(name=="after-damage")found=events.Any(e=>e.Tick==model.Tick&&e.HpLoss>0);
      else if(name=="after-kill")found=events.Any(e=>e.Tick==model.Tick&&e.HpLoss>0&&e.RemainingHp==0);
      else if(model.Tick<300){var state=JObject.Parse(model.CaptureLocalState());
       if(name=="pending-attack")found=((JArray)state["attacks"]).Count>0;
       else if(events.Count>0){string actor=events[0].Source;found=((JArray)state["actors"]).Any(a=>(string)a["id"]==actor&&(long)a["ready"]>model.Tick)&&!((JArray)state["attacks"]).Any(a=>(string)a["source"]==actor);}
      }
      if(found)break;if(model.Outcome!=BasicCombatOutcome.Running)break;
     }
     if(!found)throw new InvalidOperationException("Required supported boundary absent: "+name);
     Ce03AcceptedPlayerTrial.Atomic(snapshot,model.CaptureLocalState());
     // Continue the uninterrupted owner until a real next hit is observed.
     eventsBefore=model.Events.Count;
     if(name=="terminal"){for(int n=0;n<30;n++){model.Advance(step,1);advanceCalls++;}if(model.Events.Count!=eventsBefore)throw new InvalidOperationException("Terminal emitted another hit");}
     else{while(model.Events.Count==eventsBefore&&advanceCalls<500){model.Advance(step,1);advanceCalls++;}if(model.Events.Count==eventsBefore)throw new InvalidOperationException("Next hit not observed: "+name);firstHit=Hit(model.Events[eventsBefore]);}
     Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,name+"-continuation.json"),Ce03CommandContract.Canonical(new JObject{["advanceCalls"]=advanceCalls,["eventsBefore"]=eventsBefore,["firstHit"]=firstHit}));
     Ce03AcceptedPlayerTrial.Atomic(expected,model.CaptureLocalState());
    }else{
     var prior=JObject.Parse(File.ReadAllText(Path.Combine(folder,"continuous.json")));
     if((int)prior["processId"]==System.Diagnostics.Process.GetCurrentProcess().Id)throw new InvalidOperationException("New Player process required");
     string state=File.ReadAllText(snapshot);model=BasicCombatSimulation.RestoreLocalState(state,factory);
     if(model.CaptureLocalState()!=state)throw new InvalidOperationException("Restored boundary differs: "+name);
     step=(long)JObject.Parse(state)["clock"]["step"];
     var continuation=JObject.Parse(File.ReadAllText(Path.Combine(folder,name+"-continuation.json")));eventsBefore=model.Events.Count;
     if(eventsBefore!=(int)continuation["eventsBefore"])throw new InvalidOperationException("Prior events differ");
     advanceCalls=(int)continuation["advanceCalls"];for(int n=0;n<advanceCalls;n++)model.Advance(step,1);
     if(name=="terminal"){if(model.Events.Count!=eventsBefore)throw new InvalidOperationException("Restored terminal emitted hit");}
     else{if(model.Events.Count<=eventsBefore)throw new InvalidOperationException("Restored next hit missing");firstHit=Hit(model.Events[eventsBefore]);if(!JToken.DeepEquals(firstHit,continuation["firstHit"]))throw new InvalidOperationException("First hit differs");}
     if(model.CaptureLocalState()!=File.ReadAllText(expected))throw new InvalidOperationException("Restored first-hit continuation differs: "+name);
    }
    cases.Add(new JObject{["boundary"]=name,["advanceCalls"]=advanceCalls,["firstHit"]=firstHit,["firstHitVerified"]=name!="terminal"&&phase=="restore",["tickAfter"]=model.Tick,["eventsAfter"]=model.Events.Count,["stateHash"]=Ce03CommandContract.Sha(model.CaptureLocalState()),["fullStateEqual"]=phase=="restore"});
   }
   var result=new JObject{["phase"]=phase,["processId"]=System.Diagnostics.Process.GetCurrentProcess().Id,["runId"]=original.RunId,["key"]=original.Key,["cases"]=cases,["scope"]="ISOLATED_ACCEPTED_MODEL_REPLAY_IN_REAL_PLAYER_NO_NEW_ADMISSION",["unsupported"]="Spatial projectile renderer, unsupported lifesteal/ability effects and visible feedback are not claimed by legacy pending-attack state equality"};
   Ce03AcceptedPlayerTrial.Atomic(Path.Combine(folder,phase+".json"),Ce03CommandContract.Canonical(result));
   Ce03AcceptedPlayerTrial.Atomic(Path.Combine(output,"result.json"),Ce03CommandContract.Canonical(result));
  }
 }
}
#endif
