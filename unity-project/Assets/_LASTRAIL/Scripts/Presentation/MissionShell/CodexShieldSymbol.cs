using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Owns display layers only. The texture is borrowed from Resources.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CodexShieldSymbol : MaskableGraphic
    {
        public void Configure(Texture texture, bool activeRays)
        {
            raycastTarget=false;
            AddLayer("SteelBevel",CodexShieldLayer.Role.Bevel,null);
            AddLayer("MetalCenter",CodexShieldLayer.Role.Center,texture);
            AddLayer("CyanRim",CodexShieldLayer.Role.Rim,null);
            AddLayer("ActiveRays",CodexShieldLayer.Role.Rays,null).gameObject.SetActive(activeRays);
        }
        CodexShieldLayer AddLayer(string label,CodexShieldLayer.Role role,Texture texture)
        {
            var go=new GameObject(label,typeof(RectTransform));go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(160,200);
            var layer=go.AddComponent<CodexShieldLayer>();layer.Configure(role,texture);return layer;
        }
        protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();}
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CodexShieldLayer : MaskableGraphic
    {
        public enum Role { Center, Bevel, Rim, Rays }
        Role role;Texture texture;
        public Role LayerRole=>role;
        public Texture LayerTexture {get=>texture;set{texture=value;SetMaterialDirty();SetVerticesDirty();}}
        public override Texture mainTexture=>texture?texture:Texture2D.whiteTexture;
        public void Configure(Role value,Texture borrowedTexture){role=value;LayerTexture=borrowedTexture;raycastTarget=false;}
        static readonly Vector2[] Outline={new Vector2(.5f,0),new Vector2(.82f,.14f),new Vector2(1,.19f),new Vector2(.97f,.58f),new Vector2(.84f,.78f),new Vector2(.5f,1),new Vector2(.16f,.78f),new Vector2(.03f,.58f),new Vector2(0,.19f),new Vector2(.18f,.14f)};
        Vector2 Point(Vector2 p,float scale=1){var r=rectTransform.rect;p=(p-Vector2.one*.5f)*scale+Vector2.one*.5f;return new Vector2(r.x+p.x*r.width,r.y+(1-p.y)*r.height);}
        void Vertex(VertexHelper mesh,Vector2 p,Color tint){var r=rectTransform.rect;mesh.AddVert(p,tint,new Vector2((p.x-r.x)/r.width,(p.y-r.y)/r.height));}
        void Ring(VertexHelper mesh,float outer,float inner,Color top,Color bottom)
        {
            for(int i=0;i<Outline.Length;i++){int next=(i+1)%Outline.Length,start=mesh.currentVertCount;var a=Outline[i];var b=Outline[next];
                Vertex(mesh,Point(a,outer),Color.Lerp(top,bottom,a.y));Vertex(mesh,Point(b,outer),Color.Lerp(top,bottom,b.y));
                Vertex(mesh,Point(b,inner),Color.Lerp(top,bottom,b.y)*new Color(.63f,.67f,.72f,1));Vertex(mesh,Point(a,inner),Color.Lerp(top,bottom,a.y)*new Color(.63f,.67f,.72f,1));
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);}
        }
        void Ray(VertexHelper mesh,Vector2 a,Vector2 b)
        {
            a=Point(a);b=Point(b);var n=new Vector2(-(b-a).y,(b-a).x).normalized*2;int s=mesh.currentVertCount;var tint=new Color(.3f,.82f,.91f);
            Vertex(mesh,a+n,tint);Vertex(mesh,b+n,tint);Vertex(mesh,b-n,tint);Vertex(mesh,a-n,tint);mesh.AddTriangle(s,s+1,s+2);mesh.AddTriangle(s,s+2,s+3);
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(role==Role.Center){Vertex(mesh,Point(Vector2.one*.5f),Color.white);foreach(var p in Outline)Vertex(mesh,Point(p,.81f),Color.white);for(int i=0;i<Outline.Length;i++)mesh.AddTriangle(0,1+i,1+(i+1)%Outline.Length);}
            else if(role==Role.Bevel)Ring(mesh,.98f,.81f,new Color(.72f,.78f,.82f),new Color(.19f,.28f,.34f));
            else if(role==Role.Rim){Ring(mesh,1,.975f,new Color(.47f,.88f,.95f),new Color(.2f,.63f,.73f));Ring(mesh,.835f,.82f,new Color(.21f,.60f,.69f),new Color(.13f,.38f,.46f));}
            else {Ray(mesh,new Vector2(1.07f,.25f),new Vector2(1.3f,.15f));Ray(mesh,new Vector2(1.09f,.4f),new Vector2(1.38f,.4f));Ray(mesh,new Vector2(1.07f,.55f),new Vector2(1.3f,.65f));}
        }
    }
}
