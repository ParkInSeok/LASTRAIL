using UnityEngine;

namespace LastRail.Presentation.MissionShell
{
    // Textures belong to the prepared screen scope; callers own the created sprite.
    public static class LobbyArtwork
    {
        public static Rect IconUv(int index)=>new Rect(index%4*.25f,1-(index/4+1)*.25f,.25f,.25f);
        public static Sprite CreateFrame()
        {
            var frame=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/frame-landscape-v01");
            return Sprite.Create(frame,new Rect(52f/2172*frame.width,133f/724*frame.height,2069f/2172*frame.width,465f/724*frame.height),new Vector2(.5f,.5f),400,0,SpriteMeshType.FullRect,new Vector4(65f/2172*frame.width,65f/724*frame.height,65f/2172*frame.width,65f/724*frame.height));
        }
    }
}
