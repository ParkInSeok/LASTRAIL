using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public enum LobbyPage { Operators, Train, Recruit, Shop, Formation, Missions }

    // View-only navigation. No economy mutations or fabricated backend acknowledgements.
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
        LobbyCommandDeck backOwner;
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
        long viewGeneration;
        AccountScreenRuntime accountState;
        string selectedFormationOperator;
        int rosterPage,presetPage,recruitView,recruitListPage;
        public void RefreshAccountView(AccountScreenRuntime state){BindAccount(state);if(IsOpen&&!fixture&&(CurrentPage==LobbyPage.Formation||CurrentPage==LobbyPage.Operators||CurrentPage==LobbyPage.Recruit||CurrentPage==LobbyPage.Train||CurrentPage==LobbyPage.Shop||CurrentPage==LobbyPage.Missions))Rebuild();}
        public void BindAccount(AccountScreenRuntime state){if(accountState!=state)selectedFormationOperator=null;accountState=state;if(content!=null&&!fixture){for(int i=0;i<3;i++){var label=content.Find("CurrencyValue"+i)?.GetComponent<TMP_Text>();if(label!=null)label.text=state?.BalanceText(i)??"—";}
            var notice=content.Find("AccountStateNotice")?.GetComponent<TMP_Text>();if(notice!=null)notice.text=state?.AccountNotice??"";}}

        RectTransform content;
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
            var page=root.AddComponent<LobbyDestinationPages>();page.font=font;page.closed=closed;page.backOwner=parent.GetComponentInParent<LobbyCommandDeck>();
            page.backdrop=page.Rect("Environment",root.transform,0,0,941,1672).gameObject.AddComponent<RawImage>();page.backdrop.raycastTarget=false;
            page.Controls=page.Rect("PageControls",root.transform,0,0,1600,900);page.Controls.anchorMin=page.Controls.anchorMax=page.Controls.pivot=new Vector2(.5f,.5f);page.Controls.anchoredPosition=Vector2.zero;
            page.chrome=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/icons-landscape-v01");root.SetActive(false);return page;
        }
        public void Open(LobbyPage page,bool editorFixture=false){_ = OpenPreparedPageAsync(page,editorFixture);}
        void OpenPreparedPage(LobbyPage page,bool editorFixture)
        {
            fixture=false;
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
            fixture=editorFixture;
#endif
            viewGeneration++;ultimatePanel=false;selectedFormationOperator=null;CurrentPage=page;Category=0;SelectedMember=0;SelectedSlot=0;IsOpen=true;
            gameObject.SetActive(true);transform.SetAsLastSibling();Rebuild();
            if(page==LobbyPage.Recruit&&!fixture){recruitView=0;recruitListPage=0;LoadRecruitment();}
            if(page==LobbyPage.Train&&!fixture&&accountState!=null)LoadTrain();
            if(page==LobbyPage.Formation&&!fixture&&accountState!=null)LoadUltimatePage();
            if(page==LobbyPage.Shop&&!fixture&&accountState!=null)RefreshShop();if(page==LobbyPage.Missions&&!fixture&&accountState!=null)RefreshMissions();
        }
        async void RefreshTrain(){await System.Threading.Tasks.Task.WhenAll(accountState.RefreshSharedAsync(),accountState.RefreshTrainLoadoutAsync());}
        async void LoadTrain(){var state=accountState;if(state!=null)await System.Threading.Tasks.Task.WhenAll(state.EnsureSharedAsync(),state.EnsureTrainLoadoutAsync());}
        async void LoadRecruitment(bool refresh=false){var state=accountState;if(state==null)return;await System.Threading.Tasks.Task.WhenAll(refresh?state.RefreshSharedAsync():state.EnsureSharedAsync(),state.RefreshBannersAsync());if(this!=null&&IsOpen&&CurrentPage==LobbyPage.Recruit&&state.BannersReady&&state.Banners.Count>0)await state.SelectBannerAsync(state.Banners.Any(b=>b.Id==state.SelectedBanner?.Id)?state.SelectedBanner.Id:state.Banners[0].Id);}
        public void Close(){bool opening=pageResourceRequest!=null;ReleasePageResources();if(!IsOpen&&!opening)return;IsOpen=false;gameObject.SetActive(false);closed?.Invoke();}
        void OnGUI(){if(IsOpen&&backOwner==null)backKey.Handle(Event.current,RequestBack,ScreenBackKey.Editing);}
        void OnEnable(){backKey.Synchronize();}
        void OnApplicationFocus(bool focused){if(focused)backKey.Synchronize();}
        void OnRectTransformDimensionsChange(){Fit();}
        void OnDisable(){ReleasePageResources();viewGeneration++;ReleaseDetailInput();ReleaseEquipmentMultishot();IsOpen=false;}
        void OnDestroy(){ReleasePageResources();viewGeneration++;IsOpen=false;ReleaseEquipmentMultishot();ReleaseSkins();accountState=null;closed=null;content=null;Controls=null;backdrop=null;font=null;art=null;chrome=null;}
        void ReleaseSkins(){foreach(var skin in skins)if(skin!=null){if(Application.isPlaying)Destroy(skin);else DestroyImmediate(skin);}skins.Clear();}
        void Clear()
        {
            ReleaseDetailInput();
            viewGeneration++;
            if(content!=null){content.gameObject.SetActive(false);if(Application.isPlaying)Destroy(content.gameObject);else DestroyImmediate(content.gameObject);}
            ReleaseSkins();pageLandscape=new LandscapeSectionLayout();overlayLayouts.Clear();content=Rect("PageContent",Controls,0,0,1600,900);
        }
        void Rebuild()
        {
            ReleaseEquipmentMultishot();Clear();bool plan=CurrentPage==LobbyPage.Formation&&!fixture;
            art=plan?null:LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/"+Keys[(int)CurrentPage]+"-atlas");
            backdrop.texture=plan?LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01"):LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/"+Keys[(int)CurrentPage]+"-background");
            backdrop.enabled=true;backdrop.color=plan?new Color(.18f,.20f,.22f,1):Color.white;
            Header();
            switch(CurrentPage){case LobbyPage.Operators:Operators();break;case LobbyPage.Train:Train();break;case LobbyPage.Recruit:Recruit();break;case LobbyPage.Shop:Shop();break;case LobbyPage.Formation:Formation();break;case LobbyPage.Missions:Missions();break;}
            Navigation();Fit();BindAccount(accountState);
        }
        void Header()
        {
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
        }
        void Navigation()
        {
            string[] labels={"로비","대원","열차","모집","상점"};int active=CurrentPage==LobbyPage.Formation?1:CurrentPage==LobbyPage.Missions?-1:(int)CurrentPage+1;
            if(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel)
            {
                for(int i=0;i<5;i++){int n=i;var button=Button("PageNav"+i,content,10+i*318,835,308,60,labels[i],()=>{if(n==0)Close();else Open((LobbyPage)(n-1),fixture);},i==active);LobbyTabVisual.ApplyText(button,button.GetComponentInChildren<TMP_Text>(),i==active);}
                return;
            }
            int[] sx={63,253,442,630,815};
            for(int i=0;i<5;i++){int n=i;var button=Button("PageNav"+i,content,10+i*186,1534,181,122,null,()=>{if(n==0)Close();else Open((LobbyPage)(n-1),fixture);},i==active);
                var icon=Crop("Icon",button.transform,chrome,58,13,65,53,sx[i],1547,65,53);icon.uvRect=LobbyArtwork.IconUv(new[]{10,9,11,12,13}[i]);var label=Text("Label",button.transform,labels[i],0,75,181,36,28,true);LobbyTabVisual.Apply(button,icon,label,i==active,true);}
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
            var state=accountState;var snapshot=state?.Account;
            if(snapshot==null||snapshot.Operators.Count==0){Unavailable(575,snapshot==null?state?.AccountNotice??"대원 정보를 불러오지 못했습니다.":"보유한 대원이 없습니다.");return;}
            SelectedMember=Math.Max(0,Math.Min(SelectedMember,snapshot.Operators.Count-1));var selected=snapshot.Operators[SelectedMember];var id=selected.Text("operatorId");
            var portrait=OperatorVisualCatalog.Portrait(id);if(portrait!=null){var image=Rect("SelectedPortrait",content,34,218,280,203).gameObject.AddComponent<RawImage>();image.texture=portrait;image.uvRect=OperatorVisualCatalog.Crop(portrait,280,203);image.raycastTarget=false;}
            Text("SelectedName",content,OperatorVisualCatalog.Label(id),343,228,563,57,42);
            var core=snapshot.TrainingCoreSlots.FirstOrDefault(s=>s.Text("operatorId")==id);
            Text("OwnedGrowthState",content,(core==null?"훈련 코어 미배치":"훈련 Lv."+core.Number("trainedLevel"))+"\n스킬 훈련 "+selected.Number("skillTraining")+" · 승급 "+selected.Number("promotion"),343,301,563,103,29);
            Tabs(new[]{"전체","전투","지원"},438);
            // Role catalog is not in S01; do not invent membership from art metadata.
            content.Find("Category1").GetComponent<Button>().interactable=false;content.Find("Category2").GetComponent<Button>().interactable=false;
            var pageCount=Math.Max(1,(snapshot.Operators.Count+5)/6);rosterPage%=pageCount;
            for(int i=0;i<6;i++){int index=rosterPage*6+i;if(index>=snapshot.Operators.Count)break;var operatorId=snapshot.Operators[index].Text("operatorId");float x=34+i%3*296,y=525+i/3*237;
                var button=Button("SelectOwnedOperator"+index,content,x,y,280,220,null,()=>{SelectedMember=index;Rebuild();},index==SelectedMember);
                var face=OperatorVisualCatalog.Portrait(operatorId);if(face!=null){var img=Rect("Portrait",button.transform,8,8,264,161).gameObject.AddComponent<RawImage>();img.texture=face;img.uvRect=OperatorVisualCatalog.Crop(face,264,161);img.raycastTarget=false;}
                Text("OwnedName",button.transform,OperatorVisualCatalog.Label(operatorId),8,177,264,35,28,true);
            }
            Button("OwnedPage",content,688,1003,219,60,(rosterPage+1)+" / "+pageCount,()=>{rosterPage=(rosterPage+1)%pageCount;Rebuild();});
            Button("OpenFormation",content,34,1003,425,60,"편성",()=>Open(LobbyPage.Formation));
            Button("ManageTrainingCore",content,475,1003,197,60,"훈련 코어",()=>CoreManager(id));
            Text("GrowthNotice",content,string.IsNullOrEmpty(state.ActionNotice)?state.GrowthNotice:state.ActionNotice,34,1085,873,95,25,true);
            var kinds=new[]{"growth-core","growth-skill","growth-promotion"};var titles=new[]{"레벨업","스킬 훈련","승급"};
            for(int i=0;i<3;i++){string kind=kinds[i],target=i==0?core?.Number("slot").ToString():id;float x=34+i*296;
                Panel("GrowthOption"+i,content,x,1200,280,290);var cost=state.GrowthReady?state.Growth?.Cost(kind,target,snapshot):null;
                Text("GrowthCost"+i,content,i==0&&core==null?"훈련 코어 미배치":OperatorGrowthConfig.CostLabel(cost),x+12,1220,256,122,24,true);
                var button=Button("GrowOperator"+i,content,x+10,1370,260,91,titles[i],async()=>{await state.GrowAsync(kind,target);if(this!=null&&IsOpen)Rebuild();},true);button.interactable=state.CanGrow(kind,target);
                if(i==0&&state.Pending?.Kind.StartsWith("growth-")==true){button.GetComponentInChildren<TMP_Text>().text="결과 재확인";button.onClick.RemoveAllListeners();button.onClick.AddListener(async()=>{await state.RetryPendingAsync();if(this!=null&&IsOpen)Rebuild();});button.interactable=!state.Busy;}
            }
        }
        RectTransform CoreShield(string name)
        {
            ReleaseDetailInput();
            foreach(Transform child in content)if(!child.name.StartsWith("PageNav"))child.gameObject.SetActive(false);
            var shield=Rect(name,content,0,0,941,1515);var shade=shield.gameObject.AddComponent<Image>();shade.color=new Color(.008f,.014f,.021f,.97f);
            detailBack=()=>CloseCoreShield(shield);detailInput=new AugmentSelection.AugmentModalInputScope(shield,transform,null);return shield;
        }
        void CloseCoreShield(RectTransform shield){if(shield==null||!shield.gameObject.activeInHierarchy)return;shield.gameObject.SetActive(false);Rebuild();}
        void CoreManager(string operatorId)
        {
            var state=accountState;if(state?.Account==null)return;var shield=CoreShield("TrainingCoreManager");
            Text("CoreTitle",shield,"훈련 코어",40,185,861,65,42,true);
            Text("CoreCandidate",shield,"선택 대원: "+OperatorVisualCatalog.Label(operatorId)+"\n빈 슬롯에 배치하거나 기존 대원을 교체합니다.",45,268,851,110,29,true);
            for(int i=0;i<5;i++){
                int slot=i;var core=state.Account.TrainingCoreSlots.FirstOrDefault(s=>s.Number("slot")==slot);float y=415+i*139;
                Panel("CoreSlot"+i,shield,40,y,861,123);
                Text("CoreState"+i,shield,(i+1)+"  "+(core==null?"빈 슬롯":OperatorVisualCatalog.Label(core.Text("operatorId"))+" · Lv."+core.Number("trainedLevel"))+"\n기록된 투자 EXP "+(core?.Number("investedOperatorExp")??0).ToString("N0"),64,y+12,538,100,27);
                var button=Button("ReplaceCore"+i,shield,638,y+19,238,85,core==null?"배치":"교체",()=>{
                    var refund=core?.Number("investedOperatorExp")??0;
                    CloseCoreShield(shield);CoreConfirm("코어 "+(slot+1)+(core==null?" 배치":" 교체"),OperatorVisualCatalog.Label(operatorId)+"을(를) Lv.1로 배치합니다.\n기록된 투자 EXP "+refund.ToString("N0")+" 전액 환급\n자동 재투자는 하지 않습니다.",()=>state.CanReplaceCore(slot,operatorId),()=>state.ReplaceCoreAsync(slot,operatorId));
                },true);button.interactable=state.CanReplaceCore(slot,operatorId);
            }
            Text("CoreSyncExplanation",shield,"동기 성장: 낮은 레벨부터 보유 EXP로 성장\n5개 슬롯 필요 · 배치된 대원은 중복 배치 불가",48,1124,845,98,27,true);
            var sync=Button("SyncTrainingCore",shield,48,1247,845,90,"동기 성장",()=>{
                var balance=state.Account.Currencies.TryGetValue("OPERATOR_EXP",out var exp)?exp:0;
                CloseCoreShield(shield);CoreConfirm("동기 성장","보유 EXP "+balance.ToString("N0")+" 범위에서\n낮은 레벨부터 반복 성장합니다.\nEXP가 부족하거나 모두 최고 레벨이면 멈춥니다.",()=>state.CanGrow("growth-sync","all"),()=>state.GrowAsync("growth-sync","all"));
            },true);sync.interactable=state.CanGrow("growth-sync","all");
            Button("CloseTrainingCore",shield,48,1370,845,90,"닫기",()=>CloseCoreShield(shield));
        }
        void CoreConfirm(string title,string message,Func<bool> allowed,Func<System.Threading.Tasks.Task> submit)
        {
            var shield=CoreShield("CoreConfirmation");Text("Title",shield,title,65,470,811,75,40,true);
            Text("Message",shield,message,65,596,811,240,29,true);
            var confirm=Button("ConfirmCoreAction",shield,65,925,811,100,"실행",async()=>{if(shield==null||!shield.gameObject.activeInHierarchy)return;CloseCoreShield(shield);var owner=accountState;long generation=viewGeneration;if(allowed())await submit();if(this!=null&&IsOpen&&ReferenceEquals(owner,accountState)&&generation==viewGeneration)Rebuild();},true);confirm.interactable=allowed();
            Button("CancelCoreAction",shield,65,1056,811,90,"취소",()=>CloseCoreShield(shield));
        }
        void Train()
        {
            if(!fixture){LiveTrain();return;}
            Tabs(new[]{"기관차","전투칸","지원칸","화물칸"},866);
            string title=new[]{"기관차","전투칸","지원칸","화물칸"}[Category];
            Text("SelectedCar",content,title,325,325,350,55,38,true);
            Panel("CarDetails",content,29,996,883,486);Text("CarTitle",content,title+" 강화",60,1012,800,65,36);
            Crop("CarArt",content,art,49,1090,322,350,49,1090,322,350);
            Text("CarStats",content,fixture?"내구도  5,200  →  5,800\n\n출력     120  →  140":"열차 정보를 불러오지 못했습니다.",405,1092,465,190,29);
            Text("Materials",content,fixture?"필요 재료\n강철 · 기어 · 전선":"",407,1290,460,95,27);
            Guarded("Upgrade",content,400,1387,470,72,"강화");
        }
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
        void LiveTrain()
        {
            Tabs(new[]{"기관차","차량 배치","보유 차량","보유 포탑"},202);var state=accountState;var snapshot=state?.Account;
            if(Category==1){LiveTrainLoadout();return;}
            if(snapshot==null){Unavailable(650,"열차 정보를 불러오지 못했습니다.");Button("RefreshTrain",content,60,1350,821,90,"다시 확인",()=>RefreshTrain());return;}
            if(Category!=0){
                var rows=Category==2?snapshot.Carriages:snapshot.Turrets;var key=Category==2?"carriageId":"turretId";
                Panel("TrainHoldings",content,34,445,873,769);Text("HoldingsTitle",content,Category==2?"보유 차량":"보유 포탑",64,462,813,60,36,true);
                var pages=Math.Max(1,(rows.Count+5)/6);rosterPage%=pages;
                if(rows.Count==0)Text("NoTrainHoldings",content,"보유 정보가 없습니다.",65,650,811,95,31,true);
                for(int i=0;i<6;i++){int index=rosterPage*6+i;if(index>=rows.Count)break;var row=rows[index];Text("TrainHolding"+index,content,row.Text(key)+" · Lv."+row.Number("level"),65,565+i*84,811,77,30);}
                Button("TrainHoldingsPage",content,64,1109,813,76,(rosterPage+1)+" / "+pages,()=>{rosterPage=(rosterPage+1)%pages;Rebuild();});
                Text("TrainEquipmentStatus",content,Category==2?"차량 배치 탭에서 장착을 확인하세요.":"포탑 장착은 준비 중입니다.",65,1245,811,85,28,true);Button("RefreshTrain",content,65,1360,811,90,"새로고침",()=>RefreshTrain());return;
            }
            var train=snapshot.Train;
            Text("OwnedTrainName",content,train==null?"보유 기관차가 없습니다.":train.Text("trainId"),65,840,811,80,41,true);
            Panel("TrainGrowthPanel",content,34,947,873,539);Text("TrainLevel",content,train==null?"—":"Lv."+train.Number("level"),64,970,813,66,38,true);
            var id=train?.Text("trainId");var cost=state.GrowthReady?state.Growth?.Cost("growth-train",id,snapshot):null;
            Text("TrainGrowthCost",content,OperatorGrowthConfig.CostLabel(cost),65,1052,811,133,30,true);
            Text("TrainGrowthNotice",content,string.IsNullOrEmpty(state.ActionNotice)?state.GrowthNotice:state.ActionNotice,65,1200,811,92,25,true);
            var upgrade=Button("UpgradeTrain",content,65,1320,811,100,"기관차 강화",()=>CoreConfirm("기관차 강화",OperatorGrowthConfig.CostLabel(cost)+"\n위 재료를 사용해 1레벨 강화합니다.",()=>state.CanGrow("growth-train",id),()=>state.GrowAsync("growth-train",id)),true);upgrade.interactable=state.CanGrow("growth-train",id);
            if(state.Pending?.Kind=="growth-train"){upgrade.GetComponentInChildren<TMP_Text>().text="강화 결과 재확인";upgrade.onClick.RemoveAllListeners();upgrade.onClick.AddListener(async()=>await state.RetryPendingAsync());upgrade.interactable=!state.Busy;}
            Button("RefreshTrain",content,65,1432,811,45,"새로고침",()=>RefreshTrain());
        }
        void LiveTrainLoadout()
        {
            var state=accountState;var snapshot=state?.TrainLoadout;if(snapshot==null){Unavailable(650,state?.TrainLoadoutNotice??"장착 정보를 불러오지 못했습니다.");Button("RefreshTrainLoadout",content,65,1320,811,90,"다시 확인",()=>RefreshTrain());return;}
            Panel("LoadoutPanel",content,34,313,873,1190,new Color(.008f,.016f,.024f,.95f));
            Text("LoadoutState",content,(state.TrainDraftDirty?"편집 중":"저장된 배치")+" · 슬롯 "+(snapshot.SlotCount?.ToString()??"확인 필요")+(state.TrainLoadoutReady?"":" · 이전 정보"),55,330,831,62,31,true);
            var selected=state.TrainDraft;SelectedSlot=Math.Max(0,Math.Min(SelectedSlot,3));
            for(int i=0;i<4;i++){int slot=i;bool unlocked=snapshot.SlotCount.HasValue&&i<snapshot.SlotCount.Value;var label=(i+1)+" · "+(selected[i]??(unlocked?"차량 선택":"잠김"));
                var b=Button("TrainLoadoutSlot"+i,content,55+i%2*426,410+i/2*101,405,86,label,()=>{SelectedSlot=slot;Rebuild();},i==SelectedSlot);b.interactable=unlocked&&state.CanEditTrain;
            }
            var rows=snapshot.Carriages;var pages=Math.Max(1,(rows.Count+5)/6);rosterPage%=pages;
            for(int i=0;i<6;i++){int index=rosterPage*6+i;if(index>=rows.Count)break;var c=rows[index];string id=(string)c["carriageId"],reason=(string)c["reason"];bool eligible=(bool)c["installable"];
                var b=Button("InstallOwnedCarriage"+index,content,55,640+i*72,831,64,id+" · Lv."+c["level"]+(eligible?"":" · "+AccountScreenRuntime.TrainLoadoutProblem(reason)),()=>state.SelectTrainCarriage(SelectedSlot,id),selected.Contains(id));b.interactable=eligible&&state.CanEditTrain&&snapshot.SlotCount.HasValue&&SelectedSlot<snapshot.SlotCount.Value;
            }
            if(rows.Count==0)Text("NoLoadoutCarriages",content,"보유 차량이 없습니다.",65,690,811,110,30,true);
            Button("LoadoutRosterPage",content,55,1095,831,61,(rosterPage+1)+" / "+pages,()=>{rosterPage=(rosterPage+1)%pages;Rebuild();});
            Text("LoadoutNotice",content,"전투칸 1개 필수 · 열린 슬롯 모두 배치 · 중복 금지\n"+state.TrainLoadoutNotice+"\n"+state.ActionNotice,55,1168,831,122,24,true);
            var save=Button("SaveTrainLoadout",content,55,1310,831,87,"차량 배치 저장",async()=>await state.SaveTrainLoadoutAsync(),true);save.interactable=state.CanSaveTrainLoadout;
            if(state.Pending?.Kind=="train-loadout"){save.GetComponentInChildren<TMP_Text>().text="원래 장착 요청 재확인";save.onClick.RemoveAllListeners();save.onClick.AddListener(async()=>await state.RetryPendingAsync());save.interactable=!state.Busy;}
            var clear=Button("ClearTrainLoadout",content,55,1420,405,60,"선택 비우기",()=>state.ClearTrainDraft());clear.interactable=state.CanEditTrain;
            Button("RefreshTrainLoadout",content,481,1420,405,60,"새로고침",()=>RefreshTrain());
        }
        void LiveRecruitment()
        {
            var state=accountState;if(state==null){Unavailable(600,"모집 정보를 불러오지 못했습니다.");return;}
            Button("RecruitMain",content,34,210,273,73,"모집",()=>{recruitView=0;recruitListPage=0;Rebuild();},recruitView==0);
            Button("RecruitHistory",content,334,210,273,73,"모집 기록",async()=>{recruitView=1;recruitListPage=0;await state.RefreshHistoryAsync();},recruitView==1);
            Button("RecruitRates",content,634,210,273,73,"확률 정보",()=>{recruitView=2;recruitListPage=0;Rebuild();},recruitView==2);
            Panel("RecruitReadPanel",content,34,325,873,835,new Color(.008f,.016f,.024f,.94f));
            Text("RecruitReadNotice",content,state.RecruitmentNotice,55,1168,831,80,24,true);
            if(recruitView==1){
                Text("RecruitHistoryTitle",content,state.HistoryReady?(state.History.Count==0?"모집 기록이 없습니다.":"내 모집 기록"):"기록 확인 중 · 이전 정보",60,350,821,70,33,true);
                var rows=state.History;var pages=Math.Max(1,(rows.Count+5)/6);recruitListPage%=pages;
                for(int i=0;i<6;i++){int index=recruitListPage*6+i;if(index>=rows.Count)break;var batch=rows[index];Button("ViewBatch"+index,content,65,443+i*99,811,86,batch.CreatedAt.Substring(0,16).Replace('T',' ')+" · "+batch.Count+"회 · "+(batch.Complete?"확인 완료":"확인 필요"),async()=>{recruitView=3;await state.RefreshBatchAsync(batch.Id);});}
                RecruitListControls(pages,()=>state.RefreshHistoryAsync(true),state.HistoryCursor!=null&&state.HistoryReady&&!state.HistoryLoading);
                Button("RefreshRecruitHistory",content,65,1267,811,88,"기록 새로고침",async()=>await state.RefreshHistoryAsync());return;
            }
            if(recruitView==3){
                var batch=state.SelectedBatch;Text("BatchTitle",content,batch==null?"모집 결과 확인 중":batch.Count+"회 모집 · "+(batch.Complete?"확인 완료":"일부 확인 필요"),60,343,821,66,34,true);
                if(batch!=null){Text("BatchPolicy",content,"정책 버전: "+(batch.Version??"미상 (과거 기록)")+(state.BatchReady?"":" · 이전 정보"),65,412,811,62,23,true);for(int i=1;i<=batch.Count;i++)Text("BatchResult"+i,content,batch.ResultLine(i),65,492+(i-1)*61,811,57,26);}
                Button("RefreshRecruitBatch",content,65,1267,811,88,"같은 결과 재조회",async()=>{if(state.ViewedBatchId!=null)await state.RefreshBatchAsync(state.ViewedBatchId);});
                if(state.Pending?.Kind=="gacha")Button("RetryRecruit",content,65,1370,811,83,"원래 모집 요청 재확인",async()=>await state.RetryPendingAsync());return;
            }
            var banner=state.SelectedBanner;
            if(recruitView==2){
                Text("RatesTitle",content,"기본 확률 / 다음 1회 확률",60,347,821,63,34,true);
                Text("RatesCaution",content,"다음 1회 확률은 10회 모집 전체에 고정 적용되지 않습니다.",60,420,821,90,25,true);
                if(banner!=null&&state.BannerReady){var rows=banner.Probabilities;var pages=Math.Max(1,(rows.Count+5)/6);recruitListPage%=pages;
                    for(int i=0;i<6;i++){int index=recruitListPage*6+i;if(index>=rows.Count)break;var r=rows[index];var id=(string)r["resultId"];var label=OperatorVisualCatalog.Label(id);Text("Probability"+index,content,label+" · "+(string)r["resultRarity"]+"\n"+((double)r["baseProbability"]*100).ToString("0.####",System.Globalization.CultureInfo.InvariantCulture)+"% / "+((double)r["nextPullProbability"]*100).ToString("0.####",System.Globalization.CultureInfo.InvariantCulture)+"%",70,535+i*78,801,73,24);}
                    RecruitListControls(pages,null,false);
                }else Text("RatesUnavailable",content,"배너를 선택하고 상세 정보를 확인해 주세요.",70,640,801,100,29,true);
                return;
            }
            Text("RecruitBannerTitle",content,banner?.Id??(state.BannersReady&&state.Banners.Count==0?"현재 진행 중인 모집이 없습니다.":"배너를 확인해 주세요."),65,363,811,90,34,true);
            var list=state.Banners;var listPages=Math.Max(1,(list.Count+4)/5);recruitListPage%=listPages;
            for(int i=0;i<5;i++){int index=recruitListPage*5+i;if(index>=list.Count)break;var b=list[index];Button("SelectRecruitBanner"+index,content,65,468+i*77,811,65,b.Id+" · "+(b.Type=="PICKUP"?"특별 모집":"일반 모집"),async()=>await state.SelectBannerAsync(b.Id),b.Id==banner?.Id);}
            if(banner!=null){var p=banner.Pity;var c=banner.Thresholds;Text("RecruitPity",content,"누적 "+p["totalPullCount"]+"회 · 다음 보장 "+GuaranteeLabel(banner.Guarantee)+"\n희귀 "+p["rareOrHigherPityCount"]+" / "+c["rare"]+" · 정예 "+p["elitePityCount"]+" / "+c["elite"]+" · 픽업 "+p["pickupElitePityCount"]+" / "+c["pickup"]+"\n"+(banner.Availability=="ACTIVE"?"진행 중":banner.Availability=="ENDED"?"종료된 모집":"진행 예정")+" · 정책 "+banner.Version,65,881,811,146,25,true);}
            RecruitListControls(listPages,()=>state.RefreshBannersAsync(true),state.BannerCursor!=null&&state.BannersReady&&!state.BannersLoading);
            Text("RecruitTicketBalance",content,"모집권 "+(state.AccountStatus==AccountStateReadStatus.Ready?state.RecruitmentTickets.ToString("N0"):"—")+"개",65,1233,811,56,29,true);
            foreach(int count in new[]{1,10}){float x=count==1?36:482;var price=banner?.Price(count);var button=Button(count==1?"RecruitOne":"RecruitTen",content,x,1300,423,105,count+"회 모집\n모집권 "+(price?.ToString()??"—")+"개",()=>CoreConfirm(count+"회 모집","모집권 "+price+"개를 사용합니다.",()=>state.CanRecruit(count),async()=>{await state.RecruitAsync(count);if(state.SelectedBatch!=null)recruitView=3;}),true);button.interactable=state.CanRecruit(count);}
            var refresh=Button("RefreshRecruitment",content,36,1423,871,71,state.Pending?.Kind=="gacha"?"원래 모집 요청 재확인":"배너 새로고침",async()=>{if(state.Pending?.Kind=="gacha"){await state.RetryPendingAsync();if(state.SelectedBatch!=null)recruitView=3;if(this!=null&&IsOpen)Rebuild();}else LoadRecruitment(true);});refresh.interactable=!state.Busy;
        }
        static string GuaranteeLabel(string value)=>value=="ELITE_30"?"정예":value=="PICKUP_ELITE_60"?"픽업 정예":value=="RARE_OR_HIGHER_10"?"희귀 이상":"없음";
        void RecruitListControls(int pages,Func<System.Threading.Tasks.Task> loadMore,bool canMore)
        {
            Button("RecruitPrevious",content,65,1070,208,64,"이전",()=>{recruitListPage=(recruitListPage+pages-1)%pages;Rebuild();});
            Text("RecruitPage",content,(recruitListPage+1)+" / "+pages,280,1080,162,45,25,true);
            Button("RecruitNext",content,447,1070,208,64,"다음",()=>{recruitListPage=(recruitListPage+1)%pages;Rebuild();});
            if(loadMore!=null){var more=Button("RecruitLoadMore",content,666,1070,210,64,"더 불러오기",async()=>{await loadMore();});more.interactable=canMore;}
        }
        void LiveFormation()
        {
            if(ultimatePanel){LiveUltimate();return;}
            var state=accountState;var snapshot=state?.Account;
            if(snapshot==null){Unavailable(575,state?.AccountNotice??"편성 정보를 불러오지 못했습니다.");return;}
            var edit=state.Formation;var selection=edit.Selection;long generation=viewGeneration;
            Text("FormationHelp",content,"요원을 선택한 뒤 전투 위치를 눌러 배치하세요",36,212,870,50,30);
            var inputRect=Panel("PresetName",content,34,272,510,65);var name=Text("NameValue",inputRect,edit.Name,14,5,482,55,29);
            var input=inputRect.gameObject.AddComponent<TMP_InputField>();input.textViewport=inputRect;input.textComponent=(TextMeshProUGUI)name;input.text=edit.Name;input.characterLimit=80;input.interactable=!state.Busy&&state.Pending==null;
            input.onEndEdit.AddListener(value=>{if(this==null||!IsOpen||generation!=viewGeneration||accountState!=state)return;edit.Rename(value);var saveControl=content.Find("SaveFormation")?.GetComponent<Button>();if(saveControl!=null)saveControl.interactable=edit.CanSave;var activateControl=content.Find("ActivateFormation")?.GetComponent<Button>();if(activateControl!=null)activateControl.interactable=edit.CanActivate;});
            var load=Button("LoadPreset",content,558,272,165,65,"불러오기",()=>{
                if(snapshot.Presets.Count>0){presetPage%=snapshot.Presets.Count;edit.Load(snapshot.Presets[presetPage++]);Rebuild();}
            });load.interactable=snapshot.Presets.Count>0&&!state.Busy&&state.Pending==null;
            var fresh=Button("NewPreset",content,737,272,170,65,"새 편성",()=>{edit.Reset();Rebuild();});fresh.interactable=!state.Busy&&state.Pending==null;
            var numbered=FormationPlanView.Create(content,font);
            numbered.Bind(slot=>{if(this!=null&&IsOpen&&generation==viewGeneration&&accountState==state)SelectFormationPosition(slot);});
            numbered.Render(selection,SelectedSlot,!state.Busy&&state.Pending==null);
            Panel("SlotDetails",content,29,877,883,94);Text("SelectedSlotInfo",content,"선택 위치 "+(SelectedSlot+1)+" · "+OperatorVisualCatalog.KoreanLabel(selection[SelectedSlot]),60,897,811,52,30);
            Text("ActiveFormation",content,"현재 활성 편성: "+(snapshot.ActivePresetId==null?"없음":snapshot.Presets.FirstOrDefault(p=>p.Text("id")==snapshot.ActivePresetId)?.Text("name")??"저장된 편성"),34,989,625,55,28,true);
            Button("OpenUltimateSelection",content,673,989,234,55,"분대장 선택",()=>{ultimatePanel=true;Rebuild();RefreshUltimatePage();});
            Panel("Roster",content,29,1060,883,265);Text("RosterTitle",content,"대원 목록",56,1070,620,40,28);
            var pageCount=Math.Max(1,(snapshot.Operators.Count+5)/6);rosterPage%=pageCount;
            Button("RosterNext",content,730,1070,150,45,(rosterPage+1)+" / "+pageCount,()=>{rosterPage=(rosterPage+1)%pageCount;Rebuild();});
            var rows=snapshot.Operators.Skip(rosterPage*6).Take(6).ToArray();
            for(int i=0;i<rows.Length;i++){var id=rows[i].Text("operatorId");float x=42+i*145;var button=Button("AssignOwned"+i,content,x,1132,174,190,null,()=>{if(state.Busy||state.Pending!=null)return;selectedFormationOperator=id;Rebuild();},selectedFormationOperator==id);button.interactable=!state.Busy&&state.Pending==null;
                var portrait=FormationPlanView.Portrait(id);if(portrait!=null){var holder=Rect("OwnedPortrait",button.transform,7,7,160,151);var image=LastRail.Auth.AuthFormVisuals.Rect("OwnedPortraitImage",holder).gameObject.AddComponent<RawImage>();image.texture=portrait;image.raycastTarget=false;image.uvRect=FormationPlanView.PortraitCrop(id,portrait);}
                Text("OwnedName",button.transform,OperatorVisualCatalog.KoreanLabel(id),4,158,166,29,22,true);if(selection.Contains(id)){FormationPlanView.SelectionBorder(button.transform,174,190);var check=Rect("AssignedBadge",button.transform,141,8,25,25).gameObject.AddComponent<RouletteSurfaceGraphic>();check.Radius=.5f;check.color=new Color(.02f,.45f,.7f);check.raycastTarget=false;Text("AssignedCheck",check.transform,"✓",0,0,25,25,22,true);}}
            Text("FormationNotice",content,state.ActionNotice,34,1324,873,68,23,true);
            bool Current()=>this!=null&&IsOpen&&CurrentPage==LobbyPage.Formation&&generation==viewGeneration&&accountState==state;
            var save=Button("SaveFormation",content,34,1402,425,85,"편성 저장",async()=>{await edit.SaveAsync();if(Current())Rebuild();},true);save.interactable=edit.CanSave;
            var activate=Button("ActivateFormation",content,482,1402,425,85,"활성화",async()=>{await edit.ActivateAsync();if(Current())Rebuild();},true);activate.interactable=edit.CanActivate;
            if(state.Pending?.Kind.StartsWith("formation-")==true){save.GetComponentInChildren<TMP_Text>().text="결과 재확인";save.onClick.RemoveAllListeners();save.onClick.AddListener(async()=>{await edit.RetryAsync();if(Current())Rebuild();});save.interactable=!state.Busy;}
        }
        void SelectFormationPosition(int slot)
        {
            var state=accountState;if(state==null||state.Busy||state.Pending!=null||CurrentPage!=LobbyPage.Formation)return;
            SelectedSlot=slot;
            if(selectedFormationOperator==null)selectedFormationOperator=state.Formation.Selection[slot];
            else if(state.Formation.Select(slot,selectedFormationOperator))selectedFormationOperator=null;
            Rebuild();
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
            var br=backdrop.rectTransform;br.anchorMin=br.anchorMax=br.pivot=new Vector2(.5f,.5f);br.anchoredPosition=Vector2.zero;br.sizeDelta=new Vector2(941,1672)*Mathf.Max(area.width/941f,area.height/1672f);
        }
        LandscapeSectionLayout pageLandscape=new LandscapeSectionLayout();
        readonly Dictionary<RectTransform,LandscapeSectionLayout> overlayLayouts=new Dictionary<RectTransform,LandscapeSectionLayout>();
        void ReflowPage()
        {
            if(content==null)return;
            float split=1060;
            if(CurrentPage==LobbyPage.Formation)split=ultimatePanel?985:877;
            if(CurrentPage==LobbyPage.Missions)split=fixture?1016:926;
            if(CurrentPage==LobbyPage.Train)split=Category==1?630:Category==0?947:1000;
            if(CurrentPage==LobbyPage.Shop)split=Category==2?920:529;
            pageLandscape.Apply(content,(name,r)=>
            {
                if(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel){if(name=="HeaderShade")return new Rect(0,0,1600,78);if(name=="Back")return new Rect(20,10,80,60);if(name=="PageTitle")return new Rect(130,5,340,65);if(name.StartsWith("CurrencyValue")&&int.TryParse(name.Substring(13),out var cv))return new Rect(847+cv*250,17,185,40);if(name.StartsWith("Currency")&&int.TryParse(name.Substring(8),out var ci))return new Rect(800+ci*250,15,240,45);if(name=="CurrencyIcon")return new Rect(809+(r.x-40)/304*250,19,34,34);}
                if(name=="HeaderShade")return new Rect(0,0,1600,145);
                if(name=="Back")return new Rect(20,25,91,70);
                if(name=="PageTitle")return new Rect(137,20,390,75);
                if(name.StartsWith("CurrencyValue")&&int.TryParse(name.Substring(13),out var value))return new Rect(594+value*305,45,205,45);
                if(name.StartsWith("Currency")&&int.TryParse(name.Substring(8),out var currency))return new Rect(535+currency*305,43,285,51);
                if(name=="CurrencyIcon")return new Rect(543+(r.x-40)/304*305,47,40,42);
                if(name=="AccountStateNotice")return new Rect(535,100,900,42);
                if(name.StartsWith("PageNav")&&int.TryParse(name.Substring(7),out var nav))return CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel?new Rect(10,130+nav*130,140,105):new Rect(180+nav*250,778,181,122);
                if(name=="TrainingCoreManager"||name=="CoreConfirmation"||name=="Notice"||name=="EquipmentMultishotDetails")return new Rect(0,0,1600,775);
                if(TryArrangeDestinationSection(name,r,out var section))return section;
                // Remaining sections use their existing group boundaries.
                var result=r.y<split?LandscapeSectionLayout.Section(r,210,40,165,.7f):LandscapeSectionLayout.Section(r,split,850,165,.7f);
                // Tall backing plates are decorative; do not draw them over the adjacent section.
                if(r.y<split&&r.yMax>split&&r.height>400)result.height=(split-r.y)*.7f;
                return result;
            }, !(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel));
            if(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel)
            {
                foreach(RectTransform section in content)
                {
                    // Reserve the left rail without distorting portraits or their child hit areas.
                    if(TryArrangeDestinationSection(section.name,default,out var formationRect))
                    {
                        section.anchoredPosition=new Vector2(160+formationRect.x*.9f,-(130+(formationRect.y-90)*.9f));
                        section.localScale=Vector3.one*.9f;
                    }
                    foreach(var childName in new[]{"Frame","Label","NameValue"})
                    {
                        var child=section.Find(childName) as RectTransform;if(child==null)continue;
                        Stretch(child);if(childName!="Frame"){child.offsetMin=new Vector2(8,3);child.offsetMax=new Vector2(-8,-3);}
                    }
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
            if(CurrentPage==LobbyPage.Formation&&!fixture&&!ultimatePanel)
            {
                if(name=="FormationPlan")target=new Rect(440,102,1120,467);
                else if(name=="FormationHelp")target=new Rect(445,576,1090,25);
                else if(name=="Roster")target=new Rect(15,90,405,685);
                else if(name=="RosterTitle")target=new Rect(30,105,225,40);
                else if(name=="RosterNext")target=new Rect(270,108,125,35);
                else if(name.StartsWith("AssignOwned")&&int.TryParse(name.Substring(11),out var index))target=new Rect(33+index%2*188,165+index/2*198,174,190);
                else if(name=="PresetName")target=new Rect(450,665,500,38);
                else if(name=="LoadPreset")target=new Rect(970,665,165,38);
                else if(name=="NewPreset")target=new Rect(1150,665,170,38);
                else if(name=="SlotDetails")target=new Rect(440,606,1120,56);
                else if(name=="SelectedSlotInfo")target=new Rect(460,609,760,48);
                else if(name=="ActiveFormation")target=new Rect(450,706,1000,30);
                else if(name=="OpenUltimateSelection")target=new Rect(1300,612,234,44);
                else if(name=="FormationNotice")target=new Rect(450,800,1090,30);
                else if(name=="SaveFormation")target=new Rect(990,739,550,61);
                else if(name=="ActivateFormation")target=new Rect(540,739,425,61);
                else return false;
                return true;
            }
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
                var skin=LobbyArtwork.CreateFrame();skins.Add(skin);frame.sprite=skin;frame.type=Image.Type.Sliced;frame.fillCenter=false;frame.raycastTarget=false;}
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
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.disabledColor=new Color(.65f,.65f,.65f,1);b.colors=colors;b.onClick.AddListener(()=>{if(this!=null&&IsOpen&&boundGeneration==viewGeneration)action?.Invoke();});if(label!=null)Text("Label",r,label,8,3,w-16,h-6,30,true);return b;
        }
        void Guarded(string name,Transform parent,float x,float y,float w,float h,string label){var b=Button(name,parent,x,y,w,h,label,null,true);b.interactable=false;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,bool center=false){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.richText=false;t.raycastTarget=false;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.70f;t.fontSizeMax=size;t.color=new Color(.93f,.91f,.84f);t.alignment=center?TextAlignmentOptions.Center:TextAlignmentOptions.MidlineLeft;return t;}
        RawImage Crop(string name,Transform parent,Texture2D texture,float x,float y,float w,float h,float sx,float sy,float sw,float sh){var im=Rect(name,parent,x,y,w,h).gameObject.AddComponent<RawImage>();im.texture=texture;im.uvRect=new Rect(sx/941f,1-(sy+sh)/1672f,sw/941f,sh/1672f);im.raycastTarget=false;return im;}
        void Bar(Transform parent,float x,float y,float w,float h,float value){Panel("Track",parent,x,y,w,h, new Color(.09f,.16f,.18f));Panel("Fill",parent,x,y,w*value,h,new Color(.1f,.7f,.87f));}
    }
}


