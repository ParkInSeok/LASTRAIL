using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Cosmetic reader of existing shield units and resolved absorption. No projectile
// deletion, immunity, damage routing, or expiry changes are performed here.
public sealed class CombatElectromagneticState {
 internal const double AbsorptionTailSeconds=.32;
 readonly long stepTimeSpanTicks;
 int seenHits,previousShield;double issuedAt=-1000000;readonly JArray pulses=new JArray();
 static long Step(JObject hud){if(hud?["stepTimeSpanTicks"]?.Type!=JTokenType.Integer||(long)hud["stepTimeSpanTicks"]<=0)throw new ArgumentException("Original combat step duration required.");return (long)hud["stepTimeSpanTicks"];}
 internal double TimeAt(long tick)=>tick*(stepTimeSpanTicks/10000000d);
 public CombatElectromagneticState(JObject initial){stepTimeSpanTicks=Step(initial);seenHits=((JArray)initial["hits"]).Count;previousShield=(int)((JArray)initial["actors"]).Single(a=>(bool)a["train"])["shield"];}
 public JObject Read(JObject hud){
  if(Step(hud)!=stepTimeSpanTicks)throw new InvalidOperationException("Combat clock owner changed; rebind presentation.");
  double now=(double)hud["motionSeconds"];var train=((JArray)hud["actors"]).Single(a=>(bool)a["train"]);int amount=(int)train["shield"];var hits=(JArray)hud["hits"];
  if(amount>previousShield)issuedAt=now;previousShield=amount;
  for(int i=seenHits;i<hits.Count;i++){var h=hits[i];if((string)h["Target"]==(string)train["id"]&&(int)h["Absorbed"]>0)pulses.Add(new JObject{["index"]=i,["tick"]=h["Tick"],["source"]=h["Source"],["absorbed"]=h["Absorbed"],["hpLoss"]=h["HpLoss"]});}seenHits=hits.Count;
  foreach(var p in pulses.ToArray())if(now-TimeAt((long)p["tick"])>=AbsorptionTailSeconds)p.Remove();
  bool running=(int)train["hp"]>0&&(string)hud["outcome"]=="Running";
  return new JObject{["active"]=running&&(amount>0||pulses.Count>0),["shield"]=amount,["issueAge"]=now-issuedAt,["time"]=now,["absorptions"]=running?new JArray(pulses.Select(p=>{var r=(JObject)p.DeepClone();r["age"]=Math.Max(0,now-TimeAt((long)r["tick"]));return r;})):new JArray()};
 }
}
}
