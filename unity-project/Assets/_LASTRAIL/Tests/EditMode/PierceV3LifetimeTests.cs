#if UNITY_EDITOR
using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class PierceV3LifetimeTests {
  const string V="friendly-projectile-lifecycle.v2.20261016";
  static JObject Number(int n)=>new JObject{["numerator"]=n.ToString(),["denominator"]="1"};
  static JObject Defense()=>new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=Number(0),["effects"]=new JArray(),["weakening"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
  static JObject Slot(string id,int rank,int bps)=>new JObject{["contactId"]=id,["targetId"]=id,["targetGeneration"]=1,["ordinal"]=rank,["coefficientBps"]=bps};
  static JObject Launch(string version)=>new JObject{["version"]=version,["opportunityId"]="o",["sourceId"]="fixture",["sourceGeneration"]=1,["tick"]=0,["sequence"]=1,["attack"]=new JObject{["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=Number(100),["appliedOffense"]=new JArray(),["penetration"]=new JArray()},["geometry"]=new JObject{["version"]="TEST_ONLY",["revision"]=1,["hash"]="fixture"},["branches"]=new JArray(new JObject{["branchId"]="main",["pathId"]="ray",["kind"]="MAIN",["contacts"]=new JArray(Slot("a",0,10000),Slot("b",1,6000),Slot("c",2,3000))})};
  static JObject Contact(string id,long sequence,string version=V)=>new JObject{["version"]=version,["opportunityId"]="o",["branchId"]="main",["contactId"]=id,["tick"]=1,["sequence"]=sequence,["targetGeneration"]=1,["eligible"]=true,["arrival"]=Defense()};
  static FriendlyProjectileOwner New(){var ctor=typeof(FriendlyProjectileOwner).GetConstructor(new[]{typeof(string),typeof(int),typeof(string)});Assert.That(ctor,Is.Not.Null,"Explicit v2 owner construction missing");return (FriendlyProjectileOwner)ctor.Invoke(new object[]{"fixture",128,V});}
  [Test] public void ActualContactCanOvertakeReservationWithoutExchangingCoefficients(){var o=New();o.Launch(Launch(V),true);o.AdvanceTo(1);Assert.That(o.ResolveContact(Contact("c",2)).Damage,Is.EqualTo(30));Assert.That(o.ResolveContact(Contact("a",3)).Damage,Is.EqualTo(100));Assert.That(o.ResolveContact(Contact("b",4)).Damage,Is.EqualTo(60));var saved=JObject.Parse(o.Capture().ToString());var restored=FriendlyProjectileOwner.Restore(saved,"fixture",FriendlyProjectileOwner.Fingerprint(saved));Assert.That(restored.ResolveContact(Contact("c",2)).NewlyResolved,Is.False);Assert.That(JToken.DeepEquals(saved,restored.Capture()),Is.True);}
  [Test] public void V1OriginalOrderingAndReplayRemainStrict(){var o=new FriendlyProjectileOwner("fixture",128);o.Launch(Launch(FriendlyProjectileOwner.Version),true);o.AdvanceTo(1);Assert.Throws<InvalidOperationException>(()=>o.ResolveContact(Contact("c",2,FriendlyProjectileOwner.Version)));Assert.That((string)o.Capture()["version"],Is.EqualTo(FriendlyProjectileOwner.Version));}
 }
}
#endif