using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Internal calculation seam after original action/stat/geometry gates. Never owns or changes HP/clock.
 internal interface IBDirectAttackBudget {
  CombatNumericDefenseResult Resolve(BDirectAttackInput.Entity source,BDirectAttackInput.Entity target,JObject originalNumeric);
 }
}