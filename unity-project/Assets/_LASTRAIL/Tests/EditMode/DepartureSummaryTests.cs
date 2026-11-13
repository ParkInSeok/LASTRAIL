using System;
using System.IO;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Run;
using LastRail.Tests.EditMode;

public sealed class DepartureSummaryTests
{
    sealed class Journal:IAccountActionJournal
    {
        PendingAccountAction saved=new PendingAccountAction("account_test","departure-preview","opn_01_last_departure",0,"summary_original","{\"operationId\":\"opn_01_last_departure\",\"risk\":\"I\",\"expectedFormationRevision\":1,\"expectedLoadoutRevision\":1,\"expectedUltimateRevision\":1}");
        public PendingAccountAction Read(string account)=>saved;public bool Save(PendingAccountAction action){saved=action;return true;}public bool Remove(PendingAccountAction action){saved=null;return true;}
    }
    sealed class Wire:IRunApiHttpTransport
    {
        public int Calls;public long LoadoutRevision=1;public readonly TaskCompletionSource<RunApiHttpResponse> Preview=new TaskCompletionSource<RunApiHttpResponse>();
        public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){
            Calls++;if(request.EndpointPath=="/v1/operations/preview")return Preview.Task;
            string json;
            if(request.EndpointPath=="/v1/trains/loadout")json=typeof(TrainLoadoutTests).GetMethod("Snapshot",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{LoadoutRevision}).ToString();
            else if(request.EndpointPath=="/v1/formation/ultimate")json="{\"accepted\":true,\"rulesVersion\":\"ultimate-selection-v1\",\"operatorId\":\"op_eve\",\"revision\":1,\"formationRevision\":1,\"selectionStatus\":\"SELECTED\",\"changeLocked\":false}";
            else {Assert.AreEqual("/v1/account/state",request.EndpointPath);var account=JObject.Parse(AccountStateReadTests.Json());var form=(JObject)account["formation"];form["activationSupported"]=true;form["revision"]=1;form["activePresetId"]="preset-a";form["activePresetRevision"]=1;form["updatedAt"]="2026-09-10T00:00:00.000Z";form["operators"]=Quote().Admission["formation"]["operators"];json=account.ToString();}
            return Task.FromResult(RunApiHttpResponse.Success(200,json));
        }
    }
    [Test] public async Task LatePreviewRetainsOriginalButDoesNotMatchChangedLoadoutOrFetchAgain()
    {
        var wire=new Wire();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
        await state.RefreshAccountAsync();await state.RefreshTrainLoadoutAsync();await state.RefreshUltimateAsync();
        var pending=state.RetryPendingAsync();wire.LoadoutRevision=2;await state.RefreshTrainLoadoutAsync();int before=wire.Calls;
        wire.Preview.SetResult(RunApiHttpResponse.Success(200,File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Departure549/server-quote.json")));await pending;
        Assert.IsNotNull(state.Departure);StringAssert.Contains("열차 배치",state.DepartureSelectionNotice);Assert.IsFalse(state.CanConfirmDeparture);Assert.AreEqual(before,wire.Calls);
        Assert.AreEqual(1L,(long)state.Departure.Admission["loadoutRevision"]);Assert.AreEqual(2,state.TrainLoadout.Revision);
    }
    [Test] public void RejectionDetailSurvivesInBlockListAndClearsWithSession()
    {
        var wire=new Wire();var session=SupportInquiryTests.Session();long generation=1;using var state=new AccountScreenRuntime(wire,null,()=>session,()=>generation);
        typeof(AccountScreenRuntime).GetProperty("DepartureConditionNotice").SetValue(state,"열차 배치 변경 안내");
        var root=new UnityEngine.GameObject("SummaryBlocks",typeof(UnityEngine.RectTransform));
        try{var deck=root.AddComponent<LobbyCommandDeck>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(LobbyCommandDeck).GetField("accountState",flags).SetValue(deck,state);
            var blocks=(DepartureBlockState)typeof(LobbyCommandDeck).GetMethod("CurrentDepartureBlocks",flags).Invoke(deck,null);
            Assert.IsTrue(System.Linq.Enumerable.Any(blocks.Reasons,x=>x.Code=="quote"&&x.Message=="열차 배치 변경 안내"));
            generation++;state.SynchronizeSession();Assert.IsNull(state.DepartureConditionNotice);Assert.AreEqual(0,wire.Calls);
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    static DepartureQuote Quote()=>new DepartureQuote(File.ReadAllText("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/Departure549/server-quote.json"),"{\"operationId\":\"opn_01_last_departure\",\"risk\":\"I\",\"expectedFormationRevision\":1,\"expectedLoadoutRevision\":1,\"expectedUltimateRevision\":1}");
    [Test] public void QuoteIdentifiesChangedSelectionWithoutMutatingPinnedAdmission()
    {
        var quote=Quote();var original=quote.Admission.ToString();var method=typeof(DepartureQuote).GetMethod("SelectionChangeNotice");Assert.IsNotNull(method);
        Assert.IsNull(method.Invoke(quote,new object[]{1L,1L,1L}));
        var notice=(string)method.Invoke(quote,new object[]{2L,3L,4L});
        StringAssert.Contains("대원 편성",notice);StringAssert.Contains("열차 배치",notice);StringAssert.Contains("궁극기",notice);
        Assert.AreEqual(original,quote.Admission.ToString());
        var copy=quote.Admission;copy["formation"]["operators"][0]["operatorId"]="changed";Assert.AreEqual(original,quote.Admission.ToString());
    }
    [TestCase("valid")][TestCase("foreign")][TestCase("version")][TestCase("authorize")][TestCase("unknown")][TestCase("objectVersion")][TestCase("arrayPreview")]
    public void OnlyBoundKnownChangeDetailsAreShown(string mutation)
    {
        var response=JObject.Parse("{\"accepted\":false,\"errorCode\":\"departure_quote_changed\",\"departureChange\":{\"version\":\"departure.quote-change.v1\",\"operationPreviewId\":\"preview_test\",\"changedFields\":[\"FORMATION\",\"LOADOUT\"],\"newAttemptAuthorized\":false}}");
        var detail=(JObject)response["departureChange"];
        if(mutation=="foreign")detail["operationPreviewId"]="other";
        if(mutation=="version")detail["version"]="future";
        if(mutation=="authorize")detail["newAttemptAuthorized"]=true;
        if(mutation=="unknown")detail["changedFields"]=new JArray("UNTRUSTED_MESSAGE");
        if(mutation=="objectVersion")detail["version"]=new JObject();
        if(mutation=="arrayPreview")detail["operationPreviewId"]=new JArray();
        var method=typeof(DepartureContract).GetMethod("RejectionNotice");Assert.IsNotNull(method);
        var notice=(string)method.Invoke(null,new object[]{response,"preview_test"});
        if(mutation=="valid"){StringAssert.Contains("대원 편성",notice);StringAssert.Contains("열차 배치",notice);}
        else{StringAssert.DoesNotContain("대원 편성",notice);StringAssert.DoesNotContain("UNTRUSTED",notice);}
    }
}
