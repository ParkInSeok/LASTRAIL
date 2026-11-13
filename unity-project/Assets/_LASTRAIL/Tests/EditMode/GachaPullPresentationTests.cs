using NUnit.Framework;
using LastRail.Gacha;

namespace LastRail.Tests.EditMode
{
    public sealed class GachaPullPresentationTests
    {
        [Test]
        public void PullCount_AllowsOnlyOneOrTen()
        {
            Assert.That(GachaPullCount.Single.Value, Is.EqualTo(1));
            Assert.That(GachaPullCount.Ten.Value, Is.EqualTo(10));
            Assert.Throws<System.ArgumentException>(() => GachaPullCount.FromValue(0));
            Assert.Throws<System.ArgumentException>(() => GachaPullCount.FromValue(2));
            Assert.Throws<System.ArgumentException>(() => GachaPullCount.FromValue(11));
        }

        [Test]
        public void PullResultPresentation_StoresServerResultForDisplayWithoutGrantLogic()
        {
            var result = new GachaPullResultPresentation(
                "result_001",
                pullIndex: 1,
                GachaPullResultKind.Operator,
                GachaRarity.Elite,
                "op_vex",
                "벡스",
                isDuplicateConversion: false,
                duplicateSourceId: null,
                conversionItemId: null,
                conversionQuantity: 0);

            Assert.That(result.ResultId, Is.EqualTo("result_001"));
            Assert.That(result.PullIndex, Is.EqualTo(1));
            Assert.That(result.ResultKind, Is.EqualTo(GachaPullResultKind.Operator));
            Assert.That(result.Rarity, Is.EqualTo(GachaRarity.Elite));
            Assert.That(result.RewardId, Is.EqualTo("op_vex"));
            Assert.That(result.DisplayName, Is.EqualTo("벡스"));
            Assert.That(result.IsDuplicateConversion, Is.EqualTo(false));
            Assert.That(result.HasConversionReward, Is.EqualTo(false));
        }

        [Test]
        public void PullResultPresentation_StoresDuplicateConversionForDisplay()
        {
            var result = new GachaPullResultPresentation(
                "result_002",
                pullIndex: 2,
                GachaPullResultKind.OperatorToken,
                GachaRarity.Elite,
                "cur_operator_token",
                "요원 증표",
                isDuplicateConversion: true,
                duplicateSourceId: "op_vex",
                conversionItemId: "cur_operator_token",
                conversionQuantity: 30);

            Assert.That(result.IsDuplicateConversion, Is.EqualTo(true));
            Assert.That(result.DuplicateSourceId, Is.EqualTo("op_vex"));
            Assert.That(result.ConversionItemId, Is.EqualTo("cur_operator_token"));
            Assert.That(result.ConversionQuantity, Is.EqualTo(30));
            Assert.That(result.HasConversionReward, Is.EqualTo(true));
        }

        [Test]
        public void PullBatchPresentation_CopiesResultsAndRequiresCountToMatch()
        {
            var results = new[]
            {
                new GachaPullResultPresentation("result_001", 1, GachaPullResultKind.Item, GachaRarity.Rare, "item_fuel_can_s", "소형 연료 캔", false, null, null, 0)
            };
            var batch = new GachaPullBatchPresentation("batch_001", "banner_act1_elite_001", GachaPullCount.Single, results);

            results[0] = new GachaPullResultPresentation("result_002", 1, GachaPullResultKind.Item, GachaRarity.Common, "cur_machine_parts", "기계 부품", false, null, null, 0);

            Assert.That(batch.BatchId, Is.EqualTo("batch_001"));
            Assert.That(batch.BannerId, Is.EqualTo("banner_act1_elite_001"));
            Assert.That(batch.PullCount.Value, Is.EqualTo(1));
            Assert.That(batch.Results.Count, Is.EqualTo(1));
            Assert.That(batch.Results[0].ResultId, Is.EqualTo("result_001"));
            Assert.Throws<System.ArgumentException>(() => new GachaPullBatchPresentation("batch_bad", "banner", GachaPullCount.Ten, batch.Results));
        }
    }
}
