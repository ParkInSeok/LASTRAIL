using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Frozen selections and owned growth only. This source does not map formation
 // slots to carriage occupants or calculate effective combat profiles.
 public sealed class BDepartureRuntimeInput {
  public const string Version="combat.b-departure-runtime-input.v1.20261006";
  readonly JObject original;
  public string PacketHash=>(string)original["packetHash"];public JObject Capture()=>(JObject)original.DeepClone();
  public JObject OriginalFormation=>(JObject)original["originalDeparture"]["formation"].DeepClone();
  public JObject OriginalLoadout=>(JObject)original["originalDeparture"]["loadout"].DeepClone();
  public JObject OriginalOwnedState=>(JObject)original["originalDeparture"]["ownedState"].DeepClone();
  public bool CanStart=>false;
  BDepartureRuntimeInput(JObject packet){original=(JObject)packet.DeepClone();}
  public void RequireOccupantBinding()=>throw new InvalidOperationException("b_original_effective_profile_and_occupant_binding_unavailable");
  public void RequireSameDeparture(BRouteBinding route){var r=route?.Capture();if(r==null||new[]{"accountId","runId","originalNativeOrigin","nativeContextId","departurePinHash","startSnapshotHash"}.Any(k=>!JToken.DeepEquals(original[k],r[k])))throw new ArgumentException("b_departure_input_native_original_conflict");}
  public static BDepartureRuntimeInput Read(string json,string etag,string account,string run){
   var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,"version","accountId","runId","originalNativeOrigin","nativeContextId","departurePinHash","startSnapshotHash","originalDeparture","effectiveCombatStatsIncluded","runtimeOccupantBindingIncluded","selectionSlotIsCarriageSlot","canStart","missingSources","packetHash");
   var body=(JObject)p.DeepClone();body.Remove("packetHash");
   if((string)p["version"]!=Version||(string)p["accountId"]!=account||(string)p["runId"]!=run||etag!=(string)p["packetHash"]||BDirectAttackInput.Digest(body).Substring(7)!=etag)throw new ArgumentException("b_departure_input_original_conflict");
   foreach(string k in new[]{"packetHash","departurePinHash","startSnapshotHash"})if(p[k]?.Type!=JTokenType.String||!Regex.IsMatch((string)p[k],"^[a-f0-9]{64}$"))throw new ArgumentException("b_departure_input_hash_invalid");
   foreach(string k in new[]{"effectiveCombatStatsIncluded","runtimeOccupantBindingIncluded","selectionSlotIsCarriageSlot","canStart"})SafeTransitionWire.Boolean(p,k,false);
   if(!JToken.DeepEquals(p["missingSources"],new JArray("ORIGINAL_EFFECTIVE_COMBAT_PROFILE_AND_CARRIAGE_OCCUPANT_BINDING"))||p["originalNativeOrigin"]?.Type!=JTokenType.Object||p["nativeContextId"]?.Type!=JTokenType.String||string.IsNullOrEmpty((string)p["nativeContextId"]))throw new ArgumentException("b_departure_input_source_invalid");
   if(RunWaveManifestReader.Id(p["nativeContextId"])==run)throw new ArgumentException("b_departure_input_native_context_conflict");
   var origin=RunWaveManifestReader.Obj(p["originalNativeOrigin"]);RunWaveManifestReader.DepartureOrigin(origin);if((string)origin["accountId"]!=account||(string)origin["runId"]!=run)throw new ArgumentException("b_departure_input_origin_conflict");
   var d=RunWaveManifestReader.Obj(p["originalDeparture"]);if((string)d?["version"]!="departure-v4")throw new ArgumentException("b_departure_input_version_invalid");BDirectAttackInput.Nat(d,"formationRevision",1);BDirectAttackInput.Nat(d,"loadoutRevision",1);
   var f=RunWaveManifestReader.Obj(d["formation"]);BDirectAttackInput.Fields((JObject)f,"schemaVersion","operators");if(BDirectAttackInput.Nat(f,"schemaVersion",1,1)!=1)throw new ArgumentException();
   var operators=f["operators"] as JArray;var load=RunWaveManifestReader.Obj(d["loadout"]);BDirectAttackInput.Fields((JObject)load,"rulesVersion","trainId","carriages");var cars=load["carriages"] as JArray;
   if(operators==null||operators.Count!=5||cars==null||cars.Count<2||cars.Count>4||(string)load["rulesVersion"]!="train-loadout-v1")throw new ArgumentException("b_departure_input_selection_invalid");
   void Slots(JArray rows,string id,int max){foreach(var row in rows){BDirectAttackInput.Fields(RunWaveManifestReader.Obj(row),"slot",id);BDirectAttackInput.Nat(row,"slot",0,max);BDirectAttackInput.Text(row,id);}if(rows.Select(x=>(long)x["slot"]).Distinct().Count()!=rows.Count||rows.Select(x=>(string)x[id]).Distinct().Count()!=rows.Count||rows.OrderBy(x=>(long)x["slot"]).Where((x,i)=>(long)x["slot"]!=i).Any())throw new ArgumentException("b_departure_input_slots_invalid");}
   Slots(operators,"operatorId",4);Slots(cars,"carriageId",3);
   RunWaveManifestReader.Id(load["trainId"]);var owned=RunWaveManifestReader.Obj(d["ownedState"]);RunWaveManifestReader.Id(RunWaveManifestReader.Obj(owned["train"])["trainId"]);var ownedOperators=owned?["operators"] as JArray;var ownedCars=owned?["carriages"] as JArray;
   if(ownedOperators==null||ownedOperators.Count!=5||ownedCars==null||ownedCars.Count!=cars.Count||!operators.Select(x=>(string)x["operatorId"]).OrderBy(x=>x).SequenceEqual(ownedOperators.Select(x=>(string)x["operatorId"]).OrderBy(x=>x))||!cars.Select(x=>(string)x["carriageId"]).OrderBy(x=>x).SequenceEqual(ownedCars.Select(x=>(string)x["carriageId"]).OrderBy(x=>x))||(string)load["trainId"]!=(string)owned["train"]?["trainId"])throw new ArgumentException("b_departure_input_owned_selection_conflict");
   foreach(var row in ownedOperators){BDirectAttackInput.Nat(row,"revision",1);BDirectAttackInput.Nat(row,"skillTraining",0);BDirectAttackInput.Nat(row,"promotion",0);}foreach(var row in ownedCars){BDirectAttackInput.Nat(row,"revision",1);BDirectAttackInput.Nat(row,"level",1);}BDirectAttackInput.Nat(owned["train"],"revision",1);BDirectAttackInput.Nat(owned["train"],"level",1);
   return new BDepartureRuntimeInput(p);
  }
 }
 public sealed class BDepartureRuntimeInputClient {
  readonly IGameApiAuthenticatedHttpTransport transport;public BDepartureRuntimeInputClient(IGameApiAuthenticatedHttpTransport original){transport=original??throw new ArgumentNullException(nameof(original));}
  public async Task<BDepartureRuntimeInput> ReadAsync(string account,string run,GameApiAccessToken token,string expectedPacketHash=null,CancellationToken cancellation=default){
   if(string.IsNullOrEmpty(account)||account.Length>200||string.IsNullOrEmpty(run)||run.Length>200||expectedPacketHash!=null&&!Regex.IsMatch(expectedPacketHash,"^[a-f0-9]{64}$"))throw new ArgumentException("b_departure_input_request_invalid");
   var headers=new System.Collections.Generic.List<RunApiHttpHeader>{new RunApiHttpHeader("X-B-Departure-Input-Version",BDepartureRuntimeInput.Version)};if(expectedPacketHash!=null)headers.Add(new RunApiHttpHeader("If-Match",expectedPacketHash));
   var response=await BResultReportHttpClient.SendOriginalAsync(transport,new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(run)+"/b-departure-runtime-input",null,headers),token,cancellation);var source=BDepartureRuntimeInput.Read(response.BodyJson,response.Header("ETag"),account,run);if(expectedPacketHash!=null&&source.PacketHash!=expectedPacketHash)throw new ArgumentException("b_departure_input_expected_source_conflict");return source;
  }
 }
}
