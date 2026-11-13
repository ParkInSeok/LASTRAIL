using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
public static class CombatRailWorldLayout {
 public static JArray BuildConsist(JArray carriages){
  if(carriages==null||carriages.Count<2||carriages.Count>4)throw new ArgumentException("Selected 2–4 carriages required.");
  var result=new JArray();
  for(int i=0;i<carriages.Count;i++){
   var c=carriages[i];string id=(string)c["id"],type=(string)c["type"];
   if((int?)c["slot"]!=i||string.IsNullOrWhiteSpace(id)||!new[]{"Combat","Gun","Cargo","Defense","Medical"}.Contains(type)||result.Any(r=>(string)r["id"]==id))throw new ArgumentException("Invalid selected carriage metadata.");
   result.Add(new JObject{["id"]=id,["slot"]=i,["type"]=type,["x"]=428-i*120,["width"]=120});
  }
  return result;
 }
 public static float ArtilleryX(long distanceMm,long startDistanceMm,long exposureDistanceMm){if(exposureDistanceMm<=0)throw new ArgumentOutOfRangeException(nameof(exposureDistanceMm));double progress=Math.Max(0,Math.Min(1,(distanceMm-(double)startDistanceMm)/exposureDistanceMm));return (float)(720-progress*900);}
 public static float RouteOffset(long distanceMm,long confirmedDistanceMm,int car,int side)=>RouteOffsetAt(distanceMm,confirmedDistanceMm,car,side);
 public static float RouteOffsetAt(long distanceMm,long confirmedDistanceMm,float car,int side){
  double progress=Math.Max(0,Math.Min(1,(distanceMm-(double)confirmedDistanceMm-car*6000)/12000));
  return (float)(18*Math.Sign(side)*progress*progress*(3-2*progress));
 }
}
}
