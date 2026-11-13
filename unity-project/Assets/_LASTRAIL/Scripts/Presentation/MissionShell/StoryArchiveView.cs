using System;
using LastRail.Auth;
using LastRail.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Composes independently replaceable room, thumbnails, plates, frames and text.
    // Inputs are intents; no server permission or story progression is owned here.
    public sealed class StoryArchiveView : MonoBehaviour,IStoryArchiveView
    {
        public event Action<int> SelectionRequested;
        public event Action OpenRequested,BackRequested,RefreshRequested;
        public void RequestBack(){if(isActiveAndEnabled)BackRequested?.Invoke();}
        public StoryArchiveViewState CurrentState {get;private set;}
        StoryArchiveController controller;
        TMP_FontAsset font;Sprite frame,neutralPlate;
        float viewportHeight=-1;
        RawImage environment,detailArt;
        readonly TMP_Text[] titles=new TMP_Text[7];
        readonly RawImage[] thumbnails=new RawImage[7];
        readonly StorySelectionOutline[] selectedFrames=new StorySelectionOutline[7];
        TMP_Text detailTitle,detailText,status,backCaption;
        Button open,refresh;
        RectTransform body,readingPanel,returnArrow;
        TMP_Text readingTitle,readingText;
        const float Width=941,Height=1672;

        public static StoryArchiveView Create(Transform parent,TMP_FontAsset font,Sprite borrowedFrame,StoryArchiveReadModel reader,Action close)
        {
            var root=AuthFormVisuals.Rect("StoryArchive",parent);var view=root.gameObject.AddComponent<StoryArchiveView>();view.font=font;view.frame=borrowedFrame;view.Build();
            view.controller=new StoryArchiveController(reader,view,close);view.RefreshRead();return view;
        }
        async void RefreshRead(){await controller.EnsureAsync();}
        public void Unbind(){controller?.Dispose();}
        void OnDisable(){Unbind();}
        void OnDestroy(){Unbind();if(neutralPlate!=null){if(Application.isPlaying)Destroy(neutralPlate);else DestroyImmediate(neutralPlate);}}
        void Build()
        {
            var material=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryUi/secondary");
            if(material!=null)neutralPlate=Sprite.Create(material,new Rect(material.width*.15f,material.height*.2f,material.width*.74f,material.height*.5f),new Vector2(.5f,.5f));
            var backing=Rect("ArchiveLowerMaterial",transform,0,0,Width,Height).gameObject.AddComponent<Image>();backing.sprite=neutralPlate;backing.color=new Color(.16f,.20f,.21f);backing.raycastTarget=false;
            environment=Art("ArchiveEnvironment",transform,0,0,Width,401);environment.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatArchive/V02/room");
            Text("ArchiveTitle",transform,"이야기 기록",155,28,700,90,51);
            var headerBack=Control("BackStoryArchive",transform,"",34,45,104,70,RequestBack);Chevron("StoryHeaderArrow",headerBack.transform,40,16,24,38);
            body=Rect("ArchiveList",transform,0,0,Width,Height);
            Plate("ChapterPlate",body,27,387,887,69);Plate("ChapterNamePlate",body,310,383,320,74);Text("Chapter",body,"1장",320,389,300,62,36).alignment=TextAlignmentOptions.Center;foreach(var x in new[]{135f,646f}){var line=Rect("ChapterLine"+(int)x,body,x,419,160,2).gameObject.AddComponent<Image>();line.color=new Color(.55f,.48f,.35f);line.raycastTarget=false;}
            for(int i=0;i<7;i++)
            {
                int index=i;float y=470+i*104;
                var row=Control("StoryRow"+i,body,"",27,y,887,94,()=>SelectionRequested?.Invoke(index));
                thumbnails[i]=Art("StoryThumbnail"+i,row.transform,9,8,213,78);
                titles[i]=Text("StoryTitle"+i,row.transform,"",244,8,564,78,34);
                Text("StoryChevron"+i,row.transform,"›",819,8,44,78,44).alignment=TextAlignmentOptions.Center;
                var highlight=AuthFormVisuals.Rect("SelectionFrame",row.transform).gameObject.AddComponent<StorySelectionOutline>();highlight.color=new Color(.2f,.82f,.92f);highlight.raycastTarget=false;selectedFrames[i]=highlight;
            }
            Plate("SelectedRecordPlate",body,27,1213,887,248);
            detailArt=Art("SelectedRecordArt",body,42,1230,402,210);
            detailTitle=Text("SelectedRecordTitle",body,"",472,1230,415,49,33);
            detailText=Text("SelectedRecordSummary",body,"",472,1284,415,83,27);
            open=AuthFormVisuals.MetalButton(body,"OpenStoryRecord","기록 보기",font,true,()=>{if(isActiveAndEnabled&&open.interactable)OpenRequested?.Invoke();});
            Place((RectTransform)open.transform,470,1374,420,76);open.GetComponentInChildren<TMP_Text>().fontSize=35;
            var disabled=open.colors;disabled.disabledColor=new Color(.22f,.26f,.28f,.7f);open.colors=disabled;
            status=Text("StoryStatus",transform,"",34,1478,730,70,24);
            refresh=Control("RefreshStoryArchive",transform,"새로고침",754,1485,154,59,()=>RefreshRequested?.Invoke());
            var back=Control("StoryListReturn",transform,"목록으로",170,1559,601,91,RequestBack);backCaption=back.GetComponentInChildren<TMP_Text>();returnArrow=Chevron("StoryReturnArrow",back.transform,200,24.5f,24,42);
            readingPanel=Rect("RecordTextPanel",transform,27,387,887,1066);Plate("RecordTextPlate",readingPanel,0,0,887,1066);
            readingTitle=Text("RecordTextTitle",readingPanel,"",44,30,799,80,40);
            // Long record text scrolls; it is never silently truncated to a fixed card.
            var viewport=Rect("RecordViewport",readingPanel,44,136,799,862);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("RecordContent",viewport,0,0,799,862);
            readingText=Text("RecordText",content,"",0,0,799,862,31);readingText.enableAutoSizing=false;readingText.alignment=TextAlignmentOptions.TopLeft;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            readingPanel.gameObject.SetActive(false);
        }
        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout(), listLandscape=new LandscapeSectionLayout();
        public void SetViewportHeight(float height)
        {
            landscape.Apply(transform,(name,r)=>
            {
                if(name=="ArchiveLowerMaterial")return new Rect(0,0,1600,900);
                if(name=="ArchiveEnvironment")return new Rect(800,120,800,340);
                if(name=="ArchiveList")return new Rect(0,0,1600,900);
                if(name=="RecordTextPanel")return new Rect(355,120,887,630);
                if(name=="StoryStatus")return new Rect(35,795,730,70);
                if(name=="RefreshStoryArchive")return new Rect(800,805,154,59);
                if(name=="StoryListReturn")return new Rect(980,795,601,91);
                return r;
            });
            body.localScale=Vector3.one;body.sizeDelta=new Vector2(1600,900);
            listLandscape.Apply(body,(name,r)=>r.y<1213?LandscapeSectionLayout.Section(r,387,15,120,.8f):LandscapeSectionLayout.Section(r,1213,815,480,.8f));
            readingPanel.localScale=Vector3.one;
            ((RectTransform)readingPanel.Find("RecordTextPlate")).sizeDelta=new Vector2(887,630);
            ((RectTransform)readingPanel.Find("RecordViewport")).sizeDelta=new Vector2(799,465);
            ResizeReadingContent(false);
        }
        public void Render(StoryArchiveViewState state)
        {
            bool resetReadingPosition=CurrentState==null||!CurrentState.IsReading
                ||CurrentState.SelectedIndex!=state.SelectedIndex
                ||!string.Equals(CurrentState.DetailText,state.DetailText,StringComparison.Ordinal);
            CurrentState=state;
            for(int i=0;i<titles.Length;i++)
            {
                titles[i].text=state.Rows[i].Title;selectedFrames[i].enabled=i==state.SelectedIndex;
                var resource="NoncombatArchive/V02/"+state.Rows[i].MissionId;
                if(thumbnails[i].texture==null)thumbnails[i].texture=LastRail.Resources.PreparedContent.Load<Texture2D>(resource);
                thumbnails[i].uvRect=FocalCrop(state.Rows[i].MissionId,thumbnails[i].texture as Texture2D,213,thumbnails[i].rectTransform.rect.height);
            }
            detailTitle.text=state.Rows[state.SelectedIndex].Title;detailText.text=state.DetailText;
            detailArt.texture=thumbnails[state.SelectedIndex].texture;detailArt.uvRect=FocalCrop(state.Rows[state.SelectedIndex].MissionId,detailArt.texture as Texture2D,402,detailArt.rectTransform.rect.height);
            open.interactable=state.CanOpen;refresh.interactable=state.Status!=StoryArchiveReadStatus.Loading;
            var openLabel=open.GetComponentInChildren<TMP_Text>();openLabel.text=state.CanOpen?"기록 보기":"열람 불가";openLabel.color=state.CanOpen?new Color(.96f,.96f,.9f):new Color(.65f,.67f,.68f);
            status.text=state.Status==StoryArchiveReadStatus.Loading?"기록을 확인하고 있습니다.":state.Status==StoryArchiveReadStatus.AuthRequired?"로그인 상태를 확인해 주세요.":state.Status!=StoryArchiveReadStatus.Ready?"기록을 불러오지 못했습니다. 다시 확인해 주세요.":!state.CanOpen?"아직 확인하지 못한 기록입니다.":"확인할 수 있는 기록만 열람할 수 있습니다.";
            body.gameObject.SetActive(!state.IsReading);readingPanel.gameObject.SetActive(state.IsReading);backCaption.text=state.IsReading?"기록 목록으로":"목록으로";float captionWidth=Mathf.Min(320,backCaption.GetPreferredValues().x+8);float groupStart=(601-24-18-captionWidth)/2;Place(returnArrow,groupStart,24.5f,24,42);Place(backCaption.rectTransform,groupStart+42,3,captionWidth,85);
            readingTitle.text=detailTitle.text;readingText.text=state.IsReading?state.DetailText:"";
            if(state.IsReading)ResizeReadingContent(resetReadingPosition);
        }
        void ResizeReadingContent(bool reset)
        {var content=(RectTransform)readingText.transform.parent;var viewport=(RectTransform)content.parent;float h=Mathf.Max(viewport.rect.height,readingText.GetPreferredValues(readingText.text,799,0).y+20);content.sizeDelta=new Vector2(799,h);readingText.rectTransform.sizeDelta=new Vector2(799,h);if(reset)content.anchoredPosition=Vector2.zero;}
        static Rect FocalCrop(string id,Texture2D texture,float width,float height)
        {var crop=OperatorVisualCatalog.Crop(texture,width,height);var focal=id=="opn_02_first_signal"?new Vector2(.37f,.78f):id=="opn_03_water_stop"?new Vector2(.5f,.65f):new Vector2(.5f,.5f);crop.x=Mathf.Clamp(focal.x-crop.width*.5f,0,1-crop.width);crop.y=Mathf.Clamp(focal.y-crop.height*.5f,0,1-crop.height);return crop;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        RectTransform Chevron(string name,Transform parent,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var glyph=r.gameObject.AddComponent<CodexBackChevron>();glyph.color=new Color(.96f,.93f,.84f);glyph.raycastTarget=false;return r;}
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {var r=AuthFormVisuals.Rect(name,parent);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        RawImage Art(string name,Transform parent,float x,float y,float w,float h)
        {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
        void Plate(string name,Transform parent,float x,float y,float w,float h)
        {var r=Rect(name,parent,x,y,w,h);var plate=r.gameObject.AddComponent<Image>();plate.sprite=neutralPlate;plate.color=new Color(.2f,.25f,.27f,.99f);plate.raycastTarget=false;Border(r);}
        void Border(Transform parent)
        {var border=AuthFormVisuals.Rect("Frame",parent).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.color=new Color(1,.79f,.51f);border.raycastTarget=false;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size)
        {var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.8f;text.fontSizeMax=size;text.color=new Color(.96f,.93f,.84f);text.text=value;text.richText=false;text.raycastTarget=false;text.alignment=TextAlignmentOptions.MidlineLeft;return text;}
        Button Control(string name,Transform parent,string label,float x,float y,float w,float h,Action action,bool primary=false)
        {var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.sprite=neutralPlate;image.color=primary?new Color(.13f,.45f,.65f):new Color(.2f,.26f,.28f);Border(r);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>{if(isActiveAndEnabled&&button.interactable)action?.Invoke();});Text("Label",r,label,6,3,w-12,h-6,30).alignment=TextAlignmentOptions.Center;return button;}
    }
}

