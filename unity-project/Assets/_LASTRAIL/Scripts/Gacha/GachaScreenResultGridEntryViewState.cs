using System;
using LastRail.Localization;

namespace LastRail.Gacha
{
    public sealed class GachaScreenResultGridEntryViewState
    {
        public GachaScreenResultGridEntryViewState(GachaScreenRevealEntryPresentation entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            ResultId = RequireText(entry.ResultId, nameof(entry.ResultId));
            PullIndex = entry.PullIndex;
            DisplayName = RequireText(entry.DisplayName, nameof(entry.DisplayName));
            RewardId = RequireText(entry.RewardId, nameof(entry.RewardId));
            ResultKindLabel = UnityDisplayLabels.ForGachaPullResultKind(entry.ResultKind);
            RarityLabel = UnityDisplayLabels.ForGachaRarity(entry.Rarity);
            GuaranteeType = OptionalText(entry.GuaranteeType);
            GuaranteeBadgeLabel = entry.HasGuaranteeBadge
                ? UnityDisplayLabels.ForGachaGuaranteeType(entry.GuaranteeType)
                : null;
            GuaranteeBadgeCueId = OptionalText(entry.GuaranteeBadgeCueId);
            DuplicateConversionLabel = entry.HasDuplicateConversionCue
                ? "중복 전환: " + entry.ConversionItemId + " x" + entry.ConversionQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null;
            DuplicateConversionCueId = OptionalText(entry.DuplicateConversionCueId);
            ResultCardCueId = RequireText(entry.ResultCardCueId, nameof(entry.ResultCardCueId));
            RarityFlashCueId = RequireText(entry.RarityFlashCueId, nameof(entry.RarityFlashCueId));
            SoundCueId = RequireText(entry.SoundCueId, nameof(entry.SoundCueId));
            VibrationCueId = RequireText(entry.VibrationCueId, nameof(entry.VibrationCueId));
        }

        public string ResultId { get; }
        public int PullIndex { get; }
        public string DisplayName { get; }
        public string RewardId { get; }
        public string ResultKindLabel { get; }
        public string RarityLabel { get; }
        public string GuaranteeType { get; }
        public string GuaranteeBadgeLabel { get; }
        public string GuaranteeBadgeCueId { get; }
        public string DuplicateConversionLabel { get; }
        public string DuplicateConversionCueId { get; }
        public string ResultCardCueId { get; }
        public string RarityFlashCueId { get; }
        public string SoundCueId { get; }
        public string VibrationCueId { get; }
        public bool IsPresentationOnly => true;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
