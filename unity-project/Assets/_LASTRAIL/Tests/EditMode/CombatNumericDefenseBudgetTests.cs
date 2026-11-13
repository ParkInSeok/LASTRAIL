#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatNumericDefenseBudgetTests
    {
        static JObject Value(string n,string d="1")=>new JObject{["numerator"]=n,["denominator"]=d};
        static JObject Source(string id,long bps)=>new JObject{["sourceId"]=id,["valueBps"]=bps};
        static JObject Input(string magnitude="10000",string defense="500")=>new JObject {
            ["version"]="combat.numeric-defense-budget.v1",["base"]=Value(magnitude),["defense"]=Value(defense),["effects"]=new JArray(),
            ["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=new JObject{
                ["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};
        static Type Resolver=>typeof(CombatVitals).Assembly.GetType("LastRail.Battle.CombatNumericDefenseBudget");
        static object Resolve(JObject input) {
            Assert.That(Resolver,Is.Not.Null,"Explicit numeric defense resolver is missing.");
            try { return Resolver.GetMethod("Resolve").Invoke(null,new object[]{input}); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        static JObject Snapshot(object result)=>(JObject)result.GetType().GetMethod("Snapshot").Invoke(result,null);
        static int Magnitude(object result) {
            try { return (int)result.GetType().GetMethod("ToVitalsMagnitude").Invoke(result,null); }
            catch(TargetInvocationException e) { throw e.InnerException; }
        }
        [TestCase("0",10000)][TestCase("100",9090)][TestCase("250",8000)][TestCase("500",6666)]
        [TestCase("1000",5000)][TestCase("2000",3333)][TestCase("4000",2000)]
        public void NumericDefenseHasNoLegacyArmorCeiling(string defense,int expected)=>Assert.That(Magnitude(Resolve(Input(defense:defense))),Is.EqualTo(expected));
        [Test] public void FractionalDefenseAndIndependentGroupsRoundOnlyAtFinalDamage() {
            var input=Input("10000"); input["weakening"]=new JArray(Source("w",2000)); input["penetration"]=new JArray(Source("p",2500));
            input["reductions"]["independent"]=new JArray(new JObject{["groupId"]="a",["valueBps"]=1000},new JObject{["groupId"]="b",["valueBps"]=2000});
            var snapshot=Snapshot(Resolve(input)); Assert.That(JToken.DeepEquals(snapshot["effectiveDefense"],Value("300")),Is.True);
            Assert.That(JToken.DeepEquals(snapshot["exact"],Value("72000","13")),Is.True); Assert.That((string)snapshot["ordinaryBeforeShield"],Is.EqualTo("5538"));
            input=Input("1000"); input["defense"]=Value("1","2"); Assert.That(Magnitude(Resolve(input)),Is.EqualTo(999));
        }
        [Test] public void SourceCopiesDoNotStackAndCapsAreAppliedToEligibleGroups() {
            var input=Input(); input["weakening"]=new JArray(Source("w1",4000),Source("w2",2000));
            input["penetration"]=new JArray(Source("p",4000),Source("p",4000),Source("p2",4000));
            Assert.That(JToken.DeepEquals(Snapshot(Resolve(input))["effectiveDefense"],Value("140")),Is.True);
            input=Input("1000","0"); input["reductions"]["equipment"]=new JArray(Source("eq",3000),Source("eq",3000));
            input["reductions"]["guardStance"]=new JArray(Source("g1",2500),Source("g2",1000)); Assert.That(Magnitude(Resolve(input)),Is.EqualTo(525));
            ((JArray)input["reductions"]["equipment"]).Add(Source("eq2",3000)); Assert.That(Magnitude(Resolve(input)),Is.EqualTo(375));
        }
        [Test] public void OffenseGroupsAreAppliedOnceBeforeDefense() {
            var input=Input("1000","500"); input["effects"]=new JArray(
                new JObject{["sourceId"]="eq1",["group"]="EQUIPMENT_DIRECT",["valueBps"]=1000},
                new JObject{["sourceId"]="eq2",["group"]="EQUIPMENT_DIRECT",["valueBps"]=2000},
                new JObject{["sourceId"]="out",["group"]="NON_EQUIPMENT_OUTGOING",["valueBps"]=5000},
                new JObject{["sourceId"]="mark",["group"]="MARK",["valueBps"]=5000});
            Assert.That(Magnitude(Resolve(input)),Is.EqualTo(1352)); ((JArray)input["effects"]).Add(input["effects"][0].DeepClone());
            Assert.That(Magnitude(Resolve(input)),Is.EqualTo(1352));
        }
        [Test] public void ConflictingIdentityAndUnknownLegacyFieldsAreRejectedWithoutMutatingInput() {
            var input=Input(); input["weakening"]=new JArray(Source("shared",1000)); input["penetration"]=new JArray(Source("shared",1000));
            var original=input.DeepClone(); Assert.Throws<ArgumentException>(()=>Resolve(input)); Assert.That(JToken.DeepEquals(original,input),Is.True);
            input=Input(); input["armorBps"]=0; Assert.Throws<ArgumentException>(()=>Resolve(input));
            input=Input(); input.Remove("version"); Assert.Throws<ArgumentException>(()=>Resolve(input));
            input=Input(); input["version"]="combat.direct-damage-budget.v1"; Assert.Throws<ArgumentException>(()=>Resolve(input));
        }
        [Test] public void InvalidRationalAndReductionBypassesFailAtBoundary() {
            foreach(var value in new[]{Value("01"),Value("-1"),Value("1","0"),Value(new string('9',257))}) {
                var input=Input(); input["base"]=value; Assert.Throws<ArgumentException>(()=>Resolve(input));
            }
            foreach(var id in new[]{"EQ06","guard_stance"}) { var input=Input(); input["reductions"]["independent"]=new JArray(new JObject{["groupId"]=id,["valueBps"]=1000}); Assert.Throws<ArgumentException>(()=>Resolve(input)); }
            var tooMuch=Input(); tooMuch["reductions"]["independent"]=new JArray(new JObject{["groupId"]="a",["valueBps"]=10001}); Assert.Throws<ArgumentException>(()=>Resolve(tooMuch));
        }
        [Test] public void ExactLargeOutputCannotSilentlyOverflowVitalsAndSnapshotIsIndependent() {
            var result=Resolve(Input("2147483648","0")); Assert.Throws<OverflowException>(()=>Magnitude(result));
            var snap=Snapshot(result); snap["exact"]["numerator"]="0"; Assert.That((string)Snapshot(result)["ordinaryBeforeShield"],Is.EqualTo("2147483648"));
        }
        [Test] public void ResolvedDamageUsesExistingShieldHpAndTransferRouterOnce() {
            var train=new CombatVitals(1000,0,true); var carriage=new CombatVitals(500,0,true);
            carriage.Shield(new CombatShieldSource("support","approved",0),100,10,0);
            var router=new CombatDamageRouter("train",train,new Dictionary<string,CombatVitals>{{"car",carriage}},10);
            var resolved=Magnitude(Resolve(Input("900","500"))); Assert.That(resolved,Is.EqualTo(600));
            var first=router.ApplyCarriageDamage("original-hit","car",resolved,0,false);
            Assert.That(first.Receipt.Absorbed,Is.EqualTo(100)); Assert.That(first.Receipt.TargetHpLoss,Is.EqualTo(500));
            Assert.That(first.Receipt.TransferToTrain,Is.EqualTo(150)); Assert.That(train.Hp,Is.EqualTo(850));
            Assert.That(router.ApplyCarriageDamage("original-hit","car",resolved,0,false).Applied,Is.False); Assert.That(train.Hp,Is.EqualTo(850));
        }
        [Test] public void DefenseFactorRawDigitsMustPassTheServerBoundaryBeforeNormalization() {
            var input=Input("1"); input["defense"]=Value("1000","1"+new string('0',254)+"1");
            Assert.Throws<ArgumentException>(()=>Resolve(input));
        }
        [TestCase("1000.0")][TestCase("1e3")]
        public void IntegralJsonNumbersUseTheSameBoundaryAsServerNumbers(string text) {
            var input=Input("1000","0"); input["penetration"]=JArray.Parse("[{\"sourceId\":\"p\",\"valueBps\":"+text+"}]");
            Assert.That(Magnitude(Resolve(input)),Is.EqualTo(1000));
        }
        [Test] public void ActualServerModuleVectorsMatchExactOutput() {
            var path=System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,"../../QA/CoreLogic-20261001/NumericDefense/server-vectors.json"));
            var vectors=JObject.Parse(System.IO.File.ReadAllText(path));
            Assert.That((string)vectors["classification"],Is.EqualTo("TEST_ONLY_SERVER_MODULE_VECTORS_NOT_HTTP"));
            foreach(var pin in (JArray)vectors["sourcePins"]) {
                var source=System.IO.Path.Combine(UnityEngine.Application.dataPath,"../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/apps/game-api/src/",(string)pin["file"]);
                using(var sha=System.Security.Cryptography.SHA256.Create()) {
                    var hash=BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant();
                    Assert.That(hash,Is.EqualTo((string)pin["sha256"]),"Server arithmetic source changed; regenerate explicit test vectors.");
                }
            }
            var cases=(JArray)vectors["cases"]; Assert.That(cases.Count,Is.EqualTo(123));
            foreach(var vector in cases) {
                var input=vector["inputJson"]==null?(JObject)vector["input"]:JObject.Parse((string)vector["inputJson"]);
                if((bool?)vector["rejected"]==true) Assert.Throws<ArgumentException>(()=>Resolve(input),(string)vector["caseId"]);
                else Assert.That(JToken.DeepEquals(Snapshot(Resolve(input)),vector["expected"]),Is.True,(string)vector["caseId"]);
            }
        }
        [Test] public void FullReductionAndZeroBaseDoNotProduceMinimumOneDamage() {
            var input=Input(); input["reductions"]["guardStance"]=new JArray(Source("guard",10000)); Assert.That(Magnitude(Resolve(input)),Is.Zero);
            Assert.That(Magnitude(Resolve(Input("0"))),Is.Zero);
        }
    }
}
#endif



