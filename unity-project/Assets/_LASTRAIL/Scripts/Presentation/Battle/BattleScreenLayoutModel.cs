using System;

namespace LastRail.Presentation.Battle
{
    public sealed class BattleScreenLayoutModel
    {
        public BattleScreenLayoutModel(float externalBattleHeightRatio, float currentTrainCarInteriorHeightRatio)
        {
            if (externalBattleHeightRatio <= 0f || currentTrainCarInteriorHeightRatio <= 0f)
            {
                throw new ArgumentException("Layout regions must have positive height ratios.");
            }

            var total = externalBattleHeightRatio + currentTrainCarInteriorHeightRatio;
            if (Math.Abs(total - 1f) > 0.0001f)
            {
                throw new ArgumentException("Layout height ratios must fill the portrait screen.");
            }

            ExternalBattleHeightRatio = externalBattleHeightRatio;
            CurrentTrainCarInteriorHeightRatio = currentTrainCarInteriorHeightRatio;
            ExternalBattleRegion = new BattleScreenLayoutRegion("externalBattle", 0, externalBattleHeightRatio);
            CurrentTrainCarInteriorRegion = new BattleScreenLayoutRegion("currentTrainCarInterior", 1, currentTrainCarInteriorHeightRatio);
        }

        public float ExternalBattleHeightRatio { get; }
        public float CurrentTrainCarInteriorHeightRatio { get; }
        public float TotalHeightRatio => ExternalBattleHeightRatio + CurrentTrainCarInteriorHeightRatio;
        public BattleScreenLayoutRegion ExternalBattleRegion { get; }
        public BattleScreenLayoutRegion CurrentTrainCarInteriorRegion { get; }

        public static BattleScreenLayoutModel CreatePortraitDefault()
        {
            return new BattleScreenLayoutModel(0.55f, 0.45f);
        }
    }
}
