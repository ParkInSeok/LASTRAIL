using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatDistantRail:MaskableGraphic {
 float travel;
 bool sideView;
 public void SetTravel(float distance){travel=distance;SetVerticesDirty();}
 public void PlaceAcrossSideView(RectTransform field, float wheelLine, Transform train)
 {
  sideView = true;
  rectTransform.SetParent(field, false);
  rectTransform.sizeDelta = new Vector2(field.rect.width, 24);
  rectTransform.anchoredPosition = new Vector2(0, -wheelLine + 12);
  rectTransform.SetSiblingIndex(train.GetSiblingIndex());
  SetVerticesDirty();
 }
 Vector2 Point(float x,float offset){var r=rectTransform.rect;float center=sideView?12f:223.86f+x*.0606667f;return new Vector2(r.xMin+x,r.yMax-(center+offset));}
 static void Strip(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint){var d=b-a;var n=new Vector2(-d.y,d.x).normalized*width*.5f;int i=vh.currentVertCount;vh.AddVert(a-n,tint,Vector2.zero);vh.AddVert(a+n,tint,Vector2.zero);vh.AddVert(b+n,tint,Vector2.zero);vh.AddVert(b-n,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
 protected override void OnPopulateMesh(VertexHelper vh){
  float left=sideView?0:-20, right=sideView?rectTransform.rect.width:750;
  vh.Clear();Strip(vh,Point(left,0),Point(right,0),22,new Color(.10f,.075f,.053f,.83f));
  float offset=Mathf.Repeat(travel,24);
  float firstSleeper = (sideView ? 24f : -24f) - offset;
  float lastSleeper = sideView ? right - 1 : 740;
  for (float x = firstSleeper; x < lastSleeper; x += 24)
  {
   float end = sideView ? Mathf.Max(0, x - 4) : x - 4;
   Strip(vh,Point(x,-10),Point(end,10),3.2f,new Color(.19f,.14f,.095f));
   Strip(vh,Point(x+.8f,-10),Point(end+.8f,10),.6f,new Color(.4f,.32f,.23f,.7f));
  }
  foreach(float side in new[]{-3.4f,3.4f}){Strip(vh,Point(left,side),Point(right,side),2.8f,new Color(.06f,.065f,.07f));Strip(vh,Point(left,side-1),Point(right,side-1),.9f,new Color(.65f,.63f,.56f,.86f));}
 }
}
}
