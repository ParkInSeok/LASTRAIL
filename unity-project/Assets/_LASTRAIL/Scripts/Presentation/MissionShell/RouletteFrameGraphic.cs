using LastRail.Auth;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Roulette-only chrome. Nine-slicing preserves corner scale and never draws a center plate.
    public sealed class RouletteFrameGraphic : RawImage
    {
        public float CornerSize=58;
        public static RouletteFrameGraphic Create(Transform parent,float cornerSize)
        {
            var graphic=AuthFormVisuals.Rect("RouletteFrame",parent).gameObject.AddComponent<RouletteFrameGraphic>();
            graphic.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/roulette-frame-v01");
            graphic.CornerSize=cornerSize;graphic.raycastTarget=false;return graphic;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;if(r.width<=0||r.height<=0)return;
            float cap=Mathf.Min(CornerSize,Mathf.Min(r.width,r.height)/2);
            var x=new[]{r.xMin,r.xMin+cap,r.xMax-cap,r.xMax};
            var y=new[]{r.yMin,r.yMin+cap,r.yMax-cap,r.yMax};
            var u=new[]{.025f,.13f,.87f,.975f};var v=new[]{.021f,.092f,.918f,.990f};
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                if(row==1&&col==1)continue;int start=mesh.currentVertCount;
                mesh.AddVert(new Vector2(x[col],y[row]),color,new Vector2(u[col],v[row]));
                mesh.AddVert(new Vector2(x[col],y[row+1]),color,new Vector2(u[col],v[row+1]));
                mesh.AddVert(new Vector2(x[col+1],y[row+1]),color,new Vector2(u[col+1],v[row+1]));
                mesh.AddVert(new Vector2(x[col+1],y[row]),color,new Vector2(u[col+1],v[row]));
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
