using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle {
 // Borrows registered native references and the original attempt. No HP, motion clock or asset approval.
 // The asset owner verifies loaded asset bytes; this adapter verifies the retained definition and actual geometry.
 public sealed class OrdinaryNodeSceneGeometry:IDisposable {
  sealed class Entity {
   internal string Id;internal JObject Definition,LastMeasured;internal Transform Frame;internal Dictionary<string,Transform> Anchors;internal Dictionary<string,Collider> Shapes;internal Func<long> Generation;
  }
  readonly OrdinaryFiveStartInput original;readonly OrdinaryNodeStart node;readonly OrdinaryNodeAttempt attempt;
  readonly Transform basis;readonly Matrix4x4 originalBasis;readonly Func<bool> current;readonly JObject definition;readonly double units;
  readonly Dictionary<string,Entity> entities=new Dictionary<string,Entity>(StringComparer.Ordinal);
  JObject retained;long lastTick=-1,revision;bool disposed;
  public OrdinaryNodeSceneGeometry(OrdinaryFiveStartInput original,OrdinaryNodeStart node,OrdinaryNodeAttempt attempt,string definitionJson,Transform basis,Func<bool> currentAttempt){
   this.original=original??throw new ArgumentNullException(nameof(original));this.node=node??throw new ArgumentNullException(nameof(node));this.attempt=attempt??throw new ArgumentNullException(nameof(attempt));
   node.RequireOriginal(original);attempt.RequireNode(node);this.basis=basis?basis:throw new ArgumentNullException(nameof(basis));current=currentAttempt??throw new ArgumentNullException(nameof(currentAttempt));
   originalBasis=basis.localToWorldMatrix;
   var pin=node.GeometryBinding?.Definition;if(pin==null||definitionJson==null||definitionJson.Length>524288||BasicCombatSimulation.Digest(definitionJson)!=pin.DefinitionHash)throw new ArgumentException("Original node geometry definition bytes required.");
   definition=JObject.Parse(definitionJson);BDirectAttackInput.Fields(definition,"version","definitionId","units","millimetersPerUnit","axis","coordinateSpace","source","entities");
   Require((string)definition["version"]==OrdinaryGeometryReference.Version&&(string)definition["definitionId"]==pin.DefinitionId&&(string)definition["units"]=="MILLIMETERS"&&(string)definition["axis"]=="X_FORWARD_Y_UP_Z_SIDE"&&(string)definition["coordinateSpace"]=="BOUND_FRAME_LOCAL");
   units=(double)definition["millimetersPerUnit"];Require(!double.IsNaN(units)&&!double.IsInfinity(units)&&units>0);
   Require(definition["entities"] is JArray all&&all.Count>0&&all.Count<=128);var ids=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject e in (JArray)definition["entities"]){BDirectAttackInput.Fields(e,"authoringId","role","definitionRef","framePath","assetPath","assetHash","anchors","shapes");Require(ids.Add(Text(e["authoringId"])));Require(e["anchors"] is JArray a&&a.Count<=128&&e["shapes"] is JArray s&&s.Count<=128&&a.Count+s.Count>0);}
  }
  static void Require(bool ok){if(!ok)throw new ArgumentException("Original registered node geometry differs.");}
  static string Text(JToken t){Require(t?.Type==JTokenType.String&&!string.IsNullOrWhiteSpace((string)t));return (string)t;}
  void Active(){if(disposed)throw new ObjectDisposedException(nameof(OrdinaryNodeSceneGeometry));if(!current()||!basis)throw new InvalidOperationException("Original node geometry owner is no longer current.");if(!basis.localToWorldMatrix.Equals(originalBasis))throw new InvalidOperationException("Native coordinate basis changed; a new explicit context is required.");}
  bool IdentityMatches(string id,JObject e){
   switch((string)e["role"]){
    case "OPERATOR":return original.Actors.Any(a=>a.OperatorId==id)&&id==(string)e["definitionRef"];
    case "ENEMY":return node.Enemies?.Gunners.Any(g=>g.InstanceId==id)==true&&(string)e["definitionRef"]=="enemy_spike_cart_gunner";
    case "TRAIN":return id==original.Train.TrainId||original.Rail.PhysicalInstanceIds.Contains(id)||id==node.Enemies?.Spatial?.PhysicalInstanceId;
    case "DEVICE":return id==node.Enemies?.Artillery?.HazardId;
    case "WORLD":return id==(string)e["authoringId"];
    default:return false;
   }
  }
  public void BindEntity(string runtimeId,string authoringId,Transform frame,Dictionary<string,Transform> anchors,Dictionary<string,Collider> shapes,Func<long> generation){
   Active();if(lastTick>=0||entities.ContainsKey(runtimeId))throw new InvalidOperationException("Native registration is fixed before the first observation.");
   var e=((JArray)definition["entities"]).OfType<JObject>().SingleOrDefault(x=>(string)x["authoringId"]==authoringId);Require(e!=null&&IdentityMatches(runtimeId,e));
   if(!frame||!frame.IsChildOf(basis)||anchors==null||shapes==null||generation==null)throw new ArgumentException("Borrowed native references and original generation owner required.");
   Require(anchors.Count==((JArray)e["anchors"]).Count&&shapes.Count==((JArray)e["shapes"]).Count);
   foreach(var a in (JArray)e["anchors"])Require(anchors.ContainsKey(Text(a["id"])));
   foreach(var s in (JArray)e["shapes"])Require(shapes.ContainsKey(Text(s["id"])));
   var bound=new Entity{Id=runtimeId,Definition=e,Frame=frame,Anchors=new Dictionary<string,Transform>(anchors,StringComparer.Ordinal),Shapes=new Dictionary<string,Collider>(shapes,StringComparer.Ordinal),Generation=generation};
   bound.LastMeasured=Measure(bound);entities.Add(runtimeId,bound);
  }
  JObject Measure(Entity e){
   if(!e.Frame){
    var absent=(JObject)e.LastMeasured.DeepClone();absent["present"]=false;absent["active"]=false;
    foreach(JObject shape in (JArray)absent["shapes"])shape["active"]=false;
    return absent;
   }
   long generation=e.Generation();if(generation<1)throw new InvalidOperationException("Original native generation missing.");
   var anchors=new JArray();foreach(JObject a in (JArray)e.Definition["anchors"]){
    var p=BPhysicalSceneSource.MeasureAnchor(e.Frame,e.Anchors[(string)a["id"]],units);EqualPoint(a["positionMm"],p);
    var row=(JObject)a.DeepClone();row["positionMm"]=p;anchors.Add(row);
   }
   var shapes=new JArray();foreach(JObject s in (JArray)e.Definition["shapes"]){
    var collider=e.Shapes[(string)s["id"]];var measured=BPhysicalSceneSource.MeasureShape(e.Frame,collider,units);
    if((string)s["kind"]!=(string)measured["kind"])throw new InvalidOperationException("Native collider kind differs from registration.");
    foreach(var field in measured.Properties().Where(p=>p.Name!="kind")){if(field.Value is JArray)EqualPoint(s[field.Name],field.Value);else EqualNumber(s[field.Name],field.Value);}
    measured["id"]=s["id"].DeepClone();measured["role"]=s["role"].DeepClone();measured["active"]=collider.enabled&&collider.gameObject.activeInHierarchy;shapes.Add(measured);
   }
   return new JObject{["id"]=e.Id,["authoringId"]=e.Definition["authoringId"].DeepClone(),["definitionRef"]=e.Definition["definitionRef"].DeepClone(),["role"]=e.Definition["role"].DeepClone(),["generation"]=generation,["present"]=true,["active"]=e.Frame.gameObject.activeInHierarchy,["frame"]=BPhysicalSceneSource.MeasureFrame(basis,e.Frame,units),["anchors"]=anchors,["shapes"]=shapes};
  }
  static void EqualPoint(JToken expected,JToken actual){if(expected is not JArray a||actual is not JArray b||a.Count!=3||b.Count!=3)throw new InvalidOperationException("Registered measured point required.");for(int i=0;i<3;i++)EqualNumber(a[i],b[i]);}
  static void EqualNumber(JToken expected,JToken actual){
   if(expected==null||(expected.Type!=JTokenType.Integer&&expected.Type!=JTokenType.Float)||actual==null||(actual.Type!=JTokenType.Integer&&actual.Type!=JTokenType.Float)||(double)expected!=(double)actual)throw new InvalidOperationException("Actual native geometry differs from the registered millimeter definition.");
  }
  public JObject Capture(long tick){
   Active();if(tick<0||tick<lastTick||entities.Count==0)throw new InvalidOperationException("Original ordered logical tick and bound scene required.");
   var boundIds=new HashSet<string>(entities.Values.Select(e=>(string)e.Definition["authoringId"]),StringComparer.Ordinal);
   if(((JArray)definition["entities"]).Any(e=>!boundIds.Contains((string)e["authoringId"])))throw new InvalidOperationException("Every registered geometry entity must be bound before observation.");
   var rows=new JArray(entities.OrderBy(e=>e.Key,StringComparer.Ordinal).Select(e=>Measure(e.Value)));
   Active();
   if(tick==lastTick){if(!JToken.DeepEquals(rows,retained["entities"]))throw new InvalidOperationException("Native geometry changed within the same logical observation.");return (JObject)retained.DeepClone();}
   Active();foreach(JObject row in rows)entities[(string)row["id"]].LastMeasured=(JObject)row.DeepClone();
   Active();retained=new JObject{["version"]="ordinary.node-scene-observation.v1.20261102",["accountId"]=attempt.AccountId,["runId"]=attempt.RunId,["nodeId"]=attempt.NodeId,["snapshotHash"]=attempt.SnapshotHash,["attemptGeneration"]=attempt.Generation,["attemptRevision"]=attempt.Revision,["definitionHash"]=node.GeometryBinding.Definition.DefinitionHash,["tick"]=tick,["revision"]=checked(++revision),["quantization"]="NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO",["entities"]=rows};lastTick=tick;return (JObject)retained.DeepClone();
  }
  public void Dispose(){if(disposed)return;disposed=true;entities.Clear();retained=null;}
 }
}
