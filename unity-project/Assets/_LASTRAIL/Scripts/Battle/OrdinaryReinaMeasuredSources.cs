using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Borrows current original-state/effect ports and registered scene geometry. Owns no HP or life ledger.
 internal sealed class OrdinaryReinaMeasuredSources {
  readonly OrdinaryFiveCombatFactory factory;readonly OrdinaryNodeSceneGeometry geometry;readonly Func<string,long,JObject> originalState;
  internal OrdinaryReinaBarrageSources Sources {get;}
  internal OrdinaryReinaMeasuredSources(OrdinaryFiveCombatFactory factory,OrdinaryNodeSceneGeometry geometry,Func<string,long,JObject> originalState,Func<string,long,JObject> captureAttack,Func<string,long,JObject> captureDefense,ICombatSkillLifestealAuthority lifesteal){
   this.factory=factory??throw new ArgumentNullException(nameof(factory));this.geometry=geometry??throw new ArgumentNullException(nameof(geometry));this.originalState=originalState??throw new ArgumentNullException(nameof(originalState));
   Need(factory.NodeInput?.Source.GeometryBinding!=null&&factory.Attempt!=null);
   Sources=new OrdinaryReinaBarrageSources(Observe,captureAttack,captureDefense,lifesteal);
  }
  static void Need(bool ok){if(!ok)throw new InvalidOperationException("Original-node measured Reina source differs or geometry is unsupported.");}
  static string Text(JToken t){Need(t?.Type==JTokenType.String&&!string.IsNullOrWhiteSpace((string)t));return (string)t;}
  static bool Flag(JToken t){Need(t?.Type==JTokenType.Boolean);return (bool)t;}
  static long Number(JToken t,long min=0){Need(t?.Type==JTokenType.Integer);long n=(long)t;Need(n>=min);return n;}
  static bool Present(JObject e)=>Flag(e["present"])&&Flag(e["active"]);
  static double[] Point(JToken p){Need(p is JArray a&&a.Count==3);return p.Select(v=>{Need(v.Type==JTokenType.Integer||v.Type==JTokenType.Float);double n=(double)v;Need(!double.IsNaN(n)&&!double.IsInfinity(n));return n;}).ToArray();}
  static double[][] Basis(JToken frame){Need(frame?["basis"] is JArray a&&a.Count==3);return frame["basis"].Select(Point).ToArray();}
  // shortcut: the execution kernel requires exact integer-mm projections and axis-aligned boxes.
  // Remove this restriction only when the original kernel accepts fractional projections/oriented solids;
  // native rigid-frame tolerance alone cannot authorize implicit rounding or rotated AABB expansion.
  static JArray Exact(double[] p){return new JArray(p.Select(n=>{Need(!double.IsNaN(n)&&!double.IsInfinity(n)&&Math.Abs(n)<=1000000000&&n==Math.Truncate(n));return (long)n;}));}
  static double[] World(JToken frame,JToken local){var p=Point(local);var b=Basis(frame);var origin=Point(frame["originMm"]);return Enumerable.Range(0,3).Select(i=>origin[i]+Enumerable.Range(0,3).Sum(j=>b[j][i]*p[j])).ToArray();}
  static double[] Local(JToken frame,double[] world){var b=Basis(frame);var origin=Point(frame["originMm"]);return Enumerable.Range(0,3).Select(i=>Enumerable.Range(0,3).Sum(j=>b[i][j]*(world[j]-origin[j]))).ToArray();}
  static JObject Entity(JArray rows,string id,string role){var matches=rows.OfType<JObject>().Where(e=>(string)e["id"]==id).ToArray();Need(matches.Length==1&&(string)matches[0]["role"]==role);Number(matches[0]["generation"],1);return matches[0];}
  static JToken Anchor(JObject e,string role,string ability=null){var a=((JArray)e["anchors"]).Where(x=>(string)x["role"]==role&&(ability==null||(string)x["abilityId"]==ability)).ToArray();Need(a.Length==1);return a[0]["positionMm"];}
  static JArray Solids(JArray rows,JToken frame){
   var solids=new JArray();var targetBasis=Basis(frame);
   foreach(var e in rows.OfType<JObject>().Where(Present))foreach(var shape in (JArray)e["shapes"]){
    if((string)shape["role"]!="OCCLUDER"||!Flag(shape["active"]))continue;
    Need((string)shape["kind"]=="BOX");var sourceBasis=Basis(e["frame"]);var sourceSize=Point(shape["sizeMm"]);Need(sourceSize.All(n=>n>0));var size=new double[3];var occupied=new bool[3];
    for(int axis=0;axis<3;axis++){
     var direction=Enumerable.Range(0,3).Select(i=>Enumerable.Range(0,3).Sum(j=>targetBasis[i][j]*sourceBasis[axis][j])).ToArray();
     int index=Array.FindIndex(direction,n=>Math.Abs(n)==1);Need(index>=0&&!occupied[index]&&direction.Where((n,i)=>i!=index).All(n=>n==0));occupied[index]=true;size[index]=sourceSize[axis];
    }
    solids.Add(new JObject{["id"]=Text(e["id"])+":"+Text(shape["id"]),["centerMm"]=Exact(Local(frame,World(e["frame"],shape["centerMm"]))),["sizeMm"]=Exact(size)});
   }
   return solids;
  }
  static bool Clear(JArray solids,JArray from,JArray to)=>!solids.Any(s=>MiraSweepKernel.Box(from.Select(x=>(long)x).ToArray(),to.Select(x=>(long)x).ToArray(),s["centerMm"].Select(x=>(long)x).ToArray(),s["centerMm"].Select(x=>(long)x).ToArray(),s["sizeMm"].Select(x=>(long)x).ToArray(),0).HasValue);
  JObject Observe(string command,long tick){
   var supplied=originalState(command,tick);Need(supplied!=null);var state=(JObject)supplied.DeepClone();var a=factory.Attempt;
   Need(Number(state["tick"])==tick&&Text(state["nodeId"])==a.NodeId&&Text(state["snapshotHash"])==a.SnapshotHash&&Number(state["attemptGeneration"],1)==a.Generation);Number(state["revision"]);
   var measured=geometry.Capture(tick);
   Need((string)measured["version"]=="ordinary.node-scene-observation.v1.20261102"&&Text(measured["accountId"])==a.AccountId&&Text(measured["runId"])==a.RunId&&Text(measured["nodeId"])==a.NodeId&&Text(measured["snapshotHash"])==a.SnapshotHash&&Number(measured["attemptGeneration"],1)==a.Generation&&Number(measured["attemptRevision"])==a.Revision&&Number(measured["tick"])==tick&&Text(measured["definitionHash"])==factory.NodeInput.Source.GeometryBinding.Definition.DefinitionHash);Number(measured["revision"],1);
   var entities=measured["entities"] as JArray;Need(entities!=null);
   var originalSource=state["source"] as JObject;Need(originalSource!=null&&Text(originalSource["id"])=="op_reina"&&Text(originalSource["abilityId"])=="op_reina.ultimate");Text(originalSource["executorId"]);
   var actor=Entity(entities,"op_reina","OPERATOR");var frame=Entity(entities,Text(state["frame"]?["id"]),"TRAIN");var physical=frame["frame"];
   var muzzleWorld=Exact(World(actor["frame"],Anchor(actor,"EMISSION","op_reina.ultimate")));var muzzleLocal=Exact(Local(physical,Point(muzzleWorld)));var solids=Solids(entities,physical);
   var source=(JObject)originalSource.DeepClone();source["generation"]=actor["generation"].DeepClone();source["actionEligible"]=Flag(originalSource["actionEligible"])&&Present(actor);source["muzzleMm"]=muzzleWorld;
   Need(state["targets"] is JArray targets&&targets.Count<=10000);var output=new JArray();var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
   foreach(JObject input in (JArray)state["targets"]){
    string id=Text(input["id"]);Need(ids.Add(id));var entity=Entity(entities,id,"ENEMY");Number(input["battleOrder"]);Number(input["carDelta"],-1000000);Flag(input["protected"]);
    var world=Exact(World(entity["frame"],Anchor(entity,"HIT")));var local=Exact(Local(physical,Point(world)));var row=(JObject)input.DeepClone();
    row["generation"]=entity["generation"].DeepClone();row["eligible"]=Flag(input["eligible"])&&Present(entity);row["frameId"]=frame["id"].DeepClone();row["worldAnchorMm"]=world;row["localAnchorMm"]=local;row["castLineOfSight"]=Present(actor)&&Present(entity)&&Present(frame)&&Clear(solids,muzzleLocal,local);output.Add(row);
   }
   state["source"]=source;state["frame"]=new JObject{["id"]=frame["id"].DeepClone(),["generation"]=frame["generation"].DeepClone(),["alive"]=Present(frame),["originMm"]=physical["originMm"].DeepClone(),["basis"]=physical["basis"].DeepClone()};state["targets"]=output;state["solidOccluders"]=solids;return state;
  }
 }
}
