using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public sealed class OfflineSupplyPresentationTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public int Reads,Writes;public bool Missing;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method!="GET"){Writes++;throw new InvalidOperationException("Read-only supply fixture");}Reads++;
                return Task.FromResult(RunApiHttpResponse.Success(Missing?503:200,Missing?"{}":File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/BasicPreview01/preview.json")));
            }
        }
        [Test] public async Task BasicSupplyPreservesAllFourRewardsAndHidesUnknownAdWithoutGranting()
        {
            var wire=new Wire();var session=SupportInquiryTests.Session();using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            var root=new GameObject("OfflineSupplyTest",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var r=(RectTransform)safe.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                root.GetComponentsInChildren<Button>().Single(x=>x.name=="Shortcut3").onClick.Invoke();var view=root.GetComponentInChildren<OfflineSupplyView>();Assert.IsNotNull(view);
                Assert.That(view.CurrentState.Rewards.Count,Is.EqualTo(4));Assert.That(view.CurrentState.Rewards.Select(x=>x.Quantity),Is.EqualTo(new long[]{400,200,120,80}));Assert.That(view.CurrentState.TimeText,Is.EqualTo("04:00:00"));Assert.That(view.CurrentState.CanClaim,Is.True);Assert.That(wire.Writes,Is.Zero);
                var icons=view.GetComponentsInChildren<RawImage>().Where(x=>x.name=="RewardIcon").ToArray();
                Assert.That(icons.Length,Is.EqualTo(4),"Supply must reuse the existing painted currency artwork");
                Assert.That(view.GetComponentsInChildren<RewardResourceSymbol>().Length,Is.Zero);
                Assert.That(view.GetComponentInChildren<ScrollRect>().content.rect.width,Is.GreaterThan(700),"Four readable cards need real horizontal scroll");
                var compare=SharedRoulettePopup.Create(root.transform,font,"Compare");
                var rows=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/BasicPreview01/preview.json"))["rewardLines"] as Newtonsoft.Json.Linq.JArray;
                compare.Render(null,rows,"","",false,false,false,false,false);
                var matching=compare.GetComponentsInChildren<RawImage>().Where(x=>x.name=="RewardIcon").ToArray();
                for(int i=0;i<4;i++)
                {
                    Assert.That(icons[i].texture,Is.SameAs(matching[i].texture));Assert.That(icons[i].uvRect,Is.EqualTo(matching[i].uvRect));
                    float ratio=icons[i].texture.width*icons[i].uvRect.width/(icons[i].texture.height*icons[i].uvRect.height);
                    Assert.That(icons[i].rectTransform.rect.width/icons[i].rectTransform.rect.height,Is.EqualTo(ratio).Within(.001f));
                    Assert.That(Mathf.Max(icons[i].rectTransform.rect.width,icons[i].rectTransform.rect.height),Is.GreaterThan(100));Assert.That(icons[i].raycastTarget,Is.False);
                }
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="ExperienceChipLabel"),Is.EqualTo(1));
                UnityEngine.Object.DestroyImmediate(compare.gameObject);
                var unknown=new GameObject("UnknownIcon",typeof(RectTransform));unknown.transform.SetParent(root.transform,false);
                Assert.That(RewardIconPresentation.Create(unknown.transform,font,"unrecognized_fixture",new Rect(0,0,190,128)),Is.Null);
                Assert.That(unknown.GetComponentsInChildren<RawImage>().Length,Is.Zero);UnityEngine.Object.DestroyImmediate(unknown);
                wire.Missing=true;await account.RefreshOfflineAsync();Assert.That(view.CurrentState.Rewards.Count,Is.Zero);Assert.IsFalse(view.CurrentState.CanClaim);
                wire.Missing=false;int before=wire.Reads;view.GetComponentsInChildren<Button>().Single(x=>x.name=="RefreshOffline").onClick.Invoke();Assert.That(wire.Reads-before,Is.EqualTo(3));
                var adButton=view.GetComponentsInChildren<Button>(true).Single(x=>x.name=="OpenOfflineRoulette");Assert.That(adButton.gameObject.activeSelf,Is.False);adButton.onClick.Invoke();Assert.IsNotNull(root.GetComponentInChildren<OfflineSupplyView>());Assert.IsNull(root.GetComponentInChildren<SharedRoulettePopup>());Assert.That(wire.Writes,Is.Zero);

                root.GetComponentsInChildren<Button>().Single(x=>x.name=="CloseOfflineSupply").onClick.Invoke();Assert.IsFalse(deck.IsDialogOpen);Assert.That(wire.Writes,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
