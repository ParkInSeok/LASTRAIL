using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class StoryArchiveReadModelTests
    {
        public static string Reply(params string[] unlocked)=>new JObject{
            ["accepted"]=true,["actId"]="act_01_wasteland_railway",
            ["storyProgress"]=new JObject{["highestStoryProgressId"]="none",["highestClearedOperationId"]=null,["order"]=0},
            ["clearedOperationIds"]=new JArray(),["unlockedOperations"]=new JArray(unlocked),
            ["narrativeVersion"]="fixture-narrative-v1",
            ["narrative"]=new JObject{["narrativeVersion"]="fixture-narrative-v1",["contentStatus"]="CANDIDATE",["localizationStatus"]="PENDING",["fixtureStatus"]="PARTIAL",["missingContentStatus"]="UNKNOWN"}
        }.ToString();
        sealed class Wire:IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Reply(request);}
        }
        [Test] public async Task ExistingRecordPermissionUsesOnlyExplicitLists()
        {
            var body=JObject.Parse(Reply("opn_01_last_departure"));body["storyProgress"]["order"]=6;body["storyProgress"]["highestClearedOperationId"]="opn_06_broken_line";
            var wire=new Wire{Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,body.ToString()))};var session=SupportInquiryTests.Session();
            using var model=new StoryArchiveReadModel(wire,()=>session,()=>1);await model.RefreshAsync();
            Assert.That(model.Status,Is.EqualTo(StoryArchiveReadStatus.Ready));Assert.That(model.State.ClearedOperationIds,Is.Empty);
            var archive=CodexArchivePresentationState.CreateActOne(model.State,MissionNarrativeCatalog.CreateActOne(),new Dictionary<string,string>());
            Assert.IsTrue(archive.Entries.Single(x=>x.MissionId=="opn_01_last_departure").CanOpenForDisplay);
            Assert.IsFalse(archive.Entries.Single(x=>x.MissionId=="opn_02_first_signal").CanOpenForDisplay,"Highest order does not grant preceding records.");
            Assert.That(wire.Requests.Single().Method,Is.EqualTo("GET"));Assert.That(wire.Requests.Single().EndpointPath,Is.EqualTo("/v1/story-progress"));Assert.That(wire.Requests.Single().BodyJson,Is.Null);
        }
        [Test] public async Task PreviousGenerationFailureCannotReplaceNewerRead()
        {
            var first=new TaskCompletionSource<RunApiHttpResponse>();var second=new TaskCompletionSource<RunApiHttpResponse>();int calls=0;
            var wire=new Wire{Reply=_=>++calls==1?first.Task:second.Task};var session=SupportInquiryTests.Session();long generation=1;using var model=new StoryArchiveReadModel(wire,()=>session,()=>generation);
            var a=model.RefreshAsync();generation++;model.SynchronizeSession();var b=model.RefreshAsync();second.SetResult(RunApiHttpResponse.Success(200,Reply("opn_02_first_signal")));await b;first.SetResult(RunApiHttpResponse.NetworkError("old"));await a;
            Assert.That(model.Status,Is.EqualTo(StoryArchiveReadStatus.Ready));Assert.That(model.State.UnlockedOperations,Is.EqualTo(new[]{"opn_02_first_signal"}));
        }
        [Test] public async Task OwnerChangeDropsDelayedUnlocks()
        {
            var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=_=>pending.Task};var session=SupportInquiryTests.Session();long generation=1;using var model=new StoryArchiveReadModel(wire,()=>session,()=>generation);
            var read=model.RefreshAsync();session=null;generation++;model.SynchronizeSession();pending.SetResult(RunApiHttpResponse.Success(200,Reply("opn_01_last_departure")));await read;
            Assert.That(model.State,Is.Null);Assert.That(model.Status,Is.EqualTo(StoryArchiveReadStatus.AuthRequired));
        }
        [TestCase("rejected")][TestCase("missing")][TestCase("duplicate")][TestCase("version")]
        public async Task InvalidResponseNeverFallsBackToInitialUnlock(string variant)
        {
            var body=JObject.Parse(Reply("opn_01_last_departure"));if(variant=="rejected")body["accepted"]=false;if(variant=="missing")body.Remove("unlockedOperations");if(variant=="duplicate")((JArray)body["unlockedOperations"]).Add("opn_01_last_departure");if(variant=="version")body["narrative"]["narrativeVersion"]="other";
            var wire=new Wire{Reply=_=>Task.FromResult(RunApiHttpResponse.Success(200,body.ToString()))};var session=SupportInquiryTests.Session();long generation=1;using var model=new StoryArchiveReadModel(wire,()=>session,()=>generation);await model.RefreshAsync();
            Assert.That(model.Status,Is.EqualTo(StoryArchiveReadStatus.Unavailable));Assert.That(model.State,Is.Null);Assert.That(model.ErrorCode,Is.EqualTo("invalid_response"));
        }
        [Test] public async Task DisposeDisconnectsDelayedNotification()
        {
            var pending=new TaskCompletionSource<RunApiHttpResponse>();var wire=new Wire{Reply=_=>pending.Task};var session=SupportInquiryTests.Session();var model=new StoryArchiveReadModel(wire,()=>session,()=>1);int changes=0;model.Changed+=()=>changes++;
            var read=model.RefreshAsync();model.Dispose();int before=changes;pending.SetResult(RunApiHttpResponse.Success(200,Reply("opn_01_last_departure")));await read;Assert.That(changes,Is.EqualTo(before));Assert.That(model.State,Is.Null);
        }
    }
}
