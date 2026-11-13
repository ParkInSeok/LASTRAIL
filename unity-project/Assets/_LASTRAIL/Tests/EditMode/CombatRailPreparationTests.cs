using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatRailPreparationTests {
 public static JObject Golden(bool four=false)=>JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-RailJourney-20260914/synthetic-rail-preparation"+(four?"-four-cars":"")+".json"));
 static object Parse(JObject value){var t=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.Ce03RailPreparation");Assert.NotNull(t);try{return Activator.CreateInstance(t,new object[]{value,"a",1L});}catch(TargetInvocationException e){throw e.InnerException;}}
 [TestCase(false,2)][TestCase(true,4)] public void VerifiedServerWrapperCreatesConfiguredPlayableModel(bool four,int count){var p=Parse(Golden(four));var model=(BasicCombatSimulation)p.GetType().GetMethod("CreateModel").Invoke(p,new object[]{1u});var hud=model.ReadHudState();Assert.AreEqual(count,((JArray)hud["railJourney"]["carriages"]).Count);Assert.AreEqual(10000,(int)hud["railJourney"]["throttleBps"]);Assert.AreEqual("ce03-rail-headless-state-v1",(string)JObject.Parse(model.CaptureLocalState())["schema"]);}
 [Test] public void WrapperMutationAndLegacyInputAreRejected(){var changed=Golden();changed["railJourney"]["offers"][0]["options"][0]["effect"]["valueBps"]=999;Assert.Throws<ArgumentException>(()=>Parse(changed));Assert.Throws<ArgumentException>(()=>Parse((JObject)Golden()["basePreparation"]));}
}
