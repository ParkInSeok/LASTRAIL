using NUnit.Framework;
using LastRail.Presentation.Startup;
using UnityEngine;
using LastRail.Resources;

namespace LastRail.Tests.EditMode
{
    public class StartupIntroTests
    {
        [TestCase(1600,900)] [TestCase(2100,900)] [TestCase(1200,900)]
        public void OpeningAndFallbackCoverLandscapeWithoutDistortion(int width,int height)
        {
            StartupIntroOverlay.ResetEditorCaptureJourney();
            var root=new GameObject("LandscapeOpeningTest",typeof(RectTransform));
            try{
                var opening=StartupIntroOverlay.Create(root.transform,null,()=>false,()=>ContentStage.Config,()=>{});
                var fallback=StartupMotionBackdrop.Attach(root.transform,false);
                foreach(var image in new[]{opening.transform.Find("FlowVideo").GetComponent<UnityEngine.UI.RawImage>(),fallback.GetComponentInChildren<UnityEngine.UI.RawImage>()}){
                    var parent=(RectTransform)image.transform.parent;parent.sizeDelta=new Vector2(width,height);
                    var fit=image.GetComponent<UnityEngine.UI.AspectRatioFitter>();fit.SetLayoutHorizontal();fit.SetLayoutVertical();
                    var size=image.rectTransform.rect.size;
                    Assert.That(size.x,Is.GreaterThanOrEqualTo(width-.1f));Assert.That(size.y,Is.GreaterThanOrEqualTo(height-.1f));
                    Assert.That(size.x/size.y,Is.EqualTo((float)image.texture.width/image.texture.height).Within(.001f));
                }
            }finally{Object.DestroyImmediate(root);StartupIntroOverlay.ResetEditorCaptureJourney();}
        }
        [Test]
        public void DisableCleanupCompletesConsumedMediaAndKeepsVersionGate()
        {
            StartupIntroOverlay.ResetEditorCaptureJourney();
            var root = new GameObject("IntroLifecycleTest");
            try
            {
                var opening = StartupIntroOverlay.Create(root.transform, null, () => false, () => ContentStage.Config, () => Assert.Fail("Gate closed"));
                Assert.IsFalse(opening.MediaEnded);
                opening.gameObject.SetActive(false);
                // Non-ExecuteAlways MonoBehaviours do not receive lifecycle messages
                // in EditMode; invoke cleanup here, with real reentry covered in PlayMode QA.
                typeof(StartupIntroOverlay).GetMethod("OnDisable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(opening,null);
                opening.gameObject.SetActive(true);
                Assert.IsTrue(opening.MediaEnded);
                Assert.IsFalse(opening.Timeline.Revealed);
                Assert.IsNull(opening.Player);
                var recreated = StartupIntroOverlay.Create(root.transform, null, () => false, () => ContentStage.Config, () => Assert.Fail("Gate closed"));
                Assert.IsTrue(recreated.MediaEnded);
                Assert.AreEqual(0, recreated.PlaybackStarts);
            }
            finally { Object.DestroyImmediate(root); StartupIntroOverlay.ResetEditorCaptureJourney(); }
        }
        [Test]
        public void PreparationAndDecoderStallCannotAdvanceTravelOrRevealLogin()
        {
            var t = new StartupIntroTimeline();
            Assert.IsFalse(t.ObservePlayback(0, false, 30, true));
            Assert.AreEqual(0, t.Travel);
            Assert.IsFalse(t.ObservePlayback(7.25f, false, 30, true));
            Assert.AreEqual(.5f, t.Transition, .001f);
            Assert.IsFalse(t.ObservePlayback(7.25f, false, 30, true));
            Assert.AreEqual(.5f, t.Transition, .001f);
            Assert.IsFalse(t.ObservePlayback(8, false, 0, true), "A timestamp alone is not end-of-media.");
            Assert.IsTrue(t.ObservePlayback(8, true, 0, true));
        }
        [Test]
        public void FailedOrRequiredUpdateGateCannotRevealEvenAtVideoEnd()
        {
            var t = new StartupIntroTimeline();
            Assert.IsFalse(t.ObservePlayback(8, true, 20, false));
            Assert.IsFalse(t.Revealed);
            Assert.IsFalse(t.ObservePlayback(0, true, 20, false));
            Assert.AreEqual(8, t.Travel);
            Assert.IsTrue(t.ObservePlayback(0, true, 0, true));
            Assert.IsFalse(t.ObservePlayback(0, true, 20, true));
            Assert.IsTrue(t.Finished);
        }
        [Test]
        public void FastVersionWaitsForJourneyAndPeakLightThenRevealsExactlyOnce()
        {
            var t = new StartupIntroTimeline();
            Assert.IsFalse(t.Advance(7, true));
            Assert.IsFalse(t.Revealed);
            Assert.IsTrue(t.Advance(1, true));
            Assert.IsTrue(t.Revealed);
            Assert.IsFalse(t.Finished);
            Assert.IsFalse(t.Advance(10, true));
            Assert.IsTrue(t.Finished);
            Assert.IsFalse(t.Advance(100, true));
        }
        [Test]
        public void SlowOrFailedVersionHoldsWithoutReplayingAndRetryCanFinish()
        {
            var t = new StartupIntroTimeline();
            Assert.IsFalse(t.Advance(90, false));
            Assert.AreEqual(8, t.Travel);
            Assert.AreEqual(StartupIntroTimeline.BloomSeconds, t.Transition);
            Assert.IsFalse(t.Revealed);
            Assert.IsFalse(t.Advance(1, false));
            Assert.IsTrue(t.Advance(0, true));
        }
        [Test]
        public void RecreatedBootSkipsConsumedJourneyButStillRequiresVersion()
        {
            var t = new StartupIntroTimeline(true);
            Assert.IsFalse(t.Advance(30, false));
            Assert.IsTrue(t.Advance(0, true));
        }
    }
}
