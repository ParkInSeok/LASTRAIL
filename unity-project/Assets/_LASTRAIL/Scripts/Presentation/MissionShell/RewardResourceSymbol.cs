using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    // Known reward IDs only. Unknown ITEMs never become a guessed chest.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RewardResourceSymbol : MaskableGraphic
    {
        public string Kind;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;Vector2 P(float x,float y)=>new Vector2(r.x+x*r.width,r.y+y*r.height);
            void Poly(Color tint,params Vector2[] points){int start=mesh.currentVertCount;foreach(var p in points)mesh.AddVert(p,tint,Vector2.zero);for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);}
            // Provisional native symbols for these known IDs only. Replace their geometry when
            // corresponding independent artwork is approved; never infer an unknown item's icon.
            void Line(Color tint,Vector2 a,Vector2 b)
            {var normal=new Vector2(-(b-a).y,(b-a).x).normalized*Mathf.Min(r.width,r.height)*.02f;Poly(tint,a-normal,b-normal,b+normal,a+normal);}
            if(Kind=="cur_tactical_data")
            {
                var edge=new Color(.08f,.17f,.21f);var ink=new Color(.67f,.86f,.9f);
                Vector2[] page={P(.20f,.10f),P(.82f,.10f),P(.82f,.71f),P(.64f,.90f),P(.20f,.90f)};
                Poly(new Color(.30f,.43f,.49f),page);for(int i=0;i<page.Length;i++)Line(edge,page[i],page[(i+1)%page.Length]);
                Poly(new Color(.63f,.73f,.75f),P(.64f,.9f),P(.64f,.71f),P(.82f,.71f));
                var nodes=new[]{P(.35f,.28f),P(.66f,.42f),P(.38f,.65f)};Line(ink,nodes[0],nodes[1]);Line(ink,nodes[1],nodes[2]);
                foreach(var node in nodes){var points=new Vector2[20];for(int i=0;i<20;i++){float a=i*Mathf.PI/10;points[i]=node+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Min(r.width,r.height)*.052f;}Poly(ink,points);}return;
            }
            if(Kind=="cur_operator_exp")
            {
                var ink=new Color(.29f,.24f,.17f);Vector2[] left={P(.12f,.22f),P(.48f,.14f),P(.48f,.82f),P(.12f,.90f)};
                Vector2[] right={P(.52f,.14f),P(.88f,.22f),P(.88f,.90f),P(.52f,.82f)};
                Poly(new Color(.74f,.69f,.55f),left);Poly(new Color(.9f,.84f,.70f),right);
                foreach(var page in new[]{left,right})for(int i=0;i<page.Length;i++)Line(ink,page[i],page[(i+1)%page.Length]);
                Line(ink,P(.5f,.14f),P(.5f,.83f));Line(ink,P(.2f,.68f),P(.4f,.63f));Line(ink,P(.2f,.49f),P(.4f,.44f));Line(ink,P(.6f,.63f),P(.8f,.68f));Line(ink,P(.6f,.44f),P(.8f,.49f));return;
            }
            if(Kind=="CHECK")
            {
                void Stroke(Vector2 a,Vector2 b){var normal=new Vector2(-(b-a).y,(b-a).x).normalized*Mathf.Min(r.width,r.height)*.10f;Poly(new Color(.1f,.86f,1),a-normal,b-normal,b+normal,a+normal);}
                Stroke(P(.08f,.46f),P(.38f,.18f));Stroke(P(.38f,.18f),P(.91f,.86f));return;
            }
            if(Kind=="CRYSTAL"){Poly(new Color(.06f,.38f,.92f),P(.46f,1),P(.83f,.58f),P(.52f,0),P(.15f,.41f));Poly(new Color(.2f,.83f,1),P(.46f,1),P(.50f,.45f),P(.15f,.41f));Poly(new Color(.65f,.92f,1),P(.46f,1),P(.83f,.58f),P(.50f,.45f));return;}
            if(Kind=="cur_credits"){var points=new Vector2[24];for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;points[i]=P(.5f+Mathf.Cos(a)*.44f,.5f+Mathf.Sin(a)*.44f);}Poly(new Color(.64f,.48f,.27f),points);Poly(new Color(.94f,.78f,.48f),P(.22f,.45f),P(.78f,.45f),P(.78f,.55f),P(.22f,.55f));return;}
            if(Kind=="cur_machine_parts"){var tint=new Color(.59f,.65f,.67f);Poly(tint,P(.08f,.1f),P(.91f,.1f),P(.91f,.31f),P(.08f,.31f));Poly(tint,P(.37f,.31f),P(.62f,.31f),P(.62f,.70f),P(.37f,.70f));Poly(tint,P(.08f,.70f),P(.91f,.70f),P(.91f,.92f),P(.08f,.92f));}
        }
    }
}
