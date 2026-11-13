using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class OrdinaryGunnerPresentationTests {
 [Test] public void IssuedOrdinaryGunnersBindActualPlayerPresentationWithoutChangingHpOrOpeningCombat(){
  var f=FirstPlayableScalingTests.Fixture(staticSource:true);var original=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);
  var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  var factory=OrdinaryFiveCombatFactory.FromInitialization(original,f.Init.ToString(),500000,arrival.Scaling);
  int writes=0;var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>{writes++;return revision+1;},()=>true);driver.Open();
  var enemies=((JArray)driver.HudState["actors"]).Where(a=>(bool)a["enemy"]).ToArray();Assert.AreEqual(2,enemies.Length);CollectionAssert.AreEquivalent(arrival.Scaling.IssuedGunnerIds,enemies.Select(a=>(string)a["id"]).ToArray());
  var binding=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");Assert.NotNull(binding);
  var input=new GameObject("OrdinaryGunnerTestInput",typeof(EventSystem));var host=new GameObject("OrdinaryGunnerProductHost");var scene=host.AddComponent<CombatPlayerScene>();
  using var controller=new OrdinaryPlayerBattleController(original,driver,binding,()=>true,()=>false);
  try{string before=driver.StateJson;int prior=writes;Assert.DoesNotThrow(()=>controller.Present(scene,input.GetComponent<EventSystem>(),()=>{},r=>Assert.Fail("Unexpected result"),e=>Assert.Fail(e.ToString())));
   Assert.IsTrue(scene.IsOpen);Assert.IsFalse(scene.IsRunning);Assert.AreEqual(before,driver.StateJson);Assert.AreEqual(prior,writes);Assert.IsFalse((bool)driver.HudState["fullCombatReady"]);
   foreach(var enemy in enemies)Assert.NotNull(scene.View.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name==(string)enemy["id"]));
   var output=Environment.GetEnvironmentVariable("LASTRAIL_GUNNER_PRESENTATION_SOURCE");if(!string.IsNullOrEmpty(output)){string path=Path.GetFullPath(output);if(!path.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase)||File.Exists(path))throw new ArgumentException("Fresh D QA source required");File.WriteAllText(path,new JObject{["scope"]="ACTUAL_ORDINARY_FACTORY_AND_ASSET_BINDING_SYNTHETIC_ARRIVAL_NOT_PRODUCT_ADMISSION",["originalStart"]=f.Start,["initializationResponse"]=f.Init,["binding"]=f.Command.Binding.Capture(),["command"]=f.Command.Capture()["command"],["response"]=f.Response,["manifestHash"]=binding.ManifestHash}.ToString());}
  }finally{controller.Dispose();UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(input);}
 }
}
