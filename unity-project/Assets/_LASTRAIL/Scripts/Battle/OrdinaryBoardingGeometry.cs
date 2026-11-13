using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Initial-world millimetres are shared with the existing spatial owner; the live view adds current rail translation.
 internal sealed class OrdinaryBoardingGeometry:BOrdinarySpatialGeometry {
  readonly JObject definition,layout,packet;readonly JObject[] cars;readonly Dictionary<string,JObject> templates,entities;
  readonly BBoardingRoster roster;readonly BWaveBoardingLocalPaths paths;
  readonly LocalFiveCarPartyGeometry.Geometry localParty;
  internal OrdinaryBoardingGeometry(OrdinaryFiveCombatFactory factory,BBoardingRoster roster){
   this.roster=roster;definition=OrdinaryLiveCombatSources.ReadDefinition(factory.LiveDefinitionJson,factory.NodeInput.Source.GeometryBinding.Definition.DefinitionHash);layout=(JObject)definition["source"]["liveLayout"];
   packet=definition["source"]["boardingGeometry"] as JObject;BDirectAttackInput.Fields(packet,"version","scope","coordinateSpace","units","cars","bridges","enemyTemplates");
   Need((string)packet["version"]=="ordinary.authored-boarding-geometry.v1.20261107"&&(string)packet["scope"]=="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION"&&(string)packet["coordinateSpace"]=="CARRIAGE_FRAME_LOCAL"&&(string)packet["units"]=="MILLIMETERS");
   Need(packet["cars"] is JArray c&&c.Count==roster.CarriageIds.Count&&packet["bridges"] is JArray bridges&&bridges.Count==0);
   cars=((JArray)packet["cars"]).Cast<JObject>().OrderBy(c=>(int)c["carOrder"]).ToArray();
   templates=((JArray)packet["enemyTemplates"]).Cast<JObject>().ToDictionary(t=>(string)t["definitionRef"],StringComparer.Ordinal);
   Need(templates.Count==2&&templates.ContainsKey("enemy_raider_scout")&&templates.ContainsKey("enemy_spike_cart_gunner"));
   entities=((JArray)definition["entities"]).Cast<JObject>().ToDictionary(e=>(string)e["authoringId"],StringComparer.Ordinal);
   for(int i=0;i<cars.Length;i++){
    var car=cars[i];Need((int?)car["carOrder"]==i&&(string)car["authoringId"]==(factory.Original.Rail.LocalFiveCar?(i==0?"Engine":"Car"+(i-1)):new[]{"Engine","Combat","Gun"}[i]));var min=BMmPoint.Read(car["roofMinMm"]);var max=BMmPoint.Read(car["roofMaxMm"]);Need(min.X<max.X&&min.Y==max.Y&&min.Z<max.Z);
    foreach(string side in new[]{"left","right"}){var socket=car["sockets"]?[side] as JObject;Need(socket!=null&&!string.IsNullOrWhiteSpace((string)socket["socketId"]));foreach(string key in new[]{"approachPathMm","climbPathMm"}){var points=BuffWire.Array(socket[key],2,64);foreach(var p in points)BMmPoint.Read(p);}Need(JToken.DeepEquals(socket["approachPathMm"].Last,socket["climbPathMm"].First)&&BMmPoint.Read(socket["climbPathMm"].Last).Inside(min,max));}
    foreach(var p in BuffWire.Array(car["gunnerAnchorsMm"],1,16))Need(BMmPoint.Read(p).Inside(min,max));
   }
   foreach(var t in templates.Values){foreach(string role in new[]{"FOOT","EMISSION","HIT"})LocalAnchor(t,role);var capsule=Capsule(t);BMmPoint.Read(capsule["centerMm"]);Need((long?)capsule["radiusMm"]>0&&(long?)capsule["heightMm"]>=2*(long)capsule["radiusMm"]);}
   Hash=BasicCombatSimulation.Digest(factory.LiveDefinitionJson);paths=new BWaveBoardingLocalPaths(Hash,roster.CarriageIds,(JArray)packet["cars"]);
   if(factory.Original.Rail.LocalFiveCar){
    var initial=roster.Entities.Values.Where(e=>e.Kind=="OPERATOR").ToDictionary(e=>e.Id,e=>{var foot=EntityAnchor(e.Id,e.Slot,"actorMm");var origin=Origin(e.Slot);return new OrdinaryPartyAnchor(e.Slot,foot.X-origin.X,foot.Y-origin.Y,foot.Z-origin.Z);});
    localParty=new LocalFiveCarPartyGeometry.Geometry(factory.LiveDefinitionJson,Hash,roster.CarriageIds,initial);
   }
  }
  static void Need(bool ok){if(!ok)throw new ArgumentException("ordinary_boarding_geometry_conflict");}
  static BMmPoint Add(BMmPoint a,BMmPoint b)=>new BMmPoint(checked(a.X+b.X),checked(a.Y+b.Y),checked(a.Z+b.Z));
  static BMmPoint LocalAnchor(JObject template,string role){var rows=((JArray)template["anchors"]).Where(a=>(string)a["role"]==role).ToArray();Need(rows.Length==1);return BMmPoint.Read(rows[0]["positionMm"]);}
  static JObject Capsule(JObject template){var rows=((JArray)template["shapes"]).OfType<JObject>().Where(s=>(string)s["role"]=="HIT_VOLUME"&&(string)s["kind"]=="CAPSULE_Y").ToArray();Need(rows.Length==1);return rows[0];}
  internal JObject Template(string id)=>templates[roster.Entities[id].Definition];
  internal BMmPoint LocalPoint(string id,string role)=>LocalAnchor(Template(id),role);
  internal JObject HitCapsule(string id)=>Capsule(Template(id));
  BMmPoint Origin(int slot)=>BMmPoint.Read(layout[(string)cars[slot]["authoringId"]]["originMm"]);
  internal override BDirectAttackInput Input=>null;
  internal override string Hash{get;}
  internal override string DefinitionHash=>Hash;
  internal override string Version=>(string)packet["version"];
  internal override string SourceStatus=>(string)packet["scope"];
  internal override string OperatorMovementSource=>localParty?.OperatorMovementSource??"ORIGINAL_STATIC_ROOF_FOOT_UNTIL_OWNED_RELOCATION";
  internal override int Count=>cars.Length;
  internal override JObject Capture()=>new JObject{["geometryHash"]=Hash,["physicalHostIds"]=new JArray(roster.CarriageIds)};
  internal override BMmPoint World(int slot,BMmPoint point)=>Add(Origin(slot),point);
  internal override BMmPoint Anchor(int slot,string name)=>World(slot,new BMmPoint(0,(long)cars[slot]["roofMinMm"][1],0));
  internal override BMmPoint EntityAnchor(string id,int slot,string name){
   string role=name=="actorMm"?"FOOT":name=="fireMm"?"EMISSION":name=="hitMm"?"HIT":throw new ArgumentException("Unknown boarding anchor.");
   if(roster.Entities[id].Kind=="OPERATOR")return Add(BMmPoint.Read(layout[id]["originMm"]),LocalAnchor(entities[id],role));
   return Add(Anchor(slot,"actorMm"),LocalPoint(id,role));
  }
  internal override bool ContainsLocalRoof(int slot,BMmPoint p)=>p.Inside(BMmPoint.Read(cars[slot]["roofMinMm"]),BMmPoint.Read(cars[slot]["roofMaxMm"]));
  internal override long OperatorSpeed(string id)=>localParty!=null?localParty.OperatorSpeed(id):throw new NotSupportedException("Boarding geometry does not supply an operator relocation command.");
  internal override BMmPoint[] GunnerPoints(int slot)=>((JArray)cars[slot]["gunnerAnchorsMm"]).Select(p=>World(slot,BMmPoint.Read(p))).ToArray();
  internal override BMmPoint ScoutGoal(int slot,BMmPoint target)=>ClampRoofPoint(slot,target);
  internal override BMmPoint[] WorldPath(int slot,int side,string phase)=>((JArray)cars[slot]["sockets"][side==0?"left":"right"][phase=="approachMm"?"approachPathMm":"climbPathMm"]).Select(p=>World(slot,BMmPoint.Read(p))).ToArray();
  internal override bool Connected(int a,int b)=>localParty!=null?localParty.Connected(a,b):a>=0&&a<Count&&a==b;
  internal override BMmPoint[] Route(BMmPoint from,int a,int b,BMmPoint target)=>localParty!=null?localParty.Route(from,a,b,target):Connected(a,b)?new[]{from,target}:null;
  internal override BMmPoint[] GroundRoute(BMmPoint from,int a,int b,int side)=>localParty!=null?localParty.GroundRoute(from,a,b,side):a>=0&&a<Count&&a==b?new[]{from,WorldPath(b,side,"approachMm")[0]}:null;
  internal override int RoofSlot(BMmPoint p,int previous){for(int i=0;i<Count;i++)if(p.Inside(World(i,BMmPoint.Read(cars[i]["roofMinMm"])),World(i,BMmPoint.Read(cars[i]["roofMaxMm"]))))return i;return previous;}
  internal override BMmPoint ClampRoofPoint(int slot,BMmPoint p){var min=World(slot,BMmPoint.Read(cars[slot]["roofMinMm"]));var max=World(slot,BMmPoint.Read(cars[slot]["roofMaxMm"]));return new BMmPoint(Math.Max(min.X,Math.Min(max.X,p.X)),min.Y,Math.Max(min.Z,Math.Min(max.Z,p.Z)));}
  internal override bool Los(BMmPoint a,BMmPoint b){foreach(var e in entities.Values)foreach(JObject shape in (JArray)e["shapes"]){if((string)shape["role"]!="OCCLUDER")continue;Need((string)shape["kind"]=="BOX");var center=Add(BMmPoint.Read(layout[(string)e["authoringId"]]["originMm"]),BMmPoint.Read(shape["centerMm"]));var size=BMmPoint.Read(shape["sizeMm"]);if(MiraSweepKernel.Box(new[]{a.X,a.Y,a.Z},new[]{b.X,b.Y,b.Z},new[]{center.X,center.Y,center.Z},new[]{center.X,center.Y,center.Z},new[]{size.X,size.Y,size.Z},0).HasValue)return false;}return true;}
  internal override BWaveBoardingLocalPaths Paths()=>paths;
  internal override JObject InstanceMetadata(int slot)=>new JObject{["instanceGeneration"]=1,["physicalHostId"]=roster.CarriageIds[slot]};
 }
}
