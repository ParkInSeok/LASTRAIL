using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle {
 // Measures borrowed scene references. This packet grants no server execution authority.
 // Revision belongs to this bound component lifetime; it is not a persisted run/server revision.
 // The caller owns reference lifetime and must bind a replacement source explicitly.
 [DisallowMultipleComponent]
 public sealed class BPhysicalSceneSource : MonoBehaviour {
  public const string Version = "unity.combat-b.scene-measurement.v1.20261020";
  const long CoordinateLimit = 1000000000;
  string sourceId, unitContractId;
  double millimetersPerUnit;
  Transform frame;
  BoxCollider body;
  string[] anchorIds;
  Transform[] anchors;
  JObject retained;
  string geometryHash;
  long revision;

  public void Bind(string sourceId, string unitContractId, double millimetersPerUnit,
   Transform frame, BoxCollider body, string[] anchorIds, Transform[] anchors) {
   if (this.anchorIds != null) throw new InvalidOperationException("b_scene_source_already_bound");
   RequireId(sourceId); RequireId(unitContractId);
   if (!Finite(millimetersPerUnit) || millimetersPerUnit <= 0)
    throw new ArgumentException("b_scene_unit_contract_required");
   if (anchorIds == null || anchors == null || anchorIds.Length == 0 ||
    anchorIds.Length != anchors.Length || anchorIds.Length > 128)
    throw new ArgumentException("b_scene_anchor_contract");
   var ids = (string[])anchorIds.Clone();
   var refs = (Transform[])anchors.Clone();
   var unique = new HashSet<string>(StringComparer.Ordinal);
   foreach (var id in ids) {
    RequireId(id);
    if (!unique.Add(id)) throw new ArgumentException("b_scene_anchor_duplicate");
   }
   ValidateReferences(frame, body, refs);
   // Name order is canonical; caller array mutation cannot change the source contract.
   Array.Sort(ids, refs, StringComparer.Ordinal);
   this.sourceId = sourceId; this.unitContractId = unitContractId;
   this.millimetersPerUnit = millimetersPerUnit; this.frame = frame; this.body = body;
   this.anchorIds = ids; this.anchors = refs;
  }

  public JObject Capture() {
   if (anchorIds == null) throw new InvalidOperationException("b_scene_source_not_bound");
   ValidateReferences(frame, body, anchors);
   var size = body.size;
   if (!Finite(size.x) || !Finite(size.y) || !Finite(size.z) ||
    size.x <= 0 || size.y <= 0 || size.z <= 0)
    throw new InvalidOperationException("b_scene_body_size_invalid");
   var matrix = RelativeToFrame(body.transform, frame);
   var corners = new JArray();
   for (int x = -1; x <= 1; x += 2)
    for (int y = -1; y <= 1; y += 2)
     for (int z = -1; z <= 1; z += 2)
      corners.Add(Point(matrix.MultiplyPoint3x4(body.center +
       Vector3.Scale(size * .5f, new Vector3(x, y, z)))));
   var points = new JArray();
   for (int i = 0; i < anchors.Length; i++)
    points.Add(new JObject { ["id"] = anchorIds[i],
     ["positionMm"] = Point(RelativeToFrame(anchors[i], frame).MultiplyPoint3x4(Vector3.zero)) });
   var geometry = new JObject {
    ["version"] = Version, ["sourceId"] = sourceId,
    ["provenance"] = "UNITY_SCENE_MEASUREMENT_NOT_EXECUTION_AUTHORITY",
    ["unitContractId"] = unitContractId, ["millimetersPerUnit"] = millimetersPerUnit,
    ["coordinateSpace"] = "BOUND_FRAME_LOCAL",
    ["quantization"] = "NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO",
    ["bodyCornersMm"] = corners, ["anchors"] = points
   };
   var hash = BDirectAttackInput.Digest(geometry);
   if (retained == null || hash != geometryHash) {
    long nextRevision = checked(revision + 1);
    geometry["geometryHash"] = hash;
    geometry["sourceRevision"] = nextRevision;
    geometry["packetHash"] = BDirectAttackInput.Digest(geometry);
    retained = geometry; geometryHash = hash; revision = nextRevision;
   }
   return (JObject)retained.DeepClone();
  }

  // The server verifies these exact UTF-8 JSON strings against both hashes and
  // parsed contents. Re-serializing doubles through JavaScript would erase .0.
  public JObject CaptureExport() {
   var packet = Capture();
   var packetBody = (JObject)packet.DeepClone();
   packetBody.Remove("packetHash");
   var geometryBody = (JObject)packetBody.DeepClone();
   geometryBody.Remove("geometryHash"); geometryBody.Remove("sourceRevision");
   return new JObject {
    ["packet"] = packet,
    ["geometryCanonicalJson"] = BasicCombatSimulation.StableJson(geometryBody),
    ["packetCanonicalJson"] = BasicCombatSimulation.StableJson(packetBody)
   };
  }

  JArray Point(Vector3 point) => new JArray(Mm(point.x), Mm(point.y), Mm(point.z));
  // Shared measurement primitives for current-node adapters; these confer no registration authority.
  internal static JArray MeasureAnchor(Transform frame,Transform anchor,double units){
   if(!frame||!anchor||!anchor.IsChildOf(frame))throw new InvalidOperationException("b_scene_anchor_reference_invalid");
   return MeasuredPoint(RelativeToFrame(anchor,frame).MultiplyPoint3x4(Vector3.zero),units);
  }
  internal static JObject MeasureShape(Transform frame,Collider shape,double units){
   if(!frame||!shape||!shape.transform.IsChildOf(frame))throw new InvalidOperationException("b_scene_shape_reference_invalid");
   var m=RelativeToFrame(shape.transform,frame);
   if(Math.Abs(m.m01)+Math.Abs(m.m02)+Math.Abs(m.m10)+Math.Abs(m.m12)+Math.Abs(m.m20)+Math.Abs(m.m21)>1e-6)throw new NotSupportedException("Registered local axis-aligned shape required.");
   if(shape is BoxCollider box)return new JObject{["kind"]="BOX",["centerMm"]=MeasuredPoint(m.MultiplyPoint3x4(box.center),units),["sizeMm"]=MeasuredPoint(Vector3.Scale(box.size,new Vector3(Math.Abs(m.m00),Math.Abs(m.m11),Math.Abs(m.m22))),units)};
   if(shape is CapsuleCollider capsule){
    if(capsule.direction!=1||Math.Abs(Math.Abs(m.m00)-Math.Abs(m.m22))>1e-6)throw new NotSupportedException("Registered Y capsule with equal radial scale required.");
    return new JObject{["kind"]="CAPSULE_Y",["centerMm"]=MeasuredPoint(m.MultiplyPoint3x4(capsule.center),units),["radiusMm"]=MeasuredMm(capsule.radius*Math.Abs(m.m00),units),["heightMm"]=MeasuredMm(capsule.height*Math.Abs(m.m11),units)};
   }
   throw new NotSupportedException("Registered box or Y capsule required.");
  }
  internal static JObject MeasureFrame(Transform basis,Transform frame,double units){
   if(!basis||!frame||!frame.IsChildOf(basis)||(basis.lossyScale-Vector3.one).sqrMagnitude>1e-10)throw new InvalidOperationException("b_scene_native_basis_invalid");
   var m=RelativeToFrame(frame,basis);var x=(Vector3)m.GetColumn(0);var y=(Vector3)m.GetColumn(1);var z=(Vector3)m.GetColumn(2);
   if(Math.Abs(x.sqrMagnitude-1)>1e-5||Math.Abs(y.sqrMagnitude-1)>1e-5||Math.Abs(z.sqrMagnitude-1)>1e-5||Math.Abs(Vector3.Dot(x,y))>1e-5||Math.Abs(Vector3.Dot(x,z))>1e-5||Math.Abs(Vector3.Dot(y,z))>1e-5||Vector3.Dot(Vector3.Cross(x,y),z)<.99999f)throw new InvalidOperationException("b_scene_rigid_frame_required");
   return new JObject{["originMm"]=MeasuredPoint(m.MultiplyPoint3x4(Vector3.zero),units),["basis"]=new JArray(new JArray(x.x,x.y,x.z),new JArray(y.x,y.y,y.z),new JArray(z.x,z.y,z.z))};
  }
  static JArray MeasuredPoint(Vector3 p,double units)=>new JArray(MeasuredMm(p.x,units),MeasuredMm(p.y,units),MeasuredMm(p.z,units));
  static long MeasuredMm(float value,double units){
   if(!Finite(units)||units<=0)throw new ArgumentException("b_scene_unit_contract_required");
   double mm=Math.Round(value*units,MidpointRounding.AwayFromZero);if(!Finite(mm)||mm< -CoordinateLimit||mm>CoordinateLimit)throw new InvalidOperationException("b_scene_coordinate_out_of_range");return (long)mm;
  }
  long Mm(float value) {
   double mm = Math.Round(value * millimetersPerUnit, MidpointRounding.AwayFromZero);
   if (!Finite(mm) || mm < -CoordinateLimit || mm > CoordinateLimit)
    throw new InvalidOperationException("b_scene_coordinate_out_of_range");
   return (long)mm;
  }
  static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
  static void RequireId(string id) {
   if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || id.Trim() != id)
    throw new ArgumentException("b_scene_source_id_required");
  }
  static void ValidateReferences(Transform frame, BoxCollider body, Transform[] anchors) {
   if (!frame || !body || !body.transform.IsChildOf(frame))
    throw new InvalidOperationException("b_scene_body_reference_invalid");
   foreach (var anchor in anchors)
    if (!anchor || !anchor.IsChildOf(frame))
     throw new InvalidOperationException("b_scene_anchor_reference_invalid");
  }
  static Matrix4x4 RelativeToFrame(Transform source, Transform frame) {
   // Local hierarchy multiplication avoids precision loss from large world origins.
   var matrix = Matrix4x4.identity;
   for (var current = source; current != frame; current = current.parent) {
    var scale = current.localScale;
    if (!Finite(scale.x) || !Finite(scale.y) || !Finite(scale.z) ||
     scale.x == 0 || scale.y == 0 || scale.z == 0)
     throw new InvalidOperationException("b_scene_transform_degenerate");
    matrix = Matrix4x4.TRS(current.localPosition, current.localRotation, scale) * matrix;
   }
   return matrix;
  }
 }
}
