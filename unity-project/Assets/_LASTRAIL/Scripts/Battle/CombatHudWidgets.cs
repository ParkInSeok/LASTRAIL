using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
public static class CombatCooldownText {
 public static string Format(long? milliseconds){
  if(!milliseconds.HasValue)return "미확인";
  long value=milliseconds.Value;if(value<0)throw new ArgumentOutOfRangeException(nameof(milliseconds));
  return value==0?"":value<1000?"1초 미만":(value/1000+(value%1000==0?0:1)).ToString(System.Globalization.CultureInfo.InvariantCulture);
 }
}
public static class CombatSkillNames {
 static JObject names;
 public static string Get(string id){
  if(names==null){var source=LastRail.Resources.PreparedContent.Load<TextAsset>("CombatPresentation/skill-names");names=source==null?new JObject():JObject.Parse(source.text);}
  return (string)names?[id]??(id=="support_fire"?"지원 사격":id=="barrier"?"방어막":id=="emergency_repair"?"긴급 수리":"명칭 미정");
 }
}
// Real simulation cooldown, independent of frame rate and local UI enablement.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatCooldownWipe:MaskableGraphic {
 public float Remaining {get;private set;}
 public void SetRemaining(float value){Remaining=Mathf.Clamp01(value);SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(Remaining<=0)return;var r=GetPixelAdjustedRect();mesh.AddVert(r.center,color,Vector2.zero);
  int steps=Mathf.Max(1,Mathf.CeilToInt(64*Remaining));
  for(int i=0;i<=steps;i++){float a=Mathf.PI*.5f-Mathf.PI*2*Remaining*i/steps;mesh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width/2,Mathf.Sin(a)*r.height/2),color,Vector2.zero);}
  for(int i=0;i<steps;i++)mesh.AddTriangle(0,i+1,i+2);
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatReadyFrame:MaskableGraphic {
 public float WidthScale=1;
 public float Progress {get;private set;}
 public void SetLight(float progress,float intensity){Progress=Mathf.Clamp01(progress);color=new Color(.24f,.86f,1,intensity);SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(Progress<=0)return;var r=GetPixelAdjustedRect();float c=8;
  var points=new[]{new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMin,r.yMin+c),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin+c,r.yMax)};
  float length=0;for(int i=0;i<8;i++)length+=Vector2.Distance(points[i],points[i+1]);float left=length*Progress;
  for(int i=0;i<8&&left>0;i++){var a=points[i];float n=Vector2.Distance(a,points[i+1]);var b=Vector2.Lerp(a,points[i+1],Mathf.Min(1,left/n));left-=n;Line(mesh,a,b,7*WidthScale,new Color(color.r,color.g,color.b,color.a*.1f));Line(mesh,a,b,3*WidthScale,new Color(color.r,color.g,color.b,color.a*.35f));Line(mesh,a,b,WidthScale,color);}
 }
 static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*width;int v=mesh.currentVertCount;mesh.AddVert(a+p,tint,Vector2.zero);mesh.AddVert(b+p,tint,Vector2.zero);mesh.AddVert(b-p,tint,Vector2.zero);mesh.AddVert(a-p,tint,Vector2.zero);mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);}
}
}

