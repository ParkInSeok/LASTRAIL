using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public class ApprovedResultAudioTests {
 [Test] public void ApprovedResourceDependencyClosureContainsAllSixOriginals() {
  var set=Resources.Load<ApprovedResultAudio>("CombatPresentation/EffectAudio/ApprovedResults");
  var paths=UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(set),true);
  foreach(var clip in new[]{set.Heal,set.Buff,set.Debuff,set.ShieldContact,set.BasicContact,set.Warning})CollectionAssert.Contains(paths,UnityEditor.AssetDatabase.GetAssetPath(clip));
 }
 static JObject Row(string ability,int index,string kind,JObject actual)=>new JObject{["abilityId"]=ability,["effectIndex"]=index,["kind"]=kind,["actualResult"]=actual};
 [TestCase("op_eve.active",0,"HEAL","", "heal-new",.451568f)]
 [TestCase("op_mira.ultimate",1,"STATUS_ADD","train_damage_buff","buff-new",.283303f)]
 [TestCase("op_sera.active",2,"STATUS_REFRESH","mark","debuff-new",.180999f)]
 [TestCase("op_kyle.active",1,"STATUS_ADD","taunt","debuff-new",.65f)]
 public void ExactResultsUseApprovedOriginalAndKeepExistingGain(string ability,int index,string kind,string category,string name,float expectedGain) {
  var row=Row(ability,index,kind,new JObject{["actualHpGain"]=2,["accepted"]=true,["category"]=category});
  Assert.IsTrue(CombatEffectAudio.LoadResources().Find(row,out var clip,out var gain));Assert.AreEqual(name,clip.name);Assert.AreEqual(expectedGain,gain);
 }
 [Test] public void ShieldContactRequiresActualAbsorptionAndNeverFormation() {
  var audio=CombatEffectAudio.LoadResources();var row=Row("op_reina.active",0,"DAMAGE",new JObject{["hpLoss"]=0,["absorbed"]=2});
  Assert.IsTrue(audio.Find(row,out var clip,out var gain));Assert.AreEqual("shield-warning-local.v02.shield.20261021",clip.name);Assert.AreEqual(.297596f,gain);
  row["actualResult"]["absorbed"]=0;Assert.IsFalse(audio.Find(row,out _,out _));
  row=Row("op_kyle.active",0,"SHIELD_ISSUE",new JObject{["addedAmount"]=3});Assert.IsTrue(audio.Find(row,out clip,out _));Assert.AreEqual("op_kyle.active-0",clip.name);
 }
 [TestCase("no-absorption")][TestCase("missing-receipt")][TestCase("foreign-target")][TestCase("missing-attempt")][TestCase("unrelated-source")]
 public void NativeContactRequiresMatchingAcceptedReceipt(string change){
  var row=Row("native-enemy-basic",0,"DAMAGE",new JObject{["hpLoss"]=3,["absorbed"]=2});
  row["sourceKind"]="ENEMY_BASIC";row["targetId"]="ally";row["nodeId"]="node";row["attemptGeneration"]=1;row["healthReceipt"]=new JObject{["targetId"]="ally",["absorbed"]=2};
  var audio=CombatEffectAudio.LoadResources();Assert.IsTrue(audio.Find(row,out _,out _));
  switch(change){case "no-absorption":row["actualResult"]["absorbed"]=0;row["healthReceipt"]["absorbed"]=0;break;case "missing-receipt":row.Remove("healthReceipt");break;case "foreign-target":row["healthReceipt"]["targetId"]="other";break;case "missing-attempt":row.Remove("attemptGeneration");break;case "unrelated-source":row["sourceKind"]="SKILL";break;}
  Assert.IsFalse(audio.Find(row,out _,out _));
 }
 static JObject NativeBasic(string actor="op_reina")=>new JObject{["abilityId"]=actor+".basic",["sourceId"]=actor,["sourceKind"]="BASIC",["effectIndex"]=0,["kind"]="DAMAGE",["nodeId"]="node",["attemptGeneration"]=1,["targetId"]="enemy",["actualResult"]=new JObject{["hpLoss"]=3,["absorbed"]=0},["healthReceipt"]=new JObject{["targetId"]="enemy",["hpLoss"]=3,["absorbed"]=0}};
 [TestCase("op_kyle")][TestCase("op_reina")][TestCase("op_eve")][TestCase("op_sera")][TestCase("op_mira")]
 public void BasicDamageUsesContactButAbsorptionSelectsOnlyShield(string actor){
  var row=NativeBasic(actor);var audio=CombatEffectAudio.LoadResources();Assert.IsTrue(audio.Find(row,out var clip,out var gain));Assert.AreEqual("hit-local.v03.C.20261021",clip.name);Assert.AreEqual(.85f,gain);
  row["healthReceipt"]["absorbed"]=row["actualResult"]["absorbed"]=1;Assert.IsTrue(audio.Find(row,out clip,out _));Assert.AreEqual("shield-warning-local.v02.shield.20261021",clip.name);
 }
 [TestCase("miss")][TestCase("enemy")][TestCase("skill")][TestCase("unknown")][TestCase("foreign-target")][TestCase("mismatch")][TestCase("no-attempt")][TestCase("missing-receipt")]
 public void BasicContactDoesNotTurnUnrelatedOrUnconfirmedResultsIntoHits(string change){
  var row=NativeBasic();switch(change){case "miss":row["healthReceipt"]["hpLoss"]=row["actualResult"]["hpLoss"]=0;break;case "enemy":row["sourceKind"]="ENEMY_BASIC";break;case "skill":row["abilityId"]="unmapped.skill";break;case "unknown":row=NativeBasic("unmapped_actor");break;case "foreign-target":row["healthReceipt"]["targetId"]="other";break;case "mismatch":row["healthReceipt"]["hpLoss"]=2;break;case "no-attempt":row.Remove("attemptGeneration");break;case "missing-receipt":row.Remove("healthReceipt");break;}
  Assert.IsFalse(CombatEffectAudio.LoadResources().Find(row,out _,out _));
 }
 [Test] public void UnmappedEffectsAndRepairDoNotAcquireGenericHealingOrContactSounds() {
  var audio=CombatEffectAudio.LoadResources();Assert.IsFalse(audio.Find(Row("unknown",0,"HEAL",new JObject{["actualHpGain"]=3}),out _,out _));
  Assert.IsTrue(audio.Find(Row("emergency_repair",0,"REPAIR",new JObject{["actualHpGain"]=3}),out var clip,out _));Assert.AreEqual("emergency_repair-0",clip.name);
 }
}
