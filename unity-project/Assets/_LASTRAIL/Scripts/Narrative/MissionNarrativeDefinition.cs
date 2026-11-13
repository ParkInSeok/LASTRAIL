using System;
using System.Collections.Generic;
using System.Text;

namespace LastRail.Narrative
{
    public sealed class MissionNarrativeDefinition
    {
        public MissionNarrativeDefinition(
            string missionId,
            string displayName,
            string briefingSummary,
            IEnumerable<string> briefingLines,
            IEnumerable<string> commanderResponseCandidates,
            string missionStartButtonCopy,
            string loadingHint,
            IEnumerable<string> openingCallouts,
            IEnumerable<string> tutorialAdjacentLines,
            IEnumerable<string> escalationCallouts,
            IEnumerable<string> optionalObjectiveCallouts,
            IEnumerable<string> finalWaveCallouts,
            IEnumerable<string> victoryLines,
            IEnumerable<string> failureLines,
            string resultTitle,
            string resultSummary,
            string firstClearRewardFraming,
            string repeatClearRewardFraming,
            string codexEntry)
        {
            MissionId = RequireText(missionId, nameof(missionId));
            DisplayName = RequireText(displayName, nameof(displayName));
            BriefingSummary = RequireText(briefingSummary, nameof(briefingSummary));
            BriefingLines = CopyLines(briefingLines, nameof(briefingLines));
            CommanderResponseCandidates = CopyLines(commanderResponseCandidates, nameof(commanderResponseCandidates));
            MissionStartButtonCopy = RequireText(missionStartButtonCopy, nameof(missionStartButtonCopy));
            LoadingHint = RequireText(loadingHint, nameof(loadingHint));
            OpeningCallouts = CopyLines(openingCallouts, nameof(openingCallouts));
            TutorialAdjacentLines = CopyLines(tutorialAdjacentLines, nameof(tutorialAdjacentLines));
            EscalationCallouts = CopyLines(escalationCallouts, nameof(escalationCallouts));
            OptionalObjectiveCallouts = CopyLines(optionalObjectiveCallouts, nameof(optionalObjectiveCallouts));
            FinalWaveCallouts = CopyLines(finalWaveCallouts, nameof(finalWaveCallouts));
            VictoryLines = CopyLines(victoryLines, nameof(victoryLines));
            FailureLines = CopyLines(failureLines, nameof(failureLines));
            ResultTitle = RequireText(resultTitle, nameof(resultTitle));
            ResultSummary = RequireText(resultSummary, nameof(resultSummary));
            FirstClearRewardFraming = RequireText(firstClearRewardFraming, nameof(firstClearRewardFraming));
            RepeatClearRewardFraming = RequireText(repeatClearRewardFraming, nameof(repeatClearRewardFraming));
            CodexEntry = OptionalText(codexEntry);
        }

        public string MissionId { get; }
        public string DisplayName { get; }
        public string BriefingSummary { get; }
        public IReadOnlyList<string> BriefingLines { get; }
        public IReadOnlyList<string> CommanderResponseCandidates { get; }
        public string MissionStartButtonCopy { get; }
        public string LoadingHint { get; }
        public IReadOnlyList<string> OpeningCallouts { get; }
        public IReadOnlyList<string> TutorialAdjacentLines { get; }
        public IReadOnlyList<string> EscalationCallouts { get; }
        public IReadOnlyList<string> OptionalObjectiveCallouts { get; }
        public IReadOnlyList<string> FinalWaveCallouts { get; }
        public IReadOnlyList<string> VictoryLines { get; }
        public IReadOnlyList<string> FailureLines { get; }
        public string ResultTitle { get; }
        public string ResultSummary { get; }
        public string FirstClearRewardFraming { get; }
        public string RepeatClearRewardFraming { get; }
        public string CodexEntry { get; }
        public bool HasCodexEntry => CodexEntry != null;

        public string FlattenTextForValidation()
        {
            var builder = new StringBuilder();
            Append(builder, MissionId);
            Append(builder, DisplayName);
            Append(builder, BriefingSummary);
            Append(builder, MissionStartButtonCopy);
            Append(builder, LoadingHint);
            Append(builder, ResultTitle);
            Append(builder, ResultSummary);
            Append(builder, FirstClearRewardFraming);
            Append(builder, RepeatClearRewardFraming);
            Append(builder, CodexEntry);
            Append(builder, BriefingLines);
            Append(builder, CommanderResponseCandidates);
            Append(builder, OpeningCallouts);
            Append(builder, TutorialAdjacentLines);
            Append(builder, EscalationCallouts);
            Append(builder, OptionalObjectiveCallouts);
            Append(builder, FinalWaveCallouts);
            Append(builder, VictoryLines);
            Append(builder, FailureLines);
            return builder.ToString();
        }

        private static IReadOnlyList<string> CopyLines(IEnumerable<string> lines, string name)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(name);
            }

            var copied = new List<string>();
            foreach (var line in lines)
            {
                copied.Add(RequireText(line, name));
            }

            return copied.AsReadOnly();
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", name);
            }

            return value.Trim();
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void Append(StringBuilder builder, IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                Append(builder, value);
            }
        }

        private static void Append(StringBuilder builder, string value)
        {
            if (value == null)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(value);
        }
    }
}
