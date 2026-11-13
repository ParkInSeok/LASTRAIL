using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.Battle
{
 // v03 ally readability order only; no world visibility, collision or HUD authority.
 // All layers are siblings in the existing Canvas view, not children of actor groups.
 public static class FourAgentVisualDepth
 {
  public const string Version="four-agent.visual.v03.20261019";
  static readonly HashSet<string> Body=new HashSet<string>(StringComparer.Ordinal){"actor","drone","crystal","smoke"};
  static readonly HashSet<string> Foreground=new HashSet<string>(StringComparer.Ordinal){"muzzle","projectile","impact","impactStar","impactSpark","authoritativeImpact","beam","charge","chargeRing","chargeParticle","chargeCore","droneFlash","glove","gloveParticle","gloveRing","magicCircle"};
  public static IReadOnlyList<JObject> Order(JArray layers,int direction)
  {
   if(layers==null||direction<0||direction>3)throw new ArgumentException("Explicit layers and zero-based direction required.");
   var rows=layers.Select(x=>x as JObject??throw new ArgumentException("Visual layer must be an object.")).ToArray();
   foreach(var row in rows)
   {
    string role=(string)row["role"];
    if(role==null||!Body.Contains(role)&&!Foreground.Contains(role)||row["slot"]?.Type!=JTokenType.Integer||(int)row["slot"]<0||row["order"]?.Type!=JTokenType.Integer)throw new ArgumentException("Known layer role/slot/order required.");
    if(row["direction"]!=null&&(row["direction"].Type!=JTokenType.Integer||(int)row["direction"]<0||(int)row["direction"]>3))throw new ArgumentException("Layer direction must be zero-based 0..3.");
   }
   var actorOrders=rows.Where(r=>(string)r["role"]=="actor").GroupBy(r=>(int)r["slot"]).ToDictionary(g=>g.Key,g=>{
    if(g.Count()!=1)throw new ArgumentException("One actor body per slot; do not repeat it in the foreground pass.");return (int)g.Single()["order"];
   });
   double LocalOrder(JObject row)
   {
    if((string)row["role"]=="smoke")
    {
     if(!actorOrders.TryGetValue((int)row["slot"],out int actor))throw new ArgumentException("Smoke requires its visible actor body.");
     return actor+(((int?)row["direction"]??direction)==3?-.5:.5);
    }
    return (int)row["order"];
   }
   // Stable ordering retains beam outer/core and companion back/body/front order.
   return rows.OrderBy(r=>Body.Contains((string)r["role"])?0:1)
    .ThenBy(r=>(int)r["slot"])
    .ThenBy(r=>Body.Contains((string)r["role"])?LocalOrder(r):0).ToArray();
  }
 }
}
