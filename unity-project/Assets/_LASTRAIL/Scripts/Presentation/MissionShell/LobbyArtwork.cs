using UnityEngine;

namespace LastRail.Presentation.MissionShell
{
    // Textures belong to the prepared screen scope; callers own the created sprite.
    public static class LobbyArtwork
    {
        public static Rect IconUv(int index)=>new Rect(index%4*.25f,1-(index/4+1)*.25f,.25f,.25f);
        public static UnityEngine.UI.RawImage BlueInset(Transform parent)
        {
            var rect=new GameObject("BlueInset",typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=new Vector2(4,4);rect.offsetMax=new Vector2(-4,-4);
            var image=rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/button-surface-v06");
            image.uvRect=new Rect(0,.1f,1,.8f);image.raycastTarget=false;
            return image;
        }
        public static Sprite CreateFrame()
        {
            var frame=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/frame-v06");
            return Sprite.Create(frame,new Rect(.048f*frame.width,.048f*frame.height,.904f*frame.width,.904f*frame.height),new Vector2(.5f,.5f),500,0,SpriteMeshType.FullRect,Vector4.one*(.075f*frame.width));
        }
    }
}
