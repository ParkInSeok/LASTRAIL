using LastRail.Battle;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Auth {
// Shared historical atlas, independently replaceable frame, plate and lamp geometry.
// No authentication state or command ownership. The Button drives inherited tint.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AuthButtonSurface : CombatLayeredGraphic {
 static readonly string[] Roles={"Frame","Backplate","StateLights"};
 protected override string[] LayerNames=>Roles;
 Texture2D atlas;bool primary;
 protected override Texture LayerTexture=>atlas!=null?atlas:Texture2D.whiteTexture;
 // Selectable animates CanvasRenderer tint; the parent has no visible mesh.
 public override void CrossFadeColor(Color targetColor,float duration,bool ignoreTimeScale,bool useAlpha)
 {GetLayer("Backplate").CrossFadeColor(targetColor,duration,ignoreTimeScale,useAlpha);GetLayer("StateLights").CrossFadeColor(targetColor,duration,ignoreTimeScale,useAlpha);}
 public void Configure(Texture2D texture,bool emphasized){atlas=texture;primary=emphasized;foreach(string role in Roles)GetLayer(role).SetMaterialDirty();SetVerticesDirty();}
 Vector2[] Polygon(float left,float bottom,float right,float top,float bevel){var r=rectTransform.rect;return CombatUiMeshPartition.Aperture(new Rect(r.x+left*r.width,r.y+bottom*r.height,(right-left)*r.width,(top-bottom)*r.height),bevel*r.height);}
 Vector2[] Plate=>Polygon(primary?.137f:.132f,.12f,.919f,.755f,.11f);
 Vector2[] Lamp(bool right)=>Polygon(right?.944f:.065f,.16f,right?.988f:.105f,.75f,0);
 protected override void ConstrainReplacement(string role,VertexHelper mesh){if(role=="Backplate")CombatUiMeshPartition.KeepInterior(mesh,Plate);else if(role=="Frame"){CombatUiMeshPartition.RemoveInterior(mesh,Plate);CombatUiMeshPartition.RemoveInterior(mesh,Lamp(false));CombatUiMeshPartition.RemoveInterior(mesh,Lamp(true));}else KeepLamps(mesh);}
 void KeepLamps(VertexHelper mesh){using(var left=new VertexHelper())using(var right=new VertexHelper()){CombatUiMeshPartition.Copy(mesh,left,Lamp(false),true);CombatUiMeshPartition.Copy(mesh,right,Lamp(true),true);var stream=new System.Collections.Generic.List<UIVertex>();left.GetUIVertexStream(stream);var other=new System.Collections.Generic.List<UIVertex>();right.GetUIVertexStream(other);stream.AddRange(other);mesh.Clear();mesh.AddUIVertexTriangleStream(stream);}}
 protected override void DrawLayer(string role,VertexHelper mesh){mesh.Clear();var r=rectTransform.rect;var tint=role=="Frame"?Color.white:color;mesh.AddVert(new Vector2(r.x,r.y),tint,new Vector2(0,0));mesh.AddVert(new Vector2(r.xMax,r.y),tint,new Vector2(1,0));mesh.AddVert(new Vector2(r.xMax,r.yMax),tint,new Vector2(1,1));mesh.AddVert(new Vector2(r.x,r.yMax),tint,new Vector2(0,1));mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);ConstrainReplacement(role,mesh);}
}
}
