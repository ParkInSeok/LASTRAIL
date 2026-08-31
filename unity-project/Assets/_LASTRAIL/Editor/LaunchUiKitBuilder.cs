#if UNITY_EDITOR
using System;
using System.IO;
using LastRail.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchUiKitBuilder
    {
        private const string UiRoot = "Assets/_LASTRAIL/UI";
        private const string CommonRoot = UiRoot + "/Common";
        private const string IconRoot = UiRoot + "/Icons";
        private const string StyleRoot = UiRoot + "/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = UiRoot + "/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = UiRoot + "/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";

        private static readonly Color Ink = C(8, 12, 17, 255);
        private static readonly Color Panel = C(21, 28, 34, 255);
        private static readonly Color PanelAlt = C(33, 39, 42, 255);
        private static readonly Color RailBlue = C(45, 137, 180, 255);
        private static readonly Color Copper = C(204, 125, 60, 255);
        private static readonly Color Signal = C(100, 186, 138, 255);
        private static readonly Color Warning = C(224, 180, 80, 255);
        private static readonly Color Danger = C(190, 77, 72, 255);
        private static readonly Color Disabled = C(76, 82, 86, 255);
        private static readonly Color TextMain = C(236, 239, 235, 255);
        private static readonly Color TextMuted = C(174, 184, 184, 255);

        private static readonly string[] IconNames =
        {
            "back", "close", "settings", "retry", "info", "warning", "ticket", "reward", "codex", "operator", "mission", "gacha", "sound", "vibration", "lock", "unlock"
        };

        [MenuItem("LASTRAIL/Launch/Build Common UI Kit")]
        public static void BuildCommonUiKit()
        {
            Ensure(CommonRoot);
            Ensure(IconRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);
            Ensure(CommonRoot + "/Buttons");
            Ensure(CommonRoot + "/Sliders");
            Ensure(CommonRoot + "/InputFields");
            Ensure(CommonRoot + "/Rows");
            Ensure(CommonRoot + "/Showcase");

            EnsureTmpEssentialResources();
            WriteJson();
            NormalizeFontMaterial();
            CreateIcons();
            CreatePrefabs();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL common UI kit build complete.");
        }

        private static void EnsureTmpEssentialResources()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                return;
            }

            TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
            AssetDatabase.Refresh();
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                return;
            }

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
            var packagePath = packageInfo == null ? null : packageInfo.resolvedPath;
            var essentialsPackagePath = string.IsNullOrEmpty(packagePath)
                ? null
                : Path.Combine(packagePath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (string.IsNullOrEmpty(essentialsPackagePath) || !File.Exists(essentialsPackagePath))
            {
                throw new InvalidOperationException("Unable to locate TMP Essential Resources package for launch UI kit.");
            }

            var tempPackagePath = Path.Combine(Path.GetTempPath(), "LASTRAIL_TMP_Essential_Resources.unitypackage");
            File.Copy(essentialsPackagePath, tempPackagePath, overwrite: true);
            AssetDatabase.ImportPackage(tempPackagePath, interactive: false);
            AssetDatabase.Refresh();
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                throw new InvalidOperationException("TMP Essential Resources import did not create TMP Settings.asset.");
            }
        }

        private static void WriteJson()
        {
            File.WriteAllText(ProjectPath(StyleRoot + "/lastrail-ui-tokens.json"), @"{
  ""name"": ""LAST RAIL Act 1 Launch UI Kit"",
  ""layout"": { ""referenceResolution"": ""1080x1920"", ""portraitSafeArea"": true },
  ""touch"": { ""minDp"": 48, ""preferredDp"": 56 },
  ""typography"": {
    ""fontAsset"": ""Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset"",
    ""materialPreset"": ""Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat""
  },
  ""palette"": {
    ""railBlue"": ""#2D89B4"",
    ""accentCopper"": ""#CC7D3C"",
    ""signalGreen"": ""#64BA8A"",
    ""warningAmber"": ""#E0B450"",
    ""dangerRed"": ""#BE4D48"",
    ""coalInk"": ""#080C11""
  },
  ""surface"": { ""maxCardRadiusPx"": 8, ""glowPolicy"": ""restrained-edge-cue"" },
  ""policy"": {
    ""decorativeOrbsAllowed"": false,
    ""serverTruthFieldsAllowed"": false,
    ""legacyTextAllowed"": false,
    ""standaloneInputModuleAllowed"": false
  }
}");
            File.WriteAllText(ProjectPath(StyleRoot + "/lastrail-ui-kit-readiness.json"), @"{
  ""status"": ""ready_for_launch_screen_reskin"",
  ""referenceResolution"": ""1080x1920"",
  ""minimumTouchDp"": 48,
  ""componentPrefabCount"": 20,
  ""iconCount"": 16,
  ""fontPolicy"": ""Pretendard TMP SDF with UI material preset"",
  ""eventPolicy"": ""Root components expose callbacks; parents bind to public events only"",
  ""railsChoiceIncluded"": false,
  ""worldRaidIncluded"": false
}");
        }

        private static void CreatePrefabs()
        {
            Save(ButtonPrefab("LRButton_Primary", "출격", RailBlue, "mission", true, false), CommonRoot + "/Buttons/LRButton_Primary.prefab");
            Save(ButtonPrefab("LRButton_Secondary", "자세히", PanelAlt, "info", true, false), CommonRoot + "/Buttons/LRButton_Secondary.prefab");
            Save(ButtonPrefab("LRButton_Danger", "취소", Danger, "warning", true, false), CommonRoot + "/Buttons/LRButton_Danger.prefab");
            Save(ButtonPrefab("LRButton_Disabled", "대기", Disabled, null, false, false), CommonRoot + "/Buttons/LRButton_Disabled.prefab");
            Save(ButtonPrefab("LRButton_Icon", "", PanelAlt, "settings", true, true), CommonRoot + "/Buttons/LRButton_Icon.prefab");
            Save(SliderPrefab("LRSlider_Normal", true, false), CommonRoot + "/Sliders/LRSlider_Normal.prefab");
            Save(SliderPrefab("LRSlider_Disabled", false, false), CommonRoot + "/Sliders/LRSlider_Disabled.prefab");
            Save(SliderPrefab("LRSlider_Compact", true, true), CommonRoot + "/Sliders/LRSlider_Compact.prefab");
            Save(InputPrefab("LRInputField_Normal", "호출 부호", false, true), CommonRoot + "/InputFields/LRInputField_Normal.prefab");
            Save(InputPrefab("LRInputField_Focused", "호출 부호", false, true), CommonRoot + "/InputFields/LRInputField_Focused.prefab");
            Save(InputPrefab("LRInputField_Error", "입력 확인", true, true), CommonRoot + "/InputFields/LRInputField_Error.prefab");
            Save(InputPrefab("LRInputField_Disabled", "입력 잠김", false, false), CommonRoot + "/InputFields/LRInputField_Disabled.prefab");
            Save(TogglePrefab(), CommonRoot + "/LRToggle_Checkbox.prefab");
            Save(SegmentedPrefab(), CommonRoot + "/LRSegmentedControl.prefab");
            Save(PanelPrefab("LRModalPanel", new Vector2(800, 520), "확인 필요", "작전 정보와 상태를 다시 확인합니다."), CommonRoot + "/LRModalPanel.prefab");
            Save(ToastPrefab(), CommonRoot + "/LRToastNotification.prefab");
            Save(TooltipPrefab(), CommonRoot + "/LRTooltipCallout.prefab");
            Save(RowPrefab("LRListRow", "목록 항목", "상태", "info"), CommonRoot + "/Rows/LRListRow.prefab");
            Save(RowPrefab("LRCardRow", "작전 카드", "준비됨", "mission"), CommonRoot + "/Rows/LRCardRow.prefab");
            Save(RowPrefab("LRRewardRow", "보상 항목", "표시 전용", "reward"), CommonRoot + "/Rows/LRRewardRow.prefab");
        }

        private static GameObject ButtonPrefab(string name, string label, Color color, string iconName, bool interactable, bool iconOnly)
        {
            var root = Obj(name, iconOnly ? new Vector2(56, 56) : new Vector2(240, 64));
            var image = root.AddComponent<Image>();
            image.color = color;
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = interactable;
            var wrapper = root.AddComponent<LastRailUiButton>();
            root.AddComponent<LastRailUiStyleTag>();

            Image icon = null;
            if (!string.IsNullOrEmpty(iconName))
            {
                icon = Img(root.transform, "Icon", new Vector2(30, 30), Icon(iconName), TextMain);
                icon.rectTransform.anchorMin = new Vector2(0, 0.5f);
                icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
                icon.rectTransform.anchoredPosition = iconOnly ? Vector2.zero : new Vector2(30, 0);
            }

            TMP_Text text = null;
            if (!iconOnly)
            {
                text = Txt(root.transform, "Label", label, 24, TextMain, TextAlignmentOptions.Center, new Vector2(iconName == null ? 200 : 160, 44));
                text.rectTransform.anchoredPosition = new Vector2(iconName == null ? 0 : 24, 0);
            }

            Bind(wrapper, ("button", button), ("backgroundImage", image), ("iconImage", icon), ("labelText", text), ("actionKey", name));
            Tag(root, "button", name);
            return root;
        }

        private static GameObject SliderPrefab(string name, bool interactable, bool compact)
        {
            var root = Obj(name, compact ? new Vector2(240, 48) : new Vector2(360, 56));
            var slider = root.AddComponent<Slider>();
            slider.interactable = interactable;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.62f;
            root.AddComponent<LastRailUiStyleTag>();
            var width = root.GetComponent<RectTransform>().sizeDelta.x;
            var bg = Img(root.transform, "Track", new Vector2(width, 10), null, C(52, 60, 64, 255));
            var fill = Img(root.transform, "Fill", new Vector2(width * 0.62f, 10), null, interactable ? Copper : Disabled);
            fill.rectTransform.anchorMin = new Vector2(0, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0, 0.5f);
            fill.rectTransform.pivot = new Vector2(0, 0.5f);
            fill.rectTransform.anchoredPosition = new Vector2(-width * 0.5f, 0);
            slider.targetGraphic = bg;
            slider.fillRect = fill.rectTransform;
            Tag(root, "slider", name);
            return root;
        }

        private static GameObject InputPrefab(string name, string label, bool error, bool interactable)
        {
            var root = Obj(name, new Vector2(420, 76));
            root.AddComponent<Image>().color = error ? C(61, 34, 35, 255) : PanelAlt;
            var input = root.AddComponent<TMP_InputField>();
            input.interactable = interactable;
            input.textViewport = root.GetComponent<RectTransform>();
            var wrapper = root.AddComponent<LastRailUiInputField>();
            root.AddComponent<LastRailUiStyleTag>();
            var labelText = Txt(root.transform, "Label", label, 18, error ? Warning : TextMuted, TextAlignmentOptions.Left, new Vector2(370, 24));
            labelText.rectTransform.anchoredPosition = new Vector2(0, 18);
            var valueText = Txt(root.transform, "Text", interactable ? "입력" : "사용 불가", 22, TextMain, TextAlignmentOptions.Left, new Vector2(370, 30));
            valueText.rectTransform.anchoredPosition = new Vector2(0, -12);
            input.textComponent = valueText;
            input.placeholder = labelText;
            TMP_Text errorText = null;
            if (error)
            {
                errorText = Txt(root.transform, "Error", "다시 확인해 주세요", 16, Warning, TextAlignmentOptions.Right, new Vector2(370, 20));
                errorText.rectTransform.anchoredPosition = new Vector2(0, -42);
            }
            Bind(wrapper, ("inputField", input), ("labelText", labelText), ("errorText", errorText), ("actionKey", name));
            Tag(root, "input", name);
            return root;
        }

        private static GameObject TogglePrefab()
        {
            var root = Obj("LRToggle_Checkbox", new Vector2(280, 56));
            var toggle = root.AddComponent<Toggle>();
            var wrapper = root.AddComponent<LastRailUiToggle>();
            root.AddComponent<LastRailUiStyleTag>();
            var box = Img(root.transform, "Box", new Vector2(36, 36), null, RailBlue);
            box.rectTransform.anchorMin = new Vector2(0, 0.5f);
            box.rectTransform.anchorMax = new Vector2(0, 0.5f);
            box.rectTransform.anchoredPosition = new Vector2(24, 0);
            var mark = Txt(box.transform, "Mark", "✓", 22, TextMain, TextAlignmentOptions.Center, new Vector2(36, 36));
            var label = Txt(root.transform, "Label", "진동 사용", 22, TextMain, TextAlignmentOptions.Left, new Vector2(200, 38));
            label.rectTransform.anchoredPosition = new Vector2(48, 0);
            toggle.targetGraphic = box;
            toggle.graphic = mark;
            Bind(wrapper, ("toggle", toggle), ("labelText", label), ("actionKey", "LRToggle_Checkbox"));
            Tag(root, "toggle", "checkbox");
            return root;
        }

        private static GameObject SegmentedPrefab()
        {
            var root = Obj("LRSegmentedControl", new Vector2(520, 64));
            var wrapper = root.AddComponent<LastRailUiSegmentedControl>();
            root.AddComponent<LastRailUiStyleTag>();
            var buttons = new Button[3];
            var labels = new TMP_Text[3];
            var names = new[] { "브리핑", "전투", "기록" };
            for (var i = 0; i < 3; i++)
            {
                var segment = ButtonPrefab("Segment" + (i + 1), names[i], i == 0 ? RailBlue : PanelAlt, null, true, false);
                segment.transform.SetParent(root.transform, false);
                var rect = segment.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(168, 52);
                rect.anchoredPosition = new Vector2(-176 + i * 176, 0);
                buttons[i] = segment.GetComponent<Button>();
                labels[i] = segment.GetComponentInChildren<TMP_Text>(true);
            }
            Bind(wrapper, ("segmentButtons", buttons), ("segmentLabels", labels), ("actionKey", "LRSegmentedControl"));
            Tag(root, "segmented-control", "default");
            return root;
        }

        private static GameObject PanelPrefab(string name, Vector2 size, string title, string body)
        {
            var root = Obj(name, size);
            root.AddComponent<Image>().color = Panel;
            root.AddComponent<LastRailUiStyleTag>();
            var titleText = Txt(root.transform, "Title", title, 32, TextMain, TextAlignmentOptions.Left, new Vector2(size.x - 80, 46));
            titleText.rectTransform.anchoredPosition = new Vector2(0, size.y * 0.5f - 58);
            var bodyText = Txt(root.transform, "Body", body, 22, TextMuted, TextAlignmentOptions.Left, new Vector2(size.x - 80, 160));
            bodyText.rectTransform.anchoredPosition = new Vector2(0, 40);
            var close = ButtonPrefab("CloseButton", "", PanelAlt, "close", true, true);
            close.transform.SetParent(root.transform, false);
            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(size.x * 0.5f - 44, size.y * 0.5f - 44);
            Tag(root, "modal", name);
            return root;
        }

        private static GameObject ToastPrefab()
        {
            var root = Obj("LRToastNotification", new Vector2(740, 76));
            root.AddComponent<Image>().color = C(28, 37, 38, 245);
            root.AddComponent<LastRailUiStyleTag>();
            var icon = Img(root.transform, "Icon", new Vector2(32, 32), Icon("info"), Signal);
            icon.rectTransform.anchoredPosition = new Vector2(-330, 0);
            var text = Txt(root.transform, "Message", "상태가 동기화되었습니다", 22, TextMain, TextAlignmentOptions.Left, new Vector2(620, 40));
            text.rectTransform.anchoredPosition = new Vector2(34, 0);
            Tag(root, "toast", "default");
            return root;
        }

        private static GameObject TooltipPrefab()
        {
            var root = Obj("LRTooltipCallout", new Vector2(420, 88));
            root.AddComponent<Image>().color = C(18, 24, 29, 246);
            root.AddComponent<LastRailUiStyleTag>();
            Txt(root.transform, "Message", "아이콘 설명 또는 짧은 안내", 20, TextMain, TextAlignmentOptions.Center, new Vector2(360, 48));
            Tag(root, "tooltip", "default");
            return root;
        }

        private static GameObject RowPrefab(string name, string title, string status, string iconName)
        {
            var root = Obj(name, new Vector2(860, 84));
            root.AddComponent<Image>().color = PanelAlt;
            root.AddComponent<LastRailUiStyleTag>();
            var icon = Img(root.transform, "Icon", new Vector2(36, 36), Icon(iconName), TextMain);
            icon.rectTransform.anchoredPosition = new Vector2(-390, 0);
            var titleText = Txt(root.transform, "Title", title, 23, TextMain, TextAlignmentOptions.Left, new Vector2(520, 34));
            titleText.rectTransform.anchoredPosition = new Vector2(-80, 12);
            var statusText = Txt(root.transform, "Status", status, 19, TextMuted, TextAlignmentOptions.Left, new Vector2(520, 28));
            statusText.rectTransform.anchoredPosition = new Vector2(-80, -20);
            var open = ButtonPrefab("OpenButton", "", Panel, "back", true, true);
            open.transform.SetParent(root.transform, false);
            open.transform.localEulerAngles = new Vector3(0, 0, 180);
            open.GetComponent<RectTransform>().anchoredPosition = new Vector2(388, 0);
            Tag(root, "row", name);
            return root;
        }

        private static void CreateShowcasePrefab()
        {
            var root = Obj("LRLaunchUiKitShowcase", new Vector2(1080, 1920));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<Image>().color = Ink;
            root.AddComponent<LastRailUiStyleTag>();
            SetFixedRect(root.GetComponent<RectTransform>(), new Vector2(1080, 1920));

            var title = Txt(root.transform, "Title", "LAST RAIL UI KIT", 42, TextMain, TextAlignmentOptions.Center, new Vector2(900, 60));
            title.rectTransform.anchoredPosition = new Vector2(0, 850);
            var subtitle = Txt(root.transform, "Subtitle", "Act 1 launch components - 1080x1920", 22, TextMuted, TextAlignmentOptions.Center, new Vector2(900, 34));
            subtitle.rectTransform.anchoredPosition = new Vector2(0, 802);

            Place(root.transform, CommonRoot + "/Buttons/LRButton_Primary.prefab", -300, 700);
            Place(root.transform, CommonRoot + "/Buttons/LRButton_Secondary.prefab", 0, 700);
            Place(root.transform, CommonRoot + "/Buttons/LRButton_Danger.prefab", 300, 700);
            Place(root.transform, CommonRoot + "/Buttons/LRButton_Icon.prefab", 450, 700);
            Place(root.transform, CommonRoot + "/InputFields/LRInputField_Normal.prefab", -230, 590);
            Place(root.transform, CommonRoot + "/InputFields/LRInputField_Error.prefab", 230, 590);
            Place(root.transform, CommonRoot + "/Sliders/LRSlider_Normal.prefab", -230, 482);
            Place(root.transform, CommonRoot + "/Sliders/LRSlider_Compact.prefab", 230, 482);
            Place(root.transform, CommonRoot + "/LRToggle_Checkbox.prefab", -285, 390);
            Place(root.transform, CommonRoot + "/LRSegmentedControl.prefab", 190, 390);
            Place(root.transform, CommonRoot + "/LRToastNotification.prefab", 0, 285);
            Place(root.transform, CommonRoot + "/LRTooltipCallout.prefab", 0, 185);
            Place(root.transform, CommonRoot + "/Rows/LRListRow.prefab", 0, 58);
            Place(root.transform, CommonRoot + "/Rows/LRCardRow.prefab", 0, -48);
            Place(root.transform, CommonRoot + "/Rows/LRRewardRow.prefab", 0, -154);
            Place(root.transform, CommonRoot + "/LRModalPanel.prefab", 0, -520);

            for (var i = 0; i < IconNames.Length; i++)
            {
                var icon = Img(root.transform, "Icon_" + IconNames[i], new Vector2(48, 48), Icon(IconNames[i]), TextMain);
                icon.rectTransform.anchoredPosition = new Vector2(-420 + (i % 8) * 120, -890 + (i / 8) * 68);
            }
            Tag(root, "showcase", "launch");
            SetFixedRect(root.GetComponent<RectTransform>(), new Vector2(1080, 1920));
            Save(root, CommonRoot + "/Showcase/LRLaunchUiKitShowcase.prefab");
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommonRoot + "/Showcase/LRLaunchUiKitShowcase.prefab");
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "LRLaunchUiKitShowcase";
            }
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            EditorSceneManager.SaveScene(scene, SceneRoot + "/LaunchUiKitShowcase.unity");
        }

        private static GameObject Obj(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            SetFixedRect(rect, size);
            return go;
        }

        private static void SetFixedRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }

        private static TMP_Text Txt(Transform parent, string name, string value, int size, Color color, TextAlignmentOptions alignment, Vector2 rectSize)
        {
            var child = Obj(name, rectSize);
            child.transform.SetParent(parent, false);
            var text = child.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontSizeMin = 12;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = alignment;
            text.color = color;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) text.font = font;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) text.fontSharedMaterial = material;
            return text;
        }

        private static Image Img(Transform parent, string name, Vector2 size, Sprite sprite, Color color)
        {
            var child = Obj(name, size);
            child.transform.SetParent(parent, false);
            var image = child.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            return image;
        }

        private static Sprite Icon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(IconRoot + "/ic_" + name + ".png");
        }

        private static void Place(Transform parent, string path, float x, float y)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }

        private static void Save(GameObject root, string path)
        {
            Ensure(Path.GetDirectoryName(path).Replace('\\', '/'));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Bind(MonoBehaviour target, params (string name, object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach (var field in fields)
            {
                var property = serialized.FindProperty(field.name);
                if (property == null) continue;
                if (field.value is UnityEngine.Object unityObject) property.objectReferenceValue = unityObject;
                else if (field.value is string text) property.stringValue = text;
                else if (field.value is Button[] buttons)
                {
                    property.arraySize = buttons.Length;
                    for (var i = 0; i < buttons.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                }
                else if (field.value is TMP_Text[] labels)
                {
                    property.arraySize = labels.Length;
                    for (var i = 0; i < labels.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Tag(GameObject root, string role, string variant)
        {
            var tag = root.GetComponent<LastRailUiStyleTag>();
            if (tag == null) return;
            var serialized = new SerializedObject(tag);
            serialized.FindProperty("componentKey").stringValue = root.name;
            serialized.FindProperty("componentRole").stringValue = role;
            serialized.FindProperty("styleVariant").stringValue = variant;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeFontMaterial()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (font == null || material == null)
            {
                return;
            }

            var shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader != null)
            {
                material.shader = shader;
            }

            var atlas = font.atlasTexture;
            if (atlas != null)
            {
                material.mainTexture = atlas;
                material.SetTexture("_MainTex", atlas);
            }

            material.SetColor("_FaceColor", Color.white);
            material.SetColor("_OutlineColor", C(8, 12, 17, 255));
            material.SetFloat("_OutlineWidth", 0.015f);
            EditorUtility.SetDirty(material);
        }

        private static void CreateIcons()
        {
            foreach (var name in IconNames)
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                var clear = new Color[128 * 128];
                for (var i = 0; i < clear.Length; i++) clear[i] = Color.clear;
                texture.SetPixels(clear);
                DrawIcon(texture, name, TextMain);
                texture.Apply();
                var path = IconRoot + "/ic_" + name + ".png";
                File.WriteAllBytes(ProjectPath(path), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 100f;
                    importer.maxTextureSize = 128;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void DrawIcon(Texture2D texture, string name, Color color)
        {
            switch (name)
            {
                case "back": Line(texture, 84, 30, 42, 64, color, 5); Line(texture, 42, 64, 84, 98, color, 5); Line(texture, 44, 64, 98, 64, color, 5); break;
                case "close": Line(texture, 38, 38, 90, 90, color, 6); Line(texture, 90, 38, 38, 90, color, 6); break;
                case "settings": Circle(texture, 64, 64, 30, color, 4, true); Circle(texture, 64, 64, 8, color, 4, false); for (var i = 0; i < 8; i++) Dot(texture, 64 + Mathf.RoundToInt(Mathf.Cos(i * 0.785f) * 42f), 64 + Mathf.RoundToInt(Mathf.Sin(i * 0.785f) * 42f), color, 5); break;
                case "retry": Circle(texture, 64, 64, 36, color, 5, true); Line(texture, 88, 28, 102, 28, color, 5); Line(texture, 102, 28, 102, 44, color, 5); break;
                case "info": Circle(texture, 64, 64, 38, color, 4, true); Dot(texture, 64, 42, color, 5); Line(texture, 64, 58, 64, 88, color, 5); break;
                case "warning": Line(texture, 64, 26, 102, 98, color, 5); Line(texture, 102, 98, 26, 98, color, 5); Line(texture, 26, 98, 64, 26, color, 5); Line(texture, 64, 52, 64, 76, color, 5); Dot(texture, 64, 90, color, 4); break;
                case "ticket": Rect(texture, 30, 40, 98, 88, color, 4, true); Circle(texture, 32, 64, 8, color, 3, true); Circle(texture, 96, 64, 8, color, 3, true); Line(texture, 54, 46, 74, 82, color, 4); break;
                case "reward": Rect(texture, 36, 50, 92, 96, color, 5, true); Line(texture, 64, 50, 64, 96, color, 4); Line(texture, 28, 50, 100, 50, color, 5); Line(texture, 46, 34, 64, 50, color, 5); Line(texture, 82, 34, 64, 50, color, 5); break;
                case "codex": Rect(texture, 38, 28, 92, 100, color, 5, true); Line(texture, 50, 46, 80, 46, color, 3); Line(texture, 50, 62, 80, 62, color, 3); Line(texture, 50, 78, 72, 78, color, 3); break;
                case "operator": Circle(texture, 64, 46, 18, color, 4, true); Line(texture, 36, 102, 50, 76, color, 5); Line(texture, 50, 76, 78, 76, color, 5); Line(texture, 78, 76, 92, 102, color, 5); break;
                case "mission": Line(texture, 34, 96, 48, 34, color, 5); Line(texture, 48, 34, 92, 34, color, 5); Line(texture, 92, 34, 78, 96, color, 5); Line(texture, 34, 96, 78, 96, color, 5); Line(texture, 48, 72, 82, 72, color, 4); break;
                case "gacha": Circle(texture, 64, 64, 38, color, 5, true); Line(texture, 26, 64, 102, 64, color, 4); Rect(texture, 50, 50, 78, 78, color, 4, true); break;
                case "sound": Line(texture, 34, 54, 48, 54, color, 5); Line(texture, 48, 54, 70, 36, color, 5); Line(texture, 70, 36, 70, 92, color, 5); Line(texture, 70, 92, 48, 74, color, 5); Line(texture, 48, 74, 34, 74, color, 5); Circle(texture, 82, 64, 18, color, 4, true); break;
                case "vibration": Rect(texture, 50, 34, 78, 94, color, 5, true); Line(texture, 34, 46, 26, 58, color, 4); Line(texture, 26, 70, 34, 82, color, 4); Line(texture, 94, 46, 102, 58, color, 4); Line(texture, 102, 70, 94, 82, color, 4); break;
                case "lock": Rect(texture, 40, 56, 88, 98, color, 5, true); Circle(texture, 64, 56, 20, color, 5, true); break;
                case "unlock": Rect(texture, 40, 56, 88, 98, color, 5, true); Circle(texture, 78, 54, 20, color, 5, true); Line(texture, 58, 56, 58, 40, Color.clear, 8); break;
            }
        }

        private static void Line(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;
            while (true)
            {
                Dot(texture, x0, y0, color, thickness);
                if (x0 == x1 && y0 == y1) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void Rect(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness, bool outline)
        {
            if (outline)
            {
                Line(texture, x0, y0, x1, y0, color, thickness);
                Line(texture, x1, y0, x1, y1, color, thickness);
                Line(texture, x1, y1, x0, y1, color, thickness);
                Line(texture, x0, y1, x0, y0, color, thickness);
                return;
            }
            for (var y = y0; y <= y1; y++) Line(texture, x0, y, x1, y, color, 1);
        }

        private static void Circle(Texture2D texture, int cx, int cy, int radius, Color color, int thickness, bool outline)
        {
            for (var angle = 0; angle < 360; angle += 3)
            {
                var radians = angle * Mathf.Deg2Rad;
                if (outline)
                {
                    for (var t = 0; t < thickness; t++) Set(texture, cx + Mathf.RoundToInt(Mathf.Cos(radians) * (radius - t)), cy + Mathf.RoundToInt(Mathf.Sin(radians) * (radius - t)), color);
                }
                else
                {
                    Line(texture, cx, cy, cx + Mathf.RoundToInt(Mathf.Cos(radians) * radius), cy + Mathf.RoundToInt(Mathf.Sin(radians) * radius), color, 1);
                }
            }
        }

        private static void Dot(Texture2D texture, int x, int y, Color color, int radius)
        {
            for (var oy = -radius; oy <= radius; oy++)
            for (var ox = -radius; ox <= radius; ox++)
                if (ox * ox + oy * oy <= radius * radius) Set(texture, x + ox, y + oy, color);
        }

        private static void Set(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height) texture.SetPixel(x, y, color);
        }

        private static void Ensure(string unityPath)
        {
            unityPath = unityPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(unityPath)) return;
            var parent = Path.GetDirectoryName(unityPath)?.Replace('\\', '/');
            var folder = Path.GetFileName(unityPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folder)) throw new InvalidOperationException("Invalid asset folder: " + unityPath);
            if (!AssetDatabase.IsValidFolder(parent)) Ensure(parent);
            if (!AssetDatabase.IsValidFolder(unityPath)) AssetDatabase.CreateFolder(parent, folder);
        }

        private static string ProjectPath(string unityPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), unityPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static Color32 C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }
    }
}
#endif
