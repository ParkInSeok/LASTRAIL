using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public enum BCombatBattlefield { ExternalVertical, SpecificNodeInterior }
 public enum BCombatMode { Solo, Coop, WorldSolo, WorldCoop }
 public enum BCombatAuthority { UnitySolo, UnityCoopHost, ServerWorld }
 public sealed class BCombatSlot {
  public string CarriageId {get;} public string OperatorId {get;}
  public bool IsCargo=>OperatorId==null;
  public BCombatSlot(string carriageId,string operatorId=null){BCombatFormation.Identity(carriageId);if(operatorId!=null)BCombatFormation.Identity(operatorId);CarriageId=carriageId;OperatorId=operatorId;}
 }
 // Unity local policy descriptor, not a Server START schema or a migrated old snapshot.
 public sealed class BCombatFormation {
  public const string LocalVersion="unity.combat-b.formation-local.v1";
  readonly BCombatSlot[] slots;
  public string TrainId {get;} public BCombatMode Mode {get;} public BCombatBattlefield Battlefield {get;} public string InternalNodeId {get;}
  public BCombatAuthority Authority=>Mode==BCombatMode.Solo?BCombatAuthority.UnitySolo:Mode==BCombatMode.Coop?BCombatAuthority.UnityCoopHost:BCombatAuthority.ServerWorld;
  public IReadOnlyList<BCombatSlot> Slots=>Array.AsReadOnly(slots);
  public BCombatFormation(string trainId,BCombatMode mode,BCombatBattlefield battlefield,IEnumerable<BCombatSlot> orderedSlots,string internalNodeId=null){
   Identity(trainId);if(!Enum.IsDefined(typeof(BCombatMode),mode)||!Enum.IsDefined(typeof(BCombatBattlefield),battlefield))throw new ArgumentException("Unsupported B profile");
   if(battlefield==BCombatBattlefield.SpecificNodeInterior)Identity(internalNodeId);else if(internalNodeId!=null)throw new ArgumentException("External profile has no interior node");
   slots=orderedSlots?.ToArray()??throw new ArgumentNullException(nameof(orderedSlots));if(slots.Length<1||slots.Length>5||slots.Any(s=>s==null))throw new ArgumentException("B deployment requires1..5 carriage slots");
   var identities=new HashSet<string>(StringComparer.Ordinal){trainId};foreach(var slot in slots){if(!identities.Add(slot.CarriageId)||slot.OperatorId!=null&&!identities.Add(slot.OperatorId))throw new ArgumentException("Duplicate deployment identity");if(slot.OperatorId!=null)BCombatRoleRanges.For(slot.OperatorId);}
   TrainId=trainId;Mode=mode;Battlefield=battlefield;InternalNodeId=internalNodeId;
  }
  internal static void Identity(string id){if(string.IsNullOrWhiteSpace(id)||id.Length>128)throw new ArgumentException("Bounded stable identity required");}
  // Compatibility check only. Sparse persisted slots must never be compacted for
  // this historical dense execution schema; prepared placement has its own version.
  public void RequirePhysicalPlacement(BCombatPhysicalPlacement placement){
   if(placement==null)throw new ArgumentNullException(nameof(placement));
   if(slots.Length!=placement.OccupiedCount)throw new ArgumentException("b_formation_physical_placement_conflict");
   for(int i=0;i<slots.Length;i++){
    var car=placement.AtPhysicalSlot(i);
    if(car==null||slots[i].CarriageId!=car.InstanceId||slots[i].OperatorId!=car.OperatorId)throw new ArgumentException("b_formation_physical_placement_conflict");
   }
  }
  public JObject Capture()=>new JObject{["version"]=LocalVersion,["trainId"]=TrainId,["mode"]=Mode.ToString(),["battlefield"]=Battlefield.ToString(),["internalNodeId"]=InternalNodeId==null?JValue.CreateNull():new JValue(InternalNodeId),["slots"]=new JArray(slots.Select(s=>new JObject{["carriageId"]=s.CarriageId,["operatorId"]=s.OperatorId==null?JValue.CreateNull():new JValue(s.OperatorId)}))};
 }
}
