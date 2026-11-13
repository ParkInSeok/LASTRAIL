using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Resolution-independent line art. IDs follow real ability meanings, not concept placeholder names.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatConceptGlyph:MaskableGraphic {
 public int Symbol; public bool Halo=true;
 Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.x+x*r.width/100,r.yMax-y*r.height/100);}
 void Line(VertexHelper m,float x,float y,float u,float v,float width,Color tint){var a=P(x,y);var b=P(u,v);var d=(b-a).normalized;var n=new Vector2(-d.y,d.x);float half=width*rectTransform.rect.width/200;int i=m.currentVertCount;var clear=tint;clear.a=0;
  // A subpixel alpha fringe removes stair-step edges at the actual small UI size.
  foreach(var p in new[]{a,b}){m.AddVert(p-n*(half+.65f),clear,Vector2.zero);m.AddVert(p-n*half,tint,Vector2.zero);m.AddVert(p+n*half,tint,Vector2.zero);m.AddVert(p+n*(half+.65f),clear,Vector2.zero);}
  for(int j=0;j<3;j++){m.AddTriangle(i+j,i+j+1,i+j+4);m.AddTriangle(i+j+1,i+j+5,i+j+4);}}
 void Path(VertexHelper m,Color c,float w,params float[] p){for(int i=0;i+3<p.Length;i+=2)Line(m,p[i],p[i+1],p[i+2],p[i+3],w,c);}
 void Ring(VertexHelper m,Color c,float w,float x,float y,float r,float from=0,float to=360){for(int i=0;i<28;i++){float a=Mathf.Lerp(from,to,i/28f)*Mathf.Deg2Rad,b=Mathf.Lerp(from,to,(i+1)/28f)*Mathf.Deg2Rad;Line(m,x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r,w,c);}}
 void Draw(VertexHelper m,Color c,float w){switch(Symbol){
 case 0: Path(m,c,w,50,9,80,22,77,53,67,72,50,88,33,72,23,53,20,22,50,9);Path(m,c,w,50,18,70,28,67,52,59,67,50,76,41,67,33,52,30,28,50,18);Path(m,c,w,50,28,50,63);break;
 case 1: for(int i=0;i<3;i++){float dx=i*21,dy=i*9;Path(m,c,w,13+dx,67+dy,40+dx,22+dy,50+dx,14+dy,48+dx,27+dy,22+dx,72+dy,13+dx,67+dy);Line(m,15+dx,62+dy,24+dx,67+dy,w,c);Line(m,7+dx,78+dy,12+dx,70+dy,w,c);}break;
 case 2: Path(m,c,w,38,15,62,15,62,38,85,38,85,62,62,62,62,85,38,85,38,62,15,62,15,38,38,38,38,15);Path(m,c,w,24,51,37,51,44,40,51,63,58,49,77,49);break;
 case 3: for(int i=0;i<3;i++)Ring(m,c,w,50,78,18+i*17,218,322);Ring(m,c,w,50,79,4);Line(m,18,14,83,85,w+1,c);break;
 case 4: Path(m,c,w,67,12,58,30,70,42,89,34,85,52,72,61,59,59,31,89,17,87,12,74,44,45,42,30,50,17,67,12);Ring(m,c,w,24,77,4);break;
 case 5: Path(m,c,w,10,64,10,40,43,40,43,20,66,20,66,42,82,42,89,64,10,64);Path(m,c,w,48,29,59,29,59,41,48,41,48,29);Line(m,19,29,19,40,w,c);Line(m,17,51,37,51,w,c);Ring(m,c,w,24,72,8);Ring(m,c,w,66,72,8);Line(m,3,85,96,85,w,c);Line(m,44,68,44,74,w,c);break;
 case 10: Path(m,c,w,50,8,83,23,79,57,65,78,50,91,35,78,21,57,17,23,50,8);Path(m,c,w,50,18,72,28,68,54,57,71,50,78,43,71,32,54,28,28,50,18);Line(m,50,20,50,77,w,c);break;
 case 11: Path(m,c,w,14,83,25,54,49,32,70,29,71,50,48,74,14,83);Path(m,c,w,20,77,40,57,55,43,65,36);Path(m,c,w,25,54,48,74);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(m,73+Mathf.Cos(a)*9,25+Mathf.Sin(a)*9,73+Mathf.Cos(a)*20,25+Mathf.Sin(a)*20,w,c);}break;
 case 12: Path(m,c,w,40,20,60,20,60,40,80,40,80,60,60,60,60,82,40,82,40,60,20,60,20,40,40,40,40,20);Ring(m,c,w,50,43,39,180,355);Line(m,50,7,50,20,w,c);break;
 case 13: Ring(m,c,w,50,50,29,12,78);Ring(m,c,w,50,50,29,102,168);Ring(m,c,w,50,50,29,192,258);Ring(m,c,w,50,50,29,282,348);Ring(m,c,w,50,50,9);Line(m,50,5,50,31,w,c);Line(m,50,69,50,95,w,c);Line(m,5,50,31,50,w,c);Line(m,69,50,95,50,w,c);break;
 case 14: Path(m,c,w,28,31,72,31,82,43,82,73,68,85,32,85,18,73,18,43,28,31);Ring(m,c,w,50,57,17);Ring(m,c,w,50,57,7);Path(m,c,w,32,12,68,12,68,23,32,23,32,12);Line(m,42,23,42,31,w,c);Line(m,58,23,58,31,w,c);Path(m,c,w,18,45,8,45,8,70,18,70);Path(m,c,w,82,45,92,45,92,70,82,70);break;
 default: Path(m,c,w,50,20,50,89);Path(m,c,w,11,10,43,29,43,43,20,28,11,10);Path(m,c,w,89,10,57,29,57,43,80,28,89,10);Path(m,c,w,24,42,43,52,43,66,29,57,24,42);Path(m,c,w,76,42,57,52,57,66,71,57,76,42);break;
 }}
 protected override void OnPopulateMesh(VertexHelper m){m.Clear();if(Halo){var glow=color;glow.a*=.08f;Draw(m,glow,10);glow.a*=1.7f;Draw(m,glow,6);}Draw(m,color,2.6f);}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatConceptPanel:CombatLayeredGraphic {
 static readonly string[] Names={"Frame","Backplate","StateEffect"};protected override string[] LayerNames=>Names;
 protected override void ConstrainReplacement(string layer,VertexHelper mesh){var r=rectTransform.rect;var hole=CombatUiMeshPartition.Aperture(Inset(r,Hull?7:Metal?2.7f:1.4f),Hull?5:3);if(layer=="Frame")CombatUiMeshPartition.RemoveInterior(mesh,hole);else CombatUiMeshPartition.KeepInterior(mesh,layer=="StateEffect"?CombatUiMeshPartition.Aperture(new Rect(r.x+3,r.y+3,r.width-6,3),1):hole);}
 protected override void DrawLayer(string layer,VertexHelper mesh){var r=rectTransform.rect;if(layer=="StateEffect"){if(Gold){var tint=new Color(1,.77f,.33f,.12f*Light);Face(mesh,new Rect(r.x+3,r.y+3,r.width-6,3),1,tint,tint);}return;}using(var complete=new VertexHelper()){DrawComplete(complete);float inset=Hull?7:Metal?2.7f:1.4f;float bevel=Hull?5:3;CombatUiMeshPartition.Copy(complete,mesh,CombatUiMeshPartition.Aperture(Inset(r,inset),bevel),layer=="Backplate");}}
 public bool Gold,Metal,Hull;public float Light=.6f;
 void Face(VertexHelper m,Rect r,float b,Color low,Color high){var p=new[]{new Vector2(r.x+b,r.y),new Vector2(r.xMax-b,r.y),new Vector2(r.xMax,r.y+b),new Vector2(r.xMax,r.yMax-b),new Vector2(r.xMax-b,r.yMax),new Vector2(r.x+b,r.yMax),new Vector2(r.x,r.yMax-b),new Vector2(r.x,r.y+b)};int n=m.currentVertCount;m.AddVert(r.center,Color.Lerp(low,high,.5f),Vector2.zero);foreach(var v in p)m.AddVert(v,Color.Lerp(low,high,(v.y-r.y)/r.height),Vector2.zero);for(int i=0;i<8;i++)m.AddTriangle(n,n+1+i,n+1+(i+1)%8);}
 Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,r.width-2*n,r.height-2*n);
 void DrawComplete(VertexHelper m){m.Clear();var r=rectTransform.rect;var edge=Gold?new Color(.9f,.67f,.26f):new Color(.24f,.37f,.42f);Face(m,r,4,edge*.55f,edge);Face(m,Inset(r,1.4f),3,new Color(.018f,.026f,.03f),Gold?new Color(.085f,.069f,.025f):new Color(.043f,.065f,.075f));if(Metal){Face(m,r,Hull?8:5,new Color(.12f,.16f,.18f),new Color(.49f,.55f,.57f));Face(m,Inset(r,1.2f),Hull?7:4,new Color(.16f,.2f,.22f),new Color(.045f,.072f,.082f));Face(m,Inset(r,Hull?4:2.7f),Hull?5:3,new Color(.01f,.025f,.031f,.96f),new Color(.025f,.047f,.055f,.92f));var cyan=new Color(.13f,.56f,.66f,.6f);Face(m,new Rect(r.x+10,r.y+1.6f,r.width-20,1),0,cyan,cyan);if(Hull)foreach(float x in new[]{r.x+3,r.xMax-5}){Face(m,new Rect(x,r.center.y-1,2,2),.5f,new Color(.03f,.04f,.045f),new Color(.54f,.57f,.55f));}}
  if(Hull){
   // Keep the chamfered metal faces above visible; the former full-rect face hid them.
   var inner=Inset(r,7);Face(m,inner,5,new Color(.014f,.025f,.029f),new Color(.026f,.039f,.044f));
   var seam=new Color(.08f,.18f,.20f);Face(m,new Rect(inner.x+6,inner.y+1,inner.width-12,.7f),0,seam,seam);
   foreach(float x in new[]{r.x+5,r.xMax-8})foreach(float y in new[]{r.y+7,r.yMax-10}){
    Face(m,new Rect(x-1,y-1,5,5),1.6f,new Color(.008f,.012f,.014f),new Color(.10f,.13f,.14f));
    Face(m,new Rect(x,y,3,3),1,new Color(.08f,.09f,.085f),new Color(.48f,.47f,.40f));
    Face(m,new Rect(x+.7f,y+.7f,1.4f,1.4f),.4f,new Color(.02f,.025f,.026f),new Color(.08f,.095f,.10f));
   }
  }
 }
}
}
