using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed partial class BasicCombatSimulation {
  internal void RequestLocalPartyEnd(bool threatOnLeft){
   if(ordinaryFactory?.Original.Rail.LocalFiveCar!=true||ordinaryBoarding==null)throw new InvalidOperationException("Explicit six-car local execution required.");
   if(clock.IsPaused||Outcome!=BasicCombatOutcome.Running)throw new InvalidOperationException("Running local owner required.");
   int destination=threatOnLeft?5:1;
   var plans=ordinaryFactory.Original.Actors.OrderBy(a=>a.Slot).Select((actor,i)=>{
    var current=ordinaryBoarding.PartyAnchor(actor.OperatorId);int source=current.Slot;
    long x=(threatOnLeft?-1:1)*(4000-i*2000),z=i%2==0?-600:600;
    return new OrdinaryPartyTransfer.Plan(actor.OperatorId,new OrdinaryPartyAnchor(source,current.X,current.Y,0),new OrdinaryPartyAnchor(destination,x,3213,0),new OrdinaryPartyAnchor(destination,x,3213,z),i%2==0);
   }).ToArray();
   ordinaryBoarding.RequestPartyTransfer(threatOnLeft,plans);
   RecordOrdinary(new JObject{["kind"]="LOCAL_PARTY_END",["threatOnLeft"]=threatOnLeft});
  }
 }
}
