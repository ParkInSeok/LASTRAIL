using System;
using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class AdSkipPresentationTests
    {
        sealed class ShopWire:IRunApiHttpTransport
        {
            public int Reads,Writes;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method!="GET"){Writes++;throw new InvalidOperationException("Read-only storeless shop");}
                if(request.EndpointPath!="/v1/ad-skip")return Task.FromResult(RunApiHttpResponse.NetworkError("Other fixtures unavailable"));
                Reads++;return Task.FromResult(RunApiHttpResponse.Success(200,"{\"accepted\":true,\"contractVersion\":\"ad-skip-v1\",\"owned\":false,\"entitlement\":null,\"catalog\":null,\"purchaseAvailable\":false,\"featureAvailability\":{\"OFFLINE\":\"UNAVAILABLE\",\"COMBAT\":\"UNAVAILABLE\",\"REROLL\":\"UNSUPPORTED\"},\"serverTime\":1000}"));
            }
        }
        [Test] public void ActualShopCategoryReadsAccountAndCannotPurchase()
        {
            var root=new GameObject("ActualAdSkipShop",typeof(RectTransform),typeof(Canvas));
            var wire=new ShopWire();var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
            using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var page=LobbyDestinationPages.Create(root.transform,font,()=>{});page.BindAccount(account);page.Open(LobbyPage.Shop);
                page.GetComponentsInChildren<Button>().Single(b=>b.name=="Category1").onClick.Invoke();
                var view=page.GetComponentInChildren<AdSkipShopView>();Assert.That(view,Is.Not.Null,"Convenience category must render actual account state");
                Assert.That(wire.Reads,Is.EqualTo(1));Assert.That(wire.Writes,Is.Zero);
                Assert.That(view.GetComponentsInChildren<Button>().Single(b=>b.name=="AdSkipPurchase").interactable,Is.False);
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("보유한 이용권이 없습니다")),Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void StorelessCardIsHonestAndOwnsOnlyItsRefreshBinding()
        {
            var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.AdSkipShopView");
            Assert.That(type,Is.Not.Null,"Account-backed convenience product view is missing");
            var root=new GameObject("AdSkipShopQA",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,font});
                int refreshes=0;Action refresh=()=>refreshes++;
                type.GetMethod("Bind").Invoke(view,new object[]{refresh});type.GetMethod("Bind").Invoke(view,new object[]{refresh});
                var render=type.GetMethod("Render");render.Invoke(view,new object[]{null,false,"이용권 확인 필요"});
                Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                Assert.That(Find("AdSkipPurchase").interactable,Is.False);Assert.That(Find("AdSkipRestore").interactable,Is.False);
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("14,900")),Is.False);
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("리롤")&&t.text.Contains("미지원")),Is.True);
                Find("AdSkipRefresh").onClick.Invoke();Assert.That(refreshes,Is.EqualTo(1));
                type.GetMethod("Unbind").Invoke(view,null);Find("AdSkipRefresh").onClick.Invoke();Assert.That(refreshes,Is.EqualTo(1));
                // TEST_ONLY display observation, never a purchase grant or authority flag.
                var owned=JObject.Parse("{\"accepted\":true,\"contractVersion\":\"ad-skip-v1\",\"owned\":true,\"entitlement\":{\"status\":\"ACTIVE\",\"revision\":1,\"permanent\":true},\"catalog\":null,\"purchaseAvailable\":false,\"featureAvailability\":{\"OFFLINE\":\"AVAILABLE\",\"COMBAT\":\"AVAILABLE\",\"REROLL\":\"UNSUPPORTED\"},\"serverTime\":1000}");
                render.Invoke(view,new object[]{AdSkipSnapshot.Parse(owned.ToString()),false,""});
                Assert.That(Find("AdSkipPurchase").GetComponentInChildren<TMP_Text>().text,Is.EqualTo("보유 중"));Assert.That(Find("AdSkipPurchase").interactable,Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}

