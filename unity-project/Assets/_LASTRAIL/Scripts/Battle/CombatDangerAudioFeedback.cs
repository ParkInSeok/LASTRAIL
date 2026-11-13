using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Presentation cursor only: the combat owners determine danger and timing.
public sealed class CombatDangerAudioFeedback {
 readonly HashSet<string> seen=new HashSet<string>();
 string scope;bool seeded;
 public bool Active {get;private set;}
 static string Scope(JObject s){var o=s["ordinaryObstacle"];return o==null?"legacy":new JArray(o["accountId"],o["runId"],o["nodeId"],o["attemptGeneration"],o["snapshotHash"]).ToString(Newtonsoft.Json.Formatting.None);}
 public void Seed(JObject state){if(!seeded){scope=Scope(state);seeded=true;}Observe(state,false);}
 public string Observe(JObject s,bool audible){
  Active=false;if(!seeded||Scope(s)!=scope)return null;
  bool running=(string)s["outcome"]=="Running"&&(bool?)s["paused"]!=true;
  string cue=null;
  if(s["ordinaryObstacle"] is JObject obstacle&&(long?)s["tick"]>0){
   bool warning=running&&(bool?)s["fullCombatReady"]==true&&(string)obstacle["phase"]=="OBSTACLE"&&(string)s["ordinaryObstacleExecution"]?["phase"]=="WARNING";
   Active|=warning;if(seen.Add("obstacle")&&warning&&audible)cue="obstacle";
  }
  if(s["railArtillery"] is JObject artillery&&artillery["events"] is JArray events&&artillery["encounters"] is JArray encounters){
   foreach(var row in encounters){
    string id=(string)row["id"];if(!events.Any(e=>(string)e["kind"]=="SPAWN"&&(string)e["id"]==id))continue;
    // shortcut: only the existing initial windup is signalled; recurring warnings need an owner event.
    bool warning=running&&(bool?)artillery["terminalRequested"]!=true&&(string)row["status"]=="Active"&&(int?)row["hp"]>0&&(long?)row["nextLaunch"]>(long?)artillery["tick"]&&!events.Any(e=>(string)e["kind"]=="LAUNCH"&&(string)e["id"]==id);
    Active|=warning;if(seen.Add("artillery:"+id)&&warning&&audible)cue=cue??"artillery:"+id;
   }
  }
  return cue;
 }
}
}
