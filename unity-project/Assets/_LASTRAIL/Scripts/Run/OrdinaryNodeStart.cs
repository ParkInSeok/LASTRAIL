using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Owned by its parsed ARRIVE receipt. Runtime subcontracts are retained, not execution permission.
 public sealed class OrdinaryNodeStart {
  internal const long SafeMaximum=9007199254740991;
  readonly JObject original;
  public string AccountId=>(string)original["accountId"];
  public string RunId=>(string)original["runId"];
  public string NodeId=>(string)original["nodeId"];
  public long Generation=>(long)original["generation"];
  public long EntryMapRevision=>(long)original["entryMapRevision"];
  public string SnapshotHash=>(string)original["snapshotHash"];
  public OrdinaryNodeExecutionOrigin ExecutionOrigin {get;}
  public OrdinaryNodeEnemies Enemies {get;}
  public string BoardingSourceHash=>(string)original["snapshot"]["runtime"]["boardingSourceHash"];
  public OrdinaryRunAutomation Automation {get;}
  public OrdinaryObstaclePin Obstacle {get;private set;}
  public OrdinaryNodeGeometryBinding GeometryBinding {get;private set;}
  public bool CanStart=>false;
  public JObject Capture()=>(JObject)original.DeepClone();
  JObject Snapshot=>(JObject)original["snapshot"];
  internal bool MatchesSnapshot(JToken value)=>JToken.DeepEquals(Snapshot,value);
  OrdinaryNodeStart(JObject owned,FirstPlayableScaling scaling,JObject profile){original=(JObject)owned.DeepClone();var runtime=(JObject)Snapshot["runtime"];if((string)runtime["executionVersion"]==OrdinaryFiveCombatFactory.Version)ExecutionOrigin=OrdinaryNodeExecutionOrigin.Read(runtime);if((string)runtime["enemies"]["version"]==OrdinaryNodeEnemies.Version||(string)runtime["enemies"]["version"]==OrdinaryNodeEnemies.SpatialVersion)Enemies=OrdinaryNodeEnemies.ReadOwned((JObject)runtime["enemies"],scaling,profile);if(Snapshot.Property("automation")!=null){Automation=OrdinaryRunAutomation.Read(Snapshot["automation"]);Automation.RequireActors(((JArray)Snapshot["actors"]).Select(a=>(string)a["operatorId"]));Automation.RequireNodeSettings((JObject)runtime["actions"]);}}
  internal static void Need(bool value){if(!value)throw new ArgumentException("ordinary_node_start_conflict");}
  internal static long Revision(JObject value,string field)=>BDirectAttackInput.Nat(value,field,1,SafeMaximum);
  internal static OrdinaryNodeStart ReadArrival(JObject value,BRouteCommand request,JObject receipt,FirstPlayableScaling scaling=null){
   BDirectAttackInput.Fields(value,"version","accountId","runId","nodeId","generation","entryMapRevision","snapshot","snapshotHash");
   var route=request.Binding.Capture();var node=route["pendingNode"] as JObject;
   Need(!request.IsWave&&request.Kind=="ARRIVE"&&node!=null&&new[]{"COMBAT","ELITE","BOSS"}.Contains((string)node["eventKind"]));
   Need((string)value["version"]=="ordinary.node-start.v1.20261028"&&(string)value["accountId"]==request.Binding.AccountId&&(string)value["runId"]==request.Binding.RunId);
   Need(JToken.DeepEquals(value["nodeId"],node["nodeId"])&&Revision(value,"generation")==1&&Revision(value,"entryMapRevision")==Revision(receipt,"revision"));
   Need((string)receipt["state"]?["phase"]=="ACTIVE"&&receipt["state"]?["holdIds"] is JArray holds&&holds.Count==0);
   return ReadSnapshot(value,scaling,node["encounterDefinition"] as JObject,(string)route["startSnapshotHash"]);
  }
  // Server-owned replay input retains the entry snapshot; it does not invent an ARRIVE receipt.
  internal static OrdinaryNodeStart ReadTrusted(JObject value,OrdinaryFiveStartInput start,FirstPlayableScaling scaling){
   BDirectAttackInput.Fields(value,"version","accountId","runId","nodeId","generation","entryMapRevision","snapshot","snapshotHash");
   Need(start!=null&&(string)value["version"]=="ordinary.node-start.v1.20261028"&&(string)value["accountId"]==start.AccountId&&(string)value["runId"]==start.RunId&&Revision(value,"generation")==1);
   Revision(value,"entryMapRevision");SafeTransitionWire.Id(value,"nodeId");
   var profile=start.FirstBattle?.Definition;Need(profile!=null);
   var obstacle=value["snapshot"]?["runtime"]?["sequence"]?["obstacle"];
   if(obstacle!=null)profile["ordinaryObstacle"]=obstacle.DeepClone();
   var result=ReadSnapshot(value,scaling,profile,start.StartSnapshotHash);result.RequireOriginal(start);return result;
  }
  static OrdinaryNodeStart ReadSnapshot(JObject value,FirstPlayableScaling scaling,JObject profile,string startSnapshotHash){
   var snapshot=value["snapshot"] as JObject;
   BDirectAttackInput.Fields(snapshot,new[]{"version","accountId","runId","nodeId","entryMapRevision","startSnapshotHash","owners","actors","train","definitions","modifierSource","runtime"}.Concat(snapshot?.Property("automation")==null?Array.Empty<string>():new[]{"automation"}).ToArray());
   Need((string)snapshot["version"]=="ordinary.node-entry-state.v1.20261028"&&(string)value["snapshotHash"]==BDirectAttackInput.Digest(snapshot).Substring(7));
   foreach(var key in new[]{"accountId","runId","nodeId","entryMapRevision"})Need(JToken.DeepEquals(snapshot[key],value[key]));
   Need((string)snapshot["startSnapshotHash"]==startSnapshotHash);
   var owners=snapshot["owners"] as JObject;BDirectAttackInput.Fields(owners,"healthRevision","statRevision","inventoryRevision");
   foreach(var key in new[]{"healthRevision","statRevision","inventoryRevision"})Revision(owners,key);
   var actors=snapshot["actors"] as JArray;Need(actors!=null&&actors.Count==5);
   foreach(var token in actors){var actor=token as JObject;BDirectAttackInput.Fields(actor,"operatorId","slot","hp","maxHp","slots","effectiveStats");SafeTransitionWire.Id(actor,"operatorId");BDirectAttackInput.Nat(actor,"slot",0,4);Health(actor);
    Need(actor["slots"] is JArray slots&&slots.Count==3&&slots.All(s=>s.Type==JTokenType.Null||s is JObject)&&actor["effectiveStats"] is JObject);
    Need(JToken.DeepEquals(actor["effectiveStats"]["maxHp"],actor["maxHp"]));
   }
   Need(actors.Select(a=>(string)a["operatorId"]).Distinct().Count()==5&&actors.Select(a=>(long)a["slot"]).Distinct().Count()==5);
   var train=snapshot["train"] as JObject;BDirectAttackInput.Fields(train,"trainId","hp","maxHp");SafeTransitionWire.Id(train,"trainId");Health(train);
   var definitions=snapshot["definitions"] as JObject;BDirectAttackInput.Fields(definitions,new[]{"originHash","kitHash","trainHash","railHash","roofHash"}.Concat(definitions?.Property("relocationHash")==null?Array.Empty<string>():new[]{"relocationHash"}).ToArray());
   foreach(var key in new[]{"originHash","kitHash","trainHash","railHash","roofHash"})Need(!string.IsNullOrEmpty((string)definitions[key]));
   Need(snapshot["modifierSource"] is JObject);
   var runtime=snapshot["runtime"] as JObject;BDirectAttackInput.Fields(runtime,new[]{"executionVersion","effects","actions","spatial","enemies","determinism"}.Concat(runtime?.Property("sequence")==null?Array.Empty<string>():new[]{"sequence"}).Concat(runtime?.Property("boardingSourceHash")==null?Array.Empty<string>():new[]{"boardingSourceHash"}).ToArray());SafeTransitionWire.Id(runtime,"executionVersion");
   foreach(var key in new[]{"effects","actions","spatial","enemies","determinism"})Need(runtime[key] is JObject);
   if(runtime.Property("boardingSourceHash")!=null){var hash=(string)runtime["boardingSourceHash"];Need(runtime["boardingSourceHash"].Type==JTokenType.String&&hash.Length==64&&hash.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f'));}
   var result=new OrdinaryNodeStart(value,scaling,profile);
   result.Obstacle=OrdinaryObstaclePin.Read(runtime,profile);
   if(((JObject)runtime["spatial"]).Property("definitionBinding")!=null)result.GeometryBinding=new OrdinaryNodeGeometryBinding(runtime["spatial"]["definitionBinding"],value);
   return result;
  }
  static void Health(JObject value){long maximum=Revision(value,"maxHp");BDirectAttackInput.Nat(value,"hp",0,maximum);}
  public void RequireOriginal(OrdinaryFiveStartInput start){
   ValidateOriginal(start);
   Enemies?.RequireOriginal(start);
   Automation?.RequireOriginal(start);
   if(ExecutionOrigin!=null)OrdinaryNodeActions.Read((JObject)Snapshot["runtime"]["actions"],start);
  }
  void ValidateOriginal(OrdinaryFiveStartInput start){
   Need(start!=null&&AccountId==start.AccountId&&RunId==start.RunId&&(string)Snapshot["startSnapshotHash"]==start.StartSnapshotHash);
   Need(GeometryBinding==null?start.GeometryReference==null:GeometryBinding.Definition.Matches(start.GeometryReference));
   var d=Snapshot["definitions"];Need((string)d["originHash"]==start.OriginHash&&(string)d["kitHash"]==start.KitHash&&(string)d["trainHash"]==start.Train?.SourceHash&&(string)d["railHash"]==start.Rail?.SourceHash&&(string)d["roofHash"]==start.Roof?.SourceHash);
   if(((JObject)d).Property("relocationHash")!=null)Need(start.Relocation!=null&&(string)d["relocationHash"]==start.Relocation.RelocationHash);
   Need((string)Snapshot["train"]["trainId"]==start.Train?.TrainId);
   foreach(var actor in start.Actors)Need(((JArray)Snapshot["actors"]).Any(a=>(string)a["operatorId"]==actor.OperatorId&&(long)a["slot"]==actor.Slot));
  }
  public OrdinaryNodeActions ReadActions(OrdinaryFiveStartInput start){ValidateOriginal(start);Need(ExecutionOrigin!=null);return OrdinaryNodeActions.Read((JObject)Snapshot["runtime"]["actions"],start);}
 }
}
