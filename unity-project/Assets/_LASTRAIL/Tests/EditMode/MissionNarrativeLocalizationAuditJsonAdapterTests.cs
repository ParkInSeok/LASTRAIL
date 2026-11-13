using LastRail.Narrative;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativeLocalizationAuditJsonAdapterTests
    {
        [Test]
        public void ToJson_ExportsCompleteAuditWithStableDebugFields()
        {
            var coverageMap = MissionNarrativeStringIdCoverageMap.CreateActOne();
            var audit = MissionNarrativeLocalizationAudit.FromCoverageMap(
                coverageMap,
                coverageMap.AllStringIds);

            var json = MissionNarrativeLocalizationAuditJsonAdapter.ToJson(audit);

            Assert.That(json.Contains("\"status\":\"Complete\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"missionCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"requiredStringIdCount\":"), Is.EqualTo(true));
            Assert.That(json.Contains("\"availableStringIdCount\":"), Is.EqualTo(true));
            Assert.That(json.Contains("\"missingStringIdCount\":0"), Is.EqualTo(true));
            Assert.That(json.Contains("\"unknownAvailableStringIdCount\":0"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasLocalizedTextPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"missions\":[{\"missionId\":\"opn_01_last_departure\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"displayName\":\"마지막 출발\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"missingStringIds\":[]"), Is.EqualTo(true));
        }

        [Test]
        public void ToJson_ExportsMissingAuditWithoutLocalizedTextPayload()
        {
            var audit = MissionNarrativeLocalizationAudit.FromCoverage(
                MissionNarrativeStringIdCoverage.FromDefinition(ActOneMissionNarrativeFixtures.Opn03WaterStop()),
                new[] { "narrative.opn_03_water_stop.mission_start.copy" });

            var json = MissionNarrativeLocalizationAuditJsonAdapter.ToJson(audit);

            Assert.That(json.Contains("\"status\":\"Missing\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"missionId\":\"opn_03_water_stop\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"availableStringIdCount\":1"), Is.EqualTo(true));
            Assert.That(json.Contains("\"missingStringIds\":[\"narrative.opn_03_water_stop.loading.hint\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasLocalizedTextPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("급수 정거장에서 물을 확보하되"), Is.EqualTo(false));
            Assert.That(json.Contains("급수 정거장 진입"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_PreservesUnknownAvailableStringIdCountButNotUnknownPayload()
        {
            var audit = MissionNarrativeLocalizationAudit.FromCoverage(
                MissionNarrativeStringIdCoverage.FromDefinition(ActOneMissionNarrativeFixtures.Opn02FirstSignal()),
                new[]
                {
                    "narrative.opn_02_first_signal.mission_start.copy",
                    "narrative.unknown.extra.key"
                });

            var json = MissionNarrativeLocalizationAuditJsonAdapter.ToJson(audit);

            Assert.That(json.Contains("\"unknownAvailableStringIdCount\":1"), Is.EqualTo(true));
            Assert.That(json.Contains("narrative.unknown.extra.key"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_RejectsNullAudit()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => MissionNarrativeLocalizationAuditJsonAdapter.ToJson(null));
        }

        [Test]
        public void ToJson_DoesNotContainServerTruthRewardEconomyOrSensitiveFields()
        {
            var audit = MissionNarrativeLocalizationAudit.FromCoverageMap(
                MissionNarrativeStringIdCoverageMap.CreateActOne(),
                new string[0]);
            var json = MissionNarrativeLocalizationAuditJsonAdapter.ToJson(audit);

            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("offlineReward"), Is.EqualTo(false));
            Assert.That(json.Contains("firstClearEligible"), Is.EqualTo(false));
            Assert.That(json.Contains("unlockTruth"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("server"), Is.EqualTo(false));
            Assert.That(json.Contains("endpoint"), Is.EqualTo(false));
            Assert.That(json.Contains("HTTP"), Is.EqualTo(false));
            Assert.That(json.Contains("localizedFullText"), Is.EqualTo(false));
        }
    }
}
