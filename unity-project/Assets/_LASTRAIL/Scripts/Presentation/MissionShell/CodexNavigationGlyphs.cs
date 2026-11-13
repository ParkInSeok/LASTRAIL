using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Display-only geometry: the RectTransform is the visible ink boundary.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CodexBackChevron : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();
            Vector2 P(float x,float y)=>new Vector2(r.x+x*r.width,r.y+y*r.height);
            void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d){int n=mesh.currentVertCount;foreach(var p in new[]{a,b,c,d})mesh.AddVert(p,color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
            Quad(P(1,1),P(.3f,.5f),P(0,.5f),P(.7f,1));
            Quad(P(.3f,.5f),P(1,0),P(.7f,0),P(0,.5f));
        }
    }

    // Small native document / crossed blades / book symbols. Not finished bitmap art.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CodexTabGlyph : MaskableGraphic
    {
        public int Index;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();
            Vector2 P(float x,float y)=>new Vector2(r.x+x*r.width,r.y+y*r.height);
            void Line(float ax,float ay,float bx,float by,float thickness=3){var a=P(ax,ay);var b=P(bx,by);var n=new Vector2(-(b-a).y,(b-a).x).normalized*thickness*.5f;int start=mesh.currentVertCount;foreach(var p in new[]{a+n,b+n,b-n,a-n})mesh.AddVert(p,color,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);}
            if(Index==0){Line(.2f,.08f,.2f,.92f);Line(.2f,.92f,.65f,.92f);Line(.65f,.92f,.82f,.75f);Line(.82f,.75f,.82f,.08f);Line(.82f,.08f,.2f,.08f);Line(.65f,.92f,.65f,.75f);Line(.65f,.75f,.82f,.75f);foreach(var y in new[]{.61f,.44f,.27f})Line(.32f,y,.69f,y,2);}
            else if(Index==1){Line(.2f,.15f,.8f,.86f,7);Line(.8f,.15f,.2f,.86f,7);Line(.11f,.35f,.37f,.12f,5);Line(.89f,.35f,.63f,.12f,5);Line(.14f,.08f,.26f,.22f,5);Line(.86f,.08f,.74f,.22f,5);}
            else{Line(.5f,.13f,.5f,.83f);Line(.5f,.83f,.12f,.94f);Line(.12f,.94f,.12f,.22f);Line(.12f,.22f,.5f,.13f);Line(.5f,.83f,.88f,.94f);Line(.88f,.94f,.88f,.22f);Line(.88f,.22f,.5f,.13f);Line(.23f,.72f,.4f,.67f,2);Line(.6f,.67f,.77f,.72f,2);}
        }
    }
}
