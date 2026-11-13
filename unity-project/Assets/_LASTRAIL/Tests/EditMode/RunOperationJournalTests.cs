using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Submissions;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LastRail.Tests.EditMode
{
    public sealed class RunOperationJournalTests
    {
        private const string Account = "account_fixture", Key = "original_run_key_fixture", Preview = "preview_fixture";
        private const string Accepted = "{\"accepted\":true,\"startStatus\":\"STARTED\",\"runId\":\"run_fixture\",\"operationPreviewId\":\"preview_fixture\",\"balanceVersion\":\"1\",\"serverStartedAt\":\"2026-09-06T00:00:00.000Z\",\"runVersionSnapshot\":null}";
        private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-09-06T00:00:00.1234567Z");
        private sealed class Native : IAndroidKeystoreNativeStoragePort
        {
            public bool Available = true, FailSave;
            public int Saves, Clears, Loads;
            public string Plain;
            public AndroidKeystoreEncryptedTokenPayload Stored;
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public bool IsAndroidRuntime => Available;
            public bool IsKeystoreAvailable => Available;
            public bool UsedPlayerPrefsFallback => false;
            public string ProviderName => "explicit_run_test_crypto";
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string value)
            { Plain = value; var key = Guid.NewGuid().ToString("N"); values[key] = value; return new AndroidKeystoreEncryptedTokenPayload(key, "iv"); }
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload value) => values[value.CipherTextBase64];
            public AndroidKeystoreEncryptedTokenPayload Load() { Loads++; return Stored; }
            public bool Save(AndroidKeystoreEncryptedTokenPayload value) { Saves++; if (FailSave) return false; Stored = value; return true; }
            public bool Clear() { Clears++; Stored = null; return true; }
        }
        private static Type Required(string name)
        {
            var type = typeof(RunStartRequest).Assembly.GetType("LastRail.Run." + name);
            Assert.That(type, Is.Not.Null, "Missing durable run API: " + name); return type;
        }
        private static object Operation(string account = Account, string key = Key, string preview = Preview, long code = 10) =>
            Required("PendingRunOperation").GetMethod("Start").Invoke(null, new object[] {
                account, RunStartRequest.Create(preview, key), new AppBuildSnapshot("1.0", code), Time });
        private static object Journal(Native native) => Activator.CreateInstance(Required("EncryptedRunOperationJournal"), native);
        private static object Get(object item, string name) => item.GetType().GetProperty(name).GetValue(item);
        private static async Task<object> Call(object item, string method, params object[] args)
        { var task = (Task)item.GetType().GetMethod(method).Invoke(item, args); await task; return task.GetType().GetProperty("Result").GetValue(task); }
        private static async Task<object[]> Read(object journal, string account = Account) => ((IEnumerable)await Call(journal, "ReadAsync", account)).Cast<object>().ToArray();

        [Test] public async Task NewInstancePreservesOriginalBytesKeyBuildAndFullTimestamp()
        {
            var native = new Native(); var journal = Journal(native); var op = Operation();
            Assert.That(await Call(journal, "PutAsync", op), Is.True);
            var restored = (await Read(Journal(native))).Single();
            foreach (var field in new[] { "AccountId", "TargetId", "IdempotencyKey", "BodyJson", "ComposedAt", "EndpointPath" })
                Assert.That(Get(restored, field), Is.EqualTo(Get(op, field)), field);
            Assert.That(Get(Get(restored, "Build"), "AppVersionCode"), Is.EqualTo(10));
            Assert.That(Get(restored, "AcceptedResponseJson"), Is.Null);
            Assert.That(Required("PendingRunOperation").GetProperties().All(p => p.GetSetMethod() == null), Is.True);
            Assert.That(native.Plain, Does.Not.Contain("Authorization")); Assert.That(native.Plain, Does.Not.Contain("accessToken"));
        }
        [Test] public async Task TravelReplacementIsAtomicAndPendingSurvivesReopen(){
            var native=new Native();var journal=(IRunOperationJournal)Journal(native);var command=DurableRunClientTests.TravelCommand();
            var first=PendingRunOperation.Travel(Account,"run",command,new AppBuildSnapshot("test",1),Time);
            Assert.IsTrue(await journal.PutAsync(first));Assert.IsTrue(await journal.ConfirmAsync(first,DurableRunClientTests.TravelResponse(command)));
            var next=PendingRunOperation.Travel(Account,"run",DurableRunClientTests.TravelCommand("next",1),first.Build,Time);
            native.FailSave=true;Assert.IsFalse(await journal.PutAsync(next));
            var retained=(await ((IRunOperationJournal)Journal(native)).ReadAsync(Account)).Single();Assert.AreEqual(first.IdempotencyKey,retained.IdempotencyKey);Assert.NotNull(retained.AcceptedResponseJson);
            native.FailSave=false;Assert.IsTrue(await journal.PutAsync(next));
            retained=(await ((IRunOperationJournal)Journal(native)).ReadAsync(Account)).Single();Assert.AreEqual(next.BodyJson,retained.BodyJson);Assert.IsNull(retained.AcceptedResponseJson);
            Assert.IsFalse(await journal.PutAsync(PendingRunOperation.Travel(Account,"run",DurableRunClientTests.TravelCommand("third",2),first.Build,Time)));
        }
        [TestCase(true)] [TestCase(false)] public async Task DestinationReopensExactIntentReceiptAndReplacesOnlyConfirmedOlderDestination(bool initial){
            var native=new Native();var journal=(IRunOperationJournal)Journal(native);var intent=DurableRunClientTests.DestinationIntent("d");
            var factory=typeof(PendingRunOperation).GetMethod("ConfirmDestination");Assert.NotNull(factory);
            Func<BOperationDestinationIntent,PendingRunOperation> compose=i=>(PendingRunOperation)factory.Invoke(null,new object[]{i.AccountId,i,new AppBuildSnapshot("test",1),Time});
            var first=compose(intent);Assert.IsTrue(await journal.PutAsync(first));
            var pending=(await ((IRunOperationJournal)Journal(native)).ReadAsync(intent.AccountId)).Single();
            Assert.AreEqual(first.BodyJson,pending.BodyJson);Assert.AreEqual(intent.CommandId,pending.IdempotencyKey);
            var response=DurableRunClientTests.DestinationResponse(intent,initial);Assert.IsTrue(await journal.ConfirmAsync(pending,response));
            var confirmed=(await ((IRunOperationJournal)Journal(native)).ReadAsync(intent.AccountId)).Single();Assert.AreEqual(response,confirmed.AcceptedResponseJson);
            var restoredIntent=(BOperationDestinationIntent)typeof(PendingRunOperation).GetMethod("ReadDestinationIntent").Invoke(confirmed,null);
            Assert.IsTrue(JToken.DeepEquals(intent.Capture(),restoredIntent.Capture()));
            Assert.IsFalse(await journal.PutAsync(compose(DurableRunClientTests.DestinationIntent("stale",1))));
            var next=compose(DurableRunClientTests.DestinationIntent("next",3));native.FailSave=true;Assert.IsFalse(await journal.PutAsync(next));
            Assert.AreEqual(response,(await journal.ReadAsync(intent.AccountId)).Single().AcceptedResponseJson);
            native.FailSave=false;Assert.IsTrue(await journal.PutAsync(next));Assert.AreEqual("next",(await journal.ReadAsync(intent.AccountId)).Single().IdempotencyKey);
            Assert.IsFalse(await journal.PutAsync(compose(DurableRunClientTests.DestinationIntent("third",5))));
        }
        [Test] public async Task MapArrivalReopensWithOriginalBindingAndImmutableNodeStart(){
            var f=OrdinaryNodeStartWireTests.Fixture();var native=new Native();var journal=(IRunOperationJournal)Journal(native);
            var operation=PendingRunOperation.ArriveMap(f.Start.AccountId,f.Command,new AppBuildSnapshot("test",1),Time);
            Assert.IsTrue(await journal.PutAsync(operation));var reopened=(await ((IRunOperationJournal)Journal(native)).ReadAsync(f.Start.AccountId)).Single();
            Assert.AreEqual(operation.BodyJson,reopened.BodyJson);Assert.AreEqual(operation.MapArrivalBindingJson,reopened.MapArrivalBindingJson);Assert.AreEqual(f.Command.Binding.Hash,reopened.ReadMapArrivalCommand().Binding.Hash);
            Assert.IsTrue(await journal.ConfirmAsync(reopened,f.Response.ToString()));
            var confirmed=(await ((IRunOperationJournal)Journal(native)).ReadAsync(f.Start.AccountId)).Single();
            var result=BRouteCommandResult.Read(200,confirmed.AcceptedResponseJson,confirmed.ReadMapArrivalCommand());Assert.AreEqual(70,(int)result.NodeStart.Capture()["snapshot"]["actors"][0]["hp"]);
        }
        [Test] public async Task ExplicitTravelConflictReplacesOnlyItsRejectedOriginalAtomically(){
            var native=new Native();var journal=(EncryptedRunOperationJournal)Journal(native);var original=DurableRunClientTests.TravelCommand();
            var rejected=PendingRunOperation.Travel(Account,"run",original,new AppBuildSnapshot("test",1),Time);Assert.IsTrue(await journal.PutAsync(rejected));
            var current=JObject.Parse(DurableRunClientTests.TravelResponse(DurableRunClientTests.TravelCommand("other",4)));
            var conflict=new JObject{["accepted"]=false,["errorCode"]="combat_b_travel_revision_conflict",["current"]=current}.ToString();
            var recovery=PendingRunOperation.Travel(Account,"run",DurableRunClientTests.TravelCommand("recover",5),rejected.Build,Time);
            native.FailSave=true;Assert.IsFalse(await journal.ReplaceRejectedTravelAsync(rejected,recovery,conflict));Assert.AreEqual(rejected.IdempotencyKey,(await journal.ReadAsync(Account)).Single().IdempotencyKey);
            native.FailSave=false;Assert.IsTrue(await journal.ReplaceRejectedTravelAsync(rejected,recovery,conflict));Assert.AreEqual(recovery.IdempotencyKey,(await ((IRunOperationJournal)Journal(native)).ReadAsync(Account)).Single().IdempotencyKey);
            Assert.IsFalse(await journal.ReplaceRejectedTravelAsync(rejected,recovery,conflict));
        }
        [TestCase("opaque/key:123456")]
        [TestCase("opaque key=123456!@#$%&*()+,;?")]
        public async Task OpaqueKeySurvivesRestartConfirmationAndRemoval(string key)
        {
            var native = new Native(); var op = Operation(key:key);
            Assert.IsTrue((bool)await Call(Journal(native), "PutAsync", op));
            var restarted = Journal(native);
            var restored = (await Read(restarted)).Single();
            Assert.AreEqual(key, Get(restored,"IdempotencyKey"));
            Assert.AreEqual(Get(op,"BodyJson"), Get(restored,"BodyJson"));
            Assert.IsTrue((bool)await Call(restarted,"ConfirmAsync",restored,Accepted));
            Assert.AreEqual(Accepted,Get((await Read(Journal(native))).Single(),"AcceptedResponseJson"));
            Assert.IsTrue((bool)await Call(restarted,"RemoveConfirmedAsync",Account,PendingRunKind.Start,key));
            Assert.IsEmpty(await Read(Journal(native)));
        }
        [TestCase(16,true)] [TestCase(128,true)] [TestCase(15,false)] [TestCase(129,false)]
        public void DurableKeyLengthBoundaries(int length,bool allowed)
        {
            if(allowed) Assert.DoesNotThrow(()=>Operation(key:new string('/',length)));
            else Assert.Catch<Exception>(()=>Operation(key:new string('/',length)));
        }
        [TestCase(" leading_key_12345")] [TestCase("trailing_key_12345 ")]
        [TestCase("newline_key_12345\r\nInjected: x")] [TestCase("control_key_12345\t")]
        public void StartNeverNormalizesUnsafeOriginalKey(string key)
        {
            Assert.Catch<Exception>(()=>Operation(key:key));
        }
        [Test] public async Task InvalidRestoredKeyLeavesEncryptedRecordUntouched()
        {
            var n=new Native(); var j=Journal(n); await Call(j,"PutAsync",Operation());
            var data=JObject.Parse(n.Plain); data["records"][0]["idempotencyKey"]="invalid/key\r\nheader";
            n.Stored=n.Encrypt(data.ToString()); var held=n.Stored;
            Assert.CatchAsync<Exception>(async()=>await Read(Journal(n)));
            Assert.IsFalse((bool)await Call(j,"PutAsync",Operation()));
            Assert.AreSame(held,n.Stored); Assert.AreEqual(0,n.Clears);
        }
        private sealed class OpaqueWire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            public bool Succeed;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Requests.Add(request);
                return Task.FromResult(Succeed ? RunApiHttpResponse.Success(200,Accepted) : RunApiHttpResponse.NetworkError("test interruption"));
            }
        }
        [Test] public async Task RealJournalRestartRetriesExactOpaqueRequestWithoutRecomposition()
        {
            const string key="restore/original key:=!?";
            var native=new Native(); var wire=new OpaqueWire();
            var session=SupportInquiryTests.Session(Account);
            using(var first=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EncryptedRunOperationJournal(native),()=>session,()=>1))
                Assert.IsNull(await first.StartAsync(RunStartRequest.Create(Preview,key)));
            using(var second=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EncryptedRunOperationJournal(native),()=>session,()=>1))
            {
                await second.RestoreAsync(); Assert.AreEqual(1,wire.Requests.Count);
                var saved=second.Operations.Single(); wire.Succeed=true;
                var confirmed=await second.RetryAsync(saved); Assert.IsNotNull(confirmed);
                Assert.AreEqual(Accepted,confirmed.AcceptedResponseJson);
                Assert.AreEqual(wire.Requests[0].BodyJson,wire.Requests[1].BodyJson);
                Assert.AreEqual(key,wire.Requests[1].Headers.Single(x=>x.Name=="Idempotency-Key").Value);
                await second.RetryAsync(confirmed); Assert.AreEqual(2,wire.Requests.Count);
                Assert.AreEqual(Accepted,(await new EncryptedRunOperationJournal(native).ReadAsync(Account)).Single().AcceptedResponseJson);
            }
        }
        [Test] public async Task ChangedBodyBuildOrNewKeyForSameTargetCannotReplaceOriginal()
        {
            var n = new Native(); var j = Journal(n); Assert.That(await Call(j, "PutAsync", Operation()), Is.True);
            Assert.That(await Call(j, "PutAsync", Operation(code: 11)), Is.False);
            Assert.That(await Call(j, "PutAsync", Operation(preview: "preview_changed")), Is.False);
            Assert.That(await Call(j, "PutAsync", Operation(key: "replacement_run_key_fixture")), Is.False);
            var before = n.Saves; Assert.That(await Call(j, "PutAsync", Operation()), Is.True); Assert.That(n.Saves, Is.GreaterThan(before));
            Assert.That((await Read(j)), Has.Length.EqualTo(1));
        }
        [Test] public async Task FinishRoundtripRetainsExactSerializedBodyAndConfirmedRewardReference()
        {
            var export = new RunRuntimeStateExport("run_fixture", "1", "op_fixture", "act_fixture", "region_fixture", "faction_fixture",
                1, "wave_fixture", 1u, "reward_fixture", Time.UtcDateTime, LastRail.Sortie.SortieStage.Complete,
                15, 15, true, new TacticSelectionExport[0]);
            var request = RunResultSubmitRequest.FromExport(export, Key, RunResultOutcome.Victory, Time.UtcDateTime);
            var operation = Required("PendingRunOperation").GetMethod("Finish").Invoke(null,
                new object[] { Account, request, new AppBuildSnapshot("1.0", 10), Time });
            var n = new Native(); var j = Journal(n); Assert.That(await Call(j, "PutAsync", operation), Is.True);
            Assert.That(Get((await Read(Journal(n))).Single(), "BodyJson"), Is.EqualTo(request.BodyJson));
            const string response = "{\"accepted\":false,\"resultStatus\":\"DUPLICATE_REPLAY\",\"runId\":\"run_fixture\",\"rewardPreviewId\":\"reward_preview_fixture\",\"balanceVersion\":\"1\",\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\",\"runStartVersionSnapshot\":null,\"runFinishVersionSnapshot\":null}";
            Assert.That(await Call(j, "ConfirmAsync", operation, response), Is.True);
            Assert.That(Get((await Read(Journal(n))).Single(), "AcceptedResponseJson"), Is.EqualTo(response));
        }
        [Test] public async Task ConfirmIsDurableAndFirstAcceptedResponseNeverChanges()
        {
            var n = new Native(); var j = Journal(n); var op = Operation(); await Call(j, "PutAsync", op);
            n.FailSave = true; Assert.That(await Call(j, "ConfirmAsync", op, Accepted), Is.False);
            Assert.That(Get((await Read(Journal(n))).Single(), "AcceptedResponseJson"), Is.Null);
            n.FailSave = false; Assert.That(await Call(j, "ConfirmAsync", op, Accepted), Is.True);
            var changed = Accepted.Replace("\"balanceVersion\":\"1\"", "\"balanceVersion\":\"2\"");
            Assert.That(await Call(j, "ConfirmAsync", op, changed), Is.True);
            Assert.That(Get((await Read(Journal(n))).Single(), "AcceptedResponseJson"), Is.EqualTo(Accepted));
        }
        [Test] public async Task PendingCannotBeDeletedAndConfirmedDeletionFailureRetainsResult()
        {
            var n = new Native(); var j = Journal(n); var op = Operation(); await Call(j, "PutAsync", op);
            var kind = Get(op, "Kind");
            Assert.That(await Call(j, "RemoveConfirmedAsync", Account, kind, Key), Is.False);
            await Call(j, "ConfirmAsync", op, Accepted); n.FailSave = true;
            Assert.That(await Call(j, "RemoveConfirmedAsync", Account, kind, Key), Is.False);
            Assert.That((await Read(j)), Has.Length.EqualTo(1)); n.FailSave = false;
            Assert.That(await Call(j, "RemoveConfirmedAsync", Account, kind, Key), Is.True);
            Assert.That(await Read(j), Is.Empty);
        }
        [Test] public async Task AccountFilteringAndCapacityNeverEvictExistingPending()
        {
            var n = new Native(); var j = Journal(n);
            int admitted=0;for (;admitted<32;admitted++)if(!(bool)await Call(j,"PutAsync",Operation(account:"account_"+admitted)))break;
            Assert.That(admitted,Is.GreaterThan(0).And.LessThan(32),"START reservations must fit the unchanged aggregate journal budget.");var before=n.Stored;
            Assert.That(await Call(j, "PutAsync", Operation(account: "account_overflow")), Is.False);
            Assert.AreSame(before,n.Stored);
            Assert.That(await Read(j), Is.Empty);
            Assert.That(await Read(j, "account_0"), Has.Length.EqualTo(1)); Assert.That(n.Clears, Is.Zero);
        }
        [Test] public async Task EveryAdmittedPendingHasRoomForItsConfirmedResponse()
        {
            var journal = new EncryptedRunOperationJournal(new Native());
            var admitted = new List<PendingRunOperation>();
            for (var i = 0; i < 32; i++)
            {
                var run = "run_capacity_" + i;
                var export = new RunRuntimeStateExport(run, "1", "op_fixture", "act_fixture", "region_fixture", "faction_fixture",
                    1, "wave_fixture", 1u, "reward_fixture", Time.UtcDateTime, LastRail.Sortie.SortieStage.Complete,
                    15, 15, true, new TacticSelectionExport[0]);
                var request = RunResultSubmitRequest.FromExport(export, Key + i, RunResultOutcome.Victory, Time.UtcDateTime);
                var body = JObject.Parse(request.BodyJson);
                var runtime = JObject.Parse((string)body["runtimeStateJson"]);
                runtime["fixturePadding"] = new string('x', 140000);
                body["runtimeStateJson"] = runtime.ToString(Formatting.None);
                var op = (PendingRunOperation)Activator.CreateInstance(typeof(PendingRunOperation),
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new object[] { PendingRunKind.Finish, Account, run, Key + i, body.ToString(Formatting.None),
                        new AppBuildSnapshot("1.0", 10), Time, null }, null);
                if (!await journal.PutAsync(op)) break;
                admitted.Add(op);
            }
            Assert.That(admitted.Count, Is.GreaterThanOrEqualTo(2));
            foreach (var op in admitted)
            {
                var response = new JObject {
                    ["accepted"] = true, ["resultStatus"] = "ACCEPTED", ["runId"] = op.TargetId,
                    ["rewardPreviewId"] = "reward_fixture", ["balanceVersion"] = "1",
                    ["serverReceivedAt"] = "2026-09-06T00:00:00.000Z",
                    ["staminaRefund"] = new JObject { ["fixturePadding"] = new string('x', 60000) }
                }.ToString(Formatting.None);
                Assert.That(await journal.ConfirmAsync(op, response), Is.True, op.TargetId);
            }
            Assert.That((await journal.ReadAsync(Account)).All(r => r.AcceptedResponseJson != null), Is.True);
        }
        [Test] public void OversizedEncodedAcceptanceIsRejected()
        {
            var op = (PendingRunOperation)Operation();
            var response = ReadFixtureJson(Accepted);
            response["ordinary"] = new JObject { ["fixturePadding"] = new string('x', 131072) };
            Assert.Throws<FormatException>(() => op.Confirmed(response.ToString(Formatting.None)));
        }
        static JObject ReadFixtureJson(string json){
            using(var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None})return JObject.Load(reader);
        }
        static string SizedStart(int size,bool utf8){
            var root=ReadFixtureJson(Accepted);Assert.AreEqual(JTokenType.String,root["serverStartedAt"].Type);root["ordinary"]=new JObject{["fixturePadding"]=""};string text=root.ToString(Formatting.None);
            int remaining=size-(utf8?Encoding.UTF8.GetByteCount(text):new JValue(text).ToString(Formatting.None).Length);
            root["ordinary"]["fixturePadding"]=utf8?new string('한',remaining/3)+new string('a',remaining%3):new string('\\',remaining/4)+new string('a',remaining%4);
            text=root.ToString(Formatting.None);Assert.AreEqual(size,utf8?Encoding.UTF8.GetByteCount(text):new JValue(text).ToString(Formatting.None).Length);return text;
        }
        [TestCase(65536,true,true)][TestCase(65537,true,true)][TestCase(131072,true,true)][TestCase(131073,true,false)]
        [TestCase(65536,false,true)][TestCase(65537,false,true)][TestCase(70607,false,true)][TestCase(131072,false,true)][TestCase(131073,false,false)]
        public async Task StartRawAndEncodedBoundariesPreservePendingOrExactConfirmedReceipt(int size,bool utf8,bool accepted){
            var native=new Native();var journal=new EncryptedRunOperationJournal(native);var op=(PendingRunOperation)Operation();string response=SizedStart(size,utf8);
            Assert.IsTrue(await journal.PutAsync(op));var before=native.Stored;
            Assert.AreEqual(accepted,await journal.ConfirmAsync(op,response));var restored=(await new EncryptedRunOperationJournal(native).ReadAsync(Account)).Single();
            Assert.AreEqual(Key,restored.IdempotencyKey);Assert.AreEqual(op.BodyJson,restored.BodyJson);
            if(accepted)Assert.AreEqual(response,restored.AcceptedResponseJson);else{Assert.IsNull(restored.AcceptedResponseJson);Assert.AreSame(before,native.Stored);}
        }
        [Test] public async Task HistoricalCharacterBoundStartRemainsReadableWithoutRewritingOriginal(){
            var native=new Native();var journal=new EncryptedRunOperationJournal(native);var op=(PendingRunOperation)Operation();Assert.IsTrue(await journal.PutAsync(op));
            string response=SizedStart(131073,true);Assert.Less(new JValue(response).ToString(Formatting.None).Length,65536);
            var historical=ReadFixtureJson(native.Plain);historical["records"][0]["acceptedResponseJson"]=response;native.Stored=native.Encrypt(historical.ToString(Formatting.None));var before=native.Stored;int saves=native.Saves;
            var restored=(await new EncryptedRunOperationJournal(native).ReadAsync(Account)).Single();Assert.AreEqual(response,restored.AcceptedResponseJson);Assert.AreSame(before,native.Stored);Assert.AreEqual(saves,native.Saves);
            Assert.DoesNotThrow(()=>restored.Confirmed(response));
        }
        [Test] public async Task EveryAdmittedStartReservesItsNewMaximumWithinUnchangedAggregateLimit(){
            var native=new Native();var journal=new EncryptedRunOperationJournal(native);var admitted=new List<PendingRunOperation>();
            for(int i=0;i<32;i++){var op=(PendingRunOperation)Operation(account:"account_"+i);if(!await journal.PutAsync(op))break;admitted.Add(op);}
            Assert.That(admitted.Count,Is.GreaterThan(0).And.LessThan(32));string response=SizedStart(131072,false);
            foreach(var op in admitted)Assert.IsTrue(await journal.ConfirmAsync(op,response),op.AccountId);
            Assert.LessOrEqual(native.Plain.Length,3*1024*1024);
            foreach(var op in admitted)Assert.AreEqual(response,(await new EncryptedRunOperationJournal(native).ReadAsync(op.AccountId)).Single().AcceptedResponseJson);
        }
        [TestCase(65536,true)][TestCase(65537,false)] public void FinishResponseDoesNotBorrowTheExpandedStartBudget(int encoded,bool accepted){
            var export=new RunRuntimeStateExport("run_finish","1","op_fixture","act_fixture","region_fixture","faction_fixture",1,"wave_fixture",1u,"reward_fixture",Time.UtcDateTime,LastRail.Sortie.SortieStage.Complete,15,15,true,new TacticSelectionExport[0]);
            var request=RunResultSubmitRequest.FromExport(export,Key,RunResultOutcome.Victory,Time.UtcDateTime);var op=PendingRunOperation.Finish(Account,request,new AppBuildSnapshot("test",1),Time);
            var response=new JObject{["accepted"]=true,["resultStatus"]="ACCEPTED",["runId"]="run_finish",["rewardPreviewId"]="reward_fixture",["balanceVersion"]="1",["serverReceivedAt"]="2026-09-06T00:00:00.000Z",["staminaRefund"]=new JObject{["fixturePadding"]=""}};
            response["staminaRefund"]["fixturePadding"]=new string('x',encoded-new JValue(response.ToString(Formatting.None)).ToString(Formatting.None).Length);string text=response.ToString(Formatting.None);
            Assert.AreEqual(encoded,new JValue(text).ToString(Formatting.None).Length);if(accepted)Assert.DoesNotThrow(()=>op.Confirmed(text));else Assert.Throws<FormatException>(()=>op.Confirmed(text));
        }
        [TestCase("{}")]
        [TestCase("{\"schema\":999,\"records\":[]}")]
        public async Task CorruptJournalIsHeldAndNeverReplaced(string value)
        {
            var n = new Native(); n.Stored = n.Encrypt(value); var held = n.Stored; var j = Journal(n);
            Assert.CatchAsync<Exception>(async () => await Read(j));
            Assert.That(await Call(j, "PutAsync", Operation()), Is.False); Assert.That(n.Stored, Is.SameAs(held)); Assert.That(n.Clears, Is.Zero);
        }
        [Test] public async Task UnavailableCryptoBlocksWritesAndReadDoesNotLookEmpty()
        {
            var n = new Native(); var j = Journal(n); await Call(j, "PutAsync", Operation()); n.Available = false;
            Assert.CatchAsync<Exception>(async () => await Read(j));
            Assert.That(await Call(j, "PutAsync", Operation()), Is.False); Assert.That(n.Clears, Is.Zero);
        }
        [TestCase("{\"accepted\":false,\"startStatus\":\"REJECTED\"}")]
        [TestCase("{\"accepted\":true,\"startStatus\":\"STARTED\",\"operationPreviewId\":\"foreign\",\"runId\":\"run_foreign\"}")]
        public async Task InvalidOrForeignAcceptanceCannotClearOriginal(string response)
        {
            var n = new Native(); var j = Journal(n); var op = Operation(); await Call(j, "PutAsync", op);
            Assert.That(await Call(j, "ConfirmAsync", op, response), Is.False);
            Assert.That(Get((await Read(j)).Single(), "AcceptedResponseJson"), Is.Null);
        }
        [Test] public void RunFileDomainRejectsReceiptCiphertextWithoutTouchingAuthSlot()
        {
            var n = new Native(); var path = Path.Combine(Path.GetTempPath(), "lastrail-run-domain-" + Guid.NewGuid().ToString("N") + ".enc");
            var type = Required("AndroidEncryptedRunFilePort");
            var run = (IAndroidKeystoreNativeStoragePort)Activator.CreateInstance(type, n, path);
            var receipt = new AndroidEncryptedReceiptFilePort(n, path);
            try
            {
                Assert.That(receipt.Save(receipt.Encrypt("receipt-only")), Is.True);
                Assert.Catch<Exception>(() => run.Decrypt(run.Load()));
                Assert.That(n.Saves, Is.Zero); Assert.That(n.Loads, Is.Zero); Assert.That(n.Clears, Is.Zero);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
    }
}
