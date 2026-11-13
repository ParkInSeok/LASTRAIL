using System;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Display and input only. Payment, provider verification and recovery belong to controllers.
    public sealed class SharedRoulettePopup : MonoBehaviour
    {
        RectTransform panel, equipmentDeck;
        SharedRouletteVisual visual;
        TMP_Text notice, mainLabel, basicLabel, oddsText;
        Button mainButton, refreshButton, basicButton, closeButton, oddsButton;
        IndependentMetalSurface mainSkin;RouletteButtonGlowGraphic buttonGlow;
        GameObject odds;
        Action main, refresh, basic, close;
        bool confirmingBasic, bound;
        bool mainRequested;

        public static SharedRoulettePopup Create(Transform parent, TMP_FontAsset font, string prefix)
        {
            var root=AuthFormVisuals.Rect("SharedRoulettePopup",parent);
            AuthFormVisuals.Place(root,0,0,1600,900);
            var view=root.gameObject.AddComponent<SharedRoulettePopup>();
            view.Build(font,prefix);return view;
        }
        public void Bind(Action main,Action refresh,Action basic,Action close)
        {this.main=main;this.refresh=refresh;this.basic=basic;this.close=close;confirmingBasic=false;bound=true;ObserveReveal();}
        public void Unbind(){main=refresh=basic=close=null;confirmingBasic=false;bound=false;if(visual!=null){visual.RevealCompleted-=RefreshMainInteraction;visual.CancelAndClear();}}
        void OnEnable(){ObserveReveal();}
        void OnDisable(){Unbind();}
        void OnDestroy(){Unbind();}
        void ObserveReveal()
        {
            if(visual==null)return;
            visual.RevealCompleted-=RefreshMainInteraction;
            if(isActiveAndEnabled)visual.RevealCompleted+=RefreshMainInteraction;
        }
        void RefreshMainInteraction()
        {
            bool enabled=mainRequested&&!visual.IsAnimating;
            mainButton.interactable=enabled;mainSkin.SetPrimaryInteractable(enabled);
            mainSkin.Lights.gameObject.SetActive(false);
            buttonGlow.color=enabled?Color.white:new Color(.3f,.4f,.5f,.2f);
        }
        void Build(TMP_FontAsset font,string prefix)
        {
            var dim=gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.76f);
            panel=AuthFormVisuals.Rect("RoulettePanel",transform);
            AuthFormVisuals.Place(panel,70,170,800,1320);
            var room=AuthFormVisuals.Rect("RouletteRoom",panel).gameObject.AddComponent<RawImage>();
            room.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/roulette-room-v02")??LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/roulette-room");room.raycastTarget=false;
            room.rectTransform.offsetMin=new Vector2(18,18);room.rectTransform.offsetMax=new Vector2(-18,-18);
            room.uvRect=OperatorVisualCatalog.Crop(room.texture as Texture2D,764,1284);
            var shade=AuthFormVisuals.Rect("RoomShade",panel).gameObject.AddComponent<Image>();shade.color=new Color(0,.01f,.02f,.13f);shade.raycastTarget=false;
            void LocalLight(string name,float x,float y,float width,float height,Color tint)
            {
                var light=AuthFormVisuals.Rect(name,panel).gameObject.AddComponent<RouletteAmbientLightGraphic>();
                AuthFormVisuals.Place(light.rectTransform,x,y,width,height);light.color=tint;light.raycastTarget=false;
            }
            LocalLight("LeftLampSpill",18,95,230,400,new Color(1,.48f,.13f,.23f));
            LocalLight("RightLampSpill",557,100,225,365,new Color(1,.53f,.19f,.16f));
            LocalLight("MonitorSpill",622,260,160,350,new Color(.02f,.49f,1,.12f));
            var surface=IndependentMetalSurface.Create(panel,false,Color.white);surface.Backplate.gameObject.SetActive(false);surface.Frame.gameObject.SetActive(false);surface.Lights.gameObject.SetActive(false);RouletteFrameGraphic.Create(panel,66);
            equipmentDeck=AuthFormVisuals.Rect("RewardEquipmentDeck",panel);
            AuthFormVisuals.Place(equipmentDeck,25,740,750,460);
            var deckSurface=IndependentMetalSurface.Create(equipmentDeck,false,new Color(.48f,.51f,.53f));
            deckSurface.Frame.gameObject.SetActive(false);deckSurface.Lights.gameObject.SetActive(false);
            deckSurface.Backplate.Inset=Vector2.zero;deckSurface.Backplate.Bevel=20;
            var deckRail=AuthFormVisuals.Rect("DeckTopRail",equipmentDeck).gameObject.AddComponent<Image>();
            AuthFormVisuals.Place(deckRail.rectTransform,18,8,714,4);deckRail.color=new Color(.44f,.37f,.25f);deckRail.raycastTarget=false;
            visual=SharedRouletteVisual.Create(panel,font);AuthFormVisuals.Place((RectTransform)visual.transform,0,45,800,980);
            notice=AuthFormVisuals.Text(panel,"RouletteNotice","",font,23,false,TextAlignmentOptions.Center);
            AuthFormVisuals.Place(notice.rectTransform,45,1030,710,50);
            Button Make(string suffix,string label,Action action,bool primary=false)
            {
                Button button=null;button=AuthFormVisuals.MetalButton(panel,prefix+suffix,label,font,primary,()=>{if(bound&&isActiveAndEnabled&&button.interactable)action?.Invoke();});return button;
            }
            mainButton=Make(prefix=="Ad"?"MainAction":"Main","룰렛 돌리기",()=>{confirmingBasic=false;main?.Invoke();},true);
            mainLabel=mainButton.GetComponentInChildren<TMP_Text>();
            var old=mainButton.transform.Find("AuthButtonSurface");if(old!=null)old.gameObject.SetActive(false);
            mainSkin=IndependentMetalSurface.Create((RectTransform)mainButton.transform,true,Color.white);mainSkin.EmphasizePrimary(1.65f);mainSkin.Frame.gameObject.SetActive(false);mainSkin.Backplate.Inset=new Vector2(15,15);mainSkin.Backplate.Bevel=13;RouletteInsetFrameGraphic.Create(mainButton.transform,true);mainButton.targetGraphic=mainSkin.Backplate;
            var glass=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/roulette-button-glass-v01");if(glass!=null){mainSkin.Backplate.texture=glass;mainSkin.Backplate.uvRect=new Rect(0,0,1,1);}
            buttonGlow=AuthFormVisuals.Rect("ButtonNeon",mainButton.transform).gameObject.AddComponent<RouletteButtonGlowGraphic>();buttonGlow.raycastTarget=false;mainLabel.transform.SetAsLastSibling();
            refreshButton=Make("Refresh","다시 확인",()=>{confirmingBasic=false;refresh?.Invoke();});
            basicButton=Make("Basic","기본 보상 받기",()=>{if(!confirmingBasic){confirmingBasic=true;basicLabel.text="기본 수령 확정";notice.text="기본 보상으로 받으면 이번 룰렛은 종료됩니다.";return;}confirmingBasic=false;basic?.Invoke();});
            basicLabel=basicButton.GetComponentInChildren<TMP_Text>();
            foreach(var secondary in new[]{refreshButton,basicButton})
            {
                var caption=secondary.GetComponentInChildren<TMP_Text>();caption.fontSize=30;
                caption.rectTransform.offsetMin=new Vector2(28,10);caption.rectTransform.offsetMax=new Vector2(-28,-10);
            }
            closeButton=AuthFormVisuals.Link(panel,prefix+"Close","×",font,()=>{if(bound&&isActiveAndEnabled)close?.Invoke();},48);
            AuthFormVisuals.Place((RectTransform)closeButton.transform,716,18,66,66);
            oddsButton=AuthFormVisuals.Link(panel,"RouletteOdds","확률 정보",font,()=>{if(bound&&isActiveAndEnabled)odds.SetActive(true);},25);
            var oddsPanel=AuthFormVisuals.Rect("ProbabilityDetails",panel);AuthFormVisuals.Place(oddsPanel,50,350,700,520);odds=oddsPanel.gameObject;
            IndependentMetalSurface.Create(oddsPanel,false,Color.white);
            var details=AuthFormVisuals.Text(oddsPanel,"OddsText","총 2배  60%\n총 3배  30%\n총 4배  10%\n\n표시된 기본 재화에 배수가 적용됩니다.\n최초 클리어·특수 보상은 제외됩니다.",font,30,false,TextAlignmentOptions.Center);
            oddsText=details;AuthFormVisuals.Place(details.rectTransform,35,40,630,340);
            var dismiss=AuthFormVisuals.MetalButton(oddsPanel,"CloseOdds","확인",font,true,()=>{if(bound&&isActiveAndEnabled)odds.SetActive(false);});AuthFormVisuals.Place((RectTransform)dismiss.transform,100,395,500,85);odds.SetActive(false);
            ObserveReveal();
        }
        public void ConfigureProbability(string detail,int[] multipliers){oddsText.text=detail;visual.ConfigureMultipliers(multipliers);}
        public void Render(JObject result,JArray lines,string message,string actionLabel,bool enabled,bool busy,bool completed,bool allowRefresh,bool allowBasic)
        {
            var root=(RectTransform)transform;
            AuthFormVisuals.Place(root,0,0,1600,900);
            AuthFormVisuals.Place(panel,40,40,1520,820);
            AuthFormVisuals.Place(equipmentDeck,735,130,760,600);
            AuthFormVisuals.Place((RectTransform)visual.transform,0,0,1520,820);
            AuthFormVisuals.Place((RectTransform)visual.transform.Find("Wheel"),25,80,680,680);
            AuthFormVisuals.Place((RectTransform)visual.transform.Find("Rewards"),745,160,740,240);
            AuthFormVisuals.Place(notice.rectTransform,755,425,710,75);
            AuthFormVisuals.Place((RectTransform)closeButton.transform,1435,18,66,66);
            AuthFormVisuals.Place((RectTransform)odds.transform,780,125,700,520);
            visual.Present(result,completed&&result==null?new JArray():lines);visual.SetRewardsVisible(!completed||result!=null);
            if(completed){confirmingBasic=false;odds.SetActive(false);}
            notice.text=completed&&result!=null?"":confirmingBasic?"기본 보상으로 받으면 이번 룰렛은 종료됩니다.":message??"";
            mainLabel.text=actionLabel;mainRequested=!busy&&enabled;RefreshMainInteraction();
            bool receiptComplete=completed&&result!=null;
            float mainY=receiptComplete?(visual.RequiresRewardScroll?1040:1020):completed?1070:1090;
            float mainHeight=receiptComplete?(visual.RequiresRewardScroll?145:155):120;
            AuthFormVisuals.Place((RectTransform)mainButton.transform,755,515,710,120);
            refreshButton.gameObject.SetActive(!completed&&allowRefresh);refreshButton.interactable=!busy;
            basicButton.gameObject.SetActive(!completed&&basic!=null);basicButton.interactable=!busy&&allowBasic;
            closeButton.gameObject.SetActive(!completed);oddsButton.gameObject.SetActive(!completed);
            if(!confirmingBasic)basicLabel.text="기본 보상 받기";
            AuthFormVisuals.Place((RectTransform)refreshButton.transform,755,650,340,70);
            AuthFormVisuals.Place((RectTransform)basicButton.transform,1125,650,340,70);
            AuthFormVisuals.Place((RectTransform)oddsButton.transform,910,745,400,45);
        }
    }
}
