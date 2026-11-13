using System;
using NUnit.Framework;
using LastRail.Narrative;

namespace LastRail.Tests.EditMode
{
    public sealed class StoryProgressReadStateTests
    {
        [Test]
        public void FromResponse_PreservesServerReadFieldsForDisplayAndLabelsStoryProgress()
        {
            var dto = Response(
                highestStoryProgressId: "opn_03_water_stop",
                highestClearedOperationId: "opn_03_water_stop",
                order: 3,
                clearedOperationIds: new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop" },
                unlockedOperations: new[] { "opn_04_scrapstar_show" },
                contentStatus: "READY",
                localizationStatus: "LOCALIZED",
                fixtureStatus: "LOCAL_FIXTURE_READY",
                missingContentStatus: "NONE");

            var state = StoryProgressReadState.FromResponse(dto);

            Assert.That(state.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(state.HighestStoryProgressId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.HighestStoryProgressLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.HighestClearedOperationId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.Order, Is.EqualTo(3));
            Assert.That(state.ClearedOperationIds.Count, Is.EqualTo(3));
            Assert.That(state.UnlockedOperations.Count, Is.EqualTo(1));
            Assert.That(state.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
            Assert.That(state.ContentStatus, Is.EqualTo("READY"));
            Assert.That(state.LocalizationStatus, Is.EqualTo("LOCALIZED"));
            Assert.That(state.FixtureStatus, Is.EqualTo("LOCAL_FIXTURE_READY"));
            Assert.That(state.MissingContentStatus, Is.EqualTo("NONE"));
        }

        [Test]
        public void FromResponse_DefaultsMissingProgressToNoneAndKeepsUnknownStatusRaw()
        {
            var dto = Response(
                highestStoryProgressId: null,
                highestClearedOperationId: null,
                order: 0,
                clearedOperationIds: null,
                unlockedOperations: null,
                contentStatus: "FUTURE_CONTENT_STATUS",
                localizationStatus: "FUTURE_LOCALIZATION_STATUS",
                fixtureStatus: "FUTURE_FIXTURE_STATUS",
                missingContentStatus: "FUTURE_MISSING_STATUS");

            var state = StoryProgressReadState.FromResponse(dto);

            Assert.That(state.HighestStoryProgressId, Is.EqualTo("none"));
            Assert.That(state.HighestStoryProgressLabel, Is.EqualTo("스토리 진행 없음"));
            Assert.That(state.HighestClearedOperationId, Is.EqualTo(null));
            Assert.That(state.ClearedOperationIds.Count, Is.EqualTo(0));
            Assert.That(state.UnlockedOperations.Count, Is.EqualTo(0));
            Assert.That(state.ContentStatus, Is.EqualTo("FUTURE_CONTENT_STATUS"));
            Assert.That(state.LocalizationStatus, Is.EqualTo("FUTURE_LOCALIZATION_STATUS"));
            Assert.That(state.FixtureStatus, Is.EqualTo("FUTURE_FIXTURE_STATUS"));
            Assert.That(state.MissingContentStatus, Is.EqualTo("FUTURE_MISSING_STATUS"));
        }

        [Test]
        public void Constructor_DefensivelyCopiesCollectionsAndRejectsEmptyRequiredFields()
        {
            var cleared = new[] { "opn_01_last_departure" };
            var unlocked = new[] { "opn_02_first_signal" };
            var dto = Response(
                highestStoryProgressId: "opn_01_last_departure",
                highestClearedOperationId: "opn_01_last_departure",
                order: 1,
                clearedOperationIds: cleared,
                unlockedOperations: unlocked,
                contentStatus: "READY",
                localizationStatus: "LOCALIZED",
                fixtureStatus: "LOCAL_FIXTURE_READY",
                missingContentStatus: "NONE");
            cleared[0] = "mutated_clear";
            unlocked[0] = "mutated_unlock";

            var state = StoryProgressReadState.FromResponse(dto);

            Assert.That(state.ClearedOperationIds.Count, Is.EqualTo(1));
            Assert.That(state.ClearedOperationIds[0], Is.EqualTo("opn_01_last_departure"));
            Assert.That(state.UnlockedOperations.Count, Is.EqualTo(1));
            Assert.That(state.UnlockedOperations[0], Is.EqualTo("opn_02_first_signal"));
            Assert.Throws<ArgumentException>(() => new StoryProgressReadResponseDto(
                " ",
                "none",
                null,
                0,
                null,
                null,
                "act1.2026-08-27.001",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE"));
            Assert.Throws<ArgumentException>(() => new StoryProgressReadResponseDto(
                "act_01_wasteland_railway",
                "none",
                null,
                0,
                null,
                null,
                " ",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE"));
        }

        [Test]
        public void DebugJson_IncludesStoryProgressContextWithoutTruthOrRewardCalculationFields()
        {
            var state = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_06_broken_line",
                highestClearedOperationId: "opn_06_broken_line",
                order: 6,
                clearedOperationIds: new[] { "opn_05_night_boarders", "opn_06_broken_line" },
                unlockedOperations: new[] { "raid_raider_king_engine" },
                contentStatus: "READY",
                localizationStatus: "LOCALIZED",
                fixtureStatus: "LOCAL_FIXTURE_READY",
                missingContentStatus: "NONE"));

            var json = StoryProgressReadStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"actId\":\"act_01_wasteland_railway\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"highestStoryProgressId\":\"opn_06_broken_line\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"highestStoryProgressLabel\":\"끊어진 선로\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"narrativeVersion\":\"act1.2026-08-27.001\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"unlockedOperations\":[\"raid_raider_king_engine\"]"), Is.EqualTo(true));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("offlineReward"), Is.EqualTo(false));
            Assert.That(json.Contains("offlineStorageCap"), Is.EqualTo(false));
            Assert.That(json.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("firstClearEligible"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
        }

        private static StoryProgressReadResponseDto Response(
            string highestStoryProgressId,
            string highestClearedOperationId,
            int order,
            string[] clearedOperationIds,
            string[] unlockedOperations,
            string contentStatus,
            string localizationStatus,
            string fixtureStatus,
            string missingContentStatus)
        {
            return new StoryProgressReadResponseDto(
                "act_01_wasteland_railway",
                highestStoryProgressId,
                highestClearedOperationId,
                order,
                clearedOperationIds,
                unlockedOperations,
                "act1.2026-08-27.001",
                contentStatus,
                localizationStatus,
                fixtureStatus,
                missingContentStatus);
        }
    }
}
