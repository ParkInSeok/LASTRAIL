using System;
using System.Collections.Generic;

namespace LastRail.Presentation.Animation
{
    public enum AnimationAssetRole
    {
        Operator = 1,
        Enemy = 2,
        RaidBoss = 3,
        GachaReveal = 4
    }

    public sealed class AnimationAssetManifestEntry
    {
        public AnimationAssetManifestEntry(
            string assetId,
            AnimationAssetRole role,
            string portraitAssetId,
            string iconAssetId,
            string combatSpriteOrRigKey,
            IEnumerable<string> animationKeys)
        {
            AssetId = RequireText(assetId, nameof(assetId));
            Role = role;
            PortraitAssetId = RequireText(portraitAssetId, nameof(portraitAssetId));
            IconAssetId = RequireText(iconAssetId, nameof(iconAssetId));
            CombatSpriteOrRigKey = RequireText(combatSpriteOrRigKey, nameof(combatSpriteOrRigKey));
            AnimationKeys = CopyKeys(animationKeys);
        }

        public string AssetId { get; }
        public AnimationAssetRole Role { get; }
        public string PortraitAssetId { get; }
        public string IconAssetId { get; }
        public string CombatSpriteOrRigKey { get; }
        public IReadOnlyList<string> AnimationKeys { get; }

        public bool HasAnimationKey(string animationKey)
        {
            for (var i = 0; i < AnimationKeys.Count; i++)
            {
                if (string.Equals(AnimationKeys[i], animationKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<string> CopyKeys(IEnumerable<string> animationKeys)
        {
            if (animationKeys == null)
            {
                throw new ArgumentNullException(nameof(animationKeys));
            }

            var copied = new List<string>();
            foreach (var key in animationKeys)
            {
                copied.Add(RequireText(key, nameof(animationKeys)));
            }

            if (copied.Count == 0)
            {
                throw new ArgumentException("At least one animation key is required.", nameof(animationKeys));
            }

            return copied.AsReadOnly();
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

    public sealed class AnimationAssetManifest
    {
        public AnimationAssetManifest(IEnumerable<AnimationAssetManifestEntry> entries)
        {
            Entries = CopyEntries(entries);
        }

        public IReadOnlyList<AnimationAssetManifestEntry> Entries { get; }

        public AnimationAssetManifestEntry Find(string assetId, AnimationAssetRole role)
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Role == role && string.Equals(Entries[i].AssetId, assetId, StringComparison.Ordinal))
                {
                    return Entries[i];
                }
            }

            return null;
        }

        private static IReadOnlyList<AnimationAssetManifestEntry> CopyEntries(IEnumerable<AnimationAssetManifestEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var copied = new List<AnimationAssetManifestEntry>();
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Manifest entry cannot be null.", nameof(entries));
                }

                copied.Add(entry);
            }

            return copied.AsReadOnly();
        }
    }
}
