#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using LastRail.Presentation.Settings;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SettingsScreenQa
{
    sealed class Backend : ILocalSettingsBackend
    {
        public int Writes; public bool Fail;
        readonly Dictionary<LocalSetting, float> values = new Dictionary<LocalSetting, float> { { LocalSetting.PresentationMode, 2 }, { LocalSetting.MasterVolume, .7f }, { LocalSetting.Quality, 0 } };
        public TaskCompletionSource<DisplaySettingsInfo> Gate = new TaskCompletionSource<DisplaySettingsInfo>();
        public bool Supports(LocalSetting key) => values.ContainsKey(key);
        public float Read(LocalSetting key) => values[key];
        public void ApplyAndSave(LocalSetting key, float value) { Writes++; if (Fail) throw new IOException("Synthetic save failure"); values[key] = value; }
        public Task<DisplaySettingsInfo> ReadDisplayAsync() => Gate.Task;
    }
    sealed class Wire : IRunApiHttpTransport
    {
        public int Reads, Writes; public bool Ready;
        public TaskCompletionSource<bool> Gate = new TaskCompletionSource<bool>();
        public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
        {
            if (request.Method != "GET") { Writes++; throw new InvalidOperationException("QA forbids account writes"); }
            if (request.EndpointPath != "/v1/account/state") throw new InvalidOperationException("Unexpected read");
            Reads++; bool ready = Ready; await Gate.Task;
            var body = new JObject { ["accepted"] = true, ["accountId"] = "account_test", ["serverTime"] = "2026-09-15T00:00:00Z", ["mutationSequence"] = 0,
                ["currencies"] = new JObject { ["CREDITS"] = 0, ["MACHINE_PARTS"] = 0, ["TACTICAL_DATA"] = 0, ["SPECIAL_CORE"] = 0, ["OPERATOR_EXP"] = 0 },
                ["formation"] = new JObject { ["activationSupported"] = false, ["activePresetId"] = null, ["presets"] = new JArray() } };
            body["fuel"]=null;body["train"]=null;
            foreach (var name in new[] { "items", "operators", "trainingCoreSlots", "carriages", "turrets", "unlocks" }) body[name] = new JArray();
            return RunApiHttpResponse.Success(ready ? 200 : 503, ready ? body.ToString() : "{}");
        }
    }
    public static async void Run()
    {
        try
        {
            var output = Path.GetFullPath(Ce03NormalUiCapture.Arg("-kyleOutput"));
            if (!output.StartsWith("D:\\Project\\LASTRAIL\\unity-project\\QA\\", StringComparison.OrdinalIgnoreCase) || Directory.Exists(output)) throw new InvalidOperationException("Fresh D QA output required");
            Directory.CreateDirectory(output);
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/NoncombatSettings/room-v01.png");
            importer.npotScale = TextureImporterNPOTScale.None; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096; importer.SaveAndReimport();
            var cases = new JArray(); foreach (int height in new[] { 1280, 1600 }) await Capture(output, height, cases);
            File.WriteAllText(Path.Combine(output, "verification.json"), new JObject { ["scope"] = "Native settings menu, four tabs and return. Synthetic local save backend and delayed synthetic account GET. Display option labels read from this Unity project's actual configured names; QA changes do not modify PlayerPrefs, AudioListener or QualitySettings. Unsupported mixers/effects/frame edits/logout remain disabled. Visual match remains open.", ["cases"] = cases }.ToString());
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    static async Task Capture(string output, int height, JArray cases)
    {
        var root = new GameObject("SettingsQa", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); ((RectTransform)root.transform).sizeDelta = new Vector2(720, height);
        var safe = new GameObject("SafeArea", typeof(RectTransform)); safe.transform.SetParent(root.transform, false); var sr = (RectTransform)safe.transform; sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.sizeDelta = Vector2.zero;
        var events = new GameObject("Events", typeof(EventSystem)); var cameraObject = new GameObject("Camera", typeof(Camera)); var target = new RenderTexture(720, height, 24); var pixels = new Texture2D(720, height, TextureFormat.RGB24, false); var previous = RenderTexture.active;
        var backend = new Backend(); using var model = new LocalSettingsModel(backend);
        var wire = new Wire(); var session = new GameApiAuthSessionSnapshot("account_test", "ACTIVE", new GameApiAccessToken("lst1.fixture"), DateTime.UtcNow.AddHours(1), new GameApiRefreshToken("fixture"), DateTime.UtcNow.AddDays(1));
        using var account = new AccountScreenRuntime(wire, null, () => session, () => 0);
        try
        {
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = height / 2f; camera.aspect = 720f / height; camera.transform.position = new Vector3(0, 0, -10); camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"); int links = 0;
            var deck = LobbyCommandDeck.Attach(root, font, "LOCAL QA", 0, new MissionShellMissionRowState[0], null, () => links++); deck.BindSettings(model); deck.BindAccount(account);
            var clicks=new JArray();
            void Click(string name)
            {
                Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();
                var b = root.GetComponentsInChildren<Button>().Single(x => x.name == name);
                var rect=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
                var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Pointer target blocked: "+name+" hits="+string.Join(",",hits.Select(hit=>hit.gameObject.name))+" depth="+b.targetGraphic.depth+" pointer="+pointer.position);
                ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);clicks.Add(name);
            }
            int count = 0; var overflow = new JArray();
            void Record(string name)
            {
                Canvas.ForceUpdateCanvases(); deck.EvaluateLayout();
                foreach (var label in root.GetComponentsInChildren<TMP_Text>()) { label.ForceMeshUpdate(); if (label.isTextOverflowing && label.GetComponentInParent<SettingsScreenView>() != null) overflow.Add(name + ":" + label.name); }
                camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 720, height), 0, 0); pixels.Apply(); File.WriteAllBytes(Path.Combine(output, "settings-" + height + "-" + name + ".png"), pixels.EncodeToPNG()); count++;
            }
            async Task Settled(Func<bool> pending) { for (int i = 0; pending(); i++) { if (i > 500) throw new TimeoutException(); await Task.Delay(1); } await Task.Delay(1); }
            Click("LobbyMenu"); Click("OpenSettings"); Record("game-default");
            var settings=root.GetComponentInChildren<SettingsScreenView>();
            if(settings.GetComponentsInChildren<RectTransform>(true).All(r=>r.name!="AccountFace"))throw new InvalidOperationException("Settings independent account medallion missing");
            void HideRestore(string name,Graphic[] parts){int writes=backend.Writes;foreach(var p in parts)p.enabled=false;Record("hide-"+name);foreach(var p in parts)p.enabled=true;Record("restore-"+name);if(backend.Writes!=writes)throw new InvalidOperationException("Settings presentation wrote state");}
            HideRestore("mode-rings",settings.GetComponentsInChildren<Graphic>().Where(g=>g.name=="ModeRing").ToArray());
            HideRestore("mode-dot",settings.GetComponentsInChildren<Graphic>().Where(g=>g.name=="ModeDot").ToArray());
            void CheckMode(){var dots=settings.GetComponentsInChildren<Graphic>().Where(g=>g.name=="ModeDot").ToArray();if(dots.Length!=1||dots[0].transform.parent.name!="PresentationMode"+(int)model.Value(LocalSetting.PresentationMode))throw new InvalidOperationException("Mode selection display disagrees with model");var writes=backend.Writes;settings.Render(settings.CurrentState);if(backend.Writes!=writes)throw new InvalidOperationException("Mode Render wrote state");}
            CheckMode();Click("PresentationMode0");CheckMode();Record("game-full-saved"); Click("PresentationMode1");CheckMode();Record("game-simple-saved");
            backend.Fail=true;Click("PresentationMode0");CheckMode();if(model.SaveStatus!=SettingsSaveStatus.Failed||model.Value(LocalSetting.PresentationMode)!=1)throw new InvalidOperationException("Mode failed save selected unsaved value");Record("game-save-failed");backend.Fail=false;Click("PresentationMode0");Click("PresentationMode1");
            Click("SettingsTab1"); Record("sound");
            var soundView=root.GetComponentInChildren<SettingsScreenView>();var material=soundView.GetComponentsInChildren<RawImage>().Single(g=>g.name=="SettingsMainMaterial");var top=height-RectTransformUtility.WorldToScreenPoint(camera,material.rectTransform.TransformPoint(material.rectTransform.rect.max)).y;
            if(top<275)throw new InvalidOperationException("Settings panel overlaps tab bounds");
            var baselineWrites=backend.Writes;var knob=root.GetComponentsInChildren<Slider>().Single(x=>x.name=="MasterVolumeSlider").handleRect;var knobSize=knob.rect.size;if(Mathf.Abs(knobSize.x-knobSize.y)>.01f)throw new InvalidOperationException("Sound knob must remain circular");
            foreach(var role in new[]{"TrackBorder","Track","Fill","Handle","VolumeIcon","SavedCheck"}){var graphics=soundView.GetComponentsInChildren<Graphic>().Where(g=>g.name==role).ToArray();foreach(var g in graphics)g.enabled=false;Record("hide-sound-"+role);foreach(var g in graphics)g.enabled=true;Record("restore-sound-"+role);}
            material.enabled=false;Record("hide-main-material");material.enabled=true;Record("restore-main-material");if(backend.Writes!=baselineWrites)throw new InvalidOperationException("Display updates emitted commands");
Click("MasterVolumeMinus"); if(Mathf.Abs(model.Value(LocalSetting.MasterVolume)-.65f)>.001f)throw new InvalidOperationException("Minus command did not apply");Record("sound-saved");if(!soundView.transform.Find("SettingsContent/SavedCheck").gameObject.activeSelf)throw new InvalidOperationException("Saved status missing");
            var volume=knob.GetComponentInParent<Slider>();var pointerDrag=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,knob.TransformPoint(knob.rect.center))};var dragHits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointerDrag,dragHits);if(dragHits.Count==0||dragHits[0].gameObject.GetComponentInParent<Slider>()!=volume)throw new InvalidOperationException("Knob first-hit mismatch");pointerDrag.pointerPressRaycast=dragHits[0];pointerDrag.pointerCurrentRaycast=dragHits[0];var beforeDrag=backend.Writes;ExecuteEvents.Execute(volume.gameObject,pointerDrag,ExecuteEvents.pointerDownHandler);var area=(RectTransform)knob.parent;pointerDrag.position=RectTransformUtility.WorldToScreenPoint(camera,area.TransformPoint(new Vector3(area.rect.xMin+area.rect.width*.4f,area.rect.center.y)));ExecuteEvents.Execute(volume.gameObject,pointerDrag,ExecuteEvents.dragHandler);ExecuteEvents.Execute(volume.gameObject,pointerDrag,ExecuteEvents.pointerUpHandler);if(backend.Writes!=beforeDrag+1||Mathf.Abs(model.Value(LocalSetting.MasterVolume)-.4f)>.005f)throw new InvalidOperationException("Native knob drag duplicated or lost command");Record("sound-drag-saved");
            backend.Fail = true; Click("MasterVolumePlus"); if (model.SaveStatus != SettingsSaveStatus.Failed) throw new InvalidOperationException("Save failure hidden"); Record("sound-save-failed");if(soundView.transform.Find("SettingsContent/SavedCheck").gameObject.activeSelf)throw new InvalidOperationException("Failure displayed success"); backend.Fail = false;
            Click("SettingsTab2"); Record("display-loading");
            backend.Gate.SetException(new IOException("Synthetic display read failure")); await Settled(() => model.DisplayStatus == SettingsReadStatus.Loading); Record("display-unavailable");
            backend.Gate = new TaskCompletionSource<DisplaySettingsInfo>(); Click("DisplayRetry");
            backend.Gate.SetResult(await new UnitySettingsBackend(null).ReadDisplayAsync());
            await Settled(() => model.DisplayStatus == SettingsReadStatus.Loading); Record("display-ready");HideRestore("quality-arrow",settings.GetComponentsInChildren<Graphic>().Where(g=>g.name=="QualityArrow").ToArray());Click("QualityChoice");Record("display-options");Click("QualityChoice");if(settings.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="QualityOptions"))throw new InvalidOperationException("Quality toggle did not close");Click("QualityChoice");Click("QualityOption0");
            Click("SettingsTab3"); Record("account-loading"); wire.Gate.SetResult(true); await Settled(() => account.AccountStatus == AccountStateReadStatus.Loading); Record("account-unavailable");
            foreach(var name in new[]{"AccountFace","AccountRim","AccountPerson"})HideRestore(name,settings.GetComponentsInChildren<Graphic>().Where(g=>g.name==name).ToArray());
            var geometry=new JArray();foreach(var name in new[]{"AccountFace","AccountRim","AccountPerson","QualityArrow","ModeRing","ModeDot"}){var r=settings.GetComponentsInChildren<RectTransform>(true).First(t=>t.name==name);geometry.Add(new JObject{["name"]=name,["x"]=r.anchoredPosition.x,["y"]=r.anchoredPosition.y,["width"]=r.rect.width,["height"]=r.rect.height});}
            Click("SettingsAccountLink"); if (links != 0) throw new InvalidOperationException("Unavailable account linked");
            wire.Ready = true; wire.Gate = new TaskCompletionSource<bool>(); Click("SettingsAccountRetry"); wire.Gate.SetResult(true); await Settled(() => account.AccountStatus == AccountStateReadStatus.Loading);
            if (account.AccountStatus != AccountStateReadStatus.Ready) throw new InvalidOperationException("Synthetic account fixture rejected"); Record("account-ready"); Click("SettingsAccountLink"); if (links != 1) throw new InvalidOperationException("Existing account callback not reused once");
            Click("SettingsTab0"); Record("independence-baseline");
            var room = root.GetComponentsInChildren<RawImage>().Single(x => x.name == "SettingsRoomUpper"); var original = room.texture;
            room.enabled = false; Record("hide-room"); room.enabled = true; room.texture = Texture2D.whiteTexture; Record("swap-room"); room.texture = original; Record("restored");
            var oldView = root.GetComponentInChildren<SettingsScreenView>(); Click("SettingsReturn"); if (root.GetComponentInChildren<SettingsScreenView>() != null) throw new InvalidOperationException("Settings return failed"); Record("menu-return");
            Click("OpenSettings"); Record("reentry"); Click("SettingsBack"); Click("CloseLobbyDialog"); Record("lobby-return");
            if (deck.IsDialogOpen || wire.Writes != 0) throw new InvalidOperationException("Return/write boundary failed");
            cases.Add(new JObject { ["height"] = height,["controlsGeometry"]=geometry,["modeStateAndFailedSaveVerified"]=true,["nativeFirstHitClicks"]=clicks,["mainMaterialTop"]=top,["nativeKnobDrags"]=1,["knobWidth"]=knobSize.x,["knobHeight"]=knobSize.y, ["captures"] = count, ["accountReads"] = wire.Reads, ["accountWrites"] = wire.Writes, ["localFixtureSaveAttempts"] = backend.Writes, ["existingAccountCallbackCount"] = links, ["fourTabs"] = true, ["menuAndLobbyReturn"] = true, ["overflow"] = overflow });
        }
        finally { RenderTexture.active = previous; cameraObject.GetComponent<Camera>().targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(events); UnityEngine.Object.DestroyImmediate(root); }
    }
}
#endif



