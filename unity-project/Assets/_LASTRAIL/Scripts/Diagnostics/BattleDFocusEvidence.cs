#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastRail.Diagnostics
{
    internal static class BattleDFocusEvidence
    {
        const string Session = "D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/ActualCapture03/";
        const string Ability = "op_reina.active";

        internal static async Task Run(string output, EventSystem ev, Mouse mouse)
        {
            var identity = JObject.Parse(File.ReadAllText(Session + "session/session.json"));
            var preparation = new Ce03RailPreparation(JObject.Parse(File.ReadAllText(Session + "session/preparation.json")), (string)identity["owner"], (long)identity["supportRevision"]);
            Func<BasicCombatSimulation> factory = () => preparation.CreateModel((uint)identity["seed"]);
            var start = File.ReadAllText(Session + "initial-factory-model.json");
            var reference = JObject.Parse(File.ReadAllText(Session + "high-throttle.state.json"));
            var expected = reference["effectJournal"]["rows"].First(r => (string)r["abilityId"] == Ability && (string)r["kind"] == "DAMAGE");
            var driver = new CombatSceneDriver(factory, () => new RestoredCombatCheckpoint(BasicCombatSimulation.RestoreLocalState(start, factory), 1), (_, revision) => checked(revision + 1), () => true);
            var samples = new JArray();
            var evidence = new JObject {
                ["ability"] = Ability, ["owner"] = "isolated CE03 local checkpoint replay",
                ["referenceCheckpoint"] = "ActualCapture03/high-throttle.state.json", ["expectedReceipt"] = expected.DeepClone(),
                ["webPhaseMs"] = new JArray(1440, 2670), ["webIllustrativeHitMs"] = 2370, ["webLayer"] = "086.webp",
                ["samples"] = samples
            };
            var host = new GameObject("D focus skill owner evidence", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var camera = new GameObject("D focus camera", typeof(Camera)).GetComponent<Camera>();
            try
            {
                driver.Open();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                var view = host.AddComponent<ApprovedCombatView>();
                view.Bind(driver, LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
                view.UseLandscapeLayout();
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                var cues = new JArray();
                view.EffectPresentationCue += (kind, id) => cues.Add(new JObject { ["kind"] = kind, ["id"] = id });
                evidence["presentationAudioCues"] = cues;
                async Task Capture(string stage)
                {
                    await Awaitable.NextFrameAsync();
                    Canvas.ForceUpdateCanvases();
                    await Awaitable.EndOfFrameAsync();
                    var hud = driver.HudState;
                    var image = ScreenCapture.CaptureScreenshotAsTexture();
                    string name = "CE03-focus-" + stage + ".png";
                    try { File.WriteAllBytes(Path.Combine(output, name), image.EncodeToPNG()); }
                    finally { Object.Destroy(image); }
                    samples.Add(new JObject {
                        ["stage"] = stage, ["screenshot"] = name, ["tick"] = hud["tick"].DeepClone(), ["motionSeconds"] = hud["motionSeconds"].DeepClone(),
                        ["cast"] = hud["casts"].FirstOrDefault(c => (string)c["ability"] == Ability)?.DeepClone(),
                        ["target"] = hud["actors"].Single(a => (string)a["id"] == (string)expected["targetId"]).DeepClone(),
                        ["receipt"] = hud["effectResults"].FirstOrDefault(r => (string)r["id"] == (string)expected["id"])?.DeepClone()
                    });
                }
                bool sawCast = false, sawImpact = false;
                for (int tick = 1; tick <= 47; tick++)
                {
                    driver.Advance(500000, 2); // CE03 admits at most half a step of elapsed time per budgeted step.
                    view.Refresh();
                    var hud = driver.HudState;
                    if ((long)hud["tick"] != tick) throw new InvalidOperationException("Focus replay clock diverged at " + tick);
                    var cast = hud["casts"].FirstOrDefault(c => (string)c["ability"] == Ability);
                    if (!sawCast && (string)cast?["status"] == "CASTING")
                    {
                        sawCast = true;
                        await Capture("casting");
                    }
                    if (tick == (long)expected["tick"] - 1) await Capture("before-hit");
                    if (tick == (long)expected["tick"])
                    {
                        var receipt = hud["effectResults"].Single(r => (string)r["id"] == (string)expected["id"]);
                        if (!JToken.DeepEquals(receipt, expected) || (string)cast?["status"] != "APPLIED")
                            throw new InvalidDataException("Reina focus receipt differs from retained CE03 event.");
                        sawImpact = true;
                        await Capture("hit");
                    }
                    if (tick == (long)expected["tick"] + 3) await Capture("after-hit");
                }
                driver.Advance(200000, 1);
                view.Refresh();
                if (!sawCast || !sawImpact || Math.Abs((double)driver.HudState["motionSeconds"] - 2.370) > .000001)
                    throw new InvalidDataException("Incomplete CE03 focus replay.");
                await Capture("2370ms");
                evidence["matchedRetainedReceipt"] = true;
            }
            finally
            {
                File.WriteAllText(Path.Combine(output, "CE03-focus-evidence.json"), evidence.ToString());
                host.SetActive(false); camera.enabled = false;
                host.GetComponent<ApprovedCombatView>()?.ReleaseOwnedPresentationResources();
                Object.Destroy(host); Object.Destroy(camera.gameObject); driver.Retire();
                await Awaitable.NextFrameAsync();
            }
        }

    }
}
#endif
