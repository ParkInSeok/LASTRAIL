using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunTacticStateRecorderTests
    {
        [Test]
        public void RecordConfirmedSelection_RecordsConfirmedSessionSelectionWithResolvedKind()
        {
            var session = TacticDraftSession.StartFromResolvedPool(
                3,
                new ResolvedTacticPool(new[] { "cmd_001", "cmd_002", "ult_001" }, true))
                .ConfirmSelection("ult_001");

            var state = RunTacticStateRecorder.RecordConfirmedSelection(
                RunTacticState.Empty(),
                session,
                Definitions());

            Assert.That(state.Selections.Count, Is.EqualTo(1));
            Assert.That(state.Selections[0].WaveNumber, Is.EqualTo(3));
            Assert.That(state.Selections[0].TacticId, Is.EqualTo("ult_001"));
            Assert.That(state.Selections[0].TacticKind, Is.EqualTo(TacticKind.Ultimate));
            Assert.That(state.Selections[0].DraftContainedUltimateTactic, Is.EqualTo(true));
        }

        [Test]
        public void RecordConfirmedSelection_RejectsUnconfirmedSession()
        {
            var session = TacticDraftSession.StartFromResolvedPool(
                3,
                new ResolvedTacticPool(new[] { "cmd_001", "cmd_002", "cmd_003" }, false));

            Assert.Throws<InvalidOperationException>(() => RunTacticStateRecorder.RecordConfirmedSelection(
                RunTacticState.Empty(),
                session,
                Definitions()));
        }

        [Test]
        public void RecordConfirmedSelection_RejectsUnknownSelectedTacticId()
        {
            var session = TacticDraftSession.StartFromResolvedPool(
                3,
                new ResolvedTacticPool(new[] { "cmd_001", "cmd_002", "cmd_404" }, false))
                .ConfirmSelection("cmd_404");

            Assert.Throws<InvalidOperationException>(() => RunTacticStateRecorder.RecordConfirmedSelection(
                RunTacticState.Empty(),
                session,
                Definitions()));
        }

        [Test]
        public void RecordConfirmedSelection_UsesRunTacticStateDuplicateWaveGuard()
        {
            var session = TacticDraftSession.StartFromResolvedPool(
                3,
                new ResolvedTacticPool(new[] { "cmd_001", "cmd_002", "cmd_003" }, false))
                .ConfirmSelection("cmd_001");
            var state = RunTacticStateRecorder.RecordConfirmedSelection(
                RunTacticState.Empty(),
                session,
                Definitions());

            Assert.Throws<InvalidOperationException>(() => RunTacticStateRecorder.RecordConfirmedSelection(
                state,
                session,
                Definitions()));
        }

        private static ResolvedTacticDefinition[] Definitions()
        {
            return new[]
            {
                new ResolvedTacticDefinition("cmd_001", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_002", TacticKind.Command),
                new ResolvedTacticDefinition("cmd_003", TacticKind.Command),
                new ResolvedTacticDefinition("ult_001", TacticKind.Ultimate)
            };
        }
    }
}
