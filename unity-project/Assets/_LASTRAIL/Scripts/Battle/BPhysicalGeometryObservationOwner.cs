using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Consumes immutable test-space pose observations at a borrowed ORIGINAL tick.
 // It never advances time, HP, entities, movement or native combat actions.
 public sealed class BPhysicalGeometryObservationOwner {
  public const string Version="unity.combat-b.physical-space-observations-test.v1.20261006";
  readonly BOrdinaryGeometryBinding geometry;readonly Func<long> readOriginalTick;
  JObject current;bool publishing;Dictionary<string,JObject> poses=new Dictionary<string,JObject>(StringComparer.Ordinal);
  public BPhysicalGeometryObservationOwner(BOrdinaryGeometryBinding source,Func<long> originalTick){geometry=source??throw new ArgumentNullException(nameof(source));if(source.PhysicalPlacement==null)throw new ArgumentException("b_geometry_original_placement_missing");readOriginalTick=originalTick??throw new ArgumentNullException(nameof(originalTick));}
  static void Need(bool yes)=>BuffWire.Need(yes,"b_geometry_pose_source_conflict");
  public void Publish(JObject source,string independentlyTrustedHash){
   if(publishing)throw new InvalidOperationException("b_geometry_pose_reentrant_publish");publishing=true;try{PublishOriginal(source,independentlyTrustedHash);}finally{publishing=false;}
  }
  void PublishOriginal(JObject source,string independentlyTrustedHash){
   Need(source!=null&&BasicCombatSimulation.StableJson(source).Length<=65536&&BDirectAttackInput.Digest(source)==independentlyTrustedHash);
   var p=(JObject)source.DeepClone();BuffWire.Fields(p,"version","sourceStatus","geometryHash","bundleHash","contextId","observedTick","sourceRevision","poses");
   Need((string)p["version"]==Version&&(string)p["sourceStatus"]=="EXPLICIT_TEST_SPACE_OBSERVATIONS_NOT_NATIVE_EXECUTOR"&&(string)p["geometryHash"]==geometry.Hash&&(string)p["bundleHash"]==geometry.OriginalBundleHash&&(string)p["contextId"]==geometry.OriginalContextId);
   long tick=BuffWire.Number(p["observedTick"],0),revision=BuffWire.Number(p["sourceRevision"],1);Need(tick==readOriginalTick());
   if(current!=null){Need(revision>=(long)current["sourceRevision"]&&tick>=(long)current["observedTick"]);if(revision==(long)current["sourceRevision"]){Need(JToken.DeepEquals(current,p));return;}}
   var placement=geometry.PhysicalPlacement;var next=new Dictionary<string,JObject>(StringComparer.Ordinal);
   int count=placement.OccupiedCount+placement.Slots.Count(c=>c!=null&&!c.IsCargo);
   foreach(var row in BuffWire.Array(p["poses"],count,count)){
    var pose=BuffWire.Object(row);BuffWire.Fields(pose,"entityId","kind","originalPhysicalSlot","originalInstanceId","physicalSlot","instanceId","instanceGeneration","entityGeneration","pathRevision","positionMm");
    string id=BuffWire.Text(pose["entityId"]),kind=BuffWire.One(pose["kind"],"OPERATOR","CARRIAGE");int slot=(int)BuffWire.Number(pose["physicalSlot"],0,4);
    var car=placement.RequireCar(slot,(string)pose["instanceId"]);int originalSlot=(int)BuffWire.Number(pose["originalPhysicalSlot"],0,4);var originalCar=placement.RequireCar(originalSlot,(string)pose["originalInstanceId"]);
    if(kind=="OPERATOR"){placement.RequireActor(originalSlot,originalCar.InstanceId,id);Need(geometry.Connected(originalSlot,slot));}else Need(id==car.InstanceId&&originalSlot==slot);
    Need(BuffWire.Number(pose["instanceGeneration"],1)==geometry.InstanceGeneration(slot));long generation=BuffWire.Number(pose["entityGeneration"],1),path=BuffWire.Number(pose["pathRevision"],0);
    Need(geometry.OnRoof(slot,BMmPoint.Read(pose["positionMm"]))&&!next.ContainsKey(id));
    if(poses.TryGetValue(id,out var prior)){long old=(long)prior["entityGeneration"];Need(generation>=old&&(generation>old||path>=(long)prior["pathRevision"]));}
    next.Add(id,pose);
   }
   Need(placement.Slots.Where(c=>c!=null).All(c=>next.ContainsKey(c.InstanceId)&&(c.IsCargo||next.ContainsKey(c.OperatorId))));
   Need(tick==readOriginalTick());current=p;poses=next;
  }
  public JObject Capture()=>current==null?throw new InvalidOperationException("b_geometry_pose_unobserved"):(JObject)current.DeepClone();
  public static BPhysicalGeometryObservationOwner Restore(JObject snapshot,string hash,BOrdinaryGeometryBinding geometry,Func<long> originalTick){var owner=new BPhysicalGeometryObservationOwner(geometry,originalTick);owner.Publish(snapshot,hash);return owner;}
  public JObject Observe(string sourceId,string targetId,long expectedTick,long expectedRevision,long sourceGeneration,long targetGeneration,long sourcePathRevision,long targetPathRevision,long maximumDistanceMm){
   Need(!publishing&&current!=null&&expectedTick==readOriginalTick()&&expectedTick==(long)current["observedTick"]&&expectedRevision==(long)current["sourceRevision"]&&maximumDistanceMm>=0&&maximumDistanceMm<=1000000000);
   Need(poses.TryGetValue(sourceId,out var source)&&poses.TryGetValue(targetId,out var target));
   // Both dictionary lookups are explicit after validation to retain definite assignment.
   source=poses[sourceId];target=poses[targetId];
   Need((long)source["entityGeneration"]==sourceGeneration&&(long)target["entityGeneration"]==targetGeneration&&(long)source["pathRevision"]==sourcePathRevision&&(long)target["pathRevision"]==targetPathRevision);
   int a=(int)source["physicalSlot"],b=(int)target["physicalSlot"];var fire=Reference(source,a,"fireMm");var hit=Reference(target,b,"hitMm");bool los=geometry.Los(fire,hit);
   return new JObject{["version"]=Version,["sourceStatus"]="TEST_SPACE_DISTANCE_LOS_NOT_COMBAT_APPROVAL",["sourceId"]=sourceId,["targetId"]=targetId,["sourceSlot"]=a,["targetSlot"]=b,["sourceInstanceId"]=source["instanceId"].DeepClone(),["targetInstanceId"]=target["instanceId"].DeepClone(),["sourceGeneration"]=sourceGeneration,["targetGeneration"]=targetGeneration,["sourcePathRevision"]=sourcePathRevision,["targetPathRevision"]=targetPathRevision,["observedTick"]=expectedTick,["validUntilTick"]=expectedTick,["revision"]=expectedRevision,["geometryHash"]=geometry.Hash,["geometryVersion"]=BOrdinaryGeometryBinding.PreparedVersion,["definitionHash"]=geometry.Definition.Hash,["bundleHash"]=geometry.OriginalBundleHash,["contextId"]=geometry.OriginalContextId,["sourcePointMm"]=fire.Capture(),["targetPointMm"]=hit.Capture(),["distanceMm"]=BMmPoint.Distance(fire,hit),["maximumDistanceMm"]=maximumDistanceMm,["lineOfSight"]=los,["connected"]=geometry.Connected(a,b),["eligible"]=los&&BMmPoint.Squared(fire,hit)<=(System.Numerics.BigInteger)maximumDistanceMm*maximumDistanceMm};
  }
  BMmPoint Reference(JObject pose,int slot,string name){var p=BMmPoint.Read(pose["positionMm"]);var origin=geometry.World(slot,geometry.Definition.Point("actorMm"));var reference=geometry.World(slot,geometry.Definition.Point(name));return new BMmPoint(checked(p.X+reference.X-origin.X),checked(p.Y+reference.Y-origin.Y),checked(p.Z+reference.Z-origin.Z));}
 }
}
