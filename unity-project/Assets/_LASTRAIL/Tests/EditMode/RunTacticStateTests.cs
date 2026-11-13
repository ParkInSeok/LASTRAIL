using System;
using NUnit.Framework;
using LastRail.Run;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class RunTacticStateTests
    {
        [Test]
        public void Empty_StartsWithNoRecordedSelections()
        {
            var state = RunTacticState.Empty();

            Assert.That(state.Selections.Count, Is.EqualTo(0));
            Assert.That(state.HasSelectionAtWave(3), Is.EqualTo(false));
        }

        [Test]
        public void RecordSelection_StoresSelectionEventForDraftWave()
        {
            var state = RunTacticState.Empty().RecordSelection(
                waveNumber: 3,
                tacticId: "cmd_001",
                tacticKind: TacticKind.Command,
                draftContainedUltimateTactic: false);

            Assert.That(state.Selections.Count, Is.EqualTo(1));
            Assert.That(state.Selections[0].WaveNumber, Is.EqualTo(3));
            Assert.That(state.Selections[0].TacticId, Is.EqualTo("cmd_001"));
            Assert.That(state.Selections[0].TacticKind, Is.EqualTo(TacticKind.Command));
            Assert.That(state.Selections[0].DraftContainedUltimateTactic, Is.EqualTo(false));
            Assert.That(state.HasSelectionAtWave(3), Is.EqualTo(true));
        }

        [Test]
        public void RecordSelection_StoresUltimateSelectionAndDraftUltimatePresence()
        {
            var state = RunTacticState.Empty().RecordSelection(
                waveNumber: 6,
                tacticId: "ult_001",
                tacticKind: TacticKind.Ultimate,
                draftContainedUltimateTactic: true);

            Assert.That(state.Selections[0].TacticKind, Is.EqualTo(TacticKind.Ultimate));
            Assert.That(state.Selections[0].DraftContainedUltimateTactic, Is.EqualTo(true));
        }

        [Test]
        public void RecordSelection_RejectsDuplicateSelectionForSameDraftWave()
        {
            var state = RunTacticState.Empty().RecordSelection(3, "cmd_001", TacticKind.Command, false);

            Assert.Throws<InvalidOperationException>(() => state.RecordSelection(3, "cmd_002", TacticKind.Command, false));
        }

        [Test]
        public void RecordSelection_RejectsNonDraftWave()
        {
            Assert.Throws<InvalidOperationException>(() => RunTacticState.Empty().RecordSelection(4, "cmd_001", TacticKind.Command, false));
        }

        [Test]
        public void RecordSelection_CopiesEventListBetweenStateInstances()
        {
            var first = RunTacticState.Empty();
            var second = first.RecordSelection(3, "cmd_001", TacticKind.Command, false);
            var third = second.RecordSelection(6, "ult_001", TacticKind.Ultimate, true);

            Assert.That(first.Selections.Count, Is.EqualTo(0));
            Assert.That(second.Selections.Count, Is.EqualTo(1));
            Assert.That(third.Selections.Count, Is.EqualTo(2));
            Assert.That(third.Selections[1].WaveNumber, Is.EqualTo(6));
        }

        [Test]
        public void TacticSelectionEvent_RejectsEmptyTacticId()
        {
            Assert.Throws<ArgumentException>(() => new TacticSelectionEvent(3, " ", TacticKind.Command, false));
        }
    }
}
