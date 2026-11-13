using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation
{
    // Presentation-only geometry; callbacks and state remain with the existing screen owner.
    internal static class LandscapeOverlayLayout
    {
        internal static void HideUnboundArt(Transform root)
        {
            // Prefabs contain simultaneous sample badges/states. Only explicitly bound art is shown.
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(image.GetComponentInParent<Button>() == null && !image.name.EndsWith("LandscapeViewport")) image.enabled=false;
        }
        internal static void Art(Transform root, RectTransform safe, string name, float x,float y,float w,float h, bool visible=true)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.name!=name) continue;
                image.enabled=visible; image.raycastTarget=false;
                if(!visible) return;
                image.gameObject.SetActive(true);
                var layer = safe.Find("LandscapeArtwork") as RectTransform;
                if(layer==null)
                {
                    layer = new GameObject("LandscapeArtwork",typeof(RectTransform)).GetComponent<RectTransform>();
                    layer.SetParent(safe,false); layer.anchorMin=Vector2.zero;layer.anchorMax=Vector2.one;
                    layer.offsetMin=layer.offsetMax=Vector2.zero;layer.SetAsFirstSibling();
                }
                Place(image,layer,x,y,w,h);
                image.preserveAspect=true; image.transform.SetAsLastSibling(); return;
            }
        }
        internal static void Backdrop(RectTransform safe)
        {
            var found=safe.Find("LandscapeBackdrop");
            if(found!=null) return;
            var image=new GameObject("LandscapeBackdrop",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(safe,false); var rect=image.rectTransform;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            image.color=new Color(.025f,.045f,.06f);image.raycastTarget=false;image.transform.SetAsFirstSibling();
        }
        internal static void Place(Component item, RectTransform parent, float x, float y, float w, float h)
        {
            if (item == null) return;
            var rect = (RectTransform)item.transform;
            if (rect.parent != parent) rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(x+w, y+h);
            rect.offsetMin = new Vector2(12, 8); rect.offsetMax = new Vector2(-12, -8);
        }
        internal static void ScrollText(TMP_Text text, RectTransform parent, float x, float y, float w, float h)
        {
            if (text == null) return;
            var name = text.name + "LandscapeViewport";
            var existing = parent.Find(name);
            RectTransform viewport;
            if (existing != null) viewport = (RectTransform)existing;
            else
            {
                viewport = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
                viewport.GetComponent<Image>().color = new Color(0,0,0,.001f);
                var scroll = viewport.GetComponent<ScrollRect>(); scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewport;
                var content = text.rectTransform; content.SetParent(viewport,false);
                content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1);
                content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
                text.overflowMode = TextOverflowModes.Overflow;
                var fit = text.GetComponent<ContentSizeFitter>() ?? text.gameObject.AddComponent<ContentSizeFitter>();
                fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.content = content;
            }
            Place(viewport,parent,x,y,w,h);
        }
    }
}
