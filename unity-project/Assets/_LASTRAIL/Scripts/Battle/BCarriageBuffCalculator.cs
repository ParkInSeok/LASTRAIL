using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Pure modifier adapter; retained source belongs to Model, original stat/DEF/HP/effect/timer remain authoritative.
 public sealed class BCarriageBuffCalculator {
  readonly BCarriageBuffModel model;
  public BCarriageBuffCalculator(BCarriageBuffModel owner){model=owner??throw new ArgumentNullException(nameof(owner));}
  static CombatExactValue Factor(long bps)=>CombatExactValue.Create(10000+(BigInteger)bps,10000);
  public CombatNumericDefenseResult Damage(string eventId,JObject original){var source=model.Source(eventId);var input=(JObject)original.DeepClone();
   input["base"]=CombatExactValue.From(BuffWire.Object(original["base"])).Multiply(Factor((long)source["damageBps"])).Snapshot();return CombatNumericDefenseBudget.Resolve(input);}
  public JObject DamageSnapshot(string eventId,JObject original){var source=model.Source(eventId);var r=Damage(eventId,original).Snapshot();r["buffVersion"]=BCarriageBuffDefinitions.Version;r["group"]="B_DIRECT_DAMAGE_C";r["valueBps"]=source["damageBps"].DeepClone();return r;}
  public CombatExactValue Support(string eventId,CombatExactValue original){return original.Multiply(Factor((long)model.Source(eventId)["supportBps"]));}
  public JObject SupportSnapshot(string eventId,CombatExactValue original){var r=Support(eventId,original);return new JObject{["version"]=BCarriageBuffDefinitions.Version,["exact"]=r.Snapshot(),["projected"]=CombatExactValue.Text(r.Floor()),["valueBps"]=model.Source(eventId)["supportBps"].DeepClone()};}
  public CombatExactValue Time(string eventId,CombatExactValue original,long originalSameGroupHasteBps){BuffWire.Need(originalSameGroupHasteBps>=0&&originalSameGroupHasteBps<=BuffWire.Safe);var s=model.Source(eventId);long add=(string)s["ability"]=="ACTIVE"?(long)s["skillHasteBps"]:(string)s["ability"]=="INTERCEPT"?(long)s["interceptionHasteBps"]:0;
   return original.Multiply(CombatExactValue.Create(10000,10000+(BigInteger)originalSameGroupHasteBps+add));}
  // Only newly accepted timed ACTIVE. This calculates original-owner ticks; it never resets a running CD/phase.
  public long ActiveCooldownTicks(string eventId,long originalMicroseconds,long originalStepMicroseconds,
    IEnumerable<CombatTimingSource> originalSameGroupHaste,IEnumerable<CombatTimingSource> originalConstantCdr,CombatTimingSource originalEn08){
   var s=model.Source(eventId);BuffWire.Need((string)s["ability"]=="ACTIVE"&&(string)s["timing"]=="TIME_COOLDOWN","combat_b_timed_active_required");
   if(originalSameGroupHaste==null)throw new ArgumentNullException(nameof(originalSameGroupHaste));string identity;
   // Preserve the full source identity in Model. Map it opaquely into the old <=400-char timing-source boundary.
   using(var sha=SHA256.Create())identity="combat-b-skill-haste:"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes((string)s["sourceId"]))).Replace("-","").ToLowerInvariant();
   return CombatActionTiming.AutomaticSkillCooldownTicks(originalMicroseconds,originalStepMicroseconds,
    originalSameGroupHaste.Concat(new[]{new CombatTimingSource(identity,(long)s["skillHasteBps"])}),originalConstantCdr,originalEn08);
  }
  public static CombatExactValue DefenseStat(string ownerId,CombatExactValue originalFlat,JArray originalPercentSources,long capturedBuffFlat){
   BuffWire.Need(capturedBuffFlat>=0&&capturedBuffFlat<=BuffWire.Safe);var combined=CombatExactValue.Create(originalFlat.Numerator+(BigInteger)capturedBuffFlat*originalFlat.Denominator,originalFlat.Denominator);
   return CombatStatResolver.EquipmentDefense(ownerId,combined,originalPercentSources);
  }
 }
}