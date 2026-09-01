#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchOperatorVisualShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string PolishRoot = "Assets/_LASTRAIL/UI/Operators/CardPolish";
        private const string PolishFramesRoot = PolishRoot + "/Frames";
        private const string PolishBadgesRoot = PolishRoot + "/Badges";
        private const string PolishRarityRoot = PolishRoot + "/Rarity";
        private const string PolishRoleIconsRoot = PolishRoot + "/RoleIcons";
        private const string PolishOverlaysRoot = PolishRoot + "/Overlays";
        private const string PolishAccentsRoot = PolishRoot + "/Accents";

        [MenuItem("LASTRAIL/Launch/Capture Operator Visual Showcase QA")]
        public static void CaptureOperatorVisualShowcase()
        {
            var outputRoot = Path.Combine(Path.GetTempPath(), "LastRailOperatorVisualQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "operator-visual-showcase-qa.json");
            var screenshotPath = Path.Combine(outputRoot, "operator_visual_showcase_1080x1920.png");

            try
            {
                CaptureShowcase(screenshotPath);
                var info = new FileInfo(screenshotPath);
                if (!info.Exists || info.Length < 50000)
                {
                    throw new InvalidOperationException("Operator visual showcase screenshot is empty or too small.");
                }

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"operatorCount\":15,\"screenshotCount\":1,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL launch operator visual showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"operatorCount\":15,\"screenshotCount\":0,\"warningCount\":0,\"failCount\":1,\"blockers\":[\"operator_visual_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL launch operator visual showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void CaptureShowcase(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("OperatorVisualQaCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(5, 7, 10, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var canvasObject = new GameObject("OperatorVisualQaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 1f;

            var backdrop = Image(canvasObject.transform, "Backdrop", new Vector2(1080, 1920), new Color32(5, 7, 10, 255));
            backdrop.rectTransform.anchoredPosition = Vector2.zero;

            var rail = Image(canvasObject.transform, "OperatorGridSectionRail", new Vector2(940, 82), new Color32(255, 255, 255, 170));
            rail.sprite = LoadSprite(PolishAccentsRoot + "/operator_grid_section_rail.png");
            rail.preserveAspect = true;
            rail.rectTransform.anchoredPosition = new Vector2(0, 650);

            var title = Text(canvasObject.transform, "Title", "ACT 1 OPERATOR CARDS", 38, new Color32(238, 241, 234, 255), new Vector2(860, 70));
            title.rectTransform.anchoredPosition = new Vector2(0, 780);
            var note = Text(canvasObject.transform, "Note", "15 launch visual operators / 5 initial / 10 gacha elite", 24, new Color32(176, 188, 188, 255), new Vector2(860, 54));
            note.rectTransform.anchoredPosition = new Vector2(0, 730);

            for (var i = 0; i < LaunchOperatorVisualResourceBuilder.Operators.Length; i++)
            {
                AddCard(canvasObject.transform, LaunchOperatorVisualResourceBuilder.Operators[i], i);
            }

            Canvas.ForceUpdateCanvases();
            var tmpVertices = 0;
            foreach (var text in canvasObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                tmpVertices += text.mesh == null ? 0 : text.mesh.vertexCount;
            }

            if (tmpVertices == 0)
            {
                throw new InvalidOperationException("Operator visual showcase text did not generate visible mesh data.");
            }

            var previous = RenderTexture.active;
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static void AddCard(Transform parent, LaunchOperatorVisualResourceSpec op, int index)
        {
            var col = index % 5;
            var row = index / 5;
            var x = -400 + col * 200;
            var y = 390 - row * 460;

            var card = Image(parent, "Card_" + op.OperatorId, new Vector2(178, 237), Color.white);
            card.sprite = LoadSprite(PolishFramesRoot + "/operator_card_slot_background.png");
            card.preserveAspect = true;
            card.rectTransform.anchoredPosition = new Vector2(x, y);

            var portrait = Image(card.transform, "Portrait", new Vector2(150, 200), new Color32(255, 255, 255, 235));
            portrait.sprite = LoadSprite(op.CardPath);
            portrait.preserveAspect = true;
            portrait.rectTransform.anchoredPosition = new Vector2(0, 18);

            var rarity = Image(card.transform, "Rarity", new Vector2(132, 34), Color.white);
            rarity.sprite = LoadSprite(RarityPath(op));
            rarity.preserveAspect = true;
            rarity.rectTransform.anchoredPosition = new Vector2(0, -104);

            var role = Image(card.transform, "Role", new Vector2(42, 42), Color.white);
            role.sprite = LoadSprite(RolePath(op));
            role.preserveAspect = true;
            role.rectTransform.anchoredPosition = new Vector2(-58, 92);

            if (op.IsPickup)
            {
                var selected = Image(card.transform, "Selected", new Vector2(178, 237), Color.white);
                selected.sprite = LoadSprite(PolishFramesRoot + "/operator_card_selected_frame.png");
                selected.preserveAspect = true;
                selected.rectTransform.anchoredPosition = Vector2.zero;

                var newBadge = Image(card.transform, "NewBadge", new Vector2(78, 30), Color.white);
                newBadge.sprite = LoadSprite(PolishBadgesRoot + "/operator_badge_new.png");
                newBadge.preserveAspect = true;
                newBadge.rectTransform.anchoredPosition = new Vector2(48, 62);
            }

            if (op.Acquisition == "initial_free")
            {
                var owned = Image(card.transform, "OwnedBadge", new Vector2(78, 30), Color.white);
                owned.sprite = LoadSprite(PolishBadgesRoot + "/operator_badge_owned.png");
                owned.preserveAspect = true;
                owned.rectTransform.anchoredPosition = new Vector2(48, 94);
            }

            if (op.OperatorId == "op_sable" || op.OperatorId == "op_anchor" || op.OperatorId == "op_cinder")
            {
                var locked = Image(card.transform, "Locked", new Vector2(178, 237), new Color32(255, 255, 255, 220));
                locked.sprite = LoadSprite(PolishFramesRoot + "/operator_card_locked_overlay.png");
                locked.preserveAspect = true;
                locked.rectTransform.anchoredPosition = Vector2.zero;

                var plate = Image(card.transform, "LockedPlate", new Vector2(104, 38), Color.white);
                plate.sprite = LoadSprite(PolishOverlaysRoot + "/operator_state_locked_label_plate.png");
                plate.preserveAspect = true;
                plate.rectTransform.anchoredPosition = new Vector2(0, -16);
            }

            var level = Image(card.transform, "LevelBadge", new Vector2(76, 28), new Color32(255, 255, 255, 235));
            level.sprite = LoadSprite(PolishBadgesRoot + "/operator_badge_level.png");
            level.preserveAspect = true;
            level.rectTransform.anchoredPosition = new Vector2(-52, -58);

            var label = Text(card.transform, "Label", op.OperatorId, 15, new Color32(238, 241, 234, 255), new Vector2(154, 24));
            label.rectTransform.anchoredPosition = new Vector2(0, -76);
        }

        private static Image Image(Transform parent, string name, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.rectTransform.sizeDelta = size;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(Transform parent, string name, string value, int size, Color color, Vector2 dimensions)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            text.fontSize = size;
            text.fontSizeMin = 13;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = dimensions;
            return text;
        }

        private static string RarityPath(LaunchOperatorVisualResourceSpec op)
        {
            if (op.IsPickup)
            {
                return PolishRarityRoot + "/operator_rarity_pickup_elite.png";
            }

            return op.Acquisition == "initial_free"
                ? PolishRarityRoot + "/operator_rarity_initial_free.png"
                : PolishRarityRoot + "/operator_rarity_elite.png";
        }

        private static string RolePath(LaunchOperatorVisualResourceSpec op)
        {
            return PolishRoleIconsRoot + "/operator_role_" + RoleFor(op.OperatorId) + ".png";
        }

        private static string RoleFor(string operatorId)
        {
            switch (operatorId)
            {
                case "op_kyle":
                case "op_vex":
                case "op_rail":
                    return "striker";
                case "op_reina":
                case "op_anchor":
                case "op_warden":
                    return "guard";
                case "op_sera":
                case "op_marrow":
                    return "medic";
                case "op_eve":
                case "op_iris":
                case "op_cinder":
                    return "engineer";
                default:
                    return "support";
            }
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new FileNotFoundException("Missing operator polish screenshot sprite: " + path);
            }

            return sprite;
        }
    }
}
#endif
