using System;
using System.Linq;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Display and input only. LobbyCommandDeck binds its existing result actions once.
    public sealed class RunVictoryView : MonoBehaviour
    {
        public RunVictoryDisplayState CurrentState { get; private set; }
        Action claim, refresh, back, recover, ad, operations;
        TMP_FontAsset font;
        Sprite frame, plate;
        RectTransform lower, details, rewards;
        RawImage environment, train;
        TMP_Text title, operation, notice, rewardStatus, claimLabel, detailText;
        Button claimButton, refreshButton, recoverButton, adButton, operationsButton;
        string renderedRewards;
        bool bound;
        readonly TMP_Text[] participantLabels=new TMP_Text[5];
        readonly RawImage[] participantPortraits=new RawImage[5];
        public static RunVictoryView Create(Transform parent, TMP_FontAsset font, Sprite borrowedFrame)
        {
            var root=AuthFormVisuals.Rect("RunVictory",parent);var view=root.gameObject.AddComponent<RunVictoryView>();view.font=font;view.frame=borrowedFrame;view.Build();return view;
        }
        public void Bind(Action claim,Action refresh,Action back,Action recover)
        {this.claim=claim;this.refresh=refresh;this.back=back;this.recover=recover;bound=true;}
        public void BindAd(Action action){ad=action;}
        public void BindOperations(Action action){operations=action;}
        public void Unbind(){bound=false;claim=refresh=back=recover=ad=operations=null;}
        void OnDisable(){Unbind();}
        void OnDestroy(){Unbind();if(plate!=null){if(Application.isPlaying)Destroy(plate);else DestroyImmediate(plate);}}
        void Build()
        {
            var texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryUi/secondary");
            if(texture!=null)plate=Sprite.Create(texture,new Rect(texture.width*.15f,texture.height*.2f,texture.width*.74f,texture.height*.5f),new Vector2(.5f,.5f));
            Plate("VictoryBacking",transform,0,0,941,1672);
            environment=Art("VictoryEnvironment",transform,"NoncombatVictory/environment",0,0,941,941f*1024/1536);
            train=Art("VictoryTrain",transform,"NoncombatVictory/train",0,195,920,445);
            Text("VictoryBrand",transform,"LAST RAIL",290,15,361,66,32).alignment=TextAlignmentOptions.Center;
            title=Text("VictoryTitle",transform,"",50,93,841,125,79);title.alignment=TextAlignmentOptions.Center;
            operation=Text("VictoryOperation",transform,"",50,224,841,57,31);operation.alignment=TextAlignmentOptions.Center;
            lower=Rect("VictoryLower",transform,0,0,941,1672);
            Plate("VictoryMetrics",lower,23,631,895,106);
            Text("VictoryWave",lower,"완료 웨이브\n기록 확인 필요",55,641,400,86,29).alignment=TextAlignmentOptions.Center;
            Text("VictoryTime",lower,"전투 시간\n기록 확인 필요",491,641,395,86,29).alignment=TextAlignmentOptions.Center;
            Plate("VictoryParticipants",lower,23,754,895,335);Text("ParticipantsHeading",lower,"출발 편성",56,767,819,61,38);
            for(int i=0;i<5;i++)
            {
                var row=Plate("VictoryParticipant"+i,lower,43+i*173,837,163,215);
                participantPortraits[i]=Rect("DeparturePortrait",row,13,12,137,140).gameObject.AddComponent<RawImage>();participantPortraits[i].raycastTarget=false;participantPortraits[i].enabled=false;
                participantLabels[i]=Text("DepartureOperatorLabel",row,"해당 전투\n편성 기록\n확인 필요",13,40,137,123,24);participantLabels[i].alignment=TextAlignmentOptions.Center;
            }
            Plate("VictoryRewards",lower,23,1102,895,305);Text("RewardHeading",lower,"획득 보상",56,1113,571,64,39);
            rewardStatus=Text("VictoryRewardStatus",lower,"",641,1118,248,55,27);rewardStatus.alignment=TextAlignmentOptions.Center;
            var viewport=Rect("VictoryRewardViewport",lower,49,1186,843,196);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;scroll.viewport=viewport;
            rewards=Rect("VictoryRewardRows",viewport,0,0,843,196);scroll.content=rewards;
            Control("VictoryDetails",lower,"전투 상세",23,1424,430,72,()=>{details.gameObject.SetActive(!details.gameObject.activeSelf);});
            adButton=Control("VictoryAd",lower,"광고 룰렛 · 총 2~4배",470,1424,448,72,()=>ad?.Invoke());
            claimButton=Metal("VictoryClaim",lower,"보상 받기",48,1525,501,111,true,()=>claim?.Invoke());claimLabel=claimButton.GetComponentInChildren<TMP_Text>();
            Metal("VictoryReturn",lower,"로비로",570,1525,324,111,false,()=>back?.Invoke());
            details=Plate("VictoryDetailsPanel",lower,45,807,851,597);details.SetAsLastSibling();
            details.GetComponent<Image>().sprite=null;details.GetComponent<Image>().color=new Color(.025f,.037f,.044f,1);
            Text("DetailHeading",details,"전투 상세",26,18,797,66,36);
            detailText=Text("VictoryDetailText",details,"",26,103,797,180,27);detailText.alignment=TextAlignmentOptions.TopLeft;
            notice=Text("VictoryReadNotice",details,"",26,292,797,118,25);notice.alignment=TextAlignmentOptions.TopLeft;
            refreshButton=Control("VictoryRefresh",details,"기록 새로고침",26,427,388,67,()=>refresh?.Invoke());
            operationsButton=Control("VictoryOperations",details,"구간 보기",435,427,388,67,()=>operations?.Invoke());
            recoverButton=Control("VictoryRecover",details,"결과 전송 다시 확인",26,509,797,65,()=>recover?.Invoke());
            details.gameObject.SetActive(false);
        }
        public void Render(RunVictoryDisplayState state)
        {
            CurrentState=state;title.text=state.Title;operation.text=state.Operation;rewardStatus.text=state.RewardStatus;
            adButton.interactable=state.CanOpenAd;adButton.GetComponentInChildren<TMP_Text>().text=state.AdLabel;
            adButton.gameObject.SetActive(state.ShowAdEntry);
            AuthFormVisuals.Place((RectTransform)lower.Find("VictoryDetails"),23,1424,state.ShowAdEntry?430:895,72);
            claimButton.interactable=state.CanClaim;claimLabel.text=state.ClaimLabel;refreshButton.interactable=state.CanRefresh;recoverButton.interactable=state.CanRecover;
            detailText.text=state.HistoryNotice;notice.text=state.Notice;
            operationsButton.interactable=state.CanViewOperations;
            for(int i=0;i<participantLabels.Length;i++)
            {
                var id=state.History.HasFormation?state.History.OperatorIds[i]:null;
                var portrait=participantPortraits[i];portrait.texture=OperatorVisualCatalog.Portrait(id);portrait.enabled=portrait.texture!=null;
                portrait.uvRect=OperatorVisualCatalog.Crop(portrait.texture as Texture2D,137,140);
                participantLabels[i].text=state.History.HasFormation?state.History.Names[i]:state.History.FormationNotice;
                AuthFormVisuals.Place(participantLabels[i].rectTransform,13,state.History.HasFormation?159:40,137,state.History.HasFormation?40:123);
            }
            string key=string.Join("\n",state.RewardItems.Select(item=>item.RenderKey))+"|"+state.RewardStatus+"|"+state.HasRewardRead;
            if(key!=renderedRewards)
            {
                renderedRewards=key;
                for(int i=rewards.childCount-1;i>=0;i--){var child=rewards.GetChild(i);child.gameObject.SetActive(false);if(child.name.StartsWith("VictoryReward")&&child.name!="VictoryRewardsUnavailable"&&int.TryParse(child.name.Substring("VictoryReward".Length),out int index)&&index>=state.Rewards.Count){child.SetParent(null,false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}}
                if(state.Rewards.Count==0){var unavailable=rewards.Find("VictoryRewardsUnavailable")?.GetComponent<TMP_Text>()??Text("VictoryRewardsUnavailable",rewards,"",25,42,793,105,31);unavailable.gameObject.SetActive(true);unavailable.text=state.HasRewardRead?"보상 항목이 없습니다.":"보상 내역 확인 필요";unavailable.alignment=TextAlignmentOptions.Center;}
                for(int i=0;i<state.Rewards.Count;i++)
                {
                    var cell=rewards.Find("VictoryReward"+i) as RectTransform;if(cell==null){cell=Plate("VictoryReward"+i,rewards,(i%3)*284,(i/3)*187,273,174);Text("RewardName",cell,"",14,5,245,37,27).alignment=TextAlignmentOptions.Center;Text("RewardValue",cell,"",14,127,245,39,29);}cell.gameObject.SetActive(true);
                    var item=state.RewardItems[i];
                    cell.Find("RewardName").GetComponent<TMP_Text>().text=item.Name;
                    RewardIconPresentation.Create(cell,font,item.IconId,new Rect(91,43,91,80));
                    var value=cell.Find("RewardValue").GetComponent<TMP_Text>();value.text=item.QuantityText;
                    value.alignment=TextAlignmentOptions.Center;value.fontSizeMin=16;
                }
                rewards.sizeDelta=new Vector2(843,Mathf.Max(196,Mathf.CeilToInt(state.Rewards.Count/3f)*187));
            }
            foreach(var button in GetComponentsInChildren<Button>(true))foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=button.interactable?new Color(.94f,.93f,.88f):new Color(.49f,.53f,.55f);
        }
        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout(), lowerLandscape=new LandscapeSectionLayout();
        public void SetViewportHeight(float height)
        {
            landscape.Apply(transform,(name,r)=>
            {
                if(name=="VictoryBacking")return new Rect(0,0,1600,900);
                if(name=="VictoryLower")return new Rect(0,0,1600,900);
                if(name=="VictoryEnvironment")return new Rect(20,230,750,500);
                if(name=="VictoryTrain")return new Rect(20,390,736,356);
                if(name=="VictoryBrand")return new Rect(215,15,361,66);
                if(name=="VictoryTitle")return new Rect(35,95,720,125);
                if(name=="VictoryOperation")return new Rect(35,225,720,57);
                return r;
            });
            lower.localScale=Vector3.one;lower.sizeDelta=new Vector2(1600,900);
            lowerLandscape.Apply(lower,(name,r)=>
            {
                if(name=="VictoryDetailsPanel")return new Rect(820,160,680.8f,477.6f);
                if(r.y<1424)return LandscapeSectionLayout.Section(r,631,800,30,.8f);
                return LandscapeSectionLayout.Section(r,1424,800,685,.8f);
            });
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(r,x,y,w,h);return r;}
        RectTransform Plate(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.sprite=plate;image.color=new Color(.27f,.32f,.34f);image.raycastTarget=false;
            var border=AuthFormVisuals.Rect("Frame",r).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.color=new Color(.9f,.79f,.59f);border.raycastTarget=false;return r;
        }
        RawImage Art(string name,Transform parent,string path,float x,float y,float w,float h){var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();image.texture=LastRail.Resources.PreparedContent.Load<Texture2D>(path);image.raycastTarget=false;image.enabled=image.texture!=null;return image;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size){var text=AuthFormVisuals.Text(parent,name,value,font,size);AuthFormVisuals.Place(text.rectTransform,x,y,w,h);text.enableAutoSizing=true;text.fontSizeMin=size*.8f;text.fontSizeMax=size;return text;}
        Button Control(string name,Transform parent,string label,float x,float y,float w,float h,Action action)
        {var r=Plate(name,parent,x,y,w,h);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.targetGraphic.raycastTarget=true;Text("Label",r,label,15,5,w-30,h-10,31).alignment=TextAlignmentOptions.Center;button.onClick.AddListener(()=>{if(bound&&isActiveAndEnabled&&button.interactable&&button.gameObject.activeInHierarchy)action?.Invoke();});return button;}
        Button Metal(string name,Transform parent,string label,float x,float y,float w,float h,bool primary,Action action)
        {Button button=null;button=AuthFormVisuals.MetalButton(parent,name,label,font,primary,()=>{if(bound&&isActiveAndEnabled&&button.interactable&&button.gameObject.activeInHierarchy)action?.Invoke();});AuthFormVisuals.Place((RectTransform)button.transform,x,y,w,h);return button;}
    }
}

