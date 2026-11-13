using System;
using LastRail.Narrative;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativeStringIdsTests
    {
        [Test]
        public void Build_CreatesStableNarrativeStringIdWithoutChangingMissionId()
        {
            var stringId = MissionNarrativeStringIds.Build("opn_01_last_departure", "briefing", "line_001");

            Assert.That(stringId, Is.EqualTo("narrative.opn_01_last_departure.briefing.line_001"));
        }

        [Test]
        public void Helpers_CreateExpectedSurfaceStringIdsForActOneFixture()
        {
            var definition = ActOneMissionNarrativeFixtures.Opn02FirstSignal();

            Assert.That(
                MissionNarrativeStringIds.ForMissionStart(definition.MissionId),
                Is.EqualTo("narrative.opn_02_first_signal.mission_start.copy"));
            Assert.That(
                MissionNarrativeStringIds.ForLoadingHint(definition.MissionId),
                Is.EqualTo("narrative.opn_02_first_signal.loading.hint"));
            Assert.That(
                MissionNarrativeStringIds.ForBriefingLine(definition.MissionId, 1),
                Is.EqualTo("narrative.opn_02_first_signal.briefing.line_001"));
            Assert.That(
                MissionNarrativeStringIds.ForCallout(definition.MissionId, "opening", 2),
                Is.EqualTo("narrative.opn_02_first_signal.callout.opening_002"));
            Assert.That(
                MissionNarrativeStringIds.ForCodexEntry(definition.MissionId),
                Is.EqualTo("narrative.opn_02_first_signal.codex.entry"));
        }

        [Test]
        public void Build_RejectsEmptyPartsAndUnsafeSeparators()
        {
            Assert.Throws<ArgumentException>(() => MissionNarrativeStringIds.Build("", "briefing", "line_001"));
            Assert.Throws<ArgumentException>(() => MissionNarrativeStringIds.Build("opn_01_last_departure", "", "line_001"));
            Assert.Throws<ArgumentException>(() => MissionNarrativeStringIds.Build("opn_01_last_departure", "briefing", ""));
            Assert.Throws<ArgumentException>(() => MissionNarrativeStringIds.Build("opn.01", "briefing", "line_001"));
            Assert.Throws<ArgumentException>(() => MissionNarrativeStringIds.Build("opn_01_last_departure", "briefing.summary", "line_001"));
            Assert.Throws<ArgumentOutOfRangeException>(() => MissionNarrativeStringIds.ForBriefingLine("opn_01_last_departure", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MissionNarrativeStringIds.ForCallout("opn_01_last_departure", "opening", 0));
        }

        [Test]
        public void Build_DoesNotIntroduceServerTruthRewardEconomyOrSensitiveTerms()
        {
            var combined = string.Join(
                "\n",
                MissionNarrativeStringIds.ForMissionStart("opn_03_water_stop"),
                MissionNarrativeStringIds.ForLoadingHint("opn_03_water_stop"),
                MissionNarrativeStringIds.ForBriefingLine("opn_03_water_stop", 1),
                MissionNarrativeStringIds.ForCallout("opn_03_water_stop", "final_wave", 15),
                MissionNarrativeStringIds.ForRewardFraming("opn_03_water_stop", "first_clear"));

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
        }
    }
}
