using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatRailTrack:MaskableGraphic {
 public bool Map;long distance,junction;int side,preview;bool selecting;
 public void SetRoute(long d,long j,int s,bool choosing,int p){distance=d;junction=j;side=s;selecting=choosing;preview=p;SetVerticesDirty();}
 void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint){var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int n=mesh.currentVertCount;mesh.AddVert(a-normal,tint,Vector2.zero);mesh.AddVert(a+normal,tint,Vector2.zero);mesh.AddVert(b+normal,tint,Vector2.zero);mesh.AddVert(b-normal,tint,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();var r=rectTransform.rect;
  if(Map){for(int branch=-1;branch<=1;branch+=2){var tint=(selecting?(preview%2==0?-1:1):side)==branch?new Color(.3f,.93f,.97f):new Color(.2f,.35f,.37f);Line(mesh,new Vector2(6,-21),new Vector2(66,-21),2,tint);Line(mesh,new Vector2(66,-21),new Vector2(112,-21+branch*13),2,tint);Line(mesh,new Vector2(112,-21+branch*13),new Vector2(174,-21+branch*13),2,tint);}return;}
  float Offset(float x)=>CombatRailWorldLayout.RouteOffsetAt(distance,junction,(620-x)/120,side);
  for(int i=0;i<48;i++){float a=i*15,b=a+15;float ya=230+Offset(a),yb=230+Offset(b);Line(mesh,new Vector2(a,-ya),new Vector2(b,-yb),14,new Color(.14f,.115f,.085f,.9f));Line(mesh,new Vector2(a,-ya+3),new Vector2(b,-yb+3),2,new Color(.42f,.43f,.39f));Line(mesh,new Vector2(a,-ya-3),new Vector2(b,-yb-3),2,new Color(.25f,.27f,.25f));}
  float shift=(distance/1000f*12)%18;for(float x=-shift;x<720;x+=18)Line(mesh,new Vector2(x,-230-Offset(x)+6),new Vector2(x,-230-Offset(x)-6),1.6f,new Color(.18f,.16f,.13f));
 }
}
}
