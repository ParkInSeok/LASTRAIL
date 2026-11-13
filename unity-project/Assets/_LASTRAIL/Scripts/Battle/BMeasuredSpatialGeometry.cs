using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // A bounded consumer for the new rectangular, translated authoring draft.
 // Native instance IDs come from the supplied original formation, never from
 // the template names. This does not authorize that formation for START.
 internal sealed class BMeasuredSpatialGeometry:BOrdinarySpatialGeometry {
  readonly BMeasuredPhysicalGeometry source;readonly BDirectAttackInput input;readonly JObject packet;
  readonly Dictionary<(int,int),BMmPoint[]> links=new Dictionary<(int,int),BMmPoint[]>();
  internal BMeasuredSpatialGeometry(BMeasuredPhysicalGeometry source,BDirectAttackBuffBinding binding){
   this.source=source??throw new ArgumentNullException(nameof(source));input=binding?.Attack??throw new ArgumentNullException(nameof(binding));
   if(Count<1||Count>5)throw new ArgumentException("b_measured_native_formation");
   for(int i=0;i<Count;i++){source.WorldRoof(i);for(int j=i+1;j<Count;j++)if(source.HasDirectBridge(i,j)){
    var path=source.Bridge(i,j).Select(BMmPoint.Read).ToArray();links.Add((i,j),path);links.Add((j,i),path.Reverse().ToArray());
   }}
   packet=new JObject{["version"]=Version,["sourceStatus"]=SourceStatus,["authoringHash"]=source.Hash,
    ["inputHash"]=input.OriginalHash,["compositionHash"]=binding.OriginalHash,
    ["instances"]=new JArray(Enumerable.Range(0,Count).Select(i=>new JObject{["physicalSlot"]=i,["carriageInstanceId"]=input.Formation.Slots[i].CarriageId,["operatorId"]=input.Formation.Slots[i].OperatorId,
      ["measurementIdentity"]=source.Hash+":"+i,["instanceGeneration"]=JValue.CreateNull()})),
    ["spatialPolicy"]="RECTANGULAR_ROOF_CORNERS_GUNNER_CLOSEST_ROOF_SCOUT_BODY_INTERIOR_OCCLUSION_V1",
    ["operatorMovementSource"]=OperatorMovementSource};
   Hash=BDirectAttackInput.Digest(packet);
  }
  internal override BDirectAttackInput Input=>input;
  internal override string Hash {get;}
  internal override string DefinitionHash=>source.DefinitionHash;
  internal override string Version=>"unity.combat-b.measured-spatial-draft.v1.20261020";
  internal override string SourceStatus=>"MEASURED_AUTHORED_DRAFT_NOT_RUNTIME_ADMITTED";
  internal override string OperatorMovementSource=>"NOT_SUPPLIED_MOVEMENT_UNSUPPORTED";
  internal override int Count=>input.Formation.Slots.Count;
  internal override JObject Capture()=>(JObject)packet.DeepClone();
  void Slot(int slot){if(slot<0||slot>=Count)throw new ArgumentException("b_measured_native_slot_missing");}
  internal override BMmPoint Anchor(int slot,string name){Slot(slot);string id=name=="actorMm"?"Foot":name=="fireMm"?"Fire":name=="hitMm"?"Hit":throw new ArgumentException("b_measured_anchor_kind");return BMmPoint.Read(source.WorldAnchor(slot,id));}
  internal override BMmPoint World(int slot,BMmPoint point){Slot(slot);return BMmPoint.Read(source.WorldPoint(slot,point.Capture()));}
  (BMmPoint min,BMmPoint max) Roof(int slot){
   Slot(slot);var p=source.WorldRoof(slot).Select(BMmPoint.Read).ToArray();
   return (new BMmPoint(p.Min(v=>v.X),p.Min(v=>v.Y),p.Min(v=>v.Z)),new BMmPoint(p.Max(v=>v.X),p.Max(v=>v.Y),p.Max(v=>v.Z)));
  }
  internal override bool ContainsLocalRoof(int slot,BMmPoint p){var roof=Roof(slot);return World(slot,p).Inside(roof.min,roof.max);}
  internal override long OperatorSpeed(string actor)=>throw new InvalidOperationException("b_measured_operator_movement_source_missing");
  internal override BMmPoint[] GunnerPoints(int slot){Slot(slot);return source.WorldRoof(slot).Select(BMmPoint.Read).ToArray();}
  internal override BMmPoint ScoutGoal(int slot,BMmPoint target)=>ClampRoofPoint(slot,target);
  internal override BMmPoint[] WorldPath(int slot,int side,string phase){Slot(slot);return source.WorldSocketPath(slot,side==0?"left":"right",phase=="approachMm"?"approach":"climb").Select(BMmPoint.Read).ToArray();}
  int[] Slots(int a,int b){
   Slot(a);Slot(b);var queue=new Queue<int>();var prior=new Dictionary<int,int>{{a,a}};queue.Enqueue(a);
   while(queue.Count>0){int current=queue.Dequeue();if(current==b){var path=new List<int>{b};while(path.Last()!=a)path.Add(prior[path.Last()]);path.Reverse();return path.ToArray();}
    foreach(int next in links.Keys.Where(k=>k.Item1==current).Select(k=>k.Item2).OrderBy(v=>v))if(!prior.ContainsKey(next)){prior.Add(next,current);queue.Enqueue(next);}
   }return null;
  }
  internal override bool Connected(int a,int b)=>Slots(a,b)!=null;
  internal override BMmPoint[] Route(BMmPoint from,int a,int b,BMmPoint target){
   var slots=Slots(a,b);if(slots==null)return null;var points=new List<BMmPoint>{from};for(int i=1;i<slots.Length;i++)points.AddRange(links[(slots[i-1],slots[i])]);points.Add(target);return points.ToArray();
  }
  internal override BMmPoint[] GroundRoute(BMmPoint from,int a,int b,int side){
   var slots=Slots(a,b);if(slots==null)return null;var points=new List<BMmPoint>{from,WorldPath(a,side,"approachMm")[0]};
   for(int i=1;i<slots.Length;i++){var exit=WorldPath(slots[i-1],side,"approachMm")[0];var entry=WorldPath(slots[i],side,"approachMm")[0];var bridge=links[(slots[i-1],slots[i])];
    points.Add(new BMmPoint(bridge.First().X,exit.Y,exit.Z));points.Add(new BMmPoint(bridge.Last().X,entry.Y,entry.Z));}
   points.Add(WorldPath(b,side,"approachMm")[0]);return points.ToArray();
  }
  internal override int RoofSlot(BMmPoint p,int previous){for(int i=0;i<Count;i++){var roof=Roof(i);if(p.Inside(roof.min,roof.max))return i;}return previous;}
  internal override BMmPoint ClampRoofPoint(int slot,BMmPoint p){var r=Roof(slot);return new BMmPoint(Math.Max(r.min.X,Math.Min(r.max.X,p.X)),r.min.Y,Math.Max(r.min.Z,Math.Min(r.max.Z,p.Z)));}
  // Contact with the outer hull is allowed (feet on roof / ground paths).
  // Only a nonzero segment interval strictly inside a body blocks LOS.
  static bool Interior(BMmPoint a,BMmPoint b,BMmPoint min,BMmPoint max){
   BigInteger ln=0,ld=1,hn=1,hd=1;var s=new[]{a.X,a.Y,a.Z};var e=new[]{b.X,b.Y,b.Z};var low=new[]{min.X,min.Y,min.Z};var high=new[]{max.X,max.Y,max.Z};
   for(int i=0;i<3;i++){long delta=e[i]-s[i];if(delta==0){if(s[i]<=low[i]||s[i]>=high[i])return false;continue;}
    BigInteger x=low[i]-s[i],y=high[i]-s[i],den=delta;if(den<0){den=-den;var z=x;x=-y;y=-z;}
    if(x*ld>ln*den){ln=x;ld=den;}if(y*hd<hn*den){hn=y;hd=den;}if(ln*hd>=hn*ld)return false;
   }return true;
  }
  internal override bool Los(BMmPoint a,BMmPoint b){
   for(int i=0;i<Count;i++){var points=source.WorldBodyCorners(i).Select(BMmPoint.Read).ToArray();var min=new BMmPoint(points.Min(p=>p.X),points.Min(p=>p.Y),points.Min(p=>p.Z));var max=new BMmPoint(points.Max(p=>p.X),points.Max(p=>p.Y),points.Max(p=>p.Z));if(Interior(a,b,min,max))return false;}return true;
  }
  internal override JObject InstanceMetadata(int slot){Slot(slot);return new JObject{["instanceGeneration"]=JValue.CreateNull(),["instanceIdentity"]=input.Formation.Slots[slot].CarriageId,["measurementIdentity"]=source.Hash+":"+slot};}
  internal override BWaveBoardingLocalPaths Paths(){
   JObject Side(int i,int side)=>new JObject{["socketId"]=input.Formation.Slots[i].CarriageId+":"+(side==0?"left":"right"),["approachPathMm"]=new JArray(WorldPath(i,side,"approachMm").Select(p=>p.Capture())),["climbPathMm"]=new JArray(WorldPath(i,side,"climbMm").Select(p=>p.Capture()))};
   var p=new JObject{["version"]=BWaveBoardingLocalPaths.MeasuredVersion,["provenance"]=SourceStatus,["inputHash"]=input.OriginalHash,["carriages"]=new JArray(Enumerable.Range(0,Count).Select(i=>new JObject{["carriageId"]=input.Formation.Slots[i].CarriageId,["slot"]=i,["left"]=Side(i,0),["right"]=Side(i,1)}))};
   return BWaveBoardingLocalPaths.ReadMeasured(p,BDirectAttackInput.Digest(p),input);
  }
 }
}
