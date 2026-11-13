using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class OrdinaryGunnerArtSelectionTests {
 [Test] public void ActualOrdinaryBindingShowsMissingGunnerInsteadOfHistoricalBuggy(){
  var f=FirstPlayableScalingTests.Fixture(staticSource:true);
  var original=OrdinaryFiveStartInput.Read(f.Start,"a",(string)f.Start["runId"]);
  var arrival=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);
  var factory=OrdinaryFiveCombatFactory.FromInitialization(original,f.Init.ToString(),500000,arrival.Scaling);
  var driver=new CombatSceneDriver(factory.Create,()=>null,(state,revision)=>revision+1,()=>true);driver.Open();
  var binding=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");Assert.NotNull(binding);
  var input=new GameObject("OrdinaryArtInput",typeof(EventSystem));var host=new GameObject("OrdinaryArtHost");
  var scene=host.AddComponent<CombatPlayerScene>();
  using var controller=new OrdinaryPlayerBattleController(original,driver,binding,()=>true,()=>false);
  try{
   string before=driver.StateJson;
   controller.Present(scene,input.GetComponent<EventSystem>(),()=>{},r=>Assert.Fail("Unexpected result"),e=>Assert.Fail(e.ToString()));
   foreach(var actor in driver.HudState["actors"].Where(a=>(bool)a["enemy"])){
    var unit=scene.View.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)actor["id"]);
    Assert.IsNull(unit.Find("Unit").GetComponent<RawImage>().texture,"Ordinary gunner must not display buggy or misregistered hound art.");
    Assert.IsTrue(unit.Find("MissingFigure").gameObject.activeSelf);
   }
   scene.View.Refresh();Assert.AreEqual(before,driver.StateJson);Assert.IsFalse(scene.IsRunning);
  }finally{controller.Dispose();Object.DestroyImmediate(host);Object.DestroyImmediate(input);}
 }

 [TestCase("idle",1,true,false)][TestCase("attack",2,true,false)][TestCase("down_or_defeated",4,true,false)][TestCase("down_or_defeated",4,true,true)]
 [TestCase("idle",1,false,false)][TestCase("attack",2,false,false)][TestCase("down_or_defeated",4,false,false)][TestCase("down_or_defeated",4,false,true)]
 public void OrdinarySelectionRejectsBothVehicleFallbackAndMisregisteredFrames(string animation,int frame,bool vehicle,bool lethalHit){
  var binding=LastRail.Resources.PreparedContent.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");
  var formation=((JArray)JObject.Parse(binding.ManifestJson)["formation"]).Values<string>().ToArray();
  using var lease=binding.Acquire(binding.ManifestHash,formation);
  var host=new GameObject("OrdinaryFigureSelection");
  try{
   var view=host.AddComponent<ApprovedCombatView>();
   typeof(ApprovedCombatView).GetField("assets",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,lease.ReadAssets());
   var select=typeof(ApprovedCombatView).GetMethod("SelectActorFigure",BindingFlags.Instance|BindingFlags.NonPublic);
   Assert.NotNull(select,"Both ordinary refresh and lethal hit must share the source selection policy.");
   object[] args={OrdinaryFiveCombatFactory.Version,"enemy_spike_cart_gunner",vehicle,animation,frame,lethalHit,null};
   Assert.IsNull(select.Invoke(view,args));Assert.IsNull(args[6]);
   args[0]="historical-ce03";
   var historical=(Texture2D)select.Invoke(view,args);
   Assert.AreSame(vehicle?lease.ReadAssets().Buggy:lease.ReadAssets().Find("enemy_spike_cart_gunner/"+animation+"/"+frame)??lease.ReadAssets().Find("enemy_spike_cart_gunner/idle/1"),historical);
  }finally{Object.DestroyImmediate(host);}
 }
}
