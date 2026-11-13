using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// New native repeating ballast, not a claim that the supplied canyon PNG tiles.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatTravelGround:MaskableGraphic {
 public const float Period=384,Speed=130;
 public float Phase{get;private set;}
 public bool Foreground;public Texture2D TerrainTexture;
 public override Texture mainTexture=>TerrainTexture!=null?TerrainTexture:Texture2D.whiteTexture;
 public void SetDistance(float pixels){Phase=Mathf.Repeat(pixels,Period);SetVerticesDirty();}
 Vector2 Point(float x,float depth){var r=rectTransform.rect;return new Vector2(r.xMin+x,r.yMax-(223.86f+x*.0606667f+depth));}
 static float Pattern(int n)=>Mathf.Repeat(Mathf.Sin(n*17.317f+3.9f)*43758.5f,1);
 void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color top,Color bottom,Rect? sample=null){var uv=sample??new Rect(.39f,.045f,.035f,.04f);int n=vh.currentVertCount;vh.AddVert(a,top,new Vector2(uv.xMin,uv.yMax));vh.AddVert(b,top,new Vector2(uv.xMax,uv.yMax));vh.AddVert(c,bottom,new Vector2(uv.xMax,uv.yMin));vh.AddVert(d,bottom,new Vector2(uv.xMin,uv.yMin));vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
 void Floor(VertexHelper vh,float start){
  // A narrow existing dirt patch is projected on overlapping native modules.
  // Alpha collars hide the non-seamless source edge; the cliff/road image is not tiled.
  const int columns=18;const float width=432;
  for(int col=0;col<columns;col++){
   float t0=col/(float)columns,t1=(col+1f)/columns;float x=start+t0*width,x1=start+t1*width;
   float a0=Mathf.Clamp01(Mathf.Min(t0,1-t0)*18),a1=Mathf.Clamp01(Mathf.Min(t1,1-t1)*18);
   for(int row=0;row<2;row++){
    float top=row==0?-15:-5,bottom=row==0?-5:65;float v0=row==0?.16f:.14f,v1=row==0?.14f:.015f;
    int n=vh.currentVertCount;float topAlpha=row==0?0:1;
    vh.AddVert(Point(x,top),new Color(.9f,.87f,.82f,a0*topAlpha),new Vector2(.22f+t0*.30f,v0));
    vh.AddVert(Point(x1,top),new Color(.9f,.87f,.82f,a1*topAlpha),new Vector2(.22f+t1*.30f,v0));
    vh.AddVert(Point(x1,bottom),new Color(.9f,.87f,.82f,a1),new Vector2(.22f+t1*.30f,v1));
    vh.AddVert(Point(x,bottom),new Color(.9f,.87f,.82f,a0),new Vector2(.22f+t0*.30f,v1));
    vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
   }
  }
 }
 void Stone(VertexHelper vh,float x,float depth,float size,float tint){
  var p=Point(x,depth);var dark=new Color(.11f,.09f,.075f,.95f);var side=new Color(.56f+tint,.52f+tint,.46f+tint,.96f);var light=new Color(.9f+tint,.85f+tint,.76f+tint,.96f);
  Quad(vh,p+new Vector2(-size,-size*.14f),p+new Vector2(size,-size*.12f),p+new Vector2(size*1.3f,-size*.45f),p+new Vector2(-size*.8f,-size*.4f),dark,dark);
  Quad(vh,p+new Vector2(-size*.7f,size*.55f),p+new Vector2(size*.55f,size*.4f),p+new Vector2(size,-size*.12f),p+new Vector2(-size,-size*.14f),light,side);
 }
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();if(!Foreground)for(int module=-1;module<=3;module++)Floor(vh,module*Period-Phase-24);
  // Every module uses the identical deterministic pattern, with one-module overscan.
  for(int module=-1;module<=3;module++)for(int i=0;i<(Foreground?18:100);i++){
   int seed=i+(Foreground?400:0);float x=module*Period+Pattern(seed*3+1)*Period-Phase;
   float depth=Foreground?17+Pattern(seed*3+2)*37:-9+Pattern(seed*3+2)*46;
   float size=Foreground?3+Pattern(seed*3+3)*7:.65f+Pattern(seed*3+3)*2.4f;
   Stone(vh,x,depth,size,Pattern(seed+71)*.07f);
  }
 }
}
}
