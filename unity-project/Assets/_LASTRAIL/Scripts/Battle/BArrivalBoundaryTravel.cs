using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Core.Simulation;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Original local travel only. Authentication, independent time verification and
 // map acceptance belong to the server. Source hashes must come from retained START.
 public sealed class BArrivalBoundaryTravel {
  public const string Version="unity.b-arrival-boundary-checkpoint.v1.20261020";
  const long Safe=9007199254740991;
  readonly JObject source,binding,definition,boundary;
  readonly string nodeSourceHash;
  FixedStepDebt clock;
  CombatRailJourney rail;
  JArray commands=new JArray(),results=new JArray();
  JObject arrival;
  BArrivalBoundaryTravel(JObject s,JObject b,JObject edge){
   source=(JObject)s.DeepClone();binding=(JObject)b.DeepClone();
   definition=(JObject)source["definition"];boundary=(JObject)edge.DeepClone();
   nodeSourceHash=RawHash(new JObject{["origin"]=source["origin"].DeepClone(),["boundarySourceHash"]=source["sourceHash"].DeepClone(),["boundary"]=boundary.DeepClone()});
   clock=new FixedStepDebt((long)definition["stepTicks"]);
   rail=new CombatRailJourney((JObject)definition["configuration"]);
  }
  static string RawHash(JToken p)=>BDirectAttackInput.Digest(p).Substring(7);
  static void Need(bool value,string error){if(!value)throw new ArgumentException("b_arrival_"+error);}
  static void Fields(JObject p,params string[] names)=>BDirectAttackInput.Fields(p,names);
  static string Id(JToken p,string key)=>BDirectAttackInput.Text(p,key,200);
  static long Nat(JToken p,string key,long min=0,long max=Safe)=>BDirectAttackInput.Nat(p,key,min,max);
  static void Hash(string hash)=>Need(hash!=null&&Regex.IsMatch(hash,"^[a-f0-9]{64}$"),"hash");
  public static BArrivalBoundaryTravel Create(JObject source,string independentSourceHash,JObject expectedBinding){
   Fields(source,"version","origin","definition","sourceHash");Hash(independentSourceHash);
   var body=(JObject)source.DeepClone();body.Remove("sourceHash");
   Need((string)source["version"]=="combat.b-arrival-boundaries.v1.20261020"&&(string)source["sourceHash"]==independentSourceHash&&RawHash(body)==independentSourceHash,"source_pin");
   Fields(expectedBinding,"accountId","runId","nodeId","nativeContextId","departurePinHash","mapRevision","destinationEventId","edgeId","origin");
   foreach(var k in new[]{"accountId","runId","nodeId","nativeContextId","destinationEventId","edgeId"})Id(expectedBinding,k);
   Hash((string)expectedBinding["departurePinHash"]);Nat(expectedBinding,"mapRevision",1);
   Need(source["origin"] is JObject&&JToken.DeepEquals(source["origin"],expectedBinding["origin"])
    &&(string)source["origin"]["accountId"]==(string)expectedBinding["accountId"]
    &&(string)source["origin"]["runId"]==(string)expectedBinding["runId"],"origin");
   var d=source["definition"] as JObject;
   Fields(d,"definitionId","definitionVersion","stepTicks","executorVersion","speedHalfUnits","configuration","boundaries");
   Id(d,"definitionId");Id(d,"definitionVersion");Nat(d,"stepTicks",1);
   Need((string)d["executorVersion"]=="combat.b-arrival-linear-fixed-step.v1.20261020"&&Nat(d,"speedHalfUnits")==2,"executor");
   var c=d["configuration"] as JObject;
   Fields(c,"version","selectionScaleBps","recoveryMs","baseSpeedMillimetersPerSecond","throttleMinimumBps","throttleMaximumBps","throttleCruiseBps","offers");
   Need(c["offers"] is JArray offers&&offers.Count==0,"offers");
   // Existing owner validates exact rail ranges and version, with no default cruise.
   Nat(c,"throttleCruiseBps",1,13000);new CombatRailJourney(c);
   var edges=d["boundaries"] as JArray;Need(edges!=null&&edges.Count>0&&edges.Count<=1000,"boundaries");
   foreach(var t in edges){var e=t as JObject;Fields(e,"edgeId","fromNodeId","toNodeId","distanceMm");Id(e,"edgeId");Id(e,"fromNodeId");Id(e,"toNodeId");Nat(e,"distanceMm",1);}
   Need(edges.Select(e=>(string)e["edgeId"]).Distinct(StringComparer.Ordinal).Count()==edges.Count,"duplicate_edge");
   var selected=edges.SingleOrDefault(e=>(string)e["edgeId"]==(string)expectedBinding["edgeId"]) as JObject;
   Need(selected!=null&&(string)selected["toNodeId"]==(string)expectedBinding["nodeId"],"destination");
   return new BArrivalBoundaryTravel(source,expectedBinding,selected);
  }
  public void Execute(JObject command){
   Need(command!=null,"command");string kind=Id(command,"kind"),id=Id(command,"eventId");
   if(kind=="ADVANCE"){Fields(command,"kind","eventId","elapsed","maximumSteps");Nat(command,"elapsed");Nat(command,"maximumSteps",0,10000);}
   else if(kind=="PAUSE"){Fields(command,"kind","eventId","value");Need(command["value"].Type==JTokenType.Boolean,"pause");}
   else if(kind=="TRAVEL_THROTTLE"){Fields(command,"kind","eventId","bps");Nat(command,"bps",1,13000);}
   else throw new ArgumentException("b_arrival_command_kind");
   var prior=commands.FirstOrDefault(c=>(string)c["eventId"]==id);
   if(prior!=null){Need(JToken.DeepEquals(prior,command),"command_conflict");return;}
   Need(arrival==null&&commands.Count<1000,"closed_or_capacity");
   // Stage both owners so rejected/overflowing commands never partially mutate.
   var nextClock=FixedStepDebt.Restore(clock.Capture());
   var nextRail=new CombatRailJourney((JObject)definition["configuration"]);nextRail.Restore(rail.Capture());
   if(kind=="PAUSE")nextClock.SetPaused((bool)command["value"]);
   else if(kind=="TRAVEL_THROTTLE"){Need(!nextClock.IsPaused,"throttle_paused");nextRail.SetThrottle((int)command["bps"]);}
   else {
    nextClock.Credit((long)command["elapsed"]);
    for(int i=0;i<(long)command["maximumSteps"]&&nextClock.TryConsumeStep();i++){
     nextRail.AdvanceDistance((long)definition["stepTicks"]);nextRail.CheckBoundary(0,nextClock.CompletedSteps);
     if((long)nextRail.Capture()["distanceMm"]>=(long)boundary["distanceMm"])break;
    }
   }
   var cs=nextClock.Capture();var rs=nextRail.Capture();
   long remainder=checked((long)(decimal.Parse((string)rs["distanceRemainder"],CultureInfo.InvariantCulture)*100000000000m));
   // The independent original-clock proof also transmits half ticks as a safe
   // JSON integer. Its bound is stricter than the travel-only debtTicks field.
   Need(cs.CompletedSteps<=Safe&&cs.DebtHalfTicks<=Safe&&(long)rs["distanceMm"]<=Safe,"numeric_limit");
   var row=new JObject{["tick"]=cs.CompletedSteps,["distanceMm"]=rs["distanceMm"].DeepClone(),["remainderUnits"]=remainder,["debtTicks"]=cs.DebtHalfTicks/2,["paused"]=cs.IsPaused,["throttleBps"]=rs["throttleBps"].DeepClone()};
   JObject nextArrival=null;
   if((long)rs["distanceMm"]>=(long)boundary["distanceMm"]){
    var identity=new JObject{["runId"]=binding["runId"].DeepClone(),["nodeId"]=binding["nodeId"].DeepClone(),["destinationEventId"]=binding["destinationEventId"].DeepClone(),["boundarySourceHash"]=source["sourceHash"].DeepClone(),["commandId"]=id,["tick"]=cs.CompletedSteps};
    nextArrival=new JObject{["eventId"]="B_ARRIVE:"+RawHash(identity),["commandId"]=id,["commandSequence"]=commands.Count+1,["tick"]=cs.CompletedSteps};
   }
   clock=nextClock;rail=nextRail;commands.Add(command.DeepClone());results.Add(row);arrival=nextArrival;
  }
  public JObject CaptureClock(){var c=clock.Capture();return new JObject{["stepTicks"]=c.StepTicks,["debtHalfTicks"]=c.DebtHalfTicks,["completedSteps"]=c.CompletedSteps,["speedHalfUnits"]=c.SpeedHalfUnits,["paused"]=c.IsPaused};}
  JObject Travel()=>new JObject{["version"]="combat.b-arrival-travel-observation.v1.20261020",["accountId"]=binding["accountId"].DeepClone(),["runId"]=binding["runId"].DeepClone(),["nodeId"]=binding["nodeId"].DeepClone(),["nativeContextId"]=binding["nativeContextId"].DeepClone(),["boundarySourceHash"]=source["sourceHash"].DeepClone(),["nodeSourceHash"]=nodeSourceHash,["commands"]=commands.DeepClone(),["results"]=results.DeepClone(),["arrival"]=arrival?.DeepClone()??JValue.CreateNull()};
  public JObject Capture()=>new JObject{["version"]=Version,["provenance"]="LOCAL_ORIGINAL_NOT_SERVER_ACCEPTED",["sourceHash"]=source["sourceHash"].DeepClone(),["binding"]=binding.DeepClone(),["sourceRevision"]=commands.Count,["clock"]=CaptureClock(),["rail"]=rail.Capture(),["travel"]=Travel()};
  public JObject CaptureReport(string requestId){
   Id(new JObject{["requestId"]=requestId},"requestId");Need(arrival!=null,"not_arrived");
   var t=Travel();return new JObject{["version"]="combat.b-arrival-report.v2.20261020",["requestId"]=requestId,["reportOriginMapRevision"]=binding["mapRevision"].DeepClone(),["destinationEventId"]=binding["destinationEventId"].DeepClone(),["checkpointHash"]=BDirectAttackInput.Digest(t),["travel"]=t};
  }
  public JObject CaptureExport(string requestId){
   var report=CaptureReport(requestId);var checkpoint=Capture();
   return new JObject{["provenance"]="LOCAL_ORIGINAL_REQUIRES_INDEPENDENT_VERIFIER",["source"]=source.DeepClone(),["checkpoint"]=checkpoint,["report"]=report,
    ["sourceCanonicalJson"]=BasicCombatSimulation.StableJson(source),["checkpointCanonicalJson"]=BasicCombatSimulation.StableJson(checkpoint),
    ["checkpointHash"]=BDirectAttackInput.Digest(checkpoint),["travelCanonicalJson"]=BasicCombatSimulation.StableJson(report["travel"]),
    ["reportCanonicalJson"]=BasicCombatSimulation.StableJson(report)};
  }
  public static BArrivalBoundaryTravel Restore(JObject source,string independentSourceHash,JObject expectedBinding,JObject checkpoint,string independentCheckpointHash){
   Need(checkpoint!=null&&BDirectAttackInput.Digest(checkpoint)==independentCheckpointHash,"checkpoint_pin");
   var owner=Create(source,independentSourceHash,expectedBinding);
   var journal=checkpoint["travel"]?["commands"] as JArray;Need(journal!=null&&journal.Count<=1000,"checkpoint_commands");
   foreach(var command in journal)owner.Execute(command as JObject);
   Need(JToken.DeepEquals(owner.Capture(),checkpoint),"checkpoint_replay");return owner;
  }
 }
}
