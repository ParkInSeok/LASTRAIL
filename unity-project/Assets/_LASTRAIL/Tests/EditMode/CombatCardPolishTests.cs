using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatCardPolishTests {
 [Test] public void CardsReflectAuthoritativeChargePauseAndCastingWithoutChangingState(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("cards",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));var chrome=go.transform.Find("ApprovedBattle/ultimate0/CommonCardFrame").GetComponent<CombatPanelChrome>();
   Assert.AreEqual("CHARGING",chrome.State);Assert.IsFalse(chrome.Enabled);
   for(int i=0;i<1000&&!(bool)driver.HudState["ultimate"]["enabled"];i++){driver.Advance(500000,1);view.Refresh();}
   Assert.AreEqual(100,(int)driver.HudState["ultimate"]["charge"]);Assert.IsTrue(chrome.Enabled);
   driver.SetPaused(true);view.Refresh();Assert.IsFalse(chrome.Enabled);driver.SetPaused(false);view.Refresh();var row=driver.HudState["ultimate"];driver.RequestSkill("card-actual-cast",(string)row["actor"],(string)row["ability"]);
   for(int i=0;i<30&&(string)driver.HudState["ultimate"]["status"]!="CASTING";i++){driver.Advance(500000,1);view.Refresh();}
   Assert.AreEqual("CASTING",chrome.State);Assert.IsFalse(chrome.Enabled);Assert.Less((int)driver.HudState["ultimate"]["charge"],100);string before=driver.StateJson;view.Refresh();Assert.AreEqual(before,driver.StateJson);
  }finally{Object.DestroyImmediate(go);}
 }
 [Test] public void FiveActiveAbilitiesUseDifferentReviewedIconRegions(){
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");Assert.NotNull(assets.ActiveIconAtlas);Assert.AreEqual(5,assets.ActiveIconIds.Distinct().Count());Assert.AreEqual(5,assets.ActiveIconRegions.Distinct().Count());
  foreach(string id in new[]{"op_kyle.active","op_reina.active","op_eve.active","op_sera.active","op_mira.active"}){int i=System.Array.IndexOf(assets.ActiveIconIds,id);Assert.GreaterOrEqual(i,0);var uv=assets.ActiveIconRegions[i];Assert.Greater(uv.width,0);Assert.Greater(uv.height,0);Assert.GreaterOrEqual(uv.xMin,0);Assert.LessOrEqual(uv.xMax,1);Assert.GreaterOrEqual(uv.yMin,0);Assert.LessOrEqual(uv.yMax,1);}
 }
}
