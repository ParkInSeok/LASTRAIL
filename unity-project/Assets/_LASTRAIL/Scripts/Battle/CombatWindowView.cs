using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// The scenery is rendered only inside the glass opening; the painted frame stays above it.
public sealed class CombatWindowView:RawImage {
 public Texture Texture {get=>texture;set=>texture=value;}
 public float Travel;
 public void SetProjection(Rect exterior,float screenX,float screenWidth){Travel=exterior.x;uvRect=new Rect(exterior.x+screenX*exterior.width,.38f,screenWidth*exterior.width,.42f);}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatWheelDisc:MaskableGraphic {
 public Texture Texture;public Rect Region;public float Angle;
 public override Texture mainTexture=>Texture;
 public void SetDistance(float pixels){Angle=-pixels/(rectTransform.rect.width*.5f);SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();var r=GetPixelAdjustedRect();mesh.AddVert(r.center,color,Region.center);
  for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;mesh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f),color,Region.center+new Vector2(Mathf.Cos(a-Angle)*Region.width*.5f,Mathf.Sin(a-Angle)*Region.height*.5f));}
  for(int i=0;i<40;i++)mesh.AddTriangle(0,i+1,i+2);
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatGroundShadow:MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();var r=GetPixelAdjustedRect();mesh.AddVert(r.center,color,Vector2.zero);
  for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;mesh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width*.5f,Mathf.Sin(a)*r.height*.5f),new Color(0,0,0,0),Vector2.zero);}
  for(int i=0;i<32;i++)mesh.AddTriangle(0,i+1,i+2);
 }
}
}
