#if UNITY_EDITOR
using NUnit.Framework;
using LastRail.Run;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
namespace LastRail.Tests.EditMode {
 public sealed class BFixedPlacementTests {
  [Test] public void OriginalAssignmentHasPlacementAdapter() {
   Assert.NotNull(typeof(BDepartureAssignment).Assembly.GetType("LastRail.Run.BDepartureFormationAdapter"));
  }
  static BDepartureAssignment Source()=>BDepartureAssignmentTests.Read(BDepartureAssignmentTests.Packet());
  [Test] public void OriginalFixedHolesCargoInstanceAndActorRevisionArePreserved(){
   var source=Source();var adapter=new BDepartureFormationAdapter(source);var p=adapter.Placement;
   Assert.AreEqual(5,p.Slots.Count);Assert.AreEqual(2,p.OccupiedCount);
   Assert.AreEqual("physical-0",p.Slots[0].InstanceId);Assert.AreEqual("op_kyle",p.Slots[0].OperatorId);
   Assert.AreEqual(1,p.Slots[0].ActorPhysicalRevision);Assert.IsNull(p.Slots[4].ActorPhysicalRevision);
   Assert.AreEqual("physical-4",p.Slots[4].InstanceId);Assert.IsTrue(p.Slots[4].IsCargo);
   for(int i=1;i<4;i++)Assert.IsNull(p.AtPhysicalSlot(i));
   Assert.AreEqual(source.PacketHash,p.AssignmentPacketHash);Assert.IsFalse(p.CanExecute);Assert.IsFalse(adapter.CanStart);
   var saved=adapter.Capture();Assert.AreEqual(BCombatPhysicalPlacement.Version,(string)saved["version"]);
   Assert.IsTrue(JToken.DeepEquals(source.Capture()["refitSnapshot"]["refit"],adapter.PrepareBuffDeparture()));
   saved["physicalSlots"][4]["car"]["slot"]=1;adapter.PrepareBuffDeparture()["sourceStatus"]="ISOLATED_LOCAL_FIXTURE";
   Assert.AreEqual(4,p.AtPhysicalSlot(4).PhysicalSlot);Assert.AreEqual("PERSISTED_ACCOUNT_SOURCE",(string)adapter.PrepareBuffDeparture()["sourceStatus"]);
  }
  [TestCase(1,"physical-4"),TestCase(4,"physical-0"),TestCase(0,"unknown")]
  public void PhysicalTargetsRejectHolesOrForeignInstances(int slot,string id){Assert.Throws<ArgumentException>(()=>new BDepartureFormationAdapter(Source()).Placement.RequireCar(slot,id));}
  [Test] public void ActorAndSlotEnvelopeUseOriginalPhysicalPositionsOnly(){
   var p=new BDepartureFormationAdapter(Source()).Placement;
   Assert.AreEqual(0,p.RequireActor(0,"physical-0","op_kyle").PhysicalSlot);
   Assert.Throws<ArgumentException>(()=>p.RequireActor(4,"physical-4","op_kyle"));
   Assert.Throws<ArgumentException>(()=>p.RequireActor(0,"physical-0","op_sable"));
   Assert.Throws<ArgumentException>(()=>p.RequireActor(4,"physical-4",null));
   Assert.IsTrue(p.BasicSlotIncludesTarget("op_kyle",0,"physical-0"));Assert.IsFalse(p.BasicSlotIncludesTarget("op_kyle",4,"physical-4"));
   Assert.IsFalse(p.InterceptionSlotIncludesTarget("op_kyle",4,"physical-4"));
   Assert.Throws<ArgumentException>(()=>p.BasicSlotIncludesTarget("op_kyle",1,"physical-4"));
   Assert.Throws<ArgumentException>(()=>p.BasicSlotIncludesTarget("op_sable",4,"physical-4"));
  }
  [Test] public void DenseLegacyFormationCannotCompactSlotsZeroAndFour(){
   var adapter=new BDepartureFormationAdapter(Source());
   var dense=new BCombatFormation("explicit-local-train",BCombatMode.Solo,BCombatBattlefield.ExternalVertical,new[]{new BCombatSlot("physical-0","op_kyle"),new BCombatSlot("physical-4")});
   Assert.Throws<ArgumentException>(()=>adapter.RequireSameFormation(dense));
   Assert.Throws<ArgumentException>(()=>new BCombatFormation("explicit-local-train",BCombatMode.Solo,BCombatBattlefield.ExternalVertical,new[]{new BCombatSlot("physical-0","op_kyle"),null,null,null,new BCombatSlot("physical-4")}));
   Assert.Throws<InvalidOperationException>(()=>adapter.RequireRuntimeBinding());
   Assert.Throws<ArgumentException>(()=>BCarriageBuffModel.ValidateDeparture(adapter.PrepareBuffDeparture()));
  }
  [Test] public void RestoreRequiresSameOriginalAssignmentAndIndependentHash(){
   var source=Source();var adapter=new BDepartureFormationAdapter(source);var saved=adapter.Capture();
   var restored=BDepartureFormationAdapter.Restore(saved,BDirectAttackInput.Digest(saved),source);
   Assert.IsTrue(JToken.DeepEquals(saved,restored.Capture()));
   Assert.Throws<ArgumentException>(()=>BDepartureFormationAdapter.Restore(saved,"sha256:"+new string('0',64),source));
   var packet=BDepartureAssignmentTests.Packet();packet["catalog"]["revision"]=2;BDepartureAssignmentTests.Rehash(packet);var replacement=BDepartureAssignmentTests.Read(packet);
   Assert.Throws<ArgumentException>(()=>adapter.RequireSource(replacement));
   Assert.Throws<ArgumentException>(()=>BDepartureFormationAdapter.Restore(saved,BDirectAttackInput.Digest(saved),replacement));
  }
  [TestCase("slot"),TestCase("instance"),TestCase("revision"),TestCase("source"),TestCase("authority"),TestCase("origin")]
  public void RehashedPlacementCannotReplaceOriginalSourceFacts(string kind){
   var source=Source();var saved=new BDepartureFormationAdapter(source).Capture();
   switch(kind){case "slot":saved["physicalSlots"][4]["physicalSlot"]=1;break;case "instance":saved["physicalSlots"][0]["car"]["instanceId"]="foreign";break;case "revision":saved["physicalSlots"][0]["actorPhysicalRevision"]=2;break;case "source":saved["departure"]["sourceStatus"]="ISOLATED_LOCAL_FIXTURE";break;case "authority":saved["canExecute"]=true;break;case "origin":saved["source"]["departurePinHash"]=new string('0',64);break;}
   Assert.Throws<ArgumentException>(()=>BDepartureFormationAdapter.Restore(saved,BDirectAttackInput.Digest(saved),source));
  }
  [Test] public async Task ExistingControllerPreparesItsOneLoadedSourceWithoutMutatingRuntimeCapture(){
   using(var c=new BRouteSessionController("a","LOCAL_NEW_B_PIN_19")){
    Assert.Throws<InvalidOperationException>(()=>c.PrepareDepartureFormation());var before=c.Capture();
    Assert.IsTrue(await c.ReadDepartureAssignmentAsync(_=>Task.FromResult(Source())));
    var adapter=c.PrepareDepartureFormation();adapter.RequireSource(c.DepartureAssignment);
    Assert.AreEqual(4,adapter.Placement.RequireCar(4,"physical-4").PhysicalSlot);
    Assert.IsTrue(JToken.DeepEquals(before,c.Capture()));c.Dispose();Assert.Throws<ObjectDisposedException>(()=>c.PrepareDepartureFormation());
   }
  }
 }
}
#endif
