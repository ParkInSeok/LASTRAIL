using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace LastRail.Tests.EditMode
{
    public sealed class DurableRunClientTests
    {
        private const string Key = "durable/run:original key!?";
        private const string StartJson = "{\"accepted\":true,\"startStatus\":\"STARTED\",\"runId\":\"run_001\",\"operationPreviewId\":\"preview_001\",\"operationId\":\"opn_01_last_departure\",\"actId\":\"act_01_wasteland_railway\",\"regionId\":\"region_wasteland_railway\",\"encounterType\":\"ORDINARY_15_WAVE\",\"riskLevel\":\"LOW\",\"risk\":\"LOW\",\"balanceVersion\":\"1\",\"contentVersion\":\"fixture_v1\",\"ordinary\":{\"totalWaves\":15,\"startWaveNumber\":1},\"raid\":null,\"raidId\":null,\"bossId\":null,\"bossPhaseRefs\":[],\"allowedOperatorSlots\":[\"slot_1\"],\"startAugmentPackageRef\":\"augment_fixture\",\"startTacticPackageRef\":\"tactic_fixture\",\"runVersionSnapshot\":null,\"serverStartedAt\":\"2026-09-06T00:00:00.000Z\",\"errorCode\":null,\"message\":null}";
        private const string FinishJson = "{\"runId\":\"run_001\",\"accepted\":true,\"resultStatus\":\"ACCEPTED\",\"rewardClaimId\":null,\"rewardPreviewId\":\"reward_preview_001\",\"serverReceivedAt\":\"2026-09-06T00:00:00.000Z\",\"balanceVersion\":\"1\",\"staminaRefund\":null,\"runStartVersionSnapshot\":null,\"runFinishVersionSnapshot\":null,\"message\":null,\"errorCode\":null}";

        public sealed class JournalDouble
        {
            public readonly List<object> Rows = new List<object>();
            public readonly List<string> Events = new List<string>();
            public bool PutResult = true, ConfirmResult = true, RemoveResult = true, Corrupt, ConfirmThrows;
            public TaskCompletionSource<bool> PutWait, ConfirmWait, ReadWait;
            public object Dispatch(string name, object[] args)
            {
                Events.Add(name);
                if (name == "ReadAsync")
                {
                    if (Corrupt) throw new InvalidOperationException("Explicit corrupt journal");
                    var array = Array.CreateInstance(TypeOf("PendingRunOperation"), Rows.Count);
                    for (var i = 0; i < Rows.Count; i++) array.SetValue(Rows[i], i);
                    var wait = ReadWait; ReadWait = null;
                    return typeof(JournalDouble).GetMethod(nameof(Result)).MakeGenericMethod(
                        typeof(IReadOnlyList<>).MakeGenericType(TypeOf("PendingRunOperation"))).Invoke(null,
                            new object[] { array, (Task)wait?.Task ?? Task.CompletedTask });
                }
                if (name == "PutAsync") return Put(args[0]);
                if (name == "ConfirmAsync") return Confirm(args[0], (string)args[1]);
                if (name == "RemoveConfirmedAsync")
                {
                    if (RemoveResult) Rows.RemoveAll(r => Equals(Get(r, "AccountId"), args[0])
                        && Equals(Get(r, "Kind"), args[1]) && Equals(Get(r, "IdempotencyKey"), args[2]) && Get(r, "AcceptedResponseJson") != null);
                    return Task.FromResult(RemoveResult);
                }
                throw new InvalidOperationException(name);
            }
            public static async Task<T> Result<T>(object value, Task wait) { await wait; return (T)value; }
            private async Task<bool> Put(object op)
            {
                if (!(PutWait == null ? PutResult : await PutWait.Task)) return false;
                var prior = Rows.FirstOrDefault(r => Same(r, op));
                if (prior != null) return Equals(Get(prior, "BodyJson"), Get(op, "BodyJson"))
                    && Equals(Get(prior, "ComposedAt"), Get(op, "ComposedAt"));
                if((string)Get(op,"Kind").ToString()=="TravelCommand")Rows.RemoveAll(r=>Get(r,"Kind").ToString()=="TravelCommand"&&Equals(Get(r,"AccountId"),Get(op,"AccountId"))&&Equals(Get(r,"TargetId"),Get(op,"TargetId"))&&Get(r,"AcceptedResponseJson")!=null);
                Rows.Add(op); return true;
            }
            private async Task<bool> Confirm(object op, string json)
            {
                if (ConfirmThrows) throw new InvalidOperationException("Synthetic storage failure");
                if (!(ConfirmWait == null ? ConfirmResult : await ConfirmWait.Task)) return false;
                var index = Rows.FindIndex(r => Same(r, op));
                if (index < 0) return false;
                Rows[index] = Call(Rows[index], "Confirmed", json); return true;
            }
        }
        private sealed class Wire : IRunApiHttpTransport
        {
            internal readonly List<RunApiHttpRequest> Requests = new List<RunApiHttpRequest>();
            internal Func<RunApiHttpRequest, Task<RunApiHttpResponse>> Reply = _ => Task.FromResult(RunApiHttpResponse.NetworkError("Synthetic loss"));
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request) { Requests.Add(request); return Reply(request); }
        }
        private sealed class Fixture : IDisposable
        {
            internal readonly JournalDouble Journal;
            internal readonly Wire Wire = new Wire();
            internal GameApiAuthSessionSnapshot Session = SupportInquiryTests.Session();
            internal long Generation = 1;
            internal int Captures;
            internal readonly object Client;
            internal Fixture(JournalDouble journal = null)
            {
                Journal = journal ?? new JournalDouble();
                var type = TypeOf("DurableRunClient");
                var ctor = type.GetConstructor(new[] { typeof(IGameApiAuthenticatedHttpTransport), TypeOf("IRunOperationJournal"),
                    typeof(Func<GameApiAuthSessionSnapshot>), typeof(Func<long>), typeof(Func<AppBuildSnapshot>), typeof(Func<DateTimeOffset>) });
                Assert.That(ctor, Is.Not.Null, "Missing durable run client constructor");
                Client = ctor.Invoke(new object[] { new GameApiAuthenticatedHttpTransport(Wire), Adapter(Journal),
                    (Func<GameApiAuthSessionSnapshot>)(() => Session), (Func<long>)(() => Generation),
                    (Func<AppBuildSnapshot>)(() => { Captures++; return new AppBuildSnapshot("compose_B", 22); }),
                    (Func<DateTimeOffset>)(() => DateTimeOffset.Parse("2026-09-06T00:00:00Z")) });
            }
            internal Task<object> Start(string key = Key) => InvokeAsync(Client, "StartAsync", RunStartRequest.Create("preview_001", key));
            internal Task<object> Retry(object op) => InvokeAsync(Client, "RetryAsync", op);
            internal Task Restore() => (Task)Call(Client, "RestoreAsync");
            internal object[] Operations => ((IEnumerable)Get(Client, "Operations")).Cast<object>().ToArray();
            public void Dispose() => ((IDisposable)Client).Dispose();
        }

        [Test] public void PublicContractIsExplicitAndReadOnly()
        {
            using (var f = new Fixture())
            {
                foreach (var property in new[] { "Operations", "Busy", "Notice" })
                    Assert.That(f.Client.GetType().GetProperty(property).GetSetMethod(), Is.Null);
                foreach (var method in new[] { "RestoreAsync", "StartAsync", "FinishAsync", "RetryAsync", "HandoffAsync" })
                    Assert.That(f.Client.GetType().GetMethod(method), Is.Not.Null);
            }
        }
        [Test] public void TravelUsesExistingDurableOwnerContract()
        {
            Assert.NotNull(typeof(DurableRunClient).GetMethod("TravelAsync"), "Travel must reuse the durable run owner");
            Assert.IsTrue(Enum.GetNames(typeof(PendingRunKind)).Contains("TravelCommand"));
        }
        [Test] public void MapArrivalReusesTheDurableRunOwner()
        {
            Assert.NotNull(typeof(DurableRunClient).GetMethod("ArriveMapAsync"),"Map arrival must save its original request before sending");
            Assert.NotNull(typeof(PendingRunOperation).GetMethod("ReadMapArrivalCommand"));
        }
        [Test] public async Task MapArrivalSavesBeforeSendingReusesOriginalAndConfirmsNodeStart(){
            var source=OrdinaryNodeStartWireTests.Fixture();using(var f=new Fixture(new JournalDouble{PutResult=false})){
                f.Session=SupportInquiryTests.Session(source.Start.AccountId);
                Assert.IsNull(await InvokeAsync(f.Client,"ArriveMapAsync",source.Command));Assert.IsEmpty(f.Wire.Requests);
                f.Journal.PutResult=true;Assert.IsNull(await InvokeAsync(f.Client,"ArriveMapAsync",source.Command));var pending=(PendingRunOperation)f.Operations.Single();
                Assert.AreEqual(source.Command.Binding.Hash,pending.ReadMapArrivalCommand().Binding.Hash);
                f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,source.Response.ToString()));
                var confirmed=(PendingRunOperation)await f.Retry(pending);Assert.NotNull(confirmed);
                Assert.AreEqual("/v1/runs/"+Uri.EscapeDataString(source.Start.RunId)+"/b-route/transition",f.Wire.Requests[0].EndpointPath);
                Assert.AreEqual(f.Wire.Requests[0].BodyJson,f.Wire.Requests[1].BodyJson);Assert.AreEqual(HeaderKey(f.Wire.Requests[0]),HeaderKey(f.Wire.Requests[1]));
                var result=BRouteCommandResult.Read(200,confirmed.AcceptedResponseJson,confirmed.ReadMapArrivalCommand());result.NodeStart.RequireOriginal(source.Start);
                Assert.IsFalse(result.NodeStart.CanStart);Assert.NotNull(await f.Retry(confirmed));Assert.AreEqual(2,f.Wire.Requests.Count);
            }
        }
        internal static BOperationDestinationIntent DestinationIntent(string key="destination-original",long revision=1)=>new BOperationDestinationIntent("account_test","run_001",key,revision,"edge_1");
        internal static string DestinationResponse(BOperationDestinationIntent intent,bool initial=true){
            var response=new JObject{["version"]="operation.map-destination.v1",["requestId"]=intent.CommandId,["revision"]=intent.ExpectedRevision+1,["phase"]="TRAVELLING",["pendingEdgeId"]=intent.EdgeId};
            if(initial)response["travelInitial"]=new JObject{["originalFixture"]="preserve-exactly"};return response.ToString();
        }
        [TestCase(true)] [TestCase(false)] public async Task DestinationSavesBeforeSendRetainsAcceptedOriginalAndReusesWithoutGet(bool initial){
            using(var f=new Fixture(new JournalDouble{PutResult=false})){
                var intent=DestinationIntent();Assert.IsNull(await InvokeAsync(f.Client,"ConfirmDestinationAsync",intent));Assert.IsEmpty(f.Wire.Requests);
                f.Journal.PutResult=true;var json=DestinationResponse(intent,initial);f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,json));
                var saved=(PendingRunOperation)await InvokeAsync(f.Client,"ConfirmDestinationAsync",intent);Assert.NotNull(saved);Assert.AreEqual(json,saved.AcceptedResponseJson);
                var sent=f.Wire.Requests.Single();Assert.AreEqual("POST",sent.Method);Assert.AreEqual("/v1/runs/run_001/operation-map/destination",sent.EndpointPath);
                Assert.AreEqual(intent.Body().ToString(Newtonsoft.Json.Formatting.None),sent.BodyJson);Assert.AreEqual(intent.CommandId,HeaderKey(sent));
                Assert.NotNull(await f.Retry(saved));Assert.AreEqual(1,f.Wire.Requests.Count);
            }
        }
        [Test] public async Task DestinationLossBlocksReplacementAndRetriesExactOriginal(){
            using(var f=new Fixture()){
                var intent=DestinationIntent();Assert.IsNull(await InvokeAsync(f.Client,"ConfirmDestinationAsync",intent));var pending=f.Operations.Single();
                Assert.IsNull(await InvokeAsync(f.Client,"ConfirmDestinationAsync",DestinationIntent("new-key")));Assert.AreEqual(1,f.Wire.Requests.Count);
                f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,DestinationResponse(intent)));Assert.NotNull(await f.Retry(pending));
                Assert.AreEqual(f.Wire.Requests[0].BodyJson,f.Wire.Requests[1].BodyJson);Assert.AreEqual(HeaderKey(f.Wire.Requests[0]),HeaderKey(f.Wire.Requests[1]));
            }
        }
        [Test] public async Task DestinationStorageFailureRetriesStoredAcceptanceWithoutSecondPost(){
            using(var f=new Fixture(new JournalDouble{ConfirmResult=false})){
                var intent=DestinationIntent();var json=DestinationResponse(intent);f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,json));
                Assert.IsNull(await InvokeAsync(f.Client,"ConfirmDestinationAsync",intent));var pending=f.Operations.Single();Assert.IsNull(Get(pending,"AcceptedResponseJson"));
                f.Journal.ConfirmResult=true;var saved=(PendingRunOperation)await f.Retry(pending);Assert.NotNull(saved);Assert.AreEqual(json,saved.AcceptedResponseJson);Assert.AreEqual(1,f.Wire.Requests.Count);
            }
        }
        [Test] public async Task DestinationHttpErrorRetainsOriginalAndPublishesErrorCode(){
            using(var f=new Fixture()){
                var intent=DestinationIntent();f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(409,"{\"errorCode\":\"operation_map_revision_conflict\"}"));
                Assert.IsNull(await InvokeAsync(f.Client,"ConfirmDestinationAsync",intent));Assert.AreEqual("operation_map_revision_conflict",Get(f.Client,"LastErrorCode"));
                Assert.AreEqual(intent.CommandId,Get(f.Operations.Single(),"IdempotencyKey"));Assert.IsNull(Get(f.Operations.Single(),"AcceptedResponseJson"));
            }
        }
        [Test] public async Task DestinationLateAcceptanceCannotPublishIntoChangedAccount(){
            using(var f=new Fixture()){
                var intent=DestinationIntent();var reply=new TaskCompletionSource<RunApiHttpResponse>();f.Wire.Reply=r=>reply.Task;
                var task=InvokeAsync(f.Client,"ConfirmDestinationAsync",intent);f.Session=SupportInquiryTests.Session("other");f.Generation++;
                reply.SetResult(RunApiHttpResponse.Success(200,DestinationResponse(intent)));Assert.IsNull(await task);Assert.IsEmpty(f.Operations);Assert.IsNull(Get(f.Journal.Rows.Single(),"AcceptedResponseJson"));
            }
        }
        internal static BTravelCommand TravelCommand(string key="travel-original",long revision=0)=>BTravelCommand.Read(new JObject{
            ["version"]=BTravelCommand.Version,["requestId"]=key,["destinationEventId"]="destination",["nodeId"]="node",["mapRevision"]=2,["expectedRevision"]=revision,["kind"]="RECOVER"}.ToString());
        internal static string TravelResponse(BTravelCommand c)=>new JObject{["version"]=BTravelCommand.Version,["requestId"]=c.RequestId,["destinationEventId"]=c.DestinationEventId,["nodeId"]=c.NodeId,["mapRevision"]=c.MapRevision,["revision"]=c.ExpectedRevision+1,["acceptedAtMs"]=1793400000000L,["state"]=new JObject{["tick"]=0,["distanceMm"]=0,["remainderUnits"]=0,["debtTicks"]=0,["paused"]=true,["throttleBps"]=10000}}.ToString();
        [Test] public async Task TravelSaveFailureDoesNotSendAndSuccessReusesExactReceiptWithoutGet(){
            using(var f=new Fixture(new JournalDouble{PutResult=false})){
                var command=TravelCommand();Assert.IsNull(await InvokeAsync(f.Client,"TravelAsync","run_001",command));Assert.IsEmpty(f.Wire.Requests);
                f.Journal.PutResult=true;f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,TravelResponse(command)));
                var saved=await InvokeAsync(f.Client,"TravelAsync","run_001",command);Assert.NotNull(saved);
                var sent=f.Wire.Requests.Single();Assert.AreEqual("/v1/runs/run_001/b-arrival-reports",sent.EndpointPath);Assert.AreEqual(command.BodyJson,sent.BodyJson);
                Assert.AreEqual(command.RequestId,HeaderKey(sent));Assert.IsTrue(sent.Headers.Any(h=>h.Name=="X-B-Arrival-Version"&&h.Value==BTravelCommand.Version));
                Assert.NotNull(await f.Retry(saved));Assert.AreEqual(1,f.Wire.Requests.Count);
            }
        }
        [Test] public async Task TravelLostResponseRetainsOriginalAndBlocksSecondCommand(){
            using(var f=new Fixture()){
                var command=TravelCommand();Assert.IsNull(await InvokeAsync(f.Client,"TravelAsync","run_001",command));var pending=f.Operations.Single();
                Assert.IsNull(await InvokeAsync(f.Client,"TravelAsync","run_001",TravelCommand("new-key")));Assert.AreEqual(1,f.Wire.Requests.Count);
                f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,TravelResponse(command)));Assert.NotNull(await f.Retry(pending));
                Assert.AreEqual(f.Wire.Requests[0].BodyJson,f.Wire.Requests[1].BodyJson);Assert.AreEqual(HeaderKey(f.Wire.Requests[0]),HeaderKey(f.Wire.Requests[1]));
                var next=TravelCommand("next",1);f.Wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,TravelResponse(next)));
                Assert.NotNull(await InvokeAsync(f.Client,"TravelAsync","run_001",next));Assert.AreEqual(1,f.Journal.Rows.Count);
            }
        }
        [Test] public async Task TravelLateAcceptanceCannotPublishIntoChangedAccount(){
            using(var f=new Fixture()){
                var command=TravelCommand();var reply=new TaskCompletionSource<RunApiHttpResponse>();f.Wire.Reply=r=>reply.Task;
                var task=InvokeAsync(f.Client,"TravelAsync","run_001",command);f.Session=SupportInquiryTests.Session("other");f.Generation++;
                reply.SetResult(RunApiHttpResponse.Success(200,TravelResponse(command)));Assert.IsNull(await task);Assert.IsEmpty(f.Operations);Assert.IsNull(Get(f.Journal.Rows.Single(),"AcceptedResponseJson"));
            }
        }
        [Test] public async Task RestoreReadsCurrentAccountOnlyAndNeverPosts()
        {
            using (var f = new Fixture())
            {
                f.Journal.Rows.Add(Operation()); f.Journal.Rows.Add(Operation("foreign_account"));
                await f.Restore();
                Assert.That(f.Operations.Length, Is.EqualTo(1));
                Assert.That(Get(f.Operations[0], "AccountId"), Is.EqualTo("account_test"));
                Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Captures, Is.Zero);
            }
        }
        [Test] public async Task CorruptRestoreIsVisibleAndCannotDispatch()
        {
            using (var f = new Fixture(new JournalDouble { Corrupt = true }))
            {
                try { await f.Restore(); } catch (InvalidOperationException) { }
                Assert.That((string)Get(f.Client, "Notice"), Is.Not.Empty);
                Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Operations, Is.Empty);
            }
        }
        [Test] public async Task FailedPutNeverPosts()
        {
            using (var f = new Fixture(new JournalDouble { PutResult = false }))
            { Assert.That(await f.Start(), Is.Null); Assert.That(f.Wire.Requests, Is.Empty); }
        }
        [TestCase(false)] [TestCase(true)]
        public async Task NewOwnerRestoreWaitsForBusyWorkThenReadsCurrentRecords(bool oldStart)
        {
            using (var f = new Fixture())
            {
                var gate = new TaskCompletionSource<bool>();
                Task old;
                if (oldStart) { f.Journal.PutWait = gate; old = f.Start(); }
                else
                {
                    f.Journal.Rows.Add(Operation()); f.Journal.ReadWait = gate; old = f.Restore();
                }
                Assert.That((bool)Get(f.Client, "Busy"), Is.True);
                f.Session = SupportInquiryTests.Session("account_next"); f.Generation++;
                var nextRecord = Operation("account_next"); f.Journal.Rows.Add(nextRecord);
                var next = f.Restore();
                Assert.That(next.IsCompleted, Is.False, "A new-owner restore must not silently return while busy.");
                Assert.That(f.Operations, Is.Empty, "Old-owner records cannot remain visible.");
                gate.SetResult(true); await old; await next;
                Assert.That(f.Operations.Length, Is.EqualTo(1)); Assert.That(f.Operations[0], Is.SameAs(nextRecord));
                Assert.That(f.Wire.Requests, Is.Empty, "Stale saved work and restoration must not dispatch.");
                Assert.That((bool)Get(f.Client, "Busy"), Is.False);
            }
        }
        [Test] public async Task PutMustCompleteBeforeFirstPost()
        {
            using (var f = new Fixture(new JournalDouble { PutWait = new TaskCompletionSource<bool>() }))
            {
                var task = f.Start(); Assert.That(f.Wire.Requests, Is.Empty);
                Assert.That((bool)Get(f.Client, "Busy"), Is.True);
                f.Journal.PutWait.SetResult(true); Assert.That(await task, Is.Null);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1)); Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
            }
        }
        [TestCase("generation")] [TestCase("account")] [TestCase("dispose")]
        public async Task AwaitedPutFencesReplacementOwner(string change)
        {
            using (var f = new Fixture(new JournalDouble { PutWait = new TaskCompletionSource<bool>() }))
            {
                var task = f.Start(); Change(f, change); f.Journal.PutWait.SetResult(true);
                Assert.That(await task, Is.Null); Assert.That(f.Wire.Requests, Is.Empty);
            }
        }
        [Test] public async Task SameAccountTokenRotationUsesCurrentBearerAfterPut()
        {
            using (var f = new Fixture(new JournalDouble { PutWait = new TaskCompletionSource<bool>() }))
            {
                var task = f.Start();
                f.Session = new GameApiAuthSessionSnapshot("account_test", "ACTIVE", new GameApiAccessToken("lst1.rotated"),
                    DateTime.UtcNow.AddHours(1), new GameApiRefreshToken("rotated_refresh"), DateTime.UtcNow.AddDays(1));
                f.Journal.PutWait.SetResult(true); await task;
                Assert.That(f.Wire.Requests.Single().Headers.Single(h => h.Name == "Authorization").Value, Is.EqualTo("Bearer lst1.rotated"));
            }
        }
        [Test] public async Task RepeatedLogicalStartKeepsOriginalBuildTimeAndKey()
        {
            using (var f = new Fixture())
            {
                await f.Start(); await f.Start();
                Assert.That(f.Captures, Is.EqualTo(1)); Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(2));
                Assert.That(f.Wire.Requests[1].BodyJson, Is.EqualTo(f.Wire.Requests[0].BodyJson));
                Assert.That(HeaderKey(f.Wire.Requests[1]), Is.EqualTo(Key));
            }
        }
        [Test] public async Task RestartRetriesStoredFinishBytesWithoutRecapture()
        {
            var old = Operation(finish: true); var journal = new JournalDouble(); journal.Rows.Add(old);
            using (var f = new Fixture(journal))
            {
                await f.Restore(); await f.Retry(f.Operations.Single());
                Assert.That(f.Captures, Is.Zero);
                var request = f.Wire.Requests.Single();
                Assert.That(request.EndpointPath, Is.EqualTo("/v1/runs/run_001/finish"));
                Assert.That(request.BodyJson, Is.EqualTo(Get(old, "BodyJson"))); Assert.That(HeaderKey(request), Is.EqualTo(Key));
            }
        }
        [Test] public async Task ForeignRetryCannotSendOrSave()
        {
            using (var f = new Fixture())
            { Assert.That(await f.Retry(Operation("foreign_account")), Is.Null); Assert.That(f.Wire.Requests, Is.Empty); Assert.That(f.Journal.Events.Contains("PutAsync"), Is.False); }
        }
        [TestCase(false)] [TestCase(true)]
        public async Task AcceptedResponseMustBeDurablyConfirmedBeforePublish(bool finish)
        {
            using (var f = new Fixture(new JournalDouble { ConfirmWait = new TaskCompletionSource<bool>() }))
            {
                var op = Operation(finish: finish); f.Journal.Rows.Add(op);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, finish ? FinishJson : StartJson));
                var task = f.Retry(op);
                Assert.That(task.IsCompleted, Is.False); Assert.That(Get(f.Journal.Rows.Single(), "AcceptedResponseJson"), Is.Null);
                f.Journal.ConfirmWait.SetResult(true); var accepted = await task;
                Assert.That(accepted, Is.Not.Null); Assert.That(Get(accepted, "AcceptedResponseJson"), Is.EqualTo(finish ? FinishJson : StartJson));
                await f.Retry(accepted); Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
            }
        }
        [Test] public async Task FailedConfirmKeepsPendingAndReturnsNull()
        {
            using (var f = new Fixture(new JournalDouble { ConfirmResult = false }))
            {
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, StartJson));
                Assert.That(await f.Start(), Is.Null);
                Assert.That(Get(f.Journal.Rows.Single(), "AcceptedResponseJson"), Is.Null);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public async Task ConfirmFailureRetriesLocalSaveWithoutResending(bool finish)
        {
            using (var f = new Fixture(new JournalDouble { ConfirmResult = false }))
            {
                var op = Operation(finish: finish); f.Journal.Rows.Add(op);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, finish ? FinishJson : StartJson));
                Assert.That(await f.Retry(op), Is.Null);
                await f.Restore();
                Assert.That(await f.Retry(op), Is.Null);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That(Get(f.Operations.Single(), "AcceptedResponseJson"), Is.Null);
                f.Journal.ConfirmResult = true;
                Assert.That(await f.Retry(op), Is.Not.Null);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public async Task ClaimStorageExceptionDoesNotResendUnlessGenerationChanges(bool newGeneration)
        {
            using (var f = new Fixture(new JournalDouble { ConfirmThrows = true }))
            {
                var request = RunRewardClaimRequest.Create("run_001", "reward_preview_001", Key);
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200,
                    "{\"runId\":\"run_001\",\"rewardPreviewId\":\"reward_preview_001\",\"rewardClaimId\":\"claim_001\",\"status\":\"CLAIMED\",\"serverClaimedAt\":\"2026-09-06T00:00:00.000Z\",\"balanceVersion\":\"1\",\"rewardLines\":[],\"errorCode\":null,\"message\":null}"));
                Assert.That(await InvokeAsync(f.Client, "ClaimAsync", request), Is.Null);
                f.Journal.ConfirmThrows = false;
                if (newGeneration) f.Generation++;
                var accepted = await f.Retry(f.Journal.Rows.Single());
                Assert.That(accepted, Is.Not.Null);
                Assert.That(f.Wire.Requests.Count, Is.EqualTo(newGeneration ? 2 : 1));
                Assert.That(f.Journal.Rows.Count, Is.EqualTo(1));
            }
        }
        [TestCase(409,"run_finish_request_conflict")]
        [TestCase(503,"run_finish_receipt_unavailable")]
        public async Task FinishRecoveryRejectsWithoutReplacingOriginalRequest(int status,string code)
        {
            using(var f=new Fixture()){
                var op=Operation(finish:true);f.Journal.Rows.Add(op);
                f.Wire.Reply=_=>Task.FromResult(RunApiHttpResponse.Success(status,"{\"accepted\":false,\"errorCode\":\""+code+"\"}"));
                Assert.IsNull(await InvokeAsync(f.Client,"RetryAsync",op));
                Assert.That(Get(f.Client,"LastErrorCode"),Is.EqualTo(code));
                Assert.That(Get(f.Journal.Rows.Single(),"AcceptedResponseJson"),Is.Null);
                Assert.That(Get(f.Journal.Rows.Single(),"BodyJson"),Is.EqualTo(Get(op,"BodyJson")));
                f.Wire.Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,FinishJson.Replace("\"accepted\":true","\"accepted\":false").Replace("ACCEPTED","DUPLICATE_REPLAY").Replace("\"errorCode\":null","\"errorCode\":\"duplicate_replay\"")));
                Assert.IsNotNull(await InvokeAsync(f.Client,"RetryAsync",op));
                Assert.That(Get(f.Client,"LastErrorCode"),Is.Null);
                Assert.That(f.Wire.Requests.Count,Is.EqualTo(2));
                Assert.That(f.Wire.Requests.Select(HeaderKey).Distinct().Count(),Is.EqualTo(1));
                Assert.That(f.Wire.Requests.Select(r=>r.BodyJson).Distinct().Count(),Is.EqualTo(1));
            }
        }
        [Test]
        public async Task RestoringAnotherGenerationDoesNotRepublishPriorFinishError()
        {
            using(var f=new Fixture()){
                var op=Operation(finish:true);f.Journal.Rows.Add(op);
                f.Wire.Reply=_=>Task.FromResult(RunApiHttpResponse.Success(409,"{\"accepted\":false,\"errorCode\":\"run_finish_request_conflict\"}"));
                await InvokeAsync(f.Client,"RetryAsync",op);Assert.That(Get(f.Client,"LastErrorCode"),Is.EqualTo("run_finish_request_conflict"));
                f.Generation++;Assert.That(Get(f.Client,"LastErrorCode"),Is.Null);
                await InvokeAsync(f.Client,"RestoreAsync");Assert.That(Get(f.Client,"LastErrorCode"),Is.Null);
            }
        }
        [Test] public async Task FinishDuplicateFalseAcceptedIsStillDurableCompletion()
        {
            using (var f = new Fixture())
            {
                var op = Operation(finish: true); f.Journal.Rows.Add(op);
                var json = FinishJson.Replace("\"accepted\":true", "\"accepted\":false").Replace("ACCEPTED", "DUPLICATE_REPLAY")
                    .Replace("\"errorCode\":null", "\"errorCode\":\"duplicate_replay\"");
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, json));
                Assert.That(Get(await f.Retry(op), "AcceptedResponseJson"), Is.EqualTo(json));
            }
        }
        [Test] public async Task WrongTargetResponseCannotConfirm()
        {
            using (var f = new Fixture())
            {
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, StartJson.Replace("preview_001", "preview_foreign")));
                Assert.That(await f.Start(), Is.Null); Assert.That(f.Journal.Events.Contains("ConfirmAsync"), Is.False);
            }
        }
        [Test] public async Task ConcurrentClientsReturnTheFirstDurableCanonicalResponse()
        {
            var journal = new JournalDouble();
            var pending = Operation(finish: true); journal.Rows.Add(pending);
            using (var first = new Fixture(journal))
            using (var second = new Fixture(journal))
            {
                var firstResponse = new TaskCompletionSource<RunApiHttpResponse>();
                var secondResponse = new TaskCompletionSource<RunApiHttpResponse>();
                first.Wire.Reply = _ => firstResponse.Task;
                second.Wire.Reply = _ => secondResponse.Task;
                var firstSend = first.Retry(pending);
                var secondSend = second.Retry(pending);
                Assert.That(first.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That(second.Wire.Requests.Count, Is.EqualTo(1));
                Assert.That(Get(journal.Rows.Single(), "AcceptedResponseJson"), Is.Null);
                firstResponse.SetResult(RunApiHttpResponse.Success(200, FinishJson));
                var firstAccepted = await firstSend;
                Assert.That(Get(firstAccepted, "AcceptedResponseJson"), Is.EqualTo(FinishJson));
                var duplicate = FinishJson.Replace("\"accepted\":true", "\"accepted\":false")
                    .Replace("ACCEPTED", "DUPLICATE_REPLAY")
                    .Replace("\"errorCode\":null", "\"errorCode\":\"duplicate_replay\"");
                secondResponse.SetResult(RunApiHttpResponse.Success(200, duplicate));
                var secondAccepted = await secondSend;
                Assert.That(Get(journal.Rows.Single(), "AcceptedResponseJson"), Is.EqualTo(FinishJson));
                Assert.That(secondAccepted, Is.Not.Null);
                Assert.That(Get(secondAccepted, "AcceptedResponseJson"), Is.EqualTo(Get(journal.Rows.Single(), "AcceptedResponseJson")),
                    "Durable success must return the first stored response, not the second client's replay JSON.");
                Assert.That(Get(second.Operations.Single(), "AcceptedResponseJson"), Is.EqualTo(FinishJson));
                Assert.That(HeaderKey(first.Wire.Requests.Single()), Is.EqualTo(HeaderKey(second.Wire.Requests.Single())));
                Assert.That(first.Wire.Requests.Single().BodyJson, Is.EqualTo(second.Wire.Requests.Single().BodyJson));
            }
        }
        [Test] public async Task StaleAcceptedResponseCannotPublishOrConfirm()
        {
            using (var f = new Fixture())
            {
                var gate = new TaskCompletionSource<RunApiHttpResponse>(); f.Wire.Reply = _ => gate.Task;
                var task = f.Start(); f.Generation++; gate.SetResult(RunApiHttpResponse.Success(200, StartJson));
                Assert.That(await task, Is.Null); Assert.That(f.Journal.Events.Contains("ConfirmAsync"), Is.False);
            }
        }
        [Test] public async Task StaleWhileConfirmKeepsDurableResponseButDoesNotPublish()
        {
            using (var f = new Fixture(new JournalDouble { ConfirmWait = new TaskCompletionSource<bool>() }))
            {
                f.Wire.Reply = _ => Task.FromResult(RunApiHttpResponse.Success(200, StartJson));
                var task = f.Start(); f.Generation++; f.Journal.ConfirmWait.SetResult(true);
                Assert.That(await task, Is.Null); Assert.That(Get(f.Journal.Rows.Single(), "AcceptedResponseJson"), Is.EqualTo(StartJson));
            }
        }
        [TestCase("sinkfalse")] [TestCase("deletefalse")] [TestCase("stale")] [TestCase("success")]
        public async Task HandoffRemovesOnlyAfterDurableSinkAndCurrentOwner(string mode)
        {
            using (var f = new Fixture())
            {
                var op = Call(Operation(), "Confirmed", StartJson); f.Journal.Rows.Add(op); await f.Restore();
                f.Journal.RemoveResult = mode != "deletefalse";
                var gate = new TaskCompletionSource<bool>();
                var sink = typeof(DurableRunClientTests).GetMethod(nameof(Sink), BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(TypeOf("PendingRunOperation")).Invoke(null, new object[] { gate.Task });
                var task = InvokeAsync(f.Client, "HandoffAsync", op, sink);
                Assert.That(f.Journal.Events.Contains("RemoveConfirmedAsync"), Is.False);
                if (mode == "stale") f.Generation++;
                gate.SetResult(mode != "sinkfalse");
                Assert.That(await task, Is.EqualTo(mode == "success"));
                Assert.That(f.Journal.Rows.Count, Is.EqualTo(mode == "success" ? 0 : 1));
                Assert.That(f.Wire.Requests, Is.Empty);
            }
        }

        public static Func<T, Task<bool>> Sink<T>(Task<bool> result) => _ => result;
        private static void Change(Fixture f, string kind)
        { if (kind == "generation") f.Generation++; else if (kind == "account") f.Session = SupportInquiryTests.Session("foreign_account"); else f.Dispose(); }
        private static object Operation(string account = "account_test", bool finish = false)
        {
            object request = RunStartRequest.Create("preview_001", Key);
            if (finish)
            {
                var runtime = RunRuntimeState.StartActOne(RunStartSnapshotBuilder.BuildActOne(
                    runId: "run_001", balanceVersion: "1", contentVersion: "fixture_v1",
                    selectedOperatorIds: null, selectedUltimateOperatorId: null, selectedTrainCarSlots: null,
                    completedOperationIds: new[] { "opn_03_water_stop" }, engineOutput: 82, riskLevel: 2,
                    operationId: "opn_03_water_stop", actId: "act_01_wasteland_railway", regionId: "region_wasteland_railway",
                    enemyFactionId: "faction_wasteland_raiders", wavePlanId: "waveplan_fixture", rewardTableId: "reward_fixture",
                    seed: 1234u, serverStartedAt: new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc)));
                for (var i = 0; i < 15; i++) runtime = runtime.CompleteCurrentWave();
                request = RunResultSubmitRequest.FromRuntimeState(runtime.CompleteFinalOperation(), Key, RunResultOutcome.Victory,
                    new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc));
            }
            return TypeOf("PendingRunOperation").GetMethod(finish ? "Finish" : "Start").Invoke(null,
                new object[] { account, request, new AppBuildSnapshot("original_A", 11), DateTimeOffset.Parse("2026-09-06T00:00:00Z") });
        }
        private static bool Same(object a, object b) => new[] { "AccountId", "Kind", "IdempotencyKey" }.All(p => Equals(Get(a, p), Get(b, p)));
        private static string HeaderKey(RunApiHttpRequest r) => r.Headers.Single(h => h.Name == "Idempotency-Key").Value;
        private static Type TypeOf(string name)
        { var type = typeof(RunStartRequest).Assembly.GetType("LastRail.Run." + name); Assert.That(type, Is.Not.Null, "RED missing " + name); return type; }
        private static object Get(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static object Call(object target, string name, params object[] args)
        { try { return target.GetType().GetMethod(name).Invoke(target, args); } catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; } }
        private static async Task<object> InvokeAsync(object target, string name, params object[] args)
        { var task = (Task)Call(target, name, args); await task; return task.GetType().GetProperty("Result")?.GetValue(task); }
        private static object Adapter(JournalDouble journal)
        {
            var contract = TypeOf("IRunOperationJournal");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DurableRunJournalTest_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("Main").DefineType("JournalAdapter", TypeAttributes.Public | TypeAttributes.Sealed);
            type.AddInterfaceImplementation(contract);
            var field = type.DefineField("owner", typeof(JournalDouble), FieldAttributes.Private);
            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(JournalDouble) });
            var il = ctor.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Stfld, field); il.Emit(OpCodes.Ret);
            foreach (var method in contract.GetMethods())
            {
                var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
                var implementation = type.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, method.ReturnType, parameters);
                il = implementation.GetILGenerator(); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field); il.Emit(OpCodes.Ldstr, method.Name);
                il.Emit(OpCodes.Ldc_I4, parameters.Length); il.Emit(OpCodes.Newarr, typeof(object));
                for (var i = 0; i < parameters.Length; i++)
                { il.Emit(OpCodes.Dup); il.Emit(OpCodes.Ldc_I4, i); il.Emit(OpCodes.Ldarg, i + 1); if (parameters[i].IsValueType) il.Emit(OpCodes.Box, parameters[i]); il.Emit(OpCodes.Stelem_Ref); }
                il.Emit(OpCodes.Callvirt, typeof(JournalDouble).GetMethod(nameof(JournalDouble.Dispatch))); il.Emit(OpCodes.Castclass, method.ReturnType); il.Emit(OpCodes.Ret);
                type.DefineMethodOverride(implementation, method);
            }
            return Activator.CreateInstance(type.CreateType(), journal);
        }
    }
}
