using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Independent display element; the owning view supplies pause state.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatHeaderGlyph : MaskableGraphic {
 public enum Symbol { Speed, Pause, Play }
 Symbol symbol;
 public void SetSymbol(Symbol value){if(symbol==value)return;symbol=value;SetVerticesDirty();}
 void Polygon(VertexHelper mesh, params Vector2[] points){int n=mesh.currentVertCount;var r=rectTransform.rect;foreach(var p in points){var tint=Color.Lerp(new Color(.08f,.65f,.8f),new Color(.65f,.96f,1),p.y);mesh.AddVert(new Vector2(r.x+p.x*r.width,r.y+p.y*r.height),tint*color,Vector2.zero);}for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(n,n+i,n+i+1);}
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();if(symbol==Symbol.Play){Polygon(mesh,new Vector2(.22f,.08f),new Vector2(.85f,.5f),new Vector2(.22f,.92f));return;}if(symbol==Symbol.Pause){foreach(float x in new[]{.2f,.62f})Polygon(mesh,new Vector2(x,.08f),new Vector2(x+.18f,.08f),new Vector2(x+.18f,.92f),new Vector2(x,.92f));return;}foreach(float x in new[]{0f,.55f}){Polygon(mesh,new Vector2(x,.05f),new Vector2(x+.28f,.05f),new Vector2(x+.45f,.5f),new Vector2(x+.17f,.5f));Polygon(mesh,new Vector2(x+.17f,.5f),new Vector2(x+.45f,.5f),new Vector2(x+.28f,.95f),new Vector2(x,.95f));}}
}
}
