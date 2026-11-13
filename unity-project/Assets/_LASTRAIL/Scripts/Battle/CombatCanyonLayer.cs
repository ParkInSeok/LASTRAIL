using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatCanyonLayer:MaskableGraphic {
 Texture2D texture;Vector2[] points=Array.Empty<Vector2>();int[] indices=Array.Empty<int>();
 public override Texture mainTexture=>texture!=null?texture:Texture2D.whiteTexture;
 public void Configure(Texture2D source,JObject data){
  if(source==null||data==null)throw new ArgumentException();var vertices=data["verticesNormalizedTopLeft"] as JArray;var triangles=data["triangles"] as JArray;
  if(vertices==null||vertices.Count<3||vertices.Count>128||triangles==null||triangles.Count>768||triangles.Count%3!=0)throw new ArgumentException("Invalid supplied terrain mesh");
  var next=new Vector2[vertices.Count];for(int i=0;i<next.Length;i++){var p=vertices[i] as JArray;if(p==null||p.Count!=2)throw new ArgumentException();float x=(float)p[0],y=(float)p[1];if(float.IsNaN(x)||float.IsNaN(y)||x<0||x>1||y<0||y>1)throw new ArgumentException();next[i]=new Vector2(x,y);}
  var nextIndices=new int[triangles.Count];for(int i=0;i<nextIndices.Length;i++){int n=(int)triangles[i];if(n<0||n>=next.Length)throw new ArgumentException();nextIndices[i]=n;}
  texture=source;points=next;indices=nextIndices;raycastTarget=false;SetMaterialDirty();SetVerticesDirty();
 }
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var rect=GetPixelAdjustedRect();foreach(var p in points)vh.AddVert(new Vector2(rect.xMin+p.x*rect.width,rect.yMax-p.y*rect.height),Color.white,new Vector2(p.x,1-p.y));for(int i=0;i<indices.Length;i+=3)vh.AddTriangle(indices[i],indices[i+1],indices[i+2]);}
}
}
