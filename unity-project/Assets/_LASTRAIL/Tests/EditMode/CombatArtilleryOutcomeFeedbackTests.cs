using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatArtilleryOutcomeFeedbackTests {
 static JObject Hud(CombatRailArtillery owner,long tick)=>new JObject{["outcome"]="Running",["tick"]=tick,["railArtillery"]=owner.Capture()};
 [Test] public void ActualDestructionShowsOnceWithoutMutatingAndRestoreDoesNotReplay(){
  var owner=new CombatRailArtillery(CombatRailArtilleryTests.Config());owner.Advance(1,2,0,false);var display=new CombatArtilleryOutcomeFeedback();display.Seed(Hud(owner,1));
  owner.Damage("gun-2",1000);owner.Advance(2,2,0,false);var state=Hud(owner,2);var before=state.ToString();Assert.AreEqual("포대 무력화",display.Observe(state,false));Assert.AreEqual(before,state.ToString());
  var restored=new CombatArtilleryOutcomeFeedback();restored.Seed(state);Assert.IsNull(restored.Observe(state,false));
  state["tick"]=62;Assert.IsNull(display.Observe(state,false));Assert.IsNull(display.Observe(state,false));
 }
 [Test] public void NewThreatSuppressesSummaryAndItDoesNotReappearAfterThreat(){
  var owner=new CombatRailArtillery(CombatRailArtilleryTests.Config());owner.Advance(1,2,0,false);var display=new CombatArtilleryOutcomeFeedback();display.Seed(Hud(owner,1));
  owner.Advance(11,2,0,false);owner.Damage("gun-2",1000);owner.Advance(12,2,0,false);var state=Hud(owner,12);
  Assert.IsNotEmpty(CombatThreatProjection.Read(state).Warnings);Assert.IsNull(display.Observe(state,true));
  owner.Advance(21,2,0,false);Assert.IsNull(display.Observe(Hud(owner,21),false));
 }
 [Test] public void PassedSourceIsNotClaimedAsAvoidance(){
  var owner=new CombatRailArtillery(CombatRailArtilleryTests.Config());owner.Advance(1,2,0,false);var display=new CombatArtilleryOutcomeFeedback();display.Seed(Hud(owner,1));owner.Advance(2,2,120000,false);Assert.IsNull(display.Observe(Hud(owner,2),false));
 }
}
