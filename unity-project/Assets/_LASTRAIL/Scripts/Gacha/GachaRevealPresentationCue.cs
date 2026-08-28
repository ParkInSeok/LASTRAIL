using System;

namespace LastRail.Gacha
{
    public sealed class GachaRevealPresentationCue
    {
        private const string GuaranteeNone = "NONE";
        private const string GuaranteeRareOrHigher10 = "RARE_OR_HIGHER_10";
        private const string GuaranteeElite30 = "ELITE_30";
        private const string GuaranteePickupElite60 = "PICKUP_ELITE_60";

        public GachaRevealPresentationCue(
            string revealStartCueId,
            string rarityFlashCueId,
            string guaranteeBadgeCueId,
            string duplicateConversionCueId,
            string resultCardCueId,
            string soundCueId,
            string vibrationCueId)
        {
            RevealStartCueId = RequireText(revealStartCueId, nameof(revealStartCueId));
            RarityFlashCueId = RequireText(rarityFlashCueId, nameof(rarityFlashCueId));
            GuaranteeBadgeCueId = OptionalText(guaranteeBadgeCueId);
            DuplicateConversionCueId = OptionalText(duplicateConversionCueId);
            ResultCardCueId = RequireText(resultCardCueId, nameof(resultCardCueId));
            SoundCueId = RequireText(soundCueId, nameof(soundCueId));
            VibrationCueId = RequireText(vibrationCueId, nameof(vibrationCueId));
        }

        public string RevealStartCueId { get; }
        public string RarityFlashCueId { get; }
        public string GuaranteeBadgeCueId { get; }
        public string DuplicateConversionCueId { get; }
        public string ResultCardCueId { get; }
        public string SoundCueId { get; }
        public string VibrationCueId { get; }
        public bool HasGuaranteeBadge => !string.IsNullOrEmpty(GuaranteeBadgeCueId);
        public bool HasDuplicateConversionCue => !string.IsNullOrEmpty(DuplicateConversionCueId);
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;

        public static GachaRevealPresentationCue FromResult(GachaPullResultDto result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return Create(
                result.ResultRarity,
                result.ResultType,
                result.IsPickup,
                result.GuaranteeType,
                result.WasDuplicate && result.DuplicateConversion != null);
        }

        public static GachaRevealPresentationCue FromPresentation(GachaPullResultPresentation result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return Create(
                result.Rarity,
                result.ResultKind,
                false,
                result.GuaranteeType,
                result.HasConversionReward);
        }

        private static GachaRevealPresentationCue Create(
            GachaRarity rarity,
            GachaPullResultKind resultKind,
            bool isPickup,
            string guaranteeType,
            bool hasDuplicateConversion)
        {
            return new GachaRevealPresentationCue(
                "gacha_reveal_start",
                MapRarityFlashCueId(rarity),
                MapGuaranteeBadgeCueId(guaranteeType),
                hasDuplicateConversion ? "gacha_duplicate_conversion" : null,
                MapResultCardCueId(resultKind),
                MapSoundCueId(rarity, isPickup, hasDuplicateConversion),
                MapVibrationCueId(rarity));
        }

        private static string MapRarityFlashCueId(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common:
                    return "gacha_flash_common";
                case GachaRarity.Rare:
                    return "gacha_flash_rare";
                case GachaRarity.Elite:
                    return "gacha_flash_elite";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unsupported gacha rarity.");
            }
        }

        private static string MapGuaranteeBadgeCueId(string guaranteeType)
        {
            switch (OptionalText(guaranteeType))
            {
                case null:
                case GuaranteeNone:
                    return null;
                case GuaranteeRareOrHigher10:
                    return "gacha_badge_rare_or_higher_10";
                case GuaranteeElite30:
                    return "gacha_badge_elite_30";
                case GuaranteePickupElite60:
                    return "gacha_badge_pickup_elite_60";
                default:
                    return "gacha_badge_unknown";
            }
        }

        private static string MapResultCardCueId(GachaPullResultKind resultKind)
        {
            switch (resultKind)
            {
                case GachaPullResultKind.Operator:
                    return "gacha_card_operator";
                case GachaPullResultKind.OperatorToken:
                    return "gacha_card_operator_token";
                case GachaPullResultKind.Item:
                    return "gacha_card_item";
                case GachaPullResultKind.Currency:
                    return "gacha_card_currency";
                default:
                    throw new ArgumentOutOfRangeException(nameof(resultKind), resultKind, "Unsupported gacha result kind.");
            }
        }

        private static string MapSoundCueId(GachaRarity rarity, bool isPickup, bool hasDuplicateConversion)
        {
            if (hasDuplicateConversion)
            {
                return "sfx_gacha_duplicate_conversion";
            }

            if (isPickup && rarity == GachaRarity.Elite)
            {
                return "sfx_gacha_pickup_elite";
            }

            switch (rarity)
            {
                case GachaRarity.Common:
                    return "sfx_gacha_common";
                case GachaRarity.Rare:
                    return "sfx_gacha_rare";
                case GachaRarity.Elite:
                    return "sfx_gacha_elite";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unsupported gacha rarity.");
            }
        }

        private static string MapVibrationCueId(GachaRarity rarity)
        {
            switch (rarity)
            {
                case GachaRarity.Common:
                    return "vibration_gacha_light";
                case GachaRarity.Rare:
                    return "vibration_gacha_medium";
                case GachaRarity.Elite:
                    return "vibration_gacha_heavy";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "Unsupported gacha rarity.");
            }
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value;
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
