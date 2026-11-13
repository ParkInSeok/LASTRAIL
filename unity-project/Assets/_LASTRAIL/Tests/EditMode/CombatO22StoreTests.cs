using System;
using System.IO;
using System.Collections.Generic;
using LastRail.Auth;
using LastRail.Submissions;
using LastRail.Battle;
using LastRail.Run;
using NUnit.Framework;
public class CombatO22StoreTests
{
    sealed class TestCrypto:IAndroidKeystoreNativeStoragePort
    {
        readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"EXPLICIT_TEST_ONLY_CRYPTO";
        public string LastPlain;
        public AndroidKeystoreEncryptedTokenPayload Encrypt(string plain){LastPlain=plain;string id=Guid.NewGuid().ToString("N");values[id]=plain;return new AndroidKeystoreEncryptedTokenPayload(id,"test_iv");}
        public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>values[p.CipherTextBase64];
        public bool Save(AndroidKeystoreEncryptedTokenPayload p)=>throw new NotSupportedException();public AndroidKeystoreEncryptedTokenPayload Load()=>throw new NotSupportedException();public bool Clear()=>throw new NotSupportedException();
    }
    [Test]public void ActualFileReloadUsesDedicatedDomainAndPreservesDifferentDomainData()
    {
        string path=Path.GetFullPath("../LocalTemp/o22-file-"+Guid.NewGuid().ToString("N")+".json");
        var crypto=new TestCrypto();var original=CombatO22ApplicationOriginal.Read(CombatO22OriginalTests.Packet().ToString(),"account","run","grant","TEST",new string('a',64));
        var store=new CombatO22ApplicationStore(crypto,path);store.Commit(original);StringAssert.StartsWith("lastrail_o22_application_original_v1:",crypto.LastPlain);
        var saved=File.ReadAllBytes(path);new CombatO22ApplicationStore(crypto,path).Commit(original);CollectionAssert.AreEqual(saved,File.ReadAllBytes(path));
        Assert.AreEqual(original.Capture(),new CombatO22ApplicationStore(crypto,path).Load("account","run","grant","TEST",new string('a',64)).Capture());
        var otherDomain=new AndroidEncryptedReceiptFilePort(crypto,path);Assert.IsTrue(otherDomain.Save(otherDomain.Encrypt("wrong-domain")));var other=File.ReadAllBytes(path);
        Assert.Throws<FormatException>(()=>store.Commit(original));CollectionAssert.AreEqual(other,File.ReadAllBytes(path));
    }
    [Test]public void OriginalSaveLostAcknowledgementReplaysWithoutOverwrite()
    {
        var type=typeof(CombatVitals).Assembly.GetType("LastRail.Run.CombatO22ApplicationStore");Assert.IsNotNull(type);
        string path=Path.GetFullPath("../LocalTemp/o22-store-"+Guid.NewGuid().ToString("N")+".json"),saved=null;int writes=0;bool lose=true;
        Func<string> read=()=>saved;Action<string> write=s=>{saved=s;writes++;if(lose)throw new IOException("ack lost");};
        var store=Activator.CreateInstance(type,new object[]{path,read,write});
        var original=CombatO22ApplicationOriginal.Read(CombatO22OriginalTests.Packet().ToString(),"account","run","grant","TEST",new string('a',64));
        var commit=type.GetMethod("Commit");Assert.Throws<System.Reflection.TargetInvocationException>(()=>commit.Invoke(store,new object[]{original}));
        lose=false;commit.Invoke(store,new object[]{original});Assert.AreEqual(1,writes);
        var restored=(CombatO22ApplicationOriginal)type.GetMethod("Load").Invoke(store,new object[]{"account","run","grant","TEST",new string('a',64)});
        Assert.AreEqual(original.Capture(),restored.Capture());Assert.IsFalse(restored.Applied);
        Assert.Throws<System.Reflection.TargetInvocationException>(()=>type.GetMethod("Load").Invoke(store,new object[]{"another","run","grant","TEST",new string('a',64)}));
        Assert.AreEqual(1,writes);
    }
}
