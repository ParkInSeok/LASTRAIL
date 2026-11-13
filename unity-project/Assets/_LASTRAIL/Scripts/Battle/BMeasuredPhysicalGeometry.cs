using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Immutable authored draft, not execution admission or an account placement.
 // First consumer supports axis-aligned boxes and identity-linear mm transforms.
 public sealed class BMeasuredPhysicalGeometry {
  public const string Version="unity.b-measured-physical-draft.v1.20261020";
  static readonly string[] RoofNames={"RoofRearLeft","RoofFrontLeft","RoofFrontRight","RoofRearRight"};
  static readonly string[] Names=RoofNames.Concat(new[]{"Foot","Fire","Hit","LeftApproachStart","LeftApproachEnd","LeftClimbStart","LeftClimbEnd","RightApproachStart","RightApproachEnd","RightClimbStart","RightClimbEnd","CouplerRear","CouplerFront"}).ToArray();
  readonly JObject original,definition;
  readonly Dictionary<int,JObject> bindings=new Dictionary<int,JObject>(),packets=new Dictionary<int,JObject>();
  readonly Dictionary<(int,int),JArray> bridges=new Dictionary<(int,int),JArray>();
  public string Hash{get;}
  public string DefinitionHash{get;}
  public IReadOnlyList<int> OccupiedSlots=>Array.AsReadOnly(bindings.Keys.OrderBy(x=>x).ToArray());
  public JObject Capture()=>(JObject)original.DeepClone();
  static void Need(bool yes,string reason){if(!yes)throw new ArgumentException("b_measured_geometry_"+reason);}
  static void Fields(JObject p,params string[] names)=>BDirectAttackInput.Fields(p,names);
  static long N(JToken p,string key,long min=0,long max=9007199254740991)=>BDirectAttackInput.Nat(p,key,min,max);
  static string Text(JToken p,string key)=>BDirectAttackInput.Text(p,key,200);
  static long[] Point(JToken p){
   Need(p is JArray a&&a.Count==3,"point");var result=new long[3];
   for(int i=0;i<3;i++){Need(p[i].Type==JTokenType.Integer,"point_integer");result[i]=(long)p[i];Need(result[i]>=-1000000000&&result[i]<=1000000000,"point_range");}
   return result;
  }
  static JObject Obj(JToken p)=>p as JObject??throw new ArgumentException("b_measured_geometry_object");
  public static BMeasuredPhysicalGeometry ReadAuthored(JObject definition,string definitionHash,JObject measurements,string measurementsHash)=>new BMeasuredPhysicalGeometry(definition,definitionHash,measurements,measurementsHash);
  BMeasuredPhysicalGeometry(JObject d,string dh,JObject m,string mh){
   Need(d!=null&&m!=null&&BDirectAttackInput.Digest(d)==dh&&BDirectAttackInput.Digest(m)==mh,"independent_pin");
   d=(JObject)d.DeepClone();m=(JObject)m.DeepClone();definition=d;DefinitionHash=dh;
   Fields(d,"version","status","unitContractId","millimetersPerUnit","axis","dimensionOrigin","carLengthMm","carWidthMm","carBodyHeightMm","gapMm","bindings","connections","railStartPath","railEndPath","railSpeed","stepTicks","runtimeNodeBoundaries","missingRuntimeBindings");
   Need((string)d["version"]=="b.physical-authoring.v1.20261020"&&(string)d["status"]=="AUTHORED_DRAFT_NOT_RUNTIME_ADMITTED"
    &&(string)d["axis"]=="X_FORWARD_Y_UP_Z_SIDE"&&(string)d["dimensionOrigin"]=="NEW_AUTHORING_BASELINE_NOT_HISTORICAL_APPROVAL_OR_ART_MEASUREMENT"
    &&N(d,"millimetersPerUnit",1)==1000,"draft_contract");
   Text(d,"unitContractId");Text(d,"railStartPath");Text(d,"railEndPath");
   foreach(var k in new[]{"carLengthMm","carWidthMm","carBodyHeightMm","gapMm"})N(d,k,1,1000000000);
   foreach(var k in new[]{"railSpeed","stepTicks","runtimeNodeBoundaries"})Need(d[k].Type==JTokenType.Null,"unsupported_runtime_definition");
   Need(d["missingRuntimeBindings"] is JArray,"missing_bindings");
   Fields(m,"version","instances");Need((string)m["version"]=="b.physical-authoring-export.v1.20261020","export_version");
   Need(d["bindings"] is JArray br&&br.Count==5&&m["instances"] is JArray mr&&mr.Count==5,"template_count");
   foreach(var row in (JArray)d["bindings"]){
    var b=Obj(row);Fields(b,"templateSlot","framePath","colliderPath","unitContractId","millimetersPerUnit","anchorIds","frameToNative","transformTranslationUnit","runtimeCarriageInstanceId");
    int slot=(int)N(b,"templateSlot",0,4);Need(!bindings.ContainsKey(slot),"duplicate_template");Text(b,"framePath");Text(b,"colliderPath");
    Need((string)b["unitContractId"]==(string)d["unitContractId"]&&N(b,"millimetersPerUnit",1)==1000
     &&(string)b["transformTranslationUnit"]=="MILLIMETERS"&&b["runtimeCarriageInstanceId"].Type==JTokenType.Null,"binding_units_or_instance");
    Need(b["anchorIds"] is JArray ids&&ids.Count==Names.Length&&ids.All(x=>x.Type==JTokenType.String)
     &&((JArray)b["anchorIds"]).Select(x=>(string)x).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(Names.OrderBy(x=>x,StringComparer.Ordinal)),"anchor_contract");
    var matrix=b["frameToNative"] as JArray;Need(matrix!=null&&matrix.Count==16,"matrix");
    for(int i=0;i<16;i++){
     Need(matrix[i].Type==JTokenType.Integer,"matrix_integer");
     long value=(long)matrix[i];
     if(i==3||i==7||i==11)Need(value>=-1000000000&&value<=1000000000,"translation_range");
     else Need(value==(i==0||i==5||i==10||i==15?1:0),"unsupported_linear_transform");
    }
    bindings.Add(slot,b);
   }
   foreach(var row in (JArray)m["instances"]){
    var item=Obj(row);Fields(item,"templateSlot","measurement");int slot=(int)N(item,"templateSlot",0,4);
    Need(bindings.ContainsKey(slot)&&!packets.ContainsKey(slot),"measurement_template");
    var export=Obj(item["measurement"]);Fields(export,"packet","geometryCanonicalJson","packetCanonicalJson");
    var p=Obj(export["packet"]);Fields(p,"version","sourceId","provenance","unitContractId","millimetersPerUnit","coordinateSpace","quantization","bodyCornersMm","anchors","geometryHash","sourceRevision","packetHash");
    string geometryBytes=(string)export["geometryCanonicalJson"],packetBytes=(string)export["packetCanonicalJson"];
    Need(geometryBytes!=null&&packetBytes!=null&&geometryBytes.Length<=131072&&packetBytes.Length<=131072,"canonical_size");
    Need("sha256:"+BasicCombatSimulation.Digest(geometryBytes)==(string)p["geometryHash"]
     &&"sha256:"+BasicCombatSimulation.Digest(packetBytes)==(string)p["packetHash"],"canonical_hash");
    var body=(JObject)p.DeepClone();body.Remove("packetHash");
    Need(JToken.DeepEquals(JObject.Parse(packetBytes),body),"packet_bytes");
    body.Remove("geometryHash");body.Remove("sourceRevision");
    Need(JToken.DeepEquals(JObject.Parse(geometryBytes),body),"geometry_bytes");
    Need((string)p["version"]==BPhysicalSceneSource.Version&&(string)p["provenance"]=="UNITY_SCENE_MEASUREMENT_NOT_EXECUTION_AUTHORITY"
     &&(string)p["coordinateSpace"]=="BOUND_FRAME_LOCAL"&&(string)p["quantization"]=="NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO"
     &&(string)p["unitContractId"]==(string)d["unitContractId"]&&(double)p["millimetersPerUnit"]==1000,"measurement_contract");
    Text(p,"sourceId");N(p,"sourceRevision",1);
    var corners=p["bodyCornersMm"] as JArray;Need(corners!=null&&corners.Count==8,"box_corners");var values=corners.Select(Point).ToArray();
    var axes=Enumerable.Range(0,3).Select(i=>values.Select(v=>v[i]).Distinct().OrderBy(x=>x).ToArray()).ToArray();
    Need(axes.All(a=>a.Length==2)&&values.Select(v=>string.Join(",",v)).Distinct().Count()==8,"unsupported_body_shape");
    Need(axes[0][1]-axes[0][0]==(long)d["carLengthMm"]&&axes[1][1]-axes[1][0]==(long)d["carBodyHeightMm"]&&axes[2][1]-axes[2][0]==(long)d["carWidthMm"],"body_dimensions");
    var anchors=p["anchors"] as JArray;Need(anchors!=null&&anchors.Count==Names.Length,"anchors");
    string previous=null;foreach(var token in anchors){var a=Obj(token);Fields(a,"id","positionMm");string id=Text(a,"id");Need(Names.Contains(id)&&(previous==null||StringComparer.Ordinal.Compare(previous,id)<0),"anchor_order");Point(a["positionMm"]);previous=id;}
    packets.Add(slot,p);
    foreach(var a in anchors)WorldAnchor(slot,(string)a["id"]);WorldBodyCorners(slot);
    var roof=WorldRoof(slot).Select(Point).ToArray();
    Need(roof.Select(a=>a[1]).Distinct().Count()==1&&roof[0][0]==roof[3][0]&&roof[1][0]==roof[2][0]&&roof[0][2]==roof[1][2]&&roof[2][2]==roof[3][2]
     &&roof[0][0]<roof[1][0]&&roof[0][2]<roof[3][2],"unsupported_roof");
    foreach(string side in new[]{"left","right"})Need(JToken.DeepEquals(WorldSocketPath(slot,side,"approach").Last,WorldSocketPath(slot,side,"climb").First),"socket_gap");
   }
   var links=d["connections"] as JArray;Need(links!=null&&links.Count<=10,"connections");
   foreach(var token in links){
    var c=Obj(token);Fields(c,"fromTemplateSlot","toTemplateSlot","fromAnchorId","toAnchorId");
    int a=(int)N(c,"fromTemplateSlot",0,4),b=(int)N(c,"toTemplateSlot",0,4);
    Need(a!=b&&!bridges.ContainsKey((a,b)),"duplicate_connection");
    Need((string)c["fromAnchorId"]=="CouplerFront"&&(string)c["toAnchorId"]=="CouplerRear","coupler_contract");
    var path=new JArray(WorldAnchor(a,(string)c["fromAnchorId"]),WorldAnchor(b,(string)c["toAnchorId"]));
    bridges.Add((a,b),path);bridges.Add((b,a),new JArray(path.Reverse().Select(x=>x.DeepClone())));
   }
   original=new JObject{["version"]=Version,["definitionHash"]=dh,["measurementsHash"]=mh,["definition"]=d,["measurements"]=m};
   Hash=BDirectAttackInput.Digest(original);
  }
  void Slot(int slot){if(!packets.ContainsKey(slot))throw new ArgumentException("b_measured_geometry_template_missing");}
  JArray World(int slot,JToken point){
   Slot(slot);var p=Point(point);var t=bindings[slot]["frameToNative"];
   for(int i=0;i<3;i++){p[i]=checked(p[i]+(long)t[i*4+3]);Need(p[i]>=-1000000000&&p[i]<=1000000000,"native_coordinate_range");}
   return new JArray(p);
  }
  public JArray WorldAnchor(int templateSlot,string id){
   Slot(templateSlot);var point=((JArray)packets[templateSlot]["anchors"]).SingleOrDefault(a=>(string)a["id"]==id);
   if(point==null)throw new ArgumentException("b_measured_geometry_anchor_missing");return World(templateSlot,point["positionMm"]);
  }
  public JArray WorldPoint(int templateSlot,JArray localMm)=>World(templateSlot,localMm);
  public JArray WorldBodyCorners(int templateSlot){Slot(templateSlot);return new JArray(((JArray)packets[templateSlot]["bodyCornersMm"]).Select(p=>World(templateSlot,p)));}
  public JArray WorldRoof(int templateSlot)=>new JArray(RoofNames.Select(id=>WorldAnchor(templateSlot,id)));
  public JArray WorldSocketPath(int templateSlot,string side,string phase){
   Need((side=="left"||side=="right")&&(phase=="approach"||phase=="climb"),"socket_kind");
   string prefix=(side=="left"?"Left":"Right")+(phase=="approach"?"Approach":"Climb");
   return new JArray(WorldAnchor(templateSlot,prefix+"Start"),WorldAnchor(templateSlot,prefix+"End"));
  }
  public bool HasDirectBridge(int a,int b){Slot(a);Slot(b);return bridges.ContainsKey((a,b));}
  public JArray Bridge(int a,int b){Slot(a);Slot(b);if(!bridges.TryGetValue((a,b),out var path))throw new ArgumentException("b_measured_geometry_direct_bridge_missing");return (JArray)path.DeepClone();}
  public bool Connected(int a,int b){
   Slot(a);Slot(b);var seen=new HashSet<int>{a};var queue=new Queue<int>();queue.Enqueue(a);
   while(queue.Count>0){int current=queue.Dequeue();if(current==b)return true;foreach(var key in bridges.Keys)if(key.Item1==current&&seen.Add(key.Item2))queue.Enqueue(key.Item2);}return false;
  }
  public JObject BindPlacement(BCombatPhysicalPlacement placement,string contextId){
   if(placement==null)throw new ArgumentNullException(nameof(placement));Text(new JObject{["contextId"]=contextId},"contextId");
   var instances=new JArray();var occupied=new HashSet<int>();
   foreach(var car in placement.Slots.Where(c=>c!=null)){
    int slot=car.PhysicalSlot;Slot(slot);occupied.Add(slot);var p=packets[slot];
    instances.Add(new JObject{["physicalSlot"]=slot,["templateSlot"]=slot,["carriageInstanceId"]=car.InstanceId,["carriageDefinitionId"]=car.DefinitionId,
     ["operatorId"]=car.OperatorId==null?JValue.CreateNull():new JValue(car.OperatorId),["actorPhysicalRevision"]=car.ActorPhysicalRevision.HasValue?new JValue(car.ActorPhysicalRevision.Value):JValue.CreateNull(),
     ["instanceGeneration"]=JValue.CreateNull(),["templateSourceId"]=p["sourceId"].DeepClone(),["measurementPacketHash"]=p["packetHash"].DeepClone(),
     ["measurementGeometryHash"]=p["geometryHash"].DeepClone(),["measurementSourceRevision"]=p["sourceRevision"].DeepClone(),["binding"]=bindings[slot].DeepClone()});
   }
   var connections=new JArray(((JArray)definition["connections"]).Where(c=>occupied.Contains((int)c["fromTemplateSlot"])&&occupied.Contains((int)c["toTemplateSlot"])).Select(c=>c.DeepClone()));
   return new JObject{["version"]="unity.b-measured-placement-draft.v1.20261020",["contextId"]=contextId,["geometryHash"]=Hash,["definitionHash"]=DefinitionHash,
    ["placementHash"]=BDirectAttackInput.Digest(placement.Capture()),["assignmentPacketHash"]=placement.AssignmentPacketHash,
    ["instances"]=instances,["connections"]=connections};
  }
 }
}
