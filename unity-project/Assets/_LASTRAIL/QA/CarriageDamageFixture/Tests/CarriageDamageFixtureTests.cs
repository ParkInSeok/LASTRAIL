using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Qa.CarriageDamage;

public sealed class CarriageDamageFixtureTests
{
    static JObject Config()=>JObject.Parse(@"{'trainMaxHp':1000,'cars':[{'id':'a','maxHp':500,'devicePeriod':8},{'id':'b','maxHp':500,'devicePeriod':8}],'repairTicks':[10,15,20],'recoveryBps':4000,'protectionTicks':2}");
    static ConvoyFixture Make()=>new ConvoyFixture(Config());
    static JObject Hit(string id,long amount,string car="a",string origin="projectile",long? fallback=null){var e=new JObject{["eventId"]=id,["route"]="carriage",["carId"]=car,["amount"]=amount,["origin"]=origin};if(fallback.HasValue)e["disabledDirectAmount"]=fallback.Value;return e;}
    static JObject Fatal()=>new JObject{["eventId"]="fatal",["route"]="train",["amount"]=1000,["origin"]="physical"};
    static JToken State(ConvoyFixture m)=>m.Capture()["state"];
    static void Same(JToken a,JToken b)=>Assert.IsTrue(JToken.DeepEquals(a,b),"Expected "+a+"\nActual "+b);

