using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public sealed class PassPresentationTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public int Syncs,Reads,Claims;public bool Missing,Premium;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.EndpointPath=="/v1/recurring/sync"){Syncs++;return Task.FromResult(RunApiHttpResponse.Success(200,"{\"accepted\":true,\"addedObjectives\":0,\"lastEventId\":0}"));}
                if(request.Method!="GET"){Claims++;throw new InvalidOperationException("No reward claims in presentation fixture");}Reads++;
                return Task.FromResult(RunApiHttpResponse.Success(Missing?503:200,Missing?"{}":File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-PassBulkSamples-20260914/recurring-state-premium-"+(Premium?"owned":"required")+".json")));
            }
        }
        [Test] public async Task FiftyLevelsKeepTrackAuthorityAndFailureHidesStaleRows()
        {
            var wire=new Wire();var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            var root=new GameObject("PassPresentation",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var r=(RectTransform)safe.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPass();var view=root.GetComponentInChildren<SeasonPassView>();Assert.IsNotNull(view);Assert.IsTrue(state.RecurringReady,state.RecurringError);
                Assert.That(view.CurrentState.Rewards.Count,Is.EqualTo(100));Assert.That(view.CurrentState.Rewards.Count(x=>x.CanClaim),Is.EqualTo(1));Assert.That(view.CurrentState.Rewards.Single(x=>x.Level==2&&x.Track=="FREE").Status,Is.EqualTo("EARNED"));Assert.That(view.CurrentState.Rewards.Single(x=>x.Level==2&&x.Track=="PREMIUM").Status,Is.EqualTo("PREMIUM_REQUIRED"));Assert.That(wire.Syncs,Is.EqualTo(1));Assert.That(wire.Claims,Is.Zero);
                var scroll=view.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
                view.GetComponentsInChildren<Button>().Single(x=>x.name=="PassProgressTab").onClick.Invoke();Assert.IsTrue(view.CurrentState.ShowObjectives);Assert.That(wire.Syncs,Is.EqualTo(1));view.GetComponentsInChildren<Button>().Single(x=>x.name=="PassRewardsTab").onClick.Invoke();
                wire.Premium=true;await state.RefreshRecurringAsync();Assert.That(view.CurrentState.Rewards.Count(x=>x.CanClaim),Is.EqualTo(2));
                wire.Missing=true;await state.RefreshRecurringAsync();Assert.That(view.CurrentState.Rewards.Count,Is.Zero);Assert.IsFalse(view.CurrentState.CanBulk);
                view.GetComponentsInChildren<Button>().Single(x=>x.name=="CloseSeasonPass").onClick.Invoke();Assert.IsFalse(deck.IsDialogOpen);Assert.That(wire.Claims,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
