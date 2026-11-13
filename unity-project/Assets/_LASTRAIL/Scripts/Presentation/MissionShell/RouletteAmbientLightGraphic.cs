using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Independent local light spill behind the apparatus, without changing the background asset.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouletteAmbientLightGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            const int rings=12, sides=48;
            Vector2 Point(float radius,int side)
            {
                float angle=side*Mathf.PI*2/sides;
                return r.center+new Vector2(Mathf.Cos(angle)*r.width,Mathf.Sin(angle)*r.height)*radius*.5f;
            }
            Color Shade(float radius){var c=color;c.a*=Mathf.Pow(1-radius*radius,3);return c;}
            for(int ring=0;ring<rings;ring++)
            {
                float inner=(float)ring/rings,outer=(float)(ring+1)/rings;
                for(int side=0;side<sides;side++)
                {
                    int v=mesh.currentVertCount;
                    mesh.AddVert(Point(inner,side),Shade(inner),Vector2.zero);
                    mesh.AddVert(Point(outer,side),Shade(outer),Vector2.zero);
                    mesh.AddVert(Point(outer,side+1),Shade(outer),Vector2.zero);
                    mesh.AddVert(Point(inner,side+1),Shade(inner),Vector2.zero);
                    mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);
                }
            }
        }
    }
}
