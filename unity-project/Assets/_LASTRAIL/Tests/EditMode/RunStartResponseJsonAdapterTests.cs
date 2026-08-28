using System;
using NUnit.Framework;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class RunStartResponseJsonAdapterTests
    {
        [Test]
        public void FromJson_MapsServerCamelCaseFieldsIntoDto()
        {
            var dto = RunStartResponseJsonAdapter.FromJson(Json());

            Assert.That(dto.RunId, Is.EqualTo("run_001"));
            Assert.That(dto.BalanceVersion, Is.EqualTo("balance_v1"));
            Assert.That(dto.OperationId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(dto.ActId, Is.EqualTo("act_01_wasteland_railway"));
            Assert.That(dto.RegionId, Is.EqualTo("region_wasteland_railway"));
            Assert.That(dto.EnemyFactionId, Is.EqualTo("faction_wasteland_raiders"));
            Assert.That(dto.RiskLevel, Is.EqualTo(2));
            Assert.That(dto.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed1234"));
            Assert.That(dto.Seed, Is.EqualTo(1234u));
            Assert.That(dto.RewardTableId, Is.EqualTo("reward_act1_opn03_risk2"));
            Assert.That(dto.StartAugmentPackageRef, Is.EqualTo("augment_pkg_act1_opn03_v1"));
            Assert.That(dto.StartTacticPackageRef, Is.EqualTo("tactic_pkg_act1_opn03_v1"));
            Assert.That(dto.ServerStartedAt, Is.EqualTo(StartedAt()));
            Assert.That(dto.FirstClearEligible, Is.EqualTo(true));
        }

        [Test]
        public void FromJson_AcceptsServerIsoUtcMillisecondsTimestamp()
        {
            var dto = RunStartResponseJsonAdapter.FromJson(Json().Replace(
                "2026-08-26T12:30:00.0000000Z",
                "2026-08-26T12:30:00.123Z"));

            Assert.That(dto.ServerStartedAt, Is.EqualTo(new DateTime(2026, 8, 26, 12, 30, 0, 123, DateTimeKind.Utc)));
        }

        [Test]
        public void ToJson_WritesServerCamelCaseFieldsAndUtcRoundTripTimestamp()
        {
            var json = RunStartResponseJsonAdapter.ToJson(Dto());

            Assert.That(json.Contains("\"runId\":\"run_001\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"balanceVersion\":\"balance_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"riskLevel\":2"), Is.EqualTo(true));
            Assert.That(json.Contains("\"seed\":1234"), Is.EqualTo(true));
            Assert.That(json.Contains("\"startAugmentPackageRef\":\"augment_pkg_act1_opn03_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"startTacticPackageRef\":\"tactic_pkg_act1_opn03_v1\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"serverStartedAt\":\"2026-08-26T12:30:00.0000000Z\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"firstClearEligible\":true"), Is.EqualTo(true));
        }

        [Test]
        public void FromJson_TreatsMissingStartPackageRefsAsServerNotProvided()
        {
            var json = Json()
                .Replace("\"startAugmentPackageRef\":\"augment_pkg_act1_opn03_v1\",", string.Empty)
                .Replace("\"startTacticPackageRef\":\"tactic_pkg_act1_opn03_v1\",", string.Empty);

            var dto = RunStartResponseJsonAdapter.FromJson(json);

            Assert.That(dto.StartAugmentPackageRef, Is.EqualTo(null));
            Assert.That(dto.StartTacticPackageRef, Is.EqualTo(null));
        }

        [Test]
        public void FromJson_RejectsMissingRequiredField()
        {
            var json = Json().Replace("\"wavePlanId\":\"waveplan_act1_opn03_seed1234\",", string.Empty);

            Assert.Throws<InvalidOperationException>(() => RunStartResponseJsonAdapter.FromJson(json));
        }

        [Test]
        public void FromJson_RejectsNullOrEmptyRequiredStringField()
        {
            Assert.Throws<InvalidOperationException>(() => RunStartResponseJsonAdapter.FromJson(Json().Replace("\"runId\":\"run_001\"", "\"runId\":null")));
            Assert.Throws<ArgumentException>(() => RunStartResponseJsonAdapter.FromJson(Json().Replace("\"runId\":\"run_001\"", "\"runId\":\" \"")));
        }

        [Test]
        public void FromJson_RejectsNonUtcServerStartedAt()
        {
            var json = Json().Replace("2026-08-26T12:30:00.0000000Z", "2026-08-26T12:30:00+09:00");

            Assert.Throws<FormatException>(() => RunStartResponseJsonAdapter.FromJson(json));
        }

        [Test]
        public void ToJson_RejectsNullDto()
        {
            Assert.Throws<ArgumentNullException>(() => RunStartResponseJsonAdapter.ToJson(null));
        }

        private static RunStartResponseDto Dto()
        {
            return new RunStartResponseDto(
                "run_001",
                "balance_v1",
                "opn_03_water_stop",
                "act_01_wasteland_railway",
                "region_wasteland_railway",
                "faction_wasteland_raiders",
                2,
                "waveplan_act1_opn03_seed1234",
                1234u,
                "reward_act1_opn03_risk2",
                StartedAt(),
                true,
                "augment_pkg_act1_opn03_v1",
                "tactic_pkg_act1_opn03_v1");
        }

        private static string Json()
        {
            return "{"
                + "\"runId\":\"run_001\","
                + "\"balanceVersion\":\"balance_v1\","
                + "\"operationId\":\"opn_03_water_stop\","
                + "\"actId\":\"act_01_wasteland_railway\","
                + "\"regionId\":\"region_wasteland_railway\","
                + "\"enemyFactionId\":\"faction_wasteland_raiders\","
                + "\"riskLevel\":2,"
                + "\"wavePlanId\":\"waveplan_act1_opn03_seed1234\","
                + "\"seed\":1234,"
                + "\"rewardTableId\":\"reward_act1_opn03_risk2\","
                + "\"startAugmentPackageRef\":\"augment_pkg_act1_opn03_v1\","
                + "\"startTacticPackageRef\":\"tactic_pkg_act1_opn03_v1\","
                + "\"serverStartedAt\":\"2026-08-26T12:30:00.0000000Z\","
                + "\"firstClearEligible\":true"
                + "}";
        }

        private static DateTime StartedAt()
        {
            return new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc);
        }
    }
}
