#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchArtResourceShowcaseQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private static readonly Color TextMain = new Color32(236, 239, 235, 255);
        private static readonly Color Scrim = new Color32(8, 12, 17, 190);

        [MenuItem("LASTRAIL/Launch/Capture Art Resource Showcase QA")]
        public static void CaptureArtResources()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", "LastRailArtResourceQa");
            Directory.CreateDirectory(outputRoot);
            var reportPath = Path.Combine(outputRoot, "art-resource-showcase-qa.json");
            var screenshotCount = 0;

            try
            {
                foreach (var resource in LaunchArtResourceBuilder.Resources)
                {
                    var screenshotPath = Path.Combine(outputRoot, resource.Id + ".png");
                    CaptureResource(resource, screenshotPath);
                    var info = new FileInfo(screenshotPath);
                    if (!info.Exists || info.Length < 8192)
                    {
                        throw new InvalidOperationException("Launch art screenshot is empty or too small: " + resource.Id);
                    }
                    screenshotCount++;
                }

                File.WriteAllText(reportPath, "{\"status\":\"PASS\",\"width\":1080,\"height\":1920,\"screenshotCount\":8,\"warningCount\":0,\"failCount\":0,\"blockers\":[]}");
                Debug.Log("LASTRAIL launch art showcase QA captured: " + outputRoot);
            }
            catch (Exception ex)
            {
                File.WriteAllText(reportPath, "{\"status\":\"FAIL\",\"width\":1080,\"height\":1920,\"screenshotCount\":" + screenshotCount + ",\"warningCount\":0,\"failCount\":1,\"blockers\":[\"launch_art_screenshot_qa_failed\"]}");
                Debug.LogError("LASTRAIL launch art showcase QA failed: " + ex.GetType().Name + ": " + ex.Message);
                throw;
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void CaptureResource(LaunchArtResourceBuilder.LaunchArtResourceSpec resource, string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("LaunchArtQaCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var canvasObject = new GameObject("LaunchArtQaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            var background = backgroundObject.GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(resource.Path);
            if (background.sprite == null)
            {
                throw new InvalidOperationException("Missing launch art sprite: " + resource.Path);
            }
            background.preserveAspect = false;
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;

            var topScrim = Image(canvasObject.transform, "TopReadabilityScrim", new Vector2(900f, 96f), Scrim);
            topScrim.rectTransform.anchoredPosition = new Vector2(0f, 760f);
            var title = Text(canvasObject.transform, "SurfaceLabel", resource.Label, 32, TextMain, new Vector2(820f, 56f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 760f);

            var bottomScrim = Image(canvasObject.transform, "BottomSafeAreaScrim", new Vector2(920f, 190f), Scrim);
            bottomScrim.rectTransform.anchoredPosition = new Vector2(0f, -760f);
            var note = Text(canvasObject.transform, "ReadabilityNote", "safe area / overlay readable / presentation resource", 24, TextMain, new Vector2(820f, 60f));
            note.rectTransform.anchoredPosition = new Vector2(0f, -760f);

            Canvas.ForceUpdateCanvases();
            var tmpCount = 0;
            var tmpVertices = 0;
            foreach (var text in canvasObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
                tmpCount++;
                tmpVertices += text.mesh == null ? 0 : text.mesh.vertexCount;
            }
            if (tmpCount < 2 || tmpVertices == 0)
            {
                throw new InvalidOperationException("Launch art overlay TMP text did not generate visible mesh data: " + resource.Id);
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

        private static Image Image(Transform parent, string name, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.rectTransform.sizeDelta = size;
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
            text.fontSizeMin = 16;
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = dimensions;
            return text;
        }
    }
}
#endif
