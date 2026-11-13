using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Animation
{
    public sealed class AnimationAssetCoverageAuditResult
    {
        public AnimationAssetCoverageAuditResult(
            int operatorCount,
            int initialOperatorCount,
            int gachaEliteOperatorCount,
            int enemyCount,
            int raidBossCount,
            IEnumerable<AnimationAssetMissingRequirement> missingRequirements)
        {
            OperatorCount = operatorCount;
            InitialOperatorCount = initialOperatorCount;
            GachaEliteOperatorCount = gachaEliteOperatorCount;
            EnemyCount = enemyCount;
            RaidBossCount = raidBossCount;
            MissingRequirements = CopyMissing(missingRequirements);
        }

        public int OperatorCount { get; }
        public int InitialOperatorCount { get; }
        public int GachaEliteOperatorCount { get; }
        public int EnemyCount { get; }
        public int RaidBossCount { get; }
        public IReadOnlyList<AnimationAssetMissingRequirement> MissingRequirements { get; }
        public int MissingRequirementCount => MissingRequirements.Count;
        public bool IsComplete => MissingRequirements.Count == 0;
        public bool HasRawArtPayload => false;

        private static IReadOnlyList<AnimationAssetMissingRequirement> CopyMissing(IEnumerable<AnimationAssetMissingRequirement> missingRequirements)
        {
            if (missingRequirements == null)
            {
                throw new ArgumentNullException(nameof(missingRequirements));
            }

            var copied = new List<AnimationAssetMissingRequirement>();
            foreach (var requirement in missingRequirements)
            {
                copied.Add(requirement);
            }

            return copied.AsReadOnly();
        }
    }

    public readonly struct AnimationAssetMissingRequirement
    {
        public AnimationAssetMissingRequirement(string assetId, AnimationAssetRole role, string missingKey)
        {
            AssetId = RequireText(assetId, nameof(assetId));
            Role = role;
            MissingKey = RequireText(missingKey, nameof(missingKey));
        }

        public string AssetId { get; }
        public AnimationAssetRole Role { get; }
        public string MissingKey { get; }

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
