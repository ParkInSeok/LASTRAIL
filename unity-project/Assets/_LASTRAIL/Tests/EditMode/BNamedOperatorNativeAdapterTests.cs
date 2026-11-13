#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class BNamedOperatorNativeAdapterTests {
  internal static JObject Profile()=>(JObject)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json"))["profile"];
  internal static CombatDefinitionIndex Definition()=>CombatDefinitionIntake.Read(JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/CommanderS1-20260922/c3-definition-intake-20261001.json")));
  [Test] public void VerifiedKyleInputAdvertisesItsActualBasicAction(){var b=BInterceptSourceTests.Binding(BInterceptSourceTests.Sample());Assert.Contains("op_kyle:BASIC",b.Composition.Attack.SupportedActions.ToArray());Assert.IsFalse(b.Composition.Attack.OperationallyExecutable);}
  [Test] public void NamedOwnedNativeAdapterMustExist(){Assert.NotNull(typeof(BDirectAttackBasicLoop).Assembly.GetType("LastRail.Battle.BNamedOperatorNativeAdapter"),"Missing immutable owned actor-to-native boundary");}
  internal static BNamedOperatorNativeAdapter Adapter(JObject profile=null,JObject packet=null){var p=BInterceptSourceTests.Sample();var b=BOrdinaryGeometryTests.Stress(p);profile=profile??Profile();packet=packet??b.Composition.Attack.Capture();return BNamedOperatorNativeAdapter.Read(packet,BDirectAttackInput.Digest(packet),profile,BDirectAttackInput.Digest(profile),Definition(),b.Composition.Attack.Formation,BInterceptSourceTests.Read(p));}
  internal static BDirectAttackBasicLoopBinding Binding(BNamedOperatorNativeAdapter adapter){var old=BOrdinaryGeometryTests.Stress(BInterceptSourceTests.Sample());var c=old.Composition.Capture();c["attackInputHash"]=adapter.Input.OriginalHash;var composition=new BDirectAttackBuffBinding(adapter.Input,BInterceptSourceTests.Defs(),c,BDirectAttackInput.Digest(c));var routes=old.Capture();routes["compositionHash"]=composition.OriginalHash;return new BDirectAttackBasicLoopBinding(composition,routes,BDirectAttackInput.Digest(routes));}
  [Test] public void ActualOwnedKyleStatsReplaceProbeWithoutMutatingAnyOriginal(){var profile=Profile();var old=BOrdinaryGeometryTests.Stress(BInterceptSourceTests.Sample());var packet=old.Composition.Attack.Capture();var saved=(JObject)packet.DeepClone();var a=Adapter(profile,packet);var entity=a.Input.Capture()["entities"].Single(e=>(string)e["id"]=="op_kyle");Assert.AreEqual(1999,(int)entity["maxHp"]);Assert.IsTrue(JToken.DeepEquals(profile["operators"].Single(e=>(string)e["operatorId"]=="op_kyle")["permanentStats"]["attack"],entity["attack"]));Assert.AreEqual(250,(long)entity["defense"]["numerator"]);Assert.AreEqual(1800000,(long)entity["basicIntervalMicroseconds"]);Assert.IsTrue(JToken.DeepEquals(saved,packet));profile["operators"][0]["permanentStats"]["maxHp"]=1;Assert.AreEqual(1999,(int)a.Input.Capture()["entities"].Single(e=>(string)e["id"]=="op_kyle")["maxHp"]);var caps=a.CaptureCapabilities();Assert.IsTrue((bool)caps["actors"][0]["basicAttackBound"]);Assert.IsTrue((bool)caps["actors"][0]["interceptionSourceBound"]);foreach(string key in new[]{"activeBound","ultimateBound","operatingMovementBound","effectiveStatProfileBound","fullActorSupported"})Assert.IsFalse((bool)caps["actors"][0][key]);}
  [TestCase("maxHp")][TestCase("attack")][TestCase("defense")][TestCase("basicIntervalMs")]
  public void MissingStatSourceIsNotDefaulted(string field){var p=Profile();((JObject)p["operators"].Single(e=>(string)e["operatorId"]=="op_kyle")["permanentStats"]).Remove(field);var e=Assert.Throws<ArgumentException>(()=>Adapter(p));StringAssert.Contains("b_named_actor_stat_source_missing",e.Message);}
  [TestCase("owner")][TestCase("negative")][TestCase("effect")][TestCase("step")][TestCase("shield")]
  public void InvalidSourceCannotCreateNativeOwner(string fault){var p=Profile();var packet=BOrdinaryGeometryTests.Stress(BInterceptSourceTests.Sample()).Composition.Attack.Capture();var row=p["operators"].Single(e=>(string)e["operatorId"]=="op_kyle");if(fault=="owner")row["statInputs"]["sourceOwner"]="foreign";if(fault=="negative")row["permanentStats"]["defense"]=-1;if(fault=="effect")((JArray)packet["entities"].Single(e=>(string)e["id"]=="op_kyle")["attackSources"]).Add(new JObject());if(fault=="step")packet["stepTimeSpanTicks"]=100000;if(fault=="shield")((JArray)packet["initialShields"]).Add(new JObject{["targetId"]="op_kyle",["sourceEntity"]="UNBOUND",["sourceAbility"]="UNBOUND",["effectIndex"]=0,["amount"]=1000000,["expiryTick"]=1000000});var original=packet.DeepClone();Assert.Throws<ArgumentException>(()=>Adapter(p,packet));Assert.IsTrue(JToken.DeepEquals(original,packet));}
  [Test] public void SableSourceAbsenceDoesNotAliasKyleOrInventNumbers(){var profile=Profile();var sample=BInterceptSourceTests.Sample(9);var receipt=BInterceptSourceTests.Read(sample);Assert.AreEqual("op_sable",receipt.OperatorId);var old=BInterceptSourceTests.Binding(BInterceptSourceTests.Sample());var formation=new BCombatFormation("train",BCombatMode.Solo,BCombatBattlefield.ExternalVertical,new[]{new BCombatSlot(old.Composition.Attack.Formation.Slots[0].CarriageId,"op_sable")});var packet=old.Composition.Attack.Capture();var e=Assert.Throws<ArgumentException>(()=>BNamedOperatorNativeAdapter.Read(packet,BDirectAttackInput.Digest(packet),profile,BDirectAttackInput.Digest(profile),Definition(),formation,receipt));StringAssert.Contains("b_named_actor_definition_missing:op_sable",e.Message);}
  [Test] public void OwnedKyleUsesOriginalHpPeriodBasicInterceptionAndReplay(){
   var a=Adapter();var b=Binding(a);var g=BOrdinaryGeometryTests.Geometry(b);var native=a.CreateRuntime(b,g,10000);
   Assert.AreEqual(1999,native.Hp("op_kyle"));
   for(int n=0;n<200&&native.Capture()["gunnerIntercept"]["projectiles"].Count()==0;n++)native.Advance("wait"+n,1000000,50);
   string shot=(string)native.Capture()["gunnerIntercept"]["projectiles"][0]["id"];
   native.AcceptInterceptionSource("original-time",shot,BInterceptSourceTests.Read(BInterceptSourceTests.Sample()));native.Advance("contact",2500000,125);
   Assert.AreEqual("INTERCEPTED",(string)native.Capture()["gunnerIntercept"]["projectiles"][0]["terminal"]);
   native.Advance("recovery",10000000,500);Assert.Less(native.Hp("op_kyle"),1999);
   var life=native.Capture()["boarding"]["lives"].First(l=>(bool)l["registered"]&&(bool)l["targetable"]);string id=(string)life["id"];int hp=native.Hp(id);
   native.ApplyWaveBasic("actual-owned-basic","op_kyle",id,(long)life["generation"]);
   Assert.That(hp-native.Hp(id),Is.GreaterThan(0).And.LessThanOrEqualTo(50));
   Assert.AreEqual(native.Tick+900,(long)native.Capture()["state"]["direct"]["next"]["op_kyle"]);
   var end=native.Capture();var restored=BDirectAttackBasicLoop.RestoreOrdinaryGeometryRuntime(end,BDirectAttackInput.Digest(end),b,BInterceptSourceTests.Read(BInterceptSourceTests.Sample()).OriginalSource,g,10000,true);
   Assert.IsTrue(JToken.DeepEquals(end,restored.Capture()));var before=native.Capture();
   Assert.Throws<ArgumentException>(()=>a.CreateRuntime(BOrdinaryGeometryTests.Stress(BInterceptSourceTests.Sample()),g,10000));Assert.IsTrue(JToken.DeepEquals(before,native.Capture()));
   string folder="D:/Project/LASTRAIL/QA/CombatBMigration-20261004/NamedOperatorAdapter";Directory.CreateDirectory(folder);
   File.WriteAllText(folder+"/native-owned-kyle-boundary.json",end.ToString());File.WriteAllText(folder+"/capabilities.json",a.CaptureCapabilities().ToString());
  }
  [Test] public void OldAcceptedReceiptAfterNewBasicIsRejectedAtomically(){var a=Adapter();var b=Binding(a);var g=BOrdinaryGeometryTests.Geometry(b);var native=a.CreateRuntime(b,g,10000);native.Advance("roof",40000000,2000);var life=native.Capture()["boarding"]["lives"].First(l=>(bool)l["registered"]&&(bool)l["targetable"]);native.ApplyWaveBasic("new-basic","op_kyle",(string)life["id"],(long)life["generation"]);for(int n=0;n<100&&native.Capture()["gunnerIntercept"]["projectiles"].Count()==0;n++)native.Advance("wait"+n,1000000,50);string shot=(string)native.Capture()["gunnerIntercept"]["projectiles"][0]["id"];var before=native.Capture();Assert.Throws<ArgumentException>(()=>native.AcceptInterceptionSource("old-prefix",shot,BInterceptSourceTests.Read(BInterceptSourceTests.Sample())));Assert.IsTrue(JToken.DeepEquals(before,native.Capture()));}
 }
}
#endif