using System.Linq;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class StartupLongBodyTests
    {
        GameObject root;
        StartupScreenRootBehaviour view;
        public static string LongText => string.Join(" ", Enumerable.Repeat("긴 안내문도 순서대로 보존하고 연결 상태를 확인한 뒤 다시 시도해 주세요.", 16));
        [SetUp] public void Setup()
        {
            root=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
            root.GetComponent<CanvasScaler>().enabled=false;root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);view=root.GetComponent<StartupScreenRootBehaviour>();Canvas.ForceUpdateCanvases();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);}
        ScrollRect Scroll => root.GetComponentsInChildren<ScrollRect>(true).Single(s=>s.name=="PopupBodyScroll");
        void Render(StartupScreenViewState state){view.Render(state);Canvas.ForceUpdateCanvases();}
        [TestCase(false)][TestCase(true)] public void LongBodyRetainsReadableFontAndEntireText(bool single)
        {
            var state=single?StartupScreenViewState.DownloadError(LongText):StartupScreenViewState.UpdateNotice("",LongText);Render(state);
            var lead=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="StartupDetails");
            Assert.That(lead.fontSize,Is.EqualTo(single?30:32));Assert.That(Scroll.gameObject.activeInHierarchy,Is.True);
            Assert.That(Scroll.content.rect.height,Is.GreaterThan(Scroll.viewport.rect.height));
            var tail=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="PopupFollowup");
            Assert.That(single?lead.text:lead.text+"\n\n"+tail.text,Is.EqualTo(state.Details));
            Assert.That(Scroll.verticalNormalizedPosition,Is.EqualTo(1).Within(.001));
        }
        [Test] public void SameBodyRetainsReadingPositionAndNewBodyStartsAtTop()
        {
            var state=StartupScreenViewState.UpdateNotice("",LongText);Render(state);Scroll.verticalNormalizedPosition=.35f;Render(state);
            Assert.That(Scroll.verticalNormalizedPosition,Is.EqualTo(.35f).Within(.001));
            Render(StartupScreenViewState.UpdateNotice("",LongText+" 새 안내입니다."));Assert.That(Scroll.verticalNormalizedPosition,Is.EqualTo(1).Within(.001));
        }
        [Test] public void NormalAndNonPopupRestoreBorrowedTextWithoutActiveMask()
        {
            Render(StartupScreenViewState.UpdateNotice("",LongText));Render(StartupScreenViewState.UpdateNotice("","다시 접속해 주세요."));
            Assert.That(Scroll.gameObject.activeSelf,Is.False);
            var lead=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="StartupDetails");Assert.That(lead.transform.parent.name,Is.EqualTo("StartupActionBand"));
            Render(StartupScreenViewState.Waiting());Assert.That(Scroll.gameObject.activeSelf,Is.False);Assert.That(lead.GetComponentInParent<RectMask2D>(),Is.Null);
        }
    }
}
