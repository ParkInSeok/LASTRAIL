using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class OrdinaryLiveThrottleViewTests {
 [TestCase(0,14000)][TestCase(1,26000)]
 public void ExistingSliderCallsOriginalLiveThrottleOnce(float position,long expected){
  var factory=OrdinaryLiveExecutionTests.CreateFactory(OrdinaryLiveExecutionTests.CreateInput());
  var driver=CombatSceneDriver.ForOrdinaryNode(factory,()=>true);driver.Open();
  var root=new GameObject("LiveThrottle",typeof(RectTransform),typeof(Canvas));
  try{
   var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation"));view.UseLandscapeLayout();
   var slider=root.GetComponentsInChildren<Slider>(true).Single(s=>s.name=="TrainThrottle");Assert.IsTrue(slider.interactable);
   int before=((JArray)JObject.Parse(driver.StateJson)["operations"]).Count;slider.value=position;
   Assert.AreEqual(expected,(long)driver.HudState["ordinaryRail"]["targetSpeedMmPerSecond"]);
   Assert.AreEqual(before+1,((JArray)JObject.Parse(driver.StateJson)["operations"]).Count);
   view.Refresh();Assert.AreEqual(before+1,((JArray)JObject.Parse(driver.StateJson)["operations"]).Count);
   Assert.IsFalse(root.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text=="속도 정보 없음"));
   Assert.AreEqual(position,slider.value);
  }finally{Object.DestroyImmediate(root);driver.Retire();}
 }
}
