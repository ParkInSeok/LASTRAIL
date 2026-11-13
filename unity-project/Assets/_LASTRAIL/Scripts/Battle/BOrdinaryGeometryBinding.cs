using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Original instance transforms and topology; never infers connections from slot proximity.
 public sealed class BOrdinaryGeometryBinding {
  public const string Version="unity.combat-b.geometry-instances-test.v1.20261004";
  public const string PreparedVersion="unity.combat-b.physical-geometry-test.v1.20261006";
  readonly JObject packet;readonly Dictionary<int,BMmPoint> translations=new Dictionary<int,BMmPoint>();readonly Dictionary<int,int> yaw=new Dictionary<int,int>();readonly Dictionary<(int,int),BMmPoint[]> links=new Dictionary<(int,int),BMmPoint[]>();
  public BOrdinaryGeometrySnapshot Definition {get;}public string Hash {get;}internal BDirectAttackInput Input {get;}internal int Count=>translations.Keys.Max()+1;
  public BCombatPhysicalPlacement PhysicalPlacement {get;}
  public IReadOnlyList<int> OccupiedSlots=>Array.AsReadOnly(translations.Keys.OrderBy(i=>i).ToArray());
  public string OriginalContextId=>(string)packet["contextId"];public string OriginalBundleHash=>(string)packet["bundleHash"];
  public JObject Capture()=>(JObject)packet.DeepClone();
  public static BOrdinaryGeometryBinding Read(JObject p,string hash,BOrdinaryGeometrySnapshot asset,BDirectAttackBuffBinding native)=>new BOrdinaryGeometryBinding(p,hash,asset,native);
  BOrdinaryGeometryBinding(JObject p,string hash,BOrdinaryGeometrySnapshot asset,BDirectAttackBuffBinding native){CheckHash(p,hash);Definition=asset??throw new ArgumentNullException(nameof(asset));Input=native?.Attack??throw new ArgumentNullException(nameof(native));BuffWire.Fields(p,"version","geometryHash","inputHash","sourceRevision","instances","connections");BuffWire.Need((string)p["version"]==Version&&(string)p["geometryHash"]==asset.Hash&&(string)p["inputHash"]==Input.OriginalHash&&BuffWire.Number(p["sourceRevision"],1)==1,"b_geometry_instance_version");var rows=BuffWire.Array(p["instances"],CountOf(native),CountOf(native));var seen=new HashSet<int>();foreach(var row in rows){var c=BuffWire.Object(row);InstanceFields(c);int i=(int)BuffWire.Number(c["slot"],0,rows.Count-1);var car=((JArray)native.Capture()["departure"]["cars"]).Single(x=>(int)x["slot"]==i);BuffWire.Need(seen.Add(i)&&(string)c["carriageId"]==Input.Formation.Slots[i].CarriageId&&(string)c["sourceDefinitionId"]==(string)car["carriageId"]&&BuffWire.Number(c["generation"],1)==1,"b_geometry_original_instance");AddInstance(i,c);}
   Connections(p,false);packet=(JObject)p.DeepClone();Hash=hash;
  }
  public static BOrdinaryGeometryBinding ReadPrepared(JObject p,string hash,BOrdinaryGeometrySnapshot asset,BCombatPhysicalPlacement placement,string originalContextId,string originalBundleHash)=>new BOrdinaryGeometryBinding(p,hash,asset,placement,originalContextId,originalBundleHash);
  BOrdinaryGeometryBinding(JObject p,string hash,BOrdinaryGeometrySnapshot asset,BCombatPhysicalPlacement placement,string context,string bundleHash){
   CheckHash(p,hash);Definition=asset??throw new ArgumentNullException(nameof(asset));PhysicalPlacement=placement??throw new ArgumentNullException(nameof(placement));
   BuffWire.Fields(p,"version","geometryHash","placementHash","bundleHash","contextId","provenance","transformSource","sourceRevision","instances","connections");
   BuffWire.Need((string)p["version"]==PreparedVersion&&(string)p["geometryHash"]==asset.Hash&&(string)p["placementHash"]==BDirectAttackInput.Digest(placement.Capture())&&(string)p["bundleHash"]==bundleHash&&(string)p["contextId"]==context&&!string.IsNullOrEmpty(context)&&(string)p["provenance"]=="EXPLICIT_TEST_PHYSICAL_TRANSFORMS_NOT_OPERATING_GEOMETRY"&&BuffWire.Number(p["sourceRevision"],1)==1,"b_geometry_prepared_source_conflict");BuffWire.Text(p["transformSource"],1024);
   foreach(var row in BuffWire.Array(p["instances"],placement.OccupiedCount,placement.OccupiedCount)){
    var c=BuffWire.Object(row);InstanceFields(c);int i=(int)BuffWire.Number(c["slot"],0,4);var car=placement.RequireCar(i,(string)c["carriageId"]);
    BuffWire.Need((string)c["sourceDefinitionId"]==car.DefinitionId&&!translations.ContainsKey(i),"b_geometry_prepared_instance_conflict");BuffWire.Number(c["generation"],1);AddInstance(i,c);
   }
   Connections(p,true);packet=(JObject)p.DeepClone();Hash=hash;
  }
  static void CheckHash(JObject p,string hash)=>BuffWire.Need(p!=null&&BasicCombatSimulation.StableJson(p).Length<=65536&&BDirectAttackInput.Digest(p)==hash,"b_geometry_instance_hash");
  static void InstanceFields(JObject c)=>BuffWire.Fields(c,"slot","carriageId","sourceDefinitionId","generation","positionMm","yawQuarterTurns");
  void AddInstance(int slot,JObject c){translations.Add(slot,BMmPoint.Read(c["positionMm"]));yaw.Add(slot,(int)BuffWire.Number(c["yawQuarterTurns"],0,3));}
  void Connections(JObject p,bool sparse){foreach(var row in BuffWire.Array(p["connections"],0,4)){
   var c=BuffWire.Object(row);BuffWire.Fields(c,"fromSlot","toSlot","bridgeMm");int a=(int)BuffWire.Number(c["fromSlot"],0,Count-1),b=(int)BuffWire.Number(c["toSlot"],0,Count-1);
   BuffWire.Need(translations.ContainsKey(a)&&translations.ContainsKey(b)&&(sparse?b>a:b==a+1)&&!links.ContainsKey((a,b)),"b_geometry_connection_topology");var points=BuffWire.Array(c["bridgeMm"],2,16).Select(BMmPoint.Read).ToArray();
   BuffWire.Need(JToken.DeepEquals(points.First().Capture(),World(a,Definition.Point("frontMm")).Capture())&&JToken.DeepEquals(points.Last().Capture(),World(b,Definition.Point("backMm")).Capture())&&BOrdinaryGeometrySnapshot.Length(points)<=5000,"b_geometry_connection_endpoints");links.Add((a,b),points);links.Add((b,a),points.Reverse().ToArray());
  }}
  static int CountOf(BDirectAttackBuffBinding b)=>b.Attack.Formation.Slots.Count;
  void RequireSlot(int slot){if(!translations.ContainsKey(slot))throw new ArgumentException("b_geometry_physical_slot_missing");}
  internal BMmPoint World(int slot,BMmPoint p){RequireSlot(slot);long x=p.X,z=p.Z;for(int i=0;i<yaw[slot];i++){long old=x;x=-z;z=old;}var t=translations[slot];return new BMmPoint(checked(x+t.X),checked(p.Y+t.Y),checked(z+t.Z));}
  public JArray WorldPoint(int slot,JArray point)=>World(slot,BMmPoint.Read(point)).Capture();
  internal BMmPoint[] WorldPath(int slot,int side,string field)=>Definition.Path(side,field).Select(p=>World(slot,p)).ToArray();
  BMmPoint Local(int slot,BMmPoint position){RequireSlot(slot);var t=translations[slot];long x=position.X-t.X,z=position.Z-t.Z;for(int i=0;i<yaw[slot];i++){long old=x;x=z;z=-old;}return new BMmPoint(x,position.Y-t.Y,z);}
  internal bool OnRoof(int slot,BMmPoint point)=>Local(slot,point).Inside(Definition.Point("roofMinMm"),Definition.Point("roofMaxMm"));
  internal long InstanceGeneration(int slot){RequireSlot(slot);return (long)((JArray)packet["instances"]).Single(p=>(int)p["slot"]==slot)["generation"];}
  internal int RoofSlot(BMmPoint position,int previous){foreach(int slot in OccupiedSlots)if(OnRoof(slot,position))return slot;return previous;}
  internal BMmPoint ClampRoofPoint(int slot,BMmPoint position){var p=Local(slot,position);var min=Definition.Point("roofMinMm");var max=Definition.Point("roofMaxMm");return World(slot,new BMmPoint(Math.Max(min.X,Math.Min(max.X,p.X)),Math.Max(min.Y,Math.Min(max.Y,p.Y)),Math.Max(min.Z,Math.Min(max.Z,p.Z))));}
  int[] SlotRoute(int a,int b){RequireSlot(a);RequireSlot(b);var pending=new Queue<int>();var prior=new Dictionary<int,int>{{a,a}};pending.Enqueue(a);while(pending.Count>0){int current=pending.Dequeue();if(current==b){var path=new List<int>{b};while(path.Last()!=a)path.Add(prior[path.Last()]);path.Reverse();return path.ToArray();}foreach(int next in links.Keys.Where(k=>k.Item1==current).Select(k=>k.Item2).OrderBy(i=>i))if(!prior.ContainsKey(next)){prior.Add(next,current);pending.Enqueue(next);}}return null;}
  internal bool Connected(int a,int b)=>SlotRoute(a,b)!=null;
  public bool ConnectedPhysicalSlots(int a,int b)=>Connected(a,b);
  internal BMmPoint[] Route(BMmPoint from,int a,int b,BMmPoint target){var slots=SlotRoute(a,b);if(slots==null)return null;var points=new List<BMmPoint>{from};for(int i=1;i<slots.Length;i++)points.AddRange(links[(slots[i-1],slots[i])]);points.Add(target);return points.ToArray();}
  internal BMmPoint[] GroundRoute(BMmPoint from,int a,int b,int side){var slots=SlotRoute(a,b);if(slots==null)return null;var points=new List<BMmPoint>{from,WorldPath(a,side,"approachMm").First()};for(int i=1;i<slots.Length;i++){int x=slots[i-1],y=slots[i];var local=Definition.Path(side,"approachMm").First();var exit=Definition.Point(y>x?"frontMm":"backMm");var entry=Definition.Point(y>x?"backMm":"frontMm");points.Add(World(x,new BMmPoint(exit.X,local.Y,local.Z)));points.Add(World(y,new BMmPoint(entry.X,local.Y,local.Z)));}points.Add(WorldPath(b,side,"approachMm").First());return points.ToArray();}
  public JArray WorldSocketPath(int slot,string side,string phase){if(side!="left"&&side!="right"||phase!="approach"&&phase!="climb")throw new ArgumentException("b_geometry_socket_path_kind");return new JArray(WorldPath(slot,side=="left"?0:1,phase=="approach"?"approachMm":"climbMm").Select(p=>p.Capture()));}
  internal bool Los(BMmPoint a,BMmPoint b){foreach(int slot in OccupiedSlots)foreach(var o in Definition.Obstacles){var x=World(slot,BMmPoint.Read(o["minMm"]));var y=World(slot,BMmPoint.Read(o["maxMm"]));var min=new BMmPoint(Math.Min(x.X,y.X),Math.Min(x.Y,y.Y),Math.Min(x.Z,y.Z));var max=new BMmPoint(Math.Max(x.X,y.X),Math.Max(x.Y,y.Y),Math.Max(x.Z,y.Z));if(BOrdinaryGeometrySnapshot.SegmentHits(a,b,min,max))return false;}return true;}
  internal BWaveBoardingLocalPaths Paths(){var p=new JObject{["version"]=BWaveBoardingLocalPaths.Version,["provenance"]="EXPLICIT_LOCAL_GRAPH_FIXTURE_NOT_ORDINARY_RUNTIME",["inputHash"]=Input.OriginalHash,["carriages"]=new JArray(Enumerable.Range(0,Count).Select(i=>new JObject{["carriageId"]=Input.Formation.Slots[i].CarriageId,["slot"]=i,["left"]=Side(i,0),["right"]=Side(i,1)}))};return BWaveBoardingLocalPaths.Read(p,BDirectAttackInput.Digest(p),Input);}
  JObject Side(int i,int side)=>new JObject{["socketId"]=Input.Formation.Slots[i].CarriageId+":"+(side==0?"left":"right"),["approachPathMm"]=new JArray(WorldPath(i,side,"approachMm").Select(p=>p.Capture())),["climbPathMm"]=new JArray(WorldPath(i,side,"climbMm").Select(p=>p.Capture()))};
 }
}
