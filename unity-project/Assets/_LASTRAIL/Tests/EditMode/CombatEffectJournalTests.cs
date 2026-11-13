using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatEffectJournalTests {
 [TestCase("Observe","actualResult")][TestCase("Observe","actualHpGain")][TestCase("Seed","actualResult")][TestCase("ReserveAudio","actualResult")]
 public void MissingEffectResultDoesNotConsumeLaterCompleteEvent(string operation,string missing){
  var model=Make("op_eve");var complete=(JObject)Cast(model,"op_eve","op_eve.active").Single();var partial=(JObject)complete.DeepClone();
  if(missing=="actualResult")partial.Remove(missing);else ((JObject)partial["actualResult"]).Remove(missing);
  var cursor=new CombatEffectFeedback();Assert.Throws<ArgumentException>(()=>{if(operation=="Observe")cursor.Observe(partial);else if(operation=="Seed")cursor.Seed(new JArray(partial));else cursor.ReserveAudio(partial);});
  Assert.That(cursor.Observe(complete),Is.True);Assert.That(cursor.ReserveAudio(complete),Is.True);Assert.That(cursor.Observe(complete),Is.False);
 }
 [Test] public void ExplicitZeroResultIsValidWhilePartialRestoreBatchKeepsExistingState(){
  var model=Make("op_eve");var original=(JObject)Cast(model,"op_eve","op_eve.active").Single();var cursor=new CombatEffectFeedback();cursor.Seed(new JArray(original));
  var zero=(JObject)original.DeepClone();zero["id"]="zero-event";zero["commandId"]="zero-command";zero["actualResult"]["actualHpGain"]=0;
  var incomplete=(JObject)original.DeepClone();incomplete["id"]="partial-event";incomplete.Remove("actualResult");
  Assert.Throws<ArgumentException>(()=>cursor.Seed(new JArray(zero,incomplete)));
  var positive=(JObject)zero.DeepClone();positive["actualResult"]["actualHpGain"]=1;Assert.That(cursor.Observe(positive),Is.True);Assert.That(cursor.Observe(original),Is.False);
  zero["id"]="explicit-zero";Assert.That(cursor.Observe(zero),Is.False);Assert.That(cursor.ReserveAudio(zero),Is.False);Assert.That(cursor.Observe(zero),Is.False);
 }
 [TestCase("Observe")][TestCase("ReserveAudio")][TestCase("Seed")]
 public void PresentationRejectsChangedPayloadForSameOriginalEvent(string operation){
  var s=Make("op_eve");var row=(JObject)Cast(s,"op_eve","op_eve.active").Single();var cursor=new CombatEffectFeedback();Assert.That(cursor.Observe(row),Is.True);
  var conflicting=(JObject)row.DeepClone();conflicting["actualResult"]["actualHpGain"]=999;
  Assert.Throws<InvalidOperationException>(()=>{if(operation=="Observe")cursor.Observe(conflicting);else if(operation=="Seed")cursor.Seed(new JArray(conflicting));else cursor.ReserveAudio(conflicting);});
  Assert.That(cursor.Observe(row),Is.False);Assert.That(cursor.ReserveAudio(row),Is.True);
 }
 [Test]public void RestoreSeedConflictDoesNotConsumeUnrelatedNewEvent(){
  var s=Make("op_eve");var row=(JObject)Cast(s,"op_eve","op_eve.active").Single();var cursor=new CombatEffectFeedback();cursor.Seed(new JArray(row));
  var next=(JObject)row.DeepClone();next["id"]="next-event";next["commandId"]="next-command";next["globalSequence"]=999;
  var conflict=(JObject)row.DeepClone();conflict["actualResult"]["actualHpGain"]=999;
  Assert.Throws<InvalidOperationException>(()=>cursor.Seed(new JArray(next,conflict)));
  Assert.That(cursor.Observe(next),Is.True);Assert.That(cursor.ReserveAudio(next),Is.True);
  var reordered=new JObject(row.Properties().Reverse().Select(p=>new JProperty(p.Name,p.Value.DeepClone())));Assert.That(cursor.Observe(reordered),Is.False);
 }
 [Test]public void ConfirmedDamageAdapterReusesOriginalSourceAndDoesNotReplayHp(){
  var s=Make("op_reina");var row=Cast(s,"op_reina","op_reina.active").Single();var before=s.CaptureLocalState();
  var method=typeof(BasicCombatSimulation).GetMethod("ReadConfirmedSkillDamage");Assert.IsNotNull(method);
  var receipt=method.Invoke(s,new object[]{(string)row["id"]});object Read(string key)=>receipt.GetType().GetProperty(key).GetValue(receipt);
  Assert.AreEqual("op_reina",Read("OperatorId"));Assert.AreEqual((string)row["id"],Read("EventId"));Assert.AreEqual("typed",Read("ParentCommandId"));Assert.AreEqual((string)row["runId"],Read("RunId"));
  Assert.AreEqual(240,((CombatDamageResult)Read("Damage")).HpLoss);Assert.AreEqual(before,s.CaptureLocalState());
  var restored=BasicCombatSimulation.RestoreLocalState(before,()=>Make("op_reina",false));var replay=method.Invoke(restored,new object[]{(string)row["id"]});Assert.AreEqual(Read("EventId"),replay.GetType().GetProperty("EventId").GetValue(replay));Assert.AreEqual(before,restored.CaptureLocalState());
  Assert.Throws<TargetInvocationException>(()=>method.Invoke(s,new object[]{"unknown"}));
 }
 [Test]public void HealJournalCannotMasqueradeAsAttributedDamage(){var s=Make("op_eve");var row=Cast(s,"op_eve","op_eve.active").Single();var method=typeof(BasicCombatSimulation).GetMethod("ReadConfirmedSkillDamage");Assert.IsNotNull(method);Assert.Throws<TargetInvocationException>(()=>method.Invoke(s,new object[]{(string)row["id"]}));}
 [Test]public void SharedTrainDamageDoesNotInventOperatorBeneficiary(){var s=Make("op_reina");var row=Cast(s,"train","support_fire").Single();var receipt=s.ReadConfirmedSkillDamage((string)row["id"]);Assert.AreEqual("train",receipt.SourceId);Assert.IsNull(receipt.OperatorId);Assert.AreEqual((int)row["actualResult"]["hpLoss"],receipt.Damage.HpLoss);}
 static BasicCombatSimulation Make(string leader,bool fixture=true)=>(BasicCombatSimulation)typeof(Ce03EffectValueTests).GetMethod(fixture?"Fixture":"Make",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{leader});
 static JArray Cast(BasicCombatSimulation s,string source,string ability,string command="typed"){
  Assert.AreEqual("QUEUED",s.RequestSkill(command,source,ability));s.Advance(12500000,25);Assert.AreEqual("APPLIED",s.CommandStatus(command));var rows=s.ReadHudState()["effectResults"] as JArray;Assert.NotNull(rows,"Commit must expose typed results");return rows;
 }
 [Test] public void ActualHealAndZeroGainHaveDifferentReceipts(){
  var s=Make("op_eve");var row=Cast(s,"op_eve","op_eve.active").Single();Assert.AreEqual("HEAL",(string)row["kind"]);Assert.AreEqual(385,(int)row["actualResult"]["actualHpGain"]);
  var raw=JObject.Parse(Make("op_eve").CaptureLocalState());foreach(var a in (JArray)raw["actors"])if(!new[]{"inside","outside","train"}.Contains((string)a["id"]))a["vitals"]["hp"]=1000;
  s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make("op_eve",false));row=Cast(s,"op_eve","op_eve.active").Single();Assert.AreEqual(0,(int)row["actualResult"]["actualHpGain"]);
 }
 [Test] public void DamageAndRepairRetainActualKindsAndAmounts(){
  var row=Cast(Make("op_reina"),"op_reina","op_reina.active").Single();Assert.AreEqual("DAMAGE",(string)row["kind"]);Assert.AreEqual(240,(int)row["actualResult"]["hpLoss"]);Assert.AreEqual(0,(int)row["actualResult"]["absorbed"]);
  row=Cast(Make("op_mira"),"op_mira","op_mira.active").Single();Assert.AreEqual("REPAIR",(string)row["kind"]);Assert.AreEqual(275,(int)row["actualResult"]["actualHpGain"]);
 }
 [Test] public void ShieldRefreshDoesNotClaimRemainingAsAdded(){
  var s=Make("op_kyle");var rows=Cast(s,"op_kyle","op_kyle.active");var shield=rows.Single(r=>(string)r["kind"]=="SHIELD_ISSUE");Assert.AreEqual(198,(int)shield["actualResult"]["addedAmount"]);
  var raw=JObject.Parse(s.CaptureLocalState());foreach(var ready in (JArray)raw["skillReady"])ready["ready"]=0;foreach(var a in (JArray)raw["actors"]){a["recoveryUntil"]=0;a["ready"]=100000;}((JArray)raw["attacks"]).Clear();
  s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make("op_kyle",false));rows=Cast(s,"op_kyle","op_kyle.active","typed2");shield=rows.Last(r=>(string)r["kind"]=="SHIELD_ISSUE");Assert.AreEqual(0,(int)shield["actualResult"]["addedAmount"]);Assert.AreEqual("REFRESH",(string)shield["actualResult"]["outcome"]);
 }
 [Test] public void JournalRestoresExactlyAndRejectsDuplicateIdentity(){
  var s=Make("op_sera");var rows=Cast(s,"op_sera","op_sera.active");Assert.AreEqual(3,rows.Count);var raw=s.CaptureLocalState();Assert.AreEqual(raw,BasicCombatSimulation.RestoreLocalState(raw,()=>Make("op_sera",false)).CaptureLocalState());
  var altered=JObject.Parse(raw);var journal=(JArray)altered["effectJournal"]["rows"];journal.Add(journal[0].DeepClone());Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(altered.ToString(),()=>Make("op_sera",false)));
 }
 [Test] public void ProtectedAndAbsorbedDamageDoNotBecomeHpLoss(){
  foreach(bool protect in new[]{false,true}){
   var raw=JObject.Parse(Make("op_reina").CaptureLocalState());var enemy=((JArray)raw["actors"]).Single(a=>(string)a["id"]=="inside");
   if(protect)enemy["protectedUntil"]=100;
   else ((JArray)enemy["vitals"]["layers"]).Add(new JObject{["entityId"]="op_kyle",["abilityId"]="op_kyle.active",["effectIndex"]=0,["remaining"]=1000,["expiry"]=100});
   var s=BasicCombatSimulation.RestoreLocalState(raw.ToString(),()=>Make("op_reina",false));var row=(JObject)Cast(s,"op_reina","op_reina.active").Single();Assert.AreEqual(0,(int)row["actualResult"]["hpLoss"]);Assert.AreEqual(protect?0:240,(int)row["actualResult"]["absorbed"]);Assert.AreEqual(!protect,CombatEffectFeedback.Positive(row));
  }
 }
 [Test] public void AudioGroupsWaitForPositiveTargetAndDoNotReplay(){
  var s=Make("op_eve");var row=(JObject)Cast(s,"op_eve","op_eve.active").Single();var zero=(JObject)row.DeepClone();zero["id"]="zero-target";zero["targetId"]="other";zero["actualResult"]["actualHpGain"]=0;
  var cursor=new CombatEffectFeedback();Assert.IsFalse(cursor.Observe(zero));Assert.IsTrue(cursor.Observe(row));Assert.IsTrue(cursor.ReserveAudio(row));Assert.IsFalse(cursor.Observe(row));
  var another=(JObject)row.DeepClone();another["id"]="positive-other";another["targetId"]="other-positive";Assert.IsTrue(cursor.Observe(another));Assert.IsFalse(cursor.ReserveAudio(another));
  cursor=new CombatEffectFeedback();cursor.Seed(new JArray(row));Assert.IsFalse(cursor.Observe(row));Assert.IsFalse(cursor.ReserveAudio(another));
 }
}
