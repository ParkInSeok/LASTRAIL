using System;
using System.Collections.Generic;
using System.Linq;

namespace LastRail.Battle
{
    // Borrows the existing HP owner; the ability owner supplies only targets in
    // its original range that permit this healing. No global roster or UI state.
    public sealed class CombatHealingTarget
    {
        public string Id {get;}
        public int Slot {get;}
        internal CombatVitals Health {get;}
        public CombatHealingTarget(string id,int slot,CombatVitals health)
        {
            if(string.IsNullOrWhiteSpace(id)||slot<0)throw new ArgumentException("Invalid healing target");
            Id=id;Slot=slot;Health=health??throw new ArgumentNullException(nameof(health));
        }
    }
    public static class CombatHealingTargetSelection
    {
        // allowManualFullHealth must be resolved by the original command owner:
        // supported overflow, manual input, cast readiness and a legal enemy.
        // This selector neither spends charge nor makes AUTO eligible.
        public static CombatHealingTarget Select(IEnumerable<CombatHealingTarget> eligibleTargets,bool allowManualFullHealth,string previousValidTargetId=null)
        {
            if(eligibleTargets==null)throw new ArgumentNullException(nameof(eligibleTargets));
            var targets=eligibleTargets.ToArray();
            if(targets.Any(t=>t==null)||targets.Select(t=>t.Id).Distinct(StringComparer.Ordinal).Count()!=targets.Length)throw new ArgumentException("Invalid healing target set");
            var living=targets.Where(t=>t.Health.Hp>0).ToArray();
            var injured=living.Where(t=>t.Health.Hp<t.Health.MaxHp).OrderBy(t=>t,Comparer<CombatHealingTarget>.Create((a,b)=>
            {
                int n=((long)a.Health.Hp*b.Health.MaxHp).CompareTo((long)b.Health.Hp*a.Health.MaxHp);
                return n!=0?n:a.Slot!=b.Slot?a.Slot.CompareTo(b.Slot):StringComparer.Ordinal.Compare(a.Id,b.Id);
            })).FirstOrDefault();
            if(injured!=null||!allowManualFullHealth)return injured;
            return living.FirstOrDefault(t=>t.Id==previousValidTargetId)??living.OrderBy(t=>t.Slot).ThenBy(t=>t.Id,StringComparer.Ordinal).FirstOrDefault();
        }
    }
}
