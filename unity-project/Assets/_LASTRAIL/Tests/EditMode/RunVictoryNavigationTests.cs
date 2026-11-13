using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class RunVictoryNavigationTests
    {
        [TestCase(false)][TestCase(true)]
        public async Task KnownTerminalRunOpensFromLobbyWithoutReadsEvenAfterCredentialRotation(bool rotate)
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await account.ReadRunResultsAsync("run-smoke-1");
            var original=account.Lifecycle;
            var originalPreview=account.RunRewardPreview;
            var root=new GameObject("KnownRunNavigation",typeof(RectTransform),typeof(Canvas));
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                if(rotate){session=SupportInquiryTests.Session();account.SynchronizeSession();}
                Assert.AreSame(original,account.Lifecycle,"Credential rotation must retain the known terminal owner state.");
                Assert.AreSame(originalPreview,account.RunRewardPreview,"A valid reward display must survive credential rotation too.");
                int reads=wire.Reads;deck.OpenOperations();
                Assert.AreEqual(reads,wire.Reads,"Opening an existing terminal record is not invalidation.");
                Assert.IsNotNull(root.GetComponentInChildren<RunVictoryView>(),"Terminal record must route to results, not old START recovery.");
                Assert.IsFalse(root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="ClaimRunReward").interactable,"Cached rendering must apply the existing claim eligibility.");
                Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="ResultRecordText").text,Is.Not.Empty,"Cached rendering must populate the record body.");
                Assert.AreEqual(0,wire.Writes);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public async Task LifecycleOnlyEntryLoadsMissingRewardWithoutRepeatingLifecycle()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await account.RefreshRunLifecycleAsync("run-smoke-1");Assert.IsNull(account.RunRewardPreview);
            var root=new GameObject("MissingRewardNavigation",typeof(RectTransform),typeof(Canvas));
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                int reads=wire.Reads;deck.OpenOperations();
                Assert.IsNotNull(account.RunRewardPreview);Assert.AreEqual(reads+1,wire.Reads,"Only missing reward detail is needed.");
                reads=wire.Reads;deck.OpenOperations();Assert.AreEqual(reads,wire.Reads);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        const string FixtureRoot = "D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
        sealed class Wire : IRunApiHttpTransport
        {
            public int Reads, Writes;
            public bool Empty, LifecycleUnavailable;
            public int ProgressOrder;
            public int StoryStatus=200;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method!="GET"){Writes++;throw new InvalidOperationException("No writes in victory navigation QA");}
                Reads++;
                if(request.EndpointPath=="/v1/story-progress")return Task.FromResult(RunApiHttpResponse.Success(StoryStatus,StoryProgressObservationTests.Observation(ProgressOrder,ProgressOrder).ToString()));
                if(request.EndpointPath.EndsWith("/lifecycle"))
                {
                    if(LifecycleUnavailable)return Task.FromResult(RunApiHttpResponse.Success(503,"{}"));
                    var json=JObject.Parse(File.ReadAllText(FixtureRoot+"result-lost-finish-restored.json"));
                    // Explicit derived QA outcome; original server artifact remains unchanged.
                    json["runStatus"]="COMPLETED";json["finish"]["outcome"]="VICTORY";
                    return Task.FromResult(RunApiHttpResponse.Success(200,json.ToString()));
                }
                if(request.EndpointPath.Contains("/reward-preview/")){var preview=JObject.Parse(File.ReadAllText(FixtureRoot+"result-preview.json"));if(Empty)preview["rewardLines"]=new JArray();return Task.FromResult(RunApiHttpResponse.Success(200,preview.ToString()));}
                return Task.FromResult(RunApiHttpResponse.Success(404,"{}"));
            }
        }
        sealed class PendingJournal : IRunOperationJournal
        {
            readonly PendingRunOperation pending;
            public PendingJournal(PendingRunOperation pending){this.pending=pending;}
            public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>Task.FromResult<IReadOnlyList<PendingRunOperation>>(new[]{pending});
            public Task<bool> PutAsync(PendingRunOperation operation)=>throw new InvalidOperationException("Read-only fixture");
            public Task<bool> ConfirmAsync(PendingRunOperation operation,string response)=>throw new InvalidOperationException("Read-only fixture");
            public Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key)=>throw new InvalidOperationException("Read-only fixture");
        }
        [Test] public async Task PendingClaimKeepsExistingRetryAuthorityWhenLifecycleReadFails()
        {
            var wire=new Wire{LifecycleUnavailable=true};var session=SupportInquiryTests.Session();
            var pending=PendingRunOperation.Claim(session.AccountId,RunRewardClaimRequest.Create("run-smoke-1","preview_test","original_claim_key"),new AppBuildSnapshot("1",100),DateTimeOffset.UtcNow);
            using var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new PendingJournal(pending),()=>session,()=>1);
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);account.BindRunRecovery(runs);
            await account.ReadRunResultsAsync("run-smoke-1");Assert.IsNull(account.Lifecycle);Assert.IsTrue(account.CanClaimRunReward("run-smoke-1"));
            var display=new RunVictoryDisplayState(account,"run-smoke-1",new MissionShellMissionRowState[0]);
            Assert.IsTrue(display.CanClaim,"Display must preserve the owner's same-key retry authority.");Assert.That(display.ClaimLabel,Is.EqualTo("지급 결과 재확인"));Assert.That(wire.Writes,Is.Zero);
        }
        [Test] public async Task CompletedRunUsesDedicatedViewWithoutInventingHistoryOrClaimAuthority()
        {
            var wire=new Wire();var session=new GameApiAuthSessionSnapshot("account_test","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            var root=new GameObject("VictoryNavigation",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1600);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);deck.OpenRunLifecycle("run-smoke-1");
                var view=root.GetComponentInChildren<RunVictoryView>();Assert.IsNotNull(view);Assert.That(view.CurrentState.Title,Is.EqualTo("작전 완료"));
                await account.StoryArchive.RefreshAsync();
                Assert.That(view.CurrentState.Notice,Does.Contain("현재 진행"));
                string initialProgress=view.CurrentState.Notice;int progressReads=wire.Reads;
                wire.ProgressOrder=2;await account.StoryArchive.RefreshAsync();
                Assert.AreSame(view,root.GetComponentInChildren<RunVictoryView>());
                Assert.AreNotEqual(initialProgress,view.CurrentState.Notice);
                Assert.That(wire.Reads,Is.EqualTo(progressReads+1),"Owner notification may render but may not read again.");
                Assert.That(view.CurrentState.Rewards.Count,Is.EqualTo(2));                Assert.That(view.GetComponentsInChildren<RawImage>().Count(x=>x.name=="RewardIcon"),Is.EqualTo(2),"Verified currency rewards need independently rendered icons.");
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Where(x=>x.name=="RewardValue").Select(x=>x.text),Is.EqualTo(new[]{"186","425"}),"Quantity is separate from name and icon.");Assert.That(view.CurrentState.HistoryNotice,Does.Contain("확인 필요"));
                var claim=view.GetComponentsInChildren<Button>().Single(x=>x.name=="VictoryClaim");Assert.IsFalse(claim.interactable,"A read-only preview does not invent a durable claim owner.");claim.onClick.Invoke();Assert.That(wire.Writes,Is.Zero);
                view.GetComponentsInChildren<Button>().Single(x=>x.name=="VictoryDetails").onClick.Invoke();int before=wire.Reads;view.GetComponentsInChildren<Button>().Single(x=>x.name=="VictoryRefresh").onClick.Invoke();Assert.That(wire.Reads-before,Is.EqualTo(2),"Existing result read executes once.");
                wire.Empty=true;view.GetComponentsInChildren<Button>().Single(x=>x.name=="VictoryRefresh").onClick.Invoke();
                Assert.That(view.GetComponentsInChildren<TMP_Text>(true).Single(x=>x.name=="VictoryRewardsUnavailable").text,Is.EqualTo("보상 항목이 없습니다."),"Verified empty rewards differ from a missing preview.");
                if(Environment.GetEnvironmentVariable("LASTRAIL_PROGRESS_CAPTURE")=="1")
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{root,"operation-result-current.png"});
                var sections=view.GetComponentsInChildren<Button>(true).SingleOrDefault(x=>x.name=="VictoryOperations");
                Assert.IsNotNull(sections,"The result must provide an explicit route to operation preparation.");
                Assert.IsTrue(sections.interactable);
                account.OriginalRunId=()=>"run-smoke-1";
                int sectionReads=wire.Reads;
                sections.onClick.Invoke();
                Assert.IsNull(root.GetComponentInChildren<RunVictoryView>());
                Assert.That(wire.Reads,Is.EqualTo(sectionReads),"Opening the known operation list must reuse progress.");
                var third=root.GetComponentsInChildren<Button>().Single(x=>x.name=="SelectDeparture2");
                Assert.IsTrue(third.interactable);third.onClick.Invoke();
                Assert.IsNotNull(root.GetComponentInChildren<DeparturePreparationView>());
                Assert.That(wire.Writes,Is.Zero,"Viewing a section must not preview or START.");
                deck.OpenRunLifecycle("run-smoke-1");view=root.GetComponentInChildren<RunVictoryView>();
                int returnReads=wire.Reads;
                view.GetComponentsInChildren<Button>().Single(x=>x.name=="VictoryReturn").onClick.Invoke();Assert.IsNull(root.GetComponentInChildren<RunVictoryView>());Assert.IsFalse(deck.IsDialogOpen);
                Assert.That(wire.Reads,Is.EqualTo(returnReads),"Returning to the lobby is not a data invalidation.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [TestCase("unbind")]
        [TestCase("disabled")]
        [TestCase("authentication")]
        public async Task ResultSectionInputCannotOutliveItsBindingOrValidatedProgress(string boundary)
        {
            var wire=new Wire{ProgressOrder=2};var session=SupportInquiryTests.Session();
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await account.StoryArchive.EnsureAsync();
            var root=new GameObject("ResultSectionLifetime",typeof(RectTransform),typeof(Canvas));
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);
                deck.BindAccount(account);deck.OpenRunLifecycle("run-smoke-1");
                var view=root.GetComponentInChildren<RunVictoryView>();
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="VictoryDetails").onClick.Invoke();
                var button=view.GetComponentsInChildren<Button>().Single(b=>b.name=="VictoryOperations");
                Assert.IsTrue(button.interactable);
                if(boundary=="unbind")view.Unbind();
                else if(boundary=="disabled")deck.enabled=false;
                else{wire.StoryStatus=401;await account.StoryArchive.RefreshAsync();Assert.IsFalse(button.interactable);}
                int reads=wire.Reads;button.onClick.Invoke();
                Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("SelectDeparture")));
                Assert.IsNull(root.GetComponentInChildren<DeparturePreparationView>());
                Assert.That(wire.Reads,Is.EqualTo(reads));Assert.That(wire.Writes,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
