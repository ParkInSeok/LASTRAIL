using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Immutable path configuration; no camera, route decision, damage or score authority.
 public readonly struct RailPathPoint {
  public decimal ArcMm {get;} public decimal ForwardMm {get;} public decimal DriverRightMm {get;}
  public RailPathPoint(decimal arcMm,decimal forwardMm,decimal rightMm){ArcMm=arcMm;ForwardMm=forwardMm;DriverRightMm=rightMm;}
 }
 public sealed class MainlineDetourPath {
  readonly JObject original;readonly RailPathPoint[] mainline,detour;readonly RailVehicleShape[] vehicles;
  public const string Version="world-boss.rail-geometry.v1.20261002";
  public string GeometryId {get;} public string RoundId {get;}
  public long MaxSpeed {get;} public long MaxAcceleration {get;} public long MaxSampleMs {get;}
  public long SplitSmm {get;} public long LeftMergeSmm {get;} public long RightMergeSmm {get;}
  public IReadOnlyList<RailVehicleShape> Vehicles=>Array.AsReadOnly(vehicles);
  public long ConsistLengthMm {get;}
  public MainlineDetourPath(JObject profile){
   if(profile==null||profile.ToString(Newtonsoft.Json.Formatting.None).Length>1048576)throw new ArgumentException("Geometry profile limit");
   original=(JObject)profile.DeepClone();
   WorldBossRailSnapshot.Fields(original,"version","status","geometryRef","contentVersion","roundId","frame","maxVehicles","maxConsistLengthMm","maxSpeedMmPerSecond","maxAccelerationMmPerSecondSquared","maxSampleMs","paths","vehicles","attack");
   if((string)original["version"]!=Version||(string)original["status"]!="LOCAL_TEST_PROFILE"||(string)original["frame"]!="PATH_LOCAL_FORWARD_RIGHT_MM")throw new ArgumentException("Geometry version/frame");
   GeometryId=WorldBossRailSnapshot.Text(original,"geometryRef",2407);RoundId=WorldBossRailSnapshot.Text(original,"roundId");WorldBossRailSnapshot.Text(original,"contentVersion");
   var attack=original["attack"] as JObject;WorldBossRailSnapshot.Fields(attack,"impactAtMs","areas");Natural(attack,"impactAtMs",9007199254740991);
   var areas=attack["areas"] as JArray;if(areas==null||areas.Count<1||areas.Count>64)throw new ArgumentException("Attack area count");
   foreach(var token in areas){var area=token as JObject;WorldBossRailSnapshot.Fields(area,"minXmm","maxXmm","minYmm","maxYmm");foreach(var value in area.Properties().Select(a=>a.Value)){if(value.Type!=JTokenType.Integer&&value.Type!=JTokenType.Float)throw new ArgumentException("Finite area coordinate required");double number=(double)value;if(double.IsNaN(number)||double.IsInfinity(number))throw new ArgumentException("Finite area coordinate required");}if((double)area["minXmm"]>=(double)area["maxXmm"]||(double)area["minYmm"]>=(double)area["maxYmm"])throw new ArgumentException("Ordered attack area required");}
   MaxSpeed=Positive(original,"maxSpeedMmPerSecond",1000000);MaxAcceleration=Positive(original,"maxAccelerationMmPerSecondSquared",1000000);MaxSampleMs=Positive(original,"maxSampleMs",10000);
   var paths=original["paths"] as JObject;WorldBossRailSnapshot.Fields(paths,"LEFT","RIGHT");
   mainline=ReadPath(paths["LEFT"] as JObject,false,out long split,out long lm);detour=ReadPath(paths["RIGHT"] as JObject,true,out long rs,out long rm);
   SplitSmm=split;LeftMergeSmm=lm;RightMergeSmm=rm;
   if(split!=rs||!Same(mainline.Where(p=>p.ArcMm<=split),detour.Where(p=>p.ArcMm<=rs),0,0)||!Same(mainline.Where(p=>p.ArcMm>=lm),detour.Where(p=>p.ArcMm>=rm),lm,rm))throw new ArgumentException("Common approach/merge mismatch");
   var raw=original["vehicles"] as JArray;long maxCount=Positive(original,"maxVehicles",64),maxLength=Positive(original,"maxConsistLengthMm",1000000000);
   if(raw==null||raw.Count<1||raw.Count>maxCount)throw new ArgumentException("Consist count");
   vehicles=raw.Select(t=>{var v=t as JObject;WorldBossRailSnapshot.Fields(v,"id","lengthMm","gapAfterMm","halfWidthMm");return new RailVehicleShape(WorldBossRailSnapshot.Text(v,"id"),Positive(v,"lengthMm",1000000000),Natural(v,"gapAfterMm",1000000000),Positive(v,"halfWidthMm",1000000000));}).ToArray();
   ConsistLengthMm=vehicles.Sum(v=>v.LengthMm+v.GapAfterMm);
   if(vehicles.Last().GapAfterMm!=0||ConsistLengthMm>maxLength||vehicles.Select(v=>v.Id).Distinct().Count()!=vehicles.Length)throw new ArgumentException("Consist bounds/identity");
  }
  internal static long Natural(JObject o,string key,long max){long n=WorldBossRailSnapshot.Nat(o,key);if(n>max)throw new ArgumentException(key+" bound");return n;}
  internal static long Positive(JObject o,string key,long max){long n=Natural(o,key,max);if(n==0)throw new ArgumentException(key+" positive");return n;}
  static bool Same(IEnumerable<RailPathPoint> a,IEnumerable<RailPathPoint> b,long ao,long bo)=>a.Select(p=>new RailPathPoint(p.ArcMm-ao,p.ForwardMm,p.DriverRightMm)).SequenceEqual(b.Select(p=>new RailPathPoint(p.ArcMm-bo,p.ForwardMm,p.DriverRightMm)));
  static RailPathPoint[] ReadPath(JObject p,bool right,out long split,out long merge){
   WorldBossRailSnapshot.Fields(p,"splitSmm","mergeSmm","points");split=Positive(p,"splitSmm",1000000000);merge=Positive(p,"mergeSmm",1000000000);
   var raw=p["points"] as JArray;if(raw==null||raw.Count<4||raw.Count>128)throw new ArgumentException("Path point count");
   var result=raw.Select(t=>{var q=t as JObject;WorldBossRailSnapshot.Fields(q,"sMm","xMm","yMm");long s=Natural(q,"sMm",1000000000);if(q["xMm"].Type!=JTokenType.Integer||q["yMm"].Type!=JTokenType.Integer)throw new ArgumentException("Integer point required");long x=(long)q["xMm"],y=(long)q["yMm"];if(Math.Abs(x)>1000000000||Math.Abs(y)>1000000000||y<0||!right&&y!=0)throw new ArgumentException("Directed path");return new RailPathPoint(s,x,y);}).ToArray();
   long splitValue=split,mergeValue=merge;
   if(result[0].ArcMm!=0||split>=merge||result.Last().ArcMm<=merge||!result.Any(q=>q.ArcMm==splitValue&&q.DriverRightMm==0)||!result.Any(q=>q.ArcMm==mergeValue&&q.DriverRightMm==0)||right&&!result.Any(q=>q.ArcMm>splitValue&&q.ArcMm<mergeValue&&q.DriverRightMm>0))throw new ArgumentException("Split/merge");
   for(int i=1;i<result.Length;i++){var a=result[i-1];var b=result[i];double dx=(double)(b.ForwardMm-a.ForwardMm),dy=(double)(b.DriverRightMm-a.DriverRightMm);if(b.ArcMm<=a.ArcMm||dx<=0||Math.Abs((double)(b.ArcMm-a.ArcMm)-Math.Sqrt(dx*dx+dy*dy))>1)throw new ArgumentException("Arc length/forward direction");}return result;
  }
  internal static void RequireRoute(string route){if(route!="LEFT"&&route!="RIGHT")throw new ArgumentException("Exactly two route choices");}
  RailPathPoint[] Points(string route){RequireRoute(route);return route=="RIGHT"?detour:mainline;}
  public long LengthMm(string route)=>(long)Points(route).Last().ArcMm;
  public long MergeSmm(string route){RequireRoute(route);return route=="RIGHT"?RightMergeSmm:LeftMergeSmm;}
  public RailPathPoint At(string route,decimal arc){var p=Points(route);if(arc<0||arc>p.Last().ArcMm)throw new ArgumentOutOfRangeException(nameof(arc));int i=1;while(i<p.Length-1&&p[i].ArcMm<arc)i++;decimal u=(arc-p[i-1].ArcMm)/(p[i].ArcMm-p[i-1].ArcMm);return new RailPathPoint(arc,p[i-1].ForwardMm+(p[i].ForwardMm-p[i-1].ForwardMm)*u,p[i-1].DriverRightMm+(p[i].DriverRightMm-p[i-1].DriverRightMm)*u);}
  // Include intermediate vertices: a body spanning a bend is not a front/rear chord.
  public IReadOnlyList<RailPathPoint> OccupiedCenterline(string route,decimal rear,decimal front){if(rear>front)throw new ArgumentException("Body order");var points=Points(route);var result=new List<RailPathPoint>{At(route,rear)};result.AddRange(points.Where(p=>p.ArcMm>rear&&p.ArcMm<front));result.Add(At(route,front));return result.AsReadOnly();}
  public JObject OriginalProfile()=>(JObject)original.DeepClone();
 }
 public readonly struct RailVehicleShape {
  public string Id {get;}public long LengthMm {get;}public long GapAfterMm {get;}public long HalfWidthMm {get;}
  internal RailVehicleShape(string id,long length,long gap,long width){Id=id;LengthMm=length;GapAfterMm=gap;HalfWidthMm=width;}
 }
 public readonly struct RailVehicleOccupancy {
  public string Id {get;}public long FrontArcMm {get;}public long RearArcMm {get;}public long HalfWidthMm {get;}
  public RailPathPoint Front {get;}public RailPathPoint Rear {get;}public IReadOnlyList<RailPathPoint> Centerline {get;}
  internal RailVehicleOccupancy(RailVehicleShape shape,long front,MainlineDetourPath path,string route){Id=shape.Id;HalfWidthMm=shape.HalfWidthMm;FrontArcMm=front;RearArcMm=front-shape.LengthMm;Front=path.At(route,FrontArcMm);Rear=path.At(route,RearArcMm);Centerline=path.OccupiedCenterline(route,RearArcMm,FrontArcMm);}
 }
}



