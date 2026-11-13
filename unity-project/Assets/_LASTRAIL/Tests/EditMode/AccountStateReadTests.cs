using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class AccountStateReadTests
    {
        public static string Json(long credits=123)=>new JObject{
            ["accepted"]=true,["accountId"]="account_test",["serverTime"]="2026-09-10T00:00:00.123Z",["mutationSequence"]=0,
            ["currencies"]=new JObject{["CREDITS"]=credits,["MACHINE_PARTS"]=10,["TACTICAL_DATA"]=20,["SPECIAL_CORE"]=30,["OPERATOR_EXP"]=40},
            ["items"]=new JArray(new JObject{["itemId"]="ticket",["quantity"]=2}),
            ["fuel"]=new JObject{["current"]=7,["max"]=10,["lastRegenAt"]="2026-09-10T00:00:00.000Z",["updatedAt"]="2026-09-10T00:00:00.000Z"},
            ["operators"]=new JArray(new JObject{["operatorId"]="op-a",["skillTraining"]=1,["promotion"]=2,["revision"]=3,["acquiredAt"]="2026-09-10T00:00:00.000Z"}),
            ["trainingCoreSlots"]=new JArray(new JObject{["slot"]=2,["operatorId"]="op-a",["trainedLevel"]=3,["investedOperatorExp"]=120,["revision"]=0}),
            ["train"]=new JObject{["trainId"]="train-a",["level"]=2,["revision"]=1,["updatedAt"]="2026-09-10T00:00:00.000Z"},
            ["carriages"]=new JArray(new JObject{["carriageId"]="car-a",["level"]=2,["revision"]=1}),
            ["turrets"]=new JArray(new JObject{["turretId"]="turret-a",["level"]=3,["revision"]=1}),
            ["formation"]=new JObject{["activePresetId"]=null,["activationSupported"]=false,["presets"]=new JArray(new JObject{["id"]="preset-a",["name"]="saved",["definitionJson"]="{\"opaque\":true}",["revision"]=1,["updatedAt"]="2026-09-10T00:00:00.000Z"})},
            ["unlocks"]=new JArray(new JObject{["unlockId"]="opn-a",["unlockedAt"]="2026-09-10T00:00:00.000Z"})
        }.ToString();
        sealed class Wire : IRunApiHttpTransport
        {
            public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
            public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);return Reply(r);}
        }
        static Task<RunApiHttpResponse> Ok(string body)=>Task.FromResult(RunApiHttpResponse.Success(200,body));
        [Test] public void CompleteSnapshotKeepsServerFieldsWithoutInventingFormation()
        {
            var s=AccountStateSnapshot.Parse(Json(),"account_test");
            Assert.That(s.Currencies["CREDITS"],Is.EqualTo(123));Assert.That(s.Fuel.Number("current"),Is.EqualTo(7));Assert.That(s.Train.Number("level"),Is.EqualTo(2));
            Assert.That(s.Items.Single().Number("quantity"),Is.EqualTo(2));Assert.That(s.Operators.Single().Text("operatorId"),Is.EqualTo("op-a"));
            Assert.That(s.TrainingCoreSlots.Single().Number("slot"),Is.EqualTo(2));Assert.That(s.Carriages.Single().Text("carriageId"),Is.EqualTo("car-a"));
            Assert.That(s.Turrets.Single().Number("level"),Is.EqualTo(3));Assert.That(s.Presets.Single().Text("definitionJson"),Is.EqualTo("{\"opaque\":true}"));
            Assert.That(s.Unlocks.Single().Text("unlockId"),Is.EqualTo("opn-a"));Assert.That(s.ActivationSupported,Is.False);Assert.That(s.ActivePresetId,Is.Null);
        }
        [Test] public void ExplicitAbsentStateIsNotInvented()
        {
            var r=JObject.Parse(Json());r["fuel"]=null;r["train"]=null;
            foreach(var key in new[]{"items","operators","trainingCoreSlots","carriages","turrets","unlocks"})r[key]=new JArray();r["formation"]["presets"]=new JArray();
            var s=AccountStateSnapshot.Parse(r.ToString(),"account_test");Assert.That(s.Fuel,Is.Null);Assert.That(s.Train,Is.Null);Assert.That(s.Items,Is.Empty);Assert.That(s.Operators,Is.Empty);Assert.That(s.Presets,Is.Empty);
        }
        [TestCase("account")][TestCase("missing")][TestCase("negative")][TestCase("slot")][TestCase("activation")][TestCase("duplicate")]
        public void InvalidSnapshotFailsClosed(string variant)
        {
            var r=JObject.Parse(Json());
            if(variant=="account")r["accountId"]="other";if(variant=="missing")r.Remove("fuel");if(variant=="negative")r["currencies"]["CREDITS"]=-1;
            if(variant=="slot")r["trainingCoreSlots"][0]["slot"]=5;if(variant=="activation")r["formation"]["activationSupported"]=true;
            if(variant=="duplicate")((JArray)r["operators"]).Add(r["operators"][0].DeepClone());
            Assert.Throws<FormatException>(()=>AccountStateSnapshot.Parse(r.ToString(),"account_test"));
        }
        [Test] public async Task SameMutationSequenceStillReplacesEntireSnapshot()
        {
            var body=Json();var wire=new Wire{Reply=r=>Ok(body)};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){
                await state.RefreshAccountAsync();var first=state.Account;
                var changed=JObject.Parse(Json(777));changed["items"]=new JArray();body=changed.ToString();await state.RefreshAccountAsync();
                Assert.That(state.Account,Is.Not.SameAs(first));Assert.That(state.Account.MutationSequence,Is.EqualTo(first.MutationSequence));
                Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(777));Assert.That(state.Account.Items,Is.Empty);
                Assert.That(first.Currencies["CREDITS"],Is.EqualTo(123));Assert.That(state.AccountIsStale,Is.False);
            }
        }
        [TestCase(false)][TestCase(true)] public async Task SupersededSuccessAndFailureCannotOverwriteNewestRead(bool oldFailure)
        {
            var replies=new Queue<TaskCompletionSource<RunApiHttpResponse>>();var first=new TaskCompletionSource<RunApiHttpResponse>();var second=new TaskCompletionSource<RunApiHttpResponse>();replies.Enqueue(first);replies.Enqueue(second);
            var wire=new Wire{Reply=r=>replies.Dequeue().Task};var session=SupportInquiryTests.Session();long generation=1;
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation)){
                var a=state.RefreshAccountAsync();generation++;state.SynchronizeSession();var b=state.RefreshAccountAsync();second.SetResult(RunApiHttpResponse.Success(200,Json(456)));await b;
                first.SetResult(oldFailure?RunApiHttpResponse.NetworkError("old"):RunApiHttpResponse.Success(200,Json()));await a;
                Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(456));Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.Ready));
            }
        }
        [TestCase(404,"",AccountStateReadStatus.ApiUnavailable)]
        [TestCase(426,"client_update_required",AccountStateReadStatus.VersionMismatch)]
        [TestCase(409,"content_release_pin_mismatch",AccountStateReadStatus.VersionMismatch)]
        [TestCase(401,"unauthorized",AccountStateReadStatus.AuthRequired)]
        [TestCase(400,"contract_violation",AccountStateReadStatus.ContractMismatch)]
        [TestCase(503,"account_state_unavailable",AccountStateReadStatus.Unavailable)]
        public async Task ErrorKeepsPreviousValuesVisiblyStale(int code,string error,AccountStateReadStatus expected)
        {
            var wire=new Wire{Reply=r=>Ok(Json())};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){
                await state.RefreshAccountAsync();var snapshot=state.Account;
                wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(code,new JObject{["accepted"]=false,["errorCode"]=error}.ToString()));await state.RefreshAccountAsync();
                Assert.That(state.Account,Is.SameAs(snapshot));Assert.That(state.AccountStatus,Is.EqualTo(expected));Assert.That(state.AccountIsStale,Is.True);
                Assert.That(state.BalanceText(1),Is.EqualTo("123 *"));Assert.That(state.AccountNotice,Does.Contain("이전 정보"));
            }
        }
        [Test] public async Task GenerationChangeClearsSnapshotAndDiscardsOldFailure()
        {
            var wire=new Wire{Reply=r=>Ok(Json())};var session=SupportInquiryTests.Session();long generation=1;
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation)){
                await state.RefreshAccountAsync();var pending=new TaskCompletionSource<RunApiHttpResponse>();wire.Reply=r=>pending.Task;var task=state.RefreshAccountAsync();
                generation++;state.SynchronizeSession();pending.SetResult(RunApiHttpResponse.NetworkError("old"));await task;
                Assert.That(state.Account,Is.Null);Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.NotLoaded));
            }
        }
        [Test] public async Task ExistingAdmissionHeadersArePreserved()
        {
            var inner=new Wire{Reply=r=>Ok(Json())};var wire=new InstalledAdmissionTransport(inner,new AppBuildSnapshot("1.2.3",123),"android",null);var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){await state.RefreshAccountAsync();}
            var request=inner.Requests.Single();Assert.That(request.Method,Is.EqualTo("GET"));Assert.That(request.BodyJson,Is.Null);
            Assert.That(request.Headers.Any(h=>h.Name=="X-LastRail-App-Version-Code"&&h.Value=="123"),Is.True);
            Assert.That(request.Headers.Any(h=>h.Name=="Authorization"),Is.True);
        }
    }
}
