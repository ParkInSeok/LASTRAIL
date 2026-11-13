using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
public sealed partial class BasicCombatSimulation {
 CombatRailArtillery railArtillery;bool railTerminalPending;
 internal bool CollectRailStepInputs;
 internal readonly JArray RailStepInputs=new JArray();
 readonly Dictionary<string,Actor> railArtilleryActors=new Dictionary<string,Actor>(StringComparer.Ordinal);
 bool IsRailArtillery(Actor actor)=>railArtilleryActors.ContainsKey(actor.Config.Id);
 bool CanDirectlyTarget(Actor source,Actor target)=>!IsRailArtillery(target)||source.Config.Train||source.Config.Timing?.Device=="Gun";
 void ConfigureArtillery(JObject config){if(config==null)return;railArtillery=new CombatRailArtillery(config);var definition=config["definition"];int index=0;foreach(var row in (JArray)config["encounters"]){string id=(string)row["id"];if(allies.Select(a=>a.Config).Concat(waves.SelectMany(w=>w)).Any(a=>a.Id==id))throw new ArgumentException("Duplicate artillery actor");var actor=new Actor(new BasicCombatActor(id,true,false,true,32+index++,(int)definition["maxHp"],(int)definition["trainDamage"],0,int.MaxValue,0,0,BasicCombatTarget.Train,0),0,approvedVitals);actor.Ready=long.MaxValue;railArtilleryActors.Add(id,actor);actorVisualIds[id]="rail_artillery_emplacement_v1";}}
 void AdvanceArtillery(){if(railArtillery==null)return;
  JObject stepInput=null;if(CollectRailStepInputs){var damage=new JObject();foreach(var row in (JArray)railArtillery.Capture()["encounters"]){string id=(string)row["id"];damage[id]=(string)row["status"]=="Active"?(int)row["hp"]-railArtilleryActors[id].Hp:0;}stepInput=new JObject{["tick"]=Tick,["wave"]=Math.Min(waves.Length,CompletedWaves+1),["distanceMm"]=railJourney.Capture()["distanceMm"],["terminalRequested"]=railTerminalPending||allies.Single(a=>a.Config.Train).Hp==0,["damageTakenById"]=damage};}
  foreach(var row in (JArray)railArtillery.Capture()["encounters"])if((string)row["status"]=="Active")railArtillery.SynchronizeHealth((string)row["id"],railArtilleryActors[(string)row["id"]].Hp);
  var train=allies.Single(a=>a.Config.Train);var impacts=railArtillery.Advance(Tick,Math.Min(waves.Length,CompletedWaves+1),(long)railJourney.Capture()["distanceMm"],railTerminalPending||train.Hp==0);
  if(stepInput!=null)RailStepInputs.Add(stepInput);
  foreach(var row in (JArray)railArtillery.Capture()["encounters"]){var actor=railArtilleryActors[(string)row["id"]];if((string)row["status"]=="Active"){if(!enemies.Contains(actor))enemies.Add(actor);}else enemies.Remove(actor);}
  foreach(var impact in impacts){var hit=train.Vitals.Damage((int)impact["damage"],Tick,false);events.Add(new BasicCombatHit(Tick,(string)impact["source"],train.Config.Id,hit.HpLoss,train.Hp,hit.Absorbed));}
  if(train.Hp==0)railTerminalPending=true;
  if(railTerminalPending&&!railArtillery.HasProjectiles)Outcome=train.Hp==0?BasicCombatOutcome.Failure:BasicCombatOutcome.Victory;
 }
}
}
