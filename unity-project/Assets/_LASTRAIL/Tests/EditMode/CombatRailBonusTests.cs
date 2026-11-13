using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRailBonusTests {
 [TestCase(500,460)][TestCase(3000,520)] public void ActualBarrierAddsRunBonusBeforeExistingMagnitudeCap(int grant,int expected){
  var prepared=(JObject)CombatRailPreparationTests.Golden()["basePreparation"];var commands=new Ce03CommandContract((JObject)prepared["commandContract"]);var model=commands.CreateLocalTest(prepared,"a",1,1);
  var config=CombatRailJourneyTests.Config();config["offers"][0]["afterCompletedWaves"]=1;var effect=config["offers"][0]["options"][0]["effect"];effect["stat"]="SHIELD_MAGNITUDE";effect["valueBps"]=grant;model.ConfigureRailJourney(config);
  string hash=(string)JObject.Parse(model.CaptureLocalState())["configurationSha256"];
  for(int i=0;i<500&&(string)model.ReadHudState()["railJourney"]["phase"]!="Selecting";i++)model.Advance(1000000,8);
  Assert.AreEqual("Selecting",(string)model.ReadHudState()["railJourney"]["phase"]);Assert.IsTrue(model.ConfirmRailRoute("fork-3","left"));
  string train=(string)((JArray)model.ReadHudState()["actors"]).Single(a=>(bool)a["train"])["id"];model.RequestSkill("bonus-barrier",train,"barrier");
  JToken receipt=null;for(int i=0;i<100&&receipt==null;i++){model.Advance(1000000,8);receipt=((JArray)model.ReadHudState()["effectResults"]).FirstOrDefault(r=>(string)r["commandId"]=="bonus-barrier"&&(string)r["kind"]=="SHIELD_ISSUE");}
  Assert.NotNull(receipt);Assert.AreEqual(expected,(int)receipt["actualResult"]["addedAmount"],"400 base shield × (base 1000bps + run grant), capped at 3000bps, one final floor");Assert.AreEqual(hash,(string)JObject.Parse(model.CaptureLocalState())["configurationSha256"]);
 }
}
