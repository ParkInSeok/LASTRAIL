using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class RecruitmentScreenControllerTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Reply(request);}
        }
        static RecruitmentScreenController Controller(AccountScreenRuntime account,Action<RecruitmentScreenState> render)=>new RecruitmentScreenController(account,render,(title,message,allowed,submit)=>Assert.Fail("Unavailable recruitment cannot request confirmation"),()=>true);
        static RunApiHttpResponse EmptyHistory()=>RunApiHttpResponse.Success(200,"{\"accepted\":true,\"batches\":[],\"nextBeforeBatchId\":null}");
        static JObject Banner()=>new JObject{
            ["bannerId"]="b",["bannerType"]="STANDARD",["balanceVersion"]="1",["pickupOperatorId"]=null,["startsAt"]="2026-09-10T00:00:00.000Z",["endsAt"]=null,["availability"]="ACTIVE",
            ["pityConfig"]=new JObject{["rare"]=10,["elite"]=30,["pickup"]=60},["pityState"]=new JObject{["totalPullCount"]=0,["elitePityCount"]=0,["pickupElitePityCount"]=0,["rareOrHigherPityCount"]=0,["revision"]=0},["nextGuarantee"]="NONE",
            ["prices"]=new JArray(new JObject{["pullCount"]=1,["quantity"]=1,["itemId"]="cur_command_ticket"},new JObject{["pullCount"]=10,["quantity"]=10,["itemId"]="cur_command_ticket"}),
            ["probabilities"]=new JArray(new JObject{["resultId"]="op_vex",["resultType"]="ELITE_OPERATOR",["resultRarity"]="ELITE",["quantity"]=1,["isPickup"]=false,["baseProbability"]=.005,["nextPullProbability"]=1})};
        [Test] public async Task HistoryRevisitAndRenderReuseReadButExplicitRefreshQueries()
        {
            var wire=new Wire{Reply=r=>Task.FromResult(EmptyHistory())};var session=SupportInquiryTests.Session();
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);RecruitmentScreenState display=null;
            using var controller=Controller(account,s=>display=s);
            await controller.SelectModeAsync(RecruitmentScreenMode.History);Assert.AreEqual(1,wire.Requests.Count);Assert.AreEqual("모집 기록이 없습니다.",display.Title);
            await controller.SelectModeAsync(RecruitmentScreenMode.Main);await controller.SelectModeAsync(RecruitmentScreenMode.History);controller.Render();controller.MovePage(-1);
            Assert.AreEqual(1,wire.Requests.Count);Assert.AreEqual(0,display.Page);
            await controller.RefreshHistoryAsync();Assert.AreEqual(2,wire.Requests.Count);
            controller.RequestRecruit(1);Assert.AreEqual(2,wire.Requests.Count);
        }
        [Test] public async Task DisposedBindingCannotRenderOrIssueCommands()
        {
            var wire=new Wire{Reply=r=>Task.FromResult(EmptyHistory())};var session=SupportInquiryTests.Session();
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);int renders=0;
            var controller=Controller(account,s=>renders++);controller.Render();controller.Dispose();controller.Dispose();int before=renders;
            await account.RefreshHistoryAsync();int reads=wire.Requests.Count;
            controller.Render();controller.MovePage(1);controller.RequestRecruit(1);await controller.SelectModeAsync(RecruitmentScreenMode.History);await controller.LoadAsync();await controller.RefreshAsync();await controller.MoreAsync();
            Assert.AreEqual(before,renders);Assert.AreEqual(reads,wire.Requests.Count);
            using var replacement=Controller(account,s=>renders++);replacement.Render();Assert.AreEqual(before+1,renders);
        }
        [TestCase(false)][TestCase(true)] public async Task PendingInitialReadCannotSelectBannerAfterScreenOrAccountChanges(bool changeAccount)
        {
            var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=r=>r.EndpointPath.StartsWith("/v1/gacha/banners?")?pending.Task:Task.FromResult(RunApiHttpResponse.NetworkError("offline"))};
            var session=SupportInquiryTests.Session();long generation=1;
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>generation);int renders=0;using var controller=Controller(account,s=>renders++);
            var load=controller.LoadAsync();int before=renders;
            if(changeAccount){session=SupportInquiryTests.Session("other");generation++;account.SynchronizeSession();}else controller.Dispose();
            pending.SetResult(RunApiHttpResponse.Success(200,new JObject{["accepted"]=true,["serverTime"]="2026-09-10T00:00:00.000Z",["banners"]=new JArray(Banner()),["nextAfterBannerId"]=null}.ToString()));await load;
            Assert.IsFalse(wire.Requests.Any(r=>r.EndpointPath=="/v1/gacha/banners/b"));Assert.AreEqual(before,renders);
        }
        [Test] public async Task RatesUseTypedDisplayAndPreserveBaseAndNextPullValues()
        {
            var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(200,new JObject{["accepted"]=true,["serverTime"]="2026-09-10T00:00:00.000Z",["banner"]=Banner()}.ToString()))};var session=SupportInquiryTests.Session();
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);await account.SelectBannerAsync("b");RecruitmentScreenState display=null;
            using var controller=Controller(account,s=>display=s);await controller.SelectModeAsync(RecruitmentScreenMode.Rates);
            Assert.IsTrue(display.RatesReady);Assert.That(display.Rows.Single().Label,Does.Contain("0.5% / 100%"));Assert.AreEqual(1,wire.Requests.Count);
        }
    }
}
