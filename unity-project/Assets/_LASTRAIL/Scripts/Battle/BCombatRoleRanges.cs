using System;
using System.Collections.Generic;
namespace LastRail.Battle {
 // Approved slot envelopes only. Actual world geometry, skill/support range and
 // interception timing/reservation must come from their own supported contracts.
 public sealed class BCombatRoleRanges {
  public int BasicAttackSlotRadius {get;} public int? InterceptionSlotRadius {get;}
  BCombatRoleRanges(int attack,int? interception=null){BasicAttackSlotRadius=attack;InterceptionSlotRadius=interception;}
  static readonly IReadOnlyDictionary<string,BCombatRoleRanges> roles=new Dictionary<string,BCombatRoleRanges>(StringComparer.Ordinal){
   ["op_kyle"]=new BCombatRoleRanges(0,0),["op_reina"]=new BCombatRoleRanges(1),["op_eve"]=new BCombatRoleRanges(1),["op_sera"]=new BCombatRoleRanges(1),["op_mira"]=new BCombatRoleRanges(2),["op_vex"]=new BCombatRoleRanges(4),["op_nyx"]=new BCombatRoleRanges(2),["op_iris"]=new BCombatRoleRanges(1),["op_sable"]=new BCombatRoleRanges(0,1),["op_marrow"]=new BCombatRoleRanges(1),["op_rail"]=new BCombatRoleRanges(0),["op_anchor"]=new BCombatRoleRanges(1),["op_lumen"]=new BCombatRoleRanges(2),["op_cinder"]=new BCombatRoleRanges(2),["op_warden"]=new BCombatRoleRanges(1)};
  public static BCombatRoleRanges For(string operatorId){if(operatorId==null||!roles.TryGetValue(operatorId,out var range))throw new ArgumentException("Unapproved B operator role");return range;}
  static void Slots(int source,int target,int count){if(count<1||count>5||source<0||source>=count||target<0||target>=count)throw new ArgumentOutOfRangeException("slot");}
  public bool BasicAttackIncludesSlot(int source,int target,int count){Slots(source,target,count);return Math.Abs(source-target)<=BasicAttackSlotRadius;}
  public bool InterceptionIncludesSlot(int source,int target,int count){Slots(source,target,count);return InterceptionSlotRadius.HasValue&&Math.Abs(source-target)<=InterceptionSlotRadius.Value;}
 }
}
