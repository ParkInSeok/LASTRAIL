using System;
using System.Collections.Generic;
using LastRail.Narrative;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionShellScreenPresenter
    {
        private readonly IMissionShellScreenView view;

        public MissionShellScreenPresenter(IMissionShellScreenView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public MissionShellScreenViewState RenderMissionSelect(
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            Require(storyProgress, nameof(storyProgress));
            Require(narrativeCatalog, nameof(narrativeCatalog));

            var state = new MissionShellScreenViewState(
                MissionShellSurface.MissionSelect,
                "작전 선택",
                "서버가 열어 준 작전만 표시합니다.",
                new[] { "출격할 작전을 선택하세요." },
                Array.Empty<string>(),
                null,
                null,
                false,
                null,
                null,
                BuildMissionRows(storyProgress, narrativeCatalog),
                null,
                null,
                false,
                false,
                false);

            view.Render(state);
            return state;
        }

        public MissionShellScreenViewState RenderBriefing(
            string missionId,
            RunEncounterType encounterType,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog,
            IReadOnlyDictionary<string, string> localizedTextByStringId)
        {
            Require(storyProgress, nameof(storyProgress));
            Require(narrativeCatalog, nameof(narrativeCatalog));
            if (localizedTextByStringId == null)
            {
                throw new ArgumentNullException(nameof(localizedTextByStringId));
            }

            var shell = ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.Briefing,
                missionId,
                encounterType,
                storyProgress,
                narrativeCatalog);
            var narrative = MissionNarrativePresentationState.Create(
                MissionNarrativeSurfaceKind.Briefing,
                missionId,
                storyProgress,
                narrativeCatalog,
                localizedTextByStringId);
            var state = new MissionShellScreenViewState(
                MissionShellSurface.Briefing,
                shell.DisplayLabel,
                shell.NarrativeVersion,
                ExtractLineText(narrative, shell.MissionStartStringId),
                ExtractLineStringIds(narrative, shell.MissionStartStringId),
                FindLocalizedOrFallback(narrative, shell.MissionStartStringId, shell.MissionStartButtonCopy ?? shell.DisplayLabel),
                shell.MissionStartStringId,
                shell.IsServerListedUnlockedForDisplay,
                "작전 선택으로",
                null,
                null,
                null,
                null,
                false,
                false,
                false);

            view.Render(state);
            return state;
        }

        public MissionShellScreenViewState RenderRunStartResult(
            RunStartClientResult result,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            Require(result, nameof(result));
            Require(storyProgress, nameof(storyProgress));
            Require(narrativeCatalog, nameof(narrativeCatalog));

            var state = result.Response == null
                ? BuildRunStartErrorState(result)
                : BuildRunStartLoadingState(result.Response, storyProgress, narrativeCatalog);

            view.Render(state);
            return state;
        }

        private static IReadOnlyList<MissionShellMissionRowState> BuildMissionRows(
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            var rows = new List<MissionShellMissionRowState>();
            foreach (var definition in narrativeCatalog.Definitions)
            {
                rows.Add(new MissionShellMissionRowState(
                    definition.MissionId,
                    definition.DisplayName,
                    definition.MissionId == "raid_raider_king_engine"
                        ? RunEncounterType.RaidBoss
                        : RunEncounterType.Ordinary15Wave,
                    Contains(storyProgress.UnlockedOperations, definition.MissionId),
                    MissionNarrativeStringIds.ForMissionStart(definition.MissionId),
                    MissionNarrativeStringIds.ForLoadingHint(definition.MissionId),
                    definition.HasCodexEntry));
            }

            return rows.AsReadOnly();
        }

        private static MissionShellScreenViewState BuildRunStartLoadingState(
            RunStartResponseDto response,
            StoryProgressReadState storyProgress,
            MissionNarrativeCatalog narrativeCatalog)
        {
            var shell = ActOneMissionShellState.CreateMissionSurface(
                MissionShellSurface.LoadingHint,
                response.OperationId,
                response.EncounterType,
                storyProgress,
                narrativeCatalog);
            var nextBattleSurface = response.EncounterType == RunEncounterType.RaidBoss
                ? MissionShellSurface.BattleRaidBoss
                : MissionShellSurface.BattleOrdinary15Wave;
            var body = response.EncounterType == RunEncounterType.RaidBoss
                ? "보스전 로딩 중입니다. 서버가 준 raid/boss id만 표시합니다."
                : "15공세 작전 로딩 중입니다. 서버가 준 run metadata만 표시합니다.";

            return new MissionShellScreenViewState(
                MissionShellSurface.LoadingHint,
                shell.DisplayLabel,
                shell.NarrativeVersion,
                new[] { body },
                new[] { shell.LoadingHintStringId },
                "전투 화면으로",
                null,
                true,
                "작전 선택으로",
                nextBattleSurface,
                null,
                null,
                null,
                false,
                false,
                false);
        }

        private static MissionShellScreenViewState BuildRunStartErrorState(RunStartClientResult result)
        {
            var surface = result.RequiresAuthRefreshOrGuestLogin
                ? MissionShellSurface.AuthGate
                : MissionShellSurface.Result;
            var title = result.RequiresAuthRefreshOrGuestLogin
                ? "인증 필요"
                : "작전 시작 대기";
            var primaryAction = result.CanRetryWithSameIdempotencyKey
                ? "같은 요청으로 재시도"
                : result.RequiresAuthRefreshOrGuestLogin
                    ? "로그인 확인"
                    : "상태 다시 불러오기";
            var body = result.ErrorMessage ?? result.ErrorCode ?? "작전 시작 결과를 확인해야 합니다.";

            return new MissionShellScreenViewState(
                surface,
                title,
                null,
                new[] { body },
                Array.Empty<string>(),
                primaryAction,
                null,
                true,
                "작전 선택으로",
                null,
                null,
                result.ErrorCode,
                result.ErrorMessage,
                result.RequiresAuthRefreshOrGuestLogin,
                result.RequiresStateResync ||
                    result.Outcome == RunStartClientOutcome.OperationPreviewAlreadyConsumed,
                result.CanRetryWithSameIdempotencyKey);
        }

        private static IReadOnlyList<string> ExtractLineText(
            MissionNarrativePresentationState narrative,
            string omittedStringId)
        {
            var values = new List<string>();
            foreach (var line in narrative.Lines)
            {
                if (string.Equals(line.StringId, omittedStringId, StringComparison.Ordinal))
                {
                    continue;
                }

                values.Add(line.Text);
            }

            return values.AsReadOnly();
        }

        private static IReadOnlyList<string> ExtractLineStringIds(
            MissionNarrativePresentationState narrative,
            string omittedStringId)
        {
            var values = new List<string>();
            foreach (var line in narrative.Lines)
            {
                if (string.Equals(line.StringId, omittedStringId, StringComparison.Ordinal))
                {
                    continue;
                }

                values.Add(line.StringId);
            }

            return values.AsReadOnly();
        }

        private static string FindLocalizedOrFallback(
            MissionNarrativePresentationState narrative,
            string stringId,
            string fallback)
        {
            foreach (var line in narrative.Lines)
            {
                if (string.Equals(line.StringId, stringId, StringComparison.Ordinal))
                {
                    return line.Text;
                }
            }

            return fallback;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Require(object value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
