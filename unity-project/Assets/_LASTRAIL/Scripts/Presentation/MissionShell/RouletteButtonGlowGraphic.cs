using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Independent optical edge layer; the metal frame, plate and text remain replaceable.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouletteButtonGlowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=rectTransform.rect;if(rect.width<70||rect.height<40)return;
            Vector2[] Points(float inset)
            {
                float left=rect.xMin+inset,right=rect.xMax-inset,top=rect.yMax-inset,bottom=rect.yMin+inset,b=7;
                return new[]{new Vector2(left+b,top),new Vector2(right-b,top),new Vector2(right,top-b),new Vector2(right,bottom+b),new Vector2(right-b,bottom),new Vector2(left+b,bottom),new Vector2(left,bottom+b),new Vector2(left,top-b)};
            }
            void Band(float outer,float inner,Color shade)
            {
                var a=Points(outer);var b=Points(inner);shade*=color;
                for(int i=0;i<8;i++){int j=(i+1)%8,k=mesh.currentVertCount;mesh.AddVert(a[i],shade,Vector2.zero);mesh.AddVert(b[i],shade,Vector2.zero);mesh.AddVert(b[j],shade,Vector2.zero);mesh.AddVert(a[j],shade,Vector2.zero);mesh.AddTriangle(k,k+1,k+2);mesh.AddTriangle(k,k+2,k+3);}
            }
            for(int layer=10;layer>=1;layer--)Band(14-layer*.65f,17+layer*.65f,new Color(0,.47f,1,.045f));
            Band(14,17,new Color(.05f,.7f,1,.8f));
            Band(15,16,new Color(.55f,.97f,1,.95f));
        }
    }
}
