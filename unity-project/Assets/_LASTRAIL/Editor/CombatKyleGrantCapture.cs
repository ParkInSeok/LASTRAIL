#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CombatKyleGrantCapture
{
    static BasicCombatSimulation Base()
    {
        var p = JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-general-active-auto-hM29XX/prepared.json"));
        var c = new Ce03CommandContract((JObject)p["commandContract"]);
        return c.CreateLocalTest(p, (string)p["ownedSelection"]["accountId"], c.Revision, 1);
    }
    static BasicCombatSimulation Setup(string scenario)
    {
        var raw = JObject.Parse(Base().CaptureLocalState());
        foreach (var actor in (JArray)raw["actors"])
        {
            actor["ready"] = 100000;
            string id = (string)actor["id"];
            if (scenario == "G-other" && id == "op_eve") actor["vitals"]["hp"] = 400;
            if (scenario == "U-one" && id.StartsWith("op_") && id != "op_kyle")
            { actor["vitals"]["hp"] = 0; actor["downCount"] = 1; actor["recoverAt"] = 100000; }
        }
        string ability = scenario == "G-other" ? "op_kyle.active" : "op_kyle.ultimate";
        foreach (var ready in (JArray)raw["skillReady"])
            if (((string)ready["key"]).StartsWith("op_", StringComparison.Ordinal))
                ready["ready"] = ((string)ready["key"]).EndsWith("/" + ability, StringComparison.Ordinal) ? 0 : 100000;
        if (scenario != "G-other") raw["operatorAutomation"]["charge"]["op_kyle"] = 100;
        return BasicCombatSimulation.RestoreLocalState(raw.ToString(), Base);
    }

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-kyleOutput");
        if (!Application.isBatchMode || index < 0 || index + 1 >= args.Length) throw new ArgumentException("Explicit batch output required");
        string output = Path.GetFullPath(args[index + 1]);
        if (!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\Unity1Artifacts\\Reports\\KylePair-20260913\\", StringComparison.OrdinalIgnoreCase)
            || Directory.Exists(output)) throw new IOException("Fresh D Kyle capture output required");
        Directory.CreateDirectory(output);
        var completed = new JArray();
        try
        {
            foreach (string scenario in new[] { "G-other", "U-one", "U-multi" }) completed.Add(Capture(output, scenario));
            File.WriteAllText(Path.Combine(output, "complete.json"), new JObject { ["scenarios"] = completed, ["scope"] = "LOCAL diagnostic HP/readiness/charge, actual .003 model receipts and BattleSceneRoot, no backend or device input" }.ToString());
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "failure.txt"), e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static JObject Capture(string output, string scenario)
    {
        var folder = Path.Combine(output, scenario); Directory.CreateDirectory(folder);
        var driver = new CombatSceneDriver(() => Setup(scenario), () => null, (s, r) => r + 1, () => true); driver.Open();
        var reference = Setup(scenario); File.WriteAllText(Path.Combine(folder, "initial.json"), driver.StateJson);
        var cameraGo = new GameObject("KyleQaCamera"); var camera = cameraGo.AddComponent<Camera>();
        var target = new RenderTexture(720, 1280, 24); var pixels = new Texture2D(720, 1280, TextureFormat.RGB24, false);
        var previous = RenderTexture.active; camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var go = new GameObject("KyleSceneQa", typeof(RectTransform), typeof(Canvas));
        System.Diagnostics.Process encoder = null;
        try
        {
            go.AddComponent<BattleSceneRoot>().BindCombat(driver, go);
            var view = go.GetComponent<ApprovedCombatView>(); double clock = 1; view.SetCapturePresentationClock(() => clock);
            view.ConfigurePresentation(new CombatPresentationPolicy(() => false, () => true) { Mode = CombatPresentationMode.Full });
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var info = new System.Diagnostics.ProcessStartInfo {
                FileName = "C:/Users/dlstj/AppData/Roaming/Python/Python39/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe",
                Arguments = "-v error -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart \"" + Path.Combine(folder, "video.mp4") + "\"",
                WorkingDirectory = "D:/Project/LASTRAIL/unity-project", UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true };
            encoder = System.Diagnostics.Process.Start(info);
            var thresholds = new[] { 0d, .10, .22, .38, .55, .60 }; var samples = new JArray(); int sample = 0; double start = -1; byte[] priorPng = null; int receiptCount = 0;
            using (var trace = new StreamWriter(Path.Combine(folder, "frames.jsonl")))
            for (int frame = 0; frame < 75; frame++)
            {
                clock = 1 + frame / 30d;
                if (frame == 65 || frame == 70)
                {
                    go.GetComponentsInChildren<Button>().Single(b => b.name == "UltimateAuto").onClick.Invoke();
                    reference.SetUltimateAuto(frame == 65);
                }
                if (frame == 6 && scenario != "G-other")
                {
                    driver.RequestSkill("kyle-capture", "op_kyle", "op_kyle.ultimate"); reference.RequestSkill("kyle-capture", "op_kyle", "op_kyle.ultimate");
                }
                long delta = (long)Math.Floor((frame + 1) * 10000000d / 30) - (long)Math.Floor(frame * 10000000d / 30);
                driver.Advance(delta, 1); reference.Advance(delta, 1); view.Refresh();
                if (driver.StateJson != reference.CaptureLocalState()) throw new Exception("Reference state mismatch " + scenario + " frame " + frame);
                var cues = go.GetComponentsInChildren<CombatKyleGrantGraphic>();
                var receipts = new JArray(((JArray)driver.HudState["effectResults"]).Where(r => (string)r["kind"] == "SHIELD_ISSUE" && ((string)r["abilityId"]).StartsWith("op_kyle.") && (int)r["actualResult"]["addedAmount"] > 0));
                if (start < 0 && cues.Length > 0)
                {
                    start = clock; receiptCount = receipts.Count;
                    if (priorPng != null) File.WriteAllBytes(Path.Combine(folder, "pre.png"), priorPng);
                }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); pixels.Apply();
                encoder.StandardInput.BaseStream.Write(pixels.GetRawTextureData<byte>().ToArray(), 0, 720 * 1280 * 3);
                var png = pixels.EncodeToPNG();
                if (frame == 65 || frame == 70)
                {
                    string autoState = frame == 65 ? "on" : "off";
                    File.WriteAllBytes(Path.Combine(folder, "auto-" + autoState + ".png"), png);
                    var button = go.GetComponentsInChildren<Button>().Single(b => b.name == "UltimateAuto");
                    var thumb = (RectTransform)button.transform.Find("AutoThumb");
                    var label = button.GetComponentInChildren<TMPro.TMP_Text>().rectTransform;
                    File.WriteAllText(Path.Combine(folder, "auto-" + autoState + ".json"), new JObject {
                        ["frame"] = frame, ["on"] = (bool)driver.HudState["ultimateAuto"],
                        ["thumbPosition"] = new JArray(thumb.anchoredPosition.x, thumb.anchoredPosition.y),
                        ["thumbSize"] = new JArray(thumb.rect.width, thumb.rect.height),
                        ["labelPosition"] = new JArray(label.anchoredPosition.x, label.anchoredPosition.y),
                        ["supportControls"] = driver.HudState["manuals"].DeepClone(),
                        ["input"] = "native Button.onClick; not physical mouse/device" }.ToString());
                }
                if (start >= 0 && sample < thresholds.Length && clock - start + .00001 >= thresholds[sample])
                {
                    string name = "phase-" + sample + ".png"; File.WriteAllBytes(Path.Combine(folder, name), png);
                    samples.Add(new JObject { ["file"] = name, ["frame"] = frame, ["elapsed"] = clock - start, ["requestedElapsed"] = thresholds[sample], ["cueCount"] = cues.Length }); sample++;
                }
                trace.WriteLine(new JObject { ["frame"] = frame, ["clock"] = clock, ["referenceExact"] = true, ["receipts"] = receipts,
                    ["cues"] = new JArray(cues.Select(c => new JObject { ["name"] = c.name, ["progress"] = c.Progress, ["width"] = c.rectTransform.rect.width, ["height"] = c.rectTransform.rect.height })) }.ToString(Newtonsoft.Json.Formatting.None));
                priorPng = png;
            }
            encoder.StandardInput.Close(); encoder.WaitForExit(); if (encoder.ExitCode != 0) throw new Exception("Encoder failed");
            int expected = scenario == "U-multi" ? 5 : 1;
            if (sample != thresholds.Length || receiptCount != expected || go.GetComponentsInChildren<CombatKyleGrantGraphic>().Length != 0) throw new Exception("Missing receipt/phase/end coverage: " + scenario + " " + receiptCount + "/" + sample);
            var finalReceipts = new JArray(((JArray)driver.HudState["effectResults"]).Where(r => (string)r["kind"] == "SHIELD_ISSUE"));
            if (scenario == "G-other" && (string)finalReceipts[0]["targetId"] != "op_eve") throw new Exception("General must demonstrate non-caster recipient");
            File.WriteAllText(Path.Combine(folder, "final.json"), driver.StateJson);
            var result = new JObject { ["scenario"] = scenario, ["frames"] = 75, ["referenceExactFrames"] = 75, ["positiveShieldReceipts"] = receiptCount, ["samples"] = samples };
            File.WriteAllText(Path.Combine(folder, "capture.json"), result.ToString()); return result;
        }
        finally
        {
            if (encoder != null) { try { encoder.StandardInput.Close(); } catch { } if (!encoder.HasExited) encoder.WaitForExit(5000); encoder.Dispose(); }
            RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(cameraGo); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
#endif
