#if UNITY_EDITOR
using System;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatProjectileBudgetTests
    {
        static JObject V(string n,string d="1")=>new JObject { ["numerator"]=n,["denominator"]=d };
        static JObject E(string id,string group,int bps)=>new JObject { ["sourceId"]=id,["group"]=group,["valueBps"]=bps };
        static JObject Launch(string d="100")=>new JObject {
            ["version"]="combat.projectile-budget.v1.20261015",["completedAttack"]=V(d),
            ["appliedOffense"]=new JArray(),["penetration"]=new JArray() };
        static JObject Arrival(string defense="0")=>new JObject {
            ["version"]="combat.projectile-budget.v1.20261015",["defense"]=V(defense),["effects"]=new JArray(),
            ["weakening"]=new JArray(),["reductions"]=new JObject {
                ["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray() }};
        static object Call(object instance,string method,params object[] args) {
            try { return instance.GetType().GetMethod(method).Invoke(instance,args); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        static object Capture(JObject packet) {
            var type=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.CombatProjectileAttackSnapshot");
            Assert.That(type,Is.Not.Null,"Pure launch/arrival budget boundary is missing.");
            try { return type.GetMethod("Capture").Invoke(null,new object[]{packet}); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        static object Reference(object shot,JObject target)=>Call(shot,"Evaluate",target);
        static int Damage(object reference,int bps=10000)=>(int)Call(reference,"ScaleAndFloor",bps);
        static JObject Snapshot(object value)=>(JObject)Call(value,"Snapshot");
        [Test] public void CompletedLaunchAttackIsDetachedFromLaterAttackAndCriticalChanges() {
            // Reading a live launch packet or reapplying its 2x offense would fail this.
            var launch=Launch("200"); launch["appliedOffense"]=new JArray(E("launch","EQUIPMENT_DIRECT",10000));
            var shot=Capture(launch); launch["completedAttack"]=V("900");
            launch["appliedOffense"][0]["valueBps"]=90000;
            Assert.That(Damage(Reference(shot,Arrival())),Is.EqualTo(200));
            Assert.That(Damage(Reference(shot,Arrival())),Is.EqualTo(200));
        }
        [Test] public void ArrivalDefenseIsEvaluatedPerTargetWithoutDoubleDefense() {
            var shot=Capture(Launch("100"));var arrival=Arrival("250");
            var first=Reference(shot,arrival); arrival["defense"]=V("1500");
            Assert.That(Damage(first,8500),Is.EqualTo(68));
            Assert.That(Damage(Reference(shot,arrival),8500),Is.EqualTo(34));
            Assert.That(Damage(first),Is.EqualTo(80));
        }
        [Test] public void TargetSpecificOffenseSharesCapsWithAlreadyAppliedLaunchGroups() {
            // D=120 already includes 20%; arrival adds 20% in the same 30%-capped group.
            var launch=Launch("120");launch["appliedOffense"]=new JArray(E("attack","NON_EQUIPMENT_OUTGOING",2000));
            var target=Arrival();target["effects"]=new JArray(E("target","NON_EQUIPMENT_OUTGOING",2000));
            Assert.That(Damage(Reference(Capture(launch),target)),Is.EqualTo(130));
        }
        [Test] public void SourceCopiesAreNotAppliedTwiceAndConflictsRejectAtomically() {
            var launch=Launch("120");launch["appliedOffense"]=new JArray(E("same","EQUIPMENT_DIRECT",2000));
            var shot=Capture(launch);var target=Arrival(); target["effects"]=new JArray(E("same","EQUIPMENT_DIRECT",2000));
            Assert.That(Damage(Reference(shot,target)),Is.EqualTo(120));
            target["effects"][0]["valueBps"]=3000;var before=target.DeepClone();
            Assert.Throws<ArgumentException>(()=>Reference(shot,target));
            Assert.That(JToken.DeepEquals(before,target),Is.True);
            Assert.That(Damage(Reference(shot,Arrival())),Is.EqualTo(120));
        }
        [Test] public void TargetMarksAndReductionsAreAppliedOnceAtArrival() {
            var target=Arrival("1000");target["effects"]=new JArray(E("marked","MARK",1000));
            target["reductions"]["independent"]=new JArray(new JObject { ["groupId"]="condition",["valueBps"]=2000 });
            Assert.That(Damage(Reference(Capture(Launch("100")),target)),Is.EqualTo(44));
        }
        [Test] public void PiercingAllocatesExactAttackBeforeDefenseAndFloorsOnlyAtEnd() {
            var shot=Capture(Launch("101"));var target=Arrival("500");
            var pierce=Call(shot,"EvaluatePiercing",target,6000);
            Assert.That(JToken.DeepEquals(Snapshot(pierce)["exact"],V("202","5")),Is.True);
            Assert.That(Damage(pierce),Is.EqualTo(40));
            var undefended=Call(Capture(Launch()),"EvaluatePiercing",Arrival(),6000);
            Assert.That(Damage(undefended),Is.EqualTo(60));
        }
        [Test] public void RepeatedReferenceEvaluationLeavesPacketsAndRandomStateUnchanged() {
            var launch=Launch();var target=Arrival("250");
            var l=launch.DeepClone();var t=target.DeepClone();var random=UnityEngine.Random.state;
            var shot=Capture(launch);
            for(var i=0;i<4;i++)Assert.That(Damage(Reference(shot,target)),Is.EqualTo(80));
            Assert.That(JToken.DeepEquals(launch,l),Is.True);Assert.That(JToken.DeepEquals(target,t),Is.True);
            Assert.That(UnityEngine.Random.state,Is.EqualTo(random));
            var result=Reference(shot,target);var snapshot=Snapshot(result);snapshot["exact"]=V("999");
            Assert.That(Damage(result),Is.EqualTo(80));
        }
        [Test] public void LaunchPenetrationIsFrozenAndArrivalWeakeningUsesExistingCaps() {
            var launch=Launch();launch["penetration"]=new JArray(new JObject { ["sourceId"]="p",["valueBps"]=2000 });
            var shot=Capture(launch);launch["penetration"][0]["valueBps"]=6000;
            var target=Arrival("1000"); target["weakening"]=new JArray(new JObject { ["sourceId"]="w",["valueBps"]=2000 });
            Assert.That(JToken.DeepEquals(Snapshot(Reference(shot,target))["effectiveDefense"],V("640")),Is.True);
            Assert.That(Damage(Reference(shot,target)),Is.EqualTo(60));
        }
        [Test] public void UnknownVersionsAndAmbiguousPreEvaluatedPacketsAreRejected() {
            var launch=Launch();launch["version"]="unknown";Assert.Throws<ArgumentException>(()=>Capture(launch));
            launch=Launch();launch["effects"]=new JArray();Assert.Throws<ArgumentException>(()=>Capture(launch));
            var shot=Capture(Launch());var target=Arrival();target["version"]="unknown";Assert.Throws<ArgumentException>(()=>Reference(shot,target));
            target=Arrival();target["ordinaryBeforeShield"]=100;Assert.Throws<ArgumentException>(()=>Reference(shot,target));
            launch=Launch();launch["completedAttack"]=V("-1");Assert.Throws<ArgumentException>(()=>Capture(launch));
        }
        [TestCase(-1)][TestCase(10001)]
        public void InvalidBranchCoefficientsAreRejected(int bps) {
            var shot=Capture(Launch());var result=Reference(shot,Arrival());
            Assert.Throws<ArgumentOutOfRangeException>(()=>Damage(result,bps));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Call(shot,"EvaluatePiercing",Arrival(),bps));
        }
        [Test] public void ZeroAndOverflowKeepExistingNoMinimumNoClampContract() {
            var shot=Capture(Launch("0"));Assert.That(Damage(Reference(shot,Arrival())),Is.Zero);
            Assert.That(Damage(Reference(Capture(Launch()),Arrival()),0),Is.Zero);
            Assert.Throws<OverflowException>(()=>Damage(Reference(Capture(Launch("2147483648")),Arrival())));
        }
    }
}
#endif