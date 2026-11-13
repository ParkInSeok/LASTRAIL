using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.Battle {
 // Independent 2D UI port of approved Canvas gradient and flash polygon. No art bake or gameplay event.
 public sealed class KyleMuzzleFlashGraphic : MaskableGraphic {
  public float Radius=12;public float VisualScale=1;
  protected override void OnPopulateMesh(VertexHelper v){v.Clear();var radii=new[]{0f,.28f,.58f,1f};var colors=new[]{new Color32(239,255,255,255),new Color32(139,255,255,255),new Color32(32,205,230,170),new Color32(32,205,230,0)};for(int ring=0;ring<3;ring++)for(int n=0;n<48;n++){float a=n*Mathf.PI*2/48,b=(n+1)*Mathf.PI*2/48;int i=v.currentVertCount;Add(v,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Radius*radii[ring],colors[ring]);Add(v,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*Radius*radii[ring],colors[ring]);Add(v,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*Radius*radii[ring+1],colors[ring+1]);Add(v,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Radius*radii[ring+1],colors[ring+1]);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}var p=new[]{new Vector2(-3,2),new Vector2(4,6),new Vector2(7,2),new Vector2(13,0),new Vector2(7,-2),new Vector2(4,-6),new Vector2(-3,-2)};for(int n=0;n<p.Length;n++){int i=v.currentVertCount;Add(v,Vector2.zero,new Color32(188,255,255,255));Add(v,p[n],new Color32(188,255,255,255));Add(v,p[(n+1)%p.Length],new Color32(188,255,255,255));v.AddTriangle(i,i+1,i+2);}}
  void Add(VertexHelper v,Vector2 p,Color32 c){c.a=(byte)Mathf.RoundToInt(c.a*color.a);v.AddVert(p*VisualScale,c,Vector2.zero);}
 }
}