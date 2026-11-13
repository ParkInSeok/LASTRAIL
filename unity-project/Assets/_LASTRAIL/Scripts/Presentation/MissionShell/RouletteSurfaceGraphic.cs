using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Borrowed texture on a circular face or annulus. No result, odds or animation state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouletteSurfaceGraphic : RawImage
    {
        public float Radius=.44f;
        public float InnerRadius;
        public float EdgeBrightness=.6f;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=rectTransform.rect;if(rect.width<=0||rect.height<=0)return;float diameter=Mathf.Min(rect.width,rect.height);
            Vector2 Point(float radius,float angle)=>rect.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*diameter*radius;
            void Vertex(Vector2 point,Color tint)
            {var uv=new Vector2(uvRect.x+(point.x-rect.x)/rect.width*uvRect.width,uvRect.y+(point.y-rect.y)/rect.height*uvRect.height);mesh.AddVert(point,tint,uv);}
            var edge=new Color(color.r*EdgeBrightness,color.g*EdgeBrightness,color.b*EdgeBrightness,color.a);
            if(InnerRadius<=0)
            {
                Vertex(rect.center,color);
                for(int i=0;i<=128;i++){Vertex(Point(Radius,i*Mathf.PI*2/128),edge);if(i>0)mesh.AddTriangle(0,i,i+1);}
            }
            else for(int i=0;i<128;i++)
            {
                float a=i*Mathf.PI*2/128,b=(i+1)*Mathf.PI*2/128;int start=mesh.currentVertCount;
                Vertex(Point(Radius,a),edge);Vertex(Point(Radius,b),edge);Vertex(Point(InnerRadius,b),color);Vertex(Point(InnerRadius,a),color);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
