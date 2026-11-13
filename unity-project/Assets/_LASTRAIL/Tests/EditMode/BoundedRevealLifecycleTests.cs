using System;
using System.Collections;
using System.Linq;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastRail.Tests.EditMode
{
    public sealed class BoundedRevealLifecycleTests
    {
        static TMP_FontAsset Font => UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
        static JObject Receipt(string id) => new JObject { ["rewardClaimId"] = id, ["multiplier"] = 4,
            ["rewardLines"] = JArray.Parse("[{\"id\":\"cur_credits\",\"quantity\":3120}]") };

        [UnityTest] public IEnumerator RouletteCompletionEnablesConfirmationOnceAndNeverReplaysReceipt()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("BoundedRoulette", typeof(RectTransform), typeof(Canvas));
            try
            {
                var popup = SharedRoulettePopup.Create(root.transform, Font, "QA");
                popup.Bind(null, null, null, null);
                var visual = root.GetComponentInChildren<SharedRouletteVisual>();
                var completed = typeof(SharedRouletteVisual).GetEvent("RevealCompleted");
                Assert.That(completed, Is.Not.Null, "Completion must be observable without polling the view");
                int calls = 0;
                completed.AddEventHandler(visual, (Action)(() => calls++));
                popup.Render(null, null, "", "Spin", true, false, false, false, false);
                var receipt = Receipt("one");
                popup.Render(receipt, null, "", "Confirm", true, false, true, false, false);
                var button = root.GetComponentsInChildren<Button>().Single(b => b.name == "QAMain");
                Assert.That(button.interactable, Is.False);
                yield return new WaitForSecondsRealtime(7.3f);
                Assert.That(visual.IsAnimating, Is.False);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(button.interactable, Is.True);
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "RewardQuantity").text, Is.EqualTo("3,120"));
                popup.Render(receipt, null, "", "Confirm", true, false, true, false, false);
                yield return null;
                Assert.That(visual.IsAnimating, Is.False);
                Assert.That(calls, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator RouletteCancelledAttemptCannotCompleteAndCompletionMayReenter()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("CancelledRoulette", typeof(RectTransform), typeof(Canvas));
            try
            {
                var visual = SharedRouletteVisual.Create(root.transform, Font);
                var completed = typeof(SharedRouletteVisual).GetEvent("RevealCompleted");
                Assert.That(completed, Is.Not.Null);
                int calls = 0;
                completed.AddEventHandler(visual, (Action)(() => { calls++; visual.CancelAndClear(); visual.Present(null, null); visual.Present(Receipt("next"), null); }));
                visual.Present(null, null); visual.Present(Receipt("cancelled"), null);
                visual.CancelAndClear();
                yield return new WaitForSecondsRealtime(7.3f);
                Assert.That(calls, Is.Zero);
                visual.Present(null, null); visual.Present(Receipt("finish"), null);
                yield return new WaitForSecondsRealtime(7.3f);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(visual.IsAnimating, Is.True, "Completion callback owns the replacement attempt");
                visual.enabled = false;
                yield return null;
                Assert.That(visual.IsAnimating, Is.False);
                visual.enabled = true;
                Assert.That(calls, Is.EqualTo(1), "Disable cancels; it is not a completion");
            }
            finally { Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator LoginSheenStopsWritingAfterItsFinalValueAndResetsOnEnable()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("BoundedTitle", typeof(RectTransform));
            int previousCaptureRate = Time.captureFramerate;
            try
            {
                Time.captureFramerate = 1;
                var label = root.AddComponent<TextMeshProUGUI>(); label.font = Font;
                var presentation = root.AddComponent<LoginTitlePresentation>();
                var material = label.fontSharedMaterial;
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(material.GetFloat("_TitleSheen"), Is.EqualTo(1.3f).Within(.001f));
                material.SetFloat("_TitleSheen", .42f);
                yield return null; yield return null;
                Assert.That(material.GetFloat("_TitleSheen"), Is.EqualTo(.42f).Within(.001f), "Finished effect must release its frame work");
                presentation.enabled = false; presentation.enabled = true;
                Assert.That(material.GetFloat("_TitleSheen"), Is.EqualTo(-1));
                presentation.enabled = false;
                yield return null;
                Assert.That(material.GetFloat("_TitleSheen"), Is.EqualTo(-1), "Disable must cancel the restarted effect");
            }
            finally { Time.captureFramerate = previousCaptureRate; Object.DestroyImmediate(root); }
            yield return new ExitPlayMode();
        }
    }
}
