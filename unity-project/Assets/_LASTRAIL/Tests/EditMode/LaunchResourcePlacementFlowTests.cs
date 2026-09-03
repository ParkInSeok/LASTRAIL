#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation;

namespace LastRail.Tests.EditMode
{
    public sealed class LaunchResourcePlacementFlowTests
    {
        private const string ScreenRoot = "Assets/_LASTRAIL/Prefabs/Screens/";
        private const string CombatAnimationManifestPath = "Assets/_LASTRAIL/UI/Style/lastrail-combat-animation-resources.json";
        private const string EditorVideoQaRunnerPath = "Assets/_LASTRAIL/Editor/LaunchEditorFlowVideoQaRunner.cs";
        private const string LatestVideoExporterPath = "Tools/export-act1-launch-latest-qa-mp4.py";

        private static readonly string[] OperatorIds =
        {
            "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira",
            "op_vex", "op_nyx", "op_sable", "op_marrow", "op_iris",
            "op_rail", "op_anchor", "op_lumen", "op_cinder", "op_warden"
        };

        private static readonly string[] InitialSquadIds = { "op_kyle", "op_reina", "op_eve", "op_sera", "op_mira" };
        private static readonly string[] UnitStates = { "idle", "attack", "skill_or_special", "hit", "down_or_defeated" };
        private static readonly string[] BossStates = { "idle", "warning", "attack", "hit", "defeat" };
        private static readonly string[] EnemyIds =
        {
            "enemy_raider_scout", "enemy_railbike_marauder", "enemy_scrapshield_brute",
            "enemy_signal_jammer_drone", "enemy_spike_cart_gunner", "enemy_cinder_hound_mech",
            "elite_broadcast_executioner", "elite_ironclad_boarder"
        };

