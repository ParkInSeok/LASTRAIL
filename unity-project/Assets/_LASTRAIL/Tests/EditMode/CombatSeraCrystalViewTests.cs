using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatSeraCrystalViewTests {
 [Test] public void IdleUsesApprovedDirectionCenterAndThreeDistinctCrystals(){
  var a=CombatSeraCrystalView.Sample(false,"idle",0,0,null);
  var b=CombatSeraCrystalView.Sample(true,"idle",0,0,null);
  Assert.That(a.Center.x,Is.EqualTo(550));Assert.That(b.Center.x,Is.EqualTo(474));
  Assert.That(Vector2.Distance(a.Point(0),a.Point(1)),Is.GreaterThan(100));
  Assert.That(a.Point(0).x,Is.EqualTo(884.36777f).Within(.001));
 }
 [Test] public void ActiveCannotInventARecipient(){
  var a=CombatSeraCrystalView.Sample(false,"active",1100,2000,null);
  Assert.That(a.Heal,Is.Zero);Assert.That(a.Targeted,Is.False);
  var b=CombatSeraCrystalView.Sample(false,"active",1100,2000,new Vector2(275,200));
  Assert.That(b.Center,Is.EqualTo(new Vector2(275,200)));Assert.That(b.Heal,Is.GreaterThan(0));
 }
 [Test] public void UltimateHasApprovedHoldIgnitionAndReturn(){
  var start=CombatSeraCrystalView.Sample(false,"ultimate",0,0,null);
  var peak=CombatSeraCrystalView.Sample(false,"ultimate",1980,0,null);
  var end=CombatSeraCrystalView.Sample(false,"ultimate",3060,0,null);
  Assert.That(start.Gate,Is.Zero);Assert.That(start.Core,Is.Zero);
  Assert.That(peak.Core,Is.EqualTo(1).Within(.0001));Assert.That(peak.Gate,Is.EqualTo(1));
  Assert.That(end.Gate,Is.Zero);Assert.That(end.Core,Is.Zero);Assert.That(end.Heal,Is.Zero);
 }
 [Test] public void RightBasicBeamHasBoundedWindowAndRequiresActualTarget(){
  Assert.That(CombatSeraCrystalView.Sample(true,"basic",1000,1900,new Vector2(690,145)).Beam,Is.EqualTo(1));
  Assert.That(CombatSeraCrystalView.Sample(true,"basic",1220,2120,new Vector2(690,145)).Beam,Is.Zero);
  Assert.That(CombatSeraCrystalView.Sample(true,"basic",1000,1900,null).Beam,Is.Zero);
  Assert.That(CombatSeraCrystalView.Sample(false,"basic",1000,1900,new Vector2(0,145)).Beam,Is.Zero);
 }
 [Test] public void HiddenOrDisabledViewLeavesNoVisibleOwnedEffects(){
  var go=new GameObject("sera-fx",typeof(RectTransform));
  try{var view=go.AddComponent<CombatSeraCrystalView>();view.Render(false,false,"idle",0,0,null,null);Assert.That(view.Visible,Is.False);view.enabled=false;Assert.That(view.Visible,Is.False);}
  finally{Object.DestroyImmediate(go);}
 }
}
