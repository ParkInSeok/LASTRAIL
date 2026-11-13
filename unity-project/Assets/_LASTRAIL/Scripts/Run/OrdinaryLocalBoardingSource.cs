using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;

namespace LastRail.Run {
 // A pinned local source is preparation data, never permission to inject entities into a running node.
 public sealed class OrdinaryLocalBoardingSource {
  public const string Version="ordinary.local-boarding-source.v1.20261107";
  public string SourceHash {get;}
  public BBoardingEventsNodeSnapshot Boarding {get;}
  public IReadOnlyList<string> PhysicalHostIds {get;}
  public bool CanStart=>false;
  public bool RuntimeSupported=>false;
  OrdinaryLocalBoardingSource(string hash,BBoardingEventsNodeSnapshot boarding,string[] hosts){SourceHash=hash;Boarding=boarding;PhysicalHostIds=Array.AsReadOnly(hosts);}
  static void Need(bool value){if(!value)throw new ArgumentException("ordinary_local_boarding_source_conflict");}
  public static OrdinaryLocalBoardingSource Read(JObject envelope,string independentlyExpectedSourceHash,OrdinaryFiveStartInput original,OrdinaryNodeStart node){
   Need(envelope!=null&&original?.Rail!=null&&original.Roof!=null&&node?.ExecutionOrigin!=null);
   Need(envelope.ToString(Newtonsoft.Json.Formatting.None).Length<=524288);
   BDirectAttackInput.Fields(envelope,"packet","sourceHash");
   var p=envelope["packet"] as JObject;
   BDirectAttackInput.Fields(p,"version","scope","accountId","runId","nodeId","startSnapshotHash","executionOrigin","railSourceHash","roofSourceHash","physicalHostIds","boarding","placementOwner","registration","geometryRequired","reentry","canStart","runtimeSupported");
   string hash=BDirectAttackInput.Digest(p).Substring(7);
   Need(hash==independentlyExpectedSourceHash&&(string)envelope["sourceHash"]==hash);
   node.RequireOriginal(original);
   Need((string)p["version"]==Version&&(string)p["scope"]=="LOCAL_QA_APPROVED_BOARDING_FULL_WAVES_NOT_EXISTING_GUNNER_SUBSTITUTION");
   Need((string)p["accountId"]==original.AccountId&&(string)p["runId"]==original.RunId&&(string)p["nodeId"]==node.NodeId&&(string)p["startSnapshotHash"]==original.StartSnapshotHash);
   Need((string)p["railSourceHash"]==original.Rail.SourceHash&&(string)p["roofSourceHash"]==original.Roof.SourceHash);
   Need(JToken.DeepEquals(p["physicalHostIds"],new JArray(original.Rail.PhysicalInstanceIds)));
   var runtime=node.Capture()["snapshot"]["runtime"];
   Need(JToken.DeepEquals(p["executionOrigin"],new JObject{["executionVersion"]=runtime["executionVersion"].DeepClone(),["determinism"]=runtime["determinism"].DeepClone()}));
   Need((string)p["placementOwner"]=="EXISTING_NATIVE_BOARDING_LEAST_ASSIGNED_LIVING_OPERATOR_CAR_THEN_FIXED_ORDER"&&(string)p["registration"]=="PLANNED_UNTIL_ACTUAL_ENTRANCE_NO_PREASSIGNED_SLOT_OR_ROOFTOP");
   Need(JToken.DeepEquals(p["geometryRequired"],new JArray("HOST_PHYSICAL_ID","CARRIAGE_FRAME","APPROACH_PATH","LEFT_RIGHT_CLIMB_SOCKET","ROOF_PATH_AND_DESTINATION","FOOT_EMISSION_HIT_AND_HIT_VOLUME","GENERATION_AND_PHASE","SUPPORTED_TARGET_KIND_AND_AREA")));
   Need((string)p["reentry"]=="ORIGINAL_NODE_START_NEW_ATTEMPT_NO_MID_COMBAT_CONTINUATION"&&p["canStart"]?.Type==JTokenType.Boolean&&!(bool)p["canStart"]&&p["runtimeSupported"]?.Type==JTokenType.Boolean&&!(bool)p["runtimeSupported"]);
   var wrapper=p["boarding"] as JObject;var raw=wrapper?["originalSource"] as JObject;
   Need(raw!=null&&(string)raw["origin"]?["accountId"]==original.AccountId&&(string)raw["origin"]?["runId"]==original.RunId&&(string)raw["origin"]?["baseSnapshotHash"]==original.StartSnapshotHash&&(string)raw["nodeId"]==node.NodeId&&(long?)raw["visitDepth"]==1&&raw["priorSourceHash"]?.Type==JTokenType.Null);
   // The independently pinned outer packet authenticates this receipt; the existing readers validate its full waves and definitions.
   var receipt=new JObject();foreach(string key in new[]{"origin","nativeContextId","seedHash","sourceHash","nodeId","visitDepth","priorSourceHash"})receipt[key]=raw[key]?.DeepClone();
   var boarding=BBoardingEventsNodeSnapshot.Read(wrapper,(string)wrapper["sourceHash"],receipt);
   Need(!boarding.OriginalSource.Waves.SelectMany(w=>w.Instances).Any(i=>node.Enemies.Gunners.Any(g=>g.InstanceId==i.InstanceId)));
   return new OrdinaryLocalBoardingSource(hash,boarding,original.Rail.PhysicalInstanceIds.ToArray());
  }
 }
}
