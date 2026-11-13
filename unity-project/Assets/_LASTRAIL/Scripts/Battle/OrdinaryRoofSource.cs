using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Original layout identity only; measured foot offsets and execution readiness belong to the scene.
 public sealed class OrdinaryRoofSource {
  public const string Version="combat.ordinary-roof-source.v1.20261027";
  public string SourceHash{get;}
  public string SourceVersion{get;}
  public sealed class Slot {
   public string OperatorId{get;} public string SlotId{get;} public string PhysicalInstanceId{get;}
   public int LocalXmm{get;} public int PhysicalOrder{get;}
   internal Slot(string actor,string slot,string physical,int order,int x){OperatorId=actor;SlotId=slot;PhysicalInstanceId=physical;PhysicalOrder=order;LocalXmm=x;}
  }
  public IReadOnlyList<Slot> Slots{get;}
  internal OrdinaryRoofSource(JToken raw,OrdinaryFiveStartInput original){
   var source=raw as JObject;BDirectAttackInput.Fields(source,"packet","sourceHash");
   SourceVersion=(string)source["packet"]?["version"];
   if(original.Rail?.GeometryBound!=true)throw new ArgumentException("ordinary_roof_requires_bound_rail");
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
   if(original.Rail.LocalFiveCar){
    bool party=SourceVersion=="combat.local-five-car-roof.v2.20261109";
    var localExpected=new JObject{["version"]=party?"combat.local-five-car-roof.v2.20261109":"combat.local-five-car-roof.v1.20261109",["scope"]="USER_APPROVED_LOCAL_REVIEW_NOT_ACCOUNT_OWNERSHIP",["railSourceHash"]=original.Rail.SourceHash,["operatorIds"]=new JArray(original.Actors.Select(a=>a.OperatorId))};
    if(party)localExpected["initialFeet"]=new JArray(original.Actors.Select((a,i)=>new JObject{["operatorId"]=a.OperatorId,["physicalOrder"]=1,["localMm"]=new JArray(4000-i*2000,3213,i%2==0?-600:600)}));
    SourceHash=BDirectAttackInput.Digest(localExpected).Substring(7);
    if(!JToken.DeepEquals(source["packet"],localExpected)||(string)source["sourceHash"]!=SourceHash)throw new ArgumentException("ordinary_local_five_car_roof_conflict");
    var localXs=party?new[]{4000,2000,0,-2000,-4000}:new[]{3000,0,-3000,3000,-3000};Slots=Array.AsReadOnly(original.Actors.Select((a,i)=>new Slot(a.OperatorId,"LOCAL_QA_ROOF_"+i,original.Rail.PhysicalInstanceIds[party?1:i+1],party?1:i+1,localXs[i])).ToArray());return;
   }
#endif
   var expected=new JObject{["version"]=Version,["definitionVersion"]="combat.ordinary-roof-layout.v1.20261027",["definitionHash"]="1fd14d7a0390049b22e1689e59d4b839e1bead095e3fb624efe8acc3dde6bab4",["railSourceHash"]=original.Rail.SourceHash,["operatorIds"]=new JArray(original.Actors.Select(a=>a.OperatorId)),["status"]="LAYOUT_BOUND_ANCHORS_UNVERIFIED"};
   SourceHash=BDirectAttackInput.Digest(expected).Substring(7);
   if(!JToken.DeepEquals(source["packet"],expected)||(string)source["sourceHash"]!=SourceHash)throw new ArgumentException("ordinary_roof_source_conflict");
   var names=new[]{"combat-front","combat-middle","combat-rear","gun-front","gun-rear"};var xs=new[]{3000,0,-3000,3000,-3000};
   Slots=Array.AsReadOnly(original.Actors.Select((a,i)=>new Slot(a.OperatorId,names[i],original.Rail.PhysicalInstanceIds[i<3?1:2],i<3?1:2,xs[i])).ToArray());
  }
 }
}
