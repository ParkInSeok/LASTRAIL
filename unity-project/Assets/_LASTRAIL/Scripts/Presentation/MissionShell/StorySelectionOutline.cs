using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Selection decoration only; the controller remains the owner of selected row state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StorySelectionOutline : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;const float inset=8,width=2.5f;
            void Quad(float left,float bottom,float right,float top)
            {int s=mesh.currentVertCount;mesh.AddVert(new Vector2(left,bottom),color,Vector2.zero);mesh.AddVert(new Vector2(left,top),color,Vector2.zero);mesh.AddVert(new Vector2(right,top),color,Vector2.zero);mesh.AddVert(new Vector2(right,bottom),color,Vector2.zero);mesh.AddTriangle(s,s+1,s+2);mesh.AddTriangle(s,s+2,s+3);}
            float x0=r.xMin+inset,x1=r.xMax-inset,y0=r.yMin+inset,y1=r.yMax-inset;if(x1<=x0||y1<=y0)return;
            Quad(x0,y0,x1,y0+width);Quad(x0,y1-width,x1,y1);Quad(x0,y0+width,x0+width,y1-width);Quad(x1-width,y0+width,x1,y1-width);
        }
    }
}
