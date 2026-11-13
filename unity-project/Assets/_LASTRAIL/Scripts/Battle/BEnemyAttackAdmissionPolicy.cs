using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Approved native policy only: not a server profile, manifest or enemy stat source.
 public sealed class BEnemyAttackAdmissionPolicy {
  public const string Version="unity.combat-b.attack-admission-policy.v03";
  public string Band {get;} public int GlobalSlots {get;} public int TargetSlots=>2; public int TargetGapMilliseconds=>600;
  BEnemyAttackAdmissionPolicy(string band,int slots){Band=band;GlobalSlots=slots;}
  public static BEnemyAttackAdmissionPolicy ForBand(string band){switch(band){case "EARLY":return new BEnemyAttackAdmissionPolicy(band,3);case "MIDDLE":return new BEnemyAttackAdmissionPolicy(band,4);case "LATE":return new BEnemyAttackAdmissionPolicy(band,5);default:throw new ArgumentException("b_admission_band");}}
  internal JObject Capture()=>new JObject{["version"]=Version,["band"]=Band,["globalSlots"]=GlobalSlots,["targetSlots"]=TargetSlots,["targetGapMilliseconds"]=TargetGapMilliseconds};
 }
}
