using System;
using System.Collections.Generic;
using LastRail.Narrative;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionNarrativePresentationStateTests
    {
        [Test]
        public void BriefingSurface_UsesStringIdLocalizationAndServerUnlockDisplayInputOnly()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_02_first_signal",
                clearedOperationIds: new[] { "opn_01_last_departure", "opn_02_first_signal" },
                unlockedOperations: new[] { "opn_03_water_stop" }));
            var localized = new Dictionary<string, string>
            {
                { "narrative.opn_03_water_stop.briefing.line_001", "급수 정거장은 아직 살아 있습니다." },
                { "narrative.opn_03_water_stop.mission_start.copy", "급수 정거장 진입" }
            };

            var state = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.Briefing,
                "opn_03_water_stop",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                localized);

            Assert.That(state.Surface, Is.EqualTo(MissionNarrativeSurfaceKind.Briefing));
            Assert.That(state.MissionId, Is.EqualTo("opn_03_water_stop"));
            Assert.That(state.DisplayName, Is.EqualTo("급수 정차"));
            Assert.That(state.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
            Assert.That(state.IsServerListedUnlockedForDisplay, Is.EqualTo(true));
            Assert.That(state.IsServerListedClearedForDisplay, Is.EqualTo(false));
            Assert.That(state.HasNarrativeDefinition, Is.EqualTo(true));
            Assert.That(state.Lines.Count, Is.EqualTo(2));
            Assert.That(state.Lines[0].StringId, Is.EqualTo("narrative.opn_03_water_stop.briefing.line_001"));
            Assert.That(state.Lines[0].Text, Is.EqualTo("급수 정거장은 아직 살아 있습니다."));
            Assert.That(state.Lines[0].IsFallbackText, Is.EqualTo(false));
            Assert.That(state.Lines[1].StringId, Is.EqualTo("narrative.opn_03_water_stop.mission_start.copy"));
            Assert.That(state.Lines[1].Text, Is.EqualTo("급수 정거장 진입"));
            Assert.That(state.LocalizationStatus, Is.EqualTo("LOCALIZED"));
            Assert.That(state.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(state.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(state.HasStoryProgressMutation, Is.EqualTo(false));
            Assert.That(state.HasRewardCalculation, Is.EqualTo(false));
        }

        [Test]
        public void LoadingResultAndCodexSurfaces_UseFixtureFallbackWhenLocalizedStringIsMissing()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_06_broken_line",
                clearedOperationIds: new[] { "opn_06_broken_line" },
                unlockedOperations: new[] { "raid_raider_king_engine" }));

            var loading = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.LoadingHint,
                "raid_raider_king_engine",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>());
            var result = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.Result,
                "raid_raider_king_engine",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>());
            var codex = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.CodexArchive,
                "raid_raider_king_engine",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>());

            Assert.That(loading.Lines[0].StringId, Is.EqualTo("narrative.raid_raider_king_engine.loading.hint"));
            Assert.That(loading.Lines[0].IsFallbackText, Is.EqualTo(true));
            Assert.That(loading.LocalizationStatus, Is.EqualTo("MISSING_FALLBACK"));
            Assert.That(result.Lines[0].StringId, Is.EqualTo("narrative.raid_raider_king_engine.result.title"));
            Assert.That(result.Lines[1].StringId, Is.EqualTo("narrative.raid_raider_king_engine.result.summary"));
            Assert.That(codex.Lines[0].StringId, Is.EqualTo("narrative.raid_raider_king_engine.codex.entry"));
            Assert.That(codex.Lines[0].Text.Contains("왕관 기관차"), Is.EqualTo(true));
        }

        [Test]
        public void CodexArchive_CreatesReadOnlyEntriesFromCatalogAndServerReadState()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_03_water_stop",
                clearedOperationIds: new[] { "opn_01_last_departure", "opn_02_first_signal", "opn_03_water_stop" },
                unlockedOperations: new[] { "opn_04_scrapstar_show" }));

            var archive = CodexArchivePresentationState.CreateActOne(
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>
                {
                    { "narrative.opn_01_last_departure.codex.entry", "첫 기록: 마지막 출발" }
                });

            Assert.That(archive.EntryCount, Is.EqualTo(7));
            Assert.That(archive.NarrativeVersion, Is.EqualTo("act1.2026-08-27.001"));
            Assert.That(archive.Entries[0].MissionId, Is.EqualTo("opn_01_last_departure"));
            Assert.That(archive.Entries[0].DisplayName, Is.EqualTo("마지막 출발"));
            Assert.That(archive.Entries[0].IsServerListedClearedForDisplay, Is.EqualTo(true));
            Assert.That(archive.Entries[0].IsServerListedUnlockedForDisplay, Is.EqualTo(false));
            Assert.That(archive.Entries[0].CanOpenForDisplay, Is.EqualTo(true));
            Assert.That(archive.Entries[0].CodexEntryText, Is.EqualTo("첫 기록: 마지막 출발"));
            Assert.That(archive.Entries[0].IsFallbackText, Is.EqualTo(false));
            Assert.That(archive.Entries[3].MissionId, Is.EqualTo("opn_04_scrapstar_show"));
            Assert.That(archive.Entries[3].IsServerListedUnlockedForDisplay, Is.EqualTo(true));
            Assert.That(archive.Entries[3].CanOpenForDisplay, Is.EqualTo(true));
            Assert.That(archive.Entries[6].MissionId, Is.EqualTo("raid_raider_king_engine"));
            Assert.That(archive.Entries[6].CanOpenForDisplay, Is.EqualTo(false));
            Assert.That(archive.IsDisplayOnly, Is.EqualTo(true));
            Assert.That(archive.HasUnlockTruth, Is.EqualTo(false));
            Assert.That(archive.HasStoryProgressMutation, Is.EqualTo(false));
            Assert.That(archive.HasRewardCalculation, Is.EqualTo(false));
        }

        [Test]
        public void DebugJson_WritesStableStringIdStatusOnlyWithoutSensitiveOrCalculationPayload()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "opn_01_last_departure",
                clearedOperationIds: new[] { "opn_01_last_departure" },
                unlockedOperations: new[] { "opn_02_first_signal" }));
            var state = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.LoadingHint,
                "opn_02_first_signal",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>());
            var archive = CodexArchivePresentationState.CreateActOne(
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>());

            var stateJson = MissionNarrativePresentationStateJsonAdapter.ToJson(state);
            var archiveJson = CodexArchivePresentationStateJsonAdapter.ToJson(archive);

            Assert.That(stateJson, Is.EqualTo(
                "{\"surface\":\"LoadingHint\"," +
                "\"missionId\":\"opn_02_first_signal\"," +
                "\"displayName\":\"첫 신호\"," +
                "\"narrativeVersion\":\"act1.2026-08-27.001\"," +
                "\"isServerListedUnlockedForDisplay\":true," +
                "\"isServerListedClearedForDisplay\":false," +
                "\"hasNarrativeDefinition\":true," +
                "\"localizationStatus\":\"MISSING_FALLBACK\"," +
                "\"lineCount\":1," +
                "\"lines\":[{\"stringId\":\"narrative.opn_02_first_signal.loading.hint\",\"isFallbackText\":true}]," +
                "\"isDisplayOnly\":true," +
                "\"hasUnlockTruth\":false," +
                "\"hasStoryProgressMutation\":false," +
                "\"hasRewardCalculation\":false}"));
            Assert.That(archiveJson.Contains("\"entryCount\":7"), Is.EqualTo(true));
            Assert.That(archiveJson.Contains("\"codexStringId\":\"narrative.opn_01_last_departure.codex.entry\""), Is.EqualTo(true));
            AssertForbidden(stateJson + archiveJson);
        }

        [Test]
        public void Factories_RejectInvalidInputsAndUnknownMissionFallsBackWithoutRawTruth()
        {
            var storyProgress = StoryProgressReadState.FromResponse(Response(
                highestStoryProgressId: "none",
                clearedOperationIds: new string[0],
                unlockedOperations: new[] { "future_operation" }));

            Assert.Throws<ArgumentNullException>(() => MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.Briefing,
                "opn_01_last_departure",
                null,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>()));
            Assert.Throws<ArgumentException>(() => MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.Briefing,
                " ",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>()));

            var state = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.CodexArchive,
                "future_operation",
                storyProgress,
                MissionNarrativeCatalog.CreateActOne(),
                new Dictionary<string, string>());

            Assert.That(state.MissionId, Is.EqualTo("future_operation"));
            Assert.That(state.DisplayName, Is.EqualTo("future_operation"));
            Assert.That(state.HasNarrativeDefinition, Is.EqualTo(false));
            Assert.That(state.IsServerListedUnlockedForDisplay, Is.EqualTo(true));
            Assert.That(state.Lines[0].StringId, Is.EqualTo("narrative.future_operation.codex.entry"));
            Assert.That(state.Lines[0].Text, Is.EqualTo("future_operation"));
            Assert.That(state.Lines[0].IsFallbackText, Is.EqualTo(true));
        }

        private static StoryProgressReadResponseDto Response(
            string highestStoryProgressId,
            string[] clearedOperationIds,
            string[] unlockedOperations)
        {
            return new StoryProgressReadResponseDto(
                "act_01_wasteland_railway",
                highestStoryProgressId,
                highestStoryProgressId == "none" ? null : highestStoryProgressId,
                0,
                clearedOperationIds,
                unlockedOperations,
                "act1.2026-08-27.001",
                "READY",
                "LOCALIZED",
                "LOCAL_FIXTURE_READY",
                "NONE");
        }

        private static void AssertForbidden(string json)
        {
            Assert.That(json.Contains("accountId"), Is.EqualTo(false));
            Assert.That(json.Contains("accessToken"), Is.EqualTo(false));
            Assert.That(json.Contains("refreshToken"), Is.EqualTo(false));
            Assert.That(json.Contains("secret"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardAmount"), Is.EqualTo(false));
            Assert.That(json.Contains("rewardMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("offlineReward"), Is.EqualTo(false));
            Assert.That(json.Contains("storyProgressMultiplier"), Is.EqualTo(false));
            Assert.That(json.Contains("firstClearEligible"), Is.EqualTo(false));
            Assert.That(json.Contains("economy"), Is.EqualTo(false));
            Assert.That(json.Contains("ledger"), Is.EqualTo(false));
            Assert.That(json.Contains("gachaProbability"), Is.EqualTo(false));
            Assert.That(json.Contains("rawRng"), Is.EqualTo(false));
            Assert.That(json.Contains("UnityWebRequest"), Is.EqualTo(false));
            Assert.That(json.Contains("RailChoice"), Is.EqualTo(false));
        }
    }
}
