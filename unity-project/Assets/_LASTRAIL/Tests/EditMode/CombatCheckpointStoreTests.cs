using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Diagnostics;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class CombatCheckpointStoreTests
    {
        [Test] public void ExplicitLocalQaFileReopensOriginalStateAndPreservesCorruption()
        {
            var factory=typeof(CombatCheckpointStore).GetMethod("CreateExplicitLocalQaFile");
            Assert.NotNull(factory,"Explicit QA Player storage must have a named opt-in factory.");
            var path=Path.Combine(Path.GetTempPath(),"unity14-checkpoint-"+Guid.NewGuid().ToString("N"),"state.json");
            CombatCheckpointStore Open()=>(CombatCheckpointStore)factory.Invoke(null,new object[]{path});
            try{
                var state=Fresh();state.Advance(500000*3,3);
                Assert.AreEqual(1,Open().Save(Start(),state,0));
                var restored=Open().RestoreWithRevision(Start(),Fresh);
                Assert.AreEqual(state.CaptureLocalState(),restored.Simulation.CaptureLocalState());
                Assert.AreEqual(1,restored.Revision);
                var raw=File.ReadAllText(path);
                Assert.Throws<InvalidOperationException>(()=>Open().Restore(Start(key:"other_original_key"),Fresh));
                Assert.AreEqual(raw,File.ReadAllText(path));
                restored.Simulation.Advance(500000,1);
                Assert.AreEqual(2,Open().Save(Start(),restored.Simulation,1));
                File.WriteAllText(path,"corrupt");
                Assert.Catch(()=>Open().Save(Start(),Fresh(),0));
                Assert.AreEqual("corrupt",File.ReadAllText(path));
            }finally{
                if(File.Exists(path))File.Delete(path);
                if(File.Exists(path+".lock"))File.Delete(path+".lock");
                if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path));
            }
        }
        sealed class Crypto : IAndroidKeystoreNativeStoragePort
        {
            public bool FailEncrypt;
            readonly Dictionary<string,string> values=new Dictionary<string,string>();
            public bool IsAndroidRuntime=>true; public bool IsKeystoreAvailable=>true;
            public bool UsedPlayerPrefsFallback=>false; public string ProviderName=>"explicit_test_crypto";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string value){if(FailEncrypt)throw new IOException("test crypto failure");var id=Guid.NewGuid().ToString("N");values[id]=value;return new AndroidKeystoreEncryptedTokenPayload(id,"test_iv");}
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>values[p.CipherTextBase64];
            public bool Save(AndroidKeystoreEncryptedTokenPayload p)=>throw new NotSupportedException();
            public AndroidKeystoreEncryptedTokenPayload Load()=>throw new NotSupportedException();
            public bool Clear()=>throw new NotSupportedException();
        }
        static PendingRunOperation Start(string account="a",string key="opaque original/key:1234")=>PendingRunOperation.Start(account,RunStartRequest.Create("preview_fixture",key),new AppBuildSnapshot("1",1),DateTimeOffset.Parse("2026-09-06T00:00:00Z")).Confirmed("{\"accepted\":true,\"startStatus\":\"STARTED\",\"runId\":\"run_fixture\",\"operationPreviewId\":\"preview_fixture\",\"balanceVersion\":\"1\",\"serverStartedAt\":\"2026-09-06T00:00:00.000Z\",\"runVersionSnapshot\":null}");
        static BasicCombatSimulation Fresh()=>FreshSeed(1);
        static BasicCombatSimulation FreshSeed(uint seed){var d=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-producer/draft.json"));d["ownedSelection"]["accountId"]="a";return BasicCombatSimulation.FromPreparedProfileForLocalTest(d,(JObject)d["ownedSelection"],"a",seed);}
        static object Store(Crypto crypto,string path){var t=typeof(BasicCombatSimulation).Assembly.GetType("LastRail.Battle.CombatCheckpointStore");Assert.NotNull(t,"Missing run-bound checkpoint storage");return Activator.CreateInstance(t,crypto,path);}
        static object Call(object store,string name,params object[] args){try{return store.GetType().GetMethod(name).Invoke(store,args);}catch(TargetInvocationException e){throw e.InnerException;}}
        [Test] public void FileReloadPreservesStateAndRejectsStaleRevisionOrDifferentOriginalStart()
        {
            var dir=Path.Combine(Path.GetTempPath(),"lastrail-checkpoint-"+Guid.NewGuid().ToString("N"));var path=Path.Combine(dir,"state.bin");
            try{
                var crypto=new Crypto();var original=Fresh();original.Advance(500000*13,13);var start=Start();
                Assert.AreEqual(1L,Call(Store(crypto,path),"Save",start,original,0L));
                var restored=(BasicCombatSimulation)Call(Store(crypto,path),"Restore",start,(Func<BasicCombatSimulation>)Fresh);
                Assert.AreEqual(original.CaptureLocalState(),restored.CaptureLocalState());var bytes=File.ReadAllBytes(path);
                Assert.Throws<InvalidOperationException>(()=>Call(Store(crypto,path),"Save",start,original,0L));
                Assert.Throws<InvalidOperationException>(()=>Call(Store(crypto,path),"Restore",Start("b"),(Func<BasicCombatSimulation>)Fresh));
                Assert.Throws<InvalidOperationException>(()=>Call(Store(crypto,path),"Restore",Start(key:"different_original_key"),(Func<BasicCombatSimulation>)Fresh));
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
                var changed=FreshSeed(2);changed.Advance(500000*13,13);
                Assert.Throws<InvalidOperationException>(()=>Call(Store(crypto,path),"Save",start,changed,1L));
                CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
                restored.Advance(500000,1);Assert.AreEqual(2L,Call(Store(crypto,path),"Save",start,restored,1L));
                var checkpoint=Call(Store(crypto,path),"RestoreWithRevision",start,(Func<BasicCombatSimulation>)Fresh);
                Assert.AreEqual(2L,checkpoint.GetType().GetProperty("Revision").GetValue(checkpoint));
                var committed=File.ReadAllBytes(path);crypto.FailEncrypt=true;
                Assert.Throws<IOException>(()=>Call(Store(crypto,path),"Save",start,restored,2L));
                CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
            }finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".lock"))File.Delete(path+".lock");if(Directory.Exists(dir))Directory.Delete(dir);}
        }
        [Test] public void CorruptFileIsPreservedAndNeverReinitialized()
        {
            var path=Path.Combine(Path.GetTempPath(),"lastrail-checkpoint-"+Guid.NewGuid().ToString("N")+".bin");
            try{File.WriteAllText(path,"corrupted");var store=Store(new Crypto(),path);Assert.Catch(()=>Call(store,"Save",Start(),Fresh(),0L));Assert.AreEqual("corrupted",File.ReadAllText(path));}
            finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".lock"))File.Delete(path+".lock");}
        }
    }
}
