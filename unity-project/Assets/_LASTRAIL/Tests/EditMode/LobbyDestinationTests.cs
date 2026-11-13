using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.MissionShell;
namespace LastRail.Tests.EditMode
{
    public sealed class LobbyDestinationTests
    {
        sealed class NoNetwork : LastRail.Run.IRunApiHttpTransport
        {
            public System.Threading.Tasks.Task<LastRail.Run.RunApiHttpResponse> SendAsync(LastRail.Run.RunApiHttpRequest request)
                =>throw new InvalidOperationException("No request expected in view teardown test");
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static WeakReference BindDisposedOwner(LobbyDestinationPages pages)
        {
            var session=SupportInquiryTests.Session();
            var owner=new AccountScreenRuntime(new NoNetwork(),null,()=>session,()=>1);
            pages.BindAccount(owner);owner.Dispose();return new WeakReference(owner);
        }
        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator DestroyedViewDoesNotRetainDisposedAccountThroughManagedWrapper()
        {
            var root=new GameObject("DestroyedViewOwnerTest",typeof(RectTransform));
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});
            pages.Open(LobbyPage.Formation,true);
            var owner=BindDisposedOwner(pages);
            // EditMode does not dispatch this non-ExecuteAlways component's
            // destroy callback; exercise the boundary explicitly here.
            typeof(LobbyDestinationPages).GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(pages,null);
            UnityEngine.Object.DestroyImmediate(root);
            Assert.IsNull(typeof(LobbyDestinationPages).GetField("accountState",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(pages),"Destroy callback must clear direct owner field before testing collection");
            // Collection after a frame boundary avoids retaining dead helper-stack
            // slots in Mono's conservative stack scan during the destroy call.
            yield return null;
            GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            Assert.IsFalse(owner.IsAlive,"A retained destroyed View wrapper must not retain the disposed account owner");
            GC.KeepAlive(pages);
        }
        [Test]
        public void PreviousEntryButtonCannotChangeReopenedFormationSelection()
        {
            var root=new GameObject("StaleViewInputTest",typeof(RectTransform));
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            try{
                var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.Open(LobbyPage.Formation,true);
                var oldInput=pages.GetComponentsInChildren<Button>().Single(b=>b.name=="FormationSlot3").onClick;
                pages.Close();pages.Open(LobbyPage.Formation,true);
                oldInput.Invoke();Assert.AreEqual(0,pages.SelectedSlot,"Old entry input must not affect a new entry");
                pages.GetComponentsInChildren<Button>().Single(b=>b.name=="FormationSlot2").onClick.Invoke();Assert.AreEqual(2,pages.SelectedSlot);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [TestCase(1600,900)] [TestCase(2400,1080)] [TestCase(1280,960)]
        public void AllPagesHaveNativeBackNavigationFitAndUnavailableActionGuards(int width,int height)
        {
            var root=new GameObject("PagesTest",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(width,height);int closed=0;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            try{
                var pages=LobbyDestinationPages.Create(root.transform,font,()=>closed++);
                foreach(LobbyPage page in Enum.GetValues(typeof(LobbyPage)))
                {
                    pages.Open(page,true);Assert.That(pages.IsOpen,Is.True);Assert.That(pages.CurrentPage,Is.EqualTo(page));
                    Assert.That(pages.Controls.localScale.x,Is.EqualTo(Mathf.Min(width/1600f,height/900f)).Within(.001));
                    Assert.That(pages.Controls.rect.size,Is.EqualTo(new Vector2(1600,900)));
                    foreach(var input in pages.GetComponentsInChildren<Button>())
                    {
                        var corners=new Vector3[4];((RectTransform)input.transform).GetWorldCorners(corners);
                        foreach(var corner in corners)
                        {
                            var local=((RectTransform)root.transform).InverseTransformPoint(corner);
                            Assert.That(local.x,Is.InRange(-width*.5f-.1f,width*.5f+.1f),page+" / "+input.name+" must fit horizontally");
                            Assert.That(local.y,Is.InRange(-height*.5f-.1f,height*.5f+.1f),page+" / "+input.name+" must fit vertically");
                        }
                    }
                    Assert.That(pages.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("PageNav")),Is.EqualTo(5));
                    int expected=page==LobbyPage.Formation?1:page==LobbyPage.Missions?-1:(int)page+1;
                    for(int tab=0;tab<5;tab++){
                        var button=pages.GetComponentsInChildren<Button>().Single(b=>b.name=="PageNav"+tab);
                        var state=button.GetComponent<LobbyTabVisual>();Assert.That(state,Is.Not.Null,"Every tab must own a complete visual state");
                        Assert.That(state.IsSelected,Is.EqualTo(tab==expected));
                        var icon=button.GetComponentInChildren<RawImage>();Assert.That(icon.material.shader.name,Is.EqualTo("UI/Default"));Assert.That(icon.texture.name,Is.EqualTo("icons-landscape-v01"));
                        var label=button.GetComponentInChildren<TMP_Text>();Assert.That(label.fontStyle==FontStyles.Bold,Is.EqualTo(tab==expected));
                        Assert.That(button.GetComponent<Image>().sprite.name,Is.EqualTo(tab==expected?"TabSelected":"TabIdle"));
                    }
                    foreach(var b in pages.GetComponentsInChildren<Button>().Where(x=>x.name=="SaveFormation"||x.name.StartsWith("Buy")||x.name=="Upgrade"||x.name=="RecruitOne"||x.name=="ClaimAll"))Assert.That(b.interactable,Is.False,"No fabricated economic acknowledgement");
                    pages.GetComponentsInChildren<Button>().Single(b=>b.name=="Back").onClick.Invoke();Assert.That(pages.IsOpen,Is.False);
                }
                Assert.That(closed,Is.EqualTo(6));pages.Open(LobbyPage.Operators);Assert.That(pages.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("SelectMember")),Is.False);
                pages.GetComponentsInChildren<Button>().Single(b=>b.name=="PageNav2").onClick.Invoke();Assert.That(pages.CurrentPage,Is.EqualTo(LobbyPage.Train));
                pages.Close();
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void CategoryTabsShareMetalStyleAndSwitchSelectionAcrossEveryPage()
        {
            var root=new GameObject("CategoryTest",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            try{
                var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});
                foreach(var page in new[]{LobbyPage.Operators,LobbyPage.Train,LobbyPage.Recruit,LobbyPage.Shop,LobbyPage.Missions}){
                    pages.Open(page,true);
                    int count=pages.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Category"));Assert.That(count,Is.GreaterThan(1));
                    for(int index=0;index<count;index++){
                        pages.GetComponentsInChildren<Button>().Single(b=>b.name=="Category"+index).onClick.Invoke();Assert.That(pages.Category,Is.EqualTo(index));
                        var tabs=pages.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Category")).ToArray();
                        Assert.That(tabs.Count(b=>b.GetComponent<LobbyTabVisual>().IsSelected),Is.EqualTo(1));
                        foreach(var tab in tabs){bool active=tab.name=="Category"+index;Assert.That(tab.GetComponent<Image>().sprite.name,Is.EqualTo(active?"TabSelected":"TabIdle"));Assert.That(tab.GetComponentInChildren<TMP_Text>().fontStyle==FontStyles.Bold,Is.EqualTo(active));}
                    }
                }
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void FormationSelectionAndCategoryRoundTripsAreNativeControls()
        {
            var root=new GameObject("SelectionTest",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            try{var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.Open(LobbyPage.Formation,true);
                pages.GetComponentsInChildren<Button>().Single(b=>b.name=="FormationSlot3").onClick.Invoke();Assert.That(pages.SelectedSlot,Is.EqualTo(3));
                pages.GetComponentsInChildren<Button>().Single(b=>b.name=="AssignMember5").onClick.Invoke();
                Assert.That(pages.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="SelectedSlotInfo").text,Does.Contain("이든"));
                pages.Open(LobbyPage.Missions,true);pages.GetComponentsInChildren<Button>().Single(b=>b.name=="Category1").onClick.Invoke();Assert.That(pages.Category,Is.EqualTo(1));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
