using System;
using System.Numerics;

namespace LastRail.Battle
{
    // Approved local calculation constants, not a server profile ID or grant.
    // The existing projectile owner still owns reservations, order and terminal
    // state. Its adapter must provide the frozen world ray and valid observations.
    public static class OverflowProjectileBallistics
    {
        public const long SpeedMmPerSecond=36000;
        public const long RadiusMm=80;
        public const long MaximumDistanceMm=24000;
        const long MicrosecondsPerSecond=1000000;
        public static CombatExactValue DistanceAtMicroseconds(long elapsedCombatMicroseconds)
        {
            if(elapsedCombatMicroseconds<0)throw new ArgumentOutOfRangeException(nameof(elapsedCombatMicroseconds));
            var numerator=(BigInteger)elapsedCombatMicroseconds*SpeedMmPerSecond;
            var maximum=(BigInteger)MaximumDistanceMm*MicrosecondsPerSecond;
            return CombatExactValue.Create(BigInteger.Min(numerator,maximum),MicrosecondsPerSecond);
        }
        public static long MaximumLifetimeMicroseconds(long combatStepMicroseconds)
        {
            if(combatStepMicroseconds<=0)throw new ArgumentOutOfRangeException(nameof(combatStepMicroseconds));
            var numerator=(BigInteger)MaximumDistanceMm*MicrosecondsPerSecond;
            var denominator=(BigInteger)SpeedMmPerSecond*combatStepMicroseconds;
            var steps=(numerator+denominator-1)/denominator;
            return checked((long)(steps*combatStepMicroseconds));
        }
        // These are contact observations, not permission to damage arbitrary
        // targets. The caller passes only its reserved target/current generation
        // and authority-classified blockers, with the segment clipped to range.
        public static double? SweepReservedTarget(long[] start,long[] end,long[] targetStartFeet,long[] targetEndFeet,long heightMm,long targetRadiusMm)
            =>MiraSweepKernel.Capsule(start,end,targetStartFeet,targetEndFeet,heightMm,targetRadiusMm,RadiusMm);
        public static double? SweepBlocker(long[] start,long[] end,long[] blockerStart,long[] blockerEnd,long[] sizeMm)
            =>MiraSweepKernel.Box(start,end,blockerStart,blockerEnd,sizeMm,RadiusMm);
    }
}
