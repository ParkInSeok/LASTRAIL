using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.MissionShell {
 // A display estimate schedules one authoritative ARRIVE; it never grants arrival or combat entry.
 public sealed class OrdinaryTravelDisplayPlan {
  public long DistanceMm{get;}
  public int MinimumBps{get;} public int MaximumBps{get;}
  readonly long stepTicks,baseSpeed;
  OrdinaryTravelDisplayPlan(JObject definition,string edge){
   DistanceMm=(long)((JArray)definition["boundaries"]).Single(x=>(string)x["edgeId"]==edge)["distanceMm"];
   var c=definition["configuration"];MinimumBps=(int)c["throttleMinimumBps"];MaximumBps=(int)c["throttleMaximumBps"];baseSpeed=(long)c["baseSpeedMillimetersPerSecond"];stepTicks=(long)definition["stepTicks"];
  }
  public static OrdinaryTravelDisplayPlan Read(JObject source,JObject binding){
   if(source==null||binding==null)throw new ArgumentException("Original travel display source is missing.");
   BArrivalBoundaryTravel.Create(source,(string)source["sourceHash"],binding);
   return new OrdinaryTravelDisplayPlan((JObject)source["definition"],(string)binding["edgeId"]);
  }
  public decimal SecondsUntilBoundary(long distance,long remainder,int throttle){
   if(distance<0||remainder<0||remainder>=100000000000L||throttle<MinimumBps||throttle>MaximumBps)throw new ArgumentException("Travel display state is invalid.");
   decimal remaining=Math.Max(0,DistanceMm-(decimal)distance-remainder/100000000000m);
   decimal perStep=stepTicks*(decimal)baseSpeed*throttle/100000000000m;
   return Math.Ceiling(remaining/perStep)*stepTicks/10000000m;
  }
 }
}
