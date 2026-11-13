using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace LastRail.Tests.EditMode
{
    public class ResourcePreparationTests
    {
        sealed class VisibleProgressBackend:IContentStartupBackend
        {
            public Action<long,long> Progress;public readonly TaskCompletionSource<bool> Hold=new TaskCompletionSource<bool>();
            public Task<ClientContentGate> FetchConfigAsync(CancellationToken c)=>Task.FromResult(new ClientContentGate{MinimumVersion="0.1"});
            public Task InitializeAsync(CancellationToken c)=>Task.CompletedTask;
            public Task UpdateCatalogAsync(CancellationToken c)=>Task.CompletedTask;
            public Task<long> RequiredBytesAsync(CancellationToken c)=>Task.FromResult(100L);
            public async Task DownloadAsync(Action<long,long> progress,CancellationToken c){Progress=progress;progress(25,100);using(c.Register(()=>Hold.TrySetCanceled()))await Hold.Task;}
            public Task VerifyRequiredAssetsAsync(CancellationToken c)=>Task.CompletedTask;
        }
        [Test] public async Task ConsentHidesUnderlyingProgressAndRestoresCurrentValues()
        {
            var root=new GameObject("ConsentVisibility",typeof(RectTransform));var access=new DownloadAccessPolicy();access.SetNetwork(ContentNetwork.Wifi);var backend=new VisibleProgressBackend();var coordinator=new DownloadCoordinator(backend,access);coordinator.StartAsync("1.0",CancellationToken.None);
            try{
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var overlay=root.AddComponent<StartupDownloadOverlay>();overlay.Initialize(font,()=>{},()=>{},()=>{});var view=root.GetComponentInChildren<ResourcePreparationView>(true);
                overlay.Show(true,75,false);overlay.RenderPreparation(true,coordinator,access);var percent=view.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Percent");Assert.IsFalse(percent.gameObject.activeInHierarchy,"Consent must hide progress outside its panel on tall screens");Assert.IsTrue(view.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="ResourceTitle"));
                backend.Progress(50,100);overlay.Show(true,50,false);overlay.RenderPreparation(true,coordinator,access);Assert.IsFalse(percent.gameObject.activeInHierarchy);overlay.Show(false,50,false);overlay.RenderPreparation(true,coordinator,access);Assert.IsTrue(percent.gameObject.activeInHierarchy);Assert.AreEqual("50%",percent.text);
            }finally{await coordinator.ResetAdmissionAsync();Object.DestroyImmediate(root);}
        }
        [Test]
        public void ExplicitPauseSurvivesNetworkChangesAndCannotBecomeConsent()
        {
            var access=new DownloadAccessPolicy(); access.SetPlan("test",100); access.SetNetwork(ContentNetwork.Wifi);
            var pause=typeof(DownloadAccessPolicy).GetMethod("SetPaused");
            Assert.That(pause,Is.Not.Null,"Pause must control download access, not only a UI label.");
            pause.Invoke(access,new object[]{true});
            Assert.That(access.Allowed,Is.False); Assert.That(access.NeedsConsent,Is.False);
            access.SetNetwork(ContentNetwork.OtherOnline); access.SetNetwork(ContentNetwork.Wifi);
            Assert.That(access.Allowed,Is.False);
            var wait=access.WaitForAccessAsync(CancellationToken.None); Assert.That(wait.IsCompleted,Is.False);
            pause.Invoke(access,new object[]{false}); Assert.That(wait.IsCompletedSuccessfully,Is.True);
        }

        [TestCase(360,640)] [TestCase(360,480)]
        public void ResourceViewShowsMeasuredProgressAndNeverInventsPreparationPercent(int width,int height)
        {
            var type=typeof(DownloadCoordinator).Assembly.GetType("LastRail.Resources.ResourcePreparationView");
            Assert.That(type,Is.Not.Null,"Existing download mockup must have a runtime view.");
            var root=new GameObject("ResourceViewTest",typeof(RectTransform),typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
            try {
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,font,(Action)(()=>{})});
                var access=new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi);
                type.GetMethod("Render").Invoke(view,new object[]{ContentStage.Downloading,25L*1048576,100L*1048576,access});
                var labels=view.GetComponentsInChildren<TMP_Text>(true);
                Assert.That(labels.Single(t=>t.name=="Percent").text,Is.EqualTo("25%"));
                Assert.That(labels.Single(t=>t.name=="Remaining").text,Does.Contain("75.0"));
                type.GetMethod("Render").Invoke(view,new object[]{ContentStage.Catalog,0L,0L,access});
                Assert.That(labels.Single(t=>t.name=="Percent").text,Is.Empty);
                Assert.That(labels.Single(t=>t.name=="Amount").text,Does.Not.Contain("100%"));
                type.GetMethod("ShowEntryFailure").Invoke(view,null);
                Assert.That(labels.Single(t=>t.name=="Status").text,Does.Contain("연결하지 못했습니다"));
                Assert.That(view.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Any(i=>i.name.Contains("download")),Is.False,"User removed download illustration.");
            } finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void LoginBaselineExcludesCommittedBytesAndNewPlanResetsEvenAtSameSize()
        {
            var type=typeof(DownloadCoordinator).Assembly.GetType("LastRail.Resources.ResourcePreparationView");
            var root=new GameObject("ResourceBaseline",typeof(RectTransform),typeof(Canvas));
            try {
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,font,(Action)(()=>{})});
                var access=new DownloadAccessPolicy(); access.SetNetwork(ContentNetwork.Wifi);
                type.GetMethod("BeginAuthenticatedPlan").Invoke(view,new object[]{100L,60L});
                var render=type.GetMethod("Render");
                render.Invoke(view,new object[]{ContentStage.Downloading,55L,100L,access});
                var percent=view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Percent");
                Assert.That(percent.text,Is.EqualTo("25%"));
                render.Invoke(view,new object[]{ContentStage.Downloading,40L,100L,access});
                Assert.That(percent.text,Is.EqualTo("0%"),"Rolled-back partial bundle bytes must not be double counted.");
                render.Invoke(view,new object[]{ContentStage.Catalog,0L,100L,access});
                render.Invoke(view,new object[]{ContentStage.Downloading,10L,100L,access});
                Assert.That(percent.text,Is.EqualTo("10%"),"New plan may coincidentally have the same byte count.");
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
