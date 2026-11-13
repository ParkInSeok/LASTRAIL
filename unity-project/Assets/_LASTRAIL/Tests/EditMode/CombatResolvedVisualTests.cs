using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatResolvedVisualTests {
 static JObject Row(string kind,JObject actual)=>new JObject{["id"]="one",["kind"]=kind,["actualResult"]=actual};
 [TestCase("HEAL","Heal")][TestCase("REPAIR","Repair")]
 public void PositiveRecoveryTypesStayDistinct(string kind,string shape){var row=Row(kind,new JObject{["actualHpGain"]=1});string before=row.ToString();Assert.AreEqual(shape,CombatResolvedVisual.Shape(row));Assert.AreEqual(before,row.ToString());row["actualResult"]["actualHpGain"]=0;Assert.IsNull(CombatResolvedVisual.Shape(row));}
 [Test] public void ShieldRefreshAndUnabsorbedDamageDoNotPretendShieldSuccess(){Assert.IsNull(CombatResolvedVisual.Shape(Row("SHIELD_ISSUE",new JObject{["addedAmount"]=0,["outcome"]="REFRESH"})));Assert.AreEqual("ShieldIssue",CombatResolvedVisual.Shape(Row("SHIELD_ISSUE",new JObject{["addedAmount"]=7})));Assert.AreEqual("ShieldAbsorb",CombatResolvedVisual.Shape(Row("DAMAGE",new JObject{["hpLoss"]=0,["absorbed"]=2})));Assert.IsNull(CombatResolvedVisual.Shape(Row("DAMAGE",new JObject{["hpLoss"]=5,["absorbed"]=0})));}
 [TestCase("mark","Mark")][TestCase("taunt","Taunt")][TestCase("train_damage_buff","TrainBuff")]
 public void StatusMustBeActuallyAccepted(string category,string expected){var row=Row("STATUS_ADD",new JObject{["category"]=category,["accepted"]=true});Assert.AreEqual(expected,CombatResolvedVisual.Shape(row));row["actualResult"]["accepted"]=false;Assert.IsNull(CombatResolvedVisual.Shape(row));}
 [Test] public void InterruptNeedsActualCancelledAction(){var row=Row("INTERRUPT",new JObject{["cancelledActionIds"]=new JArray()});Assert.IsNull(CombatResolvedVisual.Shape(row));((JArray)row["actualResult"]["cancelledActionIds"]).Add(1);Assert.AreEqual("Interrupt",CombatResolvedVisual.Shape(row));}
 [Test] public void RestoredPositiveRowsDoNotReplay(){var row=Row("HEAL",new JObject{["actualHpGain"]=20});var feedback=new CombatEffectFeedback();feedback.Seed(new JArray(row));Assert.IsFalse(feedback.Observe(row));var fresh=new CombatEffectFeedback();Assert.IsTrue(fresh.Observe(row));Assert.IsFalse(fresh.Observe(row));}
}
