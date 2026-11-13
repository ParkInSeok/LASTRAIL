using System;
using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaRevealPresentationCueTests
    {
        [Test]
        public void FromResult_MapsElitePickupGuaranteeToLaunchCueIds()
        {
            var result = Result(
                GachaRarity.Elite,
                GachaPullResultKind.Operator,
                isPickup: true,
                guaranteeType: "PICKUP_ELITE_60",
                wasDuplicate: false,
                duplicateConversion: null);

            var cue = GachaRevealPresentationCue.FromResult(result);

            Assert.That(cue.RevealStartCueId, Is.EqualTo("gacha_reveal_start"));
            Assert.That(cue.RarityFlashCueId, Is.EqualTo("gacha_flash_elite"));
            Assert.That(cue.GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_pickup_elite_60"));
            Assert.That(cue.DuplicateConversionCueId, Is.EqualTo(null));
            Assert.That(cue.ResultCardCueId, Is.EqualTo("gacha_card_operator"));
            Assert.That(cue.SoundCueId, Is.EqualTo("sfx_gacha_pickup_elite"));
            Assert.That(cue.VibrationCueId, Is.EqualTo("vibration_gacha_heavy"));
            Assert.That(cue.HasGuaranteeBadge, Is.EqualTo(true));
            Assert.That(cue.HasDuplicateConversionCue, Is.EqualTo(false));
            Assert.That(cue.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(cue.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void FromResult_MapsDuplicateConversionCueWithoutClientEconomyMutation()
        {
            var result = Result(
                GachaRarity.Rare,
                GachaPullResultKind.OperatorToken,
                isPickup: false,
                guaranteeType: "RARE_OR_HIGHER_10",
                wasDuplicate: true,
                duplicateConversion: new GachaDuplicateConversionDto("op_vex", "cur_operator_token", 50));

            var cue = GachaRevealPresentationCue.FromResult(result);

            Assert.That(cue.RarityFlashCueId, Is.EqualTo("gacha_flash_rare"));
            Assert.That(cue.GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_rare_or_higher_10"));
            Assert.That(cue.DuplicateConversionCueId, Is.EqualTo("gacha_duplicate_conversion"));
            Assert.That(cue.ResultCardCueId, Is.EqualTo("gacha_card_operator_token"));
            Assert.That(cue.SoundCueId, Is.EqualTo("sfx_gacha_duplicate_conversion"));
            Assert.That(cue.VibrationCueId, Is.EqualTo("vibration_gacha_medium"));
            Assert.That(cue.HasDuplicateConversionCue, Is.EqualTo(true));
            Assert.That(cue.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(cue.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void FromResult_MapsSupportedGuaranteeTypes()
        {
            Assert.That(GachaRevealPresentationCue.FromResult(Result(guaranteeType: null)).GuaranteeBadgeCueId, Is.EqualTo(null));
            Assert.That(GachaRevealPresentationCue.FromResult(Result(guaranteeType: "NONE")).GuaranteeBadgeCueId, Is.EqualTo(null));
            Assert.That(GachaRevealPresentationCue.FromResult(Result(guaranteeType: "RARE_OR_HIGHER_10")).GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_rare_or_higher_10"));
            Assert.That(GachaRevealPresentationCue.FromResult(Result(guaranteeType: "ELITE_30")).GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_elite_30"));
            Assert.That(GachaRevealPresentationCue.FromResult(Result(guaranteeType: "PICKUP_ELITE_60")).GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_pickup_elite_60"));
        }

        [Test]
        public void FromPresentation_UsesPresentationFieldsWithoutPickupAssumption()
        {
            var presentation = new GachaPullResultPresentation(
                "op_iris",
                1,
                GachaPullResultKind.Operator,
                GachaRarity.Elite,
                "op_iris",
                "아이리스",
                false,
                null,
                null,
                0,
                "ELITE_30");

            var cue = GachaRevealPresentationCue.FromPresentation(presentation);

            Assert.That(cue.GuaranteeBadgeCueId, Is.EqualTo("gacha_badge_elite_30"));
            Assert.That(cue.SoundCueId, Is.EqualTo("sfx_gacha_elite"));
            Assert.That(cue.ResultCardCueId, Is.EqualTo("gacha_card_operator"));
            Assert.That(cue.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(cue.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesStableCueFieldsWithoutSensitiveOrCalculationPayload()
        {
            var cue = GachaRevealPresentationCue.FromResult(Result(
                GachaRarity.Elite,
                GachaPullResultKind.Operator,
                isPickup: true,
                guaranteeType: "PICKUP_ELITE_60",
                wasDuplicate: false,
                duplicateConversion: null));

            var json = GachaRevealPresentationCueJsonAdapter.ToJson(cue);

            Assert.That(json, Is.EqualTo(
                "{\"revealStartCueId\":\"gacha_reveal_start\"," +
                "\"rarityFlashCueId\":\"gacha_flash_elite\"," +
                "\"guaranteeBadgeCueId\":\"gacha_badge_pickup_elite_60\"," +
                "\"duplicateConversionCueId\":null," +
                "\"resultCardCueId\":\"gacha_card_operator\"," +
                "\"soundCueId\":\"sfx_gacha_pickup_elite\"," +
                "\"vibrationCueId\":\"vibration_gacha_heavy\"," +
                "\"hasGuaranteeBadge\":true," +
                "\"hasDuplicateConversionCue\":false," +
                "\"hasEconomyMutation\":false," +
                "\"hasClientSideRng\":false}"));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("fullWeights"), Is.EqualTo(false));
            Assert.That(json.Contains("rngSeed"), Is.EqualTo(false));
            Assert.That(json.Contains("seedPreimage"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
        }

        [Test]
        public void FromResult_RejectsNullResult()
        {
            Assert.Throws<ArgumentNullException>(() => GachaRevealPresentationCue.FromResult(null));
            Assert.Throws<ArgumentNullException>(() => GachaRevealPresentationCue.FromPresentation(null));
            Assert.Throws<ArgumentNullException>(() => GachaRevealPresentationCueJsonAdapter.ToJson(null));
        }

        private static GachaPullResultDto Result(
            GachaRarity rarity = GachaRarity.Common,
            GachaPullResultKind resultType = GachaPullResultKind.Item,
            bool isPickup = false,
            string guaranteeType = "NONE",
            bool wasDuplicate = false,
            GachaDuplicateConversionDto duplicateConversion = null)
        {
            return new GachaPullResultDto(
                1,
                rarity,
                resultType,
                resultType == GachaPullResultKind.Operator ? "op_vex" : "item_scrap",
                new GachaRewardDto(resultType.ToString().ToUpperInvariant(), resultType == GachaPullResultKind.Operator ? "op_vex" : "item_scrap", 1),
                isPickup,
                guaranteeType,
                wasDuplicate,
                duplicateConversion);
        }
    }
}
