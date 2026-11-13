using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // One original snapshot's declared sources. No executor/profile promotion.
 public sealed class BDepartureSourceBundle {
  public const string Version="combat.b-departure-source-bundle.v1.20261006";
  readonly JObject original;
  public BDepartureAssignment Assignment {get;}
  public BNativeNodeSourceSnapshot NativeNode {get;}
  public string AccountId=>Assignment.AccountId;public string RunId=>Assignment.RunId;
  public string PacketHash=>(string)original["packetHash"];
  public string ContextId=>(string)original["originalCombatSource"]["contextId"];
  public JObject OriginalCombatSource=>(JObject)original["originalCombatSource"].DeepClone();
  public JObject OriginalRiskDefinition=>(JObject)original["originalRiskDefinition"].DeepClone();
  public IReadOnlyList<string> MissingSources=>Array.AsReadOnly(((JArray)original["missingSources"]).Select(x=>(string)x).ToArray());
  public bool CanStart=>false;
  BDepartureSourceBundle(JObject p,BDepartureAssignment assignment,BNativeNodeSourceSnapshot native){original=(JObject)p.DeepClone();Assignment=assignment;NativeNode=native;}
  public JObject Capture()=>(JObject)original.DeepClone();
  public BDepartureFormationAdapter PrepareFormation()=>new BDepartureFormationAdapter(Assignment);
  public BOrdinaryGeometryBinding ReadPreparedGeometry(JObject authored,string independentHash,BOrdinaryGeometrySnapshot definition)=>BOrdinaryGeometryBinding.ReadPrepared(authored,independentHash,definition,PrepareFormation().Placement,ContextId,PacketHash);
  public void RequireSameDeparture(BRouteBinding route){Assignment.RequireSameDeparture(route);if(ContextId!=(string)route.Capture()["nativeContextId"])throw new ArgumentException("b_bundle_route_context_conflict");}
  public void RequireRuntimeBinding()=>Assignment.RequireRuntimeBinding();
  internal void RequireOriginalSource(BDepartureSourceBundle next){
   if(next==null)throw new ArgumentNullException(nameof(next));
   foreach(string key in new[]{"accountId","runId","startSnapshotHash","departurePinHash","originalOrigin","assignment","originalCombatSource","combatSourceHash","runtimeProfileHash","originalRiskDefinition"})
    if(!JToken.DeepEquals(original[key],next.original[key]))throw new ArgumentException("b_bundle_frozen_source_changed");
  }
  static void Need(bool yes){if(!yes)throw new ArgumentException("b_departure_bundle_source_conflict");}
  static JObject Obj(JToken t)=>RunWaveManifestReader.Obj(t);
  public static BDepartureSourceBundle Read(string json,string independentHash,string account,string run){
   var p=SafeTransitionWire.Parse(json,512*1024);
   BDirectAttackInput.Fields(p,"version","accountId","runId","startSnapshotHash","departurePinHash","originalOrigin","assignment","originalCombatSource","combatSourceHash","runtimeProfileHash","originalRiskDefinition","nativeNodeSource","mapRevision","phase","pendingEdgeId","sourceStatus","predepartureStatsIncluded","effectiveCombatStatsIncluded","runtimeBindingIncluded","canStart","missingSources","packetHash");
   SafeTransitionWire.Pin(p,"packetHash",independentHash);Need((string)p["version"]==Version&&SafeTransitionWire.Id(p,"accountId")==account&&SafeTransitionWire.Id(p,"runId")==run&&(string)p["sourceStatus"]=="ORIGINAL_FROZEN_SOURCE_NOT_RUNTIME_CONSUMPTION");
   SafeTransitionWire.Boolean(p,"predepartureStatsIncluded",true);foreach(string key in new[]{"effectiveCombatStatsIncluded","runtimeBindingIncluded","canStart"})SafeTransitionWire.Boolean(p,key,false);
   foreach(string key in new[]{"startSnapshotHash","departurePinHash","combatSourceHash","runtimeProfileHash"})RunWaveManifestReader.Hash(p[key]);
   BDirectAttackInput.Nat(p,"mapRevision",1);RunWaveManifestReader.One(p["phase"],"READY","TRAVELLING","ACTIVE","FINAL_NODE_COMPLETE");
   if((string)p["phase"]=="TRAVELLING")RunWaveManifestReader.Id(p["pendingEdgeId"]);else RunWaveManifestReader.Null(p["pendingEdgeId"]);
   var ap=Obj(p["assignment"]);var assignment=BDepartureAssignment.Read(ap.ToString(),(string)ap["packetHash"],account,run);
   foreach(string key in new[]{"startSnapshotHash","departurePinHash","runtimeProfileHash"})Need(JToken.DeepEquals(p[key],ap[key]));Need(JToken.DeepEquals(p["originalOrigin"],ap["origin"]));
   var source=Obj(p["originalCombatSource"]);BDirectAttackInput.Fields(source,"version","binding","contextId","profile","riskApplicationVersion","rawProfiles");
   Need((string)source["version"]=="combat.b-departure-source.v1.20261003"&&SafeTransitionWire.Digest(source)==(string)p["combatSourceHash"]);var context=RunWaveManifestReader.Id(source["contextId"]);Need(context!=run);
   var binding=Obj(source["binding"]);BDirectAttackInput.Fields(binding,"accountId","runId","operationPreviewId","operationId","selectedRisk","operationDefinitionVersion","operationPlanHash","operationPublicationHash");var origin=Obj(p["originalOrigin"]);
   foreach(string key in new[]{"accountId","runId","operationPreviewId","operationId","selectedRisk"})Need(JToken.DeepEquals(binding[key],origin[key]));
   Need(JToken.DeepEquals(binding["operationDefinitionVersion"],origin["definitionVersion"])&&JToken.DeepEquals(binding["operationPlanHash"],origin["planHash"])&&JToken.DeepEquals(binding["operationPublicationHash"],origin["publicationHash"]));
   var profile=Obj(source["profile"]);BDirectAttackInput.Fields(profile,"id","version","executorVersion","definition");foreach(string key in new[]{"id","version","executorVersion"})RunWaveManifestReader.Id(profile[key]);var definition=Obj(profile["definition"]);Need(SafeTransitionWire.Digest(profile)==(string)p["runtimeProfileHash"]);
   var risk=Obj(p["originalRiskDefinition"]);var unsigned=(JObject)risk.DeepClone();unsigned.Remove("definitionHash");Need((string)source["riskApplicationVersion"]==BOperationRiskSource.Version&&(string)risk["version"]==BOperationRiskSource.Version&&(string)risk["definitionHash"]=="sha256:7889e8553ee2a64b383294e8f30edf87f78691096935cc74abf0b34b6652a5ce"&&BDirectAttackInput.Digest(unsigned)==(string)risk["definitionHash"]);
   var raw=RunWaveManifestReader.Rows(source["rawProfiles"],1,64);var actors=new HashSet<string>();foreach(var row in raw){var actor=Obj(row);BDirectAttackInput.Fields(actor,"actorRef","hp","actions");Need(actors.Add(RunWaveManifestReader.Id(actor["actorRef"])));Raw(Obj(actor["hp"]),true);foreach(var action in RunWaveManifestReader.Rows(actor["actions"],1,64))Raw(Obj(action),false);}
   var nativeOrigin=definition.Property("bNativeNodeOrigin");JObject declaredNative=null;
   if(nativeOrigin!=null){declaredNative=Obj(nativeOrigin.Value);RunWaveManifestReader.SelfHash(declaredNative,"originHash");Need((string)declaredNative["version"]=="combat.b-native-node-origin.v1.20261005");}
   BNativeNodeSourceSnapshot native=null;
   if(p["nativeNodeSource"].Type!=JTokenType.Null){Need(declaredNative!=null);var np=Obj(p["nativeNodeSource"]);native=BNativeNodeSourceSnapshot.Read(np.ToString(),(string)np["packetHash"],account,run);assignment.RequireSameDeparture(native.Route);
    foreach(string key in new[]{"startSnapshotHash","departurePinHash","mapRevision","phase","pendingEdgeId"})Need(JToken.DeepEquals(p[key],np[key]));
    Need(JToken.DeepEquals(p["originalOrigin"],np["originalNativeOrigin"])&&JToken.DeepEquals(p["originalOrigin"],np["originalNodeSource"]["origin"])&&(string)np["nativeContextId"]==context&&(string)np["originalNodeSource"]["nativeContextId"]==context);
    Need(JToken.DeepEquals(declaredNative["originHash"],np["nativeOriginHash"])&&JToken.DeepEquals(declaredNative["seedHash"],np["seedHash"])&&JToken.DeepEquals(declaredNative["boardingDefinition"],np["boardingSource"]["definition"])&&JToken.DeepEquals(declaredNative["interceptDefinition"],np["interceptDefinition"]));
   }
   var expected=new List<string>{"ACTUAL_B_ASSIGNMENT_EXECUTOR_BINDING","APPROVED_B_EFFECTIVE_PROFILE","ORIGINAL_B_GEOMETRY_AND_CHASSIS"};if(native==null)expected.Add(declaredNative==null?"ORIGINAL_NATIVE_NODE_ORIGIN":"SELECTED_COMBAT_NODE");
   Need(JToken.DeepEquals(p["missingSources"],new JArray(expected)));return new BDepartureSourceBundle(p,assignment,native);
  }
  static void Raw(JObject p,bool hp){BDirectAttackInput.Fields(p,hp?new[]{"sourceId","kind","value","riskAppliedVersion"}:new[]{"sourceId","kind","value","riskAppliedVersion","progressAppliedVersion"});RunWaveManifestReader.Id(p["sourceId"]);RunWaveManifestReader.One(p["kind"],hp?new[]{"BASE_HP","EHP_BUDGET"}:new[]{"RAW_ACTION_DAMAGE"});RunWaveManifestReader.Null(p["riskAppliedVersion"]);if(!hp)RunWaveManifestReader.Null(p["progressAppliedVersion"]);CombatExactValue.From(Obj(p["value"]));}
 }
 public sealed class BDepartureSourceBundleHttpClient {
  readonly IGameApiAuthenticatedHttpTransport transport;
  public BDepartureSourceBundleHttpClient(IGameApiAuthenticatedHttpTransport original){transport=original??throw new ArgumentNullException(nameof(original));}
  public async Task<BDepartureSourceBundle> ReadAsync(string account,string run,GameApiAccessToken token,string expectedHash=null,CancellationToken cancellation=default){
   RunWaveManifestReader.Id(new JValue(account));RunWaveManifestReader.Id(new JValue(run));if(expectedHash!=null)RunWaveManifestReader.Hash(new JValue(expectedHash));
   var headers=new List<RunApiHttpHeader>{new RunApiHttpHeader("X-B-Departure-Source-Version",BDepartureSourceBundle.Version)};if(expectedHash!=null)headers.Add(new RunApiHttpHeader("If-Match",expectedHash));
   var response=await BResultReportHttpClient.SendOriginalAsync(transport,new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(run)+"/b-departure-source",null,headers),token,cancellation);
   var source=BDepartureSourceBundle.Read(response.BodyJson,response.Header("ETag"),account,run);if(expectedHash!=null&&source.PacketHash!=expectedHash)throw new ArgumentException("b_bundle_expected_source_conflict");return source;
  }
 }
}
