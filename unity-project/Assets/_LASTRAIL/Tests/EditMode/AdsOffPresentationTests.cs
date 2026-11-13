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
    public sealed class AdsOffPresentationTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public string Policy,Entitlement;
            public int Writes;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method!="GET"){Writes++;throw new InvalidOperationException("No reward writes expected");}
                if(request.EndpointPath=="/v1/runtime-policy"&&Policy!=null)return Task.FromResult(RunApiHttpResponse.Success(200,Policy));
                if(request.EndpointPath=="/v1/ad-skip"&&Entitlement!=null)return Task.FromResult(RunApiHttpResponse.Success(200,Entitlement));
                return Task.FromResult(request.EndpointPath=="/v1/offline-rewards/preview"?RunApiHttpResponse.Success(200,File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/BasicPreview01/preview.json")):RunApiHttpResponse.NetworkError("TEST_ONLY policy unknown"));
            }
        }
        sealed class Provider:IRewardAdPresentation
        {
            public bool Available=>true;
            public int Shows;
            public Task ShowAsync(string user,string data,string unit){Shows++;return Task.CompletedTask;}
        }
        [TestCase(false)][TestCase(true)] public async Task OffPreservesBasicAndOnlyActiveProductEntry(bool owned)
        {
            var session=SupportInquiryTests.Session();var wire=new Wire{Policy=AdRuntimePolicyTests.Policy(false),Entitlement=AdSkipUseTests.Sample(owned?"07-owned-available":"01-unconfigured")};
            using var owner=new AccountScreenRuntime(wire,new AccountActionJournal(new AdSkipUseTests.Storage()),()=>session,()=>1);await owner.RefreshOfflineAsync();await owner.RefreshAdSkipAsync();var provider=new Provider();owner.ConfigureRewardAds(provider);
            var state=new OfflineSupplyDisplayState(owner);Assert.That(state.CanClaim,Is.True);Assert.That(state.ShowAdEntry,Is.EqualTo(owned));
            await owner.BeginAdAsync();await owner.PresentAdAsync();Assert.That(wire.Writes,Is.Zero);Assert.That(provider.Shows,Is.Zero);
        }
        [Test] public void HiddenSalesRetainsHonestOwnershipAndRestoreManagement()
        {
            var root=new GameObject("ShopOff",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AdSkipShopView.Create(root.transform,font);view.Bind(()=>{});view.SetNewSalesAllowed(false);view.Render(AdSkipSnapshot.Parse(AdSkipUseTests.Sample("07-owned-available")),false,"");
                Assert.That(view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="AdSkipPurchase").gameObject.activeSelf,Is.False);
                Assert.That(view.GetComponentsInChildren<Button>().Single(b=>b.name=="AdSkipRestore").interactable,Is.False,"No Store adapter is installed");
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("이용권 보유")),Is.True);
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("1회 구매")),Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [TestCase(false)][TestCase(true)] public async Task OffOwnedStillAuthorizesOriginalProductSpinWithoutSdk(bool combat)
        {
            var wire=new AdSkipUseTests.Wire{Override=q=>q.EndpointPath=="/v1/runtime-policy"?Task.FromResult(RunApiHttpResponse.Success(200,AdRuntimePolicyTests.Policy(false))):AdSkipUseTests.Wire.Reply(q)};
            var session=AdSkipUseTests.Session();var provider=new Provider();using var owner=new AccountScreenRuntime(wire,new AccountActionJournal(new AdSkipUseTests.Storage()),()=>session,()=>1);owner.ConfigureRewardAds(provider);await owner.RefreshAdSkipAsync();Assert.That(owner.RuntimeAds.Status,Is.EqualTo(AdRuntimePolicyStatus.Disabled));
            await AdSkipUseTests.Use(owner,combat);Assert.That(combat?owner.CombatAds.Intent.Status:owner.AdIntent.Status,Is.EqualTo("CLAIMED"));Assert.That(provider.Shows,Is.Zero);
            Assert.That(wire.Calls.Count(q=>q.EndpointPath.EndsWith("/uses/authorize")),Is.EqualTo(1));Assert.That(wire.Calls.Count(q=>q.EndpointPath.EndsWith("/spin")),Is.EqualTo(1));Assert.That(wire.Calls.Any(q=>q.EndpointPath.EndsWith("/start")),Is.False);
        }
        [Test] public async Task UnknownPolicyHidesNewAdvertisingButKeepsBasicSupply()
        {
            var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(new Wire(),null,()=>session,()=>1);await owner.RefreshOfflineAsync();
            var root=new GameObject("AdsUnknown",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=OfflineSupplyView.Create(root.transform,font);int claims=0,ads=0;view.Bind(()=>{},()=>{},()=>claims++,()=>ads++);view.Render(new OfflineSupplyDisplayState(owner));
                Assert.That(view.CurrentState.CanClaim,Is.True);
                var ad=view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="OpenOfflineRoulette");Assert.That(ad.gameObject.activeSelf,Is.False,"UNKNOWN must remove the new advertising hit target");ad.onClick.Invoke();Assert.That(ads,Is.Zero);
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="ClaimOffline").onClick.Invoke();Assert.That(claims,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void UnknownResultHasNoNewRouletteEntry()
        {
            var root=new GameObject("UnknownResult",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=RunVictoryView.Create(root.transform,font,null);int ads=0;view.Bind(()=>{},()=>{},()=>{},()=>{});view.BindAd(()=>ads++);view.Render(new RunVictoryDisplayState(null,"run",Array.Empty<MissionShellMissionRowState>()));
                var ad=view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="VictoryAd");Assert.That(ad.gameObject.activeSelf,Is.False);ad.onClick.Invoke();Assert.That(ads,Is.Zero);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
