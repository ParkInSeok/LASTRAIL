using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Partition existing triangles instead of repainting them: retain draw order, UV and gradients.
internal static class CombatUiMeshPartition {
 internal static Vector2[] Aperture(Rect r,float bevel)=>new[]{new Vector2(r.x+bevel,r.y),new Vector2(r.xMax-bevel,r.y),new Vector2(r.xMax,r.y+bevel),new Vector2(r.xMax,r.yMax-bevel),new Vector2(r.xMax-bevel,r.yMax),new Vector2(r.x+bevel,r.yMax),new Vector2(r.x,r.yMax-bevel),new Vector2(r.x,r.y+bevel)};
 static float Side(Vector2 a,Vector2 b,Vector3 p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
 static UIVertex Between(UIVertex a,UIVertex b,float t){var v=a;v.position=Vector3.LerpUnclamped(a.position,b.position,t);v.color=Color.LerpUnclamped(a.color,b.color,t);v.uv0=Vector4.LerpUnclamped(a.uv0,b.uv0,t);v.uv1=Vector4.LerpUnclamped(a.uv1,b.uv1,t);return v;}
 static List<UIVertex> Clip(List<UIVertex> input,Vector2 a,Vector2 b,bool inside){var output=new List<UIVertex>();if(input.Count==0)return output;var previous=input[input.Count-1];float pd=Side(a,b,previous.position);bool pin=inside?pd>=0:pd<=0;foreach(var next in input){float nd=Side(a,b,next.position);bool nin=inside?nd>=0:nd<=0;if(pin!=nin)output.Add(Between(previous,next,pd/(pd-nd)));if(nin)output.Add(next);previous=next;pd=nd;pin=nin;}return output;}
 static void Emit(VertexHelper mesh,List<UIVertex> polygon){if(polygon.Count<3)return;int start=mesh.currentVertCount;foreach(var v in polygon)mesh.AddVert(v);for(int i=1;i<polygon.Count-1;i++)if(Mathf.Abs(Side(polygon[0].position,polygon[i].position,polygon[i+1].position))>.00001f)mesh.AddTriangle(start,start+i,start+i+1);}
 internal static void Copy(VertexHelper source,VertexHelper target,Vector2[] hole,bool inside){var stream=new List<UIVertex>();source.GetUIVertexStream(stream);for(int i=0;i<stream.Count;i+=3){var rest=new List<UIVertex>{stream[i],stream[i+1],stream[i+2]};for(int edge=0;edge<hole.Length&&rest.Count>0;edge++){var a=hole[edge];var b=hole[(edge+1)%hole.Length];if((b-a).sqrMagnitude<.000001f)continue;if(!inside)Emit(target,Clip(rest,a,b,false));rest=Clip(rest,a,b,true);}if(inside)Emit(target,rest);}}
 internal static void RemoveInterior(VertexHelper mesh,Vector2[] hole){using(var outside=new VertexHelper()){Copy(mesh,outside,hole,false);var stream=new List<UIVertex>();outside.GetUIVertexStream(stream);mesh.Clear();mesh.AddUIVertexTriangleStream(stream);}}
 internal static void KeepInterior(VertexHelper mesh,Vector2[] hole){using(var inside=new VertexHelper()){Copy(mesh,inside,hole,true);var stream=new List<UIVertex>();inside.GetUIVertexStream(stream);mesh.Clear();mesh.AddUIVertexTriangleStream(stream);}}
}
}
