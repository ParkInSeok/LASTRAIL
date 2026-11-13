using System;
using System.Text.RegularExpressions;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 public sealed class BNativeNodeSourceSnapshot {
  public const string Version="combat.b-native-node-source.v1.20261005";
  readonly JObject original;
  public BRouteBinding Route{get;}public BBoardingEventsNodeSnapshot Boarding{get;}public BInterceptDefinitionSnapshot InterceptDefinition{get;}
  public string Hash=>(string)original["packetHash"];public bool AcceptedTimingSupported=>false;
  BNativeNodeSourceSnapshot(JObject p,BRouteBinding route,BBoardingEventsNodeSnapshot boarding,BInterceptDefinitionSnapshot intercept){original=(JObject)p.DeepClone();Route=route;Boarding=boarding;InterceptDefinition=intercept;}
  public JObject Capture()=>(JObject)original.DeepClone();
  public static BNativeNodeSourceSnapshot Read(string json,string independentHash,string account,string run){
   var p=SafeTransitionWire.Parse(json);BDirectAttackInput.Fields(p,"version","accountId","runId","nodeId","mapRevision","phase","pendingEdgeId","holdIds","startSnapshotHash","departurePinHash","nativeOriginHash","nativeContextId","originalNativeOrigin","seedHash","originalNodeSource","boardingSource","interceptDefinition","interceptDefinitionPacket","routeBinding","interceptionAcceptedTiming","definitionAvailable","runtimeSupported","canStart","acceptedRouteEvidenceSupported","requiredConsumerOwners","packetHash");
   SafeTransitionWire.Pin(p,"packetHash",independentHash);if((string)p["version"]!=Version)throw new ArgumentException("b_native_source_version_conflict");
   SafeTransitionWire.Boolean(p,"definitionAvailable",true);foreach(var k in new[]{"runtimeSupported","canStart","acceptedRouteEvidenceSupported"})SafeTransitionWire.Boolean(p,k,false);
   if(p["interceptionAcceptedTiming"].Type!=JTokenType.Null)throw new ArgumentException("b_native_accepted_timing_not_supplied_by_contract");
   foreach(var k in new[]{"nativeOriginHash","seedHash"})if(p[k].Type!=JTokenType.String||!Regex.IsMatch((string)p[k],"^[a-f0-9]{64}$"))throw new ArgumentException("b_native_source_hash_invalid");SafeTransitionWire.Ids(p["requiredConsumerOwners"]);
   var raw=(JObject)p["originalNodeSource"];var r=(JObject)p["routeBinding"];var route=BRouteBinding.Read(r.ToString(),(string)r["packetHash"],account,run);
   foreach(var k in new[]{"accountId","runId","mapRevision","phase","pendingEdgeId","holdIds","startSnapshotHash","departurePinHash","nativeContextId","originalNativeOrigin"})if(!JToken.DeepEquals(p[k],r[k]))throw new ArgumentException("b_native_route_source_conflict");
   if(route.NativeNode==null||!JToken.DeepEquals(raw,route.NativeNode.Capture())||!JToken.DeepEquals(p["nodeId"],raw["nodeId"])||!JToken.DeepEquals(p["seedHash"],raw["seedHash"]))throw new ArgumentException("b_native_original_node_conflict");
   var board=(JObject)p["boardingSource"];var boarding=BBoardingEventsNodeSnapshot.Read(board,(string)board["sourceHash"],BRouteBinding.Trust(raw));if(!JToken.DeepEquals(boarding.OriginalSource.Capture(),raw))throw new ArgumentException("b_native_original_boarding_conflict");
   var ip=(JObject)p["interceptDefinitionPacket"];var intercept=BInterceptDefinitionSnapshot.Read(ip,(string)ip["packetHash"]);if(!JToken.DeepEquals(ip["definition"],p["interceptDefinition"])||!JToken.DeepEquals(ip["definition"]["originalDefinition"],board["definition"]))throw new ArgumentException("b_native_intercept_definition_conflict");
   return new BNativeNodeSourceSnapshot(p,route,boarding,intercept);
  }
  public void RequireAcceptedTiming()=>throw new InvalidOperationException("b_native_original_accepted_interception_timing_unavailable");
 }
}
