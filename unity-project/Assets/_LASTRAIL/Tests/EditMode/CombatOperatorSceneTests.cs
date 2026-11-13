using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public sealed class CombatOperatorSceneTests {
 static BasicCombatSimulation Fresh(bool general){string folder=general?"ce03-general-active-auto-hM29XX":"ce03-individual-ultimates-cbVWkL";var p=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/"+folder+"/prepared.json"));var c=new Ce03CommandContract((JObject)p["commandContract"]);return c.CreateLocalTest(p,(string)p["ownedSelection"]["accountId"],c.Revision,1);}
 static GameObject Hud()=>UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab"));
 [Test] public void FailedPresentationPreservesBorrowedHudAndRetryPublishesOnlyAfterReady(){
  var go=Hud();var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");var openings=art.WindowOpenings;
  try{
   var scene=go.AddComponent<BattleSceneRoot>();var children=go.transform.Cast<Transform>().Select(t=>(Child:t,Active:t.gameObject.activeSelf)).ToArray();var components=go.GetComponents<Component>();
   var driver=new CombatSceneDriver(()=>Fresh(false),()=>null,(s,r)=>r+1,()=>true);
   art.WindowOpenings=null;
   Assert.Throws<NullReferenceException>(()=>scene.BindCombat(driver,go));Assert.IsNull(scene.Combat);Assert.NotNull(scene.RecoveryNotice);
   CollectionAssert.AreEquivalent(components,go.GetComponents<Component>());CollectionAssert.AreEqual(children.Select(c=>c.Child),go.transform.Cast<Transform>());
   foreach(var c in children)Assert.AreEqual(c.Active,c.Child.gameObject.activeSelf);
   var state=driver.StateJson;typeof(BattleSceneRoot).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(scene,null);Assert.AreEqual(state,driver.StateJson);
   art.WindowOpenings=openings;scene.BindCombat(driver,go);Assert.AreSame(driver,scene.Combat);Assert.IsNull(scene.RecoveryNotice);Assert.AreEqual(state,driver.StateJson);
   Assert.AreEqual(5,go.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Ultimate_")));
  }finally{art.WindowOpenings=openings;UnityEngine.Object.DestroyImmediate(go);}
 }
 [Test] public void UnsupportedOrdinaryHudCannotPublishRunningDriver(){
  var go=Hud();try{var f=OrdinaryFiveSeraFactoryTests.Sources();var factory=new OrdinaryFiveSeraFactory(f.Input,f.Stats,500000);var driver=new CombatSceneDriver(factory.Create,()=>null,(s,r)=>r+1,()=>true);var root=go.AddComponent<BattleSceneRoot>();
   Assert.Throws<InvalidOperationException>(()=>root.BindCombat(driver,go));Assert.IsNull(root.Combat);var state=driver.StateJson;typeof(BattleSceneRoot).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(root,null);Assert.AreEqual(state,driver.StateJson);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 [TestCase(false)][TestCase(true)]public void RealSceneRootBindsNewPoliciesIntoApprovedView(bool general){var go=Hud();try{var driver=new CombatSceneDriver(()=>Fresh(general),()=>null,(s,r)=>r+1,()=>true);var scene=go.AddComponent<BattleSceneRoot>();Assert.DoesNotThrow(()=>scene.BindCombat(driver,go));Assert.NotNull(go.GetComponent<ApprovedCombatView>());Assert.AreEqual(5,go.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Ultimate_")));Assert.AreEqual(general?0:5,go.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("General_")));Assert.IsTrue(go.GetComponent<CombatLiveHudBinding>()==null||!go.GetComponent<CombatLiveHudBinding>().enabled);}finally{UnityEngine.Object.DestroyImmediate(go);}}
 [TestCase(false)][TestCase(true)]public void FallbackRetainsV2GeneralAndHasSingleFunctionalUltimateAuto(bool general){var go=Hud();try{var driver=new CombatSceneDriver(()=>Fresh(general),()=>null,(s,r)=>r+1,()=>true);driver.Open();var binding=go.AddComponent<CombatLiveHudBinding>();binding.Bind(driver);binding.Bind(driver);var buttons=go.GetComponentsInChildren<Button>(true);var auto=buttons.SingleOrDefault(b=>b.name=="LiveUltimateAuto");Assert.NotNull(auto);auto.onClick.Invoke();Assert.IsTrue((bool)driver.HudState["ultimateAuto"],"One listener after rebind toggles once");auto.onClick.Invoke();Assert.IsFalse((bool)driver.HudState["ultimateAuto"]);if(!general){buttons.Single(b=>b.name=="SkillIconSlot_0").onClick.Invoke();driver.Advance(500000,1);Assert.IsTrue(((JArray)driver.HudState["casts"]).Any(c=>(string)c["ability"]=="op_kyle.active"));Assert.AreEqual(5,buttons.Count(b=>b.name.StartsWith("LiveUltimate_op_")));}driver.SetPaused(true);binding.Refresh();Assert.IsFalse(auto.interactable);}finally{UnityEngine.Object.DestroyImmediate(go);}}
}
