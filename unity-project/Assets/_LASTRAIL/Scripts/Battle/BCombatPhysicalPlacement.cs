using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Immutable source placement, not an effective combat profile or an executable formation.
 public sealed class BCombatPlacedCar {
  readonly JObject original;
  public int PhysicalSlot {get;}
  public string InstanceId {get;}
  public string DefinitionId {get;}
  public string OperatorId {get;}
  public long? ActorPhysicalRevision {get;}
  public bool IsCargo=>OperatorId==null;
  internal BCombatPlacedCar(JObject car,JObject actor) {
   original=(JObject)car.DeepClone();PhysicalSlot=(int)car["slot"];
   InstanceId=(string)car["instanceId"];DefinitionId=(string)car["carriageId"];
   OperatorId=(string)car["occupant"]["operatorId"];
   ActorPhysicalRevision=actor==null?(long?)null:(long)actor["physicalRevision"];
  }
  public JObject Capture()=>(JObject)original.DeepClone();
 }
 public sealed class BCombatPhysicalPlacement {
  public const string Version="unity.combat-b.persisted-placement.v1.20261006";
  public const int PhysicalSlotCount=5;
  readonly BCombatPlacedCar[] slots=new BCombatPlacedCar[PhysicalSlotCount];
  readonly JObject source,departure;
  public IReadOnlyList<BCombatPlacedCar> Slots {get;}
  public int OccupiedCount=>slots.Count(s=>s!=null);
  public string AccountId=>(string)source["accountId"];
  public string RunId=>(string)source["runId"];
  public string AssignmentPacketHash=>(string)source["packetHash"];
  public bool CanExecute=>false;
  // Only the strict assignment adapter constructs this derived descriptor.
  internal BCombatPhysicalPlacement(JObject verifiedAssignment) {
   source=new JObject();foreach(string key in new[]{"accountId","runId","packetHash","assignmentHash","origin","departurePinHash","runtimeProfileHash","startSnapshotHash"})source[key]=verifiedAssignment[key].DeepClone();
   departure=(JObject)verifiedAssignment["refitSnapshot"]["refit"].DeepClone();
   foreach(JObject car in (JArray)departure["cars"]) {
    var actor=((JArray)verifiedAssignment["actors"]).OfType<JObject>().SingleOrDefault(a=>(int)a["physicalSlot"]==(int)car["slot"]);
    var placed=new BCombatPlacedCar(car,actor);slots[placed.PhysicalSlot]=placed;
   }
   Slots=Array.AsReadOnly(slots);
  }
  public BCombatPlacedCar AtPhysicalSlot(int slot) {
   if(slot<0||slot>=PhysicalSlotCount)throw new ArgumentOutOfRangeException(nameof(slot));return slots[slot];
  }
  public BCombatPlacedCar RequireCar(int slot,string instanceId) {
   var car=AtPhysicalSlot(slot);
   if(car==null||car.InstanceId!=instanceId)throw new ArgumentException("b_placement_physical_instance_conflict");return car;
  }
  public BCombatPlacedCar RequireActor(int slot,string instanceId,string operatorId) {
   var car=RequireCar(slot,instanceId);
   if(operatorId==null||car.OperatorId!=operatorId)throw new ArgumentException("b_placement_actor_conflict");return car;
  }
  // Slot envelopes only: no geometry, LOS, timing, impact or authority approval.
  public bool BasicSlotIncludesTarget(string operatorId,int targetSlot,string targetInstanceId) {
   RequireCar(targetSlot,targetInstanceId);var actor=Actor(operatorId);
   return BCombatRoleRanges.For(operatorId).BasicAttackIncludesSlot(actor.PhysicalSlot,targetSlot,PhysicalSlotCount);
  }
  public bool InterceptionSlotIncludesTarget(string operatorId,int targetSlot,string targetInstanceId) {
   RequireCar(targetSlot,targetInstanceId);var actor=Actor(operatorId);
   return BCombatRoleRanges.For(operatorId).InterceptionIncludesSlot(actor.PhysicalSlot,targetSlot,PhysicalSlotCount);
  }
  BCombatPlacedCar Actor(string id)=>slots.SingleOrDefault(s=>s!=null&&s.OperatorId!=null&&s.OperatorId==id)??throw new ArgumentException("b_placement_actor_missing");
  public JObject CaptureDeparture()=>(JObject)departure.DeepClone();
  public JObject Capture()=>new JObject {
   ["version"]=Version,["source"]=source.DeepClone(),["departure"]=departure.DeepClone(),
   ["physicalSlots"]=new JArray(Enumerable.Range(0,PhysicalSlotCount).Select(i=>new JObject {
    ["physicalSlot"]=i,["car"]=slots[i]==null?JValue.CreateNull():(JToken)slots[i].Capture(),
    ["actorPhysicalRevision"]=slots[i]?.ActorPhysicalRevision==null?JValue.CreateNull():new JValue(slots[i].ActorPhysicalRevision.Value)
   })),["canExecute"]=false
  };
 }
}
