using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Separate source pieces on a joint hierarchy. Diagnostic source is never selected implicitly.
public sealed class CombatMultipartRig:MonoBehaviour {
 readonly Dictionary<string,RectTransform> joints=new Dictionary<string,RectTransform>();
 readonly List<CombatClippedPart> graphics=new List<CombatClippedPart>();
 readonly Dictionary<string,float> restAngles=new Dictionary<string,float>();
 readonly Dictionary<string,JToken> partRows=new Dictionary<string,JToken>();
 Dictionary<string,float> downAngles=new Dictionary<string,float>(),downOrigin=new Dictionary<string,float>();
 public float Down{get;private set;}
 public Color Tint{get;private set;}=Color.white;
 JObject sockets;JArray poseSamples,skillSamples;bool absoluteAngles;
 CombatRigAtlasCache.Lease alphaCache;
 public bool HasSkillPose=>skillAngles.Count>0||skillSamples!=null&&skillSamples.Count>0;
 Dictionary<string,float> attackAngles=new Dictionary<string,float>{{"torso",4},{"upper_arm",-75},{"forearm_knife",24}};
 Dictionary<string,float> skillAngles=new Dictionary<string,float>{{"torso",-3},{"upper_arm",-120},{"forearm_knife",70}};
 public int TriangleCount=>graphics.Sum(p=>p.TriangleCount);
 public float Extension{get;private set;}
 public float Guard{get;private set;}
 public double BuildMilliseconds{get;private set;}
 public double MeanPoseMicroseconds=>poseSamplesCount==0?0:poseTicks*1000000d/System.Diagnostics.Stopwatch.Frequency/poseSamplesCount;
 long poseTicks,poseSamplesCount;
 public void Configure(Texture2D source,JObject data,bool cacheAlpha=false){
  var build=System.Diagnostics.Stopwatch.StartNew();
  if(joints.Count>0||source==null)throw new ArgumentException("Configure once");
  Texture renderSource=source;if(cacheAlpha){alphaCache=CombatRigAtlasCache.Acquire(source,data);renderSource=alphaCache.Texture;}
  if(data["downAngles"] is JObject down)downAngles=down.Properties().ToDictionary(p=>p.Name,p=>(float)p.Value);
  sockets=data["sockets"] as JObject;poseSamples=data["attackPoseSamples"] as JArray;skillSamples=data["skillPoseSamples"] as JArray;absoluteAngles=((JArray)data["parts"]).Any(r=>r["restAngleDeg"]!=null);if(absoluteAngles&&data["skillAngles"]==null)skillAngles=new Dictionary<string,float>();
  if(data["attackAngles"] is JObject attack)attackAngles=attack.Properties().ToDictionary(p=>p.Name,p=>(float)p.Value);
  if(data["skillAngles"] is JObject skill)skillAngles=skill.Properties().ToDictionary(p=>p.Name,p=>(float)p.Value);
  foreach(var row in (JArray)data["parts"]){
   string id=(string)row["id"],parent=(string)row["parent"];var joint=new GameObject(id,typeof(RectTransform)).GetComponent<RectTransform>();joint.SetParent(parent==null?transform:joints[parent],false);joint.anchorMin=joint.anchorMax=new Vector2(0,1);joint.sizeDelta=Vector2.zero;
   joint.anchoredPosition=new Vector2((float)row["anchor"][0],-(float)row["anchor"][1]);joints.Add(id,joint);
   partRows.Add(id,row);
   restAngles[id]=(float?)row["restAngleDeg"]??(float?)row["restAngle"]??0;SetAngle(id,0);
   var art=new GameObject("SourcePart",typeof(RectTransform)).AddComponent<CombatClippedPart>();art.transform.SetParent(joint,false);art.Configure(renderSource,row,new Vector2((float)data["sourceSize"][0],(float)data["sourceSize"][1]));if(alphaCache!=null)art.material=alphaCache.Material;art.raycastTarget=false;graphics.Add(art);
  }
  // Far arm stays behind the torso; the scale applies to source geometry, not child anchors.
  if(joints.TryGetValue("far_arm",out var far))far.SetAsFirstSibling();if(joints.TryGetValue("far_upper",out var upper))upper.SetAsFirstSibling();
  if(!absoluteAngles&&attackAngles.Keys.Concat(skillAngles.Keys).Any(id=>!joints.ContainsKey(id)))throw new ArgumentException("Pose references a missing source joint");
  build.Stop();BuildMilliseconds=build.Elapsed.TotalMilliseconds;
 }
 float SampleAngle(JArray samples,string id,float t){
  float rest=restAngles[id];JToken a=samples[0],b=samples[samples.Count-1];for(int i=1;i<samples.Count;i++)if((float)samples[i]["t"]>=t){a=samples[i-1];b=samples[i];break;}
  float at=(float)a["t"],bt=(float)b["t"];float blend=bt>at?Mathf.Clamp01((t-at)/(bt-at)):0;
  return Mathf.LerpAngle((float?)a["angles"][id]??rest,(float?)b["angles"][id]??rest,blend);
 }
 float AttackAngle(string id,float t){
  float rest=restAngles[id];if(absoluteAngles&&poseSamples!=null&&poseSamples.Count>0)return SampleAngle(poseSamples,id,t);
  return absoluteAngles?Mathf.LerpAngle(rest,attackAngles.TryGetValue(id,out var target)?target:rest,t):rest+(attackAngles.TryGetValue(id,out var delta)?delta*t:0);
 }
 public void SetExtension(float value){long start=System.Diagnostics.Stopwatch.GetTimestamp();float t=Mathf.Clamp01(value);Extension=t;foreach(var id in joints.Keys)joints[id].localRotation=Quaternion.Euler(0,0,-AttackAngle(id,t));poseTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;poseSamplesCount++;}
 public void SetGuard(float value){float t=Mathf.Clamp01(value);Guard=t;if(t<=0||!HasSkillPose)return;if(absoluteAngles&&skillSamples!=null&&skillSamples.Count>0){foreach(var id in joints.Keys)joints[id].localRotation=Quaternion.Euler(0,0,-SampleAngle(skillSamples,id,t));return;}foreach(var id in joints.Keys){float from=AttackAngle(id,Extension);float to=absoluteAngles?(skillAngles.TryGetValue(id,out var target)?target:restAngles[id]):restAngles[id]+(skillAngles.TryGetValue(id,out var delta)?delta:0);joints[id].localRotation=Quaternion.Euler(0,0,-Mathf.LerpAngle(from,to,t));}}
 void SetAngle(string id,float clockwise){if(joints.TryGetValue(id,out var joint))joint.localRotation=Quaternion.Euler(0,0,-clockwise-(restAngles.TryGetValue(id,out var rest)?rest:0));}
 public void BeginDown(){downOrigin=joints.ToDictionary(p=>p.Key,p=>Mathf.DeltaAngle(0,-p.Value.localEulerAngles.z));}
 public void SetDown(float amount,bool recovering){
  Down=Mathf.Clamp01(amount);if(Down<=0&&recovering)return;if(downAngles.Count==0)throw new InvalidOperationException("Approved down pose missing");
  foreach(var id in joints.Keys){float target=downAngles.TryGetValue(id,out var angle)?angle:restAngles[id];float from=recovering?Mathf.DeltaAngle(0,-joints[id].localEulerAngles.z):downOrigin.TryGetValue(id,out var captured)?captured:restAngles[id];joints[id].localRotation=Quaternion.Euler(0,0,-Mathf.LerpAngle(from,target,Down));}
 }
 public void SetTint(Color tint){Tint=tint;foreach(var graphic in graphics)if(graphic.color!=tint)graphic.color=tint;}
 void OnDestroy(){CombatRigAtlasCache.Release(alphaCache);alphaCache=null;}
 public bool TrySocket(string name,out Vector3 point,out Vector3 direction){
  point=default;direction=default;var socket=sockets?[name];if(socket==null)return false;string id=(string)socket["part"];
  if(!joints.TryGetValue(id,out var joint)||!(socket["sourcePoint"] is JArray sourcePoint))throw new ArgumentException("Invalid authored socket: "+name);
  var row=partRows[id];float scale=(float)row["scale"];var local=new Vector3(((float)sourcePoint[0]-(float)row["pivot"][0])*scale,-((float)sourcePoint[1]-(float)row["pivot"][1])*scale);
  point=joint.TransformPoint(local);if(name=="muzzle"&&sockets["barrelBase"]!=null){TrySocket("barrelBase",out var origin,out _);direction=(point-origin).normalized;return true;}var forward=socket["direction"] as JArray;direction=joint.TransformDirection(forward==null?Vector3.right:new Vector3((float)forward[0],-(float)forward[1])).normalized;return true;
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatClippedPart:MaskableGraphic {
 Texture source;readonly List<Vector2> points=new List<Vector2>();readonly List<int> indices=new List<int>();Vector2 sourcePivot,originalSize;float sourceScale;
 public int TriangleCount=>indices.Count/3;
 public override Texture mainTexture=>source;
 static float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
 static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c,float sign)=>Cross(a,b,p)*sign>=-.001f&&Cross(b,c,p)*sign>=-.001f&&Cross(c,a,p)*sign>=-.001f;
 static List<Vector2[]> Triangulate(List<Vector2> poly){
  float area=0;for(int i=0;i<poly.Count;i++){var a=poly[i];var b=poly[(i+1)%poly.Count];area+=a.x*b.y-b.x*a.y;}float sign=Mathf.Sign(area);if(sign==0)throw new ArgumentException("Zero-area contour");
  var indices=Enumerable.Range(0,poly.Count).ToList();var triangles=new List<Vector2[]>();int guard=poly.Count*poly.Count;
  while(indices.Count>3&&guard-->0){bool clipped=false;for(int i=0;i<indices.Count;i++){
   int ai=indices[(i+indices.Count-1)%indices.Count],bi=indices[i],ci=indices[(i+1)%indices.Count];var a=poly[ai];var b=poly[bi];var c=poly[ci];
   if(Cross(a,b,c)*sign<=.001f||indices.Any(n=>n!=ai&&n!=bi&&n!=ci&&Inside(poly[n],a,b,c,sign)))continue;
   triangles.Add(new[]{a,b,c});indices.RemoveAt(i);clipped=true;break;
  }if(!clipped)throw new ArgumentException("Contour is not a simple polygon");}
  if(indices.Count!=3)throw new ArgumentException("Contour triangulation failed");triangles.Add(indices.Select(i=>poly[i]).ToArray());return triangles;
 }
 static List<Vector2> Clip(List<Vector2> input,int axis,float edge,bool lower){
  var output=new List<Vector2>();if(input.Count==0)return output;var previous=input[input.Count-1];bool was=lower?previous[axis]>=edge:previous[axis]<=edge;
  foreach(var current in input){bool now=lower?current[axis]>=edge:current[axis]<=edge;if(now!=was){float t=(edge-previous[axis])/(current[axis]-previous[axis]);output.Add(Vector2.LerpUnclamped(previous,current,t));}if(now)output.Add(current);previous=current;was=now;}return output;
 }
 public void Configure(Texture image,JToken row,Vector2 declaredSourceSize){
  if(declaredSourceSize.x<=0||declaredSourceSize.y<=0)throw new ArgumentException("Original pixel coordinate extent required");
  source=image;originalSize=declaredSourceSize;sourcePivot=new Vector2((float)row["pivot"][0],(float)row["pivot"][1]);sourceScale=(float)row["scale"];
  var polygon=((JArray)row["polygon"]).Select(p=>new Vector2((float)p[0],(float)p[1])).ToList();
  if(row["meshVertices"] is JArray vertices){points.AddRange(vertices.Select(p=>new Vector2((float)p[0],(float)p[1])));indices.AddRange(((JArray)row["meshTriangles"]).Select(i=>(int)i));if(indices.Count%3!=0||indices.Any(i=>i<0||i>=points.Count))throw new ArgumentException("Invalid baked mesh indices");}
  else {var triangles=Triangulate(polygon);
  foreach(var triangle in triangles)foreach(var rect in (JArray)row["nativeMaskRects"]){
   float x=(float)rect[0],y=(float)rect[1],w=(float)rect[2],h=(float)rect[3];if(triangle.Max(p=>p.x)<x||triangle.Min(p=>p.x)>x+w||triangle.Max(p=>p.y)<y||triangle.Min(p=>p.y)>y+h)continue;
   var clipped=triangle.ToList();clipped=Clip(clipped,0,x,true);clipped=Clip(clipped,0,x+w,false);clipped=Clip(clipped,1,y,true);clipped=Clip(clipped,1,y+h,false);
   for(int i=1;i<clipped.Count-1;i++)if(Mathf.Abs(Cross(clipped[0],clipped[i],clipped[i+1]))>.001f){points.Add(clipped[0]);points.Add(clipped[i]);points.Add(clipped[i+1]);}
  }
  indices.AddRange(Enumerable.Range(0,points.Count));}
  if(points.Count==0||points.Count>=65000)throw new ArgumentException("Invalid or excessive clipped-part geometry");
  float minX=(polygon.Min(p=>p.x)-sourcePivot.x)*sourceScale,maxX=(polygon.Max(p=>p.x)-sourcePivot.x)*sourceScale,minY=-(polygon.Max(p=>p.y)-sourcePivot.y)*sourceScale,maxY=-(polygon.Min(p=>p.y)-sourcePivot.y)*sourceScale;
  rectTransform.anchorMin=rectTransform.anchorMax=new Vector2(0,1);rectTransform.sizeDelta=new Vector2(maxX-minX,maxY-minY);rectTransform.pivot=new Vector2(-minX/(maxX-minX),-minY/(maxY-minY));rectTransform.anchoredPosition=Vector2.zero;SetVerticesDirty();SetMaterialDirty();
 }
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();foreach(var p in points)mesh.AddVert(new Vector2((p.x-sourcePivot.x)*sourceScale,-(p.y-sourcePivot.y)*sourceScale),color,new Vector2(p.x/originalSize.x,1-p.y/originalSize.y));for(int i=0;i<indices.Count;i+=3)mesh.AddTriangle(indices[i],indices[i+1],indices[i+2]);}
}
}
