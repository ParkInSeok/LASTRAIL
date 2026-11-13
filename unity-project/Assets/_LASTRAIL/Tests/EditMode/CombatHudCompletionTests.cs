using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using TMPro;
using UnityEngine;
public class CombatHudCompletionTests {
 [Test] public void WheelAndSeparateBarrelFollowSimulationAndPause(){
  var d=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();var go=new GameObject("Rig",typeof(RectTransform),typeof(Canvas));
  try{var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var wheels=go.GetComponentsInChildren<CombatWheelDisc>();Assert.AreEqual(9,wheels.Length);Assert.IsTrue(wheels.All(w=>w.GetComponent<CanvasRenderer>()!=null));
   var barrels=go.GetComponentsInChildren<RectTransform>().Where(r=>r.name=="RecoilBarrel").ToArray();Assert.AreEqual(2,barrels.Length);
   bool recoil=false;for(int i=0;i<160;i++){d.Advance(500000,1);v.Refresh();recoil|=barrels.Any(r=>r.anchoredPosition.x<55.9f);}
   Assert.IsTrue(recoil,"Actual train/device hit must drive barrel recoil");Assert.AreNotEqual(0,wheels[0].Angle);
   float angle=wheels[0].Angle;var positions=barrels.Select(b=>b.anchoredPosition).ToArray();d.SetPaused(true);d.Advance(10000000,20);v.Refresh();Assert.AreEqual(angle,wheels[0].Angle);CollectionAssert.AreEqual(positions,barrels.Select(b=>b.anchoredPosition));
  }finally{Object.DestroyImmediate(go);}
 }
 [Test] public void LeaderLightTracksChargeNotButtonPermissionAndConsumes(){
  var d=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();
  var go=new GameObject("HUD",typeof(RectTransform),typeof(Canvas));
  try{
   var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var frame=go.GetComponentsInChildren<CombatReadyFrame>().Single(x=>x.name=="LeaderReadyFrame");Assert.AreEqual(0,frame.Progress);
   var percent=frame.transform.parent.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="Text");Assert.AreEqual("0%",percent.text);
   for(int i=0;i<650&&(int)d.HudState["ultimate"]["charge"]<100;i++){d.Advance(500000,1);v.Refresh();}
   Assert.AreEqual("100%",percent.text);d.Advance(5000000,10);v.Refresh();Assert.AreEqual(1,frame.Progress);
   d.SetPaused(true);v.Refresh();Assert.AreEqual(1,frame.Progress);d.SetPaused(false);
   var u=d.HudState["ultimate"];d.RequestSkill("light-cast",(string)u["actor"],(string)u["ability"]);
   for(int i=0;i<100&&(int)d.HudState["ultimate"]["charge"]==100;i++){d.Advance(500000,1);v.Refresh();}
   Assert.Less((int)d.HudState["ultimate"]["charge"],100);Assert.AreEqual(0,frame.Progress);
  }finally{Object.DestroyImmediate(go);}
 }
 [Test] public void CooldownLeavesNameFixedAndPausesWithModel(){
  var d=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);d.Open();var go=new GameObject("HUD",typeof(RectTransform),typeof(Canvas));
  try{
   var v=go.AddComponent<ApprovedCombatView>();v.Bind(d,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var card=go.GetComponentsInChildren<RectTransform>().Single(x=>x.name=="skills0");var label=card.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="Text");string name=label.text;
   var row=d.HudState["skills"][0];d.RequestSkill("cooldown",(string)row["actor"],(string)row["ability"]);
   for(int i=0;i<100&&(long)d.HudState["skills"][0]["cooldownMs"]==0;i++)d.Advance(500000,1);v.Refresh();
   var wipe=card.GetComponentInChildren<CombatCooldownWipe>();Assert.Greater(wipe.Remaining,0);Assert.AreEqual(name,label.text);
   float before=wipe.Remaining;d.SetPaused(true);d.Advance(10000000,20);v.Refresh();Assert.AreEqual(before,wipe.Remaining);
   d.SetPaused(false);d.Advance(10000000,20);v.Refresh();Assert.Less(wipe.Remaining,before);Assert.AreEqual(name,label.text);
  }finally{Object.DestroyImmediate(go);}
 }
}
