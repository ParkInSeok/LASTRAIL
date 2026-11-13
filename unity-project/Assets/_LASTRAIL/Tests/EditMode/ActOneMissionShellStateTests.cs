using System;
using NUnit.Framework;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class ActOneMissionShellStateTests
    {
        [Test]
        public void CreateMissionSurface_StoresMissionNarrativeAndServerUnlockRefsForPresentationOnly()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_02_first_signal",
                unlockedOperations: new[] { "opn_03_water_stop" }));
            var catalog = MissionNarrativeCatalog.CreateActOne();

            var state = ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.Briefing,
                "opn_03_water_stop",
                RunEncounterType.Ordinary15Wave,
                storyProgress,
                catalog);

            Assert.That(state.Surface, Is.EqualTo(MissionShellSurface.Briefing));
            Assert.That(state.MissionId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.EncounterType, Is.EqualTo(RunEncounterType.Ordinary15Wave));
            Assert.That(state.DisplayLabel, Is.EqualTo("급수 정차"));
            Assert.That(state.IsServerListedUnlockedForDisplay, Is.EqualTo(true));
            Assert.That(state.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
            Assert.That(state.MissionStartButtonCopy, Is.EqualTo("급수 정거장 진입"));
            Assert.That(state.MissionStartStringId, Is.EqualTo("narrative.opn_03_water_stop.mission_start.copy"));
            Assert.That(state.LoadingHintStringId, Is.EqualTo("narrative.opn_03_water_stop.loading.hint"));
            Assert.That(state.CodexEntryStringId, Is.EqualTo("narrative.opn_03_water_stop.codex.entry"));
            Assert.That(state.HasCodexEntry, Is.EqualTo(true));
            Assert.That(state.LocalizationStatus, Is.EqualTo("LOCALIZED"));
            Assert.That(state.MissingLocalizationState, Is.EqualTo("NONE"));
            Assert.That(state.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(state.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(state.HasRewardCalculation, Is.EqualTo(false));
        }

        [Test]
        public void CreateBattleSurface_RoutesOrdinaryAndRaidToDifferentBattleSurfaces()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_06_broken_line",
                unlockedOperations: new[] { "raid_raider_king_engine" }));
            var catalog = MissionNarrativeCatalog.CreateActOne();

            var ordinary = ActOneMissionShellState.CreateBattleSurface(
                "opn_06_broken_line",
                RunEncounterType.Ordinary15Wave,
                storyProgress,
                catalog);
            var raid = ActOneMissionShellState.CreateBattleSurface(
                "raid_raider_king_engine",
                RunEncounterType.RaidBoss,
                storyProgress,
                catalog);

            Assert.That(ordinary.Surface, Is.EqualTo(MissionShellSurface.BattleOrdinary15Wave));
            Assert.That(raid.Surface, Is.EqualTo(MissionShellSurface.BattleRaidBoss));
            Assert.That(raid.DisplayLabel, Is.EqualTo("약탈왕 기관차"));
            Assert.That(raid.IsServerListedUnlockedForDisplay, Is.EqualTo(true));
        }

        [Test]
        public void CreateGlobalSurface_RepresentsLaunchShellScreensWithoutMissionTruth()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "none",
                unlockedOperations: new[] { "opn_01_last_departure" }));

            var state = ActOneMissionShellState.CreateGlobalSurface(
                MissionShellSurface.MissionSelect,
                storyProgress);

            Assert.That(state.Surface, Is.EqualTo(MissionShellSurface.MissionSelect));
            Assert.That(state.MissionId, Is.EqualTo(null));
            Assert.That(state.DisplayLabel, Is.EqualTo("작전 선택"));
            Assert.That(state.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
            Assert.That(state.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(state.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(state.HasRewardCalculation, Is.EqualTo(false));
        }

        [Test]
        public void CreateMissionSurface_UsesFallbackRefsWhenNarrativeFixtureIsMissing()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "none",
                unlockedOperations: new[] { "future_operation" }));
            var catalog = MissionNarrativeCatalog.CreateActOne();

            var state = ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.LoadingHint,
                "future_operation",
                RunEncounterType.Ordinary15Wave,
                storyProgress,
                catalog);

            Assert.That(state.MissionId, Is.EqualTo("future_operation"));
            Assert.That(state.DisplayLabel, Is.EqualTo("future_operation"));
            Assert.That(state.HasNarrativeDefinition, Is.EqualTo(false));
            Assert.That(state.MissionStartButtonCopy, Is.EqualTo(null));
            Assert.That(state.MissionStartStringId, Is.EqualTo("narrative.future_operation.mission_start.copy"));
            Assert.That(state.LoadingHintStringId, Is.EqualTo("narrative.future_operation.loading.hint"));
            Assert.That(state.HasCodexEntry, Is.EqualTo(false));
            Assert.That(state.CodexEntryStringId, Is.EqualTo("narrative.future_operation.codex.entry"));
            Assert.That(state.MissingLocalizationState, Is.EqualTo("LOCAL_FIXTURE_MISSING"));
            Assert.That(state.IsServerListedUnlockedForDisplay, Is.EqualTo(true));
        }

        [Test]
        public void DebugJson_WritesStableFieldsWithoutTruthCalculationTransportOrSensitivePayload()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_05_night_boarders",
                unlockedOperations: new[] { "opn_06_broken_line" }));
            var state = ActOneMissionShellState.CreateBattleSurface(
                "opn_06_broken_line",
                RunEncounterType.Ordinary15Wave,
                storyProgress,
                MissionNarrativeCatalog.CreateActOne());

            var json = ActOneMissionShellStateJsonAdapter.ToJson(state);

            Assert.That(json, Is.EqualTo(
                "{\"surface\":\"BattleOrdinary15Wave\"," +
                "\"missionId\":\"opn_06_broken_line\"," +
                "\"encounterType\":\"Ordinary15Wave\"," +
                "\"displayLabel\":\"끊어진 선로\"," +
                "\"isServerListedUnlockedForDisplay\":true," +
                "\"narrativeVersion\":\"act1.2026-08-27.001\"," +
                "\"hasNarrativeDefinition\":true," +
                "\"missionStartButtonCopy\":\"끊어진 선로 복구\"," +
                "\"missionStartStringId\":\"narrative.opn_06_broken_line.mission_start.copy\"," +
                "\"loadingHintStringId\":\"narrative.opn_06_broken_line.loading.hint\"," +
                "\"hasCodexEntry\":true," +
                "\"codexEntryStringId\":\"narrative.opn_06_broken_line.codex.entry\"," +
                "\"localizationStatus\":\"LOCALIZED\"," +
                "\"missingLocalizationState\":\"NONE\"," +
                "\"isDisplayOnly\":true," +
                "\"hasUnlockTruth\":false," +
                "\"hasRewardCalculation\":false}"));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("offlineReward"), Is.EqualTo(false));
            Assert.That(json.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("firstClearEligible"), Is.EqualTo(false));
            Assert.That(json.Contains("gachaProbability"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("UnityWebRequest"), Is.EqualTo(false));
        }

        [Test]
        public void Factories_RejectInvalidInputsAndUnsupportedBattleSurface()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response("none", new[] { "opn_01_last_departure" }));
            var catalog = MissionNarrativeCatalog.CreateActOne();

            Assert.Throws<ArgumentException>(() => ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.AuthGate,
                "opn_01_last_departure",
                RunEncounterType.Ordinary15Wave,
                storyProgress,
                catalog));
            Assert.Throws<ArgumentException>(() => ActOneMissionShellState.CreateGlobalSurface(
                MissionShellSurface.Briefing,
                storyProgress));
            Assert.Throws<ArgumentNullException>(() => ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.Briefing,
                "opn_01_last_departure",
                RunEncounterType.Ordinary15Wave,
                null,
                catalog));
            Assert.Throws<ArgumentException>(() => ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.Briefing,
                " ",
                RunEncounterType.Ordinary15Wave,
                storyProgress,
                catalog));
        }

        private static StoryProgressReadResponseDto Response(string highestStoryProgressId, string[] unlockedOperations)
        {
            return new StoryProgressReadResponseDto(
                "act_01_wasteland_railway",
                highestStoryProgressId,
                highestStoryProgressId == "none" ? null : highestStoryProgressId,
                0,
                new string[0],
                unlockedOperations,
                "act1.2026-08-27.001",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE");
        }
    }
}
