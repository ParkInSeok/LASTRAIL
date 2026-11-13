using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Frozen START definition, not permission to execute or a source of world geometry.
 public sealed class OrdinaryBasicSource {
  public const string Version="combat.ordinary-basic.v1.20261026";
  public const string V2Version="combat.ordinary-basic.v2.20261026";
  public const string V3Version="combat.ordinary-basic.v3.20261026";
  public const string V5Version="combat.ordinary-basic.v5.20261107";
  static readonly IReadOnlyDictionary<string,string> Definitions=new Dictionary<string,string>{
   {Version,"dd99d2e4b4660a05b551bd0b226dbf3a7fb0fcf3abc26fbf3cb45345a556acf3"},
   {V2Version,"29857155020d715f504bae5a03ec4613fa93fdbf67e48944afb8f322d77e5923"},
   {V3Version,"6c0a9cc956222256db975c2441cef314a4d48a5a035656d9cb7f8b1a9882225c"},
   {V5Version,"8416c1a06c324b5c7ca350a40d140d89ef1ef2e3a8f49bc119b1517cea12ba02"}};
  public sealed class Profile {
   public string OperatorId{get;} public string AbilityId{get;} public string LaunchSource{get;}
   public string AttackForm{get;} public string MeleeRangeOrigin{get;} public string MeleeContactSource{get;}
   public long? MeleeMaximumDistanceMm{get;} public long? MeleeContactRadiusMm{get;} public int? MeleeAdjacentCars{get;}
   public IReadOnlyList<string> Modes{get;}
   public long OriginalIntervalMs{get;} public int AdjacentCars{get;} public long MaximumDistanceMm{get;}
   public long SpeedMmPerSecond{get;} public long RadiusMm{get;} public long MaximumFlightDistanceMm{get;}
   public long FlightNumeratorMs{get;} public long FlightDenominator{get;}
   public long? WindupMs{get;} public long? RecoveryMs{get;}
   internal Profile(JToken p){
    OperatorId=(string)p["operatorId"];AbilityId=(string)p["abilityId"];LaunchSource=(string)p["launchSource"];
    AttackForm=(string)p["attackForm"];
    if(p["melee"] is JObject melee){MeleeRangeOrigin=(string)melee["rangeOrigin"];MeleeContactSource=(string)melee["contactSource"];MeleeMaximumDistanceMm=(long)melee["maximumDistanceMm"];MeleeContactRadiusMm=(long)melee["contactRadiusMm"];MeleeAdjacentCars=(int)melee["physicalAdjacentCars"];}
    Modes=Array.AsReadOnly(p["modes"].Select(m=>(string)m).ToArray());OriginalIntervalMs=(long)p["originalBasicIntervalMs"];
    AdjacentCars=(int)p["range"]["physicalAdjacentCars"];MaximumDistanceMm=(long)p["range"]["maximumDistanceMm"];
    var b=p["ballistics"];SpeedMmPerSecond=(long)b["speedMmPerSecond"];RadiusMm=(long)b["radiusMm"];MaximumFlightDistanceMm=(long)b["maximumFlightDistanceMm"];
    FlightNumeratorMs=(long)b["maximumFlightMs"]["numerator"];FlightDenominator=(long)b["maximumFlightMs"]["denominator"];
    if(p["timing"] is JObject t){WindupMs=(long)t["windupMs"];RecoveryMs=(long)t["recoveryMs"];}
   }
   public (long Commit,long RecoveryEnd,long NextOpportunity) Boundaries(long startTick,CombatExactValue effectivePeriodMicroseconds,long stepMicroseconds){
    if(!WindupMs.HasValue||!RecoveryMs.HasValue)throw new InvalidOperationException("Original basic timing unavailable.");
    return CombatActionTiming.BasicOpportunityBoundaries(startTick,checked(OriginalIntervalMs*1000),checked(WindupMs.Value*1000),checked(RecoveryMs.Value*1000),effectivePeriodMicroseconds,stepMicroseconds);
   }
  }
  public string BasicHash{get;}
  public string SourceVersion{get;}
  public int? MiraPrimaryShotsPerOpportunity{get;}
  public IReadOnlyList<Profile> Profiles{get;}
  internal OrdinaryBasicSource(JToken raw,OrdinaryFiveStartInput owner){
   var source=raw as JObject;Need(source!=null);BDirectAttackInput.Fields(source,"packet","basicHash");
   var packet=source["packet"] as JObject;Need(packet!=null&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(packet))<=131072);
   BasicHash=(string)source["basicHash"];Need(BasicHash==Hash(packet));
   SourceVersion=(string)packet["version"];Need(SourceVersion!=null&&Definitions.ContainsKey(SourceVersion));
   Need((SourceVersion==V5Version)==(owner.KitSourceVersion==OrdinaryFiveStartInput.KitV3Version));
   Need((string)packet["accountId"]==owner.AccountId&&(string)packet["runId"]==owner.RunId
    &&(string)packet["originHash"]==owner.OriginHash&&(string)packet["kitHash"]==owner.KitHash);
   var definition=(JObject)packet.DeepClone();foreach(var key in new[]{"accountId","runId","originHash","kitHash"})definition.Remove(key);
   Need(Hash(definition)==Definitions[SourceVersion]);
   if(packet["miraEmissionPolicy"] is JObject emission)MiraPrimaryShotsPerOpportunity=(int)emission["primaryShotsPerOpportunity"];
   var profiles=packet["profiles"].Select(p=>new Profile(p)).ToArray();
   foreach(var p in profiles)Need(owner.Actors.Single(a=>a.OperatorId==p.OperatorId).BasicIntervalMs==p.OriginalIntervalMs);
   Profiles=Array.AsReadOnly(profiles);
  }
  static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_basic_source_invalid");}
 }
}
