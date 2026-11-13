using LastRail.Narrative;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativeStringIdCoverageJsonAdapterTests
    {
        [Test]
        public void ToJson_ExportsActOneCoverageMapWithStableDebugFields()
        {
            var json = MissionNarrativeStringIdCoverageJsonAdapter.ToJson(
                MissionNarrativeStringIdCoverageMap.CreateActOne());

            Assert.That(json.Contains("\"missionCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"totalStringIdCount\":"), Is.EqualTo(true));
            Assert.That(json.Contains("\"totalMissingRequiredSurfaceCount\":0"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasLocalizedTextPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"missions\":[{\"missionId\":\"opn_01_last_departure\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"displayName\":\"마지막 출발\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"entryCount\":20"), Is.EqualTo(true));
            Assert.That(json.Contains("\"entries\":[{\"surface\":\"mission_start\",\"slot\":\"copy\",\"stringId\":\"narrative.opn_01_last_departure.mission_start.copy\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"missionId\":\"raid_raider_king_engine\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"stringId\":\"narrative.raid_raider_king_engine.codex.entry\""), Is.EqualTo(true));
        }

        [Test]
        public void ToJson_EntryMetadataIsIdentifierOnly()
        {
            var json = MissionNarrativeStringIdCoverageJsonAdapter.ToJson(
                MissionNarrativeStringIdCoverage.FromDefinition(ActOneMissionNarrativeFixtures.Opn03WaterStop()));

            Assert.That(json.Contains("\"missionId\":\"opn_03_water_stop\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"surface\":\"mission_start\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"slot\":\"copy\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"stringId\":\"narrative.opn_03_water_stop.mission_start.copy\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasLocalizedTextPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("급수 정거장에서 물을 확보하되"), Is.EqualTo(false));
            Assert.That(json.Contains("급수 정거장 진입"), Is.EqualTo(false));
        }

        [Test]
        public void ToJson_RejectsNullInputs()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => MissionNarrativeStringIdCoverageJsonAdapter.ToJson((MissionNarrativeStringIdCoverageMap)null));
            Assert.Throws<System.ArgumentNullException>(
                () => MissionNarrativeStringIdCoverageJsonAdapter.ToJson((MissionNarrativeStringIdCoverage)null));
        }

        [Test]
        public void ToJson_DoesNotContainServerTruthRewardEconomyOrSensitiveFields()
        {
            var json = MissionNarrativeStringIdCoverageJsonAdapter.ToJson(
                MissionNarrativeStringIdCoverageMap.CreateActOne());

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
