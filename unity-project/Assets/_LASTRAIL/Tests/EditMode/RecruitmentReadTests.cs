using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
    public sealed class RecruitmentReadTests
    {
        static JObject Pity()=>new JObject{["totalPullCount"]=0,["elitePityCount"]=0,["pickupElitePityCount"]=0,["rareOrHigherPityCount"]=0,["revision"]=0};
        static JObject Banner(string id)=>new JObject{["bannerId"]=id,["bannerType"]="STANDARD",["balanceVersion"]="1",["pickupOperatorId"]=null,["startsAt"]="2026-09-10T00:00:00.000Z",["endsAt"]=null,["availability"]="ACTIVE",["pityConfig"]=new JObject{["rare"]=10,["elite"]=30,["pickup"]=60},["pityState"]=Pity(),["nextGuarantee"]="NONE",["prices"]=new JArray(new JObject{["pullCount"]=1,["quantity"]=1,["itemId"]="cur_command_ticket"},new JObject{["pullCount"]=10,["quantity"]=10,["itemId"]="cur_command_ticket"}),["probabilities"]=new JArray(new JObject{["resultId"]="op_vex",["resultType"]="ELITE_OPERATOR",["resultRarity"]="ELITE",["quantity"]=1,["isPickup"]=false,["baseProbability"]=.005,["nextPullProbability"]=1})};
        static JObject Batch()=>new JObject{["batchId"]="batch_old",["bannerId"]="ended",["pullCount"]=1,["balanceVersion"]=null,["cost"]=new JObject{["itemId"]="cur_command_ticket",["quantity"]=1},["createdAt"]="2026-09-10T00:00:00.000Z",["postPityState"]=null,["recoveryStatus"]="INCOMPLETE",["missingSequences"]=new JArray(),["unknownQuantitySequences"]=new JArray(1),["results"]=new JArray(new JObject{["pullSequence"]=1,["resultId"]="op_vex",["resultType"]="ELITE_OPERATOR",["resultRarity"]="ELITE",["isPickup"]=false,["wasDuplicate"]=true,["duplicateConversion"]=null,["reward"]=new JObject{["kind"]="UNKNOWN",["id"]=null,["quantity"]=null}})};
        [Test] public void UnknownDuplicateQuantityNeverBecomesOperatorCountOrCurrentPolicy()
        {
            var raw=Batch();var batch=new RecruitmentBatch(raw);Assert.That(batch.Version,Is.Null);Assert.That(batch.Complete,Is.False);Assert.That(batch.ResultLine(1),Does.Contain("수량 확인 필요"));Assert.That(batch.ResultLine(1),Does.Not.Contain("× 1"));
            raw["results"][0]["reward"]=new JObject{["kind"]="OPERATOR",["id"]="op_vex",["quantity"]=15};raw["unknownQuantitySequences"]=new JArray();raw["recoveryStatus"]="COMPLETE";Assert.Throws<FormatException>(()=>new RecruitmentBatch(raw));
        }
        [Test] public void ProbabilityFractionsAndSnapshotsAreNotMutatedByCallers()
        {
            var raw=Banner("b");var banner=new RecruitmentBanner(raw);raw["probabilities"][0]["baseProbability"]=20;Assert.That((double)banner.Probabilities[0]["baseProbability"],Is.EqualTo(.005));Assert.Throws<FormatException>(()=>new RecruitmentBanner(raw));
            var copy=banner.Probabilities;copy[0]["nextPullProbability"]=0;Assert.That((double)banner.Probabilities[0]["nextPullProbability"],Is.EqualTo(1));
        }
        [Test] public void MissingSequenceAndCompletenessMustAgree()
        {
            var raw=Batch();raw["results"]=new JArray();raw["unknownQuantitySequences"]=new JArray();raw["missingSequences"]=new JArray(1);var batch=new RecruitmentBatch(raw);Assert.That(batch.ResultLine(1),Does.Contain("결과 확인 필요"));raw["recoveryStatus"]="COMPLETE";Assert.Throws<FormatException>(()=>new RecruitmentBatch(raw));
        }
        sealed class Wire:IRunApiHttpTransport {public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;public List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);return Reply(r);}}
        sealed class Journal:IAccountActionJournal {public PendingAccountAction Value;public PendingAccountAction Read(string id)=>Value;public bool Save(PendingAccountAction a){Value=a;return true;}public bool Remove(PendingAccountAction a){Value=null;return true;}}
        static JObject CompleteBatch(string id){var r=Batch();r["batchId"]=id;r["accepted"]=true;r["recoveryStatus"]="COMPLETE";r["unknownQuantitySequences"]=new JArray();r["results"][0]["wasDuplicate"]=false;r["results"][0]["reward"]=new JObject{["kind"]="ITEM",["id"]="mat_scrap",["quantity"]=7};return r;}
        [Test] public async Task OpeningAnotherHistoryBatchCannotResolveAnUnfinishedPull()
        {
            var a=new TaskCompletionSource<RunApiHttpResponse>();bool delay=true;var journal=new Journal();var session=SupportInquiryTests.Session();
            var wire=new Wire{Reply=r=>{
                if(r.Method=="POST")return Task.FromResult(RunApiHttpResponse.Success(200,"{\"accepted\":true,\"resultStatus\":\"ACCEPTED\",\"bannerId\":\"ended\",\"pullCount\":1,\"batchId\":\"batch_a\"}"));
                if(r.EndpointPath.EndsWith("/batch_a"))return delay?a.Task:Task.FromResult(RunApiHttpResponse.Success(200,CompleteBatch("batch_a").ToString()));
                if(r.EndpointPath.EndsWith("/batch_b"))return Task.FromResult(RunApiHttpResponse.Success(200,CompleteBatch("batch_b").ToString()));
                return Task.FromResult(RunApiHttpResponse.NetworkError("offline"));
            }};
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                var pull=state.PullAsync("ended",1);await state.RefreshBatchAsync("batch_b");a.SetResult(RunApiHttpResponse.Success(200,CompleteBatch("batch_a").ToString()));await pull;
                Assert.That(state.Pending,Is.Not.Null,"Other history result must not clear the acknowledged request.");Assert.That(state.RecoveryBatchId,Is.EqualTo("batch_a"));
                delay=false;await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null);Assert.That(state.RecoveryBatchId,Is.Null);Assert.That(state.SelectedBatch.Id,Is.EqualTo("batch_a"));
            }
        }
        static RunApiHttpResponse List(string id=null,string cursor=null)=>RunApiHttpResponse.Success(200,new JObject{["accepted"]=true,["serverTime"]="2026-09-10T00:00:00.000Z",["banners"]=id==null?new JArray():new JArray(Banner(id)),["nextAfterBannerId"]=cursor}.ToString());
        [TestCase(false)][TestCase(true)] public async Task NewestBannerRefreshRejectsOldSuccessAndFailure(bool oldFailure)
        {
            var old=new TaskCompletionSource<RunApiHttpResponse>();int calls=0;var wire=new Wire{Reply=r=>++calls==1?old.Task:Task.FromResult(List("new"))};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){var first=state.RefreshBannersAsync();await state.RefreshBannersAsync();if(oldFailure)old.SetException(new Exception("stale"));else old.SetResult(List("old"));await first;Assert.That(state.Banners.Single().Id,Is.EqualTo("new"));Assert.That(state.BannersReady,Is.True);Assert.That(state.RecruitmentNotice,Is.Empty);}
        }
        [Test] public async Task CursorOnlyUsedForContinuationAndFailureKeepsMarkedOldRows()
        {
            int calls=0;var wire=new Wire{Reply=r=>Task.FromResult(++calls==1?List("a","a"):calls==2?List("b"):RunApiHttpResponse.NetworkError("offline"))};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>1)){await state.RefreshBannersAsync();await state.RefreshBannersAsync(true);Assert.That(state.Banners.Select(b=>b.Id),Is.EqualTo(new[]{"a","b"}));Assert.That(wire.Requests[1].EndpointPath,Does.EndWith("&afterBannerId=a"));await state.RefreshBannersAsync();Assert.That(wire.Requests[2].EndpointPath,Is.EqualTo("/v1/gacha/banners?limit=20"));Assert.That(wire.Requests.All(r=>r.BodyJson==null),Is.True);Assert.That(state.BannersReady,Is.False);Assert.That(state.Banners.Count,Is.EqualTo(2));}
        }
        [Test] public async Task ChangedAccountDiscardsRecruitmentReadAndClearsOldSelection()
        {
            var delayed=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>delayed.Task};var session=SupportInquiryTests.Session();long generation=1;
            using(var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation)){var read=state.RefreshBannersAsync();session=SupportInquiryTests.Session("other");generation++;state.SynchronizeSession();delayed.SetResult(List("private_old"));await read;Assert.That(state.Banners,Is.Empty);Assert.That(state.SelectedBanner,Is.Null);Assert.That(state.History,Is.Empty);}
        }
    }
}
