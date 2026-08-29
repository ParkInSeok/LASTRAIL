using System;

namespace LastRail.Presentation.Animation
{
    public sealed class AnimationAssetPipelineSpec
    {
        private AnimationAssetPipelineSpec(
            string toolchainName,
            string requiredUnityPackageId,
            int primaryAtlasMaxSize,
            int lowEndFallbackAtlasSize,
            string operatorCombatAtlasKey,
            string operatorPortraitIconAtlasKey,
            string enemyCombatAtlasKey,
            string raidBossCombatAtlasKey,
            string vfxAtlasKey,
            AnimationVfxBudgetSpec vfxBudget)
        {
            ToolchainName = RequireText(toolchainName, nameof(toolchainName));
            RequiredUnityPackageId = RequireText(requiredUnityPackageId, nameof(requiredUnityPackageId));
            PrimaryAtlasMaxSize = RequirePositive(primaryAtlasMaxSize, nameof(primaryAtlasMaxSize));
            LowEndFallbackAtlasSize = RequirePositive(lowEndFallbackAtlasSize, nameof(lowEndFallbackAtlasSize));
            OperatorCombatAtlasKey = RequireText(operatorCombatAtlasKey, nameof(operatorCombatAtlasKey));
            OperatorPortraitIconAtlasKey = RequireText(operatorPortraitIconAtlasKey, nameof(operatorPortraitIconAtlasKey));
            EnemyCombatAtlasKey = RequireText(enemyCombatAtlasKey, nameof(enemyCombatAtlasKey));
            RaidBossCombatAtlasKey = RequireText(raidBossCombatAtlasKey, nameof(raidBossCombatAtlasKey));
            VfxAtlasKey = RequireText(vfxAtlasKey, nameof(vfxAtlasKey));
            VfxBudget = vfxBudget ?? throw new ArgumentNullException(nameof(vfxBudget));
        }

        public string ToolchainName { get; }
        public string RequiredUnityPackageId { get; }
        public int PrimaryAtlasMaxSize { get; }
        public int LowEndFallbackAtlasSize { get; }
        public string OperatorCombatAtlasKey { get; }
        public string OperatorPortraitIconAtlasKey { get; }
        public string EnemyCombatAtlasKey { get; }
        public string RaidBossCombatAtlasKey { get; }
        public string VfxAtlasKey { get; }
        public AnimationVfxBudgetSpec VfxBudget { get; }
        public bool UsesUnity2DAnimationPackage => true;
        public bool UsesAnimatorControllers => true;
        public bool UsesAnimationClips => true;
        public bool UsesSpriteAtlas => true;
        public bool UsesSpriteSheetVfx => true;
        public bool UsesSeparateUiPortraitCombatVfxAtlases => true;

        public static AnimationAssetPipelineSpec ActOneLaunchRecommended()
        {
            return new AnimationAssetPipelineSpec(
                "Unity 2D Animation package + Animator/Animation Clip + Sprite Atlas + sprite-sheet VFX",
                "com.unity.2d.animation",
                primaryAtlasMaxSize: 2048,
                lowEndFallbackAtlasSize: 1024,
                operatorCombatAtlasKey: "atlas_operator_combat",
                operatorPortraitIconAtlasKey: "atlas_operator_portrait_icon",
                enemyCombatAtlasKey: "atlas_enemy_combat",
                raidBossCombatAtlasKey: "atlas_raid_boss_combat",
                vfxAtlasKey: "atlas_sprite_sheet_vfx",
                vfxBudget: AnimationVfxBudgetSpec.ActOneLaunchRecommended());
        }

        private static int RequirePositive(int value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name, "Value must be positive.");
            }

            return value;
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }
    }
}
