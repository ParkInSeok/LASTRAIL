using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Sole local movement owner. Server still owns receipts, authority events and attack outcomes.
 public sealed class RailConsistMotion {
  readonly MainlineDetourPath path;readonly JObject initial;readonly long acceleration,deceleration;readonly string fingerprint;
  readonly JArray operations=new JArray(),events=new JArray();JObject sample;long target,forced;string route="ENTRY";
  public long ActualSpeedMmPerSecond=>(long)sample["actualMmPerSecond"];
  public long TargetSpeedMmPerSecond=>target;public long ForcedSlowdownBps=>forced;
  public long HeadFrontArcMm=>(long)sample["headDistanceMm"];public long ElapsedMs=>(long)sample["atMs"];
  public string SelectedPath=>route;public string PhysicalPath=>route=="RIGHT"?"RIGHT":"LEFT";public bool RouteConfirmed=>route!="ENTRY";
  public long ConsistLengthMm=>path.ConsistLengthMm;public long TailRearArcMm=>HeadFrontArcMm-ConsistLengthMm;
  public bool HeadPassedSplit=>HeadFrontArcMm>=path.SplitSmm;public bool TailPassedSplit=>TailRearArcMm>=path.SplitSmm;
  public bool TailMergeCleared=>TailRearArcMm>=path.MergeSmm(PhysicalPath);public bool NextBranchPermitted=>TailMergeCleared;
  public RailConsistMotion(MainlineDetourPath geometry,JObject first,long accelerationMmPerSecondSquared,long decelerationMmPerSecondSquared){
   path=geometry??throw new ArgumentNullException(nameof(geometry));if(first==null)throw new ArgumentNullException(nameof(first));
   WorldBossRailSnapshot.Fields(first,"atMs","sequence","headDistanceMm","distanceRemainder2000","actualMmPerSecond","targetMmPerSecond","forcedSlowdownBps");
   MainlineDetourPath.Natural(first,"atMs",9007199254740991);MainlineDetourPath.Natural(first,"distanceRemainder2000",1999);MainlineDetourPath.Natural(first,"headDistanceMm",1000000000);
   if(MainlineDetourPath.Natural(first,"sequence",1000000000)!=0||(long)first["headDistanceMm"]<path.ConsistLengthMm||(long)first["headDistanceMm"]>=path.SplitSmm)throw new ArgumentException("Initial consist position/sequence");
   MainlineDetourPath.Natural(first,"actualMmPerSecond",path.MaxSpeed);target=MainlineDetourPath.Natural(first,"targetMmPerSecond",path.MaxSpeed);forced=MainlineDetourPath.Natural(first,"forcedSlowdownBps",10000);
   if(accelerationMmPerSecondSquared<=0||decelerationMmPerSecondSquared<=0||accelerationMmPerSecondSquared>path.MaxAcceleration||decelerationMmPerSecondSquared>path.MaxAcceleration)throw new ArgumentException("Acceleration profile bound");
   acceleration=accelerationMmPerSecondSquared;deceleration=decelerationMmPerSecondSquared;initial=(JObject)first.DeepClone();sample=(JObject)first.DeepClone();
   fingerprint=BasicCombatSimulation.Digest(BasicCombatSimulation.StableJson(new JObject{["profile"]=path.OriginalProfile(),["initial"]=initial,["acceleration"]=acceleration,["deceleration"]=deceleration}));
  }
  void Room(){if(operations.Count>=10000)throw new InvalidOperationException("Motion journal limit");}
  public void SelectPath(string choice){MainlineDetourPath.RequireRoute(choice);if(route==choice)return;if(RouteConfirmed||HeadPassedSplit)throw new InvalidOperationException("Route locked");Room();operations.Add(new JObject{["kind"]="SELECT",["route"]=choice});route=choice;}
  public void SetTargetSpeed(long requested){if(requested<0||requested>path.MaxSpeed)throw new ArgumentOutOfRangeException(nameof(requested));if(target==requested)return;Room();operations.Add(new JObject{["kind"]="TARGET",["value"]=requested});target=requested;}
  public void SetForcedSlowdown(long bps){if(bps<0||bps>10000)throw new ArgumentOutOfRangeException(nameof(bps));if(forced==bps)return;Room();operations.Add(new JObject{["kind"]="SLOWDOWN",["value"]=bps});forced=bps;}
  public JObject OriginalProfile()=>path.OriginalProfile();
  public JObject ReadSample()=>(JObject)sample.DeepClone();
  public JArray ObservedCrossings()=>(JArray)events.DeepClone();
  // Samples are integer linear segments. Sub-mm/s acceleration is conservatively rounded down
  // per emitted interval, never exceeding the profile bound. Command cadence is part of replay.
  public IReadOnlyList<JObject> Advance(long milliseconds){
   if(milliseconds<0||milliseconds>1000000000||ElapsedMs>9007199254740991-milliseconds)throw new ArgumentOutOfRangeException(nameof(milliseconds));
   if(milliseconds==0)return Array.AsReadOnly(new JObject[0]);Room();
   var planned=new List<JObject>();JObject previous=sample;long remaining=milliseconds,effective=target*(10000-forced)/10000;
   while(remaining>0){if(planned.Count>=36000||(long)previous["sequence"]>=36000)throw new InvalidOperationException("Motion sample budget");
    long speed=(long)previous["actualMmPerSecond"],difference=effective-speed,rate=difference>=0?acceleration:deceleration;
    long step=Math.Min(remaining,path.MaxSampleMs);if(difference!=0)step=Math.Min(step,(Math.Abs(difference)*1000+rate-1)/rate);
    long delta=Math.Min(Math.Abs(difference),rate*step/1000),nextSpeed=speed+Math.Sign(difference)*delta;
    long numerator=(speed+nextSpeed)*step+(long)previous["distanceRemainder2000"],head=(long)previous["headDistanceMm"]+numerator/2000;
    if(head>path.LengthMm(PhysicalPath)||head>1000000000)throw new InvalidOperationException("Geometry path exhausted");
    var next=new JObject{["atMs"]=(long)previous["atMs"]+step,["sequence"]=(long)previous["sequence"]+1,["headDistanceMm"]=head,["distanceRemainder2000"]=numerator%2000,["actualMmPerSecond"]=nextSpeed,["targetMmPerSecond"]=target,["forcedSlowdownBps"]=forced};
    planned.Add(next);previous=next;remaining-=step;
   }
   // Stage all samples before publishing: path exhaustion cannot partially advance the owner.
   operations.Add(new JObject{["kind"]="ADVANCE",["milliseconds"]=milliseconds});
   foreach(var next in planned){Cross("HEAD_SPLIT",HeadFrontArcMm,(long)next["headDistanceMm"],path.SplitSmm,next);Cross("TAIL_SPLIT",TailRearArcMm,(long)next["headDistanceMm"]-ConsistLengthMm,path.SplitSmm,next);Cross("TAIL_MERGE",TailRearArcMm,(long)next["headDistanceMm"]-ConsistLengthMm,path.MergeSmm(PhysicalPath),next);sample=next;}
   return Array.AsReadOnly(planned.Select(p=>(JObject)p.DeepClone()).ToArray());
  }
  void Cross(string kind,long before,long after,long threshold,JObject next){if(before<threshold&&after>=threshold&&!events.Any(e=>(string)e["kind"]==kind))events.Add(new JObject{["eventId"]=new JArray(path.GeometryId,path.RoundId,kind).ToString(Newtonsoft.Json.Formatting.None),["kind"]=kind,["previousSampleAtMs"]=ElapsedMs,["observedAtMs"]=(long)next["atMs"]});}
  public IReadOnlyList<RailVehicleOccupancy> Occupancy(){long offset=0;var result=path.Vehicles.Select(v=>{var body=new RailVehicleOccupancy(v,HeadFrontArcMm-offset,path,PhysicalPath);offset+=v.LengthMm+v.GapAfterMm;return body;}).ToArray();return Array.AsReadOnly(result);}
  public JObject CaptureLocal()=>new JObject{["schema"]="rail-consist-motion.local.v1",["configurationSha256"]=fingerprint,["sample"]=ReadSample(),["target"]=target,["forcedSlowdownBps"]=forced,["route"]=route,["events"]=ObservedCrossings(),["operations"]=operations.DeepClone()};
  public static RailConsistMotion RestoreLocal(JObject state,Func<RailConsistMotion> originalFactory){
   if(state==null||state.ToString(Newtonsoft.Json.Formatting.None).Length>2097152)throw new ArgumentException("Motion snapshot limit");var candidate=originalFactory?.Invoke()??throw new ArgumentException("Original factory required");if(candidate.operations.Count!=0)throw new ArgumentException("Fresh original factory required");
   WorldBossRailSnapshot.Fields(state,"schema","configurationSha256","sample","target","forcedSlowdownBps","route","events","operations");if((string)state["schema"]!="rail-consist-motion.local.v1"||(string)state["configurationSha256"]!=candidate.fingerprint||state["operations"] is not JArray ops||ops.Count>10000)throw new ArgumentException("Original motion definition mismatch");
   foreach(var t in ops){var op=t as JObject;string kind=WorldBossRailSnapshot.Text(op,"kind");if(kind=="ADVANCE"){WorldBossRailSnapshot.Fields(op,"kind","milliseconds");candidate.Advance(WorldBossRailSnapshot.Nat(op,"milliseconds"));}else if(kind=="SELECT"){WorldBossRailSnapshot.Fields(op,"kind","route");candidate.SelectPath(WorldBossRailSnapshot.Text(op,"route"));}else if(kind=="TARGET"||kind=="SLOWDOWN"){WorldBossRailSnapshot.Fields(op,"kind","value");long v=WorldBossRailSnapshot.Nat(op,"value");if(kind=="TARGET")candidate.SetTargetSpeed(v);else candidate.SetForcedSlowdown(v);}else throw new ArgumentException("Unknown motion operation");}
   if(BasicCombatSimulation.StableJson(candidate.CaptureLocal())!=BasicCombatSimulation.StableJson(state))throw new ArgumentException("Motion replay mismatch");return candidate;
  }
 }
}


