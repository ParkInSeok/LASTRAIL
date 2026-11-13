using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace LastRail.Battle {
 public static class KyleTauntPolicy {
  public const string Version="kyle-taunt.v2.20261109";
  public sealed class Candidate {
   public string Id {get;} public long Generation {get;} public BigInteger DistanceSquared {get;} public int Order {get;}
   public bool ThreatensCarriage {get;} public bool LineOfSight {get;} public bool Immune {get;}
   public Candidate(string id,long generation,BigInteger distanceSquared,int order,bool threatensCarriage,bool lineOfSight,bool immune){
    if(string.IsNullOrWhiteSpace(id)||generation<1||distanceSquared<0||order<0)throw new ArgumentException("Invalid taunt observation.");
    Id=id;Generation=generation;DistanceSquared=distanceSquared;Order=order;ThreatensCarriage=threatensCarriage;LineOfSight=lineOfSight;Immune=immune;
   }
  }
  public static IReadOnlyList<Candidate> Select(IEnumerable<Candidate> observations,int maximum,long rangeMm){
   if(observations==null||maximum<1||maximum>3||rangeMm<=0)throw new ArgumentException("Invalid taunt policy.");
   var rows=observations.ToArray();if(rows.Select(r=>r.Id).Distinct(StringComparer.Ordinal).Count()!=rows.Length)throw new ArgumentException("Duplicate taunt candidate.");
   return Array.AsReadOnly(rows.Where(r=>!r.Immune&&r.ThreatensCarriage&&r.LineOfSight&&r.DistanceSquared<=(BigInteger)rangeMm*rangeMm)
    .OrderBy(r=>r.DistanceSquared).ThenBy(r=>r.Order).ThenBy(r=>r.Id,StringComparer.Ordinal).Take(maximum).ToArray());
  }
 }
 internal interface IBBoardingTauntTargets {string ForcedTarget(string enemy);}
}
