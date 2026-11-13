using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatVitalsRebaseTests {
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static CombatVitals Modern(int max=1000){var ctor=typeof(CombatVitals).GetConstructor(new[]{typeof(int),typeof(int),typeof(bool)});Assert.NotNull(ctor,"Explicit approved HP policy missing");return (CombatVitals)ctor.Invoke(new object[]{max,4000,true});}
  static void Rebase(CombatVitals v,int max){var m=typeof(CombatVitals).GetMethod("RebaseMaxHp");Assert.NotNull(m);m.Invoke(v,new object[]{max});}
  static JObject Save(CombatVitals v)=>(JObject)typeof(CombatVitals).GetMethod("CaptureLocal",Private).Invoke(v,null);
  static void Load(CombatVitals v,JObject j,long tick=0)=>typeof(CombatVitals).GetMethod("RestoreLocal",Private).Invoke(v,new object[]{j,tick});
  [Test] public void RepeatedPureMaxChangesAndReloadKeepOriginalRatio(){var v=Modern();v.Damage(667,0,false);for(int i=0;i<3;i++){Rebase(v,1500);Assert.AreEqual(499,v.Hp);var r=Modern();Load(r,Save(v));v=r;Rebase(v,1000);Assert.AreEqual(333,v.Hp);}Assert.AreEqual(1000,v.MaxHp);}
  [Test] public void ActualHpLossRefreshesAnchorButShieldOnlyAndZeroDoNot(){var v=Modern();v.Damage(667,0,false);Rebase(v,1500);v.Shield(new CombatShieldSource("a","shield",0),10,8,0);v.Damage(1,0,false);v.Damage(0,0,false);Assert.AreEqual(0,v.RestoreHp(0,10000));Rebase(v,1000);Assert.AreEqual(333,v.Hp);Rebase(v,1500);v.Damage(10,0,false);Assert.AreEqual(498,v.Hp);Rebase(v,1000);Assert.AreEqual(332,v.Hp);}
  [Test] public void MinimumOneDoesNotBecomeNewAnchorOrResurrectDead(){var v=Modern();v.Damage(999,0,false);Rebase(v,100);Assert.AreEqual(1,v.Hp);Rebase(v,1000);Assert.AreEqual(1,v.Hp);Rebase(v,100);v.Damage(1,0,false);Rebase(v,2000);Assert.AreEqual(0,v.Hp);Assert.AreEqual(0,v.RestoreHp(500,10000));}
  [Test] public void ActualHealReanchorsAtEffectiveMax(){var v=Modern();v.Damage(500,0,false);Rebase(v,2000);Assert.AreEqual(100,v.RestoreHp(100,10000));Rebase(v,1000);Assert.AreEqual(550,v.Hp);}
  [Test] public void TransferAndScheduledRecoveryRefreshAnchors(){var v=Modern();v.Damage(500,0,false);Rebase(v,2000);typeof(CombatVitals).GetMethod("ApplyTransferredHpLoss",Private).Invoke(v,new object[]{200,0L});Rebase(v,1000);Assert.AreEqual(400,v.Hp);v.Damage(400,0,false);Rebase(v,1500);typeof(CombatVitals).GetMethod("RecoverFromDown",Private).Invoke(v,new object[]{300});Rebase(v,1000);Assert.AreEqual(200,v.Hp);}
  [Test] public void ModernDirectShieldExceedsHpAndSurvivesRebaseAndRestore(){var v=Modern();v.Shield(new CombatShieldSource("a","shield",0),3000,9,0);Rebase(v,100);Assert.AreEqual(3000,v.ShieldRemaining(0));var r=Modern();Load(r,Save(v));Assert.AreEqual(3000,r.ShieldRemaining(8));Assert.AreEqual(0,r.ShieldRemaining(9));}
  [Test] public void InvalidAnchorOrMissingVersionLeavesOriginalState(){var v=Modern();v.Damage(667,0,false);Rebase(v,1500);var good=Save(v);foreach(var key in new[]{"schema","anchorHp","anchorMaxHp","maxHp"}){var bad=(JObject)good.DeepClone();bad.Remove(key);var before=Save(v).ToString();Assert.Throws<TargetInvocationException>(()=>Load(v,bad));Assert.AreEqual(before,Save(v).ToString());}var mismatch=(JObject)good.DeepClone();mismatch["hp"]=500;Assert.Throws<TargetInvocationException>(()=>Load(v,mismatch));}
  [Test] public void OldAndNewPoliciesRejectEachOthersState(){var old=new CombatVitals(1000,4000);var modern=Modern();Assert.Throws<TargetInvocationException>(()=>Load(modern,Save(old)));Assert.Throws<TargetInvocationException>(()=>Load(old,Save(modern)));old.Shield(new CombatShieldSource("a","shield",0),3000,9,0);Assert.AreEqual(400,old.ShieldRemaining(0));}
  [Test] public void LargeRebaseDoesNotOverflow(){var v=Modern(int.MaxValue);v.Damage(1,0,false);Rebase(v,1);Assert.AreEqual(1,v.Hp);Rebase(v,int.MaxValue);Assert.AreEqual(int.MaxValue-1,v.Hp);}
 }
}