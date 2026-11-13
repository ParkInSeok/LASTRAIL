#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastRail.Diagnostics
{
    internal static class BattleDReinaFrames
    {
        const string Source = "D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/ActualCapture03/";
        const string Actor = "op_reina";
        const long Step = 500000L; // Retained CE03 clock step, validated against the original HUD below.

        static JToken Ultimate(JObject hud) =>
            ((JArray)hud["ultimates"]).Single(r => (string)r["actor"] == Actor);

        static async Task<string> BeforeReady(Func<BasicCombatSimulation> factory, JObject report)
        {
            string source = File.ReadAllText(Source + "ultimate-ready.state.json");
            var model = BasicCombatSimulation.RestoreLocalState(source, factory);
            if ((long)model.ReadHudState()["stepTimeSpanTicks"] != Step) throw new InvalidOperationException("CE03 source clock step changed.");
            int steps = 0;
            while ((int)Ultimate(model.ReadHudState())["charge"] < 100)
            {
                if (steps >= 900 || model.Outcome != BasicCombatOutcome.Running)
                    throw new InvalidOperationException("Reina did not reach full charge from the retained CE03 checkpoint.");
                model.Advance(Step, 100);
                if (++steps % 24 == 0) await Awaitable.NextFrameAsync();
            }
            if (steps == 0) throw new InvalidOperationException("Missing pre-charge source.");
            report["source"] = Source + "ultimate-ready.state.json";
            report["sourceSha256"] = Ce03CommandContract.Sha(source);
            report["firstFullTick"] = model.Tick;
            for (int i = 0; !(bool)Ultimate(model.ReadHudState())["enabled"]; i++)
            {
                if (i >= 900 || model.Outcome != BasicCombatOutcome.Running)
                    throw new InvalidOperationException("CE03 did not enable the full-charge Reina command.");
                model.Advance(Step, 100);
                if (i % 24 == 0) await Awaitable.NextFrameAsync();
            }
            report["firstEnabledTick"] = model.Tick;
            model = BasicCombatSimulation.RestoreLocalState(source, factory);
            for (int n = 1; n < steps; n++)
            {
                model.Advance(Step, 100);
                if (n % 24 == 0) await Awaitable.NextFrameAsync();
            }
            report["preChargeTick"] = model.Tick;
            report["stepsBeforeFinalAdvance"] = steps - 1;
            return model.CaptureLocalState();
        }

        internal static async Task Run(string output, EventSystem ev, Mouse mouse)
        {
            string folder = Path.Combine(output, "reina-frames");
            Directory.CreateDirectory(folder);
            var frames = new JArray();
            var clicks = new JArray();
            var report = new JObject { ["frames"] = frames, ["pointerInputs"] = clicks,
                ["clock"] = "Time.unscaledTimeAsDouble, unchanged Player clock",
                ["pixels"] = "consecutive end-of-frame RGB24 Player framebuffer crops; bottom-left source coordinates" };
            var identity = JObject.Parse(File.ReadAllText(Source + "session/session.json"));
            var prep = new Ce03RailPreparation(JObject.Parse(File.ReadAllText(Source + "session/preparation.json")),
                (string)identity["owner"], (long)identity["supportRevision"]);
            Func<BasicCombatSimulation> factory = () => prep.CreateModel((uint)identity["seed"]);
            var textures = new List<Texture2D>();
            CombatSceneDriver driver = null;
            GameObject host = null;
            Camera cam = null;
            try
            {
                string prior = await BeforeReady(factory, report);
                driver = new CombatSceneDriver(factory, () => new RestoredCombatCheckpoint(
                    BasicCombatSimulation.RestoreLocalState(prior, factory), 1), (m, r) => r + 1, () => true);
                driver.Open();
                host = new GameObject("CE03ReinaActualFrames", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                cam = new GameObject("CE03ReinaCamera", typeof(Camera)).GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                var view = host.AddComponent<ApprovedCombatView>();
                view.Bind(driver, LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
                view.UseLandscapeLayout();
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1;
                for (int i = 0; i < 12; i++) await Awaitable.NextFrameAsync();

                var card = BattleDPlayerCheck.Find(view.transform, "OperatorCard_" + Actor);
                var wash = BattleDPlayerCheck.Find(card, "ready-wash").GetComponent<RawImage>();
                var flash = BattleDPlayerCheck.Find(card, "ready-flash").GetComponent<RawImage>();
                Rect bounds = BattleDPlayerCheck.Bounds(card, cam);
                int left = Mathf.Max(0, Mathf.FloorToInt(bounds.xMin) - 12);
                int bottom = Mathf.Max(0, Mathf.FloorToInt(bounds.yMin) - 12);
                int width = Mathf.Min(Screen.width, Mathf.CeilToInt(bounds.xMax) + 12) - left;
                int height = Mathf.Min(Screen.height, Mathf.CeilToInt(bounds.yMax) + 12) - bottom;
                var roi = new Rect(left, bottom, width, height);
                report["framebuffer"] = new JArray(Screen.width, Screen.height);
                report["crop"] = new JArray(left, bottom, width, height);

                async Task Capture(string stage)
                {
                    await Awaitable.NextFrameAsync();
                    await Awaitable.EndOfFrameAsync();
                    var hud = driver.HudState;
                    var skill = Ultimate(hud);
                    var row = new JObject { ["stage"] = stage, ["frame"] = Time.frameCount,
                        ["time"] = Time.unscaledTimeAsDouble, ["tick"] = (long)hud["tick"],
                        ["charge"] = (int)skill["charge"], ["enabled"] = (bool)skill["enabled"], ["status"] = (string)skill["status"],
                        ["wash"] = wash.color.a, ["flash"] = flash.color.a,
                        ["file"] = $"frame-{frames.Count:D3}.png" };
                    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    textures.Add(tex);
                    var previous = RenderTexture.active;
                    try { RenderTexture.active = null; tex.ReadPixels(roi, 0, 0, false); }
                    finally { RenderTexture.active = previous; }
                    frames.Add(row);
                }

                await Capture("before");
                await Capture("before");
                driver.Advance(Step, 100);
                await Capture("first-full-frame");
                double edge = Time.unscaledTimeAsDouble;
                if ((int)frames.Last["charge"] != 100 || (long)frames.Last["tick"] != (long)report["firstFullTick"])
                    throw new InvalidOperationException("Restored CE03 model did not reproduce the first full-charge tick.");
                long firstEnabled = (long)report["firstEnabledTick"];
                long edgeTick = (long)frames.Last["tick"];
                bool observedEnabled = (bool)frames.Last["enabled"];
                // Batch costly serialized model commits while collecting a separate image on every render frame.
                // All owed ticks use the elapsed real clock, with a boundary at the first eligible tick.
                const double commitInterval = .25;
                report["realTimeCommitIntervalSeconds"] = commitInterval;
                double lastCommit = edge;
                do
                {
                    await Capture("full-charge");
                    double now = Time.unscaledTimeAsDouble;
                    long currentTick = (long)frames.Last["tick"];
                    bool enabled = (bool)frames.Last["enabled"];
                    if (enabled && !observedEnabled && currentTick != firstEnabled)
                        throw new InvalidOperationException("First Player command eligibility differed from the single-step CE03 replay.");
                    observedEnabled |= enabled;
                    if (enabled && now - edge >= .55) break;
                    if (driver.Outcome != BasicCombatOutcome.Running || now - edge > 90 || frames.Count >= 900)
                        throw new InvalidOperationException("No usable Reina command observed in the recorded window.");
                    if (now - lastCommit < commitInterval) continue;
                    long dueTick = edgeTick + (long)Math.Floor((now - edge) * TimeSpan.TicksPerSecond / Step);
                    if (!observedEnabled) dueTick = Math.Min(dueTick, firstEnabled);
                    long due = Math.Min(100L, dueTick - currentTick);
                    if (due > 0) driver.Advance(due * Step, 100);
                    lastCommit = now;
                } while (true);
                var click = BattleDPlayerCheck.Click(clicks, mouse, ev, view, cam, driver, "Ultimate_" + Actor);
                do { await Capture("pointer"); } while (!click.IsCompleted);
                await click;
                // Separate a queued pointer command from the first simulation tick that consumes charge.
                for(int step=0;step<100 && (int)Ultimate(driver.HudState)["charge"]==100;step++)
                {
                    long priorTick=(long)driver.HudState["tick"];
                    driver.Advance(Step,1);
                    await Capture("after-input-step");
                    if((long)frames.Last["tick"]-priorTick>1)
                        throw new InvalidOperationException("Consumption sampling skipped a model tick.");
                }
                if((int)Ultimate(driver.HudState)["charge"]==100)
                    throw new InvalidOperationException("No observed charge consumption after the real pointer command.");
                await Capture("after-consumption");
                foreach (JObject frame in frames) frame["secondsFromFullCharge"] = (double)frame["time"] - edge;
                report["positiveFlashFrames"] = frames.Count(frame => (double)frame["flash"] > .001);
                report["positiveFlashIntervals"] = frames.Where((frame, i) => (double)frame["flash"] > .001 &&
                    (i == 0 || (double)frames[i - 1]["flash"] <= .001)).Count();
                report["consecutiveFrameNumbers"] = frames.Skip(1).Select((frame, i) =>
                    (int)frame["frame"] == (int)frames[i]["frame"] + 1).All(v => v);
                report["consumed"] = (int)frames.Last["charge"] < 100 && (double)frames.Last["wash"] == 0 &&
                    (double)frames.Last["flash"] == 0 && (bool?)clicks.Last?["targetReached"] == true;
            }
            catch (Exception e) { report["error"] = e.ToString(); throw; }
            finally
            {
                try
                {
                    for (int n = 0; n < frames.Count; n++)
                        File.WriteAllBytes(Path.Combine(folder, (string)frames[n]["file"]), textures[n].EncodeToPNG());
                    File.WriteAllText(Path.Combine(folder, "actual-frames.json"), report.ToString());
                }
                finally
                {
                    foreach (var texture in textures) UnityEngine.Object.Destroy(texture);
                    if (host != null) { host.SetActive(false); UnityEngine.Object.Destroy(host); }
                    if (cam != null) { cam.enabled = false; UnityEngine.Object.Destroy(cam.gameObject); }
                    driver?.Retire();
                }
            }
        }
    }
}
#endif
