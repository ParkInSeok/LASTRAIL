#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using LastRail.Presentation.Audio;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchAudioResourceBuilder
    {
        private const int SampleRate = 44100;
        private const string AudioRoot = "Assets/_LASTRAIL/Audio";
        private const string UiSfxRoot = AudioRoot + "/Sfx/UI";
        private const string GachaSfxRoot = AudioRoot + "/Sfx/Gacha";
        private const string RewardSfxRoot = AudioRoot + "/Sfx/Reward";
        private const string CombatSfxRoot = AudioRoot + "/Sfx/Combat";
        private const string StoryRaidSfxRoot = AudioRoot + "/Sfx/StoryRaid";
        private const string BgmRoot = AudioRoot + "/Bgm";
        private const string ShowcaseRoot = AudioRoot + "/Showcase";
        private const string StyleRoot = "Assets/_LASTRAIL/UI/Style";
        private const string SceneRoot = "Assets/_LASTRAIL/Scenes";
        private const string FontPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset";
        private const string MaterialPath = "Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Materials/Pretendard-Regular SDF UI.mat";
        private const string ManifestPath = StyleRoot + "/lastrail-audio-resources.json";
        private const string ReadmePath = AudioRoot + "/README.md";
        private const string ShowcaseScenePath = SceneRoot + "/LaunchAudioResourceShowcase.unity";
        public const string ShowcasePrefabPath = ShowcaseRoot + "/LRLaunchAudioResourceShowcase.prefab";
        private const string UiThumbnailPath = "Assets/_LASTRAIL/UI/Common/Skins/ui_button_primary_9slice.png";
        private const string GachaThumbnailPath = "Assets/_LASTRAIL/UI/GachaResources/GachaFx/gacha_recruitment_aperture_open.png";
        private const string RewardThumbnailPath = "Assets/_LASTRAIL/UI/RewardOfflineResources/Frames/reward_result_plate.png";
        private const string CombatThumbnailPath = "Assets/_LASTRAIL/UI/CombatResources/Bosses/boss_raider_king_engine.png";
        private const string StoryRaidThumbnailPath = "Assets/_LASTRAIL/UI/KeyVisuals/kv_story_raid_boss_entrance.png";
        private const string BgmThumbnailPath = "Assets/_LASTRAIL/UI/Backgrounds/bg_main_lobby_train_interior.png";

        private static readonly Color32 Panel = C(7, 12, 16, 238);
        private static readonly Color32 PanelSoft = C(17, 27, 31, 232);
        private static readonly Color32 TextMain = C(238, 241, 234, 255);
        private static readonly Color32 TextMuted = C(172, 184, 184, 255);
        private static readonly Color32 Teal = C(58, 183, 191, 255);
        private static readonly Color32 Amber = C(232, 174, 76, 255);
        private static readonly Color32 Red = C(219, 82, 72, 255);

        public static readonly LaunchAudioResourceSpec[] Clips =
        {
            S("ui_tap", "ui_sfx", UiSfxRoot + "/ui_tap.wav", 0.12f, false, "button_tap_feedback", 880f, WaveKind.Click),
            S("ui_back", "ui_sfx", UiSfxRoot + "/ui_back.wav", 0.16f, false, "back_navigation_feedback", 660f, WaveKind.DownClick),
            S("ui_confirm", "ui_sfx", UiSfxRoot + "/ui_confirm.wav", 0.22f, false, "confirm_action_feedback", 784f, WaveKind.Chime),
            S("ui_error", "ui_sfx", UiSfxRoot + "/ui_error.wav", 0.28f, false, "recoverable_error_feedback", 220f, WaveKind.Warning),
            S("ui_toggle_or_tab", "ui_sfx", UiSfxRoot + "/ui_toggle_or_tab.wav", 0.14f, false, "tab_toggle_feedback", 990f, WaveKind.Click),
            S("gacha_reveal_start", "gacha_sfx", GachaSfxRoot + "/gacha_reveal_start.wav", 0.90f, false, "recruitment_reveal_start", 196f, WaveKind.Riser),
            S("gacha_result_rare", "gacha_sfx", GachaSfxRoot + "/gacha_result_rare.wav", 0.58f, false, "rare_result_flash", 523f, WaveKind.Chime),
            S("gacha_result_elite", "gacha_sfx", GachaSfxRoot + "/gacha_result_elite.wav", 0.74f, false, "elite_result_flash", 659f, WaveKind.Chime),
            S("gacha_result_pickup_elite", "gacha_sfx", GachaSfxRoot + "/gacha_result_pickup_elite.wav", 0.92f, false, "pickup_elite_result_flash", 740f, WaveKind.Riser),
            S("gacha_duplicate_conversion", "gacha_sfx", GachaSfxRoot + "/gacha_duplicate_conversion.wav", 0.48f, false, "duplicate_conversion_feedback", 330f, WaveKind.Shimmer),
            S("reward_claim", "reward_sfx", RewardSfxRoot + "/reward_claim.wav", 0.44f, false, "reward_claim_feedback", 587f, WaveKind.Chime),
            S("reward_line_tick", "reward_sfx", RewardSfxRoot + "/reward_line_tick.wav", 0.10f, false, "reward_line_tick_feedback", 1175f, WaveKind.Click),
            S("combat_hit", "combat_sfx", CombatSfxRoot + "/combat_hit.wav", 0.18f, false, "combat_hit_feedback", 140f, WaveKind.Impact),
            S("combat_skill", "combat_sfx", CombatSfxRoot + "/combat_skill.wav", 0.55f, false, "skill_activation_feedback", 392f, WaveKind.Riser),
            S("combat_ultimate", "combat_sfx", CombatSfxRoot + "/combat_ultimate.wav", 1.10f, false, "ultimate_activation_feedback", 110f, WaveKind.HeavyRiser),
            S("combat_shield", "combat_sfx", CombatSfxRoot + "/combat_shield.wav", 0.42f, false, "shield_state_feedback", 466f, WaveKind.Shimmer),
            S("combat_heal", "combat_sfx", CombatSfxRoot + "/combat_heal.wav", 0.46f, false, "heal_state_feedback", 698f, WaveKind.Chime),
            S("combat_buff", "combat_sfx", CombatSfxRoot + "/combat_buff.wav", 0.36f, false, "buff_state_feedback", 622f, WaveKind.Chime),
            S("combat_debuff", "combat_sfx", CombatSfxRoot + "/combat_debuff.wav", 0.42f, false, "debuff_state_feedback", 247f, WaveKind.Warning),
            S("story_raid_warning", "story_raid_sfx", StoryRaidSfxRoot + "/story_raid_warning.wav", 0.88f, false, "story_raid_warning_feedback", 185f, WaveKind.Warning),
            S("boss_defeat", "story_raid_sfx", StoryRaidSfxRoot + "/boss_defeat.wav", 1.25f, false, "boss_defeat_feedback", 82f, WaveKind.HeavyRiser),
            S("bgm_act1_rail_loop", "bgm", BgmRoot + "/bgm_act1_rail_loop.wav", 12.0f, true, "single_act1_bgm_loop", 55f, WaveKind.Loop)
        };

        [MenuItem("LASTRAIL/Launch/Build Audio Resource Foundation")]
        public static void BuildAudioResources()
        {
            Ensure(AudioRoot);
            Ensure(UiSfxRoot);
            Ensure(GachaSfxRoot);
            Ensure(RewardSfxRoot);
            Ensure(CombatSfxRoot);
            Ensure(StoryRaidSfxRoot);
            Ensure(BgmRoot);
            Ensure(ShowcaseRoot);
            Ensure(StyleRoot);
            Ensure(SceneRoot);

            foreach (var clip in Clips)
            {
                WriteWav(clip);
            }

            AssetDatabase.Refresh();

            foreach (var clip in Clips)
            {
                ConfigureAudioImporter(clip);
            }

            WriteManifest();
            WriteReadme();
            AssetDatabase.Refresh();
            CreateShowcasePrefab();
            CreateShowcaseScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LASTRAIL launch audio resource foundation build complete.");
        }

        private static LaunchAudioResourceSpec S(string id, string category, string path, float duration, bool loop, string usage, float baseFrequency, WaveKind waveKind)
        {
            return new LaunchAudioResourceSpec(id, category, path, duration, loop, usage, baseFrequency, waveKind);
        }

        private static void WriteWav(LaunchAudioResourceSpec clip)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(clip.DurationSeconds * SampleRate));
            var samples = new short[sampleCount];
            var random = new DeterministicNoise(clip.Id);

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)SampleRate;
                var p = i / (float)(sampleCount - 1);
                var value = Sample(clip, t, p, random);
                value = Mathf.Clamp(value, -0.92f, 0.92f);
                samples[i] = (short)Mathf.RoundToInt(value * short.MaxValue);
            }

            using (var writer = new BinaryWriter(File.Open(ProjectPath(clip.Path), FileMode.Create, FileAccess.Write), Encoding.ASCII))
            {
                WriteAscii(writer, "RIFF");
                writer.Write(36 + samples.Length * 2);
                WriteAscii(writer, "WAVE");
                WriteAscii(writer, "fmt ");
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                WriteAscii(writer, "data");
                writer.Write(samples.Length * 2);
                foreach (var sample in samples) writer.Write(sample);
            }
        }

        private static float Sample(LaunchAudioResourceSpec clip, float t, float p, DeterministicNoise random)
        {
            var f = clip.BaseFrequency;
            switch (clip.WaveKind)
            {
                case WaveKind.Click:
                    return Env(p, 0.01f, 0.55f) * (Sine(f, t) * 0.42f + Sine(f * 2f, t) * 0.18f);
                case WaveKind.DownClick:
                    return Env(p, 0.01f, 0.7f) * (Sine(Mathf.Lerp(f * 1.25f, f * 0.55f, p), t) * 0.36f);
                case WaveKind.Chime:
                    return Env(p, 0.03f, 0.82f) * (Sine(f, t) * 0.32f + Sine(f * 1.5f, t) * 0.16f + Sine(f * 2f, t) * 0.08f);
                case WaveKind.Warning:
                    return Env(p, 0.02f, 0.9f) * (Sine(f, t) * 0.34f + Sine(f * 0.5f, t) * 0.18f + random.NextSigned() * 0.04f);
                case WaveKind.Riser:
                    return Env(p, 0.06f, 0.95f) * (Sine(Mathf.Lerp(f, f * 3.2f, p), t) * 0.28f + random.NextSigned() * 0.05f);
                case WaveKind.HeavyRiser:
                    return Env(p, 0.08f, 0.98f) * (Sine(Mathf.Lerp(f, f * 2.4f, p), t) * 0.36f + Sine(f * 0.5f, t) * 0.22f + random.NextSigned() * 0.08f);
                case WaveKind.Shimmer:
                    return Env(p, 0.04f, 0.86f) * (Sine(f, t) * 0.20f + Sine(f * 2.03f, t) * 0.18f + random.NextSigned() * 0.035f);
                case WaveKind.Impact:
                    return Env(p, 0.005f, 0.62f) * (Sine(f, t) * 0.42f + random.NextSigned() * 0.28f);
                case WaveKind.Loop:
                    return Sine(f, t) * 0.12f + Sine(f * 2f, t) * 0.08f + Sine(f * 4f, t) * 0.035f + Sine(0.5f, t) * 0.04f;
                default:
                    return 0f;
            }
        }

        private static float Env(float p, float attack, float release)
        {
            var a = Mathf.Clamp01(p / attack);
            var r = Mathf.Clamp01((1f - p) / Mathf.Max(0.001f, 1f - release));
            return Mathf.SmoothStep(0f, 1f, a) * Mathf.SmoothStep(0f, 1f, r);
        }

        private static float Sine(float frequency, float time)
        {
            return Mathf.Sin(Mathf.PI * 2f * frequency * time);
        }

        private static void ConfigureAudioImporter(LaunchAudioResourceSpec clip)
        {
            var importer = AssetImporter.GetAtPath(clip.Path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing AudioImporter for " + clip.Path);

            importer.forceToMono = true;
            importer.loadInBackground = clip.Loop;
            var settings = importer.defaultSampleSettings;
            settings.loadType = clip.Loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = clip.Loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = clip.Loop ? 0.55f : 1f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void WriteManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"name\": \"LAST RAIL Act 1 Launch Audio Resources\",");
            builder.AppendLine("  \"generatedBy\": \"Unity procedural audio builder\",");
            builder.AppendLine("  \"presentationOnly\": true,");
            builder.AppendLine("  \"serverContractChanged\": false,");
            builder.AppendLine("  \"gameplayTimingChanged\": false,");
            builder.AppendLine("  \"rewardCalculationIncluded\": false,");
            builder.AppendLine("  \"economyMutationIncluded\": false,");
            builder.AppendLine("  \"sampleRate\": 44100,");
            builder.AppendLine("  \"clipCount\": " + Clips.Length.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"clips\": [");
            for (var i = 0; i < Clips.Length; i++)
            {
                var clip = Clips[i];
                builder.Append("    { \"id\": \"").Append(clip.Id).Append("\", \"category\": \"").Append(clip.Category)
                    .Append("\", \"path\": \"").Append(clip.Path).Append("\", \"durationSeconds\": ")
                    .Append(clip.DurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)).Append(", \"loop\": ")
                    .Append(clip.Loop ? "true" : "false").Append(", \"intendedUsage\": \"").Append(clip.IntendedUsage)
                    .Append("\", \"sourceType\": \"generated_local_launch_audio\", \"thumbnailPath\": \"").Append(ThumbnailPathFor(clip.Category))
                    .Append("\", \"sourceArtPath\": \"").Append(SourceArtPathFor(clip.Category))
                    .Append("\", \"qualityClass\": \"ACCEPTABLE_UI_ICON_OR_DECORATION\", \"showcaseVisible\": true }");
                builder.AppendLine(i == Clips.Length - 1 ? string.Empty : ",");
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(ProjectPath(ManifestPath), builder.ToString());
        }

        private static void WriteReadme()
        {
            File.WriteAllText(ProjectPath(ReadmePath), @"# LAST RAIL Launch Audio Resources

These launch SFX and BGM resources are generated by the Unity procedural audio builder as temporary launch-safe placeholders. The showcase rows use high-fidelity launch art thumbnails so video QA does not present the audio surface as debug-only rectangles.

- Source: generated locally in this Unity project by `LaunchAudioResourceBuilder`.
- License/source note: No external paid asset dependency and no third-party audio sample dependency.
- Usage: presentation feedback only for UI, gacha, reward, combat, story raid warning, and one Act 1 BGM loop.
- Replacement policy: final mixed assets may replace these ids as long as the manifest ids, loop flags, and no-truth/no-secret boundaries remain stable.
- Normal launch screens should not autoplay these clips unless a dedicated audio event system dispatches a user-facing cue.

Boundary:
- No gameplay, backend, account, paid service, signing, or non-launch feature scope is encoded here.
");
        }

        private static void CreateShowcasePrefab()
        {
            var root = new GameObject("LRLaunchAudioResourceShowcase", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1080, 1920);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;

            PanelImage(root.transform, "Backdrop", new Vector2(0, 0), new Vector2(1080, 1920), new Color32(5, 8, 12, 255));
            Text(root.transform, "Title", "LAST RAIL Audio Resource Foundation", 34, TextMain, new Vector2(0, 842), new Vector2(920, 60));
            Text(root.transform, "Subtitle", "SFX + single BGM loop / manifest-driven launch placeholders", 22, TextMuted, new Vector2(0, 794), new Vector2(920, 44));
            var audioPlayer = AttachAudioCuePlayer(root);

            var startY = 700f;
            var row = 0;
            foreach (var clip in Clips)
            {
                var x = row % 2 == 0 ? -268f : 268f;
                var y = startY - (row / 2) * 134f;
                CreateClipRow(root.transform, clip, new Vector2(x, y), audioPlayer);
                row++;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root.transform, false);

            Ensure(ShowcaseRoot);
            PrefabUtility.SaveAsPrefabAsset(root, ShowcasePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CreateClipRow(Transform parent, LaunchAudioResourceSpec clip, Vector2 position, LastRailAudioCuePlayer audioPlayer)
        {
            var row = PanelImage(parent, "AudioRow_" + clip.Id, position, new Vector2(480, 112), PanelSoft);
            Border(row.GetComponent<Image>(), clip.Loop ? Amber : Teal);

            var thumbnail = PanelImage(row.transform, "SourceArtThumbnail", new Vector2(-184, 0), new Vector2(92, 92), new Color32(255, 255, 255, 255));
            var thumbnailImage = thumbnail.GetComponent<Image>();
            thumbnailImage.sprite = LoadSprite(ThumbnailPathFor(clip.Category));
            thumbnailImage.preserveAspect = true;
            thumbnailImage.raycastTarget = false;
            Border(thumbnailImage, new Color32(238, 214, 132, 210));

            Text(row.transform, "Id", clip.Id, 18, TextMain, new Vector2(-28, 22), new Vector2(270, 30));
            Text(row.transform, "Meta", clip.Category + " / " + clip.DurationSeconds.ToString("0.00", CultureInfo.InvariantCulture) + "s" + (clip.Loop ? " / loop" : ""), 14, TextMuted, new Vector2(-28, -16), new Vector2(270, 28));

            var button = PanelImage(row.transform, "PreviewButton", new Vector2(184, -18), new Vector2(86, 36), clip.Loop ? Amber : Teal);
            var buttonComponent = button.AddComponent<Button>();
            UnityEventTools.AddStringPersistentListener(buttonComponent.onClick, audioPlayer.PlayCueFromUnityEvent, clip.Id);
            Text(button.transform, "Label", "CUE", 13, new Color32(5, 8, 12, 255), Vector2.zero, new Vector2(74, 24));

            var sourceObject = new GameObject("AudioSource_" + clip.Id, typeof(AudioSource));
            sourceObject.transform.SetParent(row.transform, false);
            var source = sourceObject.GetComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clip.Path);
            source.playOnAwake = false;
            source.loop = clip.Loop;
            source.volume = clip.Loop ? 0.35f : 0.8f;
        }


        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException("Missing audio showcase thumbnail sprite: " + assetPath);
            }

            return sprite;
        }

        private static string ThumbnailPathFor(string category)
        {
            switch (category)
            {
                case "ui_sfx":
                    return UiThumbnailPath;
                case "gacha_sfx":
                    return GachaThumbnailPath;
                case "reward_sfx":
                    return RewardThumbnailPath;
                case "combat_sfx":
                    return CombatThumbnailPath;
                case "story_raid_sfx":
                    return StoryRaidThumbnailPath;
                case "bgm":
                    return BgmThumbnailPath;
                default:
                    return UiThumbnailPath;
            }
        }

        private static string SourceArtPathFor(string category)
        {
            return ThumbnailPathFor(category);
        }

        private static LastRailAudioCuePlayer AttachAudioCuePlayer(GameObject root)
        {
            var sfxSource = root.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 0.8f;

            var bgmSource = root.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = 0.35f;

            var player = root.AddComponent<LastRailAudioCuePlayer>();
            var serialized = new SerializedObject(player);
            serialized.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            serialized.FindProperty("bgmSource").objectReferenceValue = bgmSource;
            var bindings = serialized.FindProperty("clipBindings");
            bindings.arraySize = Clips.Length;
            for (var i = 0; i < Clips.Length; i++)
            {
                var clip = Clips[i];
                var element = bindings.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cueId").stringValue = clip.Id;
                element.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(clip.Path);
                element.FindPropertyRelative("loop").boolValue = clip.Loop;
                element.FindPropertyRelative("volume").floatValue = clip.Loop ? 0.35f : 0.8f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        private static GameObject PanelImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color32 color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        private static TMP_Text Text(Transform parent, string name, string value, int size, Color32 color, Vector2 anchoredPosition, Vector2 textSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = textSize;
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            return text;
        }

        private static void Border(Image image, Color32 color)
        {
            var rect = image.GetComponent<RectTransform>();
            var parent = image.transform;
            PanelImage(parent, "BorderTop", new Vector2(0, rect.sizeDelta.y * 0.5f - 2), new Vector2(rect.sizeDelta.x, 4), color);
            PanelImage(parent, "BorderBottom", new Vector2(0, -rect.sizeDelta.y * 0.5f + 2), new Vector2(rect.sizeDelta.x, 4), color);
            PanelImage(parent, "BorderLeft", new Vector2(-rect.sizeDelta.x * 0.5f + 2, 0), new Vector2(4, rect.sizeDelta.y), color);
            PanelImage(parent, "BorderRight", new Vector2(rect.sizeDelta.x * 0.5f - 2, 0), new Vector2(4, rect.sizeDelta.y), color);
        }

        private static void CreateShowcaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcasePrefabPath);
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "LRLaunchAudioResourceShowcase";
            }

            EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        }

        private static void Ensure(string assetPath)
        {
            var fullPath = ProjectPath(assetPath);
            if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        private static void WriteAscii(BinaryWriter writer, string value)
        {
            writer.Write(Encoding.ASCII.GetBytes(value));
        }

        private static Color32 C(byte r, byte g, byte b, byte a)
        {
            return new Color32(r, g, b, a);
        }

        private sealed class DeterministicNoise
        {
            private uint state;

            public DeterministicNoise(string seed)
            {
                state = 2166136261u;
                foreach (var c in seed)
                {
                    state ^= c;
                    state *= 16777619u;
                }
            }

            public float NextSigned()
            {
                state = state * 1664525u + 1013904223u;
                return ((state >> 8) / 16777215f) * 2f - 1f;
            }
        }

        public enum WaveKind
        {
            Click,
            DownClick,
            Chime,
            Warning,
            Riser,
            HeavyRiser,
            Shimmer,
            Impact,
            Loop
        }
    }

    public sealed class LaunchAudioResourceSpec
    {
        public LaunchAudioResourceSpec(string id, string category, string path, float durationSeconds, bool loop, string intendedUsage, float baseFrequency, LaunchAudioResourceBuilder.WaveKind waveKind)
        {
            Id = id;
            Category = category;
            Path = path;
            DurationSeconds = durationSeconds;
            Loop = loop;
            IntendedUsage = intendedUsage;
            BaseFrequency = baseFrequency;
            WaveKind = waveKind;
        }

        public string Id { get; }
        public string Category { get; }
        public string Path { get; }
        public float DurationSeconds { get; }
        public bool Loop { get; }
        public string IntendedUsage { get; }
        public float BaseFrequency { get; }
        public LaunchAudioResourceBuilder.WaveKind WaveKind { get; }
    }
}
#endif
