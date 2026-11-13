namespace LastRail.Presentation.Battle
{
    public sealed class MobileCombatHudWaveState
    {
        private MobileCombatHudWaveState(bool hasWaveCounter, int currentWaveNumber, int totalWaveCount, string waveHudKey)
        {
            HasWaveCounter = hasWaveCounter;
            CurrentWaveNumber = currentWaveNumber;
            TotalWaveCount = totalWaveCount;
            WaveHudKey = waveHudKey;
        }

        public bool HasWaveCounter { get; }
        public int CurrentWaveNumber { get; }
        public int TotalWaveCount { get; }
        public string WaveHudKey { get; }

        public static MobileCombatHudWaveState Ordinary15Wave(int currentWaveNumber)
        {
            if (currentWaveNumber < 1 || currentWaveNumber > 15)
            {
                throw new System.ArgumentException("Wave number must be between 1 and 15.", nameof(currentWaveNumber));
            }

            return new MobileCombatHudWaveState(true, currentWaveNumber, 15, "ordinary_15_wave_hud");
        }

        public static MobileCombatHudWaveState HiddenForRaidBoss()
        {
            return new MobileCombatHudWaveState(false, 0, 0, "ordinary_wave_hud_hidden");
        }
    }
}
