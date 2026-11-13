using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle
{
    // Role identification only; these shapes never encode availability or effect magnitude.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SupportRoleGlyph:MaskableGraphic
    {
        public string Role;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(string.IsNullOrEmpty(Role))return;var rect=rectTransform.rect;
            Vector2 P(float x,float y)=>new Vector2(rect.xMin+x*rect.width,rect.yMin+y*rect.height);
            void Line(float x,float y,float a,float b){var p=P(x,y);var q=P(a,b);var n=new Vector2(-(q-p).y,(q-p).x).normalized*rect.width*.035f;int i=vh.currentVertCount;var tint=new Color(.55f,.88f,1);vh.AddVert(p+n,tint,Vector2.zero);vh.AddVert(q+n,tint,Vector2.zero);vh.AddVert(q-n,tint,Vector2.zero);vh.AddVert(p-n,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
            void Circle(float x,float y,float r){for(int i=0;i<20;i++){float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;Line(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r);}}
            if(Role.EndsWith("medical_support")){Line(.5f,.15f,.5f,.85f);Line(.15f,.5f,.85f,.5f);}
            else if(Role.EndsWith("smoke")){Circle(.3f,.4f,.18f);Circle(.52f,.62f,.22f);Circle(.76f,.42f,.17f);Line(.17f,.2f,.86f,.2f);}
            else if(Role.EndsWith("weakness_scan")){Circle(.5f,.5f,.28f);Line(.5f,.05f,.5f,.3f);Line(.5f,.7f,.5f,.95f);Line(.05f,.5f,.3f,.5f);Line(.7f,.5f,.95f,.5f);}
            else if(Role.EndsWith("firepower_boost")){Line(.5f,.15f,.5f,.85f);Line(.23f,.58f,.5f,.85f);Line(.5f,.85f,.77f,.58f);Line(.2f,.15f,.2f,.4f);Line(.8f,.15f,.8f,.4f);}
            else if(Role.EndsWith("electronic_disruption")){Line(.65f,.9f,.35f,.52f);Line(.35f,.52f,.65f,.52f);Line(.65f,.52f,.35f,.1f);Line(.12f,.3f,.12f,.7f);Line(.88f,.3f,.88f,.7f);}
            else if(Role.EndsWith("emergency_protection")){Line(.2f,.82f,.5f,.92f);Line(.5f,.92f,.8f,.82f);Line(.8f,.82f,.75f,.35f);Line(.75f,.35f,.5f,.1f);Line(.5f,.1f,.25f,.35f);Line(.25f,.35f,.2f,.82f);Line(.5f,.35f,.5f,.7f);}
            else{for(int i=0;i<3;i++){float x=.2f+i*.3f;Line(x,.86f,x,.3f);Line(x-.1f,.43f,x,.25f);Line(x,.25f,x+.1f,.43f);}Line(.1f,.1f,.9f,.1f);}
        }
    }
}
