using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 /// <summary>
 /// Independent first-Mira geometric path, composed with the existing lifetime and
 /// exact damage owner. No owned augment source, trial policy or native HP publication.
 /// The combat root must stage HP/death/rewards with this whole state atomically.
 /// Arrival readers must be side-effect-free; no damage/FX is emitted during restore.
 /// </summary>
 public sealed class MiraPierceFlight {
  public const string Version="mira.pierce-flight.v3.20261016";
  readonly JObject input,reservation;
  FriendlyProjectileOwner flight;JObject previous;JArray journal=new JArray();long elapsed,sequence=1;bool held,stopped;
  MiraPierceFlight(JObject input,JObject reservation,FriendlyProjectileOwner flight){this.input=input;this.reservation=reservation;this.flight=flight;previous=(JObject)input["observation"].DeepClone();}
  public static MiraPierceFlight Create(string combatId,string opportunityId,JObject observation,string observationHash,JArray primaryIds,JObject attack,int capacity){
   var plan=MiraPierceReservation.Create(observation,observationHash,primaryIds).Capture();
   var flight=new FriendlyProjectileOwner(combatId,capacity,FriendlyProjectileOwner.ActualContactVersion);
   if((long)observation["tick"]!=0)flight.AdvanceTo((long)observation["tick"]);
   var source=observation["source"];
   flight.Launch(new JObject{["version"]=FriendlyProjectileOwner.ActualContactVersion,["opportunityId"]=opportunityId,["sourceId"]=source["id"].DeepClone(),["sourceGeneration"]=source["generation"].DeepClone(),["tick"]=observation["tick"].DeepClone(),["sequence"]=1,["attack"]=attack.DeepClone(),["geometry"]=new JObject{["version"]=MiraPierceReservation.Version,["revision"]=observation["revision"].DeepClone(),["hash"]=BDirectAttackInput.Digest(plan)},["branches"]=new JArray(new JObject{["branchId"]="main",["pathId"]=opportunityId+":original-ray",["kind"]="MAIN",["contacts"]=plan["contacts"].DeepClone()})},true);
   return new MiraPierceFlight(new JObject{["combatId"]=combatId,["opportunityId"]=opportunityId,["observation"]=observation.DeepClone(),["observationHash"]=observationHash,["primaryIds"]=primaryIds.DeepClone(),["attack"]=attack.DeepClone(),["capacity"]=capacity},plan,flight);
  }
  MiraPierceFlight Clone()=>new MiraPierceFlight(input,reservation,flight.Clone()){previous=(JObject)previous.DeepClone(),journal=(JArray)journal.DeepClone(),elapsed=elapsed,sequence=sequence,held=held,stopped=stopped};
  void Room(){if(journal.Count>=(int)input["capacity"])throw new InvalidOperationException("Pierce journal capacity; preserve checkpoint.");}
  public bool SetHeld(bool value){if(held==value)return false;Room();var next=flight.Clone();next.SetHeld(value);flight=next;held=value;journal.Add(new JObject{["kind"]="HOLD",["value"]=value});return true;}
  public bool Stop(string reason){
   var staged=flight.Clone();long next=checked(sequence+1);
   bool changed=reason=="NODE_END"||reason=="COMBAT_END"?staged.End(reason,next):staged.StopBranch((string)input["opportunityId"],"main",reason,next);
   if(!changed)return false;Room();flight=staged;sequence=next;stopped=true;journal.Add(new JObject{["kind"]="STOP",["reason"]=reason});return true;
  }
  public JArray Step(JObject end,string pinnedHash,long elapsedEndUs,Func<string,JObject> readArrival){
   if(end==null||BDirectAttackInput.Digest(end)!=pinnedHash||readArrival==null)throw new ArgumentException("Independent observation/arrival reader required.");
   if(held||stopped)return new JArray();
   if(elapsedEndUs<elapsed||(long)end["tick"]<(long)previous["tick"])throw new InvalidOperationException("Original path time cannot rewind.");
   if(elapsedEndUs==elapsed&&JToken.DeepEquals(end,previous))return new JArray();
   Room();var staged=Clone();var arrivals=new JObject();var results=staged.Process(end,elapsedEndUs,id=>{var a=readArrival(id)??throw new ArgumentException("Arrival required.");arrivals[id]=a.DeepClone();return a;});
   staged.journal.Add(new JObject{["kind"]="STEP",["observation"]=end.DeepClone(),["hash"]=pinnedHash,["elapsedUs"]=elapsedEndUs,["arrivals"]=arrivals});
   flight=staged.flight;previous=staged.previous;elapsed=staged.elapsed;sequence=staged.sequence;stopped=staged.stopped;journal=staged.journal;return results;
  }
  JArray Process(JObject end,long elapsedEndUs,Func<string,JObject> readArrival){
   var output=new JArray();flight.AdvanceTo((long)end["tick"]);
   var resolved=(JArray)flight.Capture()["state"]["opportunities"][0]["branches"][0]["results"];
   var rows=((JArray)reservation["contacts"]).Where(c=>resolved.Single(r=>(string)r["contactId"]==(string)c["contactId"])["result"].Type==JTokenType.Null).ToArray();
   var traces=rows.Select(c=>new {Contact=c,Trace=MiraJudgementGeometry.TraceReserved((JObject)reservation["ray"],(string)reservation["rayHash"],previous,end,elapsed,elapsedEndUs,(string)c["targetId"],(long)c["targetGeneration"])})
    .Where(x=>(string)x.Trace["kind"]!="NONE").OrderBy(x=>(double?)x.Trace["elapsedUs"]??elapsedEndUs).ThenBy(x=>(string)x.Trace["kind"]=="BLOCKED"?0:1).ThenBy(x=>(string)x.Contact["targetId"],StringComparer.Ordinal).ToArray();
   foreach(var row in traces){if(stopped)break;string kind=(string)row.Trace["kind"];
    if(kind=="BLOCKED"||kind=="EXPIRED"){flight.StopBranch((string)input["opportunityId"],"main",kind=="BLOCKED"?"WALL":"EXPIRED",++sequence);stopped=true;break;}
    bool hit=kind=="HIT";string id=(string)row.Contact["targetId"];
    var contact=flight.ResolveContact(new JObject{["version"]=FriendlyProjectileOwner.ActualContactVersion,["opportunityId"]=input["opportunityId"].DeepClone(),["branchId"]="main",["contactId"]=row.Contact["contactId"].DeepClone(),["tick"]=end["tick"].DeepClone(),["sequence"]=++sequence,["targetGeneration"]=row.Contact["targetGeneration"].DeepClone(),["eligible"]=hit,["arrival"]=hit?(JToken)readArrival(id):JValue.CreateNull()});
    if(contact.NewlyResolved)output.Add(new JObject{["trace"]=row.Trace.DeepClone(),["contact"]=contact.Snapshot()});
   }
   previous=(JObject)end.DeepClone();elapsed=elapsedEndUs;return output;
  }
  public JObject Capture()=>new JObject{["version"]=Version,["input"]=input.DeepClone(),["reservation"]=reservation.DeepClone(),["commands"]=journal.DeepClone(),["previous"]=previous.DeepClone(),["elapsedUs"]=elapsed,["sequence"]=sequence,["held"]=held,["stopped"]=stopped,["flight"]=flight.Capture()};
  public static MiraPierceFlight Restore(JObject saved,string expectedCombatId,string independentHash){
   if(saved==null||(string)saved["version"]!=Version||BDirectAttackInput.Digest(saved)!=independentHash||(string)saved["input"]?["combatId"]!=expectedCombatId)throw new ArgumentException("Pierce checkpoint binding.");
   var i=(JObject)saved["input"];var result=Create(expectedCombatId,(string)i["opportunityId"],(JObject)i["observation"],(string)i["observationHash"],(JArray)i["primaryIds"],(JObject)i["attack"],(int)i["capacity"]);
   var commands=saved["commands"] as JArray;if(commands==null||commands.Count>(int)i["capacity"])throw new ArgumentException("Pierce journal bounds.");
   foreach(var command in commands){if((string)command["kind"]=="HOLD")result.SetHeld((bool)command["value"]);else if((string)command["kind"]=="STOP")result.Stop((string)command["reason"]);else if((string)command["kind"]=="STEP")result.Step((JObject)command["observation"],(string)command["hash"],(long)command["elapsedUs"],id=>(JObject)command["arrivals"][id]);else throw new ArgumentException("Pierce journal command.");}
   if(!JToken.DeepEquals(saved,result.Capture()))throw new ArgumentException("Pierce replay mismatch.");return result;
  }
 }
}