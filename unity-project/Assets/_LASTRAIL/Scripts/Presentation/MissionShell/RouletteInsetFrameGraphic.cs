using LastRail.Auth;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Independent empty frame: the plate, icon, label and input remain separate objects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouletteInsetFrameGraphic : MaskableGraphic
    {
        bool control;
        public static void Create(Transform parent, bool control)
        {
            var frame=AuthFormVisuals.Rect("InsetEquipmentFrame",parent).gameObject.AddComponent<RouletteInsetFrameGraphic>();
            frame.control=control;frame.raycastTarget=false;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            if(r.width<40||r.height<40)return;
            float cut=control?18:11;
            Vector2[] Contour(float inset)
            {
                float l=r.xMin+inset, rr=r.xMax-inset, b=r.yMin+inset, t=r.yMax-inset;
                float c=Mathf.Max(3,cut-inset*.35f);
                return new[]{new Vector2(l+c,t),new Vector2(rr-c,t),new Vector2(rr,t-c),new Vector2(rr,b+c),new Vector2(rr-c,b),new Vector2(l+c,b),new Vector2(l,b+c),new Vector2(l,t-c)};
            }
            void Ring(float outside,float inside,Color lit,Color shadow)
            {
                var a=Contour(outside);var b=Contour(inside);
                for(int i=0;i<8;i++)
                {
                    int j=(i+1)%8,v=mesh.currentVertCount;
                    var shade=i==0||i==7?lit:i>=3&&i<=5?shadow:Color.Lerp(lit,shadow,.5f);
                    mesh.AddVert(a[i],shade,Vector2.zero);mesh.AddVert(a[j],shade,Vector2.zero);
                    mesh.AddVert(b[j],shade,Vector2.zero);mesh.AddVert(b[i],shade,Vector2.zero);
                    mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);
                }
            }
            Ring(0,3,new Color(.035f,.04f,.045f),Color.black);
            Ring(3,4.5f,new Color(.69f,.60f,.41f),new Color(.19f,.16f,.10f));
            float rim=control?19:10;
            Ring(4.5f,rim,new Color(.24f,.27f,.28f),new Color(.075f,.085f,.095f));
            Ring(rim,rim+1.5f,new Color(.60f,.52f,.36f),new Color(.32f,.28f,.19f));
            Ring(rim+1.5f,rim+3,new Color(.015f,.02f,.025f),new Color(.02f,.025f,.03f));
            void Scratch(Vector2 a,Vector2 b,float thickness,Color shade)
            {
                var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*thickness*.5f;
                int v=mesh.currentVertCount;
                mesh.AddVert(a-n,shade,Vector2.zero);mesh.AddVert(b-n,shade,Vector2.zero);
                mesh.AddVert(b+n,shade,Vector2.zero);mesh.AddVert(a+n,shade,Vector2.zero);
                mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);
            }
            // Fixed material wear is confined to the metal band; no randomness or state changes on rebuild.
            for(int i=0;i<13;i++)
            {
                float t=(i+.35f)/13, phase=(i*7%11)/11f;
                float x=Mathf.Lerp(r.xMin+cut+6,r.xMax-cut-16,t);
                float length=3+phase*8, inset=5+phase*(rim-6);
                var top=new Vector2(x,r.yMax-inset);
                Scratch(top+Vector2.down*.8f,top+new Vector2(length,-1.3f),1.8f,new Color(.015f,.02f,.022f,.8f));
                Scratch(top,top+new Vector2(length,-.5f),.8f,new Color(.70f,.59f,.39f,.58f));
                if(i%3==0)
                {
                    var bottom=new Vector2(x+4,r.yMin+inset);
                    Scratch(bottom,bottom+new Vector2(length*.7f,1),1,new Color(.43f,.37f,.26f,.55f));
                }
            }
            for(int i=0;i<7;i++)
            {
                float y=Mathf.Lerp(r.yMin+cut+10,r.yMax-cut-10,(i+.4f)/7);
                float inset=5+(i%3)*(rim-6)/3;
                Scratch(new Vector2(r.xMin+inset,y),new Vector2(r.xMin+inset+1,y+4+i%3),.8f,new Color(.63f,.55f,.38f,.55f));
                Scratch(new Vector2(r.xMax-inset,y),new Vector2(r.xMax-inset-1,y+3),1.2f,new Color(.015f,.018f,.02f,.75f));
            }
            void Disc(Vector2 center,float radius,Color shade)
            {
                int first=mesh.currentVertCount;mesh.AddVert(center,shade,Vector2.zero);
                const int n=12;
                for(int k=0;k<=n;k++){float a=k*Mathf.PI*2/n;mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,shade,Vector2.zero);if(k>0)mesh.AddTriangle(first,first+k,first+k+1);}
            }
            float offset=control?13:9;
            foreach(float x in new[]{r.xMin+offset,r.xMax-offset})
                foreach(float y in new[]{r.yMin+offset,r.yMax-offset})
                {
                    var p=new Vector2(x,y);Disc(p,control?4.8f:3.2f,new Color(.66f,.58f,.42f));
                    Disc(p+new Vector2(.3f,-.5f),control?3.2f:2,new Color(.045f,.055f,.06f));
                    Disc(p+new Vector2(-.7f,.8f),.8f,new Color(.78f,.77f,.64f));
                }
        }
    }
}
