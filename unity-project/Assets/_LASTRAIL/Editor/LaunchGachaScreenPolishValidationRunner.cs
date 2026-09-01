#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LastRail.Gacha;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastRail.Editor
{
    public static class LaunchGachaScreenPolishValidationRunner
    {
        private const string PrefabPath = "Assets/_LASTRAIL/Prefabs/Screens/GachaScreen.prefab";
        private const string OutputDirectoryName = "LastRailGachaPolishQa";

        private static readonly (string FieldName, string SpriteName)[] RequiredSkinBindings =
        {
            ("bannerKeyVisualImage", "kv_gacha_recruitment_chamber"),
            ("ticketIconImage", "cur_command_ticket"),
            ("singlePullRevealPlateImage", "gacha_single_pull_reveal_plate"),
            ("tenPullGridFrameImage", "gacha_ten_pull_result_grid_frame"),
            ("rareGlowImage", "gacha_rare_glow"),
            ("eliteGlowImage", "gacha_elite_glow"),
            ("pickupEliteGlowImage", "gacha_pickup_elite_glow"),
            ("duplicateConversionImage", "gacha_duplicate_conversion_visual"),
            ("ticketSpendImage", "gacha_ticket_spend_pieces"),
            ("resultCardFrameImage", "gacha_result_card_frame"),
            ("operatorCardSlotBackgroundImage", "operator_card_slot_background"),
            ("operatorRarityAccentImage", "operator_rarity_pickup_elite"),
            ("operatorSelectedFrameImage", "operator_card_selected_frame"),
            ("operatorNewBadgeImage", "operator_badge_new"),
            ("operatorOwnedBadgeImage", "operator_badge_owned"),
            ("operatorLevelBadgeImage", "operator_badge_level"),
            ("operatorDuplicateTokenBadgeImage", "ui_operator_duplicate_token"),
            ("operatorGridSafeMaskImage", "operator_grid_safe_mask")
        };

        [MenuItem("LASTRAIL/Launch/Validate Gacha Screen Polish")]
        public static void ValidateGachaScreenPolish()
        {
            var failures = new List<string>();
            var outputRoot = Path.Combine(Path.GetTempPath(), OutputDirectoryName);
            Directory.CreateDirectory(outputRoot);

            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    failures.Add("missing_gacha_screen_prefab");
                }
                else
                {
                    ValidatePrefab(prefab, failures);
                }
            }
            catch (Exception ex)
            {
                failures.Add("validation_exception_" + ex.GetType().Name);
                Debug.LogError("LASTRAIL gacha screen polish validation exception: " + ex);
            }

            var status = failures.Count == 0 ? "PASS" : "FAIL";
            var json = "{\"status\":\"" + status + "\",\"screenKey\":\"gacha_screen\",\"skinBindingCount\":" +
                RequiredSkinBindings.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ",\"warningCount\":0,\"failCount\":" + failures.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ",\"blockers\":[" + QuoteJoin(failures) + "]}";
            File.WriteAllText(Path.Combine(outputRoot, "gacha-screen-polish-validation.json"), json);

            if (failures.Count > 0)
            {
                Debug.LogError("LASTRAIL gacha screen polish validation FAILED: " + outputRoot);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("LASTRAIL gacha screen polish validation PASS: " + outputRoot);
            EditorApplication.Exit(0);
        }

        private static void ValidatePrefab(GameObject prefab, ICollection<string> failures)
        {
            var root = prefab.GetComponent<GachaScreenRootBehaviour>();
            if (root == null)
            {
                failures.Add("missing_gacha_root_behaviour");
                return;
            }

            var serialized = new SerializedObject(root);
            foreach (var binding in RequiredSkinBindings)
            {
                var property = serialized.FindProperty(binding.FieldName);
                var image = property == null ? null : property.objectReferenceValue as Image;
                if (image == null)
                {
                    failures.Add("missing_skin_binding_" + binding.FieldName);
                    continue;
                }

                if (image.sprite == null || image.sprite.name != binding.SpriteName)
                {
                    failures.Add("wrong_skin_sprite_" + binding.FieldName);
                }

                if (image.raycastTarget)
                {
                    failures.Add("skin_image_blocks_touch_" + binding.FieldName);
                }
            }

            if (prefab.GetComponentsInChildren<TMP_Text>(true).Length == 0)
            {
                failures.Add("missing_tmp_text");
            }

            if (prefab.GetComponentsInChildren<Text>(true).Length > 0)
            {
                failures.Add("legacy_text_found");
            }

            if (prefab.GetComponentInChildren<StandaloneInputModule>(true) != null)
            {
                failures.Add("standalone_input_module_found");
            }

            if (prefab.GetComponentInChildren<InputSystemUIInputModule>(true) != null)
            {
                failures.Add("screen_prefab_should_not_own_event_system_input_module");
            }
        }

        private static string QuoteJoin(IReadOnlyList<string> values)
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append('"').Append(values[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
            }

            return builder.ToString();
        }
    }
}
#endif
