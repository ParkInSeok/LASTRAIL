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
            card.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(op.CardPath);
            card.preserveAspect = true;
            card.rectTransform.anchoredPosition = new Vector2(x, y);

            var frame = Image(card.transform, "Frame", new Vector2(178, 237), Color.white);
            frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(op.FramePath);
            frame.preserveAspect = true;
            frame.rectTransform.anchoredPosition = Vector2.zero;

            var label = Text(card.transform, "Label", op.OperatorId, 15, new Color32(238, 241, 234, 255), new Vector2(154, 26));
            label.rectTransform.anchoredPosition = new Vector2(0, -94);
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
    }
}
#endif
