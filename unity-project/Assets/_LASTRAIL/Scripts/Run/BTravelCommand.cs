using System;
using System.Linq;
using System.Text.RegularExpressions;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Wire originals are immutable. Time and movement are accepted only from the server receipt.
 public sealed class BTravelCommand {
  public const string Version="combat.b-travel-command.v1.20261031";
  public string RequestId{get;} public string DestinationEventId{get;} public string NodeId{get;} public long MapRevision{get;} public long ExpectedRevision{get;} public string Kind{get;} public string BodyJson{get;}
  BTravelCommand(JObject p,string json){RequestId=Id(p,"requestId");DestinationEventId=Id(p,"destinationEventId");NodeId=Id(p,"nodeId");MapRevision=BDirectAttackInput.Nat(p,"mapRevision",1);ExpectedRevision=BDirectAttackInput.Nat(p,"expectedRevision");Kind=Id(p,"kind");BodyJson=json;}
  public static BTravelCommand Read(string body){
   var p=SafeTransitionWire.Parse(body,4096);var kind=Id(p,"kind");var extra=kind=="TRAVEL_THROTTLE"?new[]{"bps"}:kind=="PAUSE"?new[]{"value"}:kind=="RECOVER"||kind=="ARRIVE"?Array.Empty<string>():throw new ArgumentException("b_travel_kind_invalid");
   BDirectAttackInput.Fields(p,new[]{"version","requestId","destinationEventId","nodeId","mapRevision","expectedRevision","kind"}.Concat(extra).ToArray());Need((string)p["version"]==Version);
   if(kind=="TRAVEL_THROTTLE")BDirectAttackInput.Nat(p,"bps",1,13000);if(kind=="PAUSE")Need(p["value"]?.Type==JTokenType.Boolean);
   return new BTravelCommand(p,body);
  }
  internal static string Id(JToken p,string key){var v=p?[key];Need(v?.Type==JTokenType.String);string s=(string)v;Need(s.Length>=1&&s.Length<=200);return s;}
  internal static void Need(bool ok){if(!ok)throw new ArgumentException("b_travel_wire_conflict");}
 }
 public sealed class BTravelState {
  public long Tick{get;} public long DistanceMm{get;} public long RemainderUnits{get;} public long DebtTicks{get;} public bool Paused{get;} public int ThrottleBps{get;}
  internal BTravelState(JObject p){BDirectAttackInput.Fields(p,"tick","distanceMm","remainderUnits","debtTicks","paused","throttleBps");Tick=BDirectAttackInput.Nat(p,"tick");DistanceMm=BDirectAttackInput.Nat(p,"distanceMm");RemainderUnits=BDirectAttackInput.Nat(p,"remainderUnits",0,99999999999);DebtTicks=BDirectAttackInput.Nat(p,"debtTicks");ThrottleBps=(int)BDirectAttackInput.Nat(p,"throttleBps",1,13000);BTravelCommand.Need(p["paused"]?.Type==JTokenType.Boolean);Paused=(bool)p["paused"];}
 }
 public sealed class BTravelReceipt {
  public const string Version=BTravelCommand.Version;
  readonly JObject original;
  public string RequestId{get;} public string DestinationEventId{get;} public string NodeId{get;} public long MapRevision{get;} public long Revision{get;} public long AcceptedAtMs{get;} public BTravelState State{get;}
  public bool IsCurrentMap{get;} public bool PauseIntent{get;} public string RunStatus{get;} public string ArrivalEvidenceRef{get;}
  public JObject MapState=>original["mapState"] is JObject p?(JObject)p.DeepClone():null;
  public JObject MapReceipt=>original["mapReceipt"] is JObject p?(JObject)p.DeepClone():null;
  public JObject Capture()=>(JObject)original.DeepClone();
  BTravelReceipt(JObject p,bool current){original=(JObject)p.DeepClone();IsCurrentMap=current;RequestId=BTravelCommand.Id(p,"requestId");MapRevision=BDirectAttackInput.Nat(p,"mapRevision",1);if(current){RunStatus=BTravelCommand.Id(p,"runStatus");return;}DestinationEventId=BTravelCommand.Id(p,"destinationEventId");NodeId=BTravelCommand.Id(p,"nodeId");Revision=BDirectAttackInput.Nat(p,"revision");AcceptedAtMs=BDirectAttackInput.Nat(p,"acceptedAtMs");State=new BTravelState(Obj(p["state"]));PauseIntent=p.Property("pauseIntent")!=null;ArrivalEvidenceRef=(string)p["arrival"]?["evidenceRef"];}
  public BTravelReceipt RequireScope(string account,string run){
   Need(!string.IsNullOrEmpty(account)&&!string.IsNullOrEmpty(run));
   if(original["arrival"] is JObject arrival){var observation=Obj(arrival["observation"]);Need(BTravelCommand.Id(observation,"accountId")==account&&BTravelCommand.Id(observation,"runId")==run);}
   if(original["mapReceipt"] is JObject receipt&&receipt.Property("nodeStart")!=null){var node=Obj(receipt["nodeStart"]);Need(BTravelCommand.Id(node,"accountId")==account&&BTravelCommand.Id(node,"runId")==run);}
   return this;
  }
  public static BTravelReceipt Read(string json,BTravelCommand request){
   if(request==null)throw new ArgumentNullException(nameof(request));var p=SafeTransitionWire.Parse(json);Need((string)p["version"]==Version&&BTravelCommand.Id(p,"requestId")==request.RequestId);
   if(p.Property("status")!=null){
    BDirectAttackInput.Fields(p,"version","requestId","status","runStatus","mapRevision","mapState","mapReceipt");Need(request.Kind=="RECOVER"&&(string)p["status"]=="CURRENT_MAP");BTravelCommand.Id(p,"runStatus");var revision=BDirectAttackInput.Nat(p,"mapRevision",1);MapProgress(Obj(p["mapState"]));
    if(p["mapReceipt"]?.Type!=JTokenType.Null){var r=Obj(p["mapReceipt"]);Need((string)r["version"]=="operation.map-receipt.v1"&&BDirectAttackInput.Nat(r,"revision",1)==revision&&JToken.DeepEquals(r["state"],p["mapState"]));BTravelCommand.Id(r,"eventId");MapProgress(Obj(r["state"]));}
    return new BTravelReceipt(p,true);
   }
   var result=Accepted(p,request.DestinationEventId,request.NodeId,request.MapRevision,request.ExpectedRevision+1);
   bool pauseAtArrival=result.PauseIntent;Need(!pauseAtArrival||request.Kind=="PAUSE"&&(bool)SafeTransitionWire.Parse(request.BodyJson)["value"]);Need((request.Kind=="ARRIVE"||pauseAtArrival)==(p.Property("arrival")!=null));
   if(request.Kind=="RECOVER")Need(result.State.Paused);if(request.Kind=="PAUSE")Need(pauseAtArrival?!result.State.Paused:result.State.Paused==(bool)SafeTransitionWire.Parse(request.BodyJson)["value"]);
   if(request.Kind=="TRAVEL_THROTTLE")Need(result.State.ThrottleBps==(int)SafeTransitionWire.Parse(request.BodyJson)["bps"]);
   return result;
  }
  public static BTravelReceipt ReadInitial(string json,string destinationEventId,string nodeId,long mapRevision){
   var p=SafeTransitionWire.Parse(json);Need(BTravelCommand.Id(p,"requestId")==destinationEventId&&p.Property("arrival")==null&&p.Property("pauseIntent")==null);var r=Accepted(p,destinationEventId,nodeId,mapRevision,0);Need(!r.State.Paused&&r.State.Tick==0&&r.State.DistanceMm==0&&r.State.RemainderUnits==0&&r.State.DebtTicks==0);return r;
  }
  // A conflict snapshot is not a success or a newly accepted command.
  public static BTravelReceipt ReadConflictCurrent(string json,BTravelCommand rejectedRequest){
   if(rejectedRequest==null)throw new ArgumentNullException(nameof(rejectedRequest));var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,"accepted","errorCode","current");SafeTransitionWire.Boolean(p,"accepted",false);Need((string)p["errorCode"]=="combat_b_travel_revision_conflict");if(p["current"]?.Type==JTokenType.Null)return null;
   var current=Obj(p["current"]);var revision=BDirectAttackInput.Nat(current,"revision",1);return Accepted(current,rejectedRequest.DestinationEventId,rejectedRequest.NodeId,rejectedRequest.MapRevision,revision);
  }
  static BTravelReceipt Accepted(JObject p,string destination,string node,long map,long revision){
   BDirectAttackInput.Fields(p,new[]{"version","requestId","destinationEventId","nodeId","mapRevision","revision","acceptedAtMs","state"}.Concat(p.Property("arrival")==null?Array.Empty<string>():new[]{"arrival"}).Concat(p.Property("pauseIntent")==null?Array.Empty<string>():new[]{"pauseIntent"}).ToArray());
   Need((string)p["version"]==Version&&BTravelCommand.Id(p,"destinationEventId")==destination&&BTravelCommand.Id(p,"nodeId")==node&&BDirectAttackInput.Nat(p,"mapRevision",1)==map&&BDirectAttackInput.Nat(p,"revision")==revision);var result=new BTravelReceipt(p,false);if(result.PauseIntent){SafeTransitionWire.Boolean(p,"pauseIntent",true);Need(!result.State.Paused);}if(p.Property("arrival")!=null)Arrival(Obj(p["arrival"]),result);return result;
  }
  static void Arrival(JObject p,BTravelReceipt r){
   BDirectAttackInput.Fields(p,"version","evidenceRef","requestId","arrivalAccepted","progressAuthorized","combatTruth","verificationLevel","observation","missingSources");Need((string)p["version"]==BArrivalReportRequest.Version&&BTravelCommand.Id(p,"requestId")==r.RequestId&&(string)p["combatTruth"]=="NOT_PROVEN"&&(string)p["verificationLevel"]=="ORIGINAL_PINNED_TRAVEL_COMMAND_REPLAY");SafeTransitionWire.Boolean(p,"arrivalAccepted",true);SafeTransitionWire.Boolean(p,"progressAuthorized",true);Need(p["missingSources"] is JArray missing&&missing.Count==0&&!r.State.Paused);string evidence=BTravelCommand.Id(p,"evidenceRef");Need(evidence.StartsWith("B_ARRIVAL:",StringComparison.Ordinal));
   var o=Obj(p["observation"]);BDirectAttackInput.Fields(o,"version","accountId","runId","nodeId","mapRevision","nativeContextId","departurePinHash","nodeSourceHash","operationBindingHash","evidenceRef","originalEventId","sequence","sourceRevision","checkpointHash","destination","arrival","travelStopped","combatTruth");Need((string)o["version"]=="combat.b-boundary-arrival-observation.v1.20261020"&&(string)o["combatTruth"]=="NOT_PROVEN"&&BTravelCommand.Id(o,"evidenceRef")==evidence&&BTravelCommand.Id(o,"nodeId")==r.NodeId&&BDirectAttackInput.Nat(o,"mapRevision",1)==r.MapRevision);SafeTransitionWire.Boolean(o,"travelStopped",true);foreach(var key in new[]{"accountId","runId","nativeContextId","originalEventId"})BTravelCommand.Id(o,key);foreach(var key in new[]{"departurePinHash","nodeSourceHash","operationBindingHash"})Hash(o,key,false);Hash(o,"checkpointHash",true);
   long sequence=BDirectAttackInput.Nat(o,"sequence",1);Need(BDirectAttackInput.Nat(o,"sourceRevision",1)==sequence);var arrival=Obj(o["arrival"]);BDirectAttackInput.Fields(arrival,"eventId","sequence","tick");Need(BTravelCommand.Id(arrival,"eventId")==(string)o["originalEventId"]&&BDirectAttackInput.Nat(arrival,"sequence",1)==sequence&&BDirectAttackInput.Nat(arrival,"tick",1)==r.State.Tick);
   var d=Obj(o["destination"]);BDirectAttackInput.Fields(d,"eventId","mapRevision","receiptHash","edgeId");Need(BTravelCommand.Id(d,"eventId")==r.DestinationEventId&&BDirectAttackInput.Nat(d,"mapRevision",1)==r.MapRevision);BTravelCommand.Id(d,"edgeId");Hash(d,"receiptHash",false);
  }
  static void MapProgress(JObject p){BDirectAttackInput.Fields(p,"version","traversedEdgeIds","pendingEdgeId","phase","holdIds");Need((string)p["version"]=="operation.map-progress.v1");string phase=BTravelCommand.Id(p,"phase");Need(new[]{"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE"}.Contains(phase));Ids(p["traversedEdgeIds"],12,false);Ids(p["holdIds"],1000,true);if(p["pendingEdgeId"]?.Type!=JTokenType.Null)BTravelCommand.Id(p,"pendingEdgeId");Need((phase=="TRAVELLING")== (p["pendingEdgeId"]?.Type==JTokenType.String));}
  static void Ids(JToken token,int maximum,bool unique){var a=token as JArray;Need(a!=null&&a.Count<=maximum&&a.All(v=>v.Type==JTokenType.String&&((string)v).Length>=1&&((string)v).Length<=200));if(unique)Need(a.Select(v=>(string)v).Distinct().Count()==a.Count);}
  static void Hash(JObject p,string key,bool prefix)=>Need(p[key]?.Type==JTokenType.String&&Regex.IsMatch((string)p[key],prefix?"^sha256:[a-f0-9]{64}$":"^[a-f0-9]{64}$"));
  static JObject Obj(JToken p)=>p as JObject??throw new ArgumentException("b_travel_object_required");
  static void Need(bool ok)=>BTravelCommand.Need(ok);
 }
}
