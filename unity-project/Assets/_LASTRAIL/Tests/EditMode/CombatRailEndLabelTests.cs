using System.Reflection;
using LastRail.Battle;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CombatRailEndLabelTests {
 [TestCase("Victory","작전 완료")][TestCase("Failure","작전 종료")]
 public void TerminalHudDoesNotAdvertiseAnotherJunction(string outcome,string label){
  var prep=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);var go=new GameObject("terminal-label",typeof(RectTransform),typeof(Canvas));
  try{var driver=new CombatSceneDriver(()=>prep.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);driver.Open();var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var hud=driver.HudState;hud["outcome"]=outcome;typeof(ApprovedCombatView).GetMethod("RefreshRailControls",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,new object[]{hud});Assert.AreEqual(label,go.transform.Find("ApprovedBattle/RouteDescription").GetComponent<TMP_Text>().text);Assert.IsFalse(go.transform.Find("ApprovedBattle/BranchLever").GetComponent<Slider>().interactable);Assert.IsFalse(go.transform.Find("ApprovedBattle/TrainThrottle").GetComponent<Slider>().interactable);}
  finally{Object.DestroyImmediate(go);}
 }
}
