#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class BMiraBasicSourceTests
    {
        static JObject Fixture()=>JObject.Parse(File.ReadAllText(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../QA/MiraBasic-20261015/server-packets.json"))));
        static JObject Packet(string name="trained")=>(JObject)Fixture()["packets"][name].DeepClone();
        static object Read(JObject input,JObject original=null,string hash=null,string account="a") {
            var type=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.BMiraBasicSource");
            Assert.That(type,Is.Not.Null,"Mira pinned source consumer is missing.");
            try{return type.GetMethod("Read").Invoke(null,new object[]{input,original??input,hash??(string)input["packetHash"],account});}
            catch(TargetInvocationException e){throw e.InnerException;}
        }
        static object Call(object source,string method,params object[] args) {
            try{return source.GetType().GetMethod(method).Invoke(source,args);}
            catch(TargetInvocationException e){throw e.InnerException;}
        }
        static object Get(object source,string name)=>source.GetType().GetProperty(name).GetValue(source);
        static JObject Capture(object source)=>(JObject)Call(source,"Capture");
        static JObject Rehash(JObject value,string field="packetHash") {
            var body=(JObject)value.DeepClone();body.Remove(field);value[field]=BDirectAttackInput.Digest(body).Substring(7);return value;
        }
        [Test] public void ActualServerPacketKeepsGrownAttackAndDoesNotAddBasicPassiveToPermanentStats() {
            var p=Packet();var s=Read(p);var actor=(JObject)Call(s,"Actor");
            Assert.That(JToken.DeepEquals(actor,p["actor"]),Is.True);
            Assert.That((string)actor["owned"]["permanentStats"]["attack"]["numerator"],Is.EqualTo("4142250000"));
            Assert.That((string)actor["owned"]["permanentStats"]["attack"]["denominator"],Is.EqualTo("100000000"));
            Assert.That(Get(s,"AbilityId"),Is.EqualTo("op_mira.basic"));
            Assert.That(Get(s,"OriginalBasicIntervalMs"),Is.EqualTo(1800L));
            Assert.That(Get(s,"PhysicalAdjacentCars"),Is.EqualTo(2));
            Assert.That(JToken.DeepEquals(Capture(s),p),Is.True);
        }
        [Test] public void ApprovedBallisticUnitsAreReadWithoutPromotingSourceToRuntime() {
            var s=Read(Packet());Assert.That(Get(s,"SpeedMmPerSecond"),Is.EqualTo(48000L));
            Assert.That(Get(s,"RadiusMm"),Is.EqualTo(60L));Assert.That(Get(s,"MaximumDistanceMm"),Is.EqualTo(24000L));
            Assert.That(Get(s,"MaximumFlightMs"),Is.EqualTo(500L));Assert.That(Get(s,"LengthUnitMm"),Is.EqualTo(12000L));
            Assert.That(Get(s,"StartReady"),Is.EqualTo(false));
            var ex=Assert.Throws<InvalidOperationException>(()=>Call(s,"RequireRuntimeReady"));
            StringAssert.Contains("ACTUAL_MUZZLE_HIT_SHAPE_AND_GEOMETRY",ex.Message);
            StringAssert.Contains("EFFECTIVE_STATS_AFTER_ORIGINAL_MODIFIERS",ex.Message);
        }
        [Test] public void MissingTrainingAndOriginalUnknownModifiersRemainNull() {
            var s=Read(Packet("missing"));var p=Capture(s);
            Assert.That(p["actor"]["owned"]["permanentStats"].Type,Is.EqualTo(JTokenType.Null));
            Assert.That(p["actor"]["owned"]["effectiveStats"].Type,Is.EqualTo(JTokenType.Null));
            foreach(var field in new[]{"equipment","research"})Assert.That(p["modifierSources"][field].Type,Is.EqualTo(JTokenType.Null));
            Assert.That(p["geometry"].Type,Is.EqualTo(JTokenType.Null));
            Assert.That(Get(s,"StartReady"),Is.EqualTo(false));
        }
        [Test] public void ActualPersistedLocalRefitAndCommanderSourceArePreserved() {
            var p=Packet("placed");var s=Read(p);var result=Capture(s);
            Assert.That(JToken.DeepEquals(result["refit"],p["refit"]),Is.True);
            Assert.That(JToken.DeepEquals(result["modifierSources"]["commanderSource"],p["modifierSources"]["commanderSource"]),Is.True);
            Assert.That((string)result["actor"]["placement"]["carriageInstanceId"],Is.EqualTo("LOCAL_MIRA_UNITY_1"));
            Assert.That((int)result["actor"]["bcb"]["rank"],Is.EqualTo(1));
        }
        [Test] public void InputActorDefinitionAndCaptureMutationsCannotRewriteFrozenOriginal() {
            var original=Packet();var input=(JObject)original.DeepClone();var s=Read(input,original);
            input["actor"]["owned"]["permanentStats"]["attack"]["numerator"]="1";
            original["definition"]["ballistics"]["speedMmPerSecond"]=1;
            var result=Capture(s);result["definition"]["ballistics"]["radiusMm"]=999;
            ((JObject)Call(s,"Actor"))["abilityId"]="changed";
            Assert.That(Get(s,"SpeedMmPerSecond"),Is.EqualTo(48000L));
            Assert.That((string)Capture(s)["actor"]["owned"]["permanentStats"]["attack"]["numerator"],Is.EqualTo("4142250000"));
        }
        [Test] public void DifferentAccountAndWrongIndependentHashAreRejected() {
            var p=Packet();Assert.Throws<ArgumentException>(()=>Read(p,p,account:"b"));
            Assert.Throws<ArgumentException>(()=>Read(p,p,new string('0',64)));
        }
        [Test] public void SelfRehashedStatsCannotReplaceIndependentlyRetainedOriginal() {
            var original=Packet();var forged=(JObject)original.DeepClone();
            forged["actor"]["owned"]["permanentStats"]["attack"]["numerator"]="999";Rehash(forged);
            Assert.Throws<ArgumentException>(()=>Read(forged,original,(string)original["packetHash"]));
            Assert.Throws<ArgumentException>(()=>Read(forged,original,(string)forged["packetHash"]));
        }
        [Test] public void PinnedOriginalRestoresWithoutReplacingItWithNewerOwnedOrPlacementData() {
            var old=Packet();var newer=Packet("placed");var read=Read(old,old);
            Assert.That(JToken.DeepEquals(Capture(read),old),Is.True);
            Assert.That(Capture(read)["refit"].Type,Is.EqualTo(JTokenType.Null));
            Assert.Throws<ArgumentException>(()=>Read(newer,old,(string)old["packetHash"]));
        }
        [Test] public void UnsupportedVersionAndDefinitionEvenWhenSelfConsistentlyHashedAreRejected() {
            var p=Packet();p["version"]="old";Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p));
            p=Packet();p["definition"]["version"]="new";
            Rehash((JObject)p["definition"],"definitionHash");p["definitionHash"]=p["definition"]["definitionHash"].DeepClone();Rehash(p);
            Assert.Throws<ArgumentException>(()=>Read(p));
        }
        [Test] public void FalseRuntimeAndNullGeometryGuardsCannotBeEnabledByClaimedPacket() {
            foreach(var path in new[]{"canStart","runtimeSupported","actor.nativeSupported","definition.canStart","definition.runtimeSupported"}) {
                var p=Packet();p.SelectToken(path).Replace(true);
                Rehash((JObject)p["definition"],"definitionHash");p["definitionHash"]=p["definition"]["definitionHash"].DeepClone();Rehash(p);
                Assert.Throws<ArgumentException>(()=>Read(p),path);
            }
            var geometry=Packet();geometry["geometry"]=new JObject();Rehash(geometry);Assert.Throws<ArgumentException>(()=>Read(geometry));
        }
        [Test] public void ActorOwnedCopyAndRefitBindingMustMatchOriginalSource() {
            var p=Packet();p["actor"]["owned"]["permanentStats"]["attack"]["numerator"]="999";Rehash(p);
            Assert.Throws<ArgumentException>(()=>Read(p));
            p=Packet("placed");p["actor"]["bcb"]["rank"]=2;Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p));
            p=Packet("placed");p["actor"]["placement"]["physicalSlot"]=0;Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p));
        }
        [Test] public void NestedUnknownFieldsAndMissingDependencyClaimsAreRejected() {
            var p=Packet();p["definition"]["ballistics"]["newPhysics"]=true;
            Rehash((JObject)p["definition"],"definitionHash");p["definitionHash"]=p["definition"]["definitionHash"].DeepClone();Rehash(p);
            Assert.Throws<ArgumentException>(()=>Read(p));
            p=Packet();p["missing"]=new JArray();Rehash(p);Assert.Throws<ArgumentException>(()=>Read(p));
        }
        [Test] public void OversizedOrMissingPacketIsRejectedBeforeUse() {
            Assert.Throws<ArgumentException>(()=>Read(null,new JObject(),"bad"));
            var p=Packet();p["oversized"]=new string('x',512*1024);Assert.Throws<ArgumentException>(()=>Read(p));
        }
        [Test] public void SuppliedHashesRevisionsAndMissingListAreDetachedAndUnchanged() {
            var p=Packet();var s=Read(p);Assert.That(Get(s,"Hash"),Is.EqualTo((string)p["packetHash"]));
            Assert.That(Get(s,"FormationRevision"),Is.EqualTo(1L));Assert.That(Get(s,"LoadoutRevision"),Is.EqualTo(1L));
            var missing=(string[])Get(s,"MissingSources");missing[0]="changed";
            Assert.That(((string[])Get(s,"MissingSources"))[0],Is.Not.EqualTo("changed"));
        }
        [Test] public void ServerPacketFixturesArePinnedToTheActuallyExecutedProducerFiles() {
            // Tests source parity, not operational HTTP/auth/START provenance.
            Read(Packet());
            foreach(var pin in (JArray)Fixture()["pins"]) {
                var path=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/",(string)pin["file"]));
                using(var sha=System.Security.Cryptography.SHA256.Create()) {
                    var hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
                    Assert.That(hash,Is.EqualTo((string)pin["sha256"]));
                }
            }
        }
    }
}
#endif