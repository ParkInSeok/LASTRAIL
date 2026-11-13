using System;
using LastRail.Core.Simulation;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CombatCheckpointPrimitiveTests
    {
        [Test]
        public void RandomRestoreContinuesExactSequence()
        {
            var original = new RunRandom(123u);
            for (var i = 0; i < 17; i++) original.NextUInt();
            var restored = RunRandom.FromState(original.ExportState());
            for (var i = 0; i < 100; i++) Assert.AreEqual(original.NextUInt(), restored.NextUInt());
        }

        [Test]
        public void InvalidRandomStateIsNotReseeded()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RunRandom.FromState(0));
            Assert.Throws<InvalidOperationException>(() => default(RunRandom).ExportState());
            Assert.AreNotEqual(0u, new RunRandom(0).ExportState());
        }

        [Test]
        public void RestoreKeepsWholePendingSteps()
        {
            var clock = new FixedStepDebt(TimeSpan.TicksPerMillisecond * 50);
            clock.Credit(TimeSpan.TicksPerMillisecond * 150);
            Assert.IsTrue(clock.TryConsumeStep());
            var restored = FixedStepDebt.Restore(clock.Capture());
            Assert.AreEqual(2, restored.PendingSteps);
            Assert.IsTrue(restored.TryConsumeStep());
            Assert.IsTrue(restored.TryConsumeStep());
            Assert.IsFalse(restored.TryConsumeStep());
            Assert.AreEqual(3, restored.CompletedSteps);
        }

        [Test]
        public void FramePartitionAndHalfTickRemainderSurviveRestore()
        {
            var split = new FixedStepDebt(7);
            split.SetSpeed(3);
            split.Credit(1);
            split = FixedStepDebt.Restore(split.Capture());
            for (var i = 0; i < 100; i++) split.Credit(1);
            var whole = new FixedStepDebt(7);
            whole.SetSpeed(3);
            whole.Credit(101);
            Assert.AreEqual(whole.Capture().DebtHalfTicks, split.Capture().DebtHalfTicks);
        }

        [Test]
        public void PauseAndSpeedChangePreserveExistingDebt()
        {
            var clock = new FixedStepDebt(10);
            clock.Credit(15);
            clock.SetSpeed(4);
            clock.SetPaused(true);
            clock.Credit(100);
            Assert.IsFalse(clock.TryConsumeStep());
            clock = FixedStepDebt.Restore(clock.Capture());
            Assert.IsTrue(clock.IsPaused);
            Assert.AreEqual(4, clock.SpeedHalfUnits);
            clock.SetPaused(false);
            clock.Credit(5);
            Assert.AreEqual(2, clock.PendingSteps);
            Assert.AreEqual(50, clock.Capture().DebtHalfTicks);
        }

        [Test]
        public void InvalidInputAndOverflowLeaveStateUnchanged()
        {
            var clock = new FixedStepDebt(10);
            clock.Credit(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Credit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed(0));
            Assert.Throws<OverflowException>(() => clock.Credit(long.MaxValue));
            Assert.AreEqual(2, clock.Capture().DebtHalfTicks);
            Assert.Throws<ArgumentOutOfRangeException>(() => FixedStepDebt.Restore(new FixedStepDebt.Snapshot(0, 0, 0, 2, false)));
            Assert.Throws<ArgumentOutOfRangeException>(() => FixedStepDebt.Restore(new FixedStepDebt.Snapshot(10, -1, 0, 2, false)));
            Assert.Throws<ArgumentOutOfRangeException>(() => FixedStepDebt.Restore(new FixedStepDebt.Snapshot(10, 0, -1, 2, false)));
            var exhausted = FixedStepDebt.Restore(new FixedStepDebt.Snapshot(10, 20, long.MaxValue, 2, false));
            Assert.Throws<OverflowException>(() => exhausted.TryConsumeStep());
            Assert.AreEqual(20, exhausted.Capture().DebtHalfTicks);
        }
    }
}
