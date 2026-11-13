using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRepairDroneTimelineTests {
 static JObject Hud(double time,string status="CASTING",int gain=0)=>new JObject{["motionSeconds"]=time,["outcome"]="Running",["casts"]=new JArray(new JObject{["id"]="repair",["ability"]="emergency_repair",["status"]=status,["due"]=12,["windupTicks"]=12}),["effectResults"]=gain==0?new JArray():new JArray(new JObject{["commandId"]="repair",["kind"]="REPAIR",["actualResult"]=new JObject{["actualHpGain"]=gain}})};
 [Test] public void WeldingRequiresPositiveActualReceiptAndReturnsAfterRepair(){var reader=new CombatRepairDroneTimeline(Hud(0));Assert.IsFalse((bool)reader.Read(Hud(.5))["weld"]);Assert.IsFalse((bool)reader.Read(Hud(.6,"APPLIED"))["weld"]);var hit=reader.Read(Hud(.6,"APPLIED",400));Assert.IsTrue((bool)hit["weld"]);Assert.AreEqual(1,(double)hit["travel"]);Assert.Greater((double)reader.Read(Hud(1.5,"APPLIED",400))["return"],0);Assert.IsFalse((bool)reader.Read(Hud(2,"APPLIED",400))["active"]);}
 [Test] public void RestoredHistoryDoesNotReplayAndPausedReadIsStable(){var hud=Hud(.7,"APPLIED",400);Assert.IsFalse((bool)new CombatRepairDroneTimeline(hud).Read(hud)["active"]);var live=new CombatRepairDroneTimeline(Hud(0));var before=hud.ToString();Assert.AreEqual(live.Read(hud).ToString(),live.Read(hud).ToString());Assert.AreEqual(before,hud.ToString());}
 [Test] public void TerminalAndCancelledNeverWeld(){var r=new CombatRepairDroneTimeline(Hud(0));Assert.IsFalse((bool)r.Read(Hud(.4,"CANCELLED"))["active"]);var hud=Hud(.7,"APPLIED",400);hud["outcome"]="Failure";Assert.IsFalse((bool)r.Read(hud)["active"]);}
}
