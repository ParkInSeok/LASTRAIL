using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using LastRail.Tests.EditMode;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public sealed class DepartureChangeReturnTests
{
    static JObject Rejection(params string[] fields)=>new JObject{["accepted"]=false,["errorCode"]="departure_quote_changed",["departureChange"]=new JObject{["version"]="departure.quote-change.v1",["operationPreviewId"]="preview_test",["newAttemptAuthorized"]=false,["changedFields"]=new JArray(fields)}};
    [TestCase("COST","None")][TestCase("FORMATION","Account, Ultimate")][TestCase("LOADOUT","Train")][TestCase("OWNED_STATE","Account, Train")][TestCase("ULTIMATE","Ultimate")]
    public void NamesOnlyInvalidateTheirOwnersWithoutInventingNewValues(string field,string expected)
    {
        var type=typeof(DepartureContract).Assembly.GetType("LastRail.Presentation.MissionShell.DepartureRejection");Assert.IsNotNull(type);
        var parsed=type.GetMethod("Read").Invoke(null,new object[]{Rejection(field),"preview_test"});
        Assert.AreEqual(expected,type.GetProperty("Refresh").GetValue(parsed).ToString());
        StringAssert.StartsWith("출격 조건이 변경되었습니다.",type.GetProperty("Notice").GetValue(parsed).ToString());
    }
    [Test] public void ChangeListUsesOnlyBoundFieldsAndRejectsMalformedOrForeignDetails()
    {
        var type=typeof(DepartureContract).Assembly.GetType("LastRail.Presentation.MissionShell.DepartureRejection");Assert.IsNotNull(type);
        var r=Rejection("COST","LOADOUT");var parsed=type.GetMethod("Read").Invoke(null,new object[]{r,"preview_test"});
        var notice=(string)type.GetProperty("Notice").GetValue(parsed);StringAssert.Contains("출격 비용",notice);StringAssert.Contains("열차 배치",notice);StringAssert.DoesNotContain("대원 편성",notice);
        r["departureChange"]["operationPreviewId"]="other";
        parsed=type.GetMethod("Read").Invoke(null,new object[]{r,"preview_test"});Assert.AreEqual("None",type.GetProperty("Refresh").GetValue(parsed).ToString());
        StringAssert.DoesNotContain("열차 배치",type.GetProperty("Notice").GetValue(parsed).ToString());
    }
    sealed class Journal:IAccountActionJournal
    {
        public PendingAccountAction Saved=new PendingAccountAction("account_test","departure-preview","opn_01_last_departure",0,"change_return_key","{\"operationId\":\"opn_01_last_departure\",\"risk\":\"I\",\"expectedFormationRevision\":1,\"expectedLoadoutRevision\":1,\"expectedUltimateRevision\":1}");
        public bool FailRemove; public PendingAccountAction Read(string a)=>Saved;public bool Save(PendingAccountAction a){Saved=a;return true;}public bool Remove(PendingAccountAction a){if(FailRemove)return false;Saved=null;return true;}
    }
    sealed class Wire:IRunApiHttpTransport
    {
        public readonly List<string> Calls=new List<string>();public readonly TaskCompletionSource<RunApiHttpResponse> Reply=new TaskCompletionSource<RunApiHttpResponse>();
        public TaskCompletionSource<RunApiHttpResponse> FirstTrain;
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Calls.Add(r.EndpointPath);if(r.EndpointPath=="/v1/trains/loadout"&&FirstTrain!=null&&Calls.Count(x=>x==r.EndpointPath)==1)return FirstTrain.Task;return r.Method=="POST"?Reply.Task:Task.FromResult(RunApiHttpResponse.Success(503,"{}"));}
    }
    [TestCase("combat_not_ready",0)][TestCase("operation_preview_not_startable",0)][TestCase("insufficient_fuel",1)][TestCase("departure_selection_changed",3)]
    public async Task KnownRejectionRefreshesOnlyInvalidatedPreparationOwners(string code,int reads)
    {
        var wire=new Wire();var journal=new Journal();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
        var task=state.RetryPendingAsync();wire.Reply.SetResult(RunApiHttpResponse.Success(409,new JObject{["accepted"]=false,["errorCode"]=code}.ToString()));await task;
        Assert.IsNull(state.Pending);Assert.IsNull(journal.Saved);Assert.AreEqual(code,state.ActionErrorCode);
        Assert.AreEqual(1+reads,wire.Calls.Count);Assert.AreEqual(1,wire.Calls.Count(x=>x=="/v1/operations/preview"));
        Assert.IsFalse(wire.Calls.Contains("/v1/wallet/crystal"));Assert.IsFalse(wire.Calls.Contains("/v1/growth/config"));
        Assert.IsNull(state.Departure);Assert.IsFalse(state.CanConfirmDeparture);
    }
    [TestCase(false)][TestCase(true)] public async Task UnknownResultOrFailedRemovalRetainsOriginalAndDoesNotRefresh(bool removalFailure)
    {
        var wire=new Wire();var journal=new Journal{FailRemove=removalFailure};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
        var task=state.RetryPendingAsync();wire.Reply.SetResult(RunApiHttpResponse.Success(removalFailure?409:503,"{\"accepted\":false,\"errorCode\":\"departure_selection_changed\"}"));await task;
        Assert.AreEqual("change_return_key",state.Pending.Key);Assert.AreSame(journal.Saved,state.Pending);Assert.AreEqual(1,wire.Calls.Count);Assert.IsNull(state.DepartureConditionNotice);
    }
    [Test] public async Task RejectionCannotJoinOrAcceptReadStartedBeforeInvalidation()
    {
        var wire=new Wire{FirstTrain=new TaskCompletionSource<RunApiHttpResponse>()};var journal=new Journal();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
        var stale=state.RefreshTrainLoadoutAsync();var rejection=state.RetryPendingAsync();
        wire.Reply.SetResult(RunApiHttpResponse.Success(409,"{\"accepted\":false,\"errorCode\":\"departure_selection_changed\"}"));
        Assert.IsTrue(rejection.IsCompleted,"The rejected selection needs a new read, not the previous in-flight task.");await rejection;
        Assert.AreEqual(2,wire.Calls.Count(x=>x=="/v1/trains/loadout"));
        var old=typeof(TrainLoadoutTests).GetMethod("Snapshot",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{1L}).ToString();
        wire.FirstTrain.SetResult(RunApiHttpResponse.Success(200,old));await stale;
        Assert.IsFalse(state.TrainLoadoutReady);Assert.IsNull(state.TrainLoadout);Assert.IsNull(state.Departure);
        Assert.AreEqual(1,wire.Calls.Count(x=>x=="/v1/operations/preview"));
    }
}
