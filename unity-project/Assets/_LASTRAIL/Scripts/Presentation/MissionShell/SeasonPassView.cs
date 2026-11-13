using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    public sealed class SeasonPassView:MonoBehaviour
    {
        public SeasonPassDisplayState CurrentState {get;private set;}
        Action close,refresh,bulk,season;Action<bool> tab;Action<PassRewardDisplay> claim;bool bound;TMP_FontAsset font;
        TMP_Text progressText,entitlement,notice,empty,seasonLabel;Image progressFill;Button refreshButton,bulkButton;
        RectTransform content;ScrollRect scroll;GameObject columnHeaders,rewardsActive,progressActive;string rendered;
        float viewportHeight=-1;
        public static SeasonPassView Create(Transform parent,TMP_FontAsset font)
        {var root=AuthFormVisuals.Rect("SeasonPass",parent);var view=root.gameObject.AddComponent<SeasonPassView>();view.font=font;view.Build();return view;}
        public void Bind(Action close,Action refresh,Action bulk,Action season,Action<bool> tab,Action<PassRewardDisplay> claim){this.close=close;this.refresh=refresh;this.bulk=bulk;this.season=season;this.tab=tab;this.claim=claim;bound=true;}
        public void Unbind(){bound=false;close=refresh=bulk=season=null;tab=null;claim=null;}
        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout();
        public void SetViewportHeight(float height)
        {
            landscape.Apply(transform,(name,r)=>
            {
                switch(name){
                case "PassTitle":return new Rect(35,24,640,64);
                case "CloseSeasonPass":return new Rect(1500,24,58,63);
                case "PassBanner":return new Rect(35,120,650,157);
                case "PassSeason":return new Rect(35,300,650,70);
                case "PassRewardsTab":return new Rect(760,110,405,59);
                case "PassProgressTab":return new Rect(1165,110,405,59);
                case "RewardsTabActive":return new Rect(772,162,381,4);
                case "ProgressTabActive":return new Rect(1177,162,381,4);
                case "PassColumnHeaders":return new Rect(760,180,790,46);
                case "PassViewport":return new Rect(760,230,810,545);
                case "PassScrollbar":return new Rect(1552,235,13,535);
                case "PassEmpty":return new Rect(795,390,740,145);
                case "PassEntitlement":return new Rect(35,405,420,55);
                case "PassPurchase":return new Rect(35,480,325,61);
                case "PassNotice":return new Rect(35,565,650,80);
                case "PassBulk":return new Rect(50,675,620,98);
                case "RefreshPass":return new Rect(35,805,230,60);
                case "PassCoopNotice":return new Rect(950,815,560,39);
                default:return r;
                }
            });
            UpdateScrollThumb();
        }
        void OnDisable()=>Unbind();void OnDestroy()=>Unbind();
        void Build()
        {
            var frame=IndependentMetalSurface.Create((RectTransform)transform,false,new Color(.43f,.46f,.48f));frame.Lights.gameObject.SetActive(false);frame.Frame.BorderScale=.6f;frame.Frame.color=new Color(.6f,.6f,.58f);frame.Backplate.Inset=new Vector2(18,18);
            Text("PassTitle",transform,"시즌 패스",110,14,640,64,51,TextAlignmentOptions.Center);Control("CloseSeasonPass",transform,"×",775,17,58,63,()=>close?.Invoke(),29);
            var banner=Rect("PassBanner",transform,25,93,810,195).gameObject.AddComponent<RawImage>();banner.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatPass/banner-v01");banner.raycastTarget=false;banner.enabled=banner.texture!=null;
            // Candidate scene preserves the gate and wheel line. Its extra locomotive remains a documented art deviation.
            banner.uvRect=new Rect(0,(793f-120-1983f*195/810)/793,1,(1983f*195/810)/793);
            var header=Control("PassSeason",transform,"",25,303,810,70,()=>season?.Invoke(),27);seasonLabel=Text("SeasonLabel",header.transform,"",14,6,240,55,24,TextAlignmentOptions.Left);
            progressText=Text("PassProgress",header.transform,"",270,4,520,44,26,TextAlignmentOptions.Right);
            var gauge=Rect("PassXpGauge",header.transform,260,48,540,16);
            var track=Rect("PassXpTrack",gauge,2,2,536,12).gameObject.AddComponent<Image>();track.color=new Color(.015f,.08f,.1f);track.raycastTarget=false;
            progressFill=AuthFormVisuals.Rect("PassXpFill",track.transform).gameObject.AddComponent<Image>();progressFill.color=new Color(.1f,.75f,.96f);progressFill.raycastTarget=false;
            var gaugeFrame=Rect("PassXpFrame",gauge,0,0,540,16);
            void Edge(string name,float x,float y,float w,float h){var image=Rect(name,gaugeFrame,x,y,w,h).gameObject.AddComponent<Image>();image.color=new Color(.34f,.48f,.50f);image.raycastTarget=false;}
            Edge("Top",0,0,540,2);Edge("Bottom",0,14,540,2);Edge("Left",0,2,2,12);Edge("Right",538,2,2,12);
            Control("PassRewardsTab",transform,"보상",25,385,405,59,()=>tab?.Invoke(false),33);Control("PassProgressTab",transform,"진행",430,385,405,59,()=>tab?.Invoke(true),33);
            rewardsActive=Rect("RewardsTabActive",transform,37,437,381,4).gameObject;var rewardLine=rewardsActive.AddComponent<Image>();rewardLine.color=new Color(.1f,.8f,1);rewardLine.raycastTarget=false;
            progressActive=Rect("ProgressTabActive",transform,442,437,381,4).gameObject;var progressLine=progressActive.AddComponent<Image>();progressLine.color=new Color(.1f,.8f,1);progressLine.raycastTarget=false;
            var heads=Rect("PassColumnHeaders",transform,25,451,790,46);columnHeaders=heads.gameObject;Text("LevelHeading",heads,"레벨",0,0,140,46,27,TextAlignmentOptions.Center);Text("FreeHeading",heads,"무료",148,0,310,46,27,TextAlignmentOptions.Center);Text("PremiumHeading",heads,"프리미엄",465,0,310,46,27,TextAlignmentOptions.Center).color=new Color(.9f,.74f,.36f);
            var viewport=Rect("PassViewport",transform,25,500,810,545);viewport.gameObject.AddComponent<RectMask2D>();viewport.gameObject.AddComponent<Image>().color=Color.clear;scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;content=Rect("PassRows",viewport,0,0,790,545);scroll.content=content;
            var bar=Rect("PassScrollbar",transform,817,505,13,535).gameObject.AddComponent<Scrollbar>();bar.gameObject.AddComponent<Image>().color=new Color(.04f,.06f,.07f);bar.direction=Scrollbar.Direction.BottomToTop;
            var handle=AuthFormVisuals.Rect("Handle",bar.transform).gameObject.AddComponent<Image>();handle.color=new Color(.12f,.66f,.83f);bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;scroll.verticalScrollbar=bar;
            empty=Text("PassEmpty",transform,"",60,660,740,145,30,TextAlignmentOptions.Center);
            entitlement=Text("PassEntitlement",transform,"",40,1060,420,55,25,TextAlignmentOptions.Left);var purchase=Control("PassPurchase",transform,"구매 준비 중",485,1057,325,61,null,26);purchase.interactable=false;
            notice=Text("PassNotice",transform,"",40,1123,770,59,20,TextAlignmentOptions.Center);
            Button all=null;all=AuthFormVisuals.MetalButton(transform,"PassBulk","전체 수령",font,true,()=>Dispatch(all,bulk));AuthFormVisuals.Place((RectTransform)all.transform,120,1190,620,98);bulkButton=all;
            refreshButton=Control("RefreshPass",transform,"진행 갱신",25,1294,230,44,()=>refresh?.Invoke(),21);
            Text("PassCoopNotice",transform,"협동 참가자 진행은 확인할 수 없습니다.",271,1295,560,39,18,TextAlignmentOptions.Right);
        }
        public void Render(SeasonPassDisplayState state)
        {
            CurrentState=state;seasonLabel.text=state.Season;progressText.text=state.ProgressText;entitlement.text=state.Entitlement;notice.text=state.Notice.Trim();
            rewardsActive.SetActive(!state.ShowObjectives);progressActive.SetActive(state.ShowObjectives);
            progressFill.rectTransform.anchorMin=Vector2.zero;progressFill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(state.Progress),1);progressFill.rectTransform.offsetMin=progressFill.rectTransform.offsetMax=Vector2.zero;
            refreshButton.interactable=state.CanRefresh;refreshButton.GetComponentInChildren<TMP_Text>().text=state.RefreshText;bulkButton.interactable=state.CanBulk;bulkButton.GetComponentInChildren<TMP_Text>().text=state.BulkText;columnHeaders.SetActive(!state.ShowObjectives);
            empty.gameObject.SetActive(state.Rewards.Count==0);empty.text=string.IsNullOrWhiteSpace(state.Notice)?"패스 정보 확인 필요":state.Notice.Trim();
            string key=state.PolicyId+"|"+state.ShowObjectives+"|"+string.Join("|",System.Linq.Enumerable.Select(state.Rewards,r=>r.Payload+r.Status+r.CanClaim+r.Caption))+string.Join("|",state.Objectives);
            if(key!=rendered)
            {
                bool reset=rendered==null||CurrentRenderedPolicy!=state.PolicyId||CurrentRenderedTab!=state.ShowObjectives;float prior=scroll.verticalNormalizedPosition;rendered=key;CurrentRenderedPolicy=state.PolicyId;CurrentRenderedTab=state.ShowObjectives;
                for(int i=content.childCount-1;i>=0;i--){var child=content.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
                if(state.ShowObjectives)
                {
                    for(int i=0;i<state.Objectives.Count;i++){var row=Plate("PassObjective"+i,content,0,i*122,790,115);Text("Objective",row,state.Objectives[i],15,9,760,97,23,TextAlignmentOptions.Center);}
                    content.sizeDelta=new Vector2(790,Mathf.Max(545,state.Objectives.Count*122));
                }
                else
                {
                    foreach(var reward in state.Rewards)
                    {
                        float y=(reward.Level-1)*109;if(reward.Track=="FREE")Text("PassLevel"+reward.Level,content,"Lv."+reward.Level,0,y+17,140,72,28,TextAlignmentOptions.Center);
                        var choice=reward;var button=Control("ClaimPass"+reward.Level+reward.Track,content,"",reward.Track=="FREE"?148:465,y,310,103,()=>claim?.Invoke(choice),23);button.interactable=reward.CanClaim;
                        // Track identity is distinct from ownership/claim status, which remains textual and authoritative.
                        if(reward.Track=="PREMIUM"){var surface=button.GetComponentInChildren<IndependentMetalSurface>();surface.Backplate.color=new Color(.28f,.24f,.17f);surface.Frame.color=new Color(.59f,.48f,.30f);}
                        var symbol=Rect("RewardSymbol",button.transform,14,13,45,48).gameObject.AddComponent<RewardResourceSymbol>();symbol.Kind=reward.Symbol;symbol.raycastTarget=false;
                        Text("Reward",button.transform,reward.Caption,72,9,227,48,20,TextAlignmentOptions.Center);
                        if(reward.CanClaim){var accent=Rect("ClaimAvailablePlate",button.transform,82,61,198,32).gameObject.AddComponent<Image>();accent.color=new Color(.025f,.24f,.35f);accent.raycastTarget=false;}
                        var caption=reward.Status=="CLAIMED"?"수령 완료":reward.Status=="PREMIUM_REQUIRED"?"프리미엄 필요":reward.Status=="LOCKED"?"잠김":"받기";Text("Status",button.transform,caption,71,60,227,32,22,TextAlignmentOptions.Center);
                        if(reward.Status=="CLAIMED"){var check=Rect("ReceivedCheck",button.transform,277,66,19,19).gameObject.AddComponent<RewardResourceSymbol>();check.Kind="CHECK";check.raycastTarget=false;}
                    }
                    content.sizeDelta=new Vector2(790,Mathf.Max(545,state.Rewards.Count/2*109));
                }
                Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=reset?1:prior;
                // Keep the native thumb proportional even before the first gameplay LateUpdate.
                UpdateScrollThumb();
            }
            foreach(var button in GetComponentsInChildren<Button>())foreach(var label in button.GetComponentsInChildren<TMP_Text>())label.color=label.name=="Reward"||button.interactable?new Color(.93f,.93f,.88f):new Color(.48f,.53f,.55f);
        }
        string CurrentRenderedPolicy;bool CurrentRenderedTab;
        void UpdateScrollThumb()
        {
            scroll.verticalScrollbar.size=Mathf.Clamp01(scroll.viewport.rect.height/Mathf.Max(1,content.rect.height));
            // ScrollRect normally synchronizes this in LateUpdate; render and resize must also agree before that frame.
            scroll.verticalScrollbar.SetValueWithoutNotify(scroll.verticalNormalizedPosition);
        }
        void Dispatch(Button button,Action action){if(bound&&isActiveAndEnabled&&button.interactable&&button.gameObject.activeInHierarchy)action?.Invoke();}
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(r,x,y,w,h);return r;}
        RectTransform Plate(string name,Transform parent,float x,float y,float w,float h)
        {var r=Rect(name,parent,x,y,w,h);var back=r.gameObject.AddComponent<Image>();back.color=new Color(.025f,.038f,.045f);back.raycastTarget=false;var surface=IndependentMetalSurface.Create(r,true,new Color(.43f,.46f,.48f));surface.Lights.gameObject.SetActive(false);surface.Backplate.Inset=new Vector2(8,8);surface.Frame.BorderScale=.24f;surface.Frame.color=new Color(.6f,.6f,.58f);return r;}
        Button Control(string name,Transform parent,string label,float x,float y,float w,float h,Action action,float size)
        {var r=Plate(name,parent,x,y,w,h);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.targetGraphic.raycastTarget=true;Text("Label",r,label,6,4,w-12,h-8,size,TextAlignmentOptions.Center);b.onClick.AddListener(()=>Dispatch(b,action));return b;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,TextAlignmentOptions alignment)
        {var text=AuthFormVisuals.Text(parent,name,value,font,size,false,alignment);AuthFormVisuals.Place(text.rectTransform,x,y,w,h);text.enableAutoSizing=true;text.fontSizeMin=size*.75f;text.fontSizeMax=size;return text;}
    }
}
