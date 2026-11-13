using System;

namespace LastRail.Tactics
{
    public static class TacticDraftRules
    {
        public const int FirstDraftWave = 3;
        public const int DraftWaveInterval = 3;
        public const int FinalDraftWave = 15;

        public static bool IsCommandTacticDraftWave(int waveNumber)
        {
            if (waveNumber < 1 || waveNumber > FinalDraftWave)
            {
                throw new ArgumentOutOfRangeException(nameof(waveNumber), waveNumber, "Wave number must be between 1 and 15.");
            }

            return waveNumber >= FirstDraftWave && waveNumber % DraftWaveInterval == 0;
        }
    }
}
