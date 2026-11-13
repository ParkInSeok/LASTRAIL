#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LastRail.Presentation.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace LastRail.Editor
{
    public static class LaunchAnimationAssetPipelineBuilder
    {
        private const string PipelineRoot = "Assets/_LASTRAIL/Art/Animation";
        private const string AtlasRoot = PipelineRoot + "/Atlases";
        private const string AnimatorRoot = PipelineRoot + "/Animators";
        private const string ClipRoot = PipelineRoot + "/Clips";

        private static readonly string[] AtlasNames =
        {
            "operator_combat",
            "operator_portrait_icon",
            "enemy_combat",
            "raid_boss_combat",
            "sprite_sheet_vfx"
        };

        private static readonly string[] VfxClipIds =
        {
            "vfx_large_impact",
            "vfx_hit_spark",
            "vfx_full_screen_flash",
            "vfx_screen_shake_small",
            "vfx_screen_shake_medium",
            "vfx_screen_shake_heavy",
            "vfx_boss_phase"
        };

        [MenuItem("LASTRAIL/Launch/Build Animation Asset Pipeline")]
        public static void BuildLaunchAnimationPipeline()
        {
            EnsureDirectory("Assets/_LASTRAIL/Art");
            EnsureDirectory(PipelineRoot);
            EnsureDirectory(AtlasRoot);
            EnsureDirectory(AnimatorRoot);
            EnsureDirectory(AnimatorRoot + "/Operators");
            EnsureDirectory(AnimatorRoot + "/Enemies");
            EnsureDirectory(AnimatorRoot + "/RaidBosses");
            EnsureDirectory(ClipRoot);
            EnsureDirectory(ClipRoot + "/Operators");
            EnsureDirectory(ClipRoot + "/Enemies");
            EnsureDirectory(ClipRoot + "/RaidBosses");
            EnsureDirectory(ClipRoot + "/VFX");
            EnsureDirectory(PipelineRoot + "/Sources");
            EnsureDirectory(PipelineRoot + "/Sources/OperatorsCombat");
            EnsureDirectory(PipelineRoot + "/Sources/OperatorsPortraitIcon");
            EnsureDirectory(PipelineRoot + "/Sources/EnemiesCombat");
            EnsureDirectory(PipelineRoot + "/Sources/RaidBossCombat");
            EnsureDirectory(PipelineRoot + "/Sources/SpriteSheetVFX");

            BuildAtlases();

            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate();
            var operatorControllerCount = 0;
            var enemyControllerCount = 0;
            var raidBossControllerCount = 0;
            var animationClipCount = 0;

            foreach (var entry in manifest.Entries)
            {
                switch (entry.Role)
                {
                    case AnimationAssetRole.Operator:
                        animationClipCount += CreateClipsAndController("Operators", entry);
                        operatorControllerCount++;
                        break;
                    case AnimationAssetRole.Enemy:
                        animationClipCount += CreateClipsAndController("Enemies", entry);
                        enemyControllerCount++;
                        break;
                    case AnimationAssetRole.RaidBoss:
                        animationClipCount += CreateClipsAndController("RaidBosses", entry);
                        raidBossControllerCount++;
                        break;
                }
            }

            var vfxClipCount = CreateVfxClips();
            WriteReadinessJson(operatorControllerCount, enemyControllerCount, raidBossControllerCount, animationClipCount, vfxClipCount);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildAtlases()
        {
            foreach (var atlasName in AtlasNames)
            {
                CreateOrUpdateAtlas(atlasName, 2048);
                CreateOrUpdateAtlas(atlasName, 1024);
            }
        }

        private static void CreateOrUpdateAtlas(string atlasName, int maxSize)
        {
            var path = AtlasRoot + "/" + atlasName + "_" + maxSize.ToString(CultureInfo.InvariantCulture) + ".spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            atlas.SetIncludeInBuild(true);
            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            });
            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                overridden = false,
                maxTextureSize = maxSize,
                format = TextureImporterFormat.Automatic
            });
            atlas.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = maxSize,
                format = TextureImporterFormat.ASTC_6x6
            });
            EditorUtility.SetDirty(atlas);
        }

        private static int CreateClipsAndController(string roleFolder, AnimationAssetManifestEntry entry)
        {
            var clipFolder = ClipRoot + "/" + roleFolder + "/" + entry.AssetId;
            EnsureDirectory(clipFolder);

            var clips = new List<AnimationClip>();
            for (var i = 0; i < entry.AnimationKeys.Count; i++)
            {
                clips.Add(CreateOrUpdateClip(clipFolder + "/" + entry.AssetId + "_" + entry.AnimationKeys[i] + ".anim"));
            }

            CreateOrUpdateController(AnimatorRoot + "/" + roleFolder + "/" + entry.AssetId + ".controller", entry.AnimationKeys, clips);
            return clips.Count;
        }

        private static AnimationClip CreateOrUpdateClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = 12f;
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void CreateOrUpdateController(string path, IReadOnlyList<string> animationKeys, IReadOnlyList<AnimationClip> clips)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            var stateMachine = controller.layers[0].stateMachine;
            for (var i = 0; i < animationKeys.Count; i++)
            {
                var state = FindState(stateMachine, animationKeys[i]);
                if (state == null)
                {
                    state = stateMachine.AddState(animationKeys[i]);
                }

                state.motion = clips[i];
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            var states = stateMachine.states;
            for (var i = 0; i < states.Length; i++)
            {
                if (string.Equals(states[i].state.name, stateName, StringComparison.Ordinal))
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static int CreateVfxClips()
        {
            for (var i = 0; i < VfxClipIds.Length; i++)
            {
                CreateOrUpdateClip(ClipRoot + "/VFX/" + VfxClipIds[i] + ".anim");
            }

            return VfxClipIds.Length;
        }

        private static void WriteReadinessJson(
            int operatorControllerCount,
            int enemyControllerCount,
            int raidBossControllerCount,
            int animationClipCount,
            int vfxClipCount)
        {
            var spec = AnimationAssetPipelineSpec.ActOneLaunchRecommended();
            var budget = spec.VfxBudget;
            var json = "{"
                + "\"pipelineStatus\":\"EDITOR_VERIFIABLE_PLACEHOLDERS\","
                + "\"toolchainName\":\"" + Escape(spec.ToolchainName) + "\","
                + "\"operatorControllerCount\":" + operatorControllerCount.ToString(CultureInfo.InvariantCulture) + ","
                + "\"enemyControllerCount\":" + enemyControllerCount.ToString(CultureInfo.InvariantCulture) + ","
                + "\"raidBossControllerCount\":" + raidBossControllerCount.ToString(CultureInfo.InvariantCulture) + ","
                + "\"animationClipCount\":" + animationClipCount.ToString(CultureInfo.InvariantCulture) + ","
                + "\"vfxClipCount\":" + vfxClipCount.ToString(CultureInfo.InvariantCulture) + ","
                + "\"primaryAtlasMaxSize\":" + spec.PrimaryAtlasMaxSize.ToString(CultureInfo.InvariantCulture) + ","
                + "\"lowEndFallbackAtlasSize\":" + spec.LowEndFallbackAtlasSize.ToString(CultureInfo.InvariantCulture) + ","
                + "\"atlasGroupCount\":" + AtlasNames.Length.ToString(CultureInfo.InvariantCulture) + ","
                + "\"largeVfxConcurrentLimit\":" + budget.LargeVfxConcurrentLimit.ToString(CultureInfo.InvariantCulture) + ","
                + "\"hitSparkConcurrentMin\":" + budget.HitSparkConcurrentMin.ToString(CultureInfo.InvariantCulture) + ","
                + "\"hitSparkConcurrentMax\":" + budget.HitSparkConcurrentMax.ToString(CultureInfo.InvariantCulture) + ","
                + "\"fullScreenFlashMaxSeconds\":" + budget.FullScreenFlashMaxSeconds.ToString("0.##", CultureInfo.InvariantCulture) + ","
                + "\"bossPhaseVfxMinSeconds\":" + budget.BossPhaseVfxMinSeconds.ToString("0.##", CultureInfo.InvariantCulture) + ","
                + "\"bossPhaseVfxMaxSeconds\":" + budget.BossPhaseVfxMaxSeconds.ToString("0.##", CultureInfo.InvariantCulture) + ","
                + "\"screenShakeTierCount\":" + budget.ScreenShakeTierCount.ToString(CultureInfo.InvariantCulture) + ","
                + "\"hudReadabilityFirst\":" + (budget.HudReadabilityFirst ? "true" : "false") + ","
                + "\"hasDownloadedExternalAsset\":false,"
                + "\"hasRawArtPayload\":false,"
                + "\"hasServerTruthMutation\":false,"
                + "\"hasRewardCalculation\":false,"
                + "\"hasEconomyMutation\":false,"
                + "\"hasClientSideRng\":false"
                + "}";

            var absolutePath = Path.Combine(Application.dataPath, "_LASTRAIL/Art/Animation/launch_animation_pipeline_readiness.json");
            File.WriteAllText(absolutePath, json);
        }

        public static void VerifyLaunchAnimationPipeline()
        {
            foreach (var atlasName in AtlasNames)
            {
                AssertAsset<SpriteAtlas>(AtlasRoot + "/" + atlasName + "_2048.spriteatlas");
                AssertAsset<SpriteAtlas>(AtlasRoot + "/" + atlasName + "_1024.spriteatlas");
            }

            var manifest = ActOneAnimationAssetManifestFixtures.CreateMinimumLaunchCandidate();
            foreach (var entry in manifest.Entries)
            {
                switch (entry.Role)
                {
                    case AnimationAssetRole.Operator:
                        AssertControllerAndClips("Operators", entry);
                        break;
                    case AnimationAssetRole.Enemy:
                        AssertControllerAndClips("Enemies", entry);
                        break;
                    case AnimationAssetRole.RaidBoss:
                        AssertControllerAndClips("RaidBosses", entry);
                        break;
                }
            }

            for (var i = 0; i < VfxClipIds.Length; i++)
            {
                AssertAsset<AnimationClip>(ClipRoot + "/VFX/" + VfxClipIds[i] + ".anim");
            }

            var report = AssetDatabase.LoadAssetAtPath<TextAsset>(PipelineRoot + "/launch_animation_pipeline_readiness.json");
            if (report == null || !report.text.Contains("\"operatorControllerCount\":15"))
            {
                throw new InvalidOperationException("Launch animation pipeline readiness report is missing or incomplete.");
            }
        }

        private static void AssertControllerAndClips(string roleFolder, AnimationAssetManifestEntry entry)
        {
            AssertAsset<AnimatorController>(AnimatorRoot + "/" + roleFolder + "/" + entry.AssetId + ".controller");
            for (var i = 0; i < entry.AnimationKeys.Count; i++)
            {
                AssertAsset<AnimationClip>(ClipRoot + "/" + roleFolder + "/" + entry.AssetId + "/" + entry.AssetId + "_" + entry.AnimationKeys[i] + ".anim");
            }
        }

        private static void AssertAsset<T>(string path) where T : UnityEngine.Object
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            {
                throw new InvalidOperationException("Missing launch animation pipeline asset: " + path);
            }
        }
        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = Path.GetFileName(path);
            EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif



