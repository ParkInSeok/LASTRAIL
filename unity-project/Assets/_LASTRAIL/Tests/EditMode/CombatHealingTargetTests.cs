using System;
using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatHealingTargetTests {
  static string Choose((string id,int slot,CombatVitals hp)[] candidates,bool manualFallback,string previous=null){
   var assembly=typeof(CombatVitals).Assembly;var item=assembly.GetType("LastRail.Battle.CombatHealingTarget");var policy=assembly.GetType("LastRail.Battle.CombatHealingTargetSelection");Assert.IsNotNull(item);Assert.IsNotNull(policy);
   var array=Array.CreateInstance(item,candidates.Length);for(int i=0;i<candidates.Length;i++)array.SetValue(Activator.CreateInstance(item,candidates[i].id,candidates[i].slot,candidates[i].hp),i);
   var result=policy.GetMethod("Select").Invoke(null,new object[]{array,manualFallback,previous});return result==null?null:(string)item.GetProperty("Id").GetValue(result);
  }
  static CombatVitals Hp(int max,int loss=0){var h=new CombatVitals(max,0);h.Damage(loss,0,false);return h;}
  [Test]public void InjuredPriorityIgnoresFullPreviousAndUsesExactRatioThenSlot(){
   var values=new[]{("full",0,Hp(1000)),("half",2,Hp(1000,500)),("third",3,Hp(300,200)),("same",1,Hp(600,400))};Assert.AreEqual("same",Choose(values,true,"full"));
  }
  [Test]public void ManualFallbackPrefersPreviousThenSlotAndStableId(){
   var values=new[]{("b",2,Hp(1000)),("z",1,Hp(1000)),("a",1,Hp(1000))};Assert.AreEqual("b",Choose(values,true,"b"));Assert.AreEqual("a",Choose(values,true,"missing"));Assert.AreEqual("a",Choose(values.Reverse().ToArray(),true));
  }
  [Test]public void NoApprovedManualFallbackNeverSpendsOnFullTargets(){Assert.IsNull(Choose(new[]{("full",0,Hp(1000))},false,"full"));}
  [Test]public void DownAndAbsentTargetsAreNeverInvented(){Assert.IsNull(Choose(new[]{("down",0,Hp(1000,1000))},true,"down"));Assert.IsNull(Choose(Array.Empty<(string,int,CombatVitals)>(),true));}
 }
}
