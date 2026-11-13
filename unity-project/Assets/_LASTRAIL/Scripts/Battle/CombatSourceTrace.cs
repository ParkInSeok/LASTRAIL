using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Decorative source-to-result line only. It never schedules or applies damage.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatSourceTrace:MaskableGraphic {
 public Vector2 From,To;public bool Cannon;float progress;
 public void SetProgress(float value){progress=Mathf.Clamp01(value);SetVerticesDirty();}
 void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint){var direction=b-a;if(direction.sqrMagnitude<.01f)return;var normal=new Vector2(-direction.y,direction.x).normalized*width*.5f;int n=vh.currentVertCount;vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();float fade=1-progress;var color=Cannon?new Color(1,.77f,.36f,fade*.72f):new Color(.3f,.85f,1,fade*.55f);var distance=To-From;
  if(Cannon){Line(vh,Vector2.Lerp(From,To,.12f+progress*.32f),Vector2.Lerp(From,To,.72f+progress*.28f),1.25f,color);var normal=new Vector2(-distance.y,distance.x).normalized;Line(vh,To-normal*7,To+normal*7,1,color);}
  else {for(int i=0;i<7;i++){float t=i/7f;Line(vh,Vector2.Lerp(From,To,t),Vector2.Lerp(From,To,t+.075f),1,color);}float pulse=Mathf.Clamp01(progress*1.6f);Line(vh,Vector2.Lerp(From,To,Mathf.Max(0,pulse-.12f)),Vector2.Lerp(From,To,pulse),2,new Color(.5f,.92f,1,fade));}
 }
}
}
