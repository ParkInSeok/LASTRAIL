#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Gacha;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchGachaScreenPolishQaRunner
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string PrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab";
        private const string OutputDirectoryName = "LastRailGachaPolishQa";

        private static readonly (string StateKey, Func<GachaScreenViewState> CreateState)[] States =
        {
            ("idle", CreateIdleState),
            ("single_pull", CreateSinglePullState),
            ("ten_pull", CreateTenPullState),
            ("duplicate_conversion", CreateDuplicateState)
        };

        [MenuItem("LASTRAIL/Launch/Capture Gacha Screen Polish QA")]
        public static void CaptureGachaScreenPolishQa()
        {
            var outputRoot = Path.Combine("D:/Project/LASTRAIL/LocalTemp/Unity", OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);
            var screenshotPaths = new List<string>();

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (var state in States)
                {
                    screenshotPaths.Add(CaptureState(state.StateKey, state.CreateState(), outputRoot));
                }

                var json = "{\"status\":\"PASS\",\"screenKey\":\"gacha_screen\",\"width\":1080,\"height\":1920,\"stateCount\":4,\"screenshotCount\":" +
                    screenshotPaths.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"warningCount\":0,\"failCount\":0,\"blockers\":[]}";
                File.WriteAllText(Path.Combine(outputRoot, "gacha-screen-polish-screenshot-qa.json"), json);
                Debug.Log("LASTRAIL gacha screen polish screenshot QA captured: " + outputRoot);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                var json = "{\"status\":\"FAIL\",\"screenKey\":\"gacha_screen\",\"width\":1080,\"height\":1920,\"stateCount\":4,\"screenshotCount\":" +
                    screenshotPaths.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    ",\"warningCount\":0,\"failCount\":1,\"blockers\":[\"gacha_screen_polish_screenshot_failed\"]}";
                File.WriteAllText(Path.Combine(outputRoot, "gacha-screen-polish-screenshot-qa.json"), json);
                Debug.LogError("LASTRAIL gacha screen polish screenshot QA failed: " + ex.GetType().Name + ": " + ex.Message);
                EditorApplication.Exit(1);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static string CaptureState(string stateKey, GachaScreenViewState state, string outputRoot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Missing gacha screen prefab: " + PrefabPath);
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate gacha screen prefab.");
            }

            var behaviour = root.GetComponent<GachaScreenRootBehaviour>();
            if (behaviour == null)
            {
                throw new InvalidOperationException("Missing gacha root behaviour.");
            }

            var cameraObject = new GameObject("gacha_polish_" + stateKey + "_Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.052f, 0.062f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            PrepareCanvasForCamera(root, camera);
            behaviour.Render(state);
            Canvas.ForceUpdateCanvases();

            var path = Path.Combine(outputRoot, "gacha_screen_polish_" + stateKey + "_1080x1920.png");
            Render(camera, path);
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < MinimumScreenshotBytes(stateKey))
            {
                throw new InvalidOperationException("Gacha polish screenshot is empty or too small: " + stateKey);
            }

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            return path;
        }

        private static long MinimumScreenshotBytes(string stateKey)
        {
            return stateKey == "idle" ? 30000 : 50000;
        }

        private static void PrepareCanvasForCamera(GameObject root, Camera camera)
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                canvas.pixelPerfect = false;
            }
        }

        private static void Render(Camera camera, string path)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GachaScreenViewState CreateIdleState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.Idle(
                "banner_act1_launch",
                "Act 1 구조 신호",
                new GachaPullCostDto("cur_command_ticket", 1),
                new GachaPityDisplayState("banner_act1_launch", 21, 17, 55, 5)));
        }

        private static GachaScreenViewState CreateSinglePullState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response(1, false))),
                "Act 1 구조 신호"));
        }

        private static GachaScreenViewState CreateTenPullState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.Accepted(Response(10, true))),
                "Act 1 구조 신호"));
        }

        private static GachaScreenViewState CreateDuplicateState()
        {
            return new GachaScreenViewState(GachaScreenPresentationState.FromUxState(
                GachaPullUxState.FromClientResult(GachaPullClientResult.DuplicateReplay(Response(10, true, GachaPullResultStatus.DuplicateReplay, false, "duplicate_replay"))),
                "Act 1 구조 신호"));
        }

        private static GachaPullResponseDto Response(
            int pullCount,
            bool includeDuplicate,
            GachaPullResultStatus status = GachaPullResultStatus.Accepted,
            bool accepted = true,
            string errorCode = null)
        {
            return new GachaPullResponseDto(
                "banner_act1_launch",
                accepted,
                "batch_polish_" + pullCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                status,
                GachaPullCount.FromValue(pullCount),
                "1",
                new GachaPullCostDto("cur_command_ticket", pullCount),
                new GachaPostPityStateDto(42, 12, 22, 2, 4),
                Results(pullCount, includeDuplicate),
                new DateTime(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc),
                errorCode,
                null);
        }

        private static IReadOnlyList<GachaPullResultDto> Results(int pullCount, bool includeDuplicate)
        {
            var results = new List<GachaPullResultDto>();
            results.Add(new GachaPullResultDto(
                1,
                GachaRarity.Elite,
                GachaPullResultKind.Operator,
                "result_001",
                new GachaRewardDto("OPERATOR", "op_vex", 1),
                true,
                "PICKUP_ELITE_60",
                false,
                null));

            for (var i = 2; i <= pullCount; i++)
            {
                if (includeDuplicate && i == 2)
                {
                    results.Add(new GachaPullResultDto(
                        i,
                        GachaRarity.Rare,
                        GachaPullResultKind.OperatorToken,
                        "result_002",
                        new GachaRewardDto("OPERATOR_TOKEN", "cur_operator_token", 50),
                        false,
                        "RARE_OR_HIGHER_10",
                        true,
                        new GachaDuplicateConversionDto("op_vex", "cur_operator_token", 50)));
                    continue;
                }

                results.Add(new GachaPullResultDto(
                    i,
                    GachaRarity.Common,
                    GachaPullResultKind.Item,
                    "result_" + i.ToString("000"),
                    new GachaRewardDto("ITEM", "item_scrap", 1),
                    false,
                    "NONE",
                    false,
                    null));
            }

            return results;
        }
    }
}
#endif
