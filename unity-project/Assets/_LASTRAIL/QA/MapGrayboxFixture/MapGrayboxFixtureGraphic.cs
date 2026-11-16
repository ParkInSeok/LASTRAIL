using System;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Qa {
 [ExecuteAlways,RequireComponent(typeof(CanvasRenderer))] public sealed class MapGrayboxFixtureGraphic:MaskableGraphic {
  public string Layer="paths",Module="";
  MapGrayboxFixtureGeometry geometry;MapGrayboxFixtureRun model;
  public void Bind(MapGrayboxFixtureGeometry g,MapGrayboxFixtureRun r){geometry=g;model=r;raycastTarget=false;SetVerticesDirty();}
  public void Unbind(){geometry=null;model=null;SetVerticesDirty();}
  public Vector2 Project(MapGrayboxFixtureGeometry.Point p){var r=rectTransform.rect;float span=string.IsNullOrEmpty(Module)?1300:700;float scale=Mathf.Min(r.width/span,r.height/450f);return new Vector2(r.center.x-span*.5f*scale+50*scale+(float)p.X/1000*scale,r.center.y+(float)p.Y/1000*scale);}
  protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();if(geometry==null)return;
   if(!string.IsNullOrEmpty(Module)){var path=geometry.PathFor(Module,0);Draw(mesh,path,new Color(.35f,.55f,.64f),3);if(Module.StartsWith("FORK_"))Draw(mesh,geometry.PathFor(Module,1),new Color(.1f,.85f,.8f),3);return;}
   if(model==null)return;
   if(Layer=="paths"){for(int route=0;route<2;route++)Draw(mesh,geometry.Assemble(new[]{"FORK_ROCKFALL","SAFE_STRAIGHT"},new[]{route,0}),route==model.Route?new Color(.05f,.85f,1):new Color(.22f,.32f,.39f),route==model.Route?7:4);return;}
   if(Layer=="hazard"){var direct=geometry.PathFor("FORK_ROCKFALL",0);var color=model.Rock=="falling"?new Color(1,.2f,.12f):model.Rock=="consumed"||model.Rock=="destroyed"||model.Rock=="skipped"?new Color(.3f,.38f,.4f):new Color(1,.65f,.18f);
    for(int d=260000;d<340000;d+=4000)Line(mesh,Project(MapGrayboxFixtureGeometry.At(direct,d)),Project(MapGrayboxFixtureGeometry.At(direct,d+4000)),22,color);
    foreach(int distance in new[]{40000,80000}){var p=Project(MapGrayboxFixtureGeometry.At(model.Path,distance));Line(mesh,p+Vector2.up*30,p-Vector2.up*30,3,distance==40000?Color.yellow:Color.magenta);}return;}
   if(Layer=="train"){for(int i=0;i<5;i++){double d=model.Distance-i*22000;var center=Project(MapGrayboxFixtureGeometry.At(model.Path,d));var ahead=Project(MapGrayboxFixtureGeometry.At(model.Path,d+1000));var direction=(ahead-center).normalized;var sideways=new Vector2(-direction.y,direction.x);float scale=Mathf.Min(rectTransform.rect.width/1300,rectTransform.rect.height/450);float half=9*scale;float height=7*scale;Quad(mesh,center-direction*half-sideways*height,center-direction*half+sideways*height,center+direction*half+sideways*height,center+direction*half-sideways*height,i==0?Color.white:new Color(.1f,.9f,.7f));}return;}
  }
  void Draw(VertexHelper mesh,MapGrayboxFixtureGeometry.Point[] points,Color color,float width){for(int i=1;i<points.Length;i++)Line(mesh,Project(points[i-1]),Project(points[i]),width,color);}
  static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*width*.5f;Quad(mesh,a-p,a+p,b+p,b-p,color);}
  static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){int index=mesh.currentVertCount;mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(b,color,Vector2.up);mesh.AddVert(c,color,Vector2.one);mesh.AddVert(d,color,Vector2.right);mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index+2,index+3,index);}
 }
}
