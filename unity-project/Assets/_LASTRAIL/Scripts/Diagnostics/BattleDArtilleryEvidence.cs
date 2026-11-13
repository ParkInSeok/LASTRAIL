#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
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
    internal static class BattleDArtilleryEvidence
    {
        const string Source = "D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/SecondFinal-20260913/ActualCapture03/";
        const string Origin = "artillery-live.state.json";
        const string Historical = "artillery-visible.state.json";

        static JObject Geometry(RectTransform rect, Camera camera)
        {
            if (rect == null) return null;
            var b = BattleDPlayerCheck.Bounds(rect, camera);
            var graphic = rect.GetComponent<Graphic>();
            return new JObject { ["name"] = rect.name, ["active"] = rect.gameObject.activeInHierarchy,
                ["texture"] = (graphic as RawImage)?.texture?.name, ["graphic"] = graphic?.GetType().Name,
                ["bounds"] = new JArray(b.x, b.y, b.width, b.height), ["onScreen"] = b.Overlaps(new Rect(0,0,Screen.width,Screen.height)) };
        }

        internal static async Task Run(string output, EventSystem ev, Mouse mouse)
        {
            string folder = Path.Combine(output, "ce03-artillery");
            Directory.CreateDirectory(folder);
            var frames = new JArray();
            var result = new JObject { ["scope"] = "Separate retained CE03 local encounter, original CE03 consist and seed; no reference-D event or enemy train added",
                ["source"] = Source + Origin, ["historical"] = Source + Historical, ["frames"] = frames };
            GameObject host = null;
            Camera cam = null;
            CombatSceneDriver driver = null;
            try
            {
                string origin = File.ReadAllText(Source + Origin);
                string historicalText = File.ReadAllText(Source + Historical);
                var historical = JObject.Parse(historicalText);
                var log = (JArray)historical["railJourney"]["artillery"]["events"];
                var launch = log.First(e => (string)e["kind"] == "LAUNCH");
                var impact = log.First(e => (string)e["kind"] == "IMPACT" && (string)e["id"] == (string)launch["id"] &&
                    (long)e["tick"] > (long)launch["tick"]);
                string shooter = (string)launch["id"];
                long fired = (long)launch["tick"], landed = (long)impact["tick"];
                var originalHit = ((JArray)historical["events"]).Single(e =>
                    (string)e["Source"] == shooter && (long)e["Tick"] == landed);
                result["sourceSha256"] = Ce03CommandContract.Sha(origin);
                result["historicalSha256"] = Ce03CommandContract.Sha(historicalText);
                result["retainedHit"] = originalHit.DeepClone();
                var identity = JObject.Parse(File.ReadAllText(Source + "session/session.json"));
                var prep = new Ce03RailPreparation(JObject.Parse(File.ReadAllText(Source + "session/preparation.json")),
                    (string)identity["owner"], (long)identity["supportRevision"]);
                result["preparationSha256"] = prep.PreparationSha256;
                result["seed"] = identity["seed"].DeepClone();
                Func<BasicCombatSimulation> factory = () => prep.CreateModel((uint)identity["seed"]);
                driver = new CombatSceneDriver(factory, () => new RestoredCombatCheckpoint(
                    BasicCombatSimulation.RestoreLocalState(origin, factory), 1), (m,r) => r + 1, () => true);
                driver.Open();
                var first = driver.HudState;
                long current = (long)first["tick"], step = (long)first["stepTimeSpanTicks"];
                result["carriages"] = first["railJourney"]["carriages"].DeepClone();
                result["roster"] = new JArray(((JArray)first["ultimates"]).Select(u => (string)u["actor"]));
                result["framebuffer"] = new JArray(Screen.width, Screen.height);
                result["artilleryArt"] = new JArray("CombatSecondFinal/artillery_emplacement-v01", "CombatSecondFinal/artillery_ledge-v01");
                result["shellRenderer"] = "Original IncomingShell_<sequence> procedurally colored UI graphic";
                host = new GameObject("OriginalCE03ArtilleryReplay", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                cam = new GameObject("OriginalCE03ArtilleryCamera", typeof(Camera)).GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                var view = host.AddComponent<ApprovedCombatView>();
                view.Bind(driver, LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
                view.UseLandscapeLayout();
                var canvas = host.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1;
                for (int i=0; i<6; i++) await Awaitable.NextFrameAsync();
                long sequence = -1;
                foreach (long tick in new[] { current, fired-1, fired, fired+3, fired+6, landed-1, landed })
                {
                    if (tick > current) driver.Advance((tick-current)*step, 100);
                    current = tick;
                    await Awaitable.NextFrameAsync();
                    await Awaitable.EndOfFrameAsync();
                    var hud = driver.HudState;
                    if ((long)hud["tick"] != tick) throw new InvalidOperationException("CE03 battle did not reach original tick " + tick);
                    var art = hud["railArtillery"];
                    var actor = ((JArray)hud["actors"]).Single(a => (string)a["id"] == shooter);
                    var train = ((JArray)hud["actors"]).Single(a => (bool)a["train"]);
                    var shot = ((JArray)art["projectiles"]).SingleOrDefault(p => (string)p["source"] == shooter && (long)p["launchedTick"] == fired);
                    if (shot != null)
                    {
                        long id = (long)shot["sequence"];
                        if (sequence > 0 && id != sequence) throw new InvalidOperationException("CE03 projectile identity changed.");
                        sequence = id;
                    }
                    var gun = BattleDPlayerCheck.Find(view.transform, shooter);
                    var shell = BattleDPlayerCheck.Find(view.transform, "IncomingShell_" + sequence);
                    string file = "tick-" + tick + ".png";
                    var row = new JObject { ["tick"] = tick, ["playerFrame"] = Time.frameCount, ["file"] = file,
                        ["shooter"] = shooter, ["shooterVisual"] = actor["visual"], ["gunHp"] = actor["hp"], ["trainTarget"] = train["id"],
                        ["trainHp"] = train["hp"], ["trainShield"] = train["shield"], ["projectile"] = shot?.DeepClone(),
                        ["gunGraphic"] = Geometry(gun == null ? null : BattleDPlayerCheck.Find(gun, "Unit"), cam),
                        ["ledgeGraphic"] = Geometry(gun == null ? null : BattleDPlayerCheck.Find(gun, "RockLedge"), cam),
                        ["shellGraphic"] = Geometry(shell, cam) };
                    frames.Add(row);
                    var image = ScreenCapture.CaptureScreenshotAsTexture();
                    try { File.WriteAllBytes(Path.Combine(folder, file), image.EncodeToPNG()); }
                    finally { UnityEngine.Object.Destroy(image); }
                }
                var end = JObject.Parse(driver.StateJson);
                var replayLog = (JArray)end["railJourney"]["artillery"]["events"];
                var replayHit = ((JArray)end["events"]).SingleOrDefault(e =>
                    (string)e["Source"] == shooter && (long)e["Tick"] == landed && (string)e["Target"] == (string)originalHit["Target"]);
                result["replayHit"] = replayHit?.DeepClone();
                result["projectileSequence"] = sequence;
                result["replayArtilleryEvents"] = replayLog.DeepClone();
                result["matchedLaunchImpactAndDamage"] = sequence > 0 && replayLog.Any(e => JToken.DeepEquals(e, launch)) &&
                    replayLog.Any(e => JToken.DeepEquals(e, impact)) && JToken.DeepEquals(replayHit, originalHit);
                if (!(bool)result["matchedLaunchImpactAndDamage"]) throw new InvalidOperationException("Retained CE03 launch, impact or damage did not match.");
            }
            catch (Exception error) { result["error"] = error.ToString(); throw; }
            finally
            {
                if (host != null) { host.SetActive(false); UnityEngine.Object.Destroy(host); }
                if (cam != null) { cam.enabled = false; UnityEngine.Object.Destroy(cam.gameObject); }
                driver?.Retire();
                File.WriteAllText(Path.Combine(folder, "actual-artillery.json"), result.ToString());
            }
        }
    }
}
#endif
