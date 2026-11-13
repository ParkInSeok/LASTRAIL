using System;
using System.Linq;
using LastRail.Narrative;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativeLocalizationAuditTests
    {
        [Test]
        public void Audit_ReturnsCompleteWhenEveryCoverageStringIdExists()
        {
            var coverageMap = MissionNarrativeStringIdCoverageMap.CreateActOne();
            var audit = MissionNarrativeLocalizationAudit.FromCoverageMap(
                coverageMap,
                coverageMap.AllStringIds);

            Assert.That(audit.MissionCount, Is.EqualTo(7));
            Assert.That(audit.RequiredStringIdCount, Is.EqualTo(coverageMap.AllStringIds.Count));
            Assert.That(audit.AvailableStringIdCount, Is.EqualTo(coverageMap.AllStringIds.Count));
            Assert.That(audit.MissingStringIdCount, Is.EqualTo(0));
            Assert.That(audit.Status, Is.EqualTo(MissionNarrativeLocalizationStatus.Complete));
            Assert.That(audit.HasLocalizedTextPayload, Is.EqualTo(false));
        }

        [Test]
        public void Audit_ReportsMissingStringIdsByMissionWithoutCarryingLocalizedText()
        {
            var coverageMap = MissionNarrativeStringIdCoverageMap.CreateActOne();
            var available = new[]
            {
                "narrative.opn_01_last_departure.mission_start.copy",
                "narrative.opn_01_last_departure.loading.hint",
                "narrative.raid_raider_king_engine.mission_start.copy"
            };

            var audit = MissionNarrativeLocalizationAudit.FromCoverageMap(coverageMap, available);
            var opn01 = audit.FindMission("opn_01_last_departure");
            var raid = audit.FindMission("raid_raider_king_engine");

            Assert.That(audit.Status, Is.EqualTo(MissionNarrativeLocalizationStatus.Missing));
            Assert.That(audit.MissingStringIdCount > 0, Is.EqualTo(true));
            Assert.That(opn01.Status, Is.EqualTo(MissionNarrativeLocalizationStatus.Missing));
            Assert.That(opn01.MissingStringIds.Contains("narrative.opn_01_last_departure.briefing.line_001"), Is.EqualTo(true));
            Assert.That(opn01.HasLocalizedTextPayload, Is.EqualTo(false));
            Assert.That(raid.Status, Is.EqualTo(MissionNarrativeLocalizationStatus.Missing));
            Assert.That(raid.AvailableStringIdCount, Is.EqualTo(1));
        }

        [Test]
        public void Audit_NormalizesAvailableIdsAndIgnoresUnknownExtraIds()
        {
            var coverage = MissionNarrativeStringIdCoverage.FromDefinition(
                ActOneMissionNarrativeFixtures.Opn02FirstSignal());
            var available = new[]
            {
                " narrative.opn_02_first_signal.mission_start.copy ",
                "narrative.opn_02_first_signal.loading.hint",
                "narrative.unknown.extra.key"
            };

            var audit = MissionNarrativeLocalizationAudit.FromCoverage(coverage, available);

            Assert.That(audit.MissionCount, Is.EqualTo(1));
            Assert.That(audit.AvailableStringIdCount, Is.EqualTo(2));
            Assert.That(audit.UnknownAvailableStringIdCount, Is.EqualTo(1));
            Assert.That(audit.FindMission("opn_02_first_signal").AvailableStringIdCount, Is.EqualTo(2));
            Assert.That(audit.FindMission("unknown"), Is.EqualTo(null));
        }

        [Test]
        public void Audit_RejectsNullCoverageAndNullAvailableCollections()
        {
            Assert.Throws<ArgumentNullException>(() => MissionNarrativeLocalizationAudit.FromCoverageMap(null, new string[0]));
            Assert.Throws<ArgumentNullException>(() => MissionNarrativeLocalizationAudit.FromCoverageMap(MissionNarrativeStringIdCoverageMap.CreateActOne(), null));
            Assert.Throws<ArgumentNullException>(() => MissionNarrativeLocalizationAudit.FromCoverage(null, new string[0]));
        }

        [Test]
        public void Audit_DoesNotContainServerTruthRewardEconomyOrSensitiveFields()
        {
            var audit = MissionNarrativeLocalizationAudit.FromCoverageMap(
                MissionNarrativeStringIdCoverageMap.CreateActOne(),
                new string[0]);
            var combined = audit.FlattenForValidation();

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
            Assert.That(combined.Contains("localizedFullText"), Is.EqualTo(false));
        }
    }
}
