using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    // Synthetic future-connection observations, NOT an active Producer or a runtime fix.
    // Reflection is confined to this Editor test: production has no carriage command yet.
    public sealed class CombatDamageCheckpointConnectionTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Event="run-a:carriage:test:1";
        string output;
        static BasicCombatSimulation Fresh()
        {
            var train=new BasicCombatActor("train",false,true,false,0,1000,0,0,100,0,0,BasicCombatTarget.EnemyAny,0);
            var car=new BasicCombatActor("car",false,false,false,1,500,0,0,100,0,0,BasicCombatTarget.EnemyAny,0,new BasicCombatTiming(0,100,"Combat",false,null));
            var enemy=new BasicCombatActor("enemy",true,false,false,0,1000,0,0,100,0,0,BasicCombatTarget.Train,0);
            return new BasicCombatSimulation("TEST_ONLY_checkpoint_connection",500000,1u,new[]{train,car},new[]{new[]{enemy}});
        }
        static CombatDamageRouter BorrowActualVitals(BasicCombatSimulation model)
        {
            var actors=(IEnumerable)typeof(BasicCombatSimulation).GetField("allies",Private).GetValue(model);
            CombatVitals train=null;var cars=new Dictionary<string,CombatVitals>();
            foreach(var actor in actors)
            {
                var type=actor.GetType();var config=(BasicCombatActor)type.GetField("Config",Private).GetValue(actor);
                var vitals=(CombatVitals)type.GetProperty("Vitals",Private).GetValue(actor);
                if(config.Train)train=vitals;else if(config.Timing?.Device!=null)cars.Add(config.Id,vitals);
            }
            return new CombatDamageRouter("train",train,cars,16);
        }
        static CombatDamageApplication Apply(CombatDamageRouter router,int amount=100)=>router.ApplyCarriageDamage(Event,"car",amount,0,false);
        static CombatDamageApplication ThroughExistingChange(CombatSceneDriver driver,Func<BasicCombatSimulation,CombatDamageApplication> command)
        {
            try{return (CombatDamageApplication)typeof(CombatSceneDriver).GetMethod("Change",Private).MakeGenericMethod(typeof(CombatDamageApplication)).Invoke(driver,new object[]{command,true});}
            catch(TargetInvocationException error)when(error.InnerException!=null){ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
        }
        sealed class MemorySavePort
        {
            public string Saved { get; private set; }
            public long Revision { get; private set; }
            public int SaveCalls { get; private set; }
            public bool FailBeforeCommit,FailAfterCommit;
            public CombatSceneDriver Driver { get; }
            public MemorySavePort(){Driver=new CombatSceneDriver(Fresh,Load,Save,()=>true);Driver.Open();}
            RestoredCombatCheckpoint Load()=>Saved==null?null:(RestoredCombatCheckpoint)Activator.CreateInstance(typeof(RestoredCombatCheckpoint),Private,null,new object[]{BasicCombatSimulation.RestoreLocalState(Saved,Fresh),Revision},null);
            long Save(BasicCombatSimulation model,long expected)
            {
                SaveCalls++;
                if(expected!=Revision)throw new InvalidOperationException("Synthetic revision conflict");
                if(FailBeforeCommit)throw new IOException("Synthetic failure before save");
                Saved=model.CaptureLocalState();Revision++;
                if(FailAfterCommit)throw new IOException("Synthetic committed write with lost acknowledgement");
                return Revision;
            }
            public CombatDamageApplication ApplyWithNewRouter(int amount=100)=>ThroughExistingChange(Driver,m=>Apply(BorrowActualVitals(m),amount));
        }
        static JObject State(BasicCombatSimulation model)=>JObject.Parse(model.CaptureLocalState());
        static JObject State(CombatSceneDriver driver)=>JObject.Parse(driver.StateJson);
        static int Hp(JObject state,string id)=>(int)((JArray)state["actors"]).Single(a=>(string)a["id"]==id)["vitals"]["hp"];
        static void HpEquals(JObject state,int train,int car){Assert.AreEqual(train,Hp(state,"train"));Assert.AreEqual(car,Hp(state,"car"));}
        [OneTimeSetUp] public void BeginEvidence()
        {
            string requested=Environment.GetEnvironmentVariable("LASTRAIL_DAMAGE_REPLAY_QA_OUTPUT");
            if(string.IsNullOrEmpty(requested))return;
            output=Path.GetFullPath(requested);
            string allowed=Path.GetFullPath("D:/Project/LASTRAIL/QA/CarriageDamage-20260914/Unity/CheckpointReplay/");
            Assert.IsTrue(output.StartsWith(allowed,StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(Directory.Exists(output),"Fresh QA output required");Directory.CreateDirectory(output);
        }
        void Evidence(string name,JObject record)
        {
            if(output==null)return;
            record["scope"]="Synthetic future connection; real Actor.Vitals/clone/restore/driver, reflection-only command bridge, memory save fault injection; current production remains UNSUPPORTED";
            File.WriteAllText(Path.Combine(output,name+".json"),record.ToString(Formatting.Indented)+"\n");
        }
        [Test] public void SameRouterLifetimePreservesIdempotencyBaseline()
        {
            var model=Fresh();var router=BorrowActualVitals(model);var first=Apply(router);var once=State(model);var retry=Apply(router);
            Assert.IsTrue(first.Applied);Assert.IsFalse(retry.Applied);Assert.AreSame(first.Receipt,retry.Receipt);HpEquals(State(model),970,400);
            Assert.IsTrue(JToken.DeepEquals(once,State(model)));
            Evidence("01-same-router-baseline",new JObject{["first"]=JObject.FromObject(first),["retry"]=JObject.FromObject(retry),["once"]=once,["afterRetry"]=State(model)});
        }
        [Test] public void ExistingSnapshotPlusNewRouterReappliesAnUnpersistedEvent()
        {
            var model=Fresh();var first=Apply(BorrowActualVitals(model));var saved=State(model);
            var restored=BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(),Fresh);HpEquals(State(restored),970,400);
            var retry=Apply(BorrowActualVitals(restored));Assert.IsTrue(retry.Applied);HpEquals(State(restored),940,300);
            Evidence("02-snapshot-only-replay-risk",new JObject{["first"]=JObject.FromObject(first),["saved"]=saved,["retry"]=JObject.FromObject(retry),["afterRetry"]=State(restored),["observation"]="HP restored; new router has no event ledger"});
        }
        [Test] public void NaivePerCloneRouterCommitsTheSameEventTwice()
        {
            var port=new MemorySavePort();var first=port.ApplyWithNewRouter();var once=State(port.Driver);var retry=port.ApplyWithNewRouter();
            Assert.IsTrue(first.Applied);Assert.IsTrue(retry.Applied);HpEquals(once,970,400);HpEquals(State(port.Driver),940,300);Assert.AreEqual(3,port.SaveCalls);
            Evidence("03-driver-new-router-each-clone",new JObject{["first"]=JObject.FromObject(first),["retry"]=JObject.FromObject(retry),["once"]=once,["afterRetry"]=State(port.Driver),["saved"]=JObject.Parse(port.Saved),["saveCalls"]=port.SaveCalls});
        }
        [Test] public void CachingRouterOutsideCandidateWouldBypassSaveBoundary()
        {
            var port=new MemorySavePort();var live=(BasicCombatSimulation)typeof(CombatSceneDriver).GetField("current",Private).GetValue(port.Driver);
            var wronglyCached=BorrowActualVitals(live);var savedBefore=JObject.Parse(port.Saved);
            var result=ThroughExistingChange(port.Driver,ignoredCandidate=>Apply(wronglyCached));
            Assert.IsTrue(result.Applied);HpEquals(State(port.Driver),970,400);HpEquals(JObject.Parse(port.Saved),1000,500);Assert.AreEqual(1,port.SaveCalls);
            Evidence("04-wrong-live-binding-risk",new JObject{["result"]=JObject.FromObject(result),["savedBefore"]=savedBefore,["publishedCurrent"]=State(port.Driver),["savedAfter"]=JObject.Parse(port.Saved),["saveCalls"]=port.SaveCalls,["observation"]="Deliberately wrong QA bridge mutated prior live vitals instead of candidate; not a supported production call"});
        }
        [Test] public void PreCommitFailureKeepsPublishedAndSavedHpUnchanged()
        {
            var port=new MemorySavePort();var before=State(port.Driver);port.FailBeforeCommit=true;
            Assert.Throws<IOException>(()=>port.ApplyWithNewRouter());Assert.IsTrue(port.Driver.RecoveryRequired);
            Assert.IsTrue(JToken.DeepEquals(before,State(port.Driver)));Assert.IsTrue(JToken.DeepEquals(before,JObject.Parse(port.Saved)));
            Assert.Throws<InvalidOperationException>(()=>port.ApplyWithNewRouter());
            var failed=State(port.Driver);port.FailBeforeCommit=false;port.Driver.Open();var retry=port.ApplyWithNewRouter();
            Assert.IsTrue(retry.Applied);HpEquals(State(port.Driver),970,400);
            Evidence("05-before-commit-failure",new JObject{["before"]=before,["afterFailure"]=failed,["retry"]=JObject.FromObject(retry),["afterRecoveryRetry"]=State(port.Driver)});
        }
        [Test] public void LostSaveAcknowledgementRequiresLedgerOnReload()
        {
            var port=new MemorySavePort();var before=State(port.Driver);port.FailAfterCommit=true;
            Assert.Throws<IOException>(()=>port.ApplyWithNewRouter());Assert.IsTrue(port.Driver.RecoveryRequired);
            Assert.IsTrue(JToken.DeepEquals(before,State(port.Driver)));var committed=JObject.Parse(port.Saved);HpEquals(committed,970,400);
            port.FailAfterCommit=false;port.Driver.Open();var loaded=State(port.Driver);HpEquals(loaded,970,400);
            var retry=port.ApplyWithNewRouter();Assert.IsTrue(retry.Applied);HpEquals(State(port.Driver),940,300);
            Evidence("06-lost-save-ack-replay-risk",new JObject{["publishedBefore"]=before,["committedButUnacknowledged"]=committed,["afterReload"]=loaded,["retry"]=JObject.FromObject(retry),["afterRetry"]=State(port.Driver)});
        }
        [Test] public void LedgerOnlyEventMustParticipateInCheckpointChangeDetection()
        {
            var port=new MemorySavePort();var before=State(port.Driver);var zero=port.ApplyWithNewRouter(0);
            Assert.IsTrue(zero.Applied);Assert.AreEqual(1,port.SaveCalls);Assert.IsTrue(JToken.DeepEquals(before,State(port.Driver)));
            var changedPayload=port.ApplyWithNewRouter(100);Assert.IsTrue(changedPayload.Applied);HpEquals(State(port.Driver),970,400);
            Evidence("07-ledger-only-fast-path-risk",new JObject{["zeroEvent"]=JObject.FromObject(zero),["before"]=before,["changedPayloadAccepted"]=JObject.FromObject(changedPayload),["afterChangedPayload"]=State(port.Driver),["observation"]="New router lost even zero-loss event; existing snapshot fast path has no ledger field"});
        }
        [Test] public void ExistingSchemaRejectsUnversionedLedgerAddition()
        {
            var saved=State(Fresh());saved["damageLedger"]=new JObject{["version"]="TEST_ONLY_not_supported",["events"]=new JArray()};
            var error=Assert.Throws<ArgumentException>(()=>BasicCombatSimulation.RestoreLocalState(saved.ToString(),Fresh));
            Evidence("08-unversioned-field-rejected",new JObject{["input"]=saved,["rejected"]=true,["error"]=error.Message});
        }
        [OneTimeTearDown] public void FinishEvidence()
        {
            if(output==null)return;
            using(var sha=SHA256.Create())
            {
                var files=new JArray(Directory.GetFiles(output,"*.json").OrderBy(p=>p,StringComparer.Ordinal).Select(p=>new JObject{["file"]=Path.GetFileName(p),["sha256"]=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant()}));
                File.WriteAllText(Path.Combine(output,"manifest.json"),new JObject{["scope"]="Observed synthetic connection behavior, not remediation PASS or active production defect",["caseFiles"]=files}.ToString(Formatting.Indented)+"\n");
            }
        }
    }
}
