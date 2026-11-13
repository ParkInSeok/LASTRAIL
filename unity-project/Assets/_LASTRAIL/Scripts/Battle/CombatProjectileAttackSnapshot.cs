using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle
{
    /// <summary>
    /// Pure internal arithmetic contract; not a server effect snapshot or a projectile lifetime.
    /// completedAttack is exact pre-defense D: ATK, skill/critical decision, original opportunity
    /// transformations and attack-wide offense have ALREADY been applied by their owners.
    /// appliedOffense records the exact numeric offense sources already included in D, not a
    /// second list to multiply. penetration is frozen attack-side. No RNG or live actor is read.
    /// The caller must distinguish those sources from target conditions not yet included in D.
    /// </summary>
    public sealed class CombatProjectileAttackSnapshot
    {
        public const string Version = "combat.projectile-budget.v1.20261015";
        public const string MitigationArrivalVersion = "combat.projectile-arrival.mitigation.v2.20261107";
        readonly CombatExactValue normalizedAttack;
        readonly JArray appliedOffense, penetration;

        CombatProjectileAttackSnapshot(CombatExactValue attack, JArray offense, JArray penetration)
        {
            normalizedAttack = attack;
            appliedOffense = offense;
            this.penetration = penetration;
        }

        public static CombatProjectileAttackSnapshot Capture(JObject launch)
        {
            Fields(launch, "version", "completedAttack", "appliedOffense", "penetration");
            var completed = CombatExactValue.From(launch["completedAttack"] as JObject);
            var offense = launch["appliedOffense"] as JArray;
            var penetration = launch["penetration"] as JArray;
            // Reuse all original source identity/range/group/cap checks, including cross-group
            // collisions. At zero DEF this yields only the offense factor already included in D.
            var unit = Numeric(CombatExactValue.Create(1, 1), CombatExactValue.Create(0, 1),
                offense, penetration, new JArray(), EmptyReductions());
            var factor = CombatExactValue.From((JObject)CombatNumericDefenseBudget.Resolve(unit).Snapshot()["exact"]);
            // Normalize without rounding; combining target conditions must retain the original
            // same-group caps (e.g. launch outgoing20% + target20% is30%, not40% or44%).
            var normalized = completed.Multiply(CombatExactValue.Create(factor.Denominator, factor.Numerator));
            return new CombatProjectileAttackSnapshot(normalized,
                (JArray)offense.DeepClone(), (JArray)penetration.DeepClone());
        }

        /// <summary>
        /// Arrival input: version, defense, effects (target-specific, NOT yet included in D),
        /// weakening, reductions. Authority supplies eligible current target values.
        /// Does not inspect HP, authorize a hit, consume buffs/opportunities, or apply damage.
        /// </summary>
        public CombatProjectileReferenceDamage Evaluate(JObject arrival) => EvaluateAllocated(arrival, 10000);
        public CombatProjectileReferenceDamage EvaluateWithMitigationRateReduction(JObject arrival,JArray sources) => EvaluateAllocated(arrival,10000,sources??throw new ArgumentNullException(nameof(sources)));

        /// <summary>Allocate a branch of exact pre-defense D, then evaluate that target once.</summary>
        public CombatProjectileReferenceDamage EvaluatePiercing(JObject arrival, int coefficientBps) =>
            EvaluateAllocated(arrival, coefficientBps);

        CombatProjectileReferenceDamage EvaluateAllocated(JObject arrival, int coefficientBps,JArray mitigationRateSources=null)
        {
            Coefficient(coefficientBps);
            if((string)arrival?["version"]==MitigationArrivalVersion){
                if(mitigationRateSources!=null)throw new ArgumentException("Current mitigation must be supplied exactly once.");
                BDirectAttackInput.Fields(arrival,"version","defense","effects","weakening","reductions","mitigationRateReduction");
                mitigationRateSources=arrival["mitigationRateReduction"] as JArray??throw new ArgumentException("Explicit current mitigation sources required.");
            }else Fields(arrival, "version", "defense", "effects", "weakening", "reductions");
            if (!(arrival["effects"] is JArray targetEffects))
                throw new ArgumentException("Explicit target offense sources required.");
            var effects = (JArray)appliedOffense.DeepClone();
            foreach (var effect in targetEffects) effects.Add(effect.DeepClone());
            var basis = normalizedAttack.Multiply(CombatExactValue.Create(coefficientBps, 10000));
            var input = Numeric(basis, CombatExactValue.From(arrival["defense"] as JObject), effects,
                penetration, arrival["weakening"], arrival["reductions"]);
            return new CombatProjectileReferenceDamage(mitigationRateSources==null?CombatNumericDefenseBudget.Resolve(input):CombatNumericDefenseBudget.ResolveWithMitigationRateReduction(input,mitigationRateSources));
        }

        static JObject Numeric(CombatExactValue basis, CombatExactValue defense, JToken offense,
            JToken penetration, JToken weakening, JToken reductions) => new JObject {
                ["version"] = CombatNumericDefenseBudget.Version,
                ["base"] = basis.Snapshot(), ["defense"] = defense.Snapshot(),
                ["effects"] = offense?.DeepClone(), ["penetration"] = penetration?.DeepClone(),
                ["weakening"] = weakening?.DeepClone(), ["reductions"] = reductions?.DeepClone()
            };
        static JObject EmptyReductions() => new JObject {
            ["equipment"] = new JArray(), ["guardStance"] = new JArray(), ["independent"] = new JArray()
        };
        internal static void Coefficient(int bps)
        {
            if (bps < 0 || bps > 10000) throw new ArgumentOutOfRangeException(nameof(bps));
        }
        static void Fields(JObject input, params string[] fields)
        {
            if (input == null || input.Count != fields.Length || fields.Any(f => input[f] == null)
                || input["version"]?.Type != JTokenType.String || (string)input["version"] != Version)
                throw new ArgumentException("Explicit projectile budget input/version required.");
        }
    }

    /// <summary>
    /// Immutable reference F before shield/HP. Scaling never applies attack, critical, DEF,
    /// or reductions again. Keep exact arithmetic through the coefficient and floor once.
    /// This is an arithmetic result, not an accepted hit or a reusable original opportunity.
    /// </summary>
    public sealed class CombatProjectileReferenceDamage
    {
        readonly CombatNumericDefenseResult result;
        readonly CombatExactValue exact;
        internal CombatProjectileReferenceDamage(CombatNumericDefenseResult result)
        {
            this.result = result;
            exact = CombatExactValue.From((JObject)result.Snapshot()["exact"]);
        }
        public int ScaleAndFloor(int coefficientBps)
        {
            CombatProjectileAttackSnapshot.Coefficient(coefficientBps);
            return checked((int)exact.Multiply(CombatExactValue.Create(coefficientBps, 10000)).Floor());
        }
        public JObject Snapshot()
        {
            var snapshot = result.Snapshot();
            snapshot["version"] = CombatProjectileAttackSnapshot.Version;
            return snapshot;
        }
    }
}
