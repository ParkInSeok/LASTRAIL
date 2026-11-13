using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Presentation cursor over the existing append-only event stream; never a combat result owner.
 public sealed class CombatArtilleryOutcomeFeedback {
  const long DisplayTicks=60; // Existing rail-artillery-v1 uses a 50 ms step.
  int observed;long visibleUntil=-1;
  public void Seed(JObject state){observed=Events(state)?.Count??0;visibleUntil=-1;}
  static JArray Events(JObject state)=>state["railArtillery"]?["events"] as JArray;
  public string Observe(JObject state,bool warningVisible){
   var events=Events(state);long tick=(long)state["tick"];
   if(events==null||events.Count<observed){Seed(state);return null;}
   for(int i=observed;i<events.Count;i++){
    var row=events[i];
    // PASSED and zero damage do not prove whole-train avoidance in this source.
    if((string)row["kind"]=="DESTROYED"&&!warningVisible&&(string)state["outcome"]=="Running")visibleUntil=(long)row["tick"]+DisplayTicks;
   }
   observed=events.Count;
   if(warningVisible||(string)state["outcome"]!="Running")visibleUntil=-1;
   return tick<visibleUntil?"포대 무력화":null;
  }
 }
}
