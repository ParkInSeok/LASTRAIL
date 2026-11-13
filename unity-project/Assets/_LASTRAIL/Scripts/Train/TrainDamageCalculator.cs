using System;

namespace LastRail.Train
{
    public static class TrainDamageCalculator
    {
        public const float CarriageDamageToTrainHpTransferRatio = 0.30f;

        public static float CalculateTransferredDamageToTrain(
            float carriageFinalDamage,
            float trainArmorDamageReductionRatio)
        {
            if (carriageFinalDamage < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(carriageFinalDamage), carriageFinalDamage, "Carriage final damage cannot be negative.");
            }

            if (trainArmorDamageReductionRatio < 0f || trainArmorDamageReductionRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(trainArmorDamageReductionRatio), trainArmorDamageReductionRatio, "Train armor damage reduction ratio must be between zero and one.");
            }

            return carriageFinalDamage * CarriageDamageToTrainHpTransferRatio;
        }

        public static float ApplyTransferredDamageToTrainHp(
            float trainTotalHp,
            float carriageFinalDamage,
            float trainArmorDamageReductionRatio)
        {
            if (trainTotalHp < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(trainTotalHp), trainTotalHp, "Train total HP cannot be negative.");
            }

            var transferredDamage = CalculateTransferredDamageToTrain(
                carriageFinalDamage,
                trainArmorDamageReductionRatio);

            return Math.Max(0f, trainTotalHp - transferredDamage);
        }
    }
}
