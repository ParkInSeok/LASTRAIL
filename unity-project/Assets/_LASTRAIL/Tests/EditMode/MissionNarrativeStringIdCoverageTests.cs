using System.Linq;
using LastRail.Narrative;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativeStringIdCoverageTests
    {
        [Test]
        public void FromDefinition_CreatesStringIdEntriesForEveryNarrativeSurface()
        {
            var coverage = MissionNarrativeStringIdCoverage.FromDefinition(
                ActOneMissionNarrativeFixtures.Opn01LastDeparture());

            Assert.That(coverage.MissionId, Is.EqualTo("opn_01_last_departure"));
            Assert.That(coverage.DisplayName, Is.EqualTo("마지막 출발"));
            Assert.That(coverage.EntryCount, Is.EqualTo(20));
            Assert.That(coverage.MissingRequiredSurfaceCount, Is.EqualTo(0));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.mission_start.copy"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.loading.hint"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.briefing.line_002"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.commander_response.line_001"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.callout.opening_002"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.callout.tutorial_002"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.callout.escalation_001"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.callout.optional_objective_001"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.callout.final_wave_001"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.result.title"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.result.summary"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.result.victory_001"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.result.failure_001"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.reward_framing.first_clear"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.reward_framing.repeat_clear"), Is.EqualTo(true));
            Assert.That(coverage.HasStringId("narrative.opn_01_last_departure.codex.entry"), Is.EqualTo(true));
        }

        [Test]
        public void CreateActOne_CoversAllFixtureMissionsAndKeepsStringIdsUnique()
        {
            var map = MissionNarrativeStringIdCoverageMap.CreateActOne();

            Assert.That(map.MissionCount, Is.EqualTo(7));
            Assert.That(map.TotalMissingRequiredSurfaceCount, Is.EqualTo(0));
            Assert.That(map.FindByMissionId("opn_04_scrapstar_show").DisplayName, Is.EqualTo("스크랩스타 쇼"));
            Assert.That(map.FindByMissionId("raid_raider_king_engine").HasStringId("narrative.raid_raider_king_engine.mission_start.copy"), Is.EqualTo(true));
            Assert.That(map.FindByMissionId("unknown"), Is.EqualTo(null));
            Assert.That(map.AllStringIds.Count, Is.EqualTo(map.AllStringIds.Distinct().Count()));
        }

        [Test]
        public void Entry_MetadataIsIdentifierOnlyAndDoesNotContainNarrativeText()
        {
            var coverage = MissionNarrativeStringIdCoverage.FromDefinition(
                ActOneMissionNarrativeFixtures.Opn03WaterStop());
            var entry = coverage.Entries[0];

            Assert.That(entry.MissionId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(entry.Surface, Is.EqualTo("mission_start"));
            Assert.That(entry.Slot, Is.EqualTo("copy"));
            Assert.That(entry.StringId, Is.EqualTo("narrative.opn_03_water_stop.mission_start.copy"));
            Assert.That(entry.HasLocalizedTextPayload, Is.EqualTo(false));
        }

        [Test]
        public void CoverageMap_DoesNotIntroduceServerTruthRewardEconomyOrSensitiveTerms()
        {
            var combined = MissionNarrativeStringIdCoverageMap.CreateActOne().FlattenStringIdsForValidation();

            Assert.That(combined.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(combined.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(combined.Contains("offlineReward"), Is.EqualTo(false));
            Assert.That(combined.Contains("firstClearEligible"), Is.EqualTo(false));
            Assert.That(combined.Contains("unlockTruth"), Is.EqualTo(false));
            Assert.That(combined.Contains("accountId"), Is.EqualTo(false));
            Assert.That(combined.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(combined.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(combined.Contains("secret"), Is.EqualTo(false));
            Assert.That(combined.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(combined.Contains("server"), Is.EqualTo(false));
            Assert.That(combined.Contains("endpoint"), Is.EqualTo(false));
        }
    }
}
