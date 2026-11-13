using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public sealed class BDepartureAssignedActor {
  readonly JObject original;
  internal BDepartureAssignedActor(JObject row){original=(JObject)row.DeepClone();}
  public string OperatorId=>(string)original["operatorId"];
  public int PhysicalSlot=>(int)original["physicalSlot"];
  public string CarriageInstanceId=>(string)original["carriageInstanceId"];
  public long PhysicalRevision=>(long)original["physicalRevision"];
  public JObject PermanentStats=>(JObject)original["stats"].DeepClone();
  public JObject OwnedGrowth=>(JObject)original["owned"].DeepClone();
  public JObject Capture()=>(JObject)original.DeepClone();
 }
 // Original persisted B assignment, independent of A formation/loadout and of
 // any live executor. Permanent stats remain owned by the frozen Server source.
 public sealed class BDepartureAssignment {
  public const string Version="combat.b-departure-assignment.v1.20261006";
  readonly JObject original;readonly IReadOnlyList<BDepartureAssignedActor> actors;
  public string AccountId=>(string)original["accountId"];public string RunId=>(string)original["runId"];
  public string PacketHash=>(string)original["packetHash"];public bool CanStart=>false;
  public IReadOnlyList<BDepartureAssignedActor> Actors=>actors;
  BDepartureAssignment(JObject p){original=(JObject)p.DeepClone();actors=Array.AsReadOnly(((JArray)original["actors"]).Select(a=>new BDepartureAssignedActor((JObject)a)).ToArray());}
  public JObject Capture()=>(JObject)original.DeepClone();
  public JObject CarAtPhysicalSlot(int slot){if(slot<0||slot>4)throw new ArgumentOutOfRangeException(nameof(slot));return ((JArray)original["refitSnapshot"]["refit"]["cars"]).SingleOrDefault(c=>(int)c["slot"]==slot)?.DeepClone() as JObject;}
  public void RequireRuntimeBinding()=>throw new InvalidOperationException("b_assignment_effective_profile_context_geometry_and_runtime_binding_unavailable");
  public void RequireSameDeparture(BRouteBinding route){var r=route?.Capture();if(r==null||AccountId!=route.AccountId||RunId!=route.RunId||!JToken.DeepEquals(original["origin"],r["originalNativeOrigin"])||!JToken.DeepEquals(original["departurePinHash"],r["departurePinHash"])||!JToken.DeepEquals(original["startSnapshotHash"],r["startSnapshotHash"]))throw new ArgumentException("b_assignment_route_original_conflict");}
  static void Need(bool condition){if(!condition)throw new ArgumentException("b_assignment_original_contract_conflict");}
  static JObject Obj(JToken t)=>RunWaveManifestReader.Obj(t);
  static long Nat(JObject p,string k,long min=1,long max=9007199254740991)=>BDirectAttackInput.Nat(p,k,min,max);
  static void Hash(JToken t)=>RunWaveManifestReader.Hash(t);
  static void Exact(JObject p){BDirectAttackInput.Fields(p,"numerator","denominator");Need(p["numerator"]?.Type==JTokenType.String&&p["denominator"]?.Type==JTokenType.String&&System.Text.RegularExpressions.Regex.IsMatch((string)p["numerator"],"\\A[0-9]+\\z")&&System.Text.RegularExpressions.Regex.IsMatch((string)p["denominator"],"\\A[1-9][0-9]*\\z"));CombatExactValue.From(p);}
  public static BDepartureAssignment Read(string json,string etag,string account,string run){
   var p=SafeTransitionWire.Parse(json,512*1024);BDirectAttackInput.Fields(p,"version","sourceVersion","accountId","runId","selectionRevision","catalog","selection","refitSnapshot","growthSourceHash","commander","definition","definitionHash","actors","applicability","predepartureStatsIncluded","runtimeBindingIncluded","effectiveCombatStatsIncluded","canStart","assignmentHash","origin","departurePinHash","runtimeProfileHash","startSnapshotHash","packetHash");
   Need((string)p["version"]==Version&&(string)p["sourceVersion"]=="combat.b-persisted-source.v1.20261006");RunWaveManifestReader.Id(p["accountId"]);RunWaveManifestReader.Id(p["runId"]);Need((string)p["accountId"]==account&&(string)p["runId"]==run);
   foreach(string k in new[]{"growthSourceHash","definitionHash","assignmentHash","departurePinHash","runtimeProfileHash","startSnapshotHash","packetHash"})Hash(p[k]);
   var body=(JObject)p.DeepClone();body.Remove("packetHash");Need(etag==(string)p["packetHash"]&&BDirectAttackInput.Digest(body)=="sha256:"+etag);
   foreach(string k in new[]{"assignmentHash","origin","departurePinHash","runtimeProfileHash","startSnapshotHash"})body.Remove(k);Need(BDirectAttackInput.Digest(body)=="sha256:"+(string)p["assignmentHash"]);
   var origin=Obj(p["origin"]);RunWaveManifestReader.DepartureOrigin(origin);Need((string)origin["accountId"]==account&&(string)origin["runId"]==run&&(string)origin["departureVersion"]=="departure-b-preparation-v1.20261006");
   Nat(p,"selectionRevision");SafeTransitionWire.Boolean(p,"predepartureStatsIncluded",true);foreach(string k in new[]{"runtimeBindingIncluded","effectiveCombatStatsIncluded","canStart"})SafeTransitionWire.Boolean(p,k,false);
   var catalog=Obj(p["catalog"]);BDirectAttackInput.Fields(catalog,"version","revision","status");RunWaveManifestReader.Id(catalog["version"]);Nat(catalog,"revision");Need((string)catalog["status"]=="PERSISTED_ACCOUNT_SOURCE");
   var selection=Obj(p["selection"]);var selected=new BSelectionDefinition(selection).Capture();Need(JToken.DeepEquals(selected,selection));
   foreach(var row in (JArray)selection["placements"]){RunWaveManifestReader.Id(row["carriageId"],128);if((string)row["occupant"]["kind"]=="OPERATOR")RunWaveManifestReader.Id(row["occupant"]["operatorId"],128);}
   var snapshot=Obj(p["refitSnapshot"]);BDirectAttackInput.Fields(snapshot,"revision","definitionHash","sourceHash","refit");Nat(snapshot,"revision");Hash(snapshot["definitionHash"]);Hash(snapshot["sourceHash"]);var refit=Obj(snapshot["refit"]);BDirectAttackInput.Fields(refit,"version","accountId","selectionRevision","rankRevision","sourceStatus","cars");Need((string)refit["version"]==BCarriageBuffDefinitions.Version&&(string)refit["accountId"]==account&&(string)refit["sourceStatus"]=="PERSISTED_ACCOUNT_SOURCE"&&JToken.DeepEquals(refit["selectionRevision"],p["selectionRevision"]));Nat(refit,"selectionRevision",1,9007199254740990);Nat(refit,"rankRevision",1,9007199254740990);
   var cars=RunWaveManifestReader.Rows(refit["cars"],1,5);var placements=(JArray)selection["placements"];Need(cars.Count==placements.Count);var instances=new HashSet<string>();var slots=new HashSet<long>();var ranks=new Dictionary<string,long>();
   foreach(var token in cars){var c=Obj(token);BDirectAttackInput.Fields(c,"slot","instanceId","carriageId","occupant","type","rank");var slot=Nat(c,"slot",0,4);Need(slots.Add(slot)&&instances.Add(RunWaveManifestReader.Id(c["instanceId"],128)));RunWaveManifestReader.Id(c["carriageId"],128);var placement=placements.SingleOrDefault(s=>(long)s["slot"]==slot);Need(placement!=null&&JToken.DeepEquals(c["carriageId"],placement["carriageId"])&&JToken.DeepEquals(c["occupant"],placement["occupant"]));var type=BuffWire.Type(c["type"]);long rank=Nat(c,"rank",1,5);Need(!ranks.TryGetValue(type,out var prior)||prior==rank);ranks[type]=rank;}
   var definition=Obj(p["definition"]);Need((string)definition["version"]==BNamedOperatorSource.DefinitionVersion&&(string)p["definitionHash"]=="08c88c65406c55a00847fcd7042ec90fae4d834c725d105ba6e574f5d9b7ecbd"&&JToken.DeepEquals(definition["definitionHash"],p["definitionHash"]));var unsigned=(JObject)definition.DeepClone();unsigned.Remove("definitionHash");Need(BDirectAttackInput.Digest(unsigned)=="sha256:"+(string)p["definitionHash"]);
   var rows=RunWaveManifestReader.Rows(p["actors"],1,2);var ids=new HashSet<string>();var actorSlots=new HashSet<long>();var operatorCars=cars.Where(c=>(string)c["occupant"]["kind"]=="OPERATOR").ToArray();Need(rows.Count==operatorCars.Length);
   foreach(var token in rows){var a=Obj(token);BDirectAttackInput.Fields(a,"operatorId","physicalSlot","carriageInstanceId","carriageDefinitionId","physicalRevision","profileVersion","definitionHash","owned","stats","skillMagnitude","skills","basic","interception","effectiveStats");var id=RunWaveManifestReader.Id(a["operatorId"],128);Need((id=="op_kyle"||id=="op_sable")&&ids.Add(id)&&actorSlots.Add(Nat(a,"physicalSlot",0,4)));Nat(a,"physicalRevision");RunWaveManifestReader.Id(a["carriageInstanceId"]);RunWaveManifestReader.Id(a["carriageDefinitionId"]);RunWaveManifestReader.Id(a["profileVersion"]);Need(JToken.DeepEquals(a["definitionHash"],p["definitionHash"])&&a["effectiveStats"]?.Type==JTokenType.Null);
    var car=operatorCars.SingleOrDefault(c=>(string)c["occupant"]["operatorId"]==id);Need(car!=null&&JToken.DeepEquals(a["physicalSlot"],car["slot"])&&JToken.DeepEquals(a["carriageInstanceId"],car["instanceId"])&&JToken.DeepEquals(a["carriageDefinitionId"],car["carriageId"]));
    var owned=Obj(a["owned"]);BDirectAttackInput.Fields(owned,"operatorId","skillTraining","promotion","revision","level","trainingSourceHash");Need((string)owned["operatorId"]==id);Nat(owned,"skillTraining",1,5);Nat(owned,"promotion",0,3);Nat(owned,"revision");Nat(owned,"level",1,50);Hash(owned["trainingSourceHash"]);
    var stats=Obj(a["stats"]);BDirectAttackInput.Fields(stats,"maxHp","attack","defense","basicIntervalMs");Nat(stats,"maxHp");Nat(stats,"defense",0);Nat(stats,"basicIntervalMs");Exact(Obj(stats["attack"]));Exact(Obj(a["skillMagnitude"]));
    var approved=RunWaveManifestReader.Rows(definition["operators"],2,2).Single(o=>(string)o["operatorId"]==id);Need(JToken.DeepEquals(a["profileVersion"],approved["profileVersion"])&&JToken.DeepEquals(a["skills"],approved["skills"])&&JToken.DeepEquals(a["basic"],approved["basicRange"])&&JToken.DeepEquals(stats["basicIntervalMs"],approved["base"]["basicIntervalMs"]));var interception=((JArray)definition["interception"]["abilities"]).Single(i=>(string)i["operatorId"]==id);Need(JToken.DeepEquals(a["interception"],interception));
   }
   var applicability=Obj(p["applicability"]);BDirectAttackInput.Fields(applicability,"equipment","research","commander","runtimeEffects");Need((string)applicability["runtimeEffects"]=="NOT_PROVEN"&&(string)applicability["commander"]?["status"]=="VERIFIED_NO_ACTIVE_EFFECTS");Need(p["commander"].Type==JTokenType.Null||p["commander"] is JObject commander&&commander["ranks"] is JObject ranksObject&&ranksObject.Count==0);
   return new BDepartureAssignment(p);
  }
 }
 public sealed class BDepartureAssignmentHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BDepartureAssignmentHttpClient(IGameApiAuthenticatedHttpTransport original){transport=original??throw new ArgumentNullException(nameof(original));}
  public async Task<BDepartureAssignment> ReadAsync(string account,string run,GameApiAccessToken token,string expectedHash=null,CancellationToken cancellation=default){RunWaveManifestReader.Id(new JValue(account));RunWaveManifestReader.Id(new JValue(run));if(expectedHash!=null)RunWaveManifestReader.Hash(new JValue(expectedHash));var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("X-B-Assignment-Version",BDepartureAssignment.Version)};if(expectedHash!=null)headers.Add(new RunApiHttpHeader("If-Match",expectedHash));var response=await BResultReportHttpClient.SendOriginalAsync(transport,new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(run)+"/b-departure-assignment",null,headers),token,cancellation);var source=BDepartureAssignment.Read(response.BodyJson,response.Header("ETag"),account,run);if(expectedHash!=null&&source.PacketHash!=expectedHash)throw new ArgumentException("b_assignment_expected_source_conflict");return source;}
 }
}
