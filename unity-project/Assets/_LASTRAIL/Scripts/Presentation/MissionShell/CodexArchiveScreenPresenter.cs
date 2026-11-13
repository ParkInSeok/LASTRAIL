using System;
using System.Collections.Generic;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    public sealed class CodexArchiveScreenPresenter
    {
        private const int DefaultCodexEntryMaxCharacters = 180;

        private readonly ICodexArchiveScreenView view;

        public CodexArchiveScreenPresenter(ICodexArchiveScreenView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public CodexArchiveScreenViewState Render(
            CodexArchivePresentationState archive,
            NarrativeLocalizationReadinessAudit readinessAudit)
        {
            if (archive == null)
            {
                throw new ArgumentNullException(nameof(archive));
            }

            if (readinessAudit == null)
            {
                throw new ArgumentNullException(nameof(readinessAudit));
            }

            var state = new CodexArchiveScreenViewState(
                "기록 보관소",
                "클리어했거나 서버가 열어 준 기록만 표시합니다.",
                archive.NarrativeVersion,
                BuildEntries(archive, readinessAudit),
                CountCodexOverflowSlots(archive, readinessAudit),
                canNavigateToMissionSelect: true,
                canNavigateToResult: true,
                canNavigateToRewardReview: true);

            view.Render(state);
            return state;
        }

        private static IReadOnlyList<CodexArchiveScreenEntryViewState> BuildEntries(
            CodexArchivePresentationState archive,
            NarrativeLocalizationReadinessAudit readinessAudit)
        {
            var entries = new List<CodexArchiveScreenEntryViewState>();
            foreach (var entry in archive.Entries)
            {
                var slot = readinessAudit.FindSlot(entry.CodexStringId);
                var characterCount = slot == null ? entry.CodexEntryText.Length : slot.CharacterCount;
                var maxCharacterCount = slot == null ? DefaultCodexEntryMaxCharacters : slot.MaxCharacterCount;
                var exceeds = slot == null ? entry.CodexEntryText.Length > DefaultCodexEntryMaxCharacters : slot.ExceedsRecommendedLength;

                entries.Add(new CodexArchiveScreenEntryViewState(
                    entry.MissionId,
                    entry.DisplayName,
                    entry.CodexStringId,
                    entry.CodexEntryText,
                    entry.IsFallbackText,
                    entry.IsServerListedClearedForDisplay,
                    entry.IsServerListedUnlockedForDisplay,
                    entry.CanOpenForDisplay,
                    characterCount,
                    maxCharacterCount,
                    exceeds,
                    ResolveReadinessStatus(entry, slot, exceeds)));
            }

            return entries.AsReadOnly();
        }

        private static int CountCodexOverflowSlots(
            CodexArchivePresentationState archive,
            NarrativeLocalizationReadinessAudit readinessAudit)
        {
            var count = 0;
            foreach (var entry in archive.Entries)
            {
                var slot = readinessAudit.FindSlot(entry.CodexStringId);
                if (slot != null && slot.ExceedsRecommendedLength)
                {
                    count++;
                }
            }

            return count;
        }

        private static string ResolveReadinessStatus(
            CodexArchiveEntryPresentationState entry,
            NarrativeLocalizationReadinessSlot slot,
            bool exceedsRecommendedLength)
        {
            if (!entry.CanOpenForDisplay)
            {
                return "LOCKED";
            }

            if (exceedsRecommendedLength)
            {
                return "OVERFLOW";
            }

            if (entry.IsFallbackText || slot == null || !slot.IsAvailable)
            {
                return "FALLBACK";
            }

            return "READY";
        }
    }
}
