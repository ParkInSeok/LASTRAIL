using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Narrative;
using LastRail.Operators;

namespace LastRail.Tests.EditMode
{
    public sealed class NarrativeLocalizationReadinessAuditTests
    {
        [Test]
        public void FromActOneLaunchScope_RequiresFifteenOperatorNamesRaidRefundToastAndMissionRewardFraming()
        {
            var audit = NarrativeLocalizationReadinessAudit.FromActOneLaunchScope(
                ActOneMissionNarrativeFixtures.All(),
                ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds(),
                CompleteLocalizedText());

            Assert.That(audit.OperatorDisplayNameStringIdCount, Is.EqualTo(15));
            Assert.That(audit.RequiredStringIdCount, Is.EqualTo(132));
            Assert.That(audit.MissingStringIdCount, Is.EqualTo(0));
            Assert.That(audit.HasStringId("narrative.operator.op_vex.display.name"), Is.EqualTo(true));
            Assert.That(audit.HasStringId("narrative.operator.op_warden.display.name"), Is.EqualTo(true));
            Assert.That(audit.HasStringId("narrative.raid_raider_king_engine.failure_refund.toast"), Is.EqualTo(true));
            Assert.That(audit.HasStringId("narrative.opn_01_last_departure.reward_framing.first_clear"), Is.EqualTo(true));
            Assert.That(audit.HasStringId("narrative.raid_raider_king_engine.codex.entry"), Is.EqualTo(true));
            Assert.That(audit.HasLocalizedFullTextPayload, Is.EqualTo(false));
            Assert.That(audit.HasServerTruthPayload, Is.EqualTo(false));
            Assert.That(audit.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(audit.HasEconomyMutation, Is.EqualTo(false));
        }

        [Test]
        public void ReadabilityAudit_FlagsLongCodexAndRewardFramingSlotsWithoutEmbeddingTextPayload()
        {
            var localized = CompleteLocalizedText();
            localized["narrative.opn_01_last_departure.codex.entry"] = new string('가', 181);
            localized["narrative.opn_01_last_departure.reward_framing.first_clear"] = new string('나', 65);

            var audit = NarrativeLocalizationReadinessAudit.FromActOneLaunchScope(
                ActOneMissionNarrativeFixtures.All(),
                ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds(),
                localized);

            Assert.That(audit.OverflowSlotCount, Is.EqualTo(2));
            Assert.That(audit.FindSlot("narrative.opn_01_last_departure.codex.entry").ExceedsRecommendedLength, Is.EqualTo(true));
            Assert.That(audit.FindSlot("narrative.opn_01_last_departure.codex.entry").MaxCharacterCount, Is.EqualTo(180));
            Assert.That(audit.FindSlot("narrative.opn_01_last_departure.reward_framing.first_clear").ExceedsRecommendedLength, Is.EqualTo(true));
            Assert.That(audit.FindSlot("narrative.opn_01_last_departure.reward_framing.first_clear").MaxCharacterCount, Is.EqualTo(64));
            Assert.That(audit.HasLocalizedFullTextPayload, Is.EqualTo(false));
        }

        [Test]
        public void ToJson_WritesStableDebugFieldsWithoutRawTextTokenTruthOrCalculationPayload()
        {
            var audit = NarrativeLocalizationReadinessAudit.FromActOneLaunchScope(
                ActOneMissionNarrativeFixtures.All(),
                ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds(),
                CompleteLocalizedText());

            var json = NarrativeLocalizationReadinessAuditJsonAdapter.ToJson(audit);

            Assert.That(json.Contains("\"requiredStringIdCount\":132"), Is.EqualTo(true));
            Assert.That(json.Contains("\"operatorDisplayNameStringIdCount\":15"), Is.EqualTo(true));
            Assert.That(json.Contains("\"raidFailureRefundToastStringId\":\"narrative.raid_raider_king_engine.failure_refund.toast\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasLocalizedFullTextPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasServerTruthPayload\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRewardCalculation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasEconomyMutation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("Last Rail의 첫 기록"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("fullWeights"), Is.EqualTo(false));
            Assert.That(json.Contains("serverTruth"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("economyGrant"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        private static Dictionary<string, string> CompleteLocalizedText()
        {
            var values = new Dictionary<string, string>();
            foreach (var definition in ActOneMissionNarrativeFixtures.All())
            {
                var coverage = MissionNarrativeStringIdCoverage.FromDefinition(definition);
                foreach (var entry in coverage.Entries)
                {
                    values[entry.StringId] = "확인";
                }
            }

            foreach (var operatorId in ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds())
            {
                values[LaunchNarrativeLocalizationStringIds.ForOperatorDisplayName(operatorId)] = operatorId;
            }

            values[LaunchNarrativeLocalizationStringIds.ForRaidFailureRefundToast()] = "실패한 레이드 스태미나 환급 안내";
            return values;
        }
    }
}
