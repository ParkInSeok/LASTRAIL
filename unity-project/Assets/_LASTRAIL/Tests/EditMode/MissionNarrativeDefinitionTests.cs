using System;
using LastRail.Narrative;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativeDefinitionTests
    {
        [Test]
        public void Constructor_StoresLocalizedMissionNarrativeSectionsAndCopiesCollections()
        {
            var briefingLines = new[] { "문을 닫기 전까지 버텨야 합니다.", "아직 뛰어오는 사람이 있습니다." };
            var definition = new MissionNarrativeDefinition(
                "opn_test",
                "테스트 작전",
                "짧은 브리핑입니다.",
                briefingLines,
                new[] { "기다린다." },
                "작전 시작",
                "칸 배치를 확인하세요.",
                new[] { "첫 교전입니다." },
                new[] { "분대를 칸 단위로 옮길 수 있습니다." },
                new[] { "적 증원입니다." },
                new[] { "민간인 안전 확인" },
                new[] { "최종 공세입니다." },
                new[] { "작전 성공." },
                new[] { "작전 실패." },
                "작전 결과",
                "열차가 출발했습니다.",
                "첫 클리어 보상은 서버 미리보기에서 확인합니다.",
                "반복 보상은 서버 미리보기에서 확인합니다.",
                "기록 항목입니다.");

            briefingLines[0] = "mutated";

            Assert.That(definition.MissionId, Is.EqualTo("opn_test"));
            Assert.That(definition.DisplayName, Is.EqualTo("테스트 작전"));
            Assert.That(definition.BriefingLines[0], Is.EqualTo("문을 닫기 전까지 버텨야 합니다."));
            Assert.That(definition.CommanderResponseCandidates.Count, Is.EqualTo(1));
            Assert.That(definition.OptionalObjectiveCallouts.Count, Is.EqualTo(1));
            Assert.That(definition.HasCodexEntry, Is.EqualTo(true));
        }

        [Test]
        public void Constructor_RejectsMissingRequiredTextOrEmptyNarrativeLine()
        {
            Assert.Throws<ArgumentException>(() => new MissionNarrativeDefinition(
                "",
                "테스트 작전",
                "브리핑",
                new[] { "line" },
                new string[0],
                "시작",
                "힌트",
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "결과",
                "요약",
                "첫 클리어",
                "반복",
                null));

            Assert.Throws<ArgumentException>(() => new MissionNarrativeDefinition(
                "opn_test",
                "테스트 작전",
                "브리핑",
                new[] { "line", " " },
                new string[0],
                "시작",
                "힌트",
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                new string[0],
                "결과",
                "요약",
                "첫 클리어",
                "반복",
                null));
        }

        [Test]
        public void Opn01Fixture_UsesStableMissionIdAndLocalizedDisplayText()
        {
            var definition = ActOneMissionNarrativeFixtures.Opn01LastDeparture();

            Assert.That(definition.MissionId, Is.EqualTo("opn_01_last_departure"));
            Assert.That(definition.DisplayName, Is.EqualTo("마지막 출발"));
            Assert.That(definition.MissionStartButtonCopy, Is.EqualTo("마지막 출발 시작"));
            Assert.That(definition.LoadingHint, Is.EqualTo("상단 전장은 선로 밖 위협, 하단은 현재 칸 내부 상황입니다."));
            Assert.That(definition.OpeningCallouts.Count, Is.EqualTo(2));
            Assert.That(definition.FinalWaveCallouts.Count, Is.EqualTo(1));
            Assert.That(definition.ResultTitle, Is.EqualTo("마지막 출발"));
            Assert.That(definition.HasCodexEntry, Is.EqualTo(true));
        }

        [Test]
        public void Opn01Fixture_DoesNotContainRewardAmountsEconomyValuesOrGachaOwnershipLanguage()
        {
            var text = ActOneMissionNarrativeFixtures.Opn01LastDeparture().FlattenTextForValidation();

            Assert.That(text.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(text.Contains("creditAmount"), Is.EqualTo(false));
            Assert.That(text.Contains("cur_"), Is.EqualTo(false));
            Assert.That(text.Contains("gacha"), Is.EqualTo(false));
            Assert.That(text.Contains("ownership"), Is.EqualTo(false));
            Assert.That(text.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(text.Contains("firstClearEligible"), Is.EqualTo(false));
        }

        [Test]
        public void ActOneFixtures_CoverSixOperationsAndRaidWithStableIdsAndButtonCopy()
        {
            var definitions = ActOneMissionNarrativeFixtures.All();

            Assert.That(definitions.Count, Is.EqualTo(7));
            Assert.That(definitions[0].MissionId, Is.EqualTo("opn_01_last_departure"));
            Assert.That(definitions[0].MissionStartButtonCopy, Is.EqualTo("마지막 출발 시작"));
            Assert.That(definitions[1].MissionId, Is.EqualTo("opn_02_first_signal"));
            Assert.That(definitions[1].MissionStartButtonCopy, Is.EqualTo("첫 신호 복원"));
            Assert.That(definitions[2].MissionId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(definitions[2].MissionStartButtonCopy, Is.EqualTo("급수 정거장 진입"));
            Assert.That(definitions[3].MissionId, Is.EqualTo("opn_04_scrapstar_show"));
            Assert.That(definitions[3].MissionStartButtonCopy, Is.EqualTo("스크랩스타 진입"));
            Assert.That(definitions[4].MissionId, Is.EqualTo("opn_05_night_boarders"));
            Assert.That(definitions[4].MissionStartButtonCopy, Is.EqualTo("야간 승선전 시작"));
            Assert.That(definitions[5].MissionId, Is.EqualTo("opn_06_broken_line"));
            Assert.That(definitions[5].MissionStartButtonCopy, Is.EqualTo("끊어진 선로 복구"));
            Assert.That(definitions[6].MissionId, Is.EqualTo("raid_raider_king_engine"));
            Assert.That(definitions[6].MissionStartButtonCopy, Is.EqualTo("왕관 기관차 요격"));
        }

        [Test]
        public void ActOneFixtures_DoNotContainRewardAmountsEconomyValuesOrGachaOwnershipLanguage()
        {
            foreach (var definition in ActOneMissionNarrativeFixtures.All())
            {
                var text = definition.FlattenTextForValidation();

                Assert.That(text.Contains("rewardAmount"), Is.EqualTo(false));
                Assert.That(text.Contains("creditAmount"), Is.EqualTo(false));
                Assert.That(text.Contains("cur_"), Is.EqualTo(false));
                Assert.That(text.Contains("gacha"), Is.EqualTo(false));
                Assert.That(text.Contains("ownership"), Is.EqualTo(false));
                Assert.That(text.Contains("storyProgressMultiplier"), Is.EqualTo(false));
                Assert.That(text.Contains("firstClearEligible"), Is.EqualTo(false));
            }
        }

        [Test]
        public void MissionNarrativeCatalog_FindsDefinitionsByStableMissionIdAndReturnsNullForUnknown()
        {
            var catalog = MissionNarrativeCatalog.CreateActOne();

            Assert.That(catalog.Count, Is.EqualTo(7));
            Assert.That(catalog.FindByMissionId("opn_03_water_stop").DisplayName, Is.EqualTo("급수 정차"));
            Assert.That(catalog.FindByMissionId("raid_raider_king_engine").DisplayName, Is.EqualTo("약탈왕 기관차"));
            Assert.That(catalog.FindByMissionId("unknown_mission"), Is.EqualTo(null));
            Assert.That(catalog.FindByMissionId(" "), Is.EqualTo(null));
        }

        [Test]
        public void MissionNarrativeCatalog_RejectsDuplicateMissionIds()
        {
            Assert.Throws<ArgumentException>(() => new MissionNarrativeCatalog(new[]
            {
                ActOneMissionNarrativeFixtures.Opn01LastDeparture(),
                ActOneMissionNarrativeFixtures.Opn01LastDeparture()
            }));
        }
    }
}
