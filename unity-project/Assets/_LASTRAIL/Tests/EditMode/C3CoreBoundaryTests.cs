#if UNITY_EDITOR
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class C3CoreBoundaryTests {
  [Test] public void NativeDriverTogglePauseResumeKeepsOriginalCheckpointRestorable(){
   var prepared=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);
   var d=new CombatSceneDriver(()=>prepared.CreateModel(1),()=>null,(s,r)=>r+1,()=>true);d.Open();
   d.SetUltimateAuto(true);Assert.IsTrue((bool)d.HudState["ultimateAuto"],"First AUTO");
   d.SetUltimateAuto(false);Assert.IsFalse((bool)d.HudState["ultimateAuto"],"Second AUTO");Assert.IsFalse(d.RecoveryRequired,"AUTO restore");
   foreach(var speed in new[]{3,4,2}){d.SetSpeed(speed);Assert.AreEqual(speed,(int)d.HudState["speedHalfUnits"],"Speed restore");}
   d.SetPaused(true);var paused=d.StateJson;d.Advance(10000000,8);Assert.AreEqual(paused,d.StateJson,"Paused advance");
   d.SetPaused(false);Assert.IsFalse((bool)d.HudState["paused"],"Resume");Assert.IsFalse(d.RecoveryRequired,"Resume restore");
  }
 }
}
#endif