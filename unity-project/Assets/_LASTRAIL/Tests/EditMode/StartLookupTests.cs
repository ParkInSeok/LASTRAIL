using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class StartLookupTests
    {
        [TestCase(SecureStorageWriteFailure.DiskFull,"receipt_storage_disk_full")]
        [TestCase(SecureStorageWriteFailure.LogicalLimit,"receipt_storage_limit")]
        [TestCase(SecureStorageWriteFailure.AccessDenied,"receipt_storage_access_denied")]
        [TestCase(SecureStorageWriteFailure.Unknown,"receipt_storage_unavailable")]
        public async Task ClassifiedStorageFailureRetainsOriginalAndRetriesWithoutHttp(SecureStorageWriteFailure reason,string code){
            var port=new Port();var journal=new AccountActionJournal(port);var original=Action();Assert.IsTrue(journal.Save(original));
            var before=port.Load().CipherTextBase64;port.Fail=true;port.Failure=reason;
            var session=Session();var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Committed()))};
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.LookupDepartureStartAsync();Assert.AreEqual(code,state.DepartureLookupError);Assert.IsTrue(state.DepartureReceiptNeedsSave);
                Assert.AreEqual(before,port.Load().CipherTextBase64);Assert.AreEqual(original.Key,journal.Read(original.Account).Key);
                port.Fail=false;await state.RetryPendingAsync();Assert.AreEqual(1,wire.Calls);Assert.IsFalse(state.DepartureReceiptNeedsSave);
                Assert.AreEqual("run_a",state.KnownDepartureRunId);Assert.IsNull(state.DepartureLookupError);
            }
        }
        [TestCase(7)][TestCase(8)] public void OriginalSurvivesJournalReloadWithoutExpandingReferences(int version){
            var e=JObject.Parse(System.IO.File.ReadAllText(version==8?"D:/Project/LASTRAIL/QA/CommanderS1-20260922/StaticReference811/source.json":"D:/Project/LASTRAIL/QA/CommanderS1-20260922/ReferenceDefinitions809/source.json"));var start=(JObject)e["originalStart"];
            string account=(string)e["accountId"],preview=(string)start["operationPreviewId"];
            var original=new PendingAccountAction(account,"departure-start",preview,0,"v7-storage",new JObject{["operationPreviewId"]=preview}.ToString(Newtonsoft.Json.Formatting.None));
            var response=new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=e["runId"].DeepClone(),["runStatus"]="ACTIVE",["startResponse"]=start,["errorCode"]=null}.ToString(Newtonsoft.Json.Formatting.None);
            Assert.LessOrEqual(System.Text.Encoding.UTF8.GetByteCount(response),65536);
            Assert.LessOrEqual(new JValue(response).ToString(Newtonsoft.Json.Formatting.None).Length,65536);
            var port=new Port();var journal=new AccountActionJournal(port);Assert.IsTrue(journal.Save(original));Assert.IsTrue(journal.ConfirmStart(original,response));
            var restored=new AccountActionJournal(port).Read(account);Assert.AreEqual(response,restored.StartResultJson);
            var input=new DepartureStartResult(200,restored.StartResultJson,preview).ReadOrdinaryInput(account);
            Assert.AreEqual("combat.first-battle-encounter-source.v"+version+".20261028",input.FirstBattle.SourceVersion);Assert.IsNull(input.FirstBattle.Definition["scaling"]["variants"]);
        }
        static string Committed(string run="run_a",string preview="preview_a")=>new JObject{["accepted"]=true,["lookupStatus"]="COMMITTED",["runId"]=run,["runStatus"]="ACTIVE",["startResponse"]=new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]=run,["operationPreviewId"]=preview,["originalTime"]="2000-01-01T00:00:00.000Z"},["errorCode"]=null}.ToString();
        static PendingAccountAction Action()=>new PendingAccountAction("account_test","departure-start","preview_a",0,"original:key/+==","{\"operationPreviewId\":\"preview_a\"}");
        sealed class Port:IAndroidKeystoreNativeStoragePort,ISecureStorageWriteDiagnostics
        {
            public SecureStorageWriteFailure Failure=SecureStorageWriteFailure.Unknown;
            public SecureStorageWriteFailure LastWriteFailure=>Fail?Failure:SecureStorageWriteFailure.None;
            public bool Fail,FailRead;public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public string ProviderName=>"test-only";public bool UsedPlayerPrefsFallback=>false;AndroidKeystoreEncryptedTokenPayload saved;
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string s)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)),"fixture");
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p.CipherTextBase64));public bool Save(AndroidKeystoreEncryptedTokenPayload p){if(Fail)return false;saved=p;return true;}public AndroidKeystoreEncryptedTokenPayload Load(){if(FailRead)throw new System.IO.IOException("Injected read failure");return saved;}public bool Clear(){saved=null;return true;}
        }
        sealed class Wire:IRunApiHttpTransport
        {public int Calls;public RunApiHttpRequest Last;public Func<Task<RunApiHttpResponse>> Reply;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Calls++;Last=r;return Reply();}}
        static GameApiAuthSessionSnapshot Session()=>new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.local"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("local"),DateTime.UtcNow.AddMinutes(10));
        static string Utf8SizedReceipt(int bytes){
            var p=JObject.Parse(Committed());p["startResponse"]["sizeBoundaryFixture"]="";
            int remaining=bytes-System.Text.Encoding.UTF8.GetByteCount(p.ToString(Newtonsoft.Json.Formatting.None));
            p["startResponse"]["sizeBoundaryFixture"]=new string('한',remaining/3)+new string('a',remaining%3);
            var result=p.ToString(Newtonsoft.Json.Formatting.None);Assert.AreEqual(bytes,System.Text.Encoding.UTF8.GetByteCount(result));return result;
        }
        [TestCase(65536,true)][TestCase(65537,true)][TestCase(131072,true)][TestCase(131073,false)]
        public void Utf8BudgetAppliesBeforeConfirmWriteWithoutReplacingPending(int bytes,bool accepted){
            var port=new Port();var journal=new AccountActionJournal(port);var original=Action();Assert.IsTrue(journal.Save(original));
            var before=port.Load().CipherTextBase64;var response=Utf8SizedReceipt(bytes);Assert.Less(response.Length,65536);
            Assert.AreEqual(accepted,journal.ConfirmStart(original,response));
            var restored=new AccountActionJournal(port).Read(original.Account);
            Assert.AreEqual(original.Key,restored.Key);Assert.AreEqual(original.Payload,restored.Payload);
            if(accepted)Assert.AreEqual(response,restored.StartResultJson);else{Assert.IsNull(restored.StartResultJson);Assert.AreEqual(before,port.Load().CipherTextBase64);}
        }
        [Test] public void Utf8BudgetAlsoProtectsDirectNewReceiptSave(){
            var action=Action();var response=Utf8SizedReceipt(131073);var port=new Port();
            var retained=new PendingAccountAction(action.Account,action.Kind,action.Target,action.Count,action.Key,action.Payload,response);
            Assert.IsFalse(new AccountActionJournal(port).Save(retained));Assert.IsNull(port.Load());
        }
        [TestCase(65537)][TestCase(131073)] public void HistoricalCharacterBoundReceiptStillReadsAndReplaysUnchanged(int bytes){
            var action=Action();var response=Utf8SizedReceipt(bytes);Assert.Less(response.Length,65536);var port=new Port();
            var historical=new JObject{["schema"]="account-actions-v1",["actions"]=new JArray(new JObject{
                ["account"]=action.Account,["kind"]=action.Kind,["target"]=action.Target,["count"]=action.Count,["key"]=action.Key,["payload"]=action.Payload,["startResultJson"]=response})};
            port.Save(port.Encrypt(historical.ToString()));var before=port.Load().CipherTextBase64;
            var journal=new AccountActionJournal(port);Assert.AreEqual(response,journal.Read(action.Account).StartResultJson);
            Assert.IsTrue(journal.ConfirmStart(action,response));Assert.AreEqual(before,port.Load().CipherTextBase64);
        }
        static string EncodedSizedReceipt(int characters){
            var p=JObject.Parse(Committed());p["startResponse"]["sizeBoundaryFixture"]="";
            string json=p.ToString(Newtonsoft.Json.Formatting.None);int remaining=characters-new JValue(json).ToString(Newtonsoft.Json.Formatting.None).Length;
            p["startResponse"]["sizeBoundaryFixture"]=new string('\\',remaining/4)+new string('a',remaining%4);
            json=p.ToString(Newtonsoft.Json.Formatting.None);Assert.AreEqual(characters,new JValue(json).ToString(Newtonsoft.Json.Formatting.None).Length);return json;
        }
        [TestCase(65536,true)][TestCase(65537,true)][TestCase(70607,true)][TestCase(131072,true)][TestCase(131073,false)]
        public void EncodedStartReceiptBudgetRetainsExactBytesAndRejectsBeforeWrite(int characters,bool accepted){
            var action=Action();string response=EncodedSizedReceipt(characters);Assert.Less(System.Text.Encoding.UTF8.GetByteCount(response),131072);
            var port=new Port();var journal=new AccountActionJournal(port);Assert.IsTrue(journal.Save(action));var before=port.Load().CipherTextBase64;
            Assert.AreEqual(accepted,journal.ConfirmStart(action,response));var restored=new AccountActionJournal(port).Read(action.Account);
            if(accepted){Assert.AreEqual(response,restored.StartResultJson);Assert.AreEqual(action.Key,restored.Key);}else{Assert.IsNull(restored.StartResultJson);Assert.AreEqual(before,port.Load().CipherTextBase64);}
        }
        [Test] public void OnlyVerifiedCommittedOriginalResponseCanBecomeDurable()
        {
            var port=new Port();var store=new AccountActionJournal(port);var action=Action();Assert.That(store.Save(action),Is.True);Assert.That(store.ConfirmStart(action,Committed()),Is.True);
            var restored=new AccountActionJournal(port).Read(action.Account);Assert.That(restored.Key,Is.EqualTo(action.Key));Assert.That(restored.Payload,Is.EqualTo(action.Payload));Assert.That(restored.StartResultJson,Is.Not.Null);
            Assert.That(store.ConfirmStart(action,Committed("different")),Is.False);Assert.That(new DepartureStartResult(200,store.Read(action.Account).StartResultJson,action.Target).RunId,Is.EqualTo("run_a"));
            Assert.Throws<FormatException>(()=>new DepartureStartResult(200,Committed("run_a","different"),"preview_a"));
            var empty=new JObject{["accepted"]=true,["lookupStatus"]="NOT_FOUND",["runId"]=null,["runStatus"]=null,["startResponse"]=null,["errorCode"]=null};Assert.Throws<FormatException>(()=>new PendingAccountAction(action.Account,action.Kind,action.Target,0,action.Key,action.Payload,empty.ToString()));
        }
        [Test] public async Task LookupUsesOriginalBodyKeyWithoutHeaderOrCombatReadinessAndSurvivesSaveFailure()
        {
            var session=Session();var port=new Port();var store=new AccountActionJournal(port);var action=Action();store.Save(action);port.Fail=true;var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Committed()))};
            using(var state=new AccountScreenRuntime(wire,store,()=>session,()=>1)){
                await state.LookupDepartureStartAsync();Assert.That(wire.Last.EndpointPath,Is.EqualTo("/v1/runs/start-result"));Assert.That(wire.Last.Headers.Any(h=>h.Name=="Idempotency-Key"),Is.False);Assert.That((string)JObject.Parse(wire.Last.BodyJson)["startIdempotencyKey"],Is.EqualTo(action.Key));
                Assert.That(state.DepartureLookupError,Is.EqualTo("receipt_storage_unavailable"));Assert.That(state.KnownDepartureRunId,Is.Null);Assert.That(store.Read(action.Account).StartResultJson,Is.Null);
                port.Fail=false;await state.LookupDepartureStartAsync();Assert.That(state.KnownDepartureRunId,Is.EqualTo("run_a"));Assert.That(store.Read(action.Account).StartResultJson,Is.Not.Null);Assert.That(state.CanConfirmDeparture,Is.False);
                Assert.That(wire.Calls,Is.EqualTo(1),"Retry must persist the confirmed response without another HTTP request.");
            }
        }
        [Test] public async Task OldSessionLookupCannotSaveOrExposeReceipt()
        {
            var session=Session();long generation=1;var store=new AccountActionJournal(new Port());store.Save(Action());var completion=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=()=>completion.Task};
            using(var state=new AccountScreenRuntime(wire,store,()=>session,()=>generation)){
                var task=state.LookupDepartureStartAsync();generation++;session=Session();state.SynchronizeSession();completion.SetResult(RunApiHttpResponse.Success(200,Committed()));await task;
                Assert.That(store.Read("account_test").StartResultJson,Is.Null);Assert.That(state.DepartureStartResult,Is.Null);Assert.That(state.DepartureLookupLoading,Is.False);
            }
        }
        [Test] public async Task ReadFailureAfterConfirmedHttpRetainsResponseAndRetriesOnlyStorage()
        {
            var session=Session();var port=new Port();var store=new AccountActionJournal(port);store.Save(Action());
            var wire=new Wire{Reply=()=>{port.FailRead=true;return Task.FromResult(RunApiHttpResponse.Success(200,Committed()));}};
            using(var state=new AccountScreenRuntime(wire,store,()=>session,()=>1)){
                await state.LookupDepartureStartAsync();Assert.IsTrue(state.DepartureReceiptNeedsSave);Assert.AreEqual("COMMITTED",state.DepartureLookupStatus);
                Assert.IsNull(state.KnownDepartureRunId);Assert.AreEqual("receipt_storage_unavailable",state.DepartureLookupError);
                await state.LookupDepartureStartAsync();Assert.AreEqual(1,wire.Calls);Assert.IsTrue(state.DepartureReceiptNeedsSave);
                port.FailRead=false;await state.LookupDepartureStartAsync();Assert.AreEqual(1,wire.Calls);Assert.IsFalse(state.DepartureReceiptNeedsSave);
                Assert.AreEqual("run_a",state.KnownDepartureRunId);Assert.IsNotNull(store.Read(Action().Account).StartResultJson);
            }
        }
        [Test] public async Task AccountGenerationChangeDiscardsUnsavedResponseWithoutWritingIt()
        {
            var session=Session();long generation=1;var port=new Port();var store=new AccountActionJournal(port);store.Save(Action());port.Fail=true;
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Committed()))};
            using(var state=new AccountScreenRuntime(wire,store,()=>session,()=>generation)){
                await state.LookupDepartureStartAsync();Assert.IsTrue(state.DepartureReceiptNeedsSave);
                generation++;session=Session();state.SynchronizeSession();Assert.IsFalse(state.DepartureReceiptNeedsSave);
                Assert.IsNull(state.KnownDepartureRunId);Assert.IsNull(store.Read(Action().Account).StartResultJson);Assert.AreEqual(1,wire.Calls);
            }
        }
        [Test] public async Task SameAccountCredentialRotationRetainsConfirmedResponseForStorageRetry()
        {
            var session=Session();var port=new Port();var store=new AccountActionJournal(port);store.Save(Action());port.Fail=true;
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Committed()))};
            using(var state=new AccountScreenRuntime(wire,store,()=>session,()=>1)){
                await state.LookupDepartureStartAsync();Assert.IsTrue(state.DepartureReceiptNeedsSave);
                session=Session();state.SynchronizeSession();Assert.IsTrue(state.DepartureReceiptNeedsSave);
                port.Fail=false;await state.LookupDepartureStartAsync();Assert.AreEqual(1,wire.Calls);
                Assert.AreEqual("run_a",state.KnownDepartureRunId);Assert.IsFalse(state.DepartureReceiptNeedsSave);
            }
        }
        [Test] public async Task PendingRetryAfterCredentialRotationAndReadFailurePersistsWithoutHttp()
        {
            var session=Session();var port=new Port();var store=new AccountActionJournal(port);store.Save(Action());port.Fail=true;
            var wire=new Wire{Reply=()=>Task.FromResult(RunApiHttpResponse.Success(200,Committed()))};
            using(var state=new AccountScreenRuntime(wire,store,()=>session,()=>1)){
                await state.LookupDepartureStartAsync();port.FailRead=true;session=Session();state.SynchronizeSession();
                Assert.IsTrue(state.DepartureReceiptNeedsSave);Assert.IsNull(state.Pending);
                Assert.IsTrue(state.HasDepartureLookupRequest);Assert.IsTrue(state.CanRetryDepartureLookup);
                port.FailRead=false;port.Fail=false;await state.RetryPendingAsync();
                Assert.IsFalse(state.DepartureReceiptNeedsSave);Assert.AreEqual("run_a",state.KnownDepartureRunId);Assert.AreEqual(1,wire.Calls);
            }
        }
        [Test] public void ConflictAndUnavailableMustNotDiscloseResultsOrMasqueradeAsSuccess()
        {
            var r=new JObject{["accepted"]=false,["lookupStatus"]="CONFLICT",["runId"]=null,["runStatus"]=null,["startResponse"]=null,["errorCode"]="start_preview_mismatch"};Assert.That(new DepartureStartResult(409,r.ToString(),"preview_a").Status,Is.EqualTo("CONFLICT"));r["runId"]="foreign";Assert.Throws<FormatException>(()=>new DepartureStartResult(409,r.ToString(),"preview_a"));r["runId"]=null;r["accepted"]=true;Assert.Throws<FormatException>(()=>new DepartureStartResult(409,r.ToString(),"preview_a"));
        }
    }
}
