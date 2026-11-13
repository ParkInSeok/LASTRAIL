using System;
using System.Collections.Generic;
using NUnit.Framework;
using LastRail.Narrative;
using LastRail.Operators;
using LastRail.Presentation.MissionShell;

namespace LastRail.Tests.EditMode
{
    public sealed class CodexArchiveScreenPresenterTests
    {
        [Test]
        public void RenderActOneArchive_BuildsSevenEntrySafeAreaViewWithStringIdFallbacks()
        {
            var view = new CapturingCodexArchiveScreenView();
            var presenter = new CodexArchiveScreenPresenter(view);
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                "opn_03_water_stop",
                new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop" }));
            var localized = CompleteLocalizedText();
            localized.Remove("narrative.opn_02_first_signal.codex.entry");
            var archive = CodexArchivePresentationState.CreateActOne(
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                localized);
            var readiness = NarrativeLocalizationReadinessAudit.FromActOneLaunchScope(
                ActOneMissionNarrativeFixtures.All(),
                ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds(),
                localized);

            presenter.Render(archive, readiness);

            Assert.That(view.LastState.Surface, Is.EqualTo(MissionShellSurface.CodexArchive));
            Assert.That(view.LastState.Title, Is.EqualTo("기록 보관소"));
            Assert.That(view.LastState.NarrativeVersion, Is.EqualTo("act1.2026-08-30.001"));
            Assert.That(view.LastState.EntryCount, Is.EqualTo(7));
            Assert.That(view.LastState.Entries[0].MissionId, Is.EqualTo("opn_01_last_departure"));
            Assert.That(view.LastState.Entries[0].CodexStringId, Is.EqualTo("narrative.opn_01_last_departure.codex.entry"));
            Assert.That(view.LastState.Entries[1].IsFallbackText, Is.EqualTo(true));
            Assert.That(view.LastState.Entries[6].MissionId, Is.EqualTo("raid_raider_king_engine"));
            Assert.That(view.LastState.UsesPortraitSafeArea, Is.EqualTo(true));
            Assert.That(view.LastState.MinimumTouchTargetDp, Is.EqualTo(48));
            Assert.That(view.LastState.PreferredAspectWidth, Is.EqualTo(9));
            Assert.That(view.LastState.PreferredAspectHeight, Is.EqualTo(16));
            Assert.That(view.LastState.CanNavigateToMissionSelect, Is.EqualTo(true));
            Assert.That(view.LastState.CanNavigateToResult, Is.EqualTo(true));
            Assert.That(view.LastState.CanNavigateToRewardReview, Is.EqualTo(true));
            Assert.That(view.LastState.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(view.LastState.HasServerTransport, Is.EqualTo(false));
            Assert.That(view.LastState.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(view.LastState.HasRewardCalculation, Is.EqualTo(false));
            Assert.That(view.LastState.HasEconomyMutation, Is.EqualTo(false));
            Assert.That(view.LastState.HasClientSideRng, Is.EqualTo(false));
        }

        [Test]
        public void RenderActOneArchive_UsesReadabilityAuditForOverflowStatusWithoutBlockingDisplay()
        {
            var view = new CapturingCodexArchiveScreenView();
            var presenter = new CodexArchiveScreenPresenter(view);
            var localized = CompleteLocalizedText();
            localized["narrative.opn_01_last_departure.codex.entry"] = new string('가', 181);
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                "opn_01_last_departure",
                new[] { "opn_01_last_departure" }));
            var archive = CodexArchivePresentationState.CreateActOne(
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                localized);
            var readiness = NarrativeLocalizationReadinessAudit.FromActOneLaunchScope(
                ActOneMissionNarrativeFixtures.All(),
                ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds(),
                localized);

            presenter.Render(archive, readiness);

            Assert.That(view.LastState.OverflowSlotCount, Is.EqualTo(1));
            Assert.That(view.LastState.RequiresOverflowVerification, Is.EqualTo(true));
            Assert.That(view.LastState.Entries[0].CharacterCount, Is.EqualTo(181));
            Assert.That(view.LastState.Entries[0].MaxCharacterCount, Is.EqualTo(180));
            Assert.That(view.LastState.Entries[0].ExceedsRecommendedLength, Is.EqualTo(true));
            Assert.That(view.LastState.Entries[0].ReadinessStatus, Is.EqualTo("OVERFLOW"));
            Assert.That(view.LastState.IsBlockingNavigation, Is.EqualTo(false));
        }

        [Test]
        public void ToDebugJson_WritesIdsStatusAndCountsWithoutFullTextTruthTokenOrRawPayload()
        {
            var state = BuildScreenState();

            var json = CodexArchiveScreenViewStateJsonAdapter.ToJson(state);

            Assert.That(json.Contains("\"surface\":\"CodexArchive\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"title\":\"기록 보관소\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"narrativeVersion\":\"act1.2026-08-30.001\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"entryCount\":7"), Is.EqualTo(true));
            Assert.That(json.Contains("\"missionId\":\"raid_raider_king_engine\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"codexStringId\":\"narrative.raid_raider_king_engine.codex.entry\""), Is.EqualTo(true));
            Assert.That(json.Contains("\"usesPortraitSafeArea\":true"), Is.EqualTo(true));
            Assert.That(json.Contains("\"minimumTouchTargetDp\":48"), Is.EqualTo(true));
            Assert.That(json.Contains("\"hasRewardCalculation\":false"), Is.EqualTo(true));
            Assert.That(json.Contains("Last Rail의 첫 기록"), Is.EqualTo(false));
            Assert.That(json.Contains("codexEntryText"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("idToken"), Is.EqualTo(false));
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("probability"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("serverSubmit"), Is.EqualTo(false));
            Assert.That(json.Contains("PlayerPrefs"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
            Assert.That(json.Contains("WorldRaid"), Is.EqualTo(false));
        }

        private static CodexArchiveScreenViewState BuildScreenState()
        {
            var view = new CapturingCodexArchiveScreenView();
            var presenter = new CodexArchiveScreenPresenter(view);
            var localized = CompleteLocalizedText();
            var archive = CodexArchivePresentationState.CreateActOne(
                StoryProgressReadState.FromResponse(Response(
                    "raid_raider_king_engine",
                    new[]
                    {
                        "opn_01_last_departure",
                        "opn_02_first_signal",
                        "opn_03_water_stop",
                        "opn_04_scrapstar_show",
                        "opn_05_night_boarders",
                        "opn_06_broken_line",
                        "raid_raider_king_engine"
                    })),
                MissionNarrativeCatalog.CreateActOne(),
                localized);
            var readiness = NarrativeLocalizationReadinessAudit.FromActOneLaunchScope(
                ActOneMissionNarrativeFixtures.All(),
                ActOneOperatorRosterRules.CreateLaunchVisualOperatorIds(),
                localized);

            presenter.Render(archive, readiness);
            return view.LastState;
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
                "act1.2026-08-30.001",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE");
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

        private sealed class CapturingCodexArchiveScreenView : ICodexArchiveScreenView
        {
            public CodexArchiveScreenViewState LastState { get; private set; }

            public void Render(CodexArchiveScreenViewState state)
            {
                LastState = state;
            }
        }
    }
}
