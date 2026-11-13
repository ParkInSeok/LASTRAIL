using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Explicit independent review factory: no inventory, START, damage, or health mutations.
 public static class LocalFiveCarPartyGeometry {
  public const string Scope="USER_APPROVED_LOCAL_REVIEW_NOT_ACCOUNT_OWNERSHIP";
  public const long ReviewSpeedMmPerSecond=2000;
  public static IOrdinaryPartyMotion Create(string rawDefinition,string trustedRawHash,IReadOnlyList<string> localHosts,
   IReadOnlyDictionary<string,OrdinaryPartyAnchor> initialFeet,BScoutGunnerNodeSnapshot originalWaveSource,long stepMicroseconds,
   Func<long> tick,Func<string,int> hp,Func<string,long> generation,Func<string,bool> canMove){
   if(originalWaveSource==null)throw new ArgumentNullException(nameof(originalWaveSource));
   var geometry=new Geometry(rawDefinition,trustedRawHash,localHosts,initialFeet);
   var roster=new BBoardingRoster(initialFeet.Select(p=>new BBoardingRoster.Entity(p.Key,"OPERATOR",p.Key,p.Value.Slot)),localHosts,stepMicroseconds);
   var actors=new Actors(tick,hp,generation);var spatial=new BOrdinarySpatialOwner(geometry,originalWaveSource,roster);
   return new NativePartyMotion(spatial,geometry,actors,canMove);
  }
  sealed class Actors:IBBoardingActors {
   readonly Func<long> tick;readonly Func<string,int> hp;readonly Func<string,long> generation;
   internal Actors(Func<long> tick,Func<string,int> hp,Func<string,long> generation){this.tick=tick??throw new ArgumentNullException(nameof(tick));this.hp=hp??throw new ArgumentNullException(nameof(hp));this.generation=generation??throw new ArgumentNullException(nameof(generation));}
   public long Tick=>tick();public int Hp(string id)=>hp(id);public long ProjectileTargetGeneration(string id)=>generation(id);
  }
  internal sealed class Geometry:BOrdinarySpatialGeometry {
   readonly JObject definition,layout,packet;readonly JObject[] cars;readonly Dictionary<string,OrdinaryPartyAnchor> feet;
   readonly string[] hosts;readonly Dictionary<(int,int),BMmPoint[]> bridges=new Dictionary<(int,int),BMmPoint[]>();
   readonly BWaveBoardingLocalPaths paths;
   internal Geometry(string raw,string trustedHash,IReadOnlyList<string> localHosts,IReadOnlyDictionary<string,OrdinaryPartyAnchor> initialFeet){
    definition=OrdinaryLiveCombatSources.ReadDefinition(raw,trustedHash);layout=(JObject)definition["source"]["liveLayout"];
    Need(localHosts!=null&&localHosts.Count==6&&localHosts.All(x=>x!=null&&x.StartsWith("LOCAL_QA_",StringComparison.Ordinal))&&localHosts.Distinct().Count()==6,"Explicit six local-only host IDs required");hosts=localHosts.ToArray();
    Need(initialFeet!=null&&initialFeet.Count==5&&initialFeet.Keys.All(x=>!string.IsNullOrWhiteSpace(x)),"Five supplied local initial feet required");feet=initialFeet.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
    var boarding=definition["source"]["boardingGeometry"] as JObject;Need(boarding!=null&&(string)boarding["version"]=="ordinary.authored-boarding-geometry.v1.20261107"&&boarding["cars"] is JArray array&&array.Count==6,"Original six-body boarding geometry required");
    cars=((JArray)boarding["cars"]).Cast<JObject>().OrderBy(c=>(int)c["carOrder"]).ToArray();
    for(int i=0;i<6;i++){
     Need((int?)cars[i]["carOrder"]==i&&(string)cars[i]["authoringId"]==(i==0?"Engine":"Car"+(i-1)),"Original engine and five authored cars required");
     var min=Min(i);var max=Max(i);Need(min.X<max.X&&min.Y==max.Y&&min.Z<0&&max.Z>0,"Planar central roof corridor required");
     if(i==0)continue;
     var front=World(i-1,new BMmPoint(Min(i-1).X,Min(i-1).Y,0));var rear=World(i,new BMmPoint(Max(i).X,Min(i).Y,0));
     Need(front.X>rear.X&&front.Y==rear.Y&&front.Z==rear.Z,"Separated same-height adjacent roofs required");
     bridges.Add((i-1,i),new[]{front,rear});bridges.Add((i,i-1),new[]{rear,front});
    }
    foreach(var p in feet)Need(p.Value.Slot>0&&p.Value.Slot<6&&ContainsLocalRoof(p.Value.Slot,Point(p.Value)),"Local initial feet must lie on a passenger roof");
    packet=new JObject{["version"]=Version,["scope"]=Scope,["sourceDefinitionHash"]=trustedHash,["physicalHostIds"]=new JArray(hosts),["operatorSpeedMmPerSecond"]=ReviewSpeedMmPerSecond,
     ["initialFeet"]=new JArray(feet.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new JObject{["actorId"]=p.Key,["slot"]=p.Value.Slot,["localMm"]=Point(p.Value).Capture()})),
     ["bridges"]=new JArray(bridges.Where(p=>p.Key.Item1<p.Key.Item2).OrderBy(p=>p.Key.Item1).Select(p=>new JObject{["fromSlot"]=p.Key.Item1,["toSlot"]=p.Key.Item2,["worldMm"]=new JArray(p.Value.Select(v=>v.Capture()))}))};
    Hash=BDirectAttackInput.Digest(packet);paths=new BWaveBoardingLocalPaths(Hash,hosts,(JArray)boarding["cars"]);
   }
   static void Need(bool ok,string reason){if(!ok)throw new ArgumentException(reason);}
   static BMmPoint Point(OrdinaryPartyAnchor p)=>new BMmPoint(p.X,p.Y,p.Z);
   BMmPoint Min(int slot)=>BMmPoint.Read(cars[slot]["roofMinMm"]);BMmPoint Max(int slot)=>BMmPoint.Read(cars[slot]["roofMaxMm"]);
   BMmPoint Origin(int slot)=>BMmPoint.Read(layout[(string)cars[slot]["authoringId"]]["originMm"]);
   static BMmPoint Add(BMmPoint a,BMmPoint b)=>new BMmPoint(checked(a.X+b.X),checked(a.Y+b.Y),checked(a.Z+b.Z));
   internal override BDirectAttackInput Input=>null;
   internal override string Hash{get;}
   internal override string DefinitionHash=>(string)packet["sourceDefinitionHash"];
   internal override string Version=>"unity.local-five-car-party-geometry.v1.20261109";
   internal override string SourceStatus=>Scope;
   internal override string OperatorMovementSource=>"USER_APPROVED_REVIEW_ONLY_2000_MM_PER_SECOND";
   internal override int Count=>6;
   internal override JObject Capture()=>(JObject)packet.DeepClone();
   internal override BMmPoint World(int slot,BMmPoint p)=>Add(Origin(slot),p);
   internal override BMmPoint Anchor(int slot,string name){if(name!="actorMm")throw new NotSupportedException("Review movement geometry supplies no combat emission or hit anchors");return World(slot,new BMmPoint(0,Min(slot).Y,0));}
   internal override BMmPoint EntityAnchor(string id,int slot,string name){if(name!="actorMm")throw new NotSupportedException("Use the original combat geometry for emission and hit anchors");var p=feet[id];return World(p.Slot,Point(p));}
   internal override bool ContainsLocalRoof(int slot,BMmPoint p)=>slot>=0&&slot<6&&p.Inside(Min(slot),Max(slot));
   internal override long OperatorSpeed(string id){Need(feet.ContainsKey(id),"Unknown local party actor");return ReviewSpeedMmPerSecond;}
   internal override BMmPoint[] GunnerPoints(int slot)=>((JArray)cars[slot]["gunnerAnchorsMm"]).Select(p=>World(slot,BMmPoint.Read(p))).ToArray();
   internal override BMmPoint ScoutGoal(int slot,BMmPoint target)=>ClampRoofPoint(slot,target);
   internal override BMmPoint[] WorldPath(int slot,int side,string phase)=>((JArray)cars[slot]["sockets"][side==0?"left":"right"][phase=="approachMm"?"approachPathMm":"climbPathMm"]).Select(p=>World(slot,BMmPoint.Read(p))).ToArray();
   internal override bool Connected(int a,int b)=>a>0&&a<6&&b>0&&b<6;
   internal override BMmPoint[] Route(BMmPoint from,int a,int b,BMmPoint target){
    if(!Connected(a,b))return null;var points=new List<BMmPoint>{from,new BMmPoint(from.X,from.Y,Origin(a).Z)};
    int step=Math.Sign(b-a);for(int slot=a;slot!=b;slot+=step)points.AddRange(bridges[(slot,slot+step)]);
    points.Add(new BMmPoint(target.X,target.Y,Origin(b).Z));points.Add(target);return points.ToArray();
   }
   internal override BMmPoint[] GroundRoute(BMmPoint from,int a,int b,int side){
    if(!Connected(a,b))return null;
    // Retreat along the original approach first, then follow its same-side ground points to the occupied car.
    var points=new List<BMmPoint>{from,WorldPath(a,side,"approachMm")[0]};
    int step=Math.Sign(b-a);for(int slot=a;slot!=b;slot+=step)points.Add(WorldPath(slot+step,side,"approachMm")[0]);
    return points.ToArray();
   }
   internal override int RoofSlot(BMmPoint point,int previous){for(int i=1;i<6;i++)if(point.Inside(World(i,Min(i)),World(i,Max(i))))return i;return previous;}
   internal override BMmPoint ClampRoofPoint(int slot,BMmPoint p){var min=World(slot,Min(slot));var max=World(slot,Max(slot));return new BMmPoint(Math.Max(min.X,Math.Min(max.X,p.X)),min.Y,Math.Max(min.Z,Math.Min(max.Z,p.Z)));}
   internal override bool Los(BMmPoint a,BMmPoint b){
    foreach(JObject entity in (JArray)definition["entities"])foreach(JObject shape in (JArray)entity["shapes"]){
     if((string)shape["role"]!="OCCLUDER")continue;Need((string)shape["kind"]=="BOX","Original box occluders required");
     var center=Add(BMmPoint.Read(layout[(string)entity["authoringId"]]["originMm"]),BMmPoint.Read(shape["centerMm"]));var size=BMmPoint.Read(shape["sizeMm"]);
     if(MiraSweepKernel.Box(new[]{a.X,a.Y,a.Z},new[]{b.X,b.Y,b.Z},new[]{center.X,center.Y,center.Z},new[]{center.X,center.Y,center.Z},new[]{size.X,size.Y,size.Z},0).HasValue)return false;
    }return true;
   }
   internal override BWaveBoardingLocalPaths Paths()=>paths;
   internal override JObject InstanceMetadata(int slot)=>new JObject{["scope"]=Scope,["physicalHostId"]=hosts[slot]};
  }
 }
}
