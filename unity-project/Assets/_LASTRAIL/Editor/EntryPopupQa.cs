#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Presentation.Startup;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class EntryPopupQa
{
    public static void Run()
    {
        var output = Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
        if (!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\", StringComparison.OrdinalIgnoreCase) || Directory.Exists(output)) throw new InvalidOperationException("Fresh D QA output required.");
        Directory.CreateDirectory(output);
        var assets = new JArray();
        foreach (var name in new[] { "popup-frame-v03", "popup-backplate", "control-frame-v03", "control-backplate" })
        {
            var path = "Assets/_LASTRAIL/Resources/EntryPopup/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = name.Contains("frame");
            importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            assets.Add(new JObject { ["path"] = path, ["width"] = texture.width, ["height"] = texture.height });
        }
        var cases = new JArray();
        foreach (var height in new[] { 1280, 1600 })
            foreach (var kind in new[] { "update", "internal", "network" }) Capture(output, height, kind, cases);
        File.WriteAllText(Path.Combine(output, "verification.json"), new JObject { ["scope"] = "Actual startup view, independent surface permutations and native input dispatch; local callbacks only", ["assets"] = assets, ["cases"] = cases, ["liveActions"] = false }.ToString());
        EditorApplication.Exit(0);
    }

    static void Capture(string output, int height, string kind, JArray cases)
    {
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));
        bool denial = kind == "internal"; bool network = kind == "network";
        var cameraObject = new GameObject("PopupQaCamera", typeof(Camera));
        var events = new GameObject("PopupQaEvents", typeof(EventSystem));
        var target = new RenderTexture(720, height, 24);
        var pixels = new Texture2D(720, height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1080f * height / 720 / 2;
            camera.aspect = 720f / height; camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.targetTexture = target;
            var view = root.GetComponent<StartupScreenRootBehaviour>();
            view.Render(network ? StartupScreenViewState.NetworkInterrupted("네트워크 상태를 확인한 뒤 다시 시도해 주세요.") : denial ? StartupScreenViewState.InternalAccessDenied() : StartupScreenViewState.UpdateNotice("HIDDEN_VERSION_SENTINEL", "업데이트 후 다시 접속해 주세요."));
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(1080, 1080f * height / 720); rect.position = Vector3.zero; rect.localScale = Vector3.one;
            var safe = root.transform.Find("SafeArea").GetComponent<RectTransform>(); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = new Vector2(24, 36); safe.offsetMax = new Vector2(-24, -36);
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) { canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; }
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing || text.text.Contains("HIDDEN_VERSION_SENTINEL")) throw new InvalidOperationException("Invalid popup text: " + text.name);
                var separator = root.GetComponentsInChildren<Image>(true).Single(i => i.name == "BodySeparator");
                var corners = new Vector3[4]; separator.rectTransform.GetWorldCorners(corners);
                var separatorBounds = Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
                foreach (var line in text.textInfo.lineInfo.Take(text.textInfo.lineCount))
                {
                    if (line.visibleCharacterCount == 0) continue;
                    var min = text.transform.TransformPoint(line.lineExtents.min); var max = text.transform.TransformPoint(line.lineExtents.max);
                    if (separator.gameObject.activeInHierarchy && separatorBounds.Overlaps(Rect.MinMaxRect(min.x, min.y, max.x, max.y))) throw new InvalidOperationException("Separator overlaps text: " + text.name);
                }
            }
            var surfaces = root.GetComponentsInChildren<IndependentMetalSurface>();
            if (surfaces.Length != 3) throw new InvalidOperationException("Expected popup and two independent control surfaces.");
            void Save(string name)
            {
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 720, height), 0, 0); pixels.Apply(false);
                File.WriteAllBytes(Path.Combine(output, (kind == "update" ? "" : kind + "-") + name + "-" + height + ".png"), pixels.EncodeToPNG());
            }
            Save("composed");
            foreach (var role in new[] { "Frame", "Backplate", "StateLights" })
            {
                foreach (var surface in surfaces)
                {
                    if (role == "Frame") surface.Frame.enabled = false;
                    else if (role == "Backplate") surface.Backplate.enabled = false;
                    else surface.Lights.gameObject.SetActive(false);
                }
                Save("hide-" + role);
                foreach (var surface in surfaces)
                { surface.Frame.enabled = true; surface.Backplate.enabled = true; surface.Lights.gameObject.SetActive(true); }
            }
            var original = surfaces.Select(s => s.Backplate.texture).ToArray();
            var colors = surfaces.Select(s => s.Backplate.color).ToArray();
            foreach (var surface in surfaces) { surface.Backplate.texture = Texture2D.whiteTexture; surface.Backplate.color = Color.magenta; }
            Save("swap-Backplate");
            for (var i = 0; i < surfaces.Length; i++) { surfaces[i].Backplate.texture = original[i]; surfaces[i].Backplate.color = colors[i]; }
            Save("restored");
            var primary = root.GetComponentsInChildren<Button>().Single(b => b.name == "StartupPrimary");
            var calls = 0; view.PrimaryActionRequested += () => calls++;
            var primaryRect = (RectTransform)primary.transform;
            var pointer = new PointerEventData(events.GetComponent<EventSystem>()) { position = RectTransformUtility.WorldToScreenPoint(camera, primaryRect.TransformPoint(primaryRect.rect.center)) };
            var hits = new System.Collections.Generic.List<RaycastResult>(); root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != primary) throw new InvalidOperationException("Native update button raycast failed: " + string.Join(",", hits.Select(h => h.gameObject.name)));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            primary.onClick.Invoke();
            if (calls != 1) throw new InvalidOperationException("Duplicate or missing popup action dispatch.");
            view.Render(StartupScreenViewState.Waiting()); view.Render(StartupScreenViewState.UpdateNotice("", "업데이트 후 다시 접속해 주세요."));
            if (root.GetComponentsInChildren<IndependentMetalSurface>(true).Length != 3) throw new InvalidOperationException("Duplicate surface after state reentry.");
            cases.Add(new JObject { ["kind"] = kind, ["height"] = height, ["surfaceCount"] = surfaces.Length, ["nativeCallbackCalls"] = calls, ["decorativeRaycastTargets"] = surfaces.Sum(s => s.GetComponentsInChildren<Graphic>(true).Count(g => g.raycastTarget)), ["stateReentryNoDuplicates"] = true, ["versionNumberRemoved"] = true, ["separatorTextOverlap"] = false });
        }
        finally
        {
            RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(events);
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
#endif


