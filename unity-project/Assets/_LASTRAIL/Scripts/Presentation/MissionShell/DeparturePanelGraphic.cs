using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Presentation-only meshes: each independently controlled layer owns no game state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeparturePanelGraphic : MaskableGraphic
    {
        public enum Shape { Frame, Plate, Rail, Lock, Role, TrainHeading, SupportHeading, AmberLamp, Coupler, Badge }
        Shape shape;
        string role;
        public void Configure(Shape value,Color tint,string roleId=null){raycastTarget=false;if(shape==value&&role==roleId&&color==tint)return;shape=value;role=roleId;color=tint;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            Vector2 P(float x,float y)=>new Vector2(r.xMin+x,r.yMax-y);
            void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
            {
                int index=mesh.currentVertCount;mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(c,tint,Vector2.zero);mesh.AddVert(d,tint,Vector2.zero);mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
            }
            void Line(float x1,float y1,float x2,float y2,float width,Color tint)
            {
                var a=P(x1,y1);var b=P(x2,y2);var delta=b-a;var side=new Vector2(-delta.y,delta.x).normalized*width*.5f;Quad(a-side,a+side,b+side,b-side,tint);
            }
            void Box(float x,float y,float width,float height,Color tint)=>Quad(P(x,y),P(x+width,y),P(x+width,y+height),P(x,y+height),tint);
            void Disc(float x,float y,float radius,Color tint)
            {
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;Quad(P(x,y),P(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),P(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),P(x,y),tint);}
            }
            void Ring(float inset,float width,Color tint)
            {
                float l=inset,t=inset,rr=r.width-inset,b=r.height-inset,k=5;
                var points=new[]{new Vector2(l+k,t),new Vector2(rr-k,t),new Vector2(rr,t+k),new Vector2(rr,b-k),new Vector2(rr-k,b),new Vector2(l+k,b),new Vector2(l,b-k),new Vector2(l,t+k)};
                for(int i=0;i<points.Length;i++){var a=points[i];var q=points[(i+1)%points.Length];Line(a.x,a.y,q.x,q.y,width,tint);}
            }
            if(shape==Shape.Frame)
            {
                Ring(4,9,new Color(color.r,color.g,color.b,.10f*color.a));Ring(4,5,new Color(color.r,color.g,color.b,.23f*color.a));Ring(4,1.8f,color);Ring(8,.8f,new Color(.48f,.83f,1,.65f*color.a));
                return;
            }
            if(shape==Shape.Plate)
            {
                for(int i=0;i<16;i++){float level=Mathf.Lerp(1,.4f,i/15f);Box(0,i*r.height/16,r.width,r.height/16+.1f,new Color(color.r*level,color.g*level,color.b*level,color.a));}
                for(float x=10;x<r.width;x+=18)Line(x,0,x,r.height,.7f,new Color(.15f,.5f,.75f,.13f*color.a));
                for(float y=8;y<r.height;y+=18)Line(0,y,r.width,y,.7f,new Color(.15f,.5f,.75f,.13f*color.a));
                Box(0,r.height-31,r.width,31,new Color(.005f,.012f,.02f,.88f*color.a));return;
            }
            if(shape==Shape.Badge)
            {
                Box(0,0,r.width,r.height,new Color(.008f,.025f,.045f,1));Line(0,0,r.width,0,1,color);Line(r.width,0,r.width,r.height,1,color);Line(r.width,r.height,0,r.height,1,color);Line(0,r.height,0,0,1,color);return;
            }
            if(shape==Shape.Rail){Box(0,2,r.width,3,new Color(.36f,.30f,.19f,color.a));Box(0,7,r.width,2,new Color(.06f,.08f,.09f,color.a));return;}
            float w=r.width,h=r.height;
            if(shape==Shape.AmberLamp)
            {
                Disc(w*.5f,h*.5f,w*.8f,new Color(1,.35f,.02f,.07f));Disc(w*.5f,h*.5f,w*.5f,new Color(1,.47f,.08f,.17f));
                Box(w*.22f,h*.1f,w*.56f,h*.8f,new Color(.24f,.15f,.05f,1));Box(w*.32f,h*.22f,w*.36f,h*.56f,new Color(1,.62f,.14f,1));Box(w*.42f,h*.27f,w*.14f,h*.46f,new Color(1,.92f,.58f,1));
                Line(w*.18f,h*.18f,w*.82f,h*.18f,1,new Color(.45f,.29f,.1f,1));Line(w*.18f,h*.83f,w*.82f,h*.83f,1,new Color(.45f,.29f,.1f,1));return;
            }
            if(shape==Shape.Coupler)
            {
                Box(0,h*.2f,w,h*.6f,new Color(.13f,.15f,.15f,1));Line(0,h*.2f,w,h*.2f,1,new Color(.47f,.4f,.25f,1));Box(w*.36f,0,w*.28f,h,new Color(.32f,.29f,.22f,1));return;
            }
            if(shape==Shape.TrainHeading)
            {
                Box(w*.1f,h*.36f,w*.48f,h*.27f,color);Box(w*.55f,h*.10f,w*.3f,h*.53f,color);Box(w*.2f,h*.16f,w*.1f,h*.2f,color);
                Box(w*.6f,h*.17f,w*.17f,h*.22f,new Color(.02f,.04f,.05f,1));Line(w*.04f,h*.69f,w*.92f,h*.69f,2,color);
                for(int i=0;i<3;i++){Disc(w*(.22f+i*.25f),h*.79f,h*.12f,color);Disc(w*(.22f+i*.25f),h*.79f,h*.045f,new Color(.02f,.04f,.05f,1));}return;
            }
            if(shape==Shape.SupportHeading)
            {
                var points=new Vector2[10];for(int i=0;i<10;i++){float angle=(90+i*36)*Mathf.Deg2Rad;float radius=i%2==0?.44f:.20f;points[i]=new Vector2(w*.5f+Mathf.Cos(angle)*w*radius,h*.45f-Mathf.Sin(angle)*h*radius);}
                for(int i=0;i<10;i++){var a=points[i];var b=points[(i+1)%10];Line(a.x,a.y,b.x,b.y,2,color);}
                for(int i=0;i<10;i++){var a=points[i];var b=points[(i+1)%10];Quad(P(w*.5f,h*.45f),P(a.x,a.y),P(b.x,b.y),P(w*.5f,h*.45f),color);}
                Line(w*.05f,h*.45f,w*.12f,h*.8f,2,color);Line(w*.12f,h*.8f,w*.5f,h,2,color);Line(w*.5f,h,w*.88f,h*.8f,2,color);Line(w*.88f,h*.8f,w*.95f,h*.45f,2,color);return;
            }
            if(shape==Shape.Lock)
            {
                Line(w*.25f,h*.42f,w*.25f,h*.2f,2.5f,color);Line(w*.25f,h*.2f,w*.4f,h*.08f,2.5f,color);Line(w*.4f,h*.08f,w*.6f,h*.08f,2.5f,color);Line(w*.6f,h*.08f,w*.75f,h*.2f,2.5f,color);Line(w*.75f,h*.2f,w*.75f,h*.42f,2.5f,color);Box(w*.15f,h*.4f,w*.7f,h*.55f,color);Box(w*.46f,h*.54f,w*.08f,h*.24f,new Color(.03f,.04f,.05f,1));return;
            }
            if(role=="GUN"){Line(w*.15f,h*.75f,w*.85f,h*.15f,6,color);Line(w*.22f,h*.83f,w*.42f,h*.60f,9,color);}
            else if(role=="COMBAT"){Line(w*.2f,h*.1f,w*.8f,h*.9f,3,color);Line(w*.8f,h*.1f,w*.2f,h*.9f,3,color);Line(w*.1f,h*.6f,w*.4f,h*.9f,2,color);Line(w*.6f,h*.9f,w*.9f,h*.6f,2,color);}
            else if(role=="MEDICAL"){Box(w*.36f,0,w*.28f,h,color);Box(0,h*.36f,w,h*.28f,color);}
            else if(role=="DEFENSE"){Line(w*.1f,0,w*.9f,0,3,color);Line(w*.1f,0,w*.2f,h*.7f,3,color);Line(w*.9f,0,w*.8f,h*.7f,3,color);Line(w*.2f,h*.7f,w*.5f,h,3,color);Line(w*.8f,h*.7f,w*.5f,h,3,color);}
            else if(role=="CARGO"){Box(0,0,w,h,color);Box(w*.45f,0,w*.1f,h,new Color(.1f,.15f,.17f,1));}
        }
    }
}
