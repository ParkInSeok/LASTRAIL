using System;

namespace LastRail.Gacha
{
    public sealed class GachaScreenRevealEntryPresentation
    {
        public GachaScreenRevealEntryPresentation(
            GachaPullResultPresentation result,
            GachaRevealPresentationCue cue)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Cue = cue ?? throw new ArgumentNullException(nameof(cue));
        }

        public GachaPullResultPresentation Result { get; }
        public GachaRevealPresentationCue Cue { get; }
        public string ResultId => Result.ResultId;
        public int PullIndex => Result.PullIndex;
        public GachaPullResultKind ResultKind => Result.ResultKind;
        public GachaRarity Rarity => Result.Rarity;
        public string RewardId => Result.RewardId;
        public string DisplayName => Result.DisplayName;
        public bool IsDuplicateConversion => Result.IsDuplicateConversion;
        public string DuplicateSourceId => Result.DuplicateSourceId;
        public string ConversionItemId => Result.ConversionItemId;
        public int ConversionQuantity => Result.ConversionQuantity;
        public string GuaranteeType => Result.GuaranteeType;
        public string RevealStartCueId => Cue.RevealStartCueId;
        public string RarityFlashCueId => Cue.RarityFlashCueId;
        public string GuaranteeBadgeCueId => Cue.GuaranteeBadgeCueId;
        public string DuplicateConversionCueId => Cue.DuplicateConversionCueId;
        public string ResultCardCueId => Cue.ResultCardCueId;
        public string SoundCueId => Cue.SoundCueId;
        public string VibrationCueId => Cue.VibrationCueId;
        public bool HasGuaranteeBadge => Cue.HasGuaranteeBadge;
        public bool HasDuplicateConversionCue => Cue.HasDuplicateConversionCue;
        public bool HasEconomyMutation => false;
        public bool HasClientSideRng => false;
    }
}
