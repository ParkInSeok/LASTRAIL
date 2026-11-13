using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
public sealed class CombatFrontlinePolicyTests {
 const string Root="D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/";
 static JObject NewInput()=>JObject.Parse(File.ReadAllText(Root+"ce03-frontline-policy-UtU7yJ/prepared.json"));
 static JObject OldInput()=>JObject.Parse(File.ReadAllText(Root+"ce03-local-producer/draft.json"));
 static BasicCombatSimulation Make(JObject p)=>BasicCombatSimulation.FromPreparedProfileForLocalTest(p,(JObject)p["ownedSelection"],(string)p["ownedSelection"]["accountId"],1);
 static BasicCombatSimulation From(JObject state,JObject p)=>BasicCombatSimulation.RestoreLocalState(state.ToString(),()=>Make(p));
 static JToken Actor(JObject state,string id)=>((JArray)state["actors"]).Single(a=>(string)a["id"]==id);
 static JObject Quiet(JObject p){var j=JObject.Parse(Make(p).CaptureLocalState());foreach(var a in (JArray)j["actors"])a["ready"]=10000;return j;}
 // Narrow headless fixture: the existing rear_ally definition occupies one scout
 // slot. Original prepared JSON and normal-contract-path test remain untouched.
 static JObject WithGunner(JObject p){p["draft"]["template"]["waves"][0]["spawns"][1]["definitionId"]="enemy_spike_cart_gunner";return p;}
 static string Gunner(JObject p)=>"wave.1.interior.1";
 static string Target(BasicCombatSimulation s,string enemy)=>(string)((JArray)s.ReadHudState()["actors"]).Single(a=>(string)a["id"]==enemy)["attackTarget"];
 [Test] public void NormalPreparedRouteOptsInWithoutChangingLegacy(){
  var p=NewInput();var contract=new Ce03CommandContract((JObject)p["commandContract"]);var s=contract.CreateLocalTest(p,(string)p["ownedSelection"]["accountId"],contract.Revision,1);
  Assert.AreEqual("ce03-headless-frontline-state-v1",(string)JObject.Parse(s.CaptureLocalState())["schema"]);
  Assert.AreEqual("ce03.frontline-first-v1",(string)JObject.Parse(s.CaptureLocalState())["targetingPolicyId"]);
  foreach(bool modern in new[]{false,true}){p=WithGunner(modern?NewInput():OldInput());var j=Quiet(p);var id=Gunner(p);Actor(j,id)["ready"]=12;s=From(j,p);s.Advance(12*500000L,12);Assert.AreEqual(modern?"op_sera":"op_kyle",Target(s,id));}
 }
 [Test] public void OldAndNewCheckpointBoundariesRejectTampering(){
  var old=OldInput();var modern=NewInput();var a=Make(old);a.Advance(70000000,140);var oldBytes=a.CaptureLocalState();Assert.AreEqual(oldBytes,BasicCombatSimulation.RestoreLocalState(oldBytes,()=>Make(old)).CaptureLocalState());
  var b=Make(modern);b.Advance(70000000,140);var newBytes=b.CaptureLocalState();Assert.AreEqual(newBytes,BasicCombatSimulation.RestoreLocalState(newBytes,()=>Make(modern)).CaptureLocalState());
  Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(oldBytes,()=>Make(modern)));Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(newBytes,()=>Make(old)));
  var j=JObject.Parse(newBytes);j["targetingPolicyId"]="legacy";Assert.Throws<ArgumentException>(()=>From(j,modern));
 }
 [Test] public void MixedPolicyFieldsCannotOptIn(){
  var p=NewInput();((JObject)p["draft"]["template"]["execution"]).Remove("targetingPolicyId");Assert.Throws<ArgumentException>(()=>Make(p));
  p=OldInput();p["draft"]["template"]["execution"]["targetingPolicyId"]="ce03.frontline-first-v1";Assert.Throws<ArgumentException>(()=>Make(p));
  p=NewInput();p["draft"]["template"]["execution"]["targetingPolicyId"]="unknown";Assert.Throws<ArgumentException>(()=>Make(p));
 }
 [Test] public void RearReservationSurvivesFrontRecoveryThenNextSelectionReturnsFront(){
  var p=WithGunner(NewInput());var j=Quiet(p);var id=Gunner(p);Actor(j,id)["ready"]=12;
  foreach(var name in new[]{"op_sera","op_mira"}){var a=Actor(j,name);a["vitals"]["hp"]=0;a["downCount"]=1;a["recoverAt"]=13;}
  var s=From(j,p);s.Advance(12*500000L,12);Assert.AreEqual("op_kyle",Target(s,id));s.Advance(500000L,1);Assert.Greater(s.GetHp("op_sera"),0);Assert.AreEqual("op_kyle",Target(s,id));
  for(int n=0;n<100&&s.Events.All(e=>e.Source!=id);n++)s.Advance(500000L,1);Assert.AreEqual("op_kyle",s.Events.Single(e=>e.Source==id).Target);
  for(int n=0;n<100&&Target(s,id)==null;n++)s.Advance(500000L,1);Assert.AreEqual("op_sera",Target(s,id));
 }
 [Test] public void DeadReservationRetargetsWithFullWindupAndNoSecondCooldown(){
  var p=WithGunner(NewInput());var j=Quiet(p);var id=Gunner(p);Actor(j,id)["ready"]=12;var s=From(j,p);s.Advance(12*500000L,12);j=JObject.Parse(s.CaptureLocalState());var attack=((JArray)j["attacks"]).Single(a=>(string)a["source"]==id);var due=(long)attack["due"];long ready=(long)Actor(j,id)["ready"];
  Actor(j,"op_sera")["vitals"]["hp"]=0;Actor(j,"op_sera")["downCount"]=1;Actor(j,"op_sera")["recoverAt"]=999;s=From(j,p);s.Advance((due-s.Tick)*500000L,(int)(due-s.Tick));j=JObject.Parse(s.CaptureLocalState());attack=((JArray)j["attacks"]).Single(a=>(string)a["source"]==id);
  Assert.AreEqual("op_mira",(string)attack["target"]);Assert.AreEqual(due+(due-12),(long)attack["due"]);Assert.AreEqual(ready,(long)Actor(j,id)["ready"]);Assert.IsEmpty(s.Events);
 }
 [Test] public void ActualKyleTauntStillOverridesFrontAndExpires(){
  var p=WithGunner(NewInput());var j=Quiet(p);var id=Gunner(p);var s=From(j,p);s.Advance(12*500000L,12);s.RequestSkill("taunt","op_kyle","op_kyle.active");
  for(int n=0;n<60&&s.CommandStatus("taunt")!="APPLIED";n++)s.Advance(500000L,1);Assert.AreEqual("APPLIED",s.CommandStatus("taunt"));j=JObject.Parse(s.CaptureLocalState());Assert.IsTrue(((JArray)j["statuses"]).Any(x=>(string)x["type"]=="taunt"&&(string)x["target"]==id));Actor(j,id)["ready"]=s.Tick+1;s=From(j,p);s.Advance(500000L,1);Assert.AreEqual("op_kyle",Target(s,id));
  j=JObject.Parse(s.CaptureLocalState());var expiry=((JArray)j["statuses"]).Where(x=>(string)x["type"]=="taunt").Max(x=>(long)x["expiry"]);s.Advance((expiry-s.Tick+150)*500000L,(int)(expiry-s.Tick+150));Assert.IsTrue(s.Events.Any(e=>e.Source==id&&e.Tick>expiry&&(e.Target=="op_sera"||e.Target=="op_mira")));
 }
 [Test] public void OfficialSlotLayoutFollowsReorderedFormationNotNames(){
  var p=NewInput();foreach(var path in new[]{p["ownedSelection"]["formation"]["operators"],p["draft"]["template"]["operators"]})foreach(var a in (JArray)path){if((string)a["operatorId"]=="op_kyle")a["slot"]=3;if((string)a["operatorId"]=="op_sera")a["slot"]=0;}
  var s=Make(p);var pos=CombatFormationLayout.Positions(s.ReadHudState());Assert.AreEqual(new UnityEngine.Vector2(242,498),pos["op_kyle"]);Assert.AreEqual(new UnityEngine.Vector2(8,485),pos["op_sera"]);
  var j=Quiet(p);Actor(j,"wave.1.interior.0")["ready"]=12;s=From(j,p);s.Advance(6000000,12);Assert.AreEqual("op_kyle",Target(s,"wave.1.interior.0"));
 }
 [Test] public void UnavailableFrontFallsBackAndSameRowCursorAlternates(){
  var p=NewInput();var j=Quiet(p);const string id="wave.1.interior.0";Actor(j,id)["ready"]=12;
  Actor(j,"op_sera")["availableAt"]=999;Actor(j,"op_mira")["availableAt"]=999;var s=From(j,p);s.Advance(6000000,12);Assert.AreEqual("op_kyle",Target(s,id));
  j=Quiet(p);Actor(j,id)["ready"]=12;s=From(j,p);s.Advance(120*500000L,120);CollectionAssert.AreEqual(new[]{"op_sera","op_mira","op_sera","op_mira"},s.Events.Where(e=>e.Source==id).Take(4).Select(e=>e.Target));
 }
 [Test] public void PreviouslyFrozenLegacyCaptureRestoresByteForByte(){
  var p=OldInput();p["ownedSelection"]["accountId"]="presentation-local-qa";BasicCombatSimulation Fresh(){var s=Make(p);s.ConfigureLocalLeaderCharge(4);return s;}
  var raw=File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/ScreenIntegration/combat-polish-final-v02-20260913/state-0.json");Assert.AreEqual(raw,BasicCombatSimulation.RestoreLocalState(raw,Fresh).CaptureLocalState());
 }
 [Test] public void RetainedOriginalStartStillConstructsLegacyWithoutChangingReceipt(){
  var raw=File.ReadAllText(Root+"ce03-local-host/session-EoOAuc/original-start.json");var json=JObject.Parse(raw);var original=new Ce03OriginalStart(raw,(string)json["owner"],new Uri((string)json["origin"]+"/"));
  var method=typeof(Ce03OriginalStart).GetMethod("CreateModel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var model=(BasicCombatSimulation)method.Invoke(original,null);var state=JObject.Parse(model.CaptureLocalState());Assert.AreEqual("ce03-headless-state-v1",(string)state["schema"]);Assert.IsNull(state["targetingPolicyId"]);Assert.AreEqual(Ce03CommandContract.Canonical(json),original.Json);Assert.AreEqual(model.CaptureLocalState(),((BasicCombatSimulation)method.Invoke(original,null)).CaptureLocalState());
 }
}
}
