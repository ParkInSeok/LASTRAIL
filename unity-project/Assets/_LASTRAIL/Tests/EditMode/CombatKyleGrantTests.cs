using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CombatKyleGrantTests
{
    static Type GraphicType => typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatKyleGrantGraphic");
    static BasicCombatSimulation Fresh()
    {
        var p = JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Fixtures/ce03-general-active-auto-hM29XX/prepared.json"));
        var c = new Ce03CommandContract((JObject)p["commandContract"]);
        return c.CreateLocalTest(p, (string)p["ownedSelection"]["accountId"], c.Revision, 1);
    }
    static Component[] Cues(GameObject go) => GraphicType == null ? Array.Empty<Component>() : go.GetComponentsInChildren(GraphicType, true);
    static void Result(ApprovedCombatView view, JObject state, JObject row) => typeof(ApprovedCombatView).GetMethod("ResultVisual", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { state, row });
    static JObject Row(string ability, string target, int amount = 25) => new JObject {
        ["id"] = Guid.NewGuid().ToString("N"), ["kind"] = "SHIELD_ISSUE", ["abilityId"] = ability,
        ["sourceId"] = "op_kyle", ["targetId"] = target, ["commandId"] = "one-command",
        ["actualResult"] = new JObject { ["addedAmount"] = amount }
    };
    static ApprovedCombatView Bind(GameObject go, CombatSceneDriver driver, Func<double> clock, CombatPresentationMode mode = CombatPresentationMode.Full)
    {
        var view = go.AddComponent<ApprovedCombatView>();
        view.SetCapturePresentationClock(clock);
        view.ConfigurePresentation(new CombatPresentationPolicy(() => false, () => true) { Mode = mode });
        view.Bind(driver, LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
        return view;
    }

    [Test]
    public void GrantGeometryReachesActualCameraPixels()
    {
        var cameraGo = new GameObject("grant-camera"); var canvasGo = new GameObject("grant-canvas", typeof(RectTransform), typeof(Canvas));
        var target = new RenderTexture(128, 128, 24); var pixels = new Texture2D(128, 128, TextureFormat.RGB24, false); var old = RenderTexture.active;
        try
        {
            var camera = cameraGo.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.targetTexture = target;
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var node = new GameObject("grant", typeof(RectTransform)); node.transform.SetParent(canvasGo.transform, false);
            ((RectTransform)node.transform).sizeDelta = new Vector2(100, 100); var graphic = node.AddComponent<CombatKyleGrantGraphic>(); graphic.SetProgress(.4f);
            Assert.NotNull(node.GetComponent<CanvasRenderer>(), "A mesh without CanvasRenderer is invisible");
            foreach (bool ultimate in new[] { false, true })
            {
                graphic.Ultimate = ultimate; graphic.SetProgress(.4f); Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
                Assert.Greater(pixels.GetPixels32().Count(p => p.b > 40 && p.g > 20), 40, "Both shapes must render visible pixels");
            }
        }
        finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(canvasGo); UnityEngine.Object.DestroyImmediate(cameraGo); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels); }
    }

    [TestCase("op_kyle.active", false)]
    [TestCase("op_kyle.ultimate", true)]
    public void PositiveRowsAnchorActualRecipientsAndExpireWithoutGameplayMutation(string ability, bool ultimate)
    {
        var driver = new CombatSceneDriver(Fresh, () => null, (s, r) => r + 1, () => true); driver.Open();
        var go = new GameObject("kyle-grant-test", typeof(RectTransform), typeof(Canvas));
        try
        {
            double clock = 1; var view = Bind(go, driver, () => clock); var state = driver.HudState;
            var original = driver.StateJson;
            Result(view, state, Row(ability, "op_eve"));
            if (ultimate) Result(view, state, Row(ability, "op_mira"));
            var cues = Cues(go); Assert.AreEqual(ultimate ? 2 : 1, cues.Length, "Every actual recipient needs its own cue");
            foreach (var cue in cues) Assert.IsFalse(((Graphic)cue).raycastTarget);
            Assert.IsTrue(cues.All(c => c.name.Contains("op_eve") || c.name.Contains("op_mira")));
            clock += .1; view.Refresh();
            var cueRect = (RectTransform)cues.Single(c => c.name.EndsWith("_op_eve")).transform;
            var body = go.GetComponentsInChildren<RawImage>(true).Single(x => x.name == "Unit" && x.transform.parent.name == "op_eve").rectTransform;
            var beforePoint = cueRect.TransformPoint(cueRect.rect.center);
            body.transform.parent.position += new Vector3(9, 5, 0);
            typeof(ApprovedCombatView).GetMethod("UpdateKyleGrants", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { state });
            Assert.Less(Vector3.Distance(cueRect.TransformPoint(cueRect.rect.center), beforePoint + new Vector3(9, 5, 0)), .01f, "Decoration follows actual recipient geometry");
            clock += .1; view.Refresh();
            float held = (float)GraphicType.GetProperty("Progress").GetValue(cueRect.GetComponent(GraphicType));
            Assert.Greater(held, 0);
            driver.SetPaused(true);
            for (int i = 0; i < 10; i++) { clock += .1; view.Refresh(); }
            Assert.AreEqual(ultimate ? 2 : 1, Cues(go).Length, "Paused cue must not expire");
            Assert.AreEqual(held, (float)GraphicType.GetProperty("Progress").GetValue(cueRect.GetComponent(GraphicType)));
            driver.SetPaused(false);
            for (int i = 0; i < 7; i++) { clock += .1; view.Refresh(); }
            Assert.AreEqual(0, Cues(go).Length, "Grant cue ends; no inferred persistent shield");
            Assert.AreEqual(original, driver.StateJson);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void ZeroOtherAbilityMissingTargetAndSimplifiedHaveNoKyleDecoration()
    {
        Assert.NotNull(GraphicType, "Kyle grant presentation is not implemented");
        foreach (var mode in new[] { CombatPresentationMode.Full, CombatPresentationMode.Simplified })
        {
            var driver = new CombatSceneDriver(Fresh, () => null, (s, r) => r + 1, () => true); driver.Open();
            var go = new GameObject("kyle-gates", typeof(RectTransform), typeof(Canvas));
            try
            {
                var view = Bind(go, driver, () => 1, mode); var state = driver.HudState;
                Result(view, state, Row("op_kyle.active", "op_eve", 0));
                Result(view, state, Row("op_eve.ultimate", "op_eve"));
                Result(view, state, Row("op_kyle.active", "missing"));
                if (mode == CombatPresentationMode.Simplified) Result(view, state, Row("op_kyle.active", "op_eve"));
                Assert.AreEqual(0, Cues(go).Length);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }

    [Test]
    public void RealGeneralAutoResultsAreNotReplayedByRefreshOrRestoredBinding()
    {
        var driver = new CombatSceneDriver(Fresh, () => null, (s, r) => r + 1, () => true); driver.Open();
        var reference = Fresh(); var go = new GameObject("kyle-real-results", typeof(RectTransform), typeof(Canvas)); GameObject restored = null;
        try
        {
            double clock = 1; var view = Bind(go, driver, () => clock); bool observed = false;
            for (int i = 0; i < 20; i++)
            {
                clock += .05; driver.Advance(500000, 1); reference.Advance(500000, 1); view.Refresh();
                Assert.AreEqual(reference.CaptureLocalState(), driver.StateJson);
                var cues = Cues(go);
                if (cues.Length == 0) continue;
                observed = true; view.Refresh(); Assert.AreEqual(cues.Length, Cues(go).Length);
                restored = new GameObject("restored", typeof(RectTransform), typeof(Canvas));
                Bind(restored, driver, () => clock).Refresh(); Assert.AreEqual(0, Cues(restored).Length); break;
            }
            Assert.IsTrue(observed, "Actual committed Kyle general-auto receipt must reach the cue");
        }
        finally { if (restored != null) UnityEngine.Object.DestroyImmediate(restored); UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void DownTargetAndImminentWarningSuppressDecoration()
    {
        var driver = new CombatSceneDriver(Fresh, () => null, (s, r) => r + 1, () => true); driver.Open();
        var go = new GameObject("kyle-warning", typeof(RectTransform), typeof(Canvas));
        try
        {
            var view = Bind(go, driver, () => 1); var down = (JObject)driver.HudState.DeepClone();
            ((JArray)down["actors"]).Single(a => (string)a["id"] == "op_eve")["hp"] = 0;
            Result(view, down, Row("op_kyle.active", "op_eve")); Assert.AreEqual(0, Cues(go).Length);
            var warning = (JObject)driver.HudState.DeepClone(); var enemy = ((JArray)warning["actors"]).First(a => (bool)a["enemy"]);
            enemy["attacking"] = true; enemy["attackDue"] = (long)warning["tick"] + 1;
            Result(view, warning, Row("op_kyle.active", "op_eve")); Assert.AreEqual(0, Cues(go).Length);
            Result(view, driver.HudState, Row("op_kyle.active", "op_eve")); Assert.AreEqual(1, Cues(go).Length);
            typeof(ApprovedCombatView).GetMethod("UpdateKyleGrants", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { warning });
            Assert.AreEqual(0, Cues(go).Length, "An emerging warning clears an existing decoration");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void DistinctFiniteGeometryOpensAndClosesAtPhaseBounds()
    {
        Assert.NotNull(GraphicType, "Kyle grant geometry is missing");
        var go = new GameObject("geometry", typeof(RectTransform));
        try
        {
            ((RectTransform)go.transform).sizeDelta = new Vector2(160, 170);
            var graphic = go.AddComponent(GraphicType);
            var phase = GraphicType.GetMethod("SetProgress"); var mesh = GraphicType.GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int generalVertices = 0;
            foreach (bool ultimate in new[] { false, true })
            {
                GraphicType.GetField("Ultimate").SetValue(graphic, ultimate);
                using (var vh = new VertexHelper())
                {
                    phase.Invoke(graphic, new object[] { .4f }); mesh.Invoke(graphic, new object[] { vh });
                    Assert.Greater(vh.currentVertCount, 24);
                    if (!ultimate) generalVertices = vh.currentVertCount; else Assert.AreNotEqual(generalVertices, vh.currentVertCount);
                    for (int i = 0; i < vh.currentVertCount; i++) { var vertex = new UIVertex(); vh.PopulateUIVertex(ref vertex, i); Assert.IsFalse(float.IsNaN(vertex.position.x)); Assert.LessOrEqual(Mathf.Abs(vertex.position.x), 81); Assert.LessOrEqual(Mathf.Abs(vertex.position.y), 86); }
                    foreach (float end in new[] { 0f, 1f }) { phase.Invoke(graphic, new object[] { end }); mesh.Invoke(graphic, new object[] { vh }); Assert.AreEqual(0, vh.currentVertCount); }
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
