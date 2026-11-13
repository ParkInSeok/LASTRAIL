using System;

namespace LastRail.Core.Simulation
{
    // Exact accounting in half TimeSpan ticks. Callers retain simulation ownership:
    // consuming a step does not execute systems or acknowledge a durable checkpoint.
    public sealed class FixedStepDebt
    {
        public readonly struct Snapshot
        {
            public readonly long StepTicks;
            public readonly long DebtHalfTicks;
            public readonly long CompletedSteps;
            public readonly int SpeedHalfUnits;
            public readonly bool IsPaused;

            public Snapshot(long stepTicks, long debtHalfTicks, long completedSteps, int speedHalfUnits, bool isPaused)
            {
                StepTicks = stepTicks;
                DebtHalfTicks = debtHalfTicks;
                CompletedSteps = completedSteps;
                SpeedHalfUnits = speedHalfUnits;
                IsPaused = isPaused;
            }
        }

        private readonly long stepHalfTicks;
        private long debtHalfTicks;
        public long CompletedSteps { get; private set; }
        public int SpeedHalfUnits { get; private set; } = 2;
        public bool IsPaused { get; private set; }
        public long PendingSteps => debtHalfTicks / stepHalfTicks;

        public FixedStepDebt(long stepTicks)
        {
            if (stepTicks <= 0 || stepTicks > long.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(stepTicks));
            stepHalfTicks = stepTicks * 2;
        }

        public void SetSpeed(int speedHalfUnits)
        {
            if (speedHalfUnits != 2 && speedHalfUnits != 3 && speedHalfUnits != 4)
                throw new ArgumentOutOfRangeException(nameof(speedHalfUnits));
            SpeedHalfUnits = speedHalfUnits;
        }

        public void SetPaused(bool paused) => IsPaused = paused;

        public void Credit(long elapsedTimeSpanTicks)
        {
            if (elapsedTimeSpanTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTimeSpanTicks));
            if (IsPaused) return;
            debtHalfTicks = checked(debtHalfTicks + checked(elapsedTimeSpanTicks * SpeedHalfUnits));
        }

        public bool TryConsumeStep()
        {
            if (IsPaused || debtHalfTicks < stepHalfTicks) return false;
            var nextCompleted = checked(CompletedSteps + 1);
            debtHalfTicks -= stepHalfTicks;
            CompletedSteps = nextCompleted;
            return true;
        }

        public Snapshot Capture() => new Snapshot(stepHalfTicks / 2, debtHalfTicks, CompletedSteps, SpeedHalfUnits, IsPaused);

        public static FixedStepDebt Restore(Snapshot snapshot)
        {
            if (snapshot.DebtHalfTicks < 0) throw new ArgumentOutOfRangeException(nameof(snapshot));
            if (snapshot.CompletedSteps < 0) throw new ArgumentOutOfRangeException(nameof(snapshot));
            var clock = new FixedStepDebt(snapshot.StepTicks);
            clock.SetSpeed(snapshot.SpeedHalfUnits);
            clock.debtHalfTicks = snapshot.DebtHalfTicks;
            clock.CompletedSteps = snapshot.CompletedSteps;
            clock.IsPaused = snapshot.IsPaused;
            return clock;
        }
    }
}
