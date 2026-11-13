using System;
using System.Collections.Generic;
namespace LastRail.Battle {
 internal sealed partial class OrdinaryBoardingExecution {
  OrdinaryPartyTransfer partyTransfer;Func<string,bool> partyReadiness;
  NativePartyMotion PartyMotion()=>new NativePartyMotion(boarding.Spatial,geometry,this,actor=>partyReadiness!=null&&partyReadiness(actor));
  internal void BindPartyReadiness(Func<string,bool> canMove){Active();partyReadiness=canMove??throw new ArgumentNullException(nameof(canMove));}
  internal void RequestPartyTransfer(bool threatOnLeft,IEnumerable<OrdinaryPartyTransfer.Plan> plans){
   Active();if(!started||!factory.Original.Rail.LocalFiveCar)throw new InvalidOperationException("Explicit running local five-car review required");
   if(partyTransfer!=null&&partyTransfer.State!=OrdinaryPartyTransfer.Phase.Complete&&partyTransfer.State!=OrdinaryPartyTransfer.Phase.Cancelled)throw new InvalidOperationException("Party transfer already owns the passage");
   partyTransfer=new OrdinaryPartyTransfer(PartyMotion(),roster.CarriageIds,threatOnLeft,plans);
  }
  void StepPartyTransfer(){partyTransfer?.Step();}
  void CloneParty(OrdinaryBoardingExecution other){partyTransfer=other.partyTransfer?.Clone(PartyMotion());}
  void RetireParty(){partyTransfer?.Dispose();partyReadiness=null;}
  internal bool PartyMoving(string actor)=>partyTransfer?.IsMoving(actor)==true;
  internal string PartyPhase=>partyTransfer?.State.ToString();
  internal OrdinaryPartyAnchor PartyAnchor(string actor)=>PartyMotion().Read(actor).Anchor;
  internal int PartyCarOrder(string actor)=>boarding.Spatial.Slot(actor);
  internal long[] PartyOffset(string actor){
   var original=geometry.EntityAnchor(actor,roster.Entities[actor].Slot,"actorMm");var current=boarding.Spatial.Position(actor);
   return new[]{checked(current.X-original.X),checked(current.Y-original.Y),checked(current.Z-original.Z)};
  }
 }
}
