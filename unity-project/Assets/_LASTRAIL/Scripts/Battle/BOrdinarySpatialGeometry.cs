using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Only the spatial queries used by the existing native owner. Historical test
 // geometry and measured authoring retain their own validation and provenance.
 internal abstract class BOrdinarySpatialGeometry {
  internal abstract BDirectAttackInput Input {get;}
  internal abstract string Hash {get;}
  internal abstract string DefinitionHash {get;}
  internal abstract string Version {get;}
  internal abstract string SourceStatus {get;}
  internal abstract string OperatorMovementSource {get;}
  internal abstract int Count {get;}
  internal abstract JObject Capture();
  internal abstract BMmPoint Anchor(int slot,string name);
  internal virtual BMmPoint EntityAnchor(string id,int slot,string name)=>Anchor(slot,name);
  internal abstract BMmPoint World(int slot,BMmPoint point);
  internal abstract bool ContainsLocalRoof(int slot,BMmPoint point);
  internal abstract long OperatorSpeed(string actor);
  internal abstract BMmPoint[] GunnerPoints(int slot);
  internal abstract BMmPoint ScoutGoal(int slot,BMmPoint target);
  internal abstract BMmPoint[] WorldPath(int slot,int side,string phase);
  internal abstract BMmPoint[] Route(BMmPoint from,int a,int b,BMmPoint target);
  internal abstract BMmPoint[] GroundRoute(BMmPoint from,int a,int b,int side);
  internal abstract int RoofSlot(BMmPoint point,int previous);
  internal abstract BMmPoint ClampRoofPoint(int slot,BMmPoint point);
  internal abstract bool Connected(int a,int b);
  internal abstract bool Los(BMmPoint a,BMmPoint b);
  internal abstract BWaveBoardingLocalPaths Paths();
  internal virtual JObject InstanceMetadata(int slot)=>new JObject{["instanceGeneration"]=1};
 }
#if !LASTRAIL_ORDINARY_CORE
 internal sealed class BLegacySpatialGeometry:BOrdinarySpatialGeometry {
  readonly BOrdinaryGeometryBinding source;
  internal BLegacySpatialGeometry(BOrdinaryGeometryBinding value){source=value??throw new ArgumentNullException(nameof(value));}
  internal override BDirectAttackInput Input=>source.Input;
  internal override string Hash=>source.Hash;
  internal override string DefinitionHash=>source.Definition.Hash;
  internal override string Version=>BOrdinaryGeometrySnapshot.Version;
  internal override string SourceStatus=>"TEST_JUDGEMENT_MODEL_REAL_NATIVE_SPATIAL_SOURCE";
  internal override string OperatorMovementSource=>"EXPLICIT_TEST_PROBE_NOT_OPERATING_OPERATOR_PROFILE";
  internal override int Count=>source.Count;
  internal override JObject Capture()=>source.Capture();
  internal override BMmPoint Anchor(int slot,string name)=>source.World(slot,source.Definition.Point(name));
  internal override BMmPoint World(int slot,BMmPoint p)=>source.World(slot,p);
  internal override bool ContainsLocalRoof(int slot,BMmPoint p)=>p.Inside(source.Definition.Point("roofMinMm"),source.Definition.Point("roofMaxMm"));
  internal override long OperatorSpeed(string actor)=>source.Definition.OperatorProbeSpeed;
  internal override BMmPoint[] GunnerPoints(int slot)=>source.Definition.GunnerAnchors.Select(p=>source.World(slot,p)).ToArray();
  internal override BMmPoint ScoutGoal(int slot,BMmPoint target){
   var a=Anchor(slot,"actorMm");var b=Anchor(slot,"scoutAnchorMm");
   return source.ClampRoofPoint(slot,new BMmPoint(target.X+b.X-a.X,target.Y+b.Y-a.Y,target.Z+b.Z-a.Z));
  }
  internal override BMmPoint[] WorldPath(int slot,int side,string phase)=>source.WorldPath(slot,side,phase);
  internal override BMmPoint[] Route(BMmPoint from,int a,int b,BMmPoint target)=>source.Route(from,a,b,target);
  internal override BMmPoint[] GroundRoute(BMmPoint from,int a,int b,int side)=>source.GroundRoute(from,a,b,side);
  internal override int RoofSlot(BMmPoint p,int previous)=>source.RoofSlot(p,previous);
  internal override BMmPoint ClampRoofPoint(int slot,BMmPoint p)=>source.ClampRoofPoint(slot,p);
  internal override bool Connected(int a,int b)=>source.Connected(a,b);
  internal override bool Los(BMmPoint a,BMmPoint b)=>source.Los(a,b);
  internal override BWaveBoardingLocalPaths Paths()=>source.Paths();
 }
#endif
}
