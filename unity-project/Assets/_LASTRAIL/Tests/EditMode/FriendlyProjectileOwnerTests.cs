#if UNITY_EDITOR
using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    // All actor, geometry, tick and damage values are explicit fixtures, not operating profiles.
    public sealed class FriendlyProjectileOwnerTests
    {
        static JObject V(string n)=>new JObject { ["numerator"]=n,["denominator"]="1" };
        static JObject Attack()=>new JObject {
            ["version"]=CombatProjectileAttackSnapshot.Version,["completedAttack"]=V("100"),
            ["appliedOffense"]=new JArray(),["penetration"]=new JArray() };
        static JObject Defense(string n="0")=>new JObject {
            ["version"]=CombatProjectileAttackSnapshot.Version,["defense"]=V(n),["effects"]=new JArray(),
            ["weakening"]=new JArray(),["reductions"]=new JObject {
                ["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray() }};
        static JObject Reservation(string id,string target,int ordinal,int bps)=>new JObject {
            ["contactId"]=id,["targetId"]=target,["targetGeneration"]=1,["ordinal"]=ordinal,["coefficientBps"]=bps };
        static JObject LaunchPacket(bool pierce=false,bool secondary=false,string id="o",long sequence=1,long tick=0) {
            var contacts=new JArray(Reservation("a","a",0,10000));
            if(pierce){contacts.Add(Reservation("b","b",1,6000));contacts.Add(Reservation("c","c",2,3000));}
            var branches=new JArray(new JObject {
                ["branchId"]="main",["pathId"]="fixed-test-ray",["kind"]="MAIN",["contacts"]=contacts });
            if(secondary)branches.Add(new JObject {
                ["branchId"]="aux",["pathId"]="fixed-test-aux",["kind"]="SECONDARY",
                ["contacts"]=new JArray(Reservation("aux","d",0,8500)) });
            return new JObject {
                ["version"]="friendly-projectile-lifecycle.v1.20261015",["opportunityId"]=id,
                ["sourceId"]="fixture-actor",["sourceGeneration"]=7,["tick"]=tick,["sequence"]=sequence,
                ["attack"]=Attack(),["geometry"]=new JObject { ["version"]="TEST_ONLY",["revision"]=2,["hash"]="test-geometry-pin" },
                ["branches"]=branches };
        }
        static JObject Contact(string id="a",string branch="main",long tick=1,long sequence=2,
            long generation=1,bool eligible=true,string defense="0",string opportunity="o")=>new JObject {
                ["version"]="friendly-projectile-lifecycle.v1.20261015",["opportunityId"]=opportunity,
                ["branchId"]=branch,["contactId"]=id,["tick"]=tick,["sequence"]=sequence,
                ["targetGeneration"]=generation,["eligible"]=eligible,
                ["arrival"]=eligible?(JToken)Defense(defense):JValue.CreateNull() };
        static Type OwnerType=>typeof(CombatVitals).Assembly.GetType("LastRail.Battle.FriendlyProjectileOwner");
        static object New(int capacity=128) {
            Assert.That(OwnerType,Is.Not.Null,"Friendly projectile lifetime owner is missing.");
            return Activator.CreateInstance(OwnerType,new object[]{"test-combat",capacity});
        }
        static object Invoke(object target,string name,params object[] args) {
            try{return target.GetType().GetMethod(name).Invoke(target,args);}
            catch(TargetInvocationException e){throw e.InnerException;}
        }
        static object Static(string name,params object[] args) {
            try{return OwnerType.GetMethod(name).Invoke(null,args);}
            catch(TargetInvocationException e){throw e.InnerException;}
        }
        static JObject State(object owner)=>(JObject)Invoke(owner,"Capture");
        static object Hit(object owner,JObject p)=>Invoke(owner,"ResolveContact",p);
        static int Amount(object application)=>(int)application.GetType().GetProperty("Damage").GetValue(application);
        static bool Fresh(object application)=>(bool)application.GetType().GetProperty("NewlyResolved").GetValue(application);
        static object Restore(object owner) {
            var snap=State(owner);return Static("Restore",snap,"test-combat",Static("Fingerprint",snap));
        }
        static void Launch(object owner,JObject p=null)=>Invoke(owner,"Launch",p??LaunchPacket(),true);
        static void Advance(object owner,long tick)=>Invoke(owner,"AdvanceTo",tick);
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void SerializedCheckpointKeepsNullAndReplayStateInEveryPhase(int phase) {
            var owner=New();if(phase==5)Invoke(owner,"CancelPreparation","o",1L);else if(phase==4){Launch(owner);Advance(owner,1);Invoke(owner,"StopBranch","o","main","WALL",2L);}else{if(phase>0)Launch(owner);if(phase>1){Advance(owner,1);Hit(owner,Contact());}if(phase>2)Invoke(owner,"End","COMBAT_END",3L);}
            var original=State(owner);var saved=JObject.Parse(original.ToString());Assert.That(Static("Fingerprint",saved),Is.EqualTo(Static("Fingerprint",original)));
            var restored=Static("Restore",saved,"test-combat",Static("Fingerprint",original));Assert.That(JToken.DeepEquals(State(restored),saved),Is.True);
        }
        [Test] public void PreparationCancellationHasNoBranchesAndCanOnlyHappenOnce() {
            var owner=New();Assert.That(Invoke(owner,"CancelPreparation","o",1L),Is.EqualTo(true));
            Assert.That(Invoke(owner,"CancelPreparation","o",2L),Is.EqualTo(false));
            Assert.That(Invoke(owner,"Launch",LaunchPacket(),true),Is.EqualTo(false));
            Assert.That(State(owner)["state"]["opportunities"][0]["branches"].HasValues,Is.False);
        }
        [Test] public void SourceDownAfterFireDoesNotKillFlightAndDoesNotAllowANewShot() {
            var owner=New();Launch(owner);
            Assert.That(Invoke(owner,"Launch",LaunchPacket(id:"later",sequence:2),false),Is.EqualTo(false));
            Advance(owner,1);Assert.That(Amount(Hit(owner,Contact(sequence:3))),Is.EqualTo(100));
            Assert.That(State(owner)["state"]["opportunities"][0]["packet"]["sourceGeneration"].Value<long>(),Is.EqualTo(7));
        }
        [Test] public void OriginalAttackAndCriticalBudgetAreFrozenAfterLaunch() {
            var owner=New();var p=LaunchPacket();Launch(owner,p);p["attack"]["completedAttack"]=V("900");
            Advance(owner,1);Assert.That(Amount(Hit(owner,Contact())),Is.EqualTo(100));
        }
        // The authority supplies the before/after budgets. This does not fabricate
        // a production temporary-effect source or decide an expiry/contact tie order.
        [TestCase(100,200,FriendlyProjectileOwner.Version)]
        [TestCase(200,100,FriendlyProjectileOwner.Version)]
        [TestCase(100,200,FriendlyProjectileOwner.ActualContactVersion)]
        [TestCase(200,100,FriendlyProjectileOwner.ActualContactVersion)]
        public void AuthorityAttackChangesAffectOnlyLaterShotsAcrossRestore(int before,int after,string version) {
            var owner=(object)new FriendlyProjectileOwner("test-combat",128,version);
            Action<JObject,int> budget=(p,amount)=>{
                p["attack"]["completedAttack"]=V(amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                p["attack"]["appliedOffense"]=amount==200?new JArray(new JObject{["sourceId"]="TEST_ONLY_offense",["group"]="EQUIPMENT_DIRECT",["valueBps"]=10000}):new JArray();
            };
            var old=LaunchPacket();old["version"]=version;budget(old,before);Launch(owner,old);
            Advance(owner,1);budget(old,after); // The original caller's mutable data has changed.
            var next=LaunchPacket(id:"next",sequence:2,tick:1);next["version"]=version;budget(next,after);Launch(owner,next);
            var restored=Restore(owner);
            var firstContact=Contact(tick:2,sequence:3,defense:"1000");firstContact["version"]=version;
            var nextContact=Contact(tick:2,sequence:4,defense:"1000",opportunity:"next");nextContact["version"]=version;
            foreach(var run in new[]{owner,restored}){
                Advance(run,2);var first=Hit(run,firstContact);var second=Hit(run,nextContact);
                Assert.That(Amount(first),Is.EqualTo(before/2));Assert.That(Amount(second),Is.EqualTo(after/2));
                Assert.That(Fresh(first)&&Fresh(second),Is.True);
                Assert.That(Fresh(Hit(run,firstContact)),Is.False);Assert.That(Fresh(Hit(run,nextContact)),Is.False);
            }
            Assert.That(JToken.DeepEquals(State(owner),State(restored)),Is.True);
            var completed=Restore(restored);Assert.That(Fresh(Hit(completed,firstContact)),Is.False);
            var conflict=(JObject)firstContact.DeepClone();conflict["arrival"]=Defense("0");var saved=State(completed);
            Assert.Throws<InvalidOperationException>(()=>Hit(completed,conflict));Assert.That(JToken.DeepEquals(saved,State(completed)),Is.True);
        }
        [Test] public void EachArrivalDefenseIsUsedAndExactReferenceIsNotFlooredEarly() {
            var owner=New();var p=LaunchPacket(secondary:true);p["attack"]["completedAttack"]=V("101");
            Launch(owner,p);Advance(owner,1);
            Assert.That(Amount(Hit(owner,Contact(defense:"500"))),Is.EqualTo(67));
            Assert.That(Amount(Hit(owner,Contact("aux","aux",sequence:3,defense:"500"))),Is.EqualTo(57));
        }
        [Test] public void MissingMainTargetDoesNotCancelIndependentAuxiliary() {
            var owner=New();Launch(owner,LaunchPacket(secondary:true));Advance(owner,1);
            Assert.That(Amount(Hit(owner,Contact(eligible:false))),Is.Zero);
            Assert.That(Amount(Hit(owner,Contact("aux","aux",sequence:3))),Is.EqualTo(85));
        }
        [Test] public void MissingPierceReservationDoesNotPromoteLaterCoefficient() {
            var owner=New();Launch(owner,LaunchPacket(pierce:true));Advance(owner,1);
            Hit(owner,Contact());Hit(owner,Contact("b",sequence:3,eligible:false));
            Assert.That(Amount(Hit(owner,Contact("c",sequence:4))),Is.EqualTo(30));
        }
        [TestCase("WALL")][TestCase("INTERCEPTED")][TestCase("EXPIRED")]
        public void InterruptedPiercingStopsOnlyThatFlight(string reason) {
            var owner=New();Launch(owner,LaunchPacket(pierce:true,secondary:true));Advance(owner,1);Hit(owner,Contact());
            Invoke(owner,"StopBranch","o","main",reason,3L);
            var stopped=Hit(owner,Contact("b",sequence:4));Assert.That(Amount(stopped),Is.Zero);Assert.That(Fresh(stopped),Is.False);
            Assert.That(Amount(Hit(owner,Contact("aux","aux",sequence:5))),Is.EqualTo(85));
        }
        [Test] public void SameTargetIdWithNewGenerationDoesNotReceiveDamage() {
            var owner=New();Launch(owner);Advance(owner,1);Assert.That(Amount(Hit(owner,Contact(generation:2))),Is.Zero);
        }
        [Test] public void DuplicateLaunchAndContactReuseOriginalResultEvenAfterTimeAndDefenseChanges() {
            var owner=New();var p=LaunchPacket();Launch(owner,p);
            Assert.That(Invoke(owner,"Launch",p,true),Is.EqualTo(false));Advance(owner,1);var c=Contact();
            Assert.That(Fresh(Hit(owner,c)),Is.True);Advance(owner,2);
            Assert.That(Fresh(Hit(owner,c)),Is.False);Assert.That(Amount(Hit(owner,c)),Is.EqualTo(100));
            var changed=(JObject)c.DeepClone();changed["arrival"]=Defense("1000");var before=State(owner);
            Assert.Throws<InvalidOperationException>(()=>Hit(owner,changed));
            Assert.That(JToken.DeepEquals(before,State(owner)),Is.True);
        }
        [Test] public void FlightRestoreContinuesAtOriginalLogicalTimeAndMatchesUninterruptedExecution() {
            var owner=New();Launch(owner,LaunchPacket(pierce:true));Advance(owner,1);Hit(owner,Contact());
            var restored=Restore(owner);
            Advance(owner,2);Advance(restored,2);
            Hit(owner,Contact("b",tick:2,sequence:3,defense:"1000"));
            Hit(restored,Contact("b",tick:2,sequence:3,defense:"1000"));
            Assert.That(JToken.DeepEquals(State(owner),State(restored)),Is.True);
        }
        [Test] public void ResolvedRestoreDoesNotIssueAnotherDamageApplication() {
            var owner=New();Launch(owner);Advance(owner,1);var c=Contact();Hit(owner,c);
            var restored=Restore(owner);Assert.That(Fresh(Hit(restored,c)),Is.False);
            Assert.That(Amount(Hit(restored,c)),Is.EqualTo(100));
        }
        [Test] public void HoldStopsClockAndContactsAndNodeEndTerminatesReservations() {
            var owner=New();Launch(owner);Invoke(owner,"SetHeld",true);
            Assert.That(Invoke(owner,"AdvanceTo",9L),Is.EqualTo(false));Assert.That(State(owner)["state"]["tick"].Value<long>(),Is.Zero);
            Assert.Throws<InvalidOperationException>(()=>Hit(owner,Contact(tick:0)));
            Invoke(owner,"SetHeld",false);Advance(owner,1);Invoke(owner,"End","NODE_END",2L);
            Assert.That(Amount(Hit(owner,Contact(sequence:3))),Is.Zero);
            Assert.That(Fresh(Hit(owner,Contact(sequence:3))),Is.False);
            Assert.Throws<InvalidOperationException>(()=>Launch(owner,LaunchPacket(id:"next",sequence:4,tick:1)));
        }
        [Test] public void ContactOrderAndAuthoritySequenceCannotBeRewrittenByView() {
            var owner=New();Launch(owner,LaunchPacket(pierce:true));Advance(owner,1);var before=State(owner);
            Assert.Throws<InvalidOperationException>(()=>Hit(owner,Contact("b")));
            Assert.Throws<InvalidOperationException>(()=>Hit(owner,Contact(sequence:1)));
            Assert.That(JToken.DeepEquals(before,State(owner)),Is.True);
            Hit(owner,Contact());Assert.That(State(owner)["state"]["opportunities"][0]["branches"][0]["status"].Value<string>(),Is.EqualTo("FLYING"));
        }
        [Test] public void UnreservedTargetsAndMalformedArrivalFailWithoutPartialContactRecord() {
            var owner=New();Launch(owner);Advance(owner,1);var before=State(owner);
            Assert.Throws<ArgumentException>(()=>Hit(owner,Contact("intruder")));
            var bad=Contact();bad["arrival"]["defense"]=V("-1");
            Assert.Throws<ArgumentException>(()=>Hit(owner,bad));
            Assert.That(JToken.DeepEquals(before,State(owner)),Is.True);
            Assert.That(Fresh(Hit(owner,Contact())),Is.True);
        }
        [Test] public void SnapshotTamperingMissingResultsAndWrongCombatAreRejected() {
            var owner=New();Launch(owner);Advance(owner,1);Hit(owner,Contact());var snapshot=State(owner);
            var hash=Static("Fingerprint",snapshot);snapshot["state"]["tick"]=999;
            Assert.Throws<ArgumentException>(()=>Static("Restore",snapshot,"test-combat",hash));
            Assert.Throws<ArgumentException>(()=>Static("Restore",snapshot,"test-combat",Static("Fingerprint",snapshot)));
            snapshot=State(owner);snapshot["state"]["opportunities"][0]["branches"][0]["results"]=new JArray();
            Assert.Throws<ArgumentException>(()=>Static("Restore",snapshot,"test-combat",Static("Fingerprint",snapshot)));
            snapshot=State(owner);
            Assert.Throws<ArgumentException>(()=>Static("Restore",snapshot,"other-combat",Static("Fingerprint",snapshot)));
        }
        sealed class TestFrame {
            internal object Owner;internal int Hp;
            internal TestFrame(object owner,int hp){Owner=owner;Hp=hp;}
        }
        // Explicit test-only composite authority, not an installed native HP consumer.
        static TestFrame Stage(TestFrame root,bool interruptAfterDamage,bool interruptAfterRecord) {
            var copy=Invoke(root.Owner,"Clone");var hp=root.Hp;
            var result=Hit(copy,Contact());
            if(interruptAfterRecord)throw new InvalidOperationException("test interruption after staged record");
            if(Fresh(result))hp-=Amount(result);
            if(interruptAfterDamage)throw new InvalidOperationException("test interruption after staged damage");
            return new TestFrame(copy,hp);
        }
        [TestCase(true,false)][TestCase(false,true)]
        public void CompositeStagingInterruptionsCannotPublishOnlyDamageOrOnlyContact(bool afterDamage,bool afterRecord) {
            var owner=New();Launch(owner);Advance(owner,1);var root=new TestFrame(owner,1000);var before=State(owner);
            Assert.Throws<InvalidOperationException>(()=>Stage(root,afterDamage,afterRecord));
            Assert.That(root.Hp,Is.EqualTo(1000));Assert.That(JToken.DeepEquals(State(root.Owner),before),Is.True);
            root=Stage(root,false,false);Assert.That(root.Hp,Is.EqualTo(900));
            root=Stage(root,false,false);Assert.That(root.Hp,Is.EqualTo(900));
        }
        [Test] public void CapacityAndVersionsAreExplicitAndSnapshotIsDetached() {
            var owner=New(1);Launch(owner);var before=State(owner);
            Assert.Throws<InvalidOperationException>(()=>Invoke(owner,"SetHeld",true));
            Assert.That(JToken.DeepEquals(before,State(owner)),Is.True);
            var snapshot=State(owner);snapshot["state"]["tick"]=99;
            Assert.That(State(owner)["state"]["tick"].Value<long>(),Is.Zero);
            var other=New();var p=LaunchPacket();p["version"]="old-basic";
            Assert.Throws<ArgumentException>(()=>Launch(other,p));
        }
    }
}
#endif
