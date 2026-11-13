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

public static class CommonPopupMaterialQa
{
    static bool comparisonOnly;
    public static void Compare(){comparisonOnly=true;Run();}
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
            var initialState=network ? StartupScreenViewState.NetworkInterrupted("네트워크 상태를 확인한 뒤 다시 시도해 주세요.") : denial ? StartupScreenViewState.InternalAccessDenied() : StartupScreenViewState.UpdateNotice("HIDDEN_VERSION_SENTINEL", "업데이트 후 다시 접속해 주세요.");view.Render(initialState);
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(1080, 1080f * height / 720); rect.position = Vector3.zero; rect.localScale = Vector3.one;
            var safe = root.transform.Find("SafeArea").GetComponent<RectTransform>(); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = new Vector2(24, 36); safe.offsetMax = new Vector2(-24, -36);
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) { canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; }
            Canvas.ForceUpdateCanvases();
            // Resolve the initial popup against the same configured safe area as later state renders.
            view.Render(initialState);Canvas.ForceUpdateCanvases();
            if(!root.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.name=="PopupFollowup"))throw new InvalidOperationException("Popup separate follow-up paragraph missing");
            var paragraphGeometry=new JArray();
            void VerifyBody(StartupScreenViewState state)
            {
                var lead=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="StartupDetails");var tail=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="PopupFollowup");var line=root.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="BodySeparator");var boundary=System.Text.RegularExpressions.Regex.Match(state.Details,@"\r?\n\r?\n");
                if((boundary.Success?lead.text+boundary.Value+tail.text:lead.text)!=state.Details)throw new InvalidOperationException("Popup body copy lost or duplicated");
                lead.ForceMeshUpdate(true);tail.ForceMeshUpdate(true);float bottom=-lead.rectTransform.anchoredPosition.y-lead.textBounds.min.y;float gap1=0,gap2=0;
                if(tail.gameObject.activeSelf){float y=-line.anchoredPosition.y;gap1=y-bottom;gap2=-tail.rectTransform.anchoredPosition.y-tail.textBounds.max.y-y-2;bottom=-tail.rectTransform.anchoredPosition.y-tail.textBounds.min.y;if(gap1<17.99f||gap2<17.99f)throw new InvalidOperationException("Popup paragraph glyph clearance");}
                else if(line.gameObject.activeSelf)throw new InvalidOperationException("Single paragraph retained separator");
                float buttonTop=-root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="StartupPrimary").GetComponent<RectTransform>().anchoredPosition.y;if(buttonTop-bottom<23.99f)throw new InvalidOperationException("Popup body touches button");
                if(lead.isTextTruncated||lead.isTextOverflowing||(tail.gameObject.activeSelf&&(tail.isTextTruncated||tail.isTextOverflowing)))throw new InvalidOperationException("Popup body truncated");
                paragraphGeometry.Add(new JObject{["phase"]=state.PhaseKey,["source"]=state.Details,["lead"]=lead.text,["followup"]=tail.text,["fontSize"]=lead.fontSize,["leadToSeparator"]=gap1,["separatorToFollowup"]=gap2,["bodyToButton"]=buttonTop-bottom});
            }
            VerifyBody(initialState);
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
            Save("unbound-disabled");
            var primary = root.GetComponentsInChildren<Button>().Single(b => b.name == "StartupPrimary");
            var secondary = root.GetComponentsInChildren<Button>().Single(b => b.name == "StartupSecondary");
            var calls = 0;
            view.PrimaryActionRequested += () => calls++;
            view.SecondaryActionRequested += () => throw new InvalidOperationException("Unexpected secondary action");
            if (!primary.interactable || !secondary.interactable) throw new InvalidOperationException("Bound popup controls must be active before material comparison.");
            Save("composed");
            foreach(var name in new[]{"StartupDetails","PopupFollowup","BodySeparator","SeparatorGlow"}){var item=root.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name==name);item.gameObject.SetActive(false);Save("hide-"+name);item.gameObject.SetActive(true);Save("restore-"+name);}
            foreach(var state in new[]{StartupScreenViewState.DownloadError("단일 문단을 빠짐없이 표시합니다."),StartupScreenViewState.UpdateNotice("",string.Join(" ",Enumerable.Repeat("긴 안내문도 순서대로 보존하고 연결 상태를 확인한 뒤 다시 시도해 주세요.",6)))}){view.Render(state);Canvas.ForceUpdateCanvases();VerifyBody(state);Save(state.PhaseKey=="download_error"?"single-paragraph":"long-followup");}
            view.Render(initialState);Canvas.ForceUpdateCanvases();VerifyBody(initialState);Save("body-restored");
            if(comparisonOnly){var primarySkin=surfaces.Single(s=>s.transform.parent.name=="StartupPrimary");primarySkin.EmphasizePrimary(1.75f);Save("middle-intensity");primarySkin.EmphasizePrimary(2.7f);Save("legacy-intensity");primarySkin.EmphasizePrimary(1.25f);Save("intensity-restored");cases.Add(new JObject{["kind"]=kind,["height"]=height,["comparisonOnly"]=true,["primaryInteractable"]=primary.interactable,["secondaryInteractable"]=secondary.interactable,["currentIntensity"]=1.25f,["middleIntensity"]=1.75f,["legacyIntensity"]=2.7f,["inputActions"]=0});return;}
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
            var primaryRect = (RectTransform)primary.transform;
            var pointer = new PointerEventData(events.GetComponent<EventSystem>()) { position = RectTransformUtility.WorldToScreenPoint(camera, primaryRect.TransformPoint(primaryRect.rect.center)) };
            var hits = new System.Collections.Generic.List<RaycastResult>(); root.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != primary) throw new InvalidOperationException("Native update button raycast failed: " + string.Join(",", hits.Select(h => h.gameObject.name)));
            ExecuteEvents.Execute(primary.gameObject, pointer, ExecuteEvents.pointerClickHandler);

            if (calls != 1) throw new InvalidOperationException("Duplicate or missing popup action dispatch.");
            view.Render(StartupScreenViewState.Waiting()); view.Render(StartupScreenViewState.UpdateNotice("", "업데이트 후 다시 접속해 주세요."));
            if (root.GetComponentsInChildren<IndependentMetalSurface>(true).Length != 3) throw new InvalidOperationException("Duplicate surface after state reentry.");
            cases.Add(new JObject { ["kind"] = kind, ["height"] = height,["paragraphGeometry"]=paragraphGeometry, ["surfaceCount"] = surfaces.Length, ["nativeCallbackCalls"] = calls, ["decorativeRaycastTargets"] = surfaces.Sum(s => s.GetComponentsInChildren<Graphic>(true).Count(g => g.raycastTarget)), ["stateReentryNoDuplicates"] = true, ["versionNumberRemoved"] = true, ["separatorTextOverlap"] = false });
        }
        finally
        {
            RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(events);
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
#endif


