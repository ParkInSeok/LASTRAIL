using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    public sealed class OfflineSupplyView:MonoBehaviour
    {
        public OfflineSupplyDisplayState CurrentState {get;private set;}
        TMP_FontAsset font;Action close,refresh,claim,roulette;bool bound;
        TMP_Text time,storage,notice,adNotice,claimLabel,adLabel,empty;Button claimButton,refreshButton,adButton;
        RectTransform rewardContent;ScrollRect rewardScroll;TMP_Text scrollHint;string rewardKey;
        public static OfflineSupplyView Create(Transform parent,TMP_FontAsset font)
        {var root=AuthFormVisuals.Rect("OfflineSupply",parent);var view=root.gameObject.AddComponent<OfflineSupplyView>();view.font=font;view.Build();return view;}
        public void Bind(Action close,Action refresh,Action claim,Action roulette){this.close=close;this.refresh=refresh;this.claim=claim;this.roulette=roulette;bound=true;}
        public void Unbind(){bound=false;close=refresh=claim=roulette=null;}
        void OnDisable()=>Unbind();
        void OnDestroy()=>Unbind();
        void Build()
        {
            var frame=IndependentMetalSurface.Create((RectTransform)transform,false,new Color(.43f,.46f,.48f));frame.Lights.gameObject.SetActive(false);frame.Frame.BorderScale=.6f;frame.Frame.color=new Color(.6f,.6f,.58f);frame.Backplate.Inset=new Vector2(18,18);
            Plate("SupplyHeaderPlate",transform,206,16,388,82);
            Text("SupplyTitle",transform,"운행 보급",95,25,610,70,53,TextAlignmentOptions.Center);
            var closeButton=Control("CloseOfflineSupply",transform,"",712,25,62,70,()=>close?.Invoke(),31);
            var closeGlyph=Rect("SupplyCloseGlyph",closeButton.transform,16,20,30,30);
            foreach(var angle in new[]{-45f,45f}){var stroke=AuthFormVisuals.Rect("Stroke",closeGlyph);stroke.anchorMin=stroke.anchorMax=stroke.pivot=new Vector2(.5f,.5f);stroke.anchoredPosition=Vector2.zero;stroke.sizeDelta=new Vector2(38,4);stroke.localRotation=Quaternion.Euler(0,0,angle);var ink=stroke.gameObject.AddComponent<Image>();ink.color=new Color(.87f,.91f,.89f);ink.raycastTarget=false;}
            Art("SupplyRoom","NoncombatOffline/room-v01",30,110,740,485);
            Art("SupplyChest","NoncombatOffline/chest-v01",48,175,704,425);
            notice=Text("SupplyNotice",transform,"",45,613,710,70,28,TextAlignmentOptions.Center);
            var timePanel=Plate("SupplyTimePanel",transform,50,695,700,80);Text("TimeHeading",timePanel,"누적 시간",24,15,250,47,30,TextAlignmentOptions.Left);
            time=Text("SupplyTime",timePanel,"—",285,4,390,64,48,TextAlignmentOptions.Right);time.color=new Color(.15f,.78f,1);
            Text("RewardsHeading",transform,"기본 보급",100,792,600,48,36,TextAlignmentOptions.Center);
            var ornaments=Rect("SupplyHeadingOrnaments",transform,0,815,800,2);
            foreach(var x in new[]{220f,530f}){var ink=Rect("SideLine",ornaments,x,0,50,2).gameObject.AddComponent<Image>();ink.color=new Color(.55f,.68f,.69f);ink.raycastTarget=false;}
            var viewport=Rect("SupplyRewardViewport",transform,50,845,700,240);viewport.gameObject.AddComponent<RectMask2D>();var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            rewardContent=Rect("SupplyRewardContent",viewport,0,0,700,240);scroll.content=rewardContent;
            rewardScroll=scroll;rewardScroll.scrollSensitivity=38;
            empty=Text("SupplyRewardsEmpty",transform,"",70,901,660,110,30,TextAlignmentOptions.Center);
            storage=Text("SupplyStorage",transform,"",70,1090,660,40,23,TextAlignmentOptions.Right);
            scrollHint=Text("SupplyScrollHint",transform,"",60,1090,340,40,19,TextAlignmentOptions.Left);
            claimButton=Metal("ClaimOffline","기본 보급 받기",50,1135,700,96,false,()=>claim?.Invoke());claimLabel=claimButton.GetComponentInChildren<TMP_Text>();
            var ad=adButton=Metal("OpenOfflineRoulette","광고 보급 · 룰렛",50,1242,700,106,true,()=>roulette?.Invoke());adLabel=ad.GetComponentInChildren<TMP_Text>();
            refreshButton=Control("RefreshOffline",transform,"새로고침",50,1361,166,51,()=>refresh?.Invoke(),23);adNotice=Text("SupplyAdNotice",transform,"",231,1356,519,60,21,TextAlignmentOptions.Center);
        }
        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout();
        public void SetLandscapeLayout()
        {
            AuthFormVisuals.Place((RectTransform)transform,0,0,1600,900);
            landscape.Apply(transform,(name,r)=>
            {
                if(name=="SupplyRoom"||name=="SupplyChest")return LandscapeSectionLayout.Section(r,110,0,150);
                if(name=="SupplyTitle")return new Rect(35,25,640,70);
                if(name.Contains("Close")&&r.y<110)return new Rect(1490,25,r.width,r.height);
                if(name=="SupplyNotice")return new Rect(35,570,710,70);
                if(name=="SupplyTimePanel")return new Rect(35,680,700,80);
                if(name=="RewardsHeading")return new Rect(850,130,600,48);
                if(name=="SupplyHeadingOrnaments")return new Rect(750,153,800,2);
                if(name=="SupplyRewardViewport")return new Rect(830,210,700,240);
                if(name=="SupplyRewardsEmpty")return new Rect(850,265,660,110);
                if(name=="SupplyStorage")return new Rect(850,465,660,40);
                if(name=="SupplyScrollHint")return new Rect(840,465,340,40);
                if(name=="ClaimOffline")return new Rect(830,525,700,96);
                if(name=="OpenOfflineRoulette")return new Rect(830,640,700,106);
                if(name=="RefreshOffline")return new Rect(830,785,166,60);
                if(name=="SupplyAdNotice")return new Rect(1011,780,519,60);
                return r;
            });
        }
        public void Render(OfflineSupplyDisplayState state)
        {
            CurrentState=state;time.text=state.TimeText;storage.text=state.StorageText;notice.text=state.Notice;adNotice.text=state.AdNotice;claimLabel.text=state.ClaimText;adLabel.text=state.AdText;
            adButton.interactable=state.CanOpenAd;claimButton.interactable=state.CanClaim;refreshButton.interactable=state.CanRefresh;empty.gameObject.SetActive(state.Rewards.Count==0);empty.text=state.HasPreview?"아직 수령할 보급이 없습니다.":"보급 내역 확인 필요";
            adButton.gameObject.SetActive(state.ShowAdEntry);
            // Keep the basic action at its existing hit position; collapsing advertising never moves it under a held pointer.
            AuthFormVisuals.Place((RectTransform)refreshButton.transform,50,state.ShowAdEntry?1361:1250,166,51);
            AuthFormVisuals.Place(adNotice.rectTransform,231,state.ShowAdEntry?1356:1245,519,60);
            AuthFormVisuals.Place((RectTransform)transform,0,0,800,state.ShowAdEntry?1430:1330);
            var key=string.Join("|",System.Linq.Enumerable.Select(state.Rewards,r=>r.RewardCode+":"+r.DisplayName+":"+r.Quantity));
            if(key!=rewardKey)
            {
                rewardKey=key;for(int i=rewardContent.childCount-1;i>=state.Rewards.Count;i--){var child=rewardContent.GetChild(i).gameObject;child.SetActive(false);child.transform.SetParent(null,false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
                float baseWidth=state.Rewards.Count<=3?(700-(state.Rewards.Count-1)*8f)/Mathf.Max(1,state.Rewards.Count):228;
                float nextX=0;
                for(int i=0;i<state.Rewards.Count;i++)
                {
                    var data=state.Rewards[i];var cell=rewardContent.Find("SupplyReward"+i) as RectTransform;if(cell==null){cell=Plate("SupplyReward"+i,rewardContent,nextX,0,baseWidth,235);Text("Value",cell,"",14,177,baseWidth-28,48,35,TextAlignmentOptions.Center);Text("Name",cell,"",8,10,baseWidth-16,38,23,TextAlignmentOptions.Center);}
                    var value=cell.Find("Value").GetComponent<TMP_Text>();value.text=data.Quantity.ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
                    value.enableAutoSizing=false;
                    var preferred=value.GetPreferredValues(value.text,Mathf.Infinity,Mathf.Infinity);
                    float width=Mathf.Max(baseWidth,Mathf.Ceil(preferred.x)+32);
                    cell.SetParent(rewardContent,false);AuthFormVisuals.Place(cell,nextX,0,width,235);
                    AuthFormVisuals.Place(value.rectTransform,14,177,width-28,Mathf.Max(48,Mathf.Ceil(preferred.y)+2));
                    var name=cell.Find("Name").GetComponent<TMP_Text>();name.text=data.DisplayName;AuthFormVisuals.Place(name.rectTransform,8,10,width-16,38);
                    float iconWidth=Mathf.Min(190,width-30);
                    RewardIconPresentation.Create(cell,font,data.RewardCode,new Rect((width-iconWidth)/2,48,iconWidth,128));
                    value.transform.SetAsLastSibling();nextX+=width+8;
                }
                rewardContent.sizeDelta=new Vector2(Mathf.Max(700,nextX-8),240);
                rewardScroll.StopMovement();rewardScroll.horizontalNormalizedPosition=0;rewardScroll.horizontal=rewardContent.sizeDelta.x>700;
                scrollHint.text=rewardScroll.horizontal?"◀ 좌우로 밀어 전체 보상 확인 ▶":"";
            }
            SetLandscapeLayout();
            foreach(var button in GetComponentsInChildren<Button>())foreach(var label in button.GetComponentsInChildren<TMP_Text>())label.color=button.interactable?new Color(.93f,.93f,.88f):new Color(.48f,.53f,.55f);
        }
        void Dispatch(Button b,Action action){if(bound&&isActiveAndEnabled&&b.interactable&&b.gameObject.activeInHierarchy)action?.Invoke();}
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(r,x,y,w,h);return r;}
        RectTransform Plate(string name,Transform parent,float x,float y,float w,float h)
        {var r=Rect(name,parent,x,y,w,h);var back=r.gameObject.AddComponent<Image>();back.color=new Color(.025f,.038f,.045f);back.raycastTarget=false;var surface=IndependentMetalSurface.Create(r,true,new Color(.43f,.46f,.48f));surface.Lights.gameObject.SetActive(false);surface.Backplate.Inset=new Vector2(8,8);surface.Frame.BorderScale=.24f;surface.Frame.color=new Color(.6f,.6f,.58f);return r;}
        Button Control(string name,Transform parent,string label,float x,float y,float w,float h,Action action,float size)
        {var r=Plate(name,parent,x,y,w,h);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.targetGraphic.raycastTarget=true;Text("Label",r,label,6,4,w-12,h-8,size,TextAlignmentOptions.Center);b.onClick.AddListener(()=>Dispatch(b,action));return b;}
        Button Metal(string name,string label,float x,float y,float w,float h,bool primary,Action action)
        {Button b=null;b=AuthFormVisuals.MetalButton(transform,name,label,font,primary,()=>Dispatch(b,action));AuthFormVisuals.Place((RectTransform)b.transform,x,y,w,h);return b;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,TextAlignmentOptions alignment)
        {var text=AuthFormVisuals.Text(parent,name,value,font,size,false,alignment);AuthFormVisuals.Place(text.rectTransform,x,y,w,h);text.enableAutoSizing=true;text.fontSizeMin=size*.75f;text.fontSizeMax=size;return text;}
        void Art(string name,string path,float x,float y,float w,float h)
        {var image=Rect(name,transform,x,y,w,h).gameObject.AddComponent<RawImage>();image.texture=LastRail.Resources.PreparedContent.Load<Texture2D>(path);image.enabled=image.texture!=null;image.raycastTarget=false;if(image.texture!=null)image.uvRect=OperatorVisualCatalog.Crop(image.texture as Texture2D,w,h);}
    }
}
