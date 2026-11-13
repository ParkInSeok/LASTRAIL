using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Immutable initial source; restored motion owns progress and this source never grants readiness.
 public sealed class OrdinaryRailSource {
  public const string BoundVersion="combat.ordinary-rail-source.v3.20261027";
  public const string MotionVersion="combat.ordinary-rail-source.v4.20261028";
  public const string LocalFiveCarVersion="combat.local-five-car-rail.v1.20261109";
  public const string LocalReviewTrackVersion="combat.local-five-car-rail.v2.20261109";
  public bool LocalFiveCar {get;}
  public bool MotionBound=>SourceVersion==MotionVersion||LocalFiveCar;
  public int TotalLengthMm=>LocalFiveCar?77000:38000;
  public int TrackLengthMm{get;}=1200000;
  public const string ConsistHash="c2e5e813adfc3fb74755124c7aed8d17de0b79002a6ae82ec7509cb7cb83f497";
  public const string GeometryHash="e63bbc228c1f8c505e6dee6c3ef6dd5c054e1ef2034603bc8e23221f65da47cf";
  public string SourceVersion{get;} public string SourceHash{get;}
  public bool GeometryBound=>SourceVersion==BoundVersion||MotionBound;
  public int? InitialSpeedMmPerSecond{get;}
  public int? InitialAccelerationMmPerSecondSquared{get;}
  public long? InitialFrontArcMm{get;}
  public IReadOnlyList<string> PhysicalInstanceIds{get;}
  internal OrdinaryRailSource(JToken raw,OrdinaryFiveStartInput original,JObject admission,string ownedHash){
   var source=raw as JObject;BDirectAttackInput.Fields(source,"packet","sourceHash");var packet=source["packet"] as JObject;
   SourceVersion=(string)packet?["version"];SourceHash=(string)source["sourceHash"];
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
   if(SourceVersion==LocalFiveCarVersion||SourceVersion==LocalReviewTrackVersion){
    bool extended=SourceVersion==LocalReviewTrackVersion;
    if(extended){BDirectAttackInput.Fields(packet,"version","scope","accountId","runId","baseSource","physicalConsist","reviewTrackLengthMm");Need(packet["reviewTrackLengthMm"]?.Type==JTokenType.Integer&&(long)packet["reviewTrackLengthMm"]==6000000);TrackLengthMm=6000000;}
    else BDirectAttackInput.Fields(packet,"version","scope","accountId","runId","baseSource","physicalConsist");
    Need((string)packet["scope"]=="USER_APPROVED_LOCAL_REVIEW_NOT_ACCOUNT_OWNERSHIP"&&(string)packet["accountId"]==original.AccountId&&(string)packet["runId"]==original.RunId&&SourceHash==Hash(packet));
    Need((string)packet["baseSource"]?["packet"]?["version"]==MotionVersion);
    var basis=new OrdinaryRailSource(packet["baseSource"],original,admission,ownedHash);
    var localIds=new[]{"LOCAL_QA_ENGINE","LOCAL_QA_CAR_0","LOCAL_QA_CAR_1","LOCAL_QA_CAR_2","LOCAL_QA_CAR_3","LOCAL_QA_CAR_4"};
    Need(JToken.DeepEquals(packet["physicalConsist"],new JObject{["instanceIds"]=new JArray(localIds),["totalLengthMm"]=77000,["initialTailArcMm"]=20000,["initialFrontArcMm"]=97000}));
    LocalFiveCar=true;PhysicalInstanceIds=Array.AsReadOnly(localIds);InitialFrontArcMm=97000;InitialSpeedMmPerSecond=basis.InitialSpeedMmPerSecond;InitialAccelerationMmPerSecondSquared=basis.InitialAccelerationMmPerSecondSquared;return;
   }
#endif
   Need(original.Train!=null&&(GeometryBound||SourceVersion=="combat.ordinary-rail-source.v1.20261027"||SourceVersion=="combat.ordinary-rail-source.v2.20261027"));
   Need(SourceHash==Hash(packet));
   var loadout=admission["loadout"] as JObject;BDirectAttackInput.Fields(loadout,"rulesVersion","trainId","carriages");
   Need((string)loadout["rulesVersion"]=="train-loadout-v1"&&(string)loadout["trainId"]==original.Train.TrainId);
   var selection=loadout["carriages"] as JArray;Need(selection!=null&&selection.Count>=2&&selection.Count<=4);var selected=new string[4];
   foreach(var row in selection){BDirectAttackInput.Fields(row as JObject,"carriageId","slot");int slot=(int)BDirectAttackInput.Nat(row,"slot",0,selection.Count-1);var id=BDirectAttackInput.Text(row,"carriageId");Need(id.Length<=100&&selected[slot]==null&&!selected.Contains(id));selected[slot]=id;}
   var owned=admission["ownedState"] as JObject;var train=owned?["train"] as JObject;
   Need(train!=null&&(string)train["trainId"]==original.Train.TrainId&&(long?)train["level"]==original.Train.Level&&(long?)train["revision"]==original.Train.Revision);
   var ownedCars=owned["carriages"] as JArray;Need(ownedCars!=null&&ownedCars.Count==selected.Count(x=>x!=null));
   var cars=new JArray();for(int slot=0;slot<selected.Length;slot++){if(selected[slot]==null)continue;var matches=ownedCars.Where(c=>(string)c["carriageId"]==selected[slot]).ToArray();Need(matches.Length==1);
    cars.Add(new JObject{["carriageId"]=selected[slot],["slot"]=slot,["level"]=BDirectAttackInput.Nat(matches[0],"level",1),["revision"]=BDirectAttackInput.Nat(matches[0],"revision",1)});}
   var policy=JObject.Parse("{\"version\":\"combat.ordinary-rail-trial.v1.20261027\",\"approval\":\"ORDINARY-RAIL-FIRST-PROFILE-v1.20261027\",\"trialBodyLengthMm\":12000,\"trialInterCarGapMm\":1000,\"speedMmPerSecond\":{\"minimum\":14000,\"cruise\":20000,\"maximum\":26000},\"accelerationMmPerSecondSquared\":5000,\"decelerationMmPerSecondSquared\":5000,\"speedEffectPolicy\":\"PRESERVE_ORIGINAL_EFFECTS\"}");
   if(SourceVersion!="combat.ordinary-rail-source.v1.20261027"){policy["version"]="combat.ordinary-rail-trial.v2.20261027";policy["finiteRail"]=JObject.Parse("{\"version\":\"ORDINARY-FINITE-RAIL-CAPACITY-v1.20261027\",\"minimumLengthMm\":500000,\"initialTailArcMm\":20000,\"previewLookaheadMs\":10000,\"endMarginMm\":20000,\"maximumTravelIncludesSpeedEffects\":true,\"prePreviewTravelBudgetRequired\":true}");}
   var expected=new JObject{["version"]=SourceVersion,["accountId"]=original.AccountId,["runId"]=original.RunId,["trainId"]=original.Train.TrainId,["ordinaryOriginHash"]=original.OriginHash,["ownedSourceHash"]=ownedHash,["loadoutRevision"]=original.LoadoutRevision,["selectedCarriages"]=cars,["definitionHash"]=Hash(policy),["status"]="TRIAL_PROFILE_ISSUED_GEOMETRY_UNBOUND",["physicalConsist"]=null,["geometry"]=null,["initialMotion"]=null};
   var ids=Array.Empty<string>();
   if(GeometryBound){
    Need(original.Train.TrainId=="train_a"&&selected[0]=="car_combat"&&selected[1]=="car_gun"&&selected.Skip(2).All(c=>c==null));
    ids=new[]{original.Train.TrainId,selected[0],selected[1]}.Select((id,order)=>Hash(new JObject{["version"]="ordinary.physical-instance.v1.20261027",["accountId"]=original.AccountId,["runId"]=original.RunId,["ordinaryOriginHash"]=original.OriginHash,["order"]=order,["selectionId"]=id,["definitionHash"]=ConsistHash}).Substring(0,32)).ToArray();
    expected["definitionVersion"]=policy["version"].DeepClone();expected["status"]="GEOMETRY_BOUND_EXECUTION_UNREADY";
    expected["physicalConsist"]=new JObject{["version"]="ordinary.trial-consist-authoring.v1.20261027",["definitionHash"]=ConsistHash,["instanceIds"]=new JArray(ids),["totalLengthMm"]=38000,["initialTailArcMm"]=20000,["initialFrontArcMm"]=58000};
    expected["geometry"]=new JObject{["version"]="ordinary.finite-rail-authoring.v1.20261027",["definitionHash"]=GeometryHash,["railSegmentId"]="ordinary-first-straight-v1"};
   }else expected["definition"]=policy;
   if(SourceVersion==MotionVersion){
    Need(original.Train.SourceVersion==OrdinaryTrainSource.InitialHealthVersion);
    policy["version"]="combat.ordinary-rail-trial.v3.20261028";
    policy["firstPlayerTrial"]=JObject.Parse("{\"approval\":\"ORDINARY-FIRST-PLAYER-TRAVEL-BUDGET-v1.20261028\",\"gunnerObservationMs\":30000,\"preWarningDelayMs\":2000,\"warningMs\":8000,\"postImpactObservationMs\":2000,\"observationMs\":42000,\"authoredLengthMm\":1200000,\"productTimeLimit\":false,\"initialSpeedMmPerSecond\":20000,\"initialAccelerationMmPerSecondSquared\":0,\"departureEffectsTakePrecedence\":true,\"progressCheckpointTakesPrecedence\":true,\"capacityRequiresOriginalSpeedEffects\":true}");
    expected.Remove("ownedSourceHash");expected["definitionVersion"]=policy["version"].DeepClone();expected["definitionHash"]=Hash(policy);
    expected["geometry"]=new JObject{["version"]="ordinary.finite-rail-authoring.v2.20261028",["definitionHash"]="eeeb0da41505f285ffe497ba789c5610f8c2fab38ec4d1950549cac91b2b00bc",["railSegmentId"]="ordinary-first-straight-v2"};
    expected["initialMotion"]=new JObject{["speedMmPerSecond"]=20000,["accelerationMmPerSecondSquared"]=0};expected["status"]="MOTION_BOUND_CAPACITY_UNVERIFIED";
    InitialSpeedMmPerSecond=20000;InitialAccelerationMmPerSecondSquared=0;
   }
   Need(JToken.DeepEquals(expected,packet));PhysicalInstanceIds=Array.AsReadOnly(ids);
   if(GeometryBound)InitialFrontArcMm=(long)packet["physicalConsist"]["initialFrontArcMm"];
  }
  static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_rail_source_invalid");}
 }
}
