using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Startup
{
    // Owns only reading layout and its ScrollRect hierarchy; borrows the popup's text and rule.
    public sealed class StartupPopupBodyPresentation
    {
        const float BodyWidth=700, ScrollbarWidth=10, ScrollbarGap=8, ParagraphGap=18;
        readonly RectTransform band, area, viewport, content, separator;
        readonly TMP_Text lead, followup;
        readonly ScrollRect scroll;
        readonly Scrollbar scrollbar;
        string displayedBody;
        float readingPosition=1;

        public StartupPopupBodyPresentation(RectTransform band, TMP_Text lead, TMP_Text followup, RectTransform separator)
        {
            this.band=band;this.lead=lead;this.followup=followup;this.separator=separator;
            area=AuthFormVisuals.Rect("PopupBodyScroll",band);
            viewport=AuthFormVisuals.Rect("PopupBodyViewport",area);
            var input=viewport.gameObject.AddComponent<Image>();input.color=Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            content=AuthFormVisuals.Rect("PopupBodyContent",viewport);
            var track=AuthFormVisuals.Rect("PopupBodyScrollbar",area);
            var trackImage=track.gameObject.AddComponent<Image>();trackImage.color=new Color(.18f,.25f,.28f,.85f);
            var thumb=AuthFormVisuals.Rect("PopupBodyThumb",track);
            thumb.anchorMin=Vector2.zero;thumb.anchorMax=Vector2.one;thumb.offsetMin=thumb.offsetMax=Vector2.zero;
            var thumbImage=thumb.gameObject.AddComponent<Image>();thumbImage.color=new Color(.3f,.85f,.94f);
            scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect=thumb;scrollbar.targetGraphic=thumbImage;scrollbar.navigation=new Navigation{mode=Navigation.Mode.Automatic};
            scroll=area.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=false;
            scroll.scrollSensitivity=50;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
            area.gameObject.SetActive(false);
        }

        public void Prepare()
        {
            if(area.gameObject.activeSelf)readingPosition=scroll.verticalNormalizedPosition;
            lead.transform.SetParent(band,false);followup.transform.SetParent(band,false);separator.SetParent(band,false);
            // ScrollRect owns its scrollbar subscription through OnEnable/OnDisable.
            area.gameObject.SetActive(false);
        }

        public void ClearReadingPosition(){displayedBody=null;readingPosition=1;}

        public void Render(string body, float origin, float buttonTop)
        {
            body=body??string.Empty;
            var boundary=System.Text.RegularExpressions.Regex.Match(body,@"\r?\n\r?\n");
            lead.text=boundary.Success?body.Substring(0,boundary.Index):body;
            followup.text=boundary.Success?body.Substring(boundary.Index+boundary.Length):string.Empty;
            bool pair=boundary.Success&&followup.text.Length>0;
            followup.gameObject.SetActive(pair);separator.gameObject.SetActive(pair);
            followup.alignment=TextAlignmentOptions.TopLeft;followup.enableAutoSizing=false;
            followup.color=lead.color;followup.fontSize=lead.fontSize;followup.lineSpacing=lead.lineSpacing;
            float limit=buttonTop-24;
            float bottom=Arrange(band,100,origin,BodyWidth,pair);
            float position=body==displayedBody?readingPosition:1;
            displayedBody=body;
            if(bottom<=limit){readingPosition=1;return;}

            float height=limit-origin, textWidth=BodyWidth-ScrollbarWidth-ScrollbarGap;
            AuthFormVisuals.Place(area,100,origin,BodyWidth,height);
            AuthFormVisuals.Place(viewport,0,0,textWidth,height);
            AuthFormVisuals.Place((RectTransform)scrollbar.transform,BodyWidth-ScrollbarWidth,0,ScrollbarWidth,height);
            bottom=Arrange(content,0,0,textWidth,pair);
            AuthFormVisuals.Place(content,0,0,textWidth,Mathf.Max(height,bottom+4));
            area.gameObject.SetActive(true);
            scroll.Rebuild(CanvasUpdate.PostLayout);
            scroll.verticalNormalizedPosition=Mathf.Clamp01(position);
            scroll.Rebuild(CanvasUpdate.PostLayout);
        }

        float Arrange(RectTransform parent, float x, float y, float width, bool pair)
        {
            lead.transform.SetParent(parent,false);followup.transform.SetParent(parent,false);separator.SetParent(parent,false);
            float leadHeight=Mathf.Ceil(lead.GetPreferredValues(lead.text,width,float.PositiveInfinity).y)+2;
            AuthFormVisuals.Place(lead.rectTransform,x,y,width,leadHeight);lead.ForceMeshUpdate(true);
            float bottom=y-lead.textBounds.min.y;
            if(!pair)return bottom;
            float lineY=bottom+ParagraphGap;
            AuthFormVisuals.Place(separator,x,lineY,width,2);
            // Keep the existing glow centered when the overflow text lane narrows.
            var glow=(RectTransform)separator.GetChild(0);glow.anchoredPosition=new Vector2((width-80)/2,glow.anchoredPosition.y);
            float tailHeight=Mathf.Ceil(followup.GetPreferredValues(followup.text,width,float.PositiveInfinity).y)+2;
            AuthFormVisuals.Place(followup.rectTransform,x,0,width,tailHeight);followup.ForceMeshUpdate(true);
            float tailY=lineY+2+ParagraphGap+followup.textBounds.max.y;
            AuthFormVisuals.Place(followup.rectTransform,x,tailY,width,tailHeight);
            return tailY-followup.textBounds.min.y;
        }
    }
}
