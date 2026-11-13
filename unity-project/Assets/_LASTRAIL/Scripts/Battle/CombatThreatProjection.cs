using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Existing HUD events supply stage/identity, but no per-response deadline or tail-crossing prediction.
public sealed class CombatThreatProjection {
 public sealed class Warning {
  public string Id {get;} public string Label {get;} public long Sequence {get;}
  internal Warning(string id,string label,long sequence=0){Id=id;Label=label;Sequence=sequence;}
  public override string ToString()=>Label;
 }
 public ReadOnlyCollection<Warning> Warnings {get;}
 public Warning Primary=>null; // shortcut: legacy HUD has no response deadlines; rank after native per-response deadlines are bound.
 CombatThreatProjection(List<Warning> warnings){Warnings=warnings.OrderBy(w=>w.Sequence).ThenBy(w=>w.Id,System.StringComparer.Ordinal).ToList().AsReadOnly();}
 public static CombatThreatProjection Read(JObject state){
  var warnings=new List<Warning>();if((string)state["outcome"]!="Running")return new CombatThreatProjection(warnings);
  var rail=state["railJourney"] as JObject;
  if((string)rail?["phase"]=="Selecting"&&rail["offer"] is JObject offer)warnings.Add(new Warning("branch:"+(string)offer["id"],"분기 · 경로 선택 가능"));
  if(state["railArtillery"] is JObject artillery){
   foreach(var row in (JArray)artillery["encounters"])if((string)row["status"]=="Active")warnings.Add(new Warning("artillery:"+(string)row["id"],"포대 작동"));
   foreach(var shot in (JArray)artillery["projectiles"])warnings.Add(new Warning("shell:"+(string)shot["source"]+":"+(long)shot["sequence"],"포탄 비행",(long)shot["sequence"]));
  }
  return new CombatThreatProjection(warnings);
 }
}
}
