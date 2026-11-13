using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace LastRail.Battle
{
    // Already eligible, owner/effect-qualified identity from a supported definition.
    // A source is immutable; this is a domain input, not a new Server DTO.
    public sealed class CombatTimingSource
    {
        public string SourceId { get; }
        public long ValueBps { get; }
        public CombatTimingSource(string sourceId,long valueBps) {
            if(string.IsNullOrWhiteSpace(sourceId)||sourceId.Length>400||valueBps<0||valueBps>9007199254740991)
                throw new ArgumentException("Invalid resolved timing source.");
            SourceId=sourceId;ValueBps=valueBps;
        }
    }
    public static class CombatActionTiming
    {
        // Local arithmetic policy label only. No admission/profile/schema is issued here.
        public const string Policy="equipment-timing.13-v08-v09";
        const int Scale=10000,NonEquipmentSpeedCap=2500,SlowCap=3000,ConstantCdrCap=4000,En08Maximum=2300,MaxSources=1024;
        sealed class Identity { internal string Group;internal long Value; }
        // BASIC-REINA-SERA-v1.20261026: snapshot the supplied effective period at opportunity start.
        // Cumulative boundaries preserve one cycle; this does not authorize a cast or order equal-tick events.
        public static (long Commit,long RecoveryEnd,long NextOpportunity) BasicOpportunityBoundaries(
            long startTick,long originalPeriodMicroseconds,long originalWindupMicroseconds,long originalRecoveryMicroseconds,
            CombatExactValue effectivePeriodMicroseconds,long stepMicroseconds)
        {
            if(startTick<0||originalPeriodMicroseconds<=0||originalWindupMicroseconds<0||originalRecoveryMicroseconds<0
                ||originalWindupMicroseconds>originalPeriodMicroseconds||originalRecoveryMicroseconds>originalPeriodMicroseconds-originalWindupMicroseconds
                ||effectivePeriodMicroseconds==null||effectivePeriodMicroseconds.Numerator<=0||stepMicroseconds<=0)
                throw new ArgumentException("Invalid proportional basic opportunity timing.");
            long At(long originalOffset) {
                var numerator=effectivePeriodMicroseconds.Numerator*originalOffset;
                var denominator=effectivePeriodMicroseconds.Denominator*originalPeriodMicroseconds*stepMicroseconds;
                return checked(startTick+(long)((numerator+denominator-1)/denominator));
            }
            return (At(originalWindupMicroseconds),At(originalWindupMicroseconds+originalRecoveryMicroseconds),At(originalPeriodMicroseconds));
        }
        public static long BasicAttackPeriodTicks(long baseDurationMicroseconds,long stepMicroseconds,
            IEnumerable<CombatTimingSource> equipment,IEnumerable<CombatTimingSource> nonEquipment,IEnumerable<CombatTimingSource> slows)
        {
            var period=Duration(baseDurationMicroseconds,stepMicroseconds);
            var identities=new Dictionary<string,Identity>(StringComparer.Ordinal);
            var e=Sum(Unique(equipment,"EQUIPMENT_ATTACK_SPEED",identities));
            var n=BigInteger.Min(Sum(Unique(nonEquipment,"NON_EQUIPMENT_ATTACK_SPEED",identities)),NonEquipmentSpeedCap);
            var slow=BigInteger.Min(Strongest(Unique(slows,"ATTACK_SLOW",identities)),SlowCap);
            return CeilingTicks(period.Multiply(CombatExactValue.Create(Scale,Scale+e))
                .Multiply(CombatExactValue.Create(Scale,Scale+n)).Multiply(CombatExactValue.Create(Scale,Scale-slow)));
        }
        // Period increase is distinct from attack-speed reduction; sample only at a new reservation.
        public static CombatExactValue BasicPeriodWithNyxDelay(CombatExactValue period,IEnumerable<CombatTimingSource> sources){
            if(period==null||period.Numerator<=0)throw new ArgumentException("Positive resolved basic period required.");
            var strongest=Strongest(Unique(sources,"NYX_BASIC_PERIOD_INCREASE",new Dictionary<string,Identity>(StringComparer.Ordinal)));
            if(strongest>2500)throw new ArgumentException("Nyx period increase exceeds the approved maximum.");
            return period.Multiply(CombatExactValue.Create(Scale+strongest,Scale));
        }
        public static long AutomaticSkillCooldownTicks(long baseDurationMicroseconds,long stepMicroseconds,
            IEnumerable<CombatTimingSource> equipmentHaste,IEnumerable<CombatTimingSource> constantReductions,CombatTimingSource en08)
        {
            var period=Duration(baseDurationMicroseconds,stepMicroseconds);
            var identities=new Dictionary<string,Identity>(StringComparer.Ordinal);
            var haste=Sum(Unique(equipmentHaste,"EQUIPMENT_SKILL_HASTE",identities));
            var cdr=BigInteger.Min(Sum(Unique(constantReductions,"CONSTANT_SKILL_CDR",identities)),ConstantCdrCap);
            long enchant=0;
            if(en08!=null) {
                if(en08.ValueBps>En08Maximum) throw new ArgumentException("Unsupported EN08 cooldown reduction.");
                Register(en08,"EN08_NEW_COOLDOWN",identities);enchant=en08.ValueBps;
            }
            return CeilingTicks(period.Multiply(CombatExactValue.Create(Scale,Scale+haste))
                .Multiply(CombatExactValue.Create(Scale-cdr,Scale)).Multiply(CombatExactValue.Create(Scale-enchant,Scale)));
        }
        // Fixed burst/sentry/windup/recovery periods never consume equipment speed or haste.
        public static long FixedPeriodTicks(long baseDurationMicroseconds,long stepMicroseconds)=>CeilingTicks(Duration(baseDurationMicroseconds,stepMicroseconds));
        static CombatExactValue Duration(long duration,long step) {
            if(duration<=0||step<=0) throw new ArgumentException("Explicit positive duration and logical step required.");
            return CombatExactValue.Create(duration,step);
        }
        static long CeilingTicks(CombatExactValue value) {
            var ticks=(value.Numerator+value.Denominator-1)/value.Denominator;
            return checked((long)BigInteger.Max(BigInteger.One,ticks));
        }
        static IEnumerable<BigInteger> Unique(IEnumerable<CombatTimingSource> sources,string group,Dictionary<string,Identity> identities) {
            if(sources==null) throw new ArgumentNullException(nameof(sources));
            var copy=sources.Take(MaxSources+1).ToArray();
            if(copy.Length>MaxSources) throw new ArgumentException("Timing source capacity exceeded.");
            var values=new List<BigInteger>();
            foreach(var source in copy) {
                if(source==null) throw new ArgumentException("Missing timing source.");
                if(Register(source,group,identities)) values.Add(source.ValueBps);
            }
            return values;
        }
        static bool Register(CombatTimingSource source,string group,Dictionary<string,Identity> identities) {
            if(identities.TryGetValue(source.SourceId,out var old)) {
                if(old.Group!=group||old.Value!=source.ValueBps) throw new ArgumentException("Timing source conflict.");
                return false;
            }
            identities.Add(source.SourceId,new Identity{Group=group,Value=source.ValueBps});return true;
        }
        static BigInteger Sum(IEnumerable<BigInteger> values)=>values.Aggregate(BigInteger.Zero,(sum,value)=>sum+value);
        static BigInteger Strongest(IEnumerable<BigInteger> values)=>values.Aggregate(BigInteger.Zero,BigInteger.Max);
    }
}
