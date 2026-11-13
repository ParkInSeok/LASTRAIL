using System;
using System.IO;
using System.Text;
using LastRail.Auth;
using LastRail.Submissions;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class StorageEnvelopeTests
    {
        const string Qa = "D:/Project/LASTRAIL/QA/CommanderS1-20260922/StorageEnvelope810";
        // Explicit base64 test double: file IO is real; cryptography and Android are not.
        sealed class Base64Only : IAndroidKeystoreNativeStoragePort
        {
            public string Plaintext;
            public bool IsAndroidRuntime => true;
            public bool IsKeystoreAvailable => true;
            public bool UsedPlayerPrefsFallback => false;
            public string ProviderName => "QA_BASE64_NOT_ENCRYPTION";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string value) { Plaintext=value; return new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(value)),"fixture"); }
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload value) => Encoding.UTF8.GetString(Convert.FromBase64String(value.CipherTextBase64));
            public AndroidKeystoreEncryptedTokenPayload Load() => throw new InvalidOperationException("Auth storage must not be used.");
            public bool Save(AndroidKeystoreEncryptedTokenPayload value) => throw new InvalidOperationException("Auth storage must not be used.");
            public bool Clear() => throw new InvalidOperationException("Auth storage must not be used.");
        }
        string path;
        [SetUp] public void Setup() { Directory.CreateDirectory(Qa); path=Path.Combine(Qa,Guid.NewGuid().ToString("N")+".test.enc"); }
        [TearDown] public void Cleanup() { if(File.Exists(path)) File.Delete(path); }
        static int Bytes(string text) => Encoding.UTF8.GetByteCount(text);
        [TestCase(unchecked((int)0x80070070),SecureStorageWriteFailure.DiskFull)]
        [TestCase(unchecked((int)0x80070027),SecureStorageWriteFailure.DiskFull)]
        [TestCase(unchecked((int)0x80070020),SecureStorageWriteFailure.Unknown)]
        public void OnlyKnownNativeDiskFullCodesAreClassified(int code,SecureStorageWriteFailure expected)
        { Assert.AreEqual(expected,AndroidEncryptedReceiptFilePort.ClassifyWriteFailure(new IOException("disk full text is not evidence",code))); }
        [Test] public void AccessFailureIsNotDiskFull()
        { Assert.AreEqual(SecureStorageWriteFailure.AccessDenied,AndroidEncryptedReceiptFilePort.ClassifyWriteFailure(new UnauthorizedAccessException("fixture"))); }
        static JObject Fixture(int version) => JObject.Parse(File.ReadAllText(version==8?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json":"D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReferenceDefinitions809/source.json"));
        static string Lookup(JObject start,string run) => new JObject { ["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=run,["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null }.ToString(Formatting.None);
        static PendingAccountAction Action(string account,string preview,string key="original-key") => new PendingAccountAction(account,"departure-start",preview,0,key,new JObject{["operationPreviewId"]=preview}.ToString(Formatting.None));

        [TestCase(524288,true)] [TestCase(524289,false)]
        public void ActualFileEnvelopeBoundaryPreservesPreviousBytes(int size,bool accepted)
        {
            var port=new AndroidEncryptedReceiptFilePort(new Base64Only(),path);
            Assert.IsTrue(port.Save(port.Encrypt("original_pending"))); var before=File.ReadAllBytes(path);
            int overhead=Bytes(AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(new AndroidKeystoreEncryptedTokenPayload("x","fixture")))-1;
            var candidate=new AndroidKeystoreEncryptedTokenPayload(new string('A',size-overhead),"fixture");
            Assert.AreEqual(size,Bytes(AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(candidate)));
            Assert.AreEqual(accepted,port.Save(candidate));
            Assert.AreEqual(accepted?SecureStorageWriteFailure.None:SecureStorageWriteFailure.LogicalLimit,port.LastWriteFailure);
            if(accepted) { Assert.AreEqual(size,new FileInfo(path).Length); Assert.AreEqual(candidate.CipherTextBase64,port.Load().CipherTextBase64); }
            else Assert.AreEqual(before,File.ReadAllBytes(path));
            Assert.IsEmpty(Directory.GetFiles(Qa,Path.GetFileName(path)+".*.tmp"));
        }

        [TestCase("oversize")] [TestCase("corrupt")]
        public void InvalidDiskRecordIsNotEmptyAndCannotBeOverwrittenByJournal(string fault)
        {
            File.WriteAllText(path,fault=="oversize"?new string('a',524289):"corrupt-record",new UTF8Encoding(false));
            var before=File.ReadAllBytes(path);
            var journal=new AccountActionJournal(new AndroidEncryptedReceiptFilePort(new Base64Only(),path));
            Assert.Catch(()=>journal.Read("account_test"));
            Assert.Catch(()=>journal.Save(Action("account_test","preview_a")));
            Assert.AreEqual(before,File.ReadAllBytes(path));
        }

        [TestCase(7)][TestCase(8)]
        public void ActualSerializersMeasureAggregateCapacityAndPreserveRejectedPending(int version)
        {
            var fixture=Fixture(version);var start=(JObject)fixture["originalStart"];
            string preview=(string)start["operationPreviewId"], response=Lookup(start,(string)fixture["runId"]);
            var crypto=new Base64Only();var port=new AndroidEncryptedReceiptFilePort(crypto,path);var journal=new AccountActionJournal(port);
            var measurements=new JArray();int confirmed=0;
            for(int i=0;i<32;i++)
            {
                var action=Action("account_"+i,preview);
                Assert.IsTrue(journal.Save(action));var before=File.ReadAllBytes(path);
                bool accepted=journal.ConfirmStart(action,response);
                int plaintextBytes=Bytes(crypto.Plaintext);
                measurements.Add(new JObject { ["attemptedRows"]=i+1,["accepted"]=accepted,["domainAndJournalUtf8Bytes"]=plaintextBytes,
                    ["base64OnlyFileBytes"]=Bytes(AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(crypto.Encrypt(crypto.Plaintext))),
                    ["aesGcm16ByteTag12ByteIvEnvelopeEstimateBytes"]=Bytes(AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(new byte[plaintextBytes+16]),Convert.ToBase64String(new byte[12])))) });
                var restored=new AccountActionJournal(new AndroidEncryptedReceiptFilePort(new Base64Only(),path)).Read(action.Account);
                Assert.AreEqual(action.Key,restored.Key);Assert.AreEqual(action.Payload,restored.Payload);
                if(accepted) { confirmed++;Assert.AreEqual(response,restored.StartResultJson); }
                else { Assert.IsNull(restored.StartResultJson);Assert.AreEqual(before,File.ReadAllBytes(path));break; }
            }
            Assert.Greater(confirmed,0);Assert.Less(confirmed,32);
            File.WriteAllText(Path.Combine(version==8?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811":Qa,"serializer-measurements.json"),new JObject {
                ["platform"]="Windows Unity Editor",["crypto"]="BASE64 TEST DOUBLE; AES size is estimate only",["androidDeviceCount"]=0,
                ["startUtf8Bytes"]=Bytes(start.ToString(Formatting.None)),["lookupUtf8Bytes"]=Bytes(response),["lookupUtf16CodeUnits"]=response.Length,
                ["lookupAsJsonStringUtf8Bytes"]=Bytes(new JValue(response).ToString(Formatting.None)),["lookupAsJsonStringUtf16CodeUnits"]=new JValue(response).ToString(Formatting.None).Length,
                ["fileLimitBytes"]=524288,["rowLimit"]=32,["confirmedRowsForThisFixtureOnly"]=confirmed,["attempts"]=measurements,
                ["maximumEquipmentEffectsVerified"]=false,["nativeEncryptionNewProcessVerified"]=false
            }.ToString(),new UTF8Encoding(false));
        }

        [TestCase("한",20000)] [TestCase("\\",20000)] [TestCase("\"",20000)]
        [TestCase("\u0085",8000)] [TestCase("\u2028",8000)] [TestCase("\u2029",8000)]
        public void AllowedPayloadTextAndPrintableKeyAreRetainedWithoutNormalization(string text,int count)
        {
            var start=new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]="run_a",["operationPreviewId"]="preview_a",["fixtureText"]=string.Concat(System.Linq.Enumerable.Repeat(text,count))};
            var response=Lookup(start,"run_a");Assert.LessOrEqual(Bytes(response),65536);
            var action=Action(new string('a',128),"preview_a",new string('\\',64)+new string('"',64));
            var journal=new AccountActionJournal(new AndroidEncryptedReceiptFilePort(new Base64Only(),path));
            Assert.IsTrue(journal.Save(action));Assert.IsTrue(journal.ConfirmStart(action,response));
            var restored=journal.Read(action.Account);Assert.AreEqual(response,restored.StartResultJson);Assert.AreEqual(action.Key,restored.Key);
        }
    }
}
