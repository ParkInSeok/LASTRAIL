using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
public sealed partial class BasicCombatSimulation {
 CombatRailJourney railJourney;
 bool railWavePending;
 public void ConfigureRailJourney(JObject configuration){
  if(Tick!=0||railJourney!=null||commands.Count!=0)throw new InvalidOperationException("Rail config belongs to original factory only");
  var candidate=new CombatRailJourney(configuration);if(((JArray)configuration["offers"]).Any(x=>(int)x["afterCompletedWaves"]>=waves.Length))throw new ArgumentException("No offer after final wave");
  railJourney=candidate;ConfigureArtillery(configuration["artillery"] as JObject);
 }
 public void PreviewRailRoute(int index){RequireRailInput();railJourney.Preview(index);}
 public bool ConfirmRailRoute(string offerId,string optionId){RequireRailInput();return railJourney.Confirm(offerId,optionId,Tick);}
 public void SetRailThrottle(int bps){if(ordinaryFactory!=null){if((!OrdinaryReady&&!IsExecutionReady)||clock.IsPaused)throw new InvalidOperationException("Ordinary rail preparation incomplete.");OrdinaryControl("THROTTLE",new JValue(bps));return;}RequireRailInput();railJourney.SetThrottle(bps);}
 void RequireRailInput(){if(railJourney==null||Outcome!=BasicCombatOutcome.Running||clock.IsPaused)throw new InvalidOperationException("Rail input unavailable");}
 int RailBonus(string stat)=>railJourney?.Bonus(stat)??0;
 void AdvanceRail(long elapsed,int maximumSteps){
  if(clock.IsPaused||maximumSteps==0)return;
  long step=clock.Capture().StepTicks;
  // Bounded foreground credit. Never bank a long suspension as combat catch-up debt.
  long remaining=Math.Min(elapsed,checked(step*maximumSteps/2));int used=0;
  while(remaining>0&&Outcome==BasicCombatOutcome.Running){
   long slice=Math.Min(10000,remaining);remaining-=slice;
   clock.Credit(railJourney.TransformElapsed(slice,false));
   while(used<maximumSteps&&clock.TryConsumeStep()){
    used++;railJourney.AdvanceDistance(step);Step();railJourney.CheckBoundary(CompletedWaves,Tick);
    if(Outcome!=BasicCombatOutcome.Running)break;
   }
  }
 }
 JObject CaptureRail(){if(railJourney==null)return null;var value=railJourney.Capture();value["waitingForWave"]=railWavePending;value["terminalPending"]=railTerminalPending;if(railArtillery!=null)value["artillery"]=railArtillery.Capture();return value;}
 void RestoreRail(JObject state){
  if((railJourney==null)!=(state==null))throw new ArgumentException("Rail profile mismatch");if(state==null)return;
  railJourney.Restore(state);if((long)state["currentTick"]!=Tick||(int)state["completedWaves"]!=CompletedWaves||state["waitingForWave"]?.Type!=JTokenType.Boolean)throw new ArgumentException("Rail/clock mismatch");
  railWavePending=(bool)state["waitingForWave"];
  if(railWavePending&&(CompletedWaves==0||CompletedWaves>=waves.Length||enemies.Any(x=>!IsRailArtillery(x)&&x.Hp>0)))throw new ArgumentException("Rail wave hold mismatch");
  if(railJourney.Selecting&&!railWavePending)throw new ArgumentException("Selecting without junction hold");
  if(railArtillery!=null){if(state["artillery"] is not JObject artillery||(long)artillery["tick"]!=Tick||(long)artillery["distanceMm"]!=(long)state["distanceMm"]||(int)artillery["wave"]!=(Tick==0?0:Math.Min(waves.Length,CompletedWaves+1))||((bool)artillery["terminalRequested"]!=railTerminalPending&&!(railTerminalPending&&events.Any(hit=>hit.Tick==Tick&&hit.RemainingHp==0&&hit.HpLoss>0&&railArtilleryActors.ContainsKey(hit.Source))))||railTerminalPending!=(CompletedWaves==waves.Length||allies.Single(a=>a.Config.Train).Hp==0))throw new ArgumentException("Artillery clock/terminal mismatch");foreach(var row in (JArray)artillery["encounters"])if((string)row["status"]=="Active"&&(int)row["hp"]!=railArtilleryActors[(string)row["id"]].Hp)throw new ArgumentException("Artillery HP mismatch");}
 }
}
}
