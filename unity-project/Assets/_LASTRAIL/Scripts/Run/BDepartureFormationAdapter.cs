using System;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Prepares source identities for existing consumers without inventing live inputs.
 public sealed class BDepartureFormationAdapter {
  readonly BDepartureAssignment assignment;
  public BCombatPhysicalPlacement Placement {get;}
  public bool CanStart=>false;
  public BDepartureFormationAdapter(BDepartureAssignment original) {
   assignment=original??throw new ArgumentNullException(nameof(original));
   Placement=new BCombatPhysicalPlacement(original.Capture());
   BCarriageBuffModel.ValidatePreparedDeparture(Placement);
  }
  public JObject Capture()=>Placement.Capture();
  public JObject PrepareBuffDeparture()=>BCarriageBuffModel.ValidatePreparedDeparture(Placement);
  public void RequireSource(BDepartureAssignment original) {
   if(original==null||assignment.PacketHash!=original.PacketHash||!JToken.DeepEquals(assignment.Capture(),original.Capture()))throw new ArgumentException("b_placement_assignment_source_conflict");
  }
  public void RequireSameFormation(BCombatFormation formation) {
   if(formation==null)throw new ArgumentNullException(nameof(formation));formation.RequirePhysicalPlacement(Placement);
  }
  public void RequireRuntimeBinding()=>assignment.RequireRuntimeBinding();
  public static BDepartureFormationAdapter Restore(JObject saved,string hash,BDepartureAssignment original) {
   if(saved==null||BDirectAttackInput.Digest(saved)!=hash)throw new ArgumentException("b_placement_snapshot_hash_conflict");
   var restored=new BDepartureFormationAdapter(original);
   if(!JToken.DeepEquals(saved,restored.Capture()))throw new ArgumentException("b_placement_snapshot_source_conflict");return restored;
  }
 }
}
