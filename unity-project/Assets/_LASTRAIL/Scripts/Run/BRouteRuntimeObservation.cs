using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Immutable in-process original owner capture. This is not a network wire or
 // accepted route evidence. Historical wave journal ticks are not checkpoint ticks.
 public sealed class BRouteRuntimeObservation {
  readonly JObject route,node;
  readonly string key;
  internal BRouteRuntimeObservation(BRouteBinding binding,JObject originalNodeSession){
   if(binding?.NativeNode==null||originalNodeSession==null)throw new InvalidOperationException("b_runtime_observation_source_unavailable");
   route=binding.Capture();node=(JObject)originalNodeSession.DeepClone();
   var n=node["runtime"] as JObject;var o=node["observation"]?["originalObservation"] as JObject;
   if(n==null||o==null||!JToken.DeepEquals(node["node"],binding.NativeNode.Capture())||!JToken.DeepEquals(o["native"],n)||BDirectAttackInput.Digest(n)!=(string)o["nativeHash"]||!JToken.DeepEquals(n["travel"]?["origin"],route["originalNativeOrigin"])||!JToken.DeepEquals(n["travel"]?["nativeContextId"],route["nativeContextId"])||!JToken.DeepEquals(n["travel"]?["nodeId"],node["node"]?["nodeId"])||!JToken.DeepEquals(n["travel"]?["nodeSourceHash"],node["node"]?["sourceHash"]))throw new ArgumentException("b_runtime_observation_original_conflict");
   var hazards=BResidualHazardObservation.ReadOwned(n).Capture();
   if(!JToken.DeepEquals(hazards,node["observation"]["hazards"])||(long)n["state"]["direct"]["clock"]["completedSteps"]!=(long)n["travel"]["motion"]["currentTick"]||(long)n["sequence"]!=(long)n["travel"]["sequence"]||((JArray)n["commands"]).Count!=(long)n["travel"]["commandRevision"])throw new ArgumentException("b_runtime_observation_clock_or_hazard_conflict");
   var commands=(JArray)n["commands"];
   if(commands.Count>0&&(string)commands.Last["eventId"]!=(string)o["acceptedEventId"])throw new ArgumentException("b_runtime_observation_command_conflict");
   key=BDirectAttackInput.Digest(new JObject{["route"]=route.DeepClone(),["nodeSession"]=node.DeepClone()});
  }
  public string DeliveryKey=>key;
  public string CheckpointHash=>(string)node["observation"]["originalObservation"]["nativeHash"];
  public string OriginalCommandEventId=>(string)node["observation"]["originalObservation"]["acceptedEventId"];
  public long SourceRevision=>(long)node["observation"]["originalObservation"]["sourceRevision"];
  public JObject OriginalRoute=>(JObject)route.DeepClone();
  public JObject OriginalNodeSession=>(JObject)node.DeepClone();
  public JObject OriginalCheckpoint=>(JObject)node["runtime"].DeepClone();
  public JObject OriginalClock=>(JObject)node["runtime"]["state"]["direct"]["clock"].DeepClone();
  public JObject OriginalHazards=>(JObject)node["observation"]["hazards"].DeepClone();
  public JArray OriginalWaveClearEvents=>new JArray(((JArray)node["runtime"]["waveEntry"]["events"]).Where(x=>(string)x["kind"]=="WAVE_CLEAR").Select(x=>x.DeepClone()));
  public JObject OriginalTravel=>(JObject)node["runtime"]["travel"].DeepClone();
  public JObject OriginalEventJournal=>node["runtime"]["originalEvents"] is JObject journal?(JObject)journal.DeepClone():null;
  public void RequireOriginalCommand(){if(string.IsNullOrEmpty(OriginalCommandEventId))throw new InvalidOperationException("b_runtime_original_command_unavailable");}
  public void RequireTravelArrival(){throw new InvalidOperationException("b_original_travel_arrival_event_source_unavailable");}
 }
}
