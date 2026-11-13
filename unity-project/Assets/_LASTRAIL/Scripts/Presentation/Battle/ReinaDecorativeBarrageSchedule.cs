using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Presentation.Battle {
 // Decorative coordinates only; no combat RNG, commands or damage receipts.
 public sealed class ReinaDecorativeBarrageSchedule {
  public sealed class Round {
   public string Id {get;internal set;}
   public string Kind {get;internal set;}
   public double Start {get;internal set;} public double Fall {get;internal set;}
   public double Impact=>Start+Fall;
   public double X {get;internal set;} public double Y {get;internal set;}
   public double EntryX {get;internal set;} public double EntryY {get;internal set;}
   public double ControlX {get;internal set;} public double ControlY {get;internal set;}
   public double Radius {get;internal set;} public double Scale {get;internal set;}
   public double CoreLife {get;internal set;} public double ShockLife {get;internal set;}
  }
  public IReadOnlyList<Round> Rounds {get;private set;}
  public ReinaDecorativeBarrageSchedule(JArray definitions,Func<double> random){
   if(definitions?.Count!=62||random==null)throw new ArgumentException("62 original decorative rounds required.");
   double Next(){double n=random();if(double.IsNaN(n)||double.IsInfinity(n)||n<0||n>=1)throw new ArgumentException("Visual random value outside [0,1).");return n;}
   double Number(JToken n){double v=(double?)n??double.NaN;if(double.IsNaN(v)||double.IsInfinity(v)||v<0)throw new ArgumentException("Finite nonnegative decorative metadata required.");return v;}
   var shots=new List<Round>();var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject e in definitions){
    string id=(string)e["id"],kind=(string)e["kind"];
    if(string.IsNullOrEmpty(id)||!ids.Add(id)||(bool?)e["decorative_only"]!=true||(kind!="major"&&kind!="minor")||!(e["anchor"] is JArray anchor)||anchor.Count!=2)throw new ArgumentException("Original decorative metadata differs.");
    shots.Add(new Round{Id=id,Kind=kind,Start=Next()*6.7,Fall=.30+Next()*.16,X=Number(anchor[0]),Y=Number(anchor[1]),Radius=Number(e["radius"]),Scale=Number(e["scale"]),CoreLife=Number(e["core_life"]),ShockLife=Number(e["shock_life"])});
   }
   shots=shots.OrderBy(x=>x.Start).ToList();var active=new List<double>();double previous=-1;
   foreach(var shot in shots){
    shot.Start=Math.Max(shot.Start,previous+.0001);active.RemoveAll(end=>end<=shot.Start);
    if(active.Count>=5){shot.Start=active.Min()+.0001;active.RemoveAll(end=>end<=shot.Start);}
    active.Add(shot.Impact);previous=shot.Start;
    shot.EntryX=Math.Max(.4,Math.Min(.96,shot.X+(Next()-.5)*.48));shot.EntryY=-.04-Next()*.14;
    shot.ControlX=shot.EntryX*.46+shot.X*.54+(Next()-.5)*.07;shot.ControlY=shot.EntryY*.45+shot.Y*.55;
   }
   double scale=Math.Min(1,7.4/shots.Max(x=>x.Impact));foreach(var shot in shots){shot.Start*=scale;shot.Fall*=scale;}
   Rounds=shots.AsReadOnly();
  }
 }
}
