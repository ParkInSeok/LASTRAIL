using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Startup
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StartupRailGraphic : MaskableGraphic
    {
        public Texture texture;
        public Vector2 topLeft=new Vector2(350,-650), topRight=new Vector2(370,-650);
        public Vector2 bottomRight=new Vector2(790,-1400), bottomLeft=new Vector2(-70,-1400);
        public bool groundSurface;
        public bool reverseCross;
        public float repeats=10;
        public float Phase => phase;
        float phase;
        public override Texture mainTexture => texture != null ? texture : Texture2D.whiteTexture;
        protected override void OnEnable()
        {
            base.OnEnable();
            var owner=GetComponentInParent<Canvas>();
            if(owner!=null) owner.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
        }
        public void SetPhase(float value) { phase=value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var a=topLeft; var b=topRight;
            var c=bottomRight; var d=bottomLeft;
            var ac=c-a; var bd=d-b;
            float cross=ac.x*bd.y-ac.y*bd.x;
            var ba=b-a; float t=(ba.x*bd.y-ba.y*bd.x)/cross;
            float s=(ba.x*ac.y-ba.y*ac.x)/cross;
            var q=new[]{1/(1-t),1/(1-s),1/t,1/s};
            var points=new[]{a,b,c,d}; var uv=new[]{new Vector2(0,phase),new Vector2(1,phase),new Vector2(1,repeats+phase),new Vector2(0,repeats+phase)};
            for(int i=0;i<4;i++) { var v=UIVertex.simpleVert; v.position=points[i]; v.color=color; v.uv0=new Vector4(uv[i].x*q[i],uv[i].y*q[i],0,0); v.uv1=new Vector4(q[i],groundSurface?1:0,reverseCross?1:0,0); vh.AddVert(v); }
            vh.AddTriangle(0,1,2); vh.AddTriangle(2,3,0);
        }
    }
}
