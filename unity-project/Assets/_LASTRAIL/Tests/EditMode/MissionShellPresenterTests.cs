using System;
using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using LastRail.Run;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionShellPresenterTests
    {
        [Test]
        public void RenderMissionSelect_BuildsSafeAreaTouchReadyViewWithoutUnlockTruth()
        {
            var view = new CapturingMissionShellScreenView();
            var presenter = new MissionShellScreenPresenter(view);

            presenter.RenderMissionSelect(
                StoryProgressReadState.FromResponse(Response("none", new[] { "opn_01_last_departure" })),
                MissionNarrativeCatalog.CreateActOne());

            Assert.That(view.LastState.Surface, Is.EqualTo(MissionShellSurface.MissionSelect));
            Assert.That(view.LastState.Title, Is.EqualTo("작전 선택"));
            Assert.That(view.LastState.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(view.LastState.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(view.LastState.MissionRows.Count, Is.EqualTo(7));
            Assert.That(view.LastState.MissionRows[0].MissionId, Is.EqualTo("opn_01_last_departure"));
            Assert.That(view.LastState.MissionRows[0].DisplayLabel, Is.EqualTo("마지막 출발"));
            Assert.That(view.LastState.MissionRows[0].IsServerListedUnlockedForDisplay, Is.EqualTo(true));
            Assert.That(view.LastState.MissionRows[1].IsServerListedUnlockedForDisplay, Is.EqualTo(false));
            Assert.That(view.LastState.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(view.LastState.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(view.LastState.HasRewardCalculation, Is.EqualTo(false));
        }

        [Test]
        public void RenderBriefing_UsesLocalizedNarrativeAndStartButtonCopy()
        {
            var view = new CapturingMissionShellScreenView();
            var presenter = new MissionShellScreenPresenter(view);

            presenter.RenderBriefing(
                "opn_01_last_departure",
                RunEncounterType.Ordinary15Wave,
                StoryProgressReadState.FromResponse(Response("none", new[] { "opn_01_last_departure" })),
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>
                {
                    { "narrative.opn_01_last_departure.briefing.line_001", "승강장 방어선을 펴라." },
                    { "narrative.opn_01_last_departure.mission_start.copy", "마지막 출발 시작" }
                });

            Assert.That(view.LastState.Surface, Is.EqualTo(MissionShellSurface.Briefing));
            Assert.That(view.LastState.Title, Is.EqualTo("마지막 출발"));
            Assert.That(view.LastState.PrimaryActionLabel, Is.EqualTo("마지막 출발 시작"));
            Assert.That(view.LastState.PrimaryActionStringId, Is.EqualTo("narrative.opn_01_last_departure.mission_start.copy"));
            Assert.That(view.LastState.BodyLines[0], Is.EqualTo("승강장 방어선을 펴라."));
            Assert.That(view.LastState.BodyLineStringIds[0], Is.EqualTo("narrative.opn_01_last_departure.briefing.line_001"));
            Assert.That(view.LastState.BodyLineStringIds, Does.Not.Contain("narrative.opn_01_last_departure.mission_start.copy"));
            Assert.That(view.LastState.HasServerTransport, Is.EqualTo(false));
        }

        [Test]
        public void RenderRunStartResult_RoutesOrdinaryAndRaidPlaceholdersWithoutServerTruthCalculation()
        {
            var view = new CapturingMissionShellScreenView();
            var presenter = new MissionShellScreenPresenter(view);
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                "opn_06_broken_line",
                new[] { "opn_06_broken_line", "raid_raider_king_engine" }));

            presenter.RenderRunStartResult(
                RunStartClientResult.Started(StartResponse(
                    "opn_06_broken_line",
                    RunEncounterType.Ordinary15Wave)),
                storyProgress,
                MissionNarrativeCatalog.CreateActOne());
            Assert.That(view.LastState.Surface, Is.EqualTo(MissionShellSurface.LoadingHint));
            Assert.That(view.LastState.NextBattleSurface, Is.EqualTo(MissionShellSurface.BattleOrdinary15Wave));
            Assert.That(view.LastState.BodyLines[0], Does.Contain("15공세"));

            presenter.RenderRunStartResult(
                RunStartClientResult.Started(StartResponse(
                    "raid_raider_king_engine",
                    RunEncounterType.RaidBoss,
                    "raid_raider_king_engine",
                    "boss_crown_engine")),
                storyProgress,
                MissionNarrativeCatalog.CreateActOne());
            Assert.That(view.LastState.Surface, Is.EqualTo(MissionShellSurface.LoadingHint));
            Assert.That(view.LastState.NextBattleSurface, Is.EqualTo(MissionShellSurface.BattleRaidBoss));
            Assert.That(view.LastState.BodyLines[0], Does.Contain("보스전"));
            Assert.That(view.LastState.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(view.LastState.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(view.LastState.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void RenderRunStartResult_MapsAuthStateConflictAndNetworkPlaceholdersForUx()
        {
            var view = new CapturingMissionShellScreenView();
            var presenter = new MissionShellScreenPresenter(view);
            var storyProgress = StoryProgressReadState.FromResponse(Response("none", new[] { "opn_01_last_departure" }));
            var catalog = MissionNarrativeCatalog.CreateActOne();

            presenter.RenderRunStartResult(
                RunStartClientResult.AuthRequired("auth_required", "login required"),
                storyProgress,
                catalog);
            Assert.That(view.LastState.Surface, Is.EqualTo(MissionShellSurface.AuthGate));
            Assert.That(view.LastState.RequiresAuthRefreshOrGuestLogin, Is.EqualTo(true));

            presenter.RenderRunStartResult(
                RunStartClientResult.StateConflict("run_start_state_conflict", "state conflict"),
                storyProgress,
                catalog);
            Assert.That(view.LastState.RequiresStateResync, Is.EqualTo(true));
            Assert.That(view.LastState.ErrorCode, Is.EqualTo("run_start_state_conflict"));

            presenter.RenderRunStartResult(
                RunStartClientResult.NetworkError("timeout"),
                storyProgress,
                catalog);
            Assert.That(view.LastState.CanRetryWithSameIdempotencyKey, Is.EqualTo(true));
            Assert.That(view.LastState.PrimaryActionLabel, Is.EqualTo("같은 요청으로 재시도"));
        }

        private static StoryProgressReadResponseDto Response(string highestStoryProgressId, string[] unlockedOperations)
        {
            return new StoryProgressReadResponseDto(
                "act_01_wasteland_railway",
                highestStoryProgressId,
                highestStoryProgressId == "none" ? null : highestStoryProgressId,
                0,
                Array.Empty<string>(),
                unlockedOperations,
                "act1.2026-08-27.001",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE");
        }

        private static RunStartResponseDto StartResponse(
            string operationId,
            RunEncounterType encounterType,
            string raidId = null,
            string bossId = null)
        {
            return new RunStartResponseDto(
                "run_001",
                "1",
                operationId,
                "act_01_wasteland_railway",
                "reg_ashline",
                "raiders",
                2,
                "wave_plan_001",
                100,
                "reward_table_001",
                DateTime.SpecifyKind(new DateTime(2026, 8, 29, 1, 2, 3), DateTimeKind.Utc),
                true,
                "aug_pkg_001",
                "tactic_pkg_001",
                "op_preview_001",
                "content_001",
                encounterType,
                encounterType == RunEncounterType.Ordinary15Wave ? 15 : (int?)null,
                raidId,
                bossId);
        }

        private sealed class CapturingMissionShellScreenView : IMissionShellScreenView
        {
            public MissionShellScreenViewState LastState { get; private set; }

            public void Render(MissionShellScreenViewState state)
            {
                LastState = state;
            }
        }
    }
}