    [Test] public void ActualLossFloorThirtyWithoutSecondDefense(){var m=Make();var r=m.Apply(Hit("e",101));Assert.AreEqual(30,(long)r["transfer"]);Assert.AreEqual(399,(long)State(m)["cars"]["a"]["hp"]);Assert.AreEqual(970,(long)State(m)["trainHp"]);}
    [Test] public void OverkillAndLateProjectileDoNotDownAgain(){var m=Make();m.Apply(Hit("first",480));var r=m.Apply(Hit("over",100));Assert.AreEqual(20,(long)r["loss"]);Assert.AreEqual(6,(long)r["transfer"]);m.Apply(Hit("late",100));Assert.AreEqual(1,(long)State(m)["cars"]["a"]["downs"]);Assert.AreEqual(850,(long)State(m)["trainHp"]);}
    [Test] public void ZeroAndDuplicatePreserveStateAndRejectConflict(){var m=Make();var e=Hit("e",0);var r=m.Apply(e);var saved=m.Capture();Same(r,m.Apply(e));Same(saved,m.Capture());Assert.Throws<InvalidOperationException>(()=>m.Apply(Hit("e",5)));Same(saved,m.Capture());}
    [Test] public void BrokenTargetExcludedAndLateShotLosesZero(){var m=Make();m.Apply(Hit("down",500));CollectionAssert.AreEqual(new[]{"b"},m.Targets());var r=m.Apply(Hit("late",40));Assert.AreEqual(0,(long)r["loss"]);Assert.AreEqual(0,(long)r["transfer"]);}
    [Test] public void PhysicalFallbackIsExplicitAndExactlyOneMemoryEvent(){var m=Make();m.Apply(Hit("down",500));var saved=m.Capture();Assert.Throws<InvalidOperationException>(()=>m.Apply(Hit("rock",100,"a","physical")));Same(saved,m.Capture());var e=Hit("rock",100,"a","physical",40);var r=m.Apply(e);Assert.AreEqual("direct-train",(string)r["route"]);Assert.AreEqual(0,(long)r["transfer"]);m.Apply(e);Assert.AreEqual(810,(long)State(m)["trainHp"]);}
    [Test] public void IndependentRecoveryWaitsFullDevicePeriod(){var m=Make();m.Apply(Hit("a",500));m.Apply(Hit("b",500,"b"));m.Advance(9);Assert.AreEqual(0,(long)State(m)["cars"]["a"]["hp"]);m.Advance(1);var car=State(m)["cars"]["a"];Assert.AreEqual(200,(long)car["hp"]);Assert.AreEqual(18,(long)car["nextPulse"]);Assert.AreEqual(0,(long)car["pulses"]);Assert.AreEqual(700,(long)State(m)["trainHp"]);m.Advance(8);Assert.AreEqual(1,(long)State(m)["cars"]["a"]["pulses"]);}
    [Test] public void TrainRepairDoesNotRepairCarAndProtectionBlocksLoss(){var m=Make();m.Apply(Hit("down",500));m.RepairTrain("repair",80);Assert.AreEqual(0,(long)State(m)["cars"]["a"]["hp"]);Assert.AreEqual(930,(long)State(m)["trainHp"]);m.Advance(10);var r=m.Apply(Hit("protected",100));Assert.AreEqual(0,(long)r["loss"]);Assert.IsTrue((bool)r["protected"]);}
    [Test] public void DefeatFreezesRecoveryAndCannotResurrect(){var m=Make();m.Apply(Hit("down",500));m.Apply(Fatal());var state=State(m);m.Advance(20);m.RepairTrain("late",1000);Same(state,State(m));Assert.AreEqual("defeated",(string)State(m)["phase"]);}
    [Test] public void RestoreRetainsLedgerAndIndependentClock(){var m=Make();var e=Hit("e",500);m.Apply(e);m.Advance(4);var restored=ConvoyFixture.Restore(m.Capture());Same(m.Apply(e),restored.Apply(e));m.Advance(6);restored.Advance(6);Same(m.Capture(),restored.Capture());}
    [Test] public void InvalidAmountsAndCrossKindEventCollisionsAreAtomic(){var m=Make();foreach(var value in new JToken[]{-1,1.5,double.NaN,double.PositiveInfinity,9007199254740992L}){var e=Hit("bad",0);e["amount"]=value;var before=m.Capture();Assert.Throws<InvalidOperationException>(()=>m.Apply(e));Same(before,m.Capture());}Assert.Throws<InvalidOperationException>(()=>m.Apply(Hit("unknown",1,"missing")));m.Apply(Hit("same",5));Assert.Throws<InvalidOperationException>(()=>m.RepairTrain("same",5));}
    [Test] public void DefeatedConvoyHasNoTargets(){var m=Make();m.Apply(Fatal());Assert.IsEmpty(m.Targets());}
    [Test] public void RepeatedRecoveryDelayCapsAtLastExplicitStage(){var m=Make();int i=0;foreach(int delay in new[]{10,15,20,20}){m.Apply(Hit("down-"+i++,500));Assert.AreEqual((long)State(m)["tick"]+delay,(long)State(m)["cars"]["a"]["recoverAt"]);m.Advance(delay+2);}Assert.AreEqual(4,(long)State(m)["cars"]["a"]["downs"]);}
    [Test] public void RepairDuplicateRetainsOriginalReceiptAfterNewDamage(){var m=Make();m.Apply(Hit("down",500));var r=m.RepairTrain("repair",80);var e=Fatal();e["amount"]=100;m.Apply(e);var before=m.Capture();Same(r,m.RepairTrain("repair",80));Same(before,m.Capture());}
    [Test] public void RestoreRejectsTamperingAndReturnsAreDetached(){var m=Make();var receipt=m.Apply(Hit("e",101));receipt["transfer"]=999;foreach(var change in new Action<JObject>[] {s=>s["state"]["trainHp"]=999,s=>s["ledger"]["e"]["receipt"]["transfer"]=999,s=>s["operations"][0]["value"]["amount"]=102}){var saved=m.Capture();change(saved);Assert.Throws<InvalidOperationException>(()=>ConvoyFixture.Restore(saved));}Assert.AreEqual(970,(long)State(m)["trainHp"]);}
    [Test] public void FatalCarriageTransferDoesNotReserveRecovery(){var c=Config();c["trainMaxHp"]=1;c["cars"]=new JArray(new JObject{["id"]="a",["maxHp"]=4,["devicePeriod"]=8});c["recoveryBps"]=10000;var m=new ConvoyFixture(c);m.Apply(Hit("fatal-car",4));Assert.AreEqual("defeated",(string)State(m)["phase"]);Assert.AreEqual(JTokenType.Null,State(m)["cars"]["a"]["recoverAt"].Type);}
    [Test] public void QuotientOracleAndSafeIntegerExtremeAreExact(){for(int loss=0;loss<=500;loss++){var m=Make();Assert.AreEqual(3*(loss/10)+(loss%10)*3/10,(long)m.Apply(Hit("e",loss))["transfer"]);}var c=Config();c["trainMaxHp"]=9007199254740991L;c["cars"]=new JArray(new JObject{["id"]="a",["maxHp"]=9007199254740991L,["devicePeriod"]=8});Assert.AreEqual(2702159776422297L,(long)new ConvoyFixture(c).Apply(Hit("e",9007199254740991L))["transfer"]);}
    [Test] public void UnsafeTickSumRejectsBeforeMutation(){var c=Config();c["repairTicks"]=new JArray(9007199254740991L);var m=new ConvoyFixture(c);var before=m.Capture();Assert.Throws<InvalidOperationException>(()=>m.Advance(1));Same(before,m.Capture());}
    [Test] public void FloatingSafeIntegerBoundaryDoesNotRoundOrAcceptUnsafe()
    {
        var c=Config();c["trainMaxHp"]=new JValue(9007199254740991d);
        c["cars"]=new JArray(new JObject{["id"]="a",["maxHp"]=new JValue(9007199254740991d),["devicePeriod"]=8});
        var m=new ConvoyFixture(c);var e=Hit("max",0);e["amount"]=new JValue(9007199254740991d);
        Assert.AreEqual(9007199254740991L,(long)m.Apply(e)["loss"]);
        e=Hit("unsafe",0);e["amount"]=new JValue(9007199254740992d);var before=m.Capture();
        Assert.Throws<InvalidOperationException>(()=>m.Apply(e));Same(before,m.Capture());
        // Replace the property itself: Newtonsoft's float equality can suppress a nearby-value setter.
        c.Remove("trainMaxHp");c.Add("trainMaxHp",new JValue(9007199254740992d));
        Assert.AreEqual(9007199254740992d,(double)c["trainMaxHp"]);
        Assert.Throws<InvalidOperationException>(()=>new ConvoyFixture(c));
        e["amount"]=new JValue(1.0000000000000002d);Assert.Throws<InvalidOperationException>(()=>m.Apply(e));Same(before,m.Capture());
    }
    [Test] public void ObservedRecordsAndAdditionalBoundariesMatch()=>CarriageDamageFixtureVerification.Verify();
    [Test] public void FixtureAssembliesAreExcludedFromPlayer()=>CarriageDamageFixtureVerification.VerifyPlayerExclusion();
}
