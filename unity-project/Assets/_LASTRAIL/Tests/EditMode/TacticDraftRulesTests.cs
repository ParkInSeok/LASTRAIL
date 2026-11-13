using System;
using NUnit.Framework;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class TacticDraftRulesTests
    {
        [Test]
        public void IsCommandTacticDraftWave_ReturnsTrueOnlyAtThreeWaveIntervalsThroughFifteen()
        {
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(1), Is.EqualTo(false));
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(2), Is.EqualTo(false));
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(3), Is.EqualTo(true));
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(6), Is.EqualTo(true));
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(9), Is.EqualTo(true));
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(12), Is.EqualTo(true));
            Assert.That(TacticDraftRules.IsCommandTacticDraftWave(15), Is.EqualTo(true));
        }

        [Test]
        public void IsCommandTacticDraftWave_RejectsOutOfRangeWaveNumbers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TacticDraftRules.IsCommandTacticDraftWave(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TacticDraftRules.IsCommandTacticDraftWave(99));
        }

        [Test]
        public void RefreshState_AllowsOneFreeAndOneAdRefreshPerSelection()
        {
            var state = TacticDraftRefreshState.CreateForSelection(waveNumber: 6);

            Assert.That(state.WaveNumber, Is.EqualTo(6));
            Assert.That(state.HasUsedFreeRefresh, Is.EqualTo(false));
            Assert.That(state.HasUsedAdRefresh, Is.EqualTo(false));

            state = state.UseFreeRefresh();
            state = state.UseAdRefresh();

            Assert.That(state.HasUsedFreeRefresh, Is.EqualTo(true));
            Assert.That(state.HasUsedAdRefresh, Is.EqualTo(true));
            Assert.That(state.CanUseFreeRefresh, Is.EqualTo(false));
            Assert.That(state.CanUseAdRefresh, Is.EqualTo(false));
        }

        [Test]
        public void RefreshState_RejectsSecondFreeOrAdRefresh()
        {
            var freeUsed = TacticDraftRefreshState.CreateForSelection(3).UseFreeRefresh();
            var adUsed = TacticDraftRefreshState.CreateForSelection(3).UseAdRefresh();

            Assert.Throws<InvalidOperationException>(() => freeUsed.UseFreeRefresh());
            Assert.Throws<InvalidOperationException>(() => adUsed.UseAdRefresh());
        }

        [Test]
        public void RefreshState_RejectsNonDraftWaveSelection()
        {
            Assert.Throws<InvalidOperationException>(() => TacticDraftRefreshState.CreateForSelection(4));
        }

        [Test]
        public void UltimateTacticAppearance_UsesFixedTenPercentChanceWhenConditionIsMet()
        {
            Assert.That(UltimateTacticAppearanceRules.AppearanceChancePercent, Is.EqualTo(10));
            Assert.That(UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(conditionMet: true, rollPercent: 0), Is.EqualTo(true));
            Assert.That(UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(conditionMet: true, rollPercent: 9), Is.EqualTo(true));
            Assert.That(UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(conditionMet: true, rollPercent: 10), Is.EqualTo(false));
            Assert.That(UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(conditionMet: true, rollPercent: 99), Is.EqualTo(false));
        }

        [Test]
        public void UltimateTacticAppearance_NeverOffersUltimateWhenConditionIsNotMet()
        {
            Assert.That(UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(conditionMet: false, rollPercent: 0), Is.EqualTo(false));
        }

        [Test]
        public void UltimateTacticAppearance_RejectsRollOutsidePercentRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(true, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => UltimateTacticAppearanceRules.ShouldOfferUltimateTactic(true, 100));
        }
    }
}

