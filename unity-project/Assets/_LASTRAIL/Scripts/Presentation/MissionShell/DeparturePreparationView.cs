using System;
using System.Collections.Generic;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Display and input intent only. The lobby controller binds existing commands;
    // authoritative account, quote and selection state never lives in this view.
    public sealed class DeparturePreparationView : MonoBehaviour
    {
        TMP_FontAsset font;
        Sprite frame;
        Sprite neutralPlate;
        DepartureScenePresentation scene;
        DeparturePanelPresentation panels;
        // Preserve the approved compact (9:16) viewport at the existing 941-unit reference width.
        const float DesignHeight=1672,CompactViewportHeight=941f*16f/9f;
        RectTransform guideButton;
        readonly Dictionary<RectTransform,Vector2> sectionPositions=new Dictionary<RectTransform,Vector2>();
        float displayedExtra=-1;
        TMP_Text operation, notice, fuel;
        readonly TMP_Text[] balances=new TMP_Text[3], members=new TMP_Text[5];
        readonly RawImage[] portraits=new RawImage[5];
        readonly string[] memberIds=new string[5];
        NumberedFormationView numberedFormation;
        Button risk, preview, start, formation, train, support, operationChange;
        readonly List<(Button owner,Image ink)> changeArrowInk=new List<(Button,Image)>();
        public IReadOnlyList<RectTransform> GuideTargets { get; private set; }
        Action<string> blockAction;
        Func<string,bool> blockAvailability;
        DepartureBlockState displayedBlocks;
        Button blockDetails;
        RectTransform blockPanel,blockContent;
        readonly Dictionary<string,Button> blockButtons=new Dictionary<string,Button>();
        public void BindBlockActions(Action<string> action){blockAction=action;}
        public void BindBlockAvailability(Func<string,bool> available){blockAvailability=available;}
        public void BindDepartureIntent(Func<Func<bool>> capture){((DepartureInputButton)start).BindIntent(capture);}
        public void RenderBlocks(DepartureBlockState state)
        {
            if(blockDetails==null)BuildBlockList();
            if(ReferenceEquals(displayedBlocks,state)){
                if(state!=null)foreach(var reason in state.Reasons)blockButtons[reason.Code].interactable=reason.Action!=DepartureBlockAction.None&&(blockAvailability?.Invoke(reason.Code)??true);
                return;
            }displayedBlocks=state;
            foreach(var row in blockButtons.Values)row.gameObject.SetActive(false);
            int count=state?.Reasons.Count??0;
            blockDetails.interactable=count>0;blockDetails.GetComponentInChildren<TMP_Text>().text=count>0?"출격 제한 "+count+"개 확인":"출격 조건";
            if(count==0){blockPanel.gameObject.SetActive(false);return;}
            for(int i=0;i<count;i++){
                var reason=state.Reasons[i];var row=blockButtons[reason.Code];row.gameObject.SetActive(true);
                ((RectTransform)row.transform).anchoredPosition=new Vector2(0,-i*112);
                row.GetComponentInChildren<TMP_Text>().text=reason.Message+"\n"+reason.ActionLabel;
                row.interactable=reason.Action!=DepartureBlockAction.None&&(blockAvailability?.Invoke(reason.Code)??true);
            }
            blockContent.sizeDelta=new Vector2(0,count*112);
        }
        void BuildBlockList()
        {
            blockDetails=Control("DepartureBlockDetails","출격 조건",35,398,285,60,()=>{if(displayedBlocks?.HasBlocks==true){blockPanel.gameObject.SetActive(true);blockPanel.SetAsLastSibling();}},false);
            blockPanel=Rect("DepartureBlockPanel",20,410,901,1010);blockPanel.gameObject.AddComponent<Image>().color=new Color(.035f,.055f,.07f,.99f);
            var title=Text("DepartureBlockTitle","출격 제한 사유",45,430,590,60,36);title.transform.SetParent(blockPanel,true);
            var close=Control("CloseDepartureBlocks","닫기",745,431,145,58,()=>blockPanel.gameObject.SetActive(false),false);close.transform.SetParent(blockPanel,true);
            var viewport=Rect("DepartureBlockViewport",45,510,851,880);viewport.SetParent(blockPanel,true);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            blockContent=AuthFormVisuals.Rect("DepartureBlockContent",viewport);blockContent.anchorMin=new Vector2(0,1);blockContent.anchorMax=new Vector2(1,1);blockContent.pivot=new Vector2(0,1);blockContent.anchoredPosition=Vector2.zero;scroll.content=blockContent;
            foreach(var reason in DepartureBlockState.FromFlags(8191).Reasons){
                var code=reason.Code;
                var button=AuthFormVisuals.MetalButton(blockContent,"ResolveDepartureBlock_"+code,"",font,false,()=>{if(!isActiveAndEnabled||!blockPanel.gameObject.activeInHierarchy||!blockButtons.TryGetValue(code,out var active)||!active.gameObject.activeInHierarchy||!active.interactable)return;blockAction?.Invoke(code);});
                var rect=(RectTransform)button.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(0,102);
                var text=button.GetComponentInChildren<TMP_Text>();text.fontSize=25;text.alignment=TextAlignmentOptions.MidlineLeft;
                blockButtons.Add(code,button);button.gameObject.SetActive(false);
            }
            blockPanel.gameObject.SetActive(false);
        }

        public static DeparturePreparationView Create(Transform parent,TMP_FontAsset font,Sprite frame,
            Action back,Action operation,Action formation,Action train,Action support,
            Action risk,Action preview,Action start,Action guide,Action refresh)
        {
            var rect=AuthFormVisuals.Rect("DeparturePreparationView",parent);
            var view=rect.gameObject.AddComponent<DeparturePreparationView>();view.font=font;view.frame=frame;
            view.Build(back,operation,formation,train,support,risk,preview,start,guide,refresh);
            return view;
        }

        void Build(Action back,Action changeOperation,Action changeFormation,Action changeTrain,Action changeSupport,
            Action changeRisk,Action readQuote,Action depart,Action guide,Action refresh)
        {
            var atlas=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryUi/secondary");
            if(atlas!=null){neutralPlate=Sprite.Create(atlas,new Rect(atlas.width*.15f,atlas.height*.2f,atlas.width*.74f,atlas.height*.5f),new Vector2(.5f,.5f));neutralPlate.name="PreparationNeutralPlate";}
            scene=new DepartureScenePresentation(transform);
            numberedFormation=NumberedFormationView.Create(transform,font,false);
            var numberedRect=(RectTransform)numberedFormation.transform;numberedRect.anchorMin=numberedRect.anchorMax=numberedRect.pivot=new Vector2(0,1);numberedRect.anchoredPosition=new Vector2(25,-120);numberedRect.localScale=Vector3.one*.58f;
            Plate("Header",15,12,911,83);
            Arrow(Control("BackDeparture","",20,18,85,68,back,false),true);
            Text("PreparationTitle","출격 준비",132,17,265,68,47);
            var approved=LastRail.Resources.PreparedContent.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");
            for(int i=0;i<3;i++)
            {
                float x=416+i*171;
                var button=Control("DepartureBalance"+i,"",x,23,165,57,refresh,false);
                var icon=Art("CurrencyIcon"+i,x+8,33,31,36);icon.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/icons-landscape-v01");icon.enabled=icon.texture!=null;
                icon.uvRect=LobbyArtwork.IconUv(i+1);
                if(i==0&&approved!=null&&approved.Fuel!=null){icon.texture=approved.Fuel;icon.uvRect=new Rect(0,0,1,1);icon.enabled=true;}
                balances[i]=Text("Balance"+i,"—",x+45,24,110,54,23);
            }
            guideButton=(RectTransform)Control("DepartureGuideEntry","출격 안내",738,398,170,60,guide,false).transform;
            Plate("OperationPanel",20,486,901,180);
            Text("OperationCaption","작전 선택",50,501,315,53,34);
            var landscape=Art("OperationLandscape",42,552,332,100);SetTexture(landscape,"NoncombatPreparation/cockpit-v01",false);landscape.uvRect=new Rect(.27f,.40f,.49f,.39f);
            operation=Text("OperationName","작전 확인 필요",416,509,311,72,37);
            notice=Text("PreparationNotice","",416,582,311,73,22);
            operationChange=Control("DepartureOperation","변경",747,527,154,68,changeOperation);Arrow(operationChange,false);
            risk=Control("DepartureRisk","위험 I",747,602,154,46,changeRisk,false);

            Plate("FormationPanel",20,676,901,284);
            Text("FormationCaption","대원 편성",50,688,470,53,34);
            formation=Control("DepartureFormation","편성 변경",711,686,191,56,changeFormation);Arrow(formation,false);
            for(int i=0;i<5;i++)
            {
                float x=43+i*174;Plate("OperatorFrame"+i,x,750,164,191);
                var portraitFrame=Rect("PreparationPortraitFrame"+i,x+6,756,152,135);
                portraits[i]=AuthFormVisuals.Rect("PreparationOperator"+i,portraitFrame).gameObject.AddComponent<RawImage>();portraits[i].raycastTarget=false;
                portraits[i].gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                members[i]=Text("PreparationOperatorLabel"+i,"미편성",x+7,892,150,43,23);
            }

            Plate("TrainPanel",20,971,901,231);
            Text("TrainCaption","열차 편성",124,985,460,54,34);
            train=Control("DepartureTrain","변경",747,982,154,57,changeTrain);Arrow(train,false);
            Plate("SupportPanel",20,1213,901,228);
            Text("SupportCaption","지원 스킬",105,1226,460,50,34);
            support=Control("LocalCommandSelection","스킬 변경",711,1223,191,56,changeSupport);Arrow(support,false);
            panels=new DeparturePanelPresentation(transform,font);
            Plate("FuelPanel",20,1452,901,78);
            Text("FuelCaption","출격 비용",50,1466,250,48,33);
            fuel=Text("FuelValue","비용 확인 필요",310,1468,374,45,28);
            preview=Control("PreviewDeparture","비용 확인",719,1464,183,50,readQuote,false);
            start=Control("ConfirmDeparture","출격",190,1536,561,112,depart);
            foreach(RectTransform section in transform)if(section.anchoredPosition.y<=-486)sectionPositions[section]=section.anchoredPosition;
            GuideTargets=Array.AsReadOnly(new[]{(RectTransform)transform.Find("FormationPanel"),(RectTransform)transform.Find("TrainPanel"),(RectTransform)transform.Find("SupportPanel"),(RectTransform)transform.Find("FuelPanel")});
        }

        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout();
        public void SetViewportHeight(float height)
        {
            landscape.Apply(transform,ArrangeLandscapeSection);
            scene.RefreshLayout();
            panels.SetImageLayout(ArrangeLandscapeSection);
            if(blockPanel!=null){var viewport=blockPanel.Find("DepartureBlockViewport") as RectTransform;if(viewport!=null)viewport.sizeDelta=new Vector2(851,650);}
        }

        static Rect ArrangeLandscapeSection(string name,Rect r)
        {
                if(name=="PreparationScene")return new Rect(25,120,752.8f,250);
                if(name=="NumberedFormation")return new Rect(25,120,752.8f,250);
                if(name=="Header")return new Rect(15,12,1570,83);
                if(name.StartsWith("DepartureBalance")&&int.TryParse(name.Substring(16),out var balance))return new Rect(1020+balance*180,23,165,57);
                if(name.StartsWith("CurrencyIcon")&&int.TryParse(name.Substring(12),out var icon))return new Rect(1028+icon*180,33,31,36);
                if(name.StartsWith("Balance")&&int.TryParse(name.Substring(7),out var text))return new Rect(1065+text*180,24,110,54);
                if(name=="DepartureGuideEntry")return new Rect(1380,110,170,60);
                if(name=="DepartureBlockDetails")return new Rect(825,110,285,60);
                if(name=="DepartureBlockPanel")return new Rect(430,150,720,640);
                if(r.y<100)return r;
                if(r.y<676)return LandscapeSectionLayout.Section(r,486,10,400);
                if(r.y<971)return LandscapeSectionLayout.Section(r,676,10,560);
                if(r.y<1213)return LandscapeSectionLayout.Section(r,971,810,200);
                if(r.y<1452)return LandscapeSectionLayout.Section(r,1213,810,410);
                if(r.y<1536)return LandscapeSectionLayout.Section(r,1452,810,620);
                return LandscapeSectionLayout.Section(r,1536,810,710);
        }

        void OnDestroy(){blockAction=null;blockAvailability=null;displayedBlocks=null;if(neutralPlate!=null){if(Application.isPlaying)Destroy(neutralPlate);else DestroyImmediate(neutralPlate);}}

        public void Render(string label,string riskLabel,string message,string[] currency,string cost,
            bool canRisk,bool canPreview,bool canStart,bool canChange,bool canSupport,string startLabel)
        {
            operation.text=label;notice.text=message;fuel.text=cost;
            for(int i=0;i<balances.Length;i++)balances[i].text=currency!=null&&i<currency.Length?currency[i]:"—";
            risk.interactable=canRisk;risk.GetComponentInChildren<TMP_Text>().text="위험 "+riskLabel;
            if(!canStart)((DepartureInputButton)start).CancelIntent();
            preview.interactable=canPreview;start.interactable=canStart;start.GetComponentInChildren<TMP_Text>().text=startLabel;
            operationChange.interactable=formation.interactable=train.interactable=canChange;support.interactable=canChange&&canSupport;
            foreach(var item in changeArrowInk)item.ink.color=item.owner.interactable?new Color(.95f,.95f,.9f):new Color(.35f,.4f,.43f);
        }

        public void Member(int slot,string id,string level,bool leader)
        {
            if(slot<0||slot>=5)return;
            if(memberIds[slot]!=id){memberIds[slot]=id;var texture=NumberedFormationView.Portrait(id);portraits[slot].texture=texture;portraits[slot].uvRect=new Rect(0,0,1,1);if(texture!=null)portraits[slot].GetComponent<AspectRatioFitter>().aspectRatio=(float)texture.width/texture.height;}
            portraits[slot].enabled=portraits[slot].texture!=null;
            members[slot].text=id==null?"미편성":(slot+1)+" "+(leader?"★ ":"")+NumberedFormationView.Label(id)+(string.IsNullOrEmpty(level)?"":"\n"+level);
            numberedFormation.Render(memberIds);
        }

        public void Carriage(int slot,string label,string resource,bool locked=false,string role=null)
        {
            panels.Carriage(slot,label,resource,locked,role);
        }

        public void Support(int slot,string id,string label)
        {
            panels.Support(slot,id,label);
        }

        void SetTexture(RawImage image,string resource,bool crop)
        {
            var texture=resource==null?null:LastRail.Resources.PreparedContent.Load<Texture2D>(resource);image.texture=texture;
            image.enabled=texture!=null;
            if(crop)image.uvRect=OperatorVisualCatalog.Crop(texture,image.rectTransform.rect.width,image.rectTransform.rect.height);
        }

        RectTransform Rect(string name,float x,float y,float w,float h)
        {
            var r=AuthFormVisuals.Rect(name,transform);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        RawImage Art(string name,float x,float y,float w,float h){var image=Rect(name,x,y,w,h).gameObject.AddComponent<RawImage>();image.raycastTarget=false;image.enabled=false;return image;}
        void Plate(string name,float x,float y,float w,float h)
        {
            var r=Rect(name,x,y,w,h);var plate=r.gameObject.AddComponent<Image>();plate.sprite=neutralPlate;plate.color=new Color(.20f,.28f,.30f,.99f);plate.raycastTarget=false;
            var border=AuthFormVisuals.Rect(name+"Frame",r).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.color=new Color(1,.79f,.51f);border.raycastTarget=false;
        }
        TMP_Text Text(string name,string value,float x,float y,float w,float h,float size)
        {
            var text=Rect(name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;
            text.enableAutoSizing=true;text.fontSizeMin=size*.7f;text.fontSizeMax=size;text.color=new Color(.95f,.95f,.9f);
            text.text=value;text.richText=false;text.raycastTarget=false;text.alignment=TextAlignmentOptions.MidlineLeft;return text;
        }
        void Arrow(Button button,bool back)
        {
            var rect=AuthFormVisuals.Rect(back?"BackArrow":"ChangeArrow",button.transform);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=back?new Vector2(30,28):new Vector2(14,24);
            rect.anchoredPosition=back?Vector2.zero:new Vector2(((RectTransform)button.transform).rect.width/2-24,0);
            if(!back){var caption=button.GetComponentInChildren<TMP_Text>().rectTransform;caption.anchorMin=Vector2.zero;caption.anchorMax=Vector2.one;caption.offsetMin=new Vector2(8,5);caption.offsetMax=new Vector2(-42,-5);}
            void Stroke(string name,Vector2 a,Vector2 b)
            {
                var r=AuthFormVisuals.Rect(name,rect);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=(a+b)/2;r.sizeDelta=new Vector2(Vector2.Distance(a,b),3);r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
                var ink=r.gameObject.AddComponent<Image>();ink.color=new Color(.95f,.95f,.9f);ink.raycastTarget=false;if(!back)changeArrowInk.Add((button,ink));
            }
            if(back){Stroke("Shaft",new Vector2(-15,0),new Vector2(15,0));Stroke("Upper",new Vector2(-13.5f,0),new Vector2(0,12.5f));Stroke("Lower",new Vector2(-13.5f,0),new Vector2(0,-12.5f));}
            else{Stroke("Upper",new Vector2(-5.5f,10.5f),new Vector2(5.5f,0));Stroke("Lower",new Vector2(-5.5f,-10.5f),new Vector2(5.5f,0));}
        }
        Button Control(string name,string label,float x,float y,float w,float h,Action action,bool primary=true)
        {
            if(name=="ConfirmDeparture")
            {
                var main=AuthFormVisuals.MetalButton<DepartureInputButton>(transform,name,label,font,true,()=>{if(isActiveAndEnabled&&start.interactable)action?.Invoke();});
                var mr=(RectTransform)main.transform;mr.anchorMin=mr.anchorMax=mr.pivot=new Vector2(0,1);mr.anchoredPosition=new Vector2(x,-y);mr.sizeDelta=new Vector2(w,h);
                var caption=main.GetComponentInChildren<TMP_Text>();caption.fontSize=52;
                var arrow=Text("DepartureChevron","›",x+w-96,y+18,55,h-36,56);arrow.transform.SetParent(main.transform,true);arrow.alignment=TextAlignmentOptions.Center;
                return main;
            }
            var r=Rect(name,x,y,w,h);var plate=r.gameObject.AddComponent<Image>();plate.color=primary?new Color(.025f,.23f,.35f):new Color(.055f,.08f,.095f);
            plate.sprite=neutralPlate;plate.color=primary?new Color(.15f,.52f,.76f):new Color(.24f,.31f,.34f);
            var border=AuthFormVisuals.Rect("Frame",r).gameObject.AddComponent<Image>();border.sprite=frame;border.type=Image.Type.Sliced;border.fillCenter=false;border.color=new Color(1,.79f,.51f);border.raycastTarget=false;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=plate;
            var colors=button.colors;colors.disabledColor=new Color(.35f,.4f,.43f);button.colors=colors;
            button.onClick.AddListener(()=>{if(isActiveAndEnabled&&button.interactable)action?.Invoke();});
            var text=Text(name+"Label",label,x+8,y+5,w-16,h-10,h>90?52:28);text.transform.SetParent(r,true);text.alignment=TextAlignmentOptions.Center;
            return button;
        }
    }
}