        [Test]
        public void CombatAnimationCoverage_RequiresAllFifteenLaunchOperatorsEnemiesAndStoryRaidBoss()
        {
            var manifest = ReadFile(CombatAnimationManifestPath);
            Assert.That(manifest, Does.Contain("\"operatorInitialFreeCount\": 5"));
            Assert.That(manifest, Does.Contain("\"operatorEliteCount\": 10"));
            Assert.That(manifest, Does.Contain("\"operatorTotalCount\": 15"));

            foreach (var operatorId in OperatorIds)
            {
                Assert.That(manifest, Does.Contain("\"id\": \"" + operatorId + "\""), operatorId);
                Assert.That(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath("Operators", operatorId)), Is.Not.Null, operatorId);
                foreach (var state in UnitStates)
                {
                    Assert.That(AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath("Operators", operatorId, state)), Is.Not.Null, operatorId + ":" + state);
                    Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(FramePath("Operators", operatorId, state, 1)), Is.Not.Null, operatorId + ":" + state);
                }
            }

            foreach (var enemyId in EnemyIds)
            {
                var folder = enemyId.StartsWith("elite_", StringComparison.Ordinal) ? "EliteEnemies" : "Enemies";
                Assert.That(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath(folder, enemyId)), Is.Not.Null, enemyId);
                foreach (var state in UnitStates)
                {
                    Assert.That(AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(folder, enemyId, state)), Is.Not.Null, enemyId + ":" + state);
                }
            }

            Assert.That(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath("Bosses", "boss_raider_king_engine")), Is.Not.Null);
            foreach (var state in BossStates)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath("Bosses", "boss_raider_king_engine", state)), Is.Not.Null, state);
            }
        }

        [Test]
        public void CombatHudLaunchFlow_UsesInitialSquadAnimatedSpritesAndNoDebugReferenceLabels()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenRoot + "FullyAnimatedCombatHud.prefab");
            Assert.That(prefab, Is.Not.Null);

            var allText = string.Join("\n", Array.ConvertAll(prefab.GetComponentsInChildren<TMP_Text>(true), item => item.text));
            Assert.That(allText, Does.Not.Contain("Unit refs"));
            Assert.That(allText, Does.Not.Contain("Cue refs"));
            Assert.That(allText, Does.Not.Contain("damage="));
            Assert.That(allText, Does.Not.Contain("hitStop="));

            foreach (var operatorId in InitialSquadIds)
            {
                var transform = FindChildRecursive(prefab.transform, "OperatorAnimated_" + operatorId);
                Assert.That(transform, Is.Not.Null, operatorId);
                Assert.That(transform.GetComponent<Image>().sprite.name, Does.StartWith(operatorId + "_idle_"), operatorId);
                Assert.That(transform.GetComponent<Animator>().runtimeAnimatorController, Is.Not.Null, operatorId);
                Assert.That(AssetDatabase.GetAssetPath(transform.GetComponent<Animator>().runtimeAnimatorController), Does.Contain("/CombatAnimations/Animators/Operators/" + operatorId + ".controller"), operatorId);
                Assert.That(transform.parent.name, Does.Not.StartWith("OperatorSlotFrame"), operatorId + " should be placed as a combat unit, not inside a card frame.");
            }

            Assert.That(FindChildRecursive(prefab.transform, "OperatorSlotFrame01"), Is.Null);
            Assert.That(PrefabContainsSprite(prefab, "operator_card_slot_background"), Is.False);
            Assert.That(FindChildRecursive(prefab.transform, "CombatOrdinaryWaveDemoSurface"), Is.Not.Null);
            Assert.That(FindChildRecursive(prefab.transform, "CombatStoryRaidBossDemoSurface"), Is.Not.Null);
            Assert.That(FindChildRecursive(prefab.transform, "StoryRaidBossAnimated_boss_raider_king_engine"), Is.Not.Null);
        }

        [Test]
        public void CombatAnimationManifest_RejectsPortraitDerivedSourcesForActualFlow()
        {
            var manifest = ReadFile(CombatAnimationManifestPath);
            Assert.That(manifest, Does.Contain("\"spriteType\": \"combat_unit\""));
            Assert.That(manifest, Does.Contain("\"portraitDerived\": false"));
            Assert.That(manifest, Does.Contain("\"hasCardFrame\": false"));
            Assert.That(manifest, Does.Contain("\"transparentOrSceneCompatible\": true"));
            Assert.That(manifest, Does.Contain("/CombatAnimations/Sources/"));
            Assert.That(manifest, Does.Not.Contain("/Operators/Portraits/"));
            Assert.That(manifest, Does.Not.Contain("/Operators/Cards/"));
            Assert.That(manifest, Does.Not.Contain("/Operators/Icons/"));
        }

        [Test]
        public void LatestVideoQa_UsesLaunchFlowFramesOnlyAndRecordsPlacementReadiness()
        {
            var runner = ReadFile(EditorVideoQaRunnerPath);
            Assert.That(runner, Does.Contain("combat_ordinary_wave_demo"));
            Assert.That(runner, Does.Contain("combat_elite_squad_a_demo"));
            Assert.That(runner, Does.Contain("combat_elite_squad_b_demo"));
            Assert.That(runner, Does.Contain("combat_story_raid_boss_demo"));
            Assert.That(runner, Does.Contain("ApplyOperatorCombatUnitSprites"));
            Assert.That(runner, Does.Not.Contain("ShowcaseScreens"));
            Assert.That(runner, Does.Not.Contain("combat_animation_showcase"));
            Assert.That(runner, Does.Not.Contain("audio_showcase"));
            Assert.That(runner, Does.Contain("ClearExistingFramePngs"));
            Assert.That(runner, Does.Contain("Directory.GetFiles(framesRoot, \"*.png\")"));
            Assert.That(runner, Does.Contain("resourcePlacementFlowReady: true"));

            var exporter = ReadFile(LatestVideoExporterPath);
            Assert.That(exporter, Does.Not.Contain("ArtResourceWalkthrough"));
            Assert.That(exporter, Does.Contain("resourcePlacementFlowReady"));
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindChildRecursive(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool PrefabContainsSprite(GameObject prefab, string spriteName)
        {
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && string.Equals(image.sprite.name, spriteName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadFile(string path)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private static string FramePath(string folder, string id, string state, int frame)
        {
            return "Assets/_LASTRAIL/UI/CombatAnimations/" + folder + "/" + id + "/" + state + "/" + id + "_" + state + "_" + frame.ToString("000") + ".png";
        }

        private static string ClipPath(string folder, string id, string state)
        {
            return "Assets/_LASTRAIL/UI/CombatAnimations/Clips/" + folder + "/" + id + "/" + id + "_" + state + ".anim";
        }

        private static string ControllerPath(string folder, string id)
        {
            return "Assets/_LASTRAIL/UI/CombatAnimations/Animators/" + folder + "/" + id + ".controller";
        }
    }
}
#endif
