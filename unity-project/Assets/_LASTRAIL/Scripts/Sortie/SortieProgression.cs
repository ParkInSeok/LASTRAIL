using System;

namespace LastRail.Sortie
{
    public sealed class SortieProgression
    {
        public const int ActOneWaveCount = 15;

        private SortieProgression(
            string wavePlanId,
            SortieStage stage,
            int currentWaveNumber,
            int completedWaveCount)
        {
            WavePlanId = wavePlanId;
            Stage = stage;
            CurrentWaveNumber = currentWaveNumber;
            CompletedWaveCount = completedWaveCount;
        }

        public string WavePlanId { get; }
        public SortieStage Stage { get; }
        public int CurrentWaveNumber { get; }
        public int CompletedWaveCount { get; }
        public int TotalWaveCount { get { return ActOneWaveCount; } }
        public bool IsComplete { get { return Stage == SortieStage.Complete; } }

        public static SortieProgression StartActOne(string wavePlanId)
        {
            if (string.IsNullOrWhiteSpace(wavePlanId))
            {
                throw new ArgumentException("Wave plan ID cannot be empty.", nameof(wavePlanId));
            }

            return new SortieProgression(wavePlanId, SortieStage.Wave, 1, 0);
        }

        public SortieProgression CompleteCurrentWave()
        {
            if (Stage != SortieStage.Wave)
            {
                throw new InvalidOperationException("Current sortie stage is not a wave.");
            }

            var nextCompletedWaveCount = CompletedWaveCount + 1;
            if (nextCompletedWaveCount >= ActOneWaveCount)
            {
                return new SortieProgression(WavePlanId, SortieStage.FinalOperation, 0, ActOneWaveCount);
            }

            return new SortieProgression(
                WavePlanId,
                SortieStage.Wave,
                CurrentWaveNumber + 1,
                nextCompletedWaveCount);
        }

        public SortieProgression CompleteFinalOperation()
        {
            if (Stage != SortieStage.FinalOperation)
            {
                throw new InvalidOperationException("Current sortie stage is not the final operation.");
            }

            return new SortieProgression(WavePlanId, SortieStage.Complete, 0, ActOneWaveCount);
        }
    }
}
