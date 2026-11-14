using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public enum LobbyPage { Operators, Train, Recruit, Shop, Formation, Missions }

    // Composes page views; migrated pages bind disposable screen controllers.
    public sealed partial class LobbyDestinationPages : MonoBehaviour
    {
        public LobbyPage CurrentPage { get; private set; }
        public bool IsOpen { get; private set; }
        public int SelectedMember { get; private set; }
        public int SelectedSlot { get; private set; }
        public int Category { get; private set; }
        public RectTransform Controls { get; private set; }
        TMP_FontAsset font;
        Action closed;
        readonly ScreenBackKey backKey=new ScreenBackKey();
        Action openPass;Action<string> openBulkSelection;Func<bool> hostActive;bool hostHandlesBack;
        Action releasePageBinding;
        void ReleasePageBinding(){var release=releasePageBinding;releasePageBinding=null;release?.Invoke();}
        public void BindNavigation(Action pass,Action<string> bulk,Func<bool> active){openPass=pass;openBulkSelection=bulk;hostActive=active;hostHandlesBack=active!=null;}
        Action detailBack;
        RectTransform noticePanel;
        AugmentSelection.AugmentModalInputScope detailInput;
        readonly Stack<(Action Back,AugmentSelection.AugmentModalInputScope Input)> parentDetails=new Stack<(Action,AugmentSelection.AugmentModalInputScope)>();
        void ReleaseDetailInput(){noticePanel=null;detailBack=null;detailInput?.Dispose();detailInput=null;while(parentDetails.Count>0)parentDetails.Pop().Input?.Dispose();}
        public void RequestBack()
        {
            if(!IsOpen||!isActiveAndEnabled)return;
            if(detailBack!=null){detailBack();return;}
            if(ultimatePanel){ultimatePanel=false;Rebuild();return;}
            Close();
        }
        bool fixture;
        long viewGeneration,entryGeneration;
        AccountScreenRuntime accountState;Func<bool> boundAccountSession;
        int rosterPage,presetPage;
        public void RefreshAccountView(AccountScreenRuntime state){BindAccount(state);if(IsOpen&&!fixture&&(CurrentPage==LobbyPage.Formation||CurrentPage==LobbyPage.Operators||CurrentPage==LobbyPage.Recruit||CurrentPage==LobbyPage.Train||CurrentPage==LobbyPage.Shop||CurrentPage==LobbyPage.Missions))Rebuild();}
        public void BindAccount(AccountScreenRuntime state){if(accountState!=state||boundAccountSession!=null&&!boundAccountSession()){ReleasePageBinding();renderBody=null;}accountState=state;boundAccountSession=state?.CaptureScreenSession();if(chromeContent!=null&&!fixture){for(int i=0;i<3;i++){var label=chromeContent.Find("CurrencyValue"+i)?.GetComponent<TMP_Text>();if(label!=null)label.text=state?.BalanceText(i)??"—";}
            var notice=chromeContent.Find("AccountStateNotice")?.GetComponent<TMP_Text>();if(notice!=null)notice.text=state?.AccountNotice??"";}}

        RectTransform content;
        Action renderBody;
        LobbyPage renderedPage;int renderedCategory;bool renderedUltimate;
        RectTransform chromeContent;
        LobbyNavigationRail navigation;
        Sprite sharedFrame;
        readonly LandscapeSectionLayout chromeLayout=new LandscapeSectionLayout();
        RawImage backdrop;
        Texture2D art, chrome;
        readonly List<Sprite> skins=new List<Sprite>();
        readonly int[] draft={0,1,2,3,4};
        static readonly string[] Keys={"operators","train","recruit","shop","formation","missions"};
        static readonly string[] Titles={"대원","열차","모집","상점","편성","임무"};
        static readonly string[] Names={"레나","카일","소피아","마르코","하린","이든","벨라","지노","미발견 대원"};
        static readonly string[] Roles={"저격수","방어","전투","전투","지원","전투","저격수","방어",""};

        public static LobbyDestinationPages Create(Transform parent,TMP_FontAsset font,Action closed)
        {
            var root=new GameObject("LobbyDestinationPages",typeof(RectTransform),typeof(Image));root.transform.SetParent(parent,false);
            Stretch((RectTransform)root.transform);root.GetComponent<Image>().color=new Color(.018f,.023f,.029f);
            var page=root.AddComponent<LobbyDestinationPages>();page.font=font;page.closed=closed;
            page.backdrop=page.Rect("Environment",root.transform,0,0,941,1672).gameObject.AddComponent<RawImage>();page.backdrop.raycastTarget=false;
            page.Controls=page.Rect("PageControls",root.transform,0,0,1600,900);page.Controls.anchorMin=page.Controls.anchorMax=page.Controls.pivot=new Vector2(.5f,.5f);page.Controls.anchoredPosition=Vector2.zero;
            page.chrome=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/icons-landscape-v01");root.SetActive(false);return page;
        }
        public void Open(LobbyPage page,bool editorFixture=false){_ = OpenPreparedPageAsync(page,editorFixture);}
        void OpenPreparedPage(LobbyPage page,bool editorFixture)
        {
            entryGeneration++;fixture=false;
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
            fixture=editorFixture;
#endif
            viewGeneration++;ultimatePanel=false;CurrentPage=page;Category=0;SelectedMember=0;SelectedSlot=0;IsOpen=true;
            gameObject.SetActive(true);transform.SetAsLastSibling();Rebuild();
            if(page==LobbyPage.Train&&!fixture&&accountState!=null)LoadTrain();
            
            if(page==LobbyPage.Shop&&!fixture&&accountState!=null)RefreshShop();if(page==LobbyPage.Missions&&!fixture&&accountState!=null)RefreshMissions();
        }


        public void Close(){entryGeneration++;bool opening=pageResourceRequest!=null;ReleasePageResources();if(!IsOpen&&!opening)return;IsOpen=false;gameObject.SetActive(false);closed?.Invoke();}
        void OnGUI(){if(IsOpen&&!hostHandlesBack)backKey.Handle(Event.current,RequestBack,ScreenBackKey.Editing);}
        void OnEnable(){backKey.Synchronize();}
        void OnApplicationFocus(bool focused){if(focused)backKey.Synchronize();}
        void OnRectTransformDimensionsChange(){Fit();}
        void OnDisable(){entryGeneration++;ReleasePageResources();viewGeneration++;ReleaseDetailInput();ReleaseEquipmentMultishot();IsOpen=false;}
        void OnDestroy(){ReleasePageResources();viewGeneration++;IsOpen=false;ReleaseEquipmentMultishot();ReleaseSkins();if(sharedFrame!=null){if(Application.isPlaying)Destroy(sharedFrame);else DestroyImmediate(sharedFrame);}ReleasePageBinding();boundAccountSession=null;accountState=null;closed=null;openPass=null;openBulkSelection=null;hostActive=null;content=null;Controls=null;backdrop=null;font=null;art=null;chrome=null;}
        void ReleaseSkins(){foreach(var skin in skins)if(skin!=null){if(Application.isPlaying)Destroy(skin);else DestroyImmediate(skin);}skins.Clear();}
        void Clear()
        {
            ReleasePageBinding();renderBody=null;
            ReleaseDetailInput();
            viewGeneration++;
            if(content!=null){content.gameObject.SetActive(false);if(Application.isPlaying)Destroy(content.gameObject);else DestroyImmediate(content.gameObject);}
            ReleaseSkins();pageLandscape=new LandscapeSectionLayout();overlayLayouts.Clear();content=Rect("PageContent",Controls,0,0,1600,900);
        }
        void Rebuild()
        {
            if(!fixture&&renderBody!=null&&renderedPage==CurrentPage&&renderedCategory==Category&&renderedUltimate==ultimatePanel){renderBody();BindAccount(accountState);return;}
            ReleaseEquipmentMultishot();Clear();bool plan=CurrentPage==LobbyPage.Formation&&!fixture;
            art=plan||CurrentPage==LobbyPage.Train?null:LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/"+Keys[(int)CurrentPage]+"-atlas");
            backdrop.texture=CurrentPage==LobbyPage.Train?LastRail.Resources.PreparedContent.Load<Texture2D>("TrainFortress/depot-background"):plan?LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/cabin-landscape-v01"):LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/"+Keys[(int)CurrentPage]+"-background");
            backdrop.enabled=true;backdrop.color=plan?new Color(.48f,.43f,.36f,1):Color.white;
            Header();
            switch(CurrentPage){case LobbyPage.Operators:Operators();break;case LobbyPage.Train:Train();break;case LobbyPage.Recruit:Recruit();break;case LobbyPage.Shop:Shop();break;case LobbyPage.Formation:Formation();break;case LobbyPage.Missions:Missions();break;}
            renderedPage=CurrentPage;renderedCategory=Category;renderedUltimate=ultimatePanel;Navigation();Fit();BindAccount(accountState);
        }
        void Header()
        {
            if(chromeContent!=null){chromeContent.Find("PageTitle").GetComponent<TMP_Text>().text=Titles[(int)CurrentPage];return;}
            chromeContent=Rect("PageChrome",Controls,0,0,1600,900);
            var body=content;content=chromeContent;
            Panel("HeaderShade",content,0,0,941,180,new Color(.02f,.025f,.03f,.94f));
            Button("Back",content,20,20,91,70,"‹",RequestBack);
            Text("PageTitle",content,Titles[(int)CurrentPage],137,15,500,75,49);
            for(int i=0;i<3;i++)
            {
                float x=32+i*304;Panel("Currency"+i,content,x,112,285,51);
                Crop("CurrencyIcon",content,chrome,x+8,116,40,42,0,0,0,0).uvRect=LobbyArtwork.IconUv(i+1);
                Text("CurrencyValue"+i,content,fixture?new[]{"80 / 100","24,800","1,250"}[i]:"—",x+59,114,205,45,26,true);
            }
            Text("AccountStateNotice",content,"",34,169,873,42,21);
            content=body;
        }
        void Navigation()
        {
            int active=CurrentPage==LobbyPage.Formation?1:CurrentPage==LobbyPage.Missions?-1:(int)CurrentPage+1;
            if(navigation==null){if(sharedFrame==null)sharedFrame=LobbyArtwork.CreateFrame();navigation=LobbyNavigationRail.Create(chromeContent,sharedFrame,chrome,active,"PageNav",n=>{if(!IsOpen)return;if(n==0)Close();else Open((LobbyPage)(n-1),fixture);}).GetComponent<LobbyNavigationRail>();}
            navigation.SetSelection(active);chromeContent.SetAsLastSibling();
        }
        void Tabs(string[] titles,float y)
        {
            float width=873f/titles.Length;
            for(int i=0;i<titles.Length;i++){int c=i;var button=Button("Category"+i,content,34+i*width,y,width-5,65,titles[i],()=>{Category=c;Rebuild();},Category==i);LobbyTabVisual.ApplyText(button,button.GetComponentInChildren<TMP_Text>(),Category==i);}
        }
        void Unavailable(float y,string text){Panel("EmptyState",content,34,y,873,115);Text("EmptyStateText",content,text,62,y+24,817,70,29,true);}
        void Operators()
        {
            if(!fixture){LiveOperators();return;}
            if(fixture){if(SelectedMember==0)Crop("HeroPortrait",content,art,20,180,450,250,0,160,480,258);else Portrait(content,SelectedMember,120,182,310,248);Text("SelectedName",content,Names[SelectedMember],492,225,400,60,46);Text("SelectedRole",content,Roles[SelectedMember]+"  ·  Lv.12",494,297,390,45,29);}
            Tabs(new[]{"전체","전투","지원"},438);
            if(!fixture){Unavailable(575,"대원 정보를 불러오지 못했습니다.");return;}
            int visibleIndex=0;for(int i=0;i<9;i++)
            {
                if(Category==1 && i==4 || Category==2 && i!=4)continue;
                int index=i;float x=34+visibleIndex%3*296,y=525+visibleIndex/3*237;visibleIndex++;
                Panel("MemberCard"+i,content,x,y,280,224);Portrait(content,i,x+7,y+7,266,166);
                Text("MemberName",content,Names[i],x+8,y+177,264,39,27,true);
                var b=Button("SelectMember"+i,content,x,y,280,224,null,()=>{SelectedMember=index;Rebuild();},false,true);b.interactable=i<8;if(i==SelectedMember){Panel("SelectedUnderline",content,x+8,y+218,264,4,new Color(.15f,.8f,1));}
            }
            Panel("SelectedMemberDetails",content,34,1250,873,238);Text("MemberDetail",content,Names[SelectedMember]+" · "+Roles[SelectedMember]+"\nLv.12",66,1273,450,160,33);
            Button("OpenMemberDetails",content,533,1282,337,70,"상세 보기",()=>Notice(Names[SelectedMember],"대원 상세 정보를 준비하고 있습니다."),true);
            Button("OpenFormation",content,533,1374,337,70,"편성",()=>Open(LobbyPage.Formation,fixture));
        }
        void LiveOperators()
        {
            OperatorScreenController controller=null;int pageCount=1;var titles=new[]{"레벨업","스킬 훈련","승급"};
            var portrait=Rect("SelectedPortrait",content,34,218,280,203).gameObject.AddComponent<RawImage>();portrait.raycastTarget=false;
            var selectedName=Text("SelectedName",content,"",343,228,563,57,42);
            var growth=Text("OwnedGrowthState",content,"",343,301,563,103,29);
            Tabs(new[]{"전체","전투","지원"},438);content.Find("Category1").GetComponent<Button>().interactable=false;content.Find("Category2").GetComponent<Button>().interactable=false;
            var cards=new Button[6];var faces=new RawImage[6];var names=new TMP_Text[6];var indices=new int[6];
            for(int i=0;i<6;i++){int cell=i;float x=34+i%3*296,y=525+i/3*237;
                cards[i]=Button("SelectOwnedOperator"+i,content,x,y,280,220,null,()=>{if(indices[cell]<0)return;controller.Select(indices[cell]);});
                faces[i]=Rect("Portrait",cards[i].transform,8,8,264,161).gameObject.AddComponent<RawImage>();faces[i].raycastTarget=false;
                names[i]=Text("OwnedName",cards[i].transform,"",8,177,264,35,28,true);
            }
            var next=Button("OwnedPage",content,688,1003,219,60,"",()=>{rosterPage=(rosterPage+1)%pageCount;Rebuild();});
            Button("OpenFormation",content,34,1003,425,60,"편성",()=>Open(LobbyPage.Formation));
            var coreButton=Button("ManageTrainingCore",content,475,1003,197,60,"훈련 코어",()=>controller.OpenCore());
            var notice=Text("GrowthNotice",content,"",34,1085,873,95,25,true);
            var costs=new TMP_Text[3];var actions=new Button[3];
            for(int i=0;i<3;i++){int action=i;float x=34+i*296;Panel("GrowthOption"+i,content,x,1200,280,290);
                costs[i]=Text("GrowthCost"+i,content,"",x+12,1220,256,122,24,true);
                actions[i]=Button("GrowOperator"+i,content,x+10,1370,260,91,titles[i],async()=>await controller.GrowAsync(action),true);
            }
            controller=new OperatorScreenController(accountState,value=>{
                SelectedMember=value.Selected;var selectedId=value.Id;int count=value.Operators.Count;
                var face=selectedId==null?null:FormationPlanView.Portrait(selectedId);portrait.texture=face;portrait.enabled=face!=null;portrait.uvRect=FormationPlanView.PortraitCrop(selectedId,face);
                selectedName.text=selectedId==null?"":OperatorVisualCatalog.KoreanLabel(selectedId);growth.text=value.Growth;
                pageCount=Math.Max(1,(count+5)/6);rosterPage%=pageCount;next.GetComponentInChildren<TMP_Text>().text=(rosterPage+1)+" / "+pageCount;
                for(int i=0;i<6;i++){int index=rosterPage*6+i;bool exists=index<count;indices[i]=exists?index:-1;cards[i].gameObject.SetActive(exists);
                    var id=exists?value.Operators[index]:null;var image=id==null?null:FormationPlanView.Portrait(id);faces[i].texture=image;faces[i].uvRect=FormationPlanView.PortraitCrop(id,image);
                    names[i].text=id==null?"":OperatorVisualCatalog.KoreanLabel(id);cards[i].GetComponent<Image>().color=index==SelectedMember?new Color(.02f,.22f,.37f,.98f):new Color(.025f,.033f,.04f,.97f);
                }
                coreButton.interactable=selectedId!=null;notice.text=value.Notice;
                for(int i=0;i<3;i++){costs[i].text=value.Actions[i].Cost;actions[i].GetComponentInChildren<TMP_Text>().text=value.Actions[i].Title;actions[i].interactable=value.Actions[i].Enabled;}
            },value=>CoreManager(value,controller.ReplaceCore,controller.SyncCore),CoreConfirm);
            releasePageBinding+=controller.Dispose;renderBody=controller.Render;renderBody();
        }
        RectTransform CoreShield(string name)
        {
            ReleaseDetailInput();

            var shield=Rect(name,content,0,0,941,1515);var shade=shield.gameObject.AddComponent<Image>();shade.color=new Color(.008f,.014f,.021f,.97f);
            detailBack=()=>CloseCoreShield(shield);detailInput=new AugmentSelection.AugmentModalInputScope(shield,transform,null);return shield;
        }
        void CloseCoreShield(RectTransform shield){if(shield==null||!shield.gameObject.activeInHierarchy)return;shield.gameObject.SetActive(false);ReleaseDetailInput();if(Application.isPlaying)Destroy(shield.gameObject);else DestroyImmediate(shield.gameObject);Rebuild();}
        void CoreManager(TrainingCoreDisplay data,Action<int> replace,Action syncAction)
        {
            var shield=CoreShield("TrainingCoreManager");
            Text("CoreTitle",shield,"훈련 코어",40,185,861,65,42,true);
            Text("CoreCandidate",shield,"선택 대원: "+OperatorVisualCatalog.Label(data.Candidate)+"\n빈 슬롯에 배치하거나 기존 대원을 교체합니다.",45,268,851,110,29,true);
            for(int i=0;i<data.Slots.Count;i++){
                int slot=i;var core=data.Slots[i];float y=415+i*139;
                Panel("CoreSlot"+i,shield,40,y,861,123);
                Text("CoreState"+i,shield,(i+1)+"  "+(core.Id==null?"빈 슬롯":OperatorVisualCatalog.Label(core.Id)+" · Lv."+core.Level)+"\n기록된 투자 EXP "+core.Invested.ToString("N0"),64,y+12,538,100,27);
                var button=Button("ReplaceCore"+i,shield,638,y+19,238,85,core.Id==null?"배치":"교체",()=>{CloseCoreShield(shield);replace(slot);},true);button.interactable=core.CanReplace;
            }
            Text("CoreSyncExplanation",shield,"동기 성장: 낮은 레벨부터 보유 EXP로 성장\n5개 슬롯 필요 · 배치된 대원은 중복 배치 불가",48,1124,845,98,27,true);
            var sync=Button("SyncTrainingCore",shield,48,1247,845,90,"동기 성장",()=>{CloseCoreShield(shield);syncAction();},true);sync.interactable=data.CanSync;
            Button("CloseTrainingCore",shield,48,1370,845,90,"닫기",()=>CloseCoreShield(shield));
        }
        void CoreConfirm(string title,string message,Func<bool> allowed,Func<System.Threading.Tasks.Task> submit)
        {
            var shield=CoreShield("CoreConfirmation");Text("Title",shield,title,65,470,811,75,40,true);
            Text("Message",shield,message,65,596,811,240,29,true);
            var confirm=Button("ConfirmCoreAction",shield,65,925,811,100,"실행",async()=>{if(shield==null||!shield.gameObject.activeInHierarchy)return;CloseCoreShield(shield);var owner=accountState;long generation=viewGeneration;if(allowed())await submit();if(this!=null&&IsOpen&&ReferenceEquals(owner,accountState)&&generation==viewGeneration)Rebuild();},true);confirm.interactable=allowed();
            Button("CancelCoreAction",shield,65,1056,811,90,"취소",()=>CloseCoreShield(shield));
        }
        void Train(){if(!fixture)LiveTrain();else BuildTrainOverview();}
        void Recruit()
        {
            if(!fixture){LiveRecruitment();return;}
            Tabs(new[]{"일반 모집","특별 모집"},202);
            Text("BannerTitle",content,Category==0?"황무지의 신호":"새로운 신호",80,962,781,90,62,true);
            Text("BannerSubtitle",content,"새로운 동료를 만나세요",80,1061,781,55,31,true);
            Guarded("RecruitOne",content,36,1200,423,125,"1회 모집");Guarded("RecruitTen",content,482,1200,423,125,"10회 모집");
            Button("RecruitHistory",content,36,1365,423,76,"모집 기록",()=>Notice("모집 기록","모집 기록을 불러오지 못했습니다."));
            Button("RecruitRates",content,482,1365,423,76,"확률 정보",()=>Notice("확률 정보","모집 정보를 불러오지 못했습니다."));
        }
        void Shop()
        {
            Tabs(new[]{"추천","편의 상품","재화"},438);
            if(!fixture){LiveShop();return;}
            string[] names={"보급 상자","연료","모집권","수리 키트","크레딧","크리스탈"};
            var visible=new List<int>();for(int i=0;i<6;i++)if(Category==0 || Category==1 && i<4 || Category==2 && i>=4)visible.Add(i);
            for(int j=0;j<visible.Count;j++){int i=visible[j];float x=34+j%2*449,y=530+j/2*326;float sx=34+i%2*449,sy=530+i/2*326;
                Panel("Product"+i,content,x,y,424,312);Text("ProductName",content,names[i],x+22,y+8,380,42,30);
                Crop("ProductArt",content,art,x+10,y+52,404,153,sx+10,sy+52,404,153);
                Text("ProductPrice",content,"가격 확인 필요",x+20,y+211,384,38,24,true);Guarded("Buy"+i,content,x+50,y+255,324,48,"구매");}
        }
        void Formation()
        {
            if(!fixture){LiveFormation();return;}
            Text("FormationHelp",content,"대원을 배치하세요",36,188,830,50,32);
            Vector2[] slots={new Vector2(162,352),new Vector2(309,527),new Vector2(613,468),new Vector2(537,737),new Vector2(790,694)};
            if(fixture)for(int i=0;i<slots.Length;i++){int slot=i;var p=slots[i];Button("FormationSlot"+i,content,p.x-42,p.y-28,84,56,(i+1).ToString(),()=>{SelectedSlot=slot;Rebuild();},SelectedSlot==i);}
            Panel("SlotDetails",content,29,877,883,94);Text("SelectedSlotInfo",content,fixture?"선택 위치 "+(SelectedSlot+1)+"  ·  "+Names[draft[SelectedSlot]]:"편성 정보를 불러오지 못했습니다.",60,897,811,52,30);
            Panel("FormationPower",content,204,992,532,58);Text("Power",content,fixture?"편성 전투력  12,480":"편성 전투력  —",211,997,518,46,28,true);
            Panel("Roster",content,29,1070,883,261);Text("RosterTitle",content,"대원 목록",56,1080,700,46,31);
            if(fixture)for(int i=0;i<6;i++){int member=i;float x=40+i*145;Portrait(content,i,x,1140,132,156);Button("AssignMember"+i,content,x,1140,132,156,null,()=>{int old=Array.IndexOf(draft,member);if(old>=0){int swap=draft[SelectedSlot];draft[old]=swap;}draft[SelectedSlot]=member;Rebuild();},false,true);}
            var auto=Button("AutoFormation",content,34,1370,373,110,"자동 편성",()=>{for(int i=0;i<5;i++)draft[i]=i;Rebuild();});auto.interactable=fixture;
            Guarded("SaveFormation",content,442,1370,465,110,"편성 저장");
        }


        void LiveFormation()
        {
            var state=accountState;
            if(state?.Account==null){Unavailable(575,state?.AccountNotice??"편성 정보를 불러오지 못했습니다.");return;}
            FormationScreenState display=null;FormationScreenController controller=null;
            Panel("Roster",content,145,120,300,748);Text("RosterTitle",content,"대원 목록",160,132,175,42,28);
            int pageCount=1;var next=Button("RosterNext",content,340,134,90,40,"",()=>{rosterPage=(rosterPage+1)%pageCount;Rebuild();});
            var rosterIds=new string[6];var rosterButtons=new Button[6];var rosterImages=new RawImage[6];var rosterNames=new TMP_Text[6];var rosterChecks=new GameObject[6];
            for(int i=0;i<6;i++){
                int index=i;var button=Button("AssignOwned"+i,content,157+i%2*140,195+i/2*200,132,180,null,()=>{
                    controller.SelectOperator(rosterIds[index]);
                });rosterButtons[i]=button;
                var image=Rect("OwnedPortrait",button.transform,6,6,120,144).gameObject.AddComponent<RawImage>();image.raycastTarget=false;rosterImages[i]=image;
                rosterNames[i]=Text("OwnedName",button.transform,"",3,150,126,28,22,true);
                var check=Rect("Assigned",button.transform,0,0,132,180);FormationPlanView.SelectionBorder(check,132,180);Text("AssignedCheck",check,"✓",102,7,26,26,24,true);rosterChecks[i]=check.gameObject;
            }
            var numbered=FormationPlanView.Create(content,font);((RectTransform)numbered.transform).anchoredPosition=new Vector2(460,-120);
            numbered.Bind(slot=>controller.SelectPosition(slot));
            Panel("SlotDetails",content,460,745,750,122);
            var slotInfo=Text("SelectedSlotInfo",content,"",478,750,710,34,25);
            var inputRect=Panel("PresetName",content,478,798,430,48);var name=Text("NameValue",inputRect,"",12,5,406,38,25);
            var input=inputRect.gameObject.AddComponent<TMP_InputField>();input.textViewport=inputRect;input.textComponent=(TextMeshProUGUI)name;input.characterLimit=80;
            var activate=Button("ActivateFormation",content,925,795,265,56,"활성화",async()=>await controller.ActivateAsync(),true);
            input.onEndEdit.AddListener(value=>controller.Rename(value));
            var notice=Text("FormationNotice",content,"",460,870,750,28,19,true);
            Panel("SavedPresets",content,1225,120,350,748);Text("SavedTitle",content,"저장된 편성",1243,132,310,44,28);
            var viewport=Rect("SavedViewport",content,1237,190,326,590);viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();
            var items=Rect("SavedContent",viewport,0,0,326,590);scroll.viewport=viewport;scroll.content=items;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,.01f);
            var savedButtons=new List<Button>();var savedLabels=new List<TMP_Text>();var savedImages=new List<RawImage[]>();
            var noSaved=Text("NoSavedPresets",items,"저장된 편성이 없습니다",5,15,316,50,22,true);
            var fresh=Button("NewPreset",content,1243,803,314,49,"+ 새 편성",()=>controller.NewPreset());
            controller=new FormationScreenController(state,value=>{
                display=value;SelectedSlot=value.Slot;bool editable=value.Editable;
                var selection=value.Selection;pageCount=Math.Max(1,(value.Roster.Count+5)/6);rosterPage%=pageCount;
                next.GetComponentInChildren<TMP_Text>().text=(rosterPage+1)+" / "+pageCount;
                var rows=value.Roster.Skip(rosterPage*6).Take(6).ToArray();
                for(int i=0;i<6;i++){
                    var id=i<rows.Length?rows[i]:null;rosterIds[i]=id;rosterButtons[i].gameObject.SetActive(id!=null);
                    rosterImages[i].texture=FormationPlanView.Portrait(id);rosterImages[i].uvRect=FormationPlanView.PortraitCrop(id,rosterImages[i].texture as Texture2D);
                    rosterNames[i].text=id==null?"":OperatorVisualCatalog.KoreanLabel(id);rosterChecks[i].SetActive(id!=null&&selection.Contains(id));
                    rosterButtons[i].interactable=editable;rosterButtons[i].GetComponent<Image>().color=value.SelectedOperator==id&&id!=null?new Color(.02f,.22f,.37f,.98f):new Color(.025f,.033f,.04f,.97f);
                }
                numbered.Render(selection.ToArray(),SelectedSlot,editable);slotInfo.text="선택 위치 "+(SelectedSlot+1)+" · "+OperatorVisualCatalog.KoreanLabel(selection[SelectedSlot]);
                if(!input.isFocused)input.SetTextWithoutNotify(value.Name);input.interactable=editable;
                bool retry=value.Retry;
                activate.GetComponentInChildren<TMP_Text>().text=retry?"결과 재확인":"활성화";activate.interactable=value.CanActivate;
                notice.text=value.Notice;fresh.interactable=editable;
                int count=value.Presets.Count;
                while(savedButtons.Count>count){int last=savedButtons.Count-1;var retired=savedButtons[last];savedButtons.RemoveAt(last);savedLabels.RemoveAt(last);savedImages.RemoveAt(last);retired.gameObject.SetActive(false);if(Application.isPlaying)Destroy(retired.gameObject);else DestroyImmediate(retired.gameObject);}
                for(int i=0;i<count;i++){
                    if(i==savedButtons.Count){
                        int index=i;var button=Button("SavedPreset"+i,items,3,i*142+3,318,132,null,()=>{
                            if(display!=null&&index<display.Presets.Count)controller.LoadPreset(display.Presets[index].Id);
                        });
                        savedButtons.Add(button);savedLabels.Add(Text("PresetTitle",button.transform,"",10,8,298,34,23,true));var portraits=new RawImage[5];
                        for(int slot=0;slot<5;slot++){portraits[slot]=Rect("SavedPortrait"+slot,button.transform,9+slot*60,52,58,65).gameObject.AddComponent<RawImage>();portraits[slot].raycastTarget=false;}
                        savedImages.Add(portraits);
                    }
                    var row=value.Presets[i];bool active=row.Active;var control=savedButtons[i];control.interactable=editable;
                    control.GetComponent<Image>().color=active?new Color(.02f,.22f,.37f,.98f):new Color(.025f,.033f,.04f,.97f);
                    savedLabels[i].text=row.Valid?row.Name+(active?"  · 활성":""):"편성 정보 확인 필요";
                    var ids=row.Operators;control.interactable=editable&&row.Valid;
                    for(int slot=0;slot<5;slot++){var image=savedImages[i][slot];var texture=FormationPlanView.Portrait(ids[slot]);image.texture=texture;image.uvRect=FormationPlanView.PortraitCrop(ids[slot],texture);}
                }
                items.sizeDelta=new Vector2(326,Math.Max(590,count*142));noSaved.gameObject.SetActive(count==0);
            });
            releasePageBinding+=controller.Dispose;renderBody=controller.Render;renderBody();
        }
        void Missions()
        {
            if(!fixture){LiveMissions();return;}
            Tabs(new[]{"일일","주간","업적"},413);
            if(!fixture){Unavailable(527,"임무 정보를 불러오지 못했습니다.");return;}
            Panel("MissionProgress",content,32,496,878,180);Text("MissionSummary",content,new[]{"오늘의 임무","이번 주의 임무","업적"}[Category],57,514,800,55,37);
            Text("Progress",content,"달성 현황  2 / 3",58,584,800,55,29);Bar(content,60,645,803,12,.67f);
            string[] daily={"전투 3회 완료","대원 강화 1회","보급품 수령","열차 강화 1회"};
            string[] weekly={"전투 20회 완료","대원 강화 5회","보급품 5회 수령","열차 강화 3회"};
            string[] feats={"첫 출발","동료들과 함께","보급 담당관","열차 정비사"};
            for(int i=0;i<4;i++){float y=700+i*158;Panel("MissionRow"+i,content,34,y,873,144);
                Text("MissionLabel",content,(Category==0?daily:Category==1?weekly:feats)[i],63,y+18,552,53,31);
                Text("MissionStatus",content,i==1||i==3?"완료":"진행 중",63,y+82,530,38,24);
                Guarded("ClaimMission"+i,content,713,y+35,168,72,i==3?"수령 완료":i==1?"받기":"이동");}
            Guarded("ClaimAll",content,225,1375,491,100,"모두 받기");
        }
        void Portrait(Transform parent,int index,float x,float y,float w,float h)
        {
            Texture2D roster=LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/operators-atlas");
            float sx=34+index%3*296,sy=525+index/3*237;
            float sw=266,sh=166;if(w/h<sw/sh){float cropped=sh*w/h;sx+=(sw-cropped)*.5f;sw=cropped;}else{float cropped=sw*h/w;sy+=(sh-cropped)*.5f;sh=cropped;}Crop("Portrait"+index,parent,roster,x,y,w,h,sx+7,sy+7,sw,sh);
        }
        void Notice(string title,string message)
        {
            if(noticePanel!=null&&noticePanel.gameObject.activeSelf)detailBack?.Invoke();
            parentDetails.Push((detailBack,detailInput));
            var shield=Panel("Notice",content,0,0,941,1672,new Color(0,0,0,.8f));shield.GetComponent<Image>().raycastTarget=true;
            noticePanel=shield;
            void Dismiss(){if(shield==null||!shield.gameObject.activeInHierarchy)return;noticePanel=null;detailInput?.Dispose();var parent=parentDetails.Pop();detailBack=parent.Back;detailInput=parent.Input;shield.gameObject.SetActive(false);if(Application.isPlaying)Destroy(shield.gameObject);else DestroyImmediate(shield.gameObject);}
            var dialog=Panel("Dialog",shield,80,490,781,515);Text("Title",shield,title,120,525,701,85,40,true);Text("Message",shield,message,120,645,701,145,30,true);var close=Button("Dismiss",shield,180,855,581,90,"닫기",Dismiss,true);
            var approved=LastRail.Resources.PreparedContent.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");
            if(approved!=null){
                var old=dialog.Find("Frame");if(old!=null)old.gameObject.SetActive(false);
                var interior=Rect("ApprovedDialogInterior",dialog,10,10,761,495).gameObject.AddComponent<RawImage>();interior.texture=approved.PanelTexture;interior.color=new Color(.12f,.15f,.18f,1);interior.raycastTarget=false;
                ApprovedUiConnections.Frame(dialog,"ApprovedDialogFrame",approved.DialogFrame,160f/28);
                var medal=Rect("ApprovedInfoMedal",dialog,352,-55,76,76).gameObject.AddComponent<RawImage>();medal.texture=approved.Medal;medal.raycastTarget=false;
                Text("InfoLetter",medal.transform,"i",14,9,48,58,48,true);
                var oldButton=close.transform.Find("Frame");if(oldButton!=null)oldButton.gameObject.SetActive(false);
                var buttonFace=close.GetComponent<Image>();buttonFace.sprite=null;buttonFace.color=new Color(.02f,.05f,.08f,1);
                var plate=Rect("ApprovedButtonInterior",close.transform,7,7,567,76).gameObject.AddComponent<RawImage>();plate.texture=approved.ButtonPlate;plate.raycastTarget=false;plate.transform.SetAsFirstSibling();
                ApprovedUiConnections.Frame(close.transform,"ApprovedButtonFrame",approved.ButtonFrame,14);
                close.transform.Find("Label")?.SetAsLastSibling();
            }
            detailBack=Dismiss;detailInput=new AugmentSelection.AugmentModalInputScope(shield,transform,close.gameObject);
        }
        void Fit()
        {
            if(Controls==null)return;var area=((RectTransform)transform).rect;float fit=Mathf.Min(area.width/1600f,area.height/900f);Controls.localScale=Vector3.one*fit;ReflowPage();
            var br=backdrop.rectTransform;br.anchorMin=br.anchorMax=br.pivot=new Vector2(.5f,.5f);br.anchoredPosition=Vector2.zero;var native=backdrop.texture==null?new Vector2(941,1672):new Vector2(backdrop.texture.width,backdrop.texture.height);br.sizeDelta=native*Mathf.Max(area.width/native.x,area.height/native.y);
        }
        LandscapeSectionLayout pageLandscape=new LandscapeSectionLayout();
        readonly Dictionary<RectTransform,LandscapeSectionLayout> overlayLayouts=new Dictionary<RectTransform,LandscapeSectionLayout>();
        void ReflowPage()
        {
            if(chromeContent!=null)chromeLayout.Apply(chromeContent,(name,r)=>{
                if(name==LobbyNavigationRail.ObjectName)return r;
                if(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel){if(name=="HeaderShade")return new Rect(0,0,1600,78);if(name=="Back")return new Rect(20,10,80,60);if(name=="PageTitle")return new Rect(130,5,340,65);if(name.StartsWith("CurrencyValue")&&int.TryParse(name.Substring(13),out var cv))return new Rect(847+cv*250,17,185,40);if(name.StartsWith("Currency")&&int.TryParse(name.Substring(8),out var ci))return new Rect(800+ci*250,15,240,45);if(name=="CurrencyIcon")return new Rect(809+(r.x-40)/304*250,19,34,34);}
                if(name=="HeaderShade")return new Rect(0,0,1600,CurrentPage==LobbyPage.Train&&Category==0?100:145);
                if(name=="Back")return new Rect(20,25,91,70);
                if(name=="PageTitle")return new Rect(137,20,390,75);
                if(name.StartsWith("CurrencyValue")&&int.TryParse(name.Substring(13),out var value))return new Rect(594+value*305,45,205,45);
                if(name.StartsWith("Currency")&&int.TryParse(name.Substring(8),out var currency))return new Rect(535+currency*305,43,285,51);
                if(name=="CurrencyIcon")return new Rect(543+(r.x-40)/304*305,47,40,42);
                if(name=="AccountStateNotice")return new Rect(535,100,900,42);
                if(name.StartsWith("PageNav")&&int.TryParse(name.Substring(7),out var nav))return CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel?new Rect(10,130+nav*130,140,105):new Rect(180+nav*250,778,181,122);
                return r;
            }, !(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel));
            if(content==null)return;
            float split=1060;
            if(CurrentPage==LobbyPage.Formation)split=ultimatePanel?985:877;
            if(CurrentPage==LobbyPage.Missions)split=fixture?1016:926;
            if(CurrentPage==LobbyPage.Train)split=Category==1?630:Category==0?947:1000;
            if(CurrentPage==LobbyPage.Shop)split=Category==2?920:529;
            pageLandscape.Apply(content,(name,r)=>
            {
                if(name=="TrainingCoreManager"||name=="CoreConfirmation"||name=="Notice"||name=="EquipmentMultishotDetails")return new Rect(0,0,1600,775);
                if(name=="TrainOverview")return r;
                if(TryArrangeDestinationSection(name,r,out var section))return section;
                var result=r.y<split?LandscapeSectionLayout.Section(r,210,40,165,.7f):LandscapeSectionLayout.Section(r,split,850,165,.7f);
                if(r.y<split&&r.yMax>split&&r.height>400)result.height=(split-r.y)*.7f;
                return result;
            }, !(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel));
            if(!(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel))
            {
                foreach(RectTransform section in content)
                {
                    if(section.name=="TrainOverview" || section.anchorMin!=section.anchorMax || section.name==LobbyNavigationRail.ObjectName || -section.anchoredPosition.y<145 ||
                       section.name=="TrainingCoreManager" || section.name=="CoreConfirmation" || section.name=="Notice" || section.name=="EquipmentMultishotDetails")continue;
                    section.anchoredPosition=new Vector2(140+section.anchoredPosition.x*.9f,-(145+(-section.anchoredPosition.y-145)*.9f));
                    section.localScale*=.9f;
                }
            }
            foreach(RectTransform overlay in content)
            {
                if(overlay.name!="TrainingCoreManager"&&overlay.name!="CoreConfirmation"&&overlay.name!="Notice"&&overlay.name!="EquipmentMultishotDetails")continue;
                overlay.localScale=Vector3.one;overlay.sizeDelta=new Vector2(1600,775);
                if(!overlayLayouts.TryGetValue(overlay,out var arrange)){arrange=new LandscapeSectionLayout();overlayLayouts.Add(overlay,arrange);}
                if(overlay.name=="Notice"){
                    overlay.sizeDelta=new Vector2(1600,900);
                    arrange.Apply(overlay,(name,r)=>new Rect(r.x+329.5f,r.y-297.5f,r.width,r.height));
                    continue;
                }
                if(overlay.name=="EquipmentMultishotDetails")
                {
                    arrange.Apply(overlay,(name,r)=>
                    {
                        if(name=="CloseEquipmentMultishot")return new Rect(820,675,740,75);
                        if(name=="EquipmentEffectTitle")return new Rect(40,40,1520,60);
                        if(name=="PlacementPreviewNotice"||name=="PreviewStatus")return new Rect(40,105,1520,35);
                        if(name.StartsWith("departure")||name.StartsWith("previewAfter"))return LandscapeSectionLayout.Section(r,790,820,150,.8f);
                        if(name.StartsWith("current")||name.StartsWith("previewBefore"))return LandscapeSectionLayout.Section(r,305,40,150,.8f);
                        if(name.StartsWith("Stored"))return LandscapeSectionLayout.Section(r,1275,40,560,.8f);
                        if(name.StartsWith("PreviewBefore"))return LandscapeSectionLayout.Section(r,1258,40,560,.8f);
                        if(name.StartsWith("PreviewAfter"))return new Rect(820+(r.x-500)*.8f,560+(r.y-1258)*.8f,r.width*.8f,r.height*.8f);
                        return LandscapeSectionLayout.Section(r,305,40,150,.8f);
                    });
                    continue;
                }
                arrange.Apply(overlay,(name,r)=>r.y<832?LandscapeSectionLayout.Section(r,185,40,60,.8f):LandscapeSectionLayout.Section(r,832,810,60,.8f));
            }
        }
        // Rows are indivisible presentation groups: their plate, art, label and hit area
        // share the same source origin. A label's own Y must never choose another column.
        bool TryArrangeDestinationSection(string name,Rect r,out Rect target)
        {
            target=r;
            if(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel)return true;
            if(CurrentPage==LobbyPage.Operators)
            {
                bool card=fixture?(name.StartsWith("MemberCard")||name.StartsWith("Portrait")||name=="MemberName"||name.StartsWith("SelectMember")||name=="SelectedUnderline")
                    :name.StartsWith("SelectOwnedOperator");
                if(card)
                {
                    int row=Mathf.Max(0,Mathf.FloorToInt((r.y-525)/237));
                    int column=Mathf.Max(0,Mathf.FloorToInt((r.x-34)/296));
                    target=ArrangeRowPart(r,new Vector2(34+column*296,525+row*237),new Vector2(825+column*230,fixture?190+row*180:230+row*180),.75f);return true;
                }
                if(name.StartsWith("Category")) {target=LandscapeSectionLayout.Section(r,438,fixture?40:810,fixture?420:160,.75f);return true;}
                if(fixture&&r.y>=1250){target=LandscapeSectionLayout.Section(r,1250,40,510,.75f);return true;}
                if(!fixture)
                {
                    if(name=="OwnedPage"){target=new Rect(1270,615,219,60);return true;}
                    if(name=="OpenFormation"||name=="ManageTrainingCore"){target=LandscapeSectionLayout.Section(r,1003,40,710,.75f);return true;}
                    if(name=="GrowthNotice"){target=LandscapeSectionLayout.Section(r,1085,40,395,.75f);return true;}
                    if(r.y>=1200){target=LandscapeSectionLayout.Section(r,1200,40,490,.75f);return true;}
                }
                if(r.y>=180&&r.y<438){target=LandscapeSectionLayout.Section(r,180,40,175,.75f);return true;}
            }
            if(CurrentPage==LobbyPage.Shop)
            {
                if(fixture)
                {
                    if(name.StartsWith("Category")){target=new Rect(300+(r.x-34)/291*350,160,r.width,r.height);return true;}
                    if(name.StartsWith("Product")||name.StartsWith("Buy"))
                    {
                        int row=Mathf.Max(0,Mathf.FloorToInt((r.y-530)/326));
                        int column=Mathf.Max(0,Mathf.FloorToInt((r.x-34)/449));int index=row*2+column;
                        target=ArrangeRowPart(r,new Vector2(34+column*449,530+row*326),new Vector2(95+index%3*490,235+index/3*265),.8f);return true;
                    }
                }
                else if(Category!=2&&r.y>=529){target=LandscapeSectionLayout.Section(r,529,850,165,.6f);return true;}
            }
            if(CurrentPage==LobbyPage.Recruit)
            {
                if(fixture&&r.y>=962){target=LandscapeSectionLayout.Section(r,962,810,190,.75f);return true;}
                if(!fixture&&r.y>=325&&r.y<1160){target=LandscapeSectionLayout.Section(r,325,810,165,.65f);return true;}
                if(!fixture&&r.y>=1160){target=LandscapeSectionLayout.Section(r,1160,40,400,.75f);return true;}
            }
            if(CurrentPage==LobbyPage.Train&&!fixture&&Category>=2)
            {
                if(r.y>=445&&r.y<1214){target=LandscapeSectionLayout.Section(r,445,810,165,.7f);return true;}
                if(r.y>=1214){target=LandscapeSectionLayout.Section(r,1214,40,440,.75f);return true;}
            }
            return false;
        }
        static Rect ArrangeRowPart(Rect source,Vector2 sourceOrigin,Vector2 destination,float scale)
            =>new Rect(destination.x+(source.x-sourceOrigin.x)*scale,destination.y+(source.y-sourceOrigin.y)*scale,source.width*scale,source.height*scale);
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color? tint=null)
        {
            var r=Rect(name,parent,x,y,w,h);var img=r.gameObject.AddComponent<Image>();img.color=tint??new Color(.025f,.033f,.04f,.97f);img.raycastTarget=false;
            if(tint==null){var frame=Rect("Frame",r,0,0,w,h).gameObject.AddComponent<Image>();
                if(sharedFrame==null)sharedFrame=LobbyArtwork.CreateFrame();frame.sprite=sharedFrame;frame.type=Image.Type.Sliced;frame.fillCenter=false;frame.raycastTarget=false;}
            return r;
        }
        Button Button(string name,Transform parent,float x,float y,float w,float h,string label,Action action,bool selected=false,bool transparent=false)
        {
            var r=Panel(name,parent,x,y,w,h,transparent?(Color?)Color.clear:null);var image=r.GetComponent<Image>();image.raycastTarget=true;
            if(selected)image.color=new Color(.02f,.22f,.37f,.98f);
            if(!transparent && selected && label!=null && !name.StartsWith("Category") && !name.StartsWith("FormationSlot")){
                var texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryUi/primary");
                if(texture!=null){var skin=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(35,14,35,14));skins.Add(skin);image.sprite=skin;image.type=Image.Type.Sliced;image.color=Color.white;var frame=r.Find("Frame");if(frame!=null)frame.gameObject.SetActive(false);}
            }
            if(selected && name.StartsWith("Category"))Panel("ActiveUnderline",r,12,h-5,w-24,3,new Color(.15f,.8f,1));
            long boundGeneration=viewGeneration;
            bool chromeInput=parent==chromeContent;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.disabledColor=new Color(.65f,.65f,.65f,1);b.colors=colors;b.onClick.AddListener(()=>{if(this!=null&&b!=null&&IsOpen&&isActiveAndEnabled&&b.IsInteractable()&&b.gameObject.activeInHierarchy&&(chromeInput||boundGeneration==viewGeneration))action?.Invoke();});if(label!=null)Text("Label",r,label,8,3,w-16,h-6,30,true);return b;
        }
        void Guarded(string name,Transform parent,float x,float y,float w,float h,string label){var b=Button(name,parent,x,y,w,h,label,null,true);b.interactable=false;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,bool center=false){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.richText=false;t.raycastTarget=false;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.70f;t.fontSizeMax=size;t.color=new Color(.93f,.91f,.84f);t.alignment=center?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;return t;}
        RawImage Crop(string name,Transform parent,Texture2D texture,float x,float y,float w,float h,float sx,float sy,float sw,float sh){var im=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();im.texture=texture;im.uvRect=new Rect(sx/941f,1-(sy+sh)/1672f,sw/941f,sh/1672f);im.raycastTarget=false;return im;}
        void Bar(Transform parent,float x,float y,float w,float h,float value){Panel("Track",parent,x,y,w,h, new Color(.09f,.16f,.18f));Panel("Fill",parent,x,y,w*value,h,new Color(.1f,.7f,.87f));}
    }
}


