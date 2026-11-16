using System;
using LastRail.Presentation.Settings;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Screen host composes views and controllers; account/run owners retain authoritative state.
    [ExecuteAlways]
    public sealed partial class LobbyCommandDeck : MonoBehaviour
    {
        public LobbyCabinMotion Motion { get; private set; }
        public RectTransform FixedUi { get; private set; }
        public bool IsDialogOpen => dialog!=null && dialog.gameObject.activeInHierarchy;
        TMP_FontAsset font;
        Texture2D atlas;
        Sprite frameSkin;
        TMP_FontAsset lobbyFont;
        RectTransform dialog,layout;
        readonly ScreenBackKey backKey=new ScreenBackKey();
        Action dialogBack;
        AugmentSelection.AugmentModalInputScope dialogInput;
        public void RequestBack()
        {
            if(!isActiveAndEnabled)return;
            if(departureGuide!=null){CloseDepartureGuide();return;}
            if(IsDialogOpen)
            {
                if(settingsView!=null)settingsView.RequestBack();
                else if(storyArchiveView!=null)storyArchiveView.RequestBack();
                else if(operatorCodexView!=null)operatorCodexView.RequestBack();
                else dialogBack?.Invoke();
                return;
            }
            if(Pages?.IsOpen==true)Pages.RequestBack();
        }
        void OnGUI(){if(Application.isPlaying)backKey.Handle(Event.current,RequestBack,ScreenBackKey.Editing);}
        StoryArchiveView storyArchiveView;
        OperatorCodexView operatorCodexView;
        SettingsScreenView settingsView;
        LocalSettingsModel settingsModel;
        public void BindSettings(LocalSettingsModel model) { if(!ReferenceEquals(settingsModel,model)&&settingsView!=null)CloseDialog(); settingsModel=model; }
        TMP_Text level,operation;
        readonly List<TMP_Text> balances=new List<TMP_Text>();
        readonly List<RawImage> portraits=new List<RawImage>();
        readonly List<GameObject> emptyLabels=new List<GameObject>();
        IReadOnlyList<MissionShellMissionRowState> missions;
        OperationProgressController operationProgress;
        bool operationProgressReady;
        void BindOperationProgress()
        {
            operationProgress?.Dispose();operationProgress=null;
            if(accountState!=null&&isActiveAndEnabled)
                operationProgress=new OperationProgressController(accountState.StoryArchive,RenderOperationProgress);
        }
        void RenderOperationProgress(IReadOnlyList<MissionShellMissionRowState> rows,bool ready)
        {
            missions=rows;operationProgressReady=ready;
            if(operation!=null)operation.text=rows.FirstOrDefault(x=>x.IsServerListedUnlockedForDisplay)?.DisplayLabel??"작전 확인 필요";
            UpdateOperationList();UpdateDepartureDialog();LifecycleChanged();
        }
        Action support,account;
        Action pageReturn;
        bool reduced;
        bool visualFixture;
        LobbySquadPortraits squadPortraits;
        LobbyHomeView homeView;LobbyHomeController homeController;
        void BindHomeController(){homeController?.Dispose();homeController=new LobbyHomeController(accountState,value=>{if(!visualFixture)homeView?.Render(value);},text=>{if(this!=null&&isActiveAndEnabled&&Pages?.IsOpen!=true)ShowInfo("보유 재화",text);});}
        async void OpenCurrencyInfo(){if(homeController==null||!homeController.IsCurrent)BindHomeController();await homeController.OpenCurrencyAsync();}
        AccountScreenRuntime accountState;
        bool offlineDialog; OfflineSupplyView offlineSupplyView;
        public void BindAccount(AccountScreenRuntime state){if(!ReferenceEquals(accountState,state)||homeController?.IsCurrent==false)CloseDialog();if(accountState!=null)accountState.Changed-=AccountChanged;accountState=state;BindHomeController();if(state!=null&&isActiveAndEnabled)state.Changed+=AccountChanged;BindOperationProgress();if(state==null)RenderOperationProgress(Array.Empty<MissionShellMissionRowState>(),false);AccountChanged();}
        void AccountChanged(){if(homeController?.IsCurrent==false)CloseDialog();if(homeController==null||!homeController.IsCurrent)BindHomeController();homeController.Render();
            Pages?.RefreshAccountView(accountState);if(offlineDialog&&dialog!=null)UpdateOfflineDialog();RenderPreparationSelections();UpdateDepartureDialog();UpdateDepartureRecoveryDialog();UpdateStartLookupDialog();MonthlyChanged();PassChanged();BulkChanged();LifecycleChanged();}
        void ShowOfflineDialog(){OpenOfflineSupply(false);}
        void OpenOfflineSupply(bool roulette)=>OpenResourceDialog(roulette?RouletteResources:new[]{"NoncombatOffline/","EntryPopup/Rewards/","EntryPopup/roulette-reward-icons"},()=>OpenOfflineSupplyPrepared(roulette));
        void OpenOfflineSupplyPrepared(bool roulette)
        {
            ShowInfo("운행 보급","");offlineDialog=true;rouletteView=false;
            if(roulette)
            {
                var body=dialog.Find("DialogBody").GetComponent<TMP_Text>();Place(body.rectTransform,48,140,650,280);
                Label("OfflineActionNotice",dialog,"",48,425,650,90,24,TextAlignmentOptions.Center);
                Button("RefreshOffline",dialog,60,535,295,75,"새로고침",RefreshOfflinePreview);
                Button("ClaimOffline",dialog,375,535,315,75,"수령",ClaimOfflineReward);
                BuildOfflineAdEntry();rouletteView=true;BuildOfflineAd();
            }
            else
            {
                foreach(Transform child in dialog)child.gameObject.SetActive(false);
                dialog.GetComponent<Image>().color=Color.clear;dialog.sizeDelta=new Vector2(800,1450);
                offlineSupplyView=OfflineSupplyView.Create(dialog,font);offlineSupplyView.Bind(CloseDialog,RefreshOfflinePreview,ClaimOfflineReward,OpenOfflineRoulette);
            }
            UpdateOfflineDialog();EvaluateLayout();RefreshOffline();
        }
        async void RefreshOfflinePreview(){if(accountState!=null)await System.Threading.Tasks.Task.WhenAll(accountState.RefreshOfflineAsync(),accountState.RefreshAdSkipAsync());}
        async void ClaimOfflineReward(){if(accountState!=null)await accountState.ClaimOfflineAsync();}
        async void RefreshOffline(){var owner=accountState;var source=dialog;if(owner!=null){var session=owner.CaptureScreenSession();await System.Threading.Tasks.Task.WhenAll(owner.RefreshOfflineAsync(),owner.RefreshAdSkipAsync());if(this!=null&&isActiveAndEnabled&&offlineDialog&&source!=null&&dialog==source&&session()&&ReferenceEquals(owner,accountState)&&owner.HasPendingAd)await owner.RecheckAdAsync();}}
        void UpdateOfflineDialog(){if(offlineSupplyView!=null){offlineSupplyView.Render(new OfflineSupplyDisplayState(accountState));return;}var data=accountState?.Offline;var text=accountState?.OfflineNotice??"보급 정보를 불러오지 못했습니다.";
            if(data!=null)text="적립 시간 "+(data.ClaimableSeconds/3600)+"시간 "+(data.ClaimableSeconds%3600/60)+"분\n저장 상한 "+data.OfflineStorageCapHours+"시간\n\n"+string.Join("\n",data.RewardLines.Take(6).Select(x=>x.DisplayName+"  "+x.Quantity.ToString("N0")))+"\n"+accountState.OfflineNotice;
            dialog.Find("DialogBody").GetComponent<TMP_Text>().text=text;dialog.Find("OfflineActionNotice").GetComponent<TMP_Text>().text=accountState?.ActionNotice??"";
            var claim=dialog.Find("ClaimOffline").GetComponent<Button>();bool pending=accountState?.Pending?.Kind=="offline";claim.GetComponentInChildren<TMP_Text>().text=pending?"결과 재확인":"수령";
            claim.interactable=accountState!=null&&!accountState.Busy&&!accountState.OfflineLoading&&(pending||accountState.Pending==null&&data?.PreviewStatus==LastRail.OfflineRewards.OfflineRewardPreviewStatus.Available);
            dialog.Find("RefreshOffline").GetComponent<Button>().interactable=accountState!=null&&!accountState.Busy&&!accountState.OfflineLoading;UpdateOfflineAd();
        }

        public LobbyDestinationPages Pages { get; private set; }
        public void OpenPage(LobbyPage page,Action returnTo=null)
        {
            CloseDialog();
            pageReturn=returnTo;
            if(Pages==null)Pages=LobbyDestinationPages.Create(FixedUi,font,()=>{var resume=pageReturn;pageReturn=null;if(!isActiveAndEnabled)return;layout.gameObject.SetActive(true);Motion.gameObject.SetActive(true);EvaluateLayout();resume?.Invoke();});
            layout.gameObject.SetActive(false);Motion.gameObject.SetActive(false);Pages.BindNavigation(OpenPass,scope=>OpenBulkSelection(scope),()=>this!=null&&isActiveAndEnabled);Pages.BindAccount(accountState);Pages.Open(page,visualFixture);
        }

        public static LobbyCommandDeck Attach(GameObject screen,TMP_FontAsset font,string playerName,long xp,
            IReadOnlyList<MissionShellMissionRowState> missions,Action support,Action account)
        {
            LastRail.Presentation.GameLandscapeDisplay.Configure(screen.GetComponent<CanvasScaler>());
            var existing=screen.GetComponentInChildren<LobbyCommandDeck>(true);
            if(existing!=null)
            {
                existing.missions=missions;
                existing.GetComponentsInChildren<TMP_Text>(true).Single(x=>x.name=="PlayerProfileName").text=playerName;
                existing.level.text="XP "+xp.ToString("N0");
                existing.operation.text=missions?.FirstOrDefault(x=>x.IsServerListedUnlockedForDisplay)?.DisplayLabel??"작전 확인 필요";
                return existing;
            }
            var safe=screen.transform.Find("SafeArea");
            if(safe==null) throw new InvalidOperationException("Bound lobby requires SafeArea.");
            foreach(var oldLabel in safe.GetComponentsInChildren<TMP_Text>(true))
                if(oldLabel.name=="PlayerProfileName")oldLabel.name="LegacyProfileName";
            foreach(Transform child in safe) child.gameObject.SetActive(false);
            foreach(var image in screen.GetComponentsInChildren<Image>(true))
                if(image.name=="MissionSelectBackgroundImage" || image.name=="MissionSelectBackgroundShade") image.enabled=false;
            var go=new GameObject("LobbyCommandDeck",typeof(RectTransform));go.transform.SetParent(safe,false);
            var deck=go.AddComponent<LobbyCommandDeck>();Stretch((RectTransform)go.transform);
            deck.font=font;deck.missions=missions;deck.support=support;deck.account=account;
            deck.lobbyFont=LastRail.Resources.PreparedContent.Load<TMP_FontAsset>("LobbyDeck/serif-v07");
            deck.atlas=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/icons-landscape-v01");
            deck.frameSkin=LobbyArtwork.CreateFrame();
            deck.Motion=LobbyCabinMotion.Create(screen.transform);deck.Motion.transform.SetAsFirstSibling();
            deck.FixedUi=(RectTransform)go.transform;
            deck.layout=Rect("FixedControls",go.transform);deck.layout.sizeDelta=new Vector2(1600,900);
            deck.Build(playerName,xp);deck.homeView=new LobbyHomeView(deck.balances,deck.layout.Find("AccountStateNotice")?.GetComponent<TMP_Text>(),deck.squadPortraits);deck.BindHomeController();deck.EvaluateLayout();return deck;
        }
        void Build(string playerName,long xp)
        {
            var root=layout;
            var emblem=Rect("CommanderMedallion",root);Place(emblem,40,16,110,110);
            var face=emblem.gameObject.AddComponent<RouletteSurfaceGraphic>();face.Radius=.49f;face.EdgeBrightness=.35f;face.color=new Color(.11f,.13f,.14f);face.raycastTarget=false;
            var rim=Rect("Rim",emblem);Stretch(rim);var ring=rim.gameObject.AddComponent<RawImage>();ring.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/commander-rim-v07");ring.uvRect=new Rect(.04f,.04f,.92f,.92f);ring.raycastTarget=false;
            Crop("CommanderPortrait",root,30,12,140,134,30,12,140,134);
            Panel("ProfileNamePlate",root,170,38,160,50);
            Label("PlayerProfileName",root,playerName,182,78,415,40,29,TextAlignmentOptions.MidlineLeft);
            level=Label("CommanderExperience",root,"XP "+xp.ToString("N0"),182,42,310,36,29,TextAlignmentOptions.MidlineLeft);
            level.gameObject.SetActive(false);
            Button("Profile",root,30,12,560,125,null,()=>ShowInfo("지휘관",playerName+"\n지휘관 경험치 "+xp.ToString("N0")),true);
            var menu=Button("LobbyMenu",root,835,37,83,70,null,OpenMenu);
            Crop("MenuIcon",menu.transform,15,17,51,35,851,53,51,35);
            string[] currencies={"연료","크레딧","크리스탈"};
            for(int i=0;i<3;i++)
            {
                var x=43+i*295;
                var b=Button("Currency"+i,root,x,147,281,49,null,OpenCurrencyInfo);
                var currencyIcon=Crop("CurrencyIcon",b.transform,5,0,60,58,0,0,0,0);
                currencyIcon.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/currency-icons-v07");currencyIcon.uvRect=new Rect(i/3f,0,1/3f,1);
                balances.Add(Label("Balance",b.transform,"—",66,0,130,58,28,TextAlignmentOptions.Center));
                Label("Plus",b.transform,"+",210,0,30,58,32,TextAlignmentOptions.Center);
            }
            Label("AccountStateNotice",root,"",43,200,775,50,22,TextAlignmentOptions.TopLeft);
            string[] names={"출석","패스","임무","운행 보급"};
            int[] iy={212,313,414,516};
            for(int i=0;i<4;i++)
            {
                string name=names[i];float y=214+i*102;
                var b=Button("Shortcut"+i,root,839,y,82,94,null,()=>{if(name=="임무")OpenPage(LobbyPage.Missions);else if(name=="출석")OpenMonthlyAttendance();else if(name=="패스")OpenPass();else if(name=="운행 보급")ShowOfflineDialog();else ShowInfo(name,"지금은 정보를 불러올 수 없습니다.\n잠시 후 다시 확인해 주세요.");});
                Crop("ShortcutIcon",b.transform,5,0,72,68,845,iy[i],72,68);
                var shortcutLabel=Label("Label",b.transform,name,67,0,78,70,24,TextAlignmentOptions.Center);
                shortcutLabel.textWrappingMode=TextWrappingModes.NoWrap;
                Place((RectTransform)b.transform.Find("ShortcutIcon"),4,3,64,64);
            }
            Panel("SquadPanel",root,29,1095,883,246);
            var squadDivider=Rect("SquadDivider",root);Place(squadDivider,154,641,834,1);
            var divider=squadDivider.gameObject.AddComponent<Image>();divider.color=new Color(.58f,.44f,.25f,.75f);divider.raycastTarget=false;
            Label("SquadTitle",root,"현재 편성",62,1105,360,42,29,TextAlignmentOptions.MidlineLeft);
            var formation=Button("Formation",root,715,1105,180,44,"편성",()=>OpenPage(LobbyPage.Formation));
            formation.GetComponent<Image>().color=new Color(.07f,.15f,.20f,1);
            LobbyArtwork.BlueInset(formation.transform).transform.SetSiblingIndex(0);
            Crop("FormationIcon",formation.transform,31,7,44,30,775,1115,36,25);
            Place(formation.GetComponentInChildren<TMP_Text>().rectTransform,82,0,68,44);
            for(int i=0;i<5;i++)
            {
                float x=46+i*172;
                Panel("SquadSlot"+i,root,x-3,1159,166,171);
                var card=Crop("SquadPortrait"+i,root,x,1162,160,165,x,1162,160,165);
                portraits.Add(card);card.color=new Color(.22f,.25f,.28f);
                emptyLabels.Add(Label("EmptySlot",root,"+",x,1162,160,165,52,TextAlignmentOptions.Center).gameObject);
            }
            Panel("OperationPanel",root,29,1358,883,137);
            squadPortraits=root.gameObject.AddComponent<LobbySquadPortraits>();squadPortraits.Bind(portraits.ToArray(),emptyLabels.ToArray());
            Crop("OperationLandscape",root,50,1372,508,108,254,1372,304,108);
            Label("NextOperationCaption",root,"다음 작전",61,1370,450,36,23,TextAlignmentOptions.MidlineLeft);
            var available=missions?.FirstOrDefault(x=>x.IsServerListedUnlockedForDisplay);
            operation=Label("NextOperation",root,available?.DisplayLabel??"작전 확인 필요",61,1406,480,54,34,TextAlignmentOptions.MidlineLeft);
            var depart=Button("Departure",root,566,1375,324,105,"출발",OpenOperations);
            depart.GetComponent<Image>().color=new Color(.025f,.23f,.39f,1);
            LobbyArtwork.BlueInset(depart.transform).transform.SetSiblingIndex(0);
            depart.GetComponentInChildren<TMP_Text>().fontSizeMax=52;
            depart.GetComponentInChildren<TMP_Text>().fontSize=52;
            Place(depart.GetComponentInChildren<TMP_Text>().rectTransform,28,0,290,80);
            Crop("DepartureChevron",depart.transform,322,23,26,34,845,1408,22,37);
            var navigation=LobbyNavigationRail.Create(root,frameSkin,atlas,0,"Navigation",index=>{if(index==0)CloseDialog();else OpenPage((LobbyPage)(index-1));});
            // The sibling rail owns this white-icon material for the same screen lifetime.
            depart.transform.Find("DepartureChevron").GetComponent<RawImage>().material=navigation.Find("Navigation0/Icon").GetComponent<RawImage>().material;
        }
        readonly LandscapeSectionLayout lobbyLandscape=new LandscapeSectionLayout();
        void ReflowLobby()
        {
            int empty=0;
            lobbyLandscape.Apply(layout,(name,r)=>
            {
                if(name=="CommanderMedallion")return new Rect(39,14,112,112);
                if(name=="CommanderPortrait")return new Rect(51,26,88,88);
                if(name=="ProfileNamePlate")return new Rect(147,30,155,58);
                if(name=="PlayerProfileName")return new Rect(159,35,136,46);
                if(name=="CommanderExperience")return new Rect(170,89,250,26);
                if(name=="Profile")return new Rect(26,18,450,100);
                if(name=="LobbyMenu")return new Rect(1488,26,83,70);
                if(name.StartsWith("Currency")&&int.TryParse(name.Substring(8),out var c))return new Rect(475+c*280,24,245,58);
                if(name=="AccountStateNotice")return new Rect(470,73,850,28);
                if(name.StartsWith("Shortcut")&&int.TryParse(name.Substring(8),out var t))return new Rect(1430,121+t*81,152,70);
                if(name==LobbyNavigationRail.ObjectName)return r;
                if(name=="SquadPanel")return new Rect(138,582,866,264);
                if(name=="SquadTitle")return new Rect(175,594,360,38);
                if(name=="Formation")return new Rect(817,592,170,44);
                if(name.StartsWith("SquadPortrait"))return new Rect(176+int.Parse(name.Substring(13))*166,663,146,148);
                if(name.StartsWith("SquadSlot"))return new Rect(169+int.Parse(name.Substring(9))*166,656,160,162);
                if(name=="EmptySlot")return new Rect(176+empty++*166,663,146,148);
                if(name=="OperationPanel")return new Rect(1017,582,567,264);
                if(name=="OperationLandscape")return new Rect(1032,595,536,100);
                if(name=="NextOperationCaption")return new Rect(1046,595,450,36);
                if(name=="NextOperation")return new Rect(1046,640,450,54);
                if(name=="Departure")return new Rect(1104,714,427,94);
                if(name.StartsWith("Navigation")&&int.TryParse(name.Substring(10),out var n))return new Rect(30+n*309,800,303,84);
                return r;
            },false);
            var depart=layout.Find("Departure");
            Place((RectTransform)depart.Find("Label"),20,0,350,94);
            Place((RectTransform)depart.Find("DepartureChevron"),365,29,30,36);
        }
        public void SetReducedMotion(bool value){reduced=value;Motion.ReducedMotion=value;Motion.Evaluate(0);}
        void OpenMenu()
        {
            ShowInfo("메뉴","");
            Button("OpenOperatorCodex",dialog,64,135,620,66,"대원 도감",OpenOperatorCodex);
            Button("OpenStoryArchive",dialog,64,215,620,66,"이야기 기록",OpenStoryArchive);
            var settingsButton=Button("OpenSettings",dialog,64,295,620,66,"설정",OpenSettings); settingsButton.interactable=settingsModel!=null;
            Button("MotionSetting",dialog,64,375,620,66,reduced?"배경 움직임 켜기":"배경 움직임 줄이기",()=>{SetReducedMotion(!reduced);OpenMenu();});
            Button("AccountSettings",dialog,64,455,620,66,"계정 관리",()=>{CloseDialog();account?.Invoke();});
            Button("OpenSupport",dialog,64,535,620,66,"고객문의",()=>{CloseDialog();support?.Invoke();});
        }
        void ShowInfo(string title,string body,Action closeAction=null)
        {
            CloseDialog();
            var shield=Rect("LobbyDialogShield",FixedUi);Stretch(shield);
            var blocker=shield.gameObject.AddComponent<Image>();blocker.color=new Color(0,0,0,.80f);blocker.raycastTarget=true;
            dialog=Panel("LobbyDialog",shield,0,0,750,760);dialog.anchorMin=dialog.anchorMax=dialog.pivot=new Vector2(.5f,.5f);dialog.anchoredPosition=Vector2.zero;
            Label("DialogTitle",dialog,title,48,45,650,70,39,TextAlignmentOptions.MidlineLeft);
            Label("DialogBody",dialog,body,48,145,650,430,28,TextAlignmentOptions.TopLeft);
            var ownedDialog=dialog;
            void RequestClose(){if(dialog==ownedDialog&&IsDialogOpen)(closeAction??CloseDialog)();}
            dialogBack=RequestClose;
            var close=Button("CloseLobbyDialog",dialog,60,645,630,70,"닫기",RequestClose);
            dialogInput=new AugmentSelection.AugmentModalInputScope(shield,FixedUi,close.gameObject);
            EvaluateLayout();
        }
        public void OpenStoryArchive()=>OpenResourceDialog(new[]{"EntryUi/secondary","NoncombatArchive/V02/"},OpenStoryArchivePrepared);
        void OpenStoryArchivePrepared()
        {
            ShowInfo("이야기 기록","");
            foreach(Transform child in dialog)child.gameObject.SetActive(false);
            dialog.sizeDelta=new Vector2(941,1672);dialog.parent.GetComponent<Image>().color=new Color(.015f,.018f,.02f,1);
            storyArchiveView=StoryArchiveView.Create(dialog,font,frameSkin,accountState?.StoryArchive,CloseDialog);
            EvaluateLayout();
        }
        public void OpenOperatorCodex()=>OpenResourceDialog(new[]{"NoncombatArchive/room","NoncombatCodex/kyle-alpha-baseline-v05","NoncombatCodex/codex-dark-steel-surface-v01","EntryPopup/popup-backplate"},OpenOperatorCodexPrepared);
        void OpenOperatorCodexPrepared()
        {
            ShowInfo("대원 도감","");
            foreach(Transform child in dialog)child.gameObject.SetActive(false);
            dialog.sizeDelta=new Vector2(941,1672);dialog.parent.GetComponent<Image>().color=new Color(.015f,.018f,.02f,1);
            operatorCodexView=OperatorCodexView.Create(dialog,font,frameSkin,CloseDialog);EvaluateLayout();
        }
        public void OpenSettings()=>OpenResourceDialog(new[]{"EntryUi/secondary","NoncombatSettings/","NoncombatCodex/codex-dark-steel-surface-v01"},OpenSettingsPrepared);
        void OpenSettingsPrepared()
        {
            if(settingsModel==null)return;
            ShowInfo("설정","");
            foreach(Transform child in dialog)child.gameObject.SetActive(false);
            dialog.sizeDelta=new Vector2(941,1672);dialog.parent.GetComponent<Image>().color=new Color(.015f,.018f,.02f,1);
            settingsView=SettingsScreenView.Create(dialog,font,frameSkin,settingsModel,accountState,OpenMenu,account);
            EvaluateLayout();
        }
        public void CloseDialog()
        {
            homeController?.CancelPopupRequest();ReleaseBulkBinding();ReleaseOfflineRouletteBinding();ReleaseLifecycleBinding();CloseCombatAdView();
            CancelResourceDialog();
            CancelDepartureExpiry();
            dialogBack=null;dialogInput?.Dispose();dialogInput=null;
            seasonPassView?.Unbind();seasonPassView=null;selectedPassPolicy=null;
            offlineSupplyView?.Unbind();offlineSupplyView=null;
            monthlyView?.Unbind();monthlyView=null;monthlyConfirmationOwner=null;
            CloseVictoryPresentation(false);resultLandscape=null;
            settingsView?.Unbind();settingsView=null;
            CloseDepartureGuide();
            storyArchiveView?.Unbind();storyArchiveView=null;
            operatorCodexView?.Unbind();operatorCodexView=null;
            departureReadGeneration++;preparationView=null;departureSupportOwner=null;departureSupportIds=Array.Empty<string>();departureSupportNotice="선택 확인 필요";
            operationListButtons.Clear();operationsDialog=false;
            lifecycleDialog=false;bulkDialog=false;monthlyDialog=false;passDialog=false;offlineDialog=false;departureDialog=false;departureRecoveryDialog=false;startLookupDialog=false;if(dialog==null)return;
            var shield=dialog.parent.gameObject;dialog=null;shield.SetActive(false);
            if(Application.isPlaying)Destroy(shield);else DestroyImmediate(shield);
        }
        bool applyingLayout;
        void OnRectTransformDimensionsChange(){if(isActiveAndEnabled)EvaluateLayout();}
        public void EvaluateLayout()
        {
            if(applyingLayout || FixedUi==null || layout==null)return;
            applyingLayout=true;
            try {
            var area=FixedUi.rect;
            float fit=Mathf.Min(area.width/1600f,area.height/900f);
            layout.localScale=Vector3.one*fit;
            ReflowLobby();
            if(dialog!=null)
            {
                if(preparationView!=null){dialog.sizeDelta=new Vector2(1600,900);preparationView.SetViewportHeight(900);}
                else if(monthlyView!=null){dialog.sizeDelta=new Vector2(1600,900);monthlyView.SetViewportHeight(900);}
                else if(seasonPassView!=null){dialog.sizeDelta=new Vector2(1600,900);seasonPassView.SetViewportHeight(900);}
                else if(storyArchiveView!=null){dialog.sizeDelta=new Vector2(1600,900);storyArchiveView.SetViewportHeight(900);}
                else if(operatorCodexView!=null){dialog.sizeDelta=new Vector2(1600,900);operatorCodexView.SetViewportHeight(900);}
                else if(victoryView!=null){dialog.sizeDelta=new Vector2(1600,900);victoryView.SetViewportHeight(900);}
                else if(settingsView!=null){dialog.sizeDelta=new Vector2(1600,900);settingsView.SetViewportHeight(900);}
                else if(lifecycleDialog){SetResultViewportHeight(900);}
                else if(offlineSupplyView!=null){dialog.sizeDelta=new Vector2(1600,900);offlineSupplyView.SetLandscapeLayout();}
                dialog.localScale=Vector3.one*Mathf.Max(.01f,Mathf.Min((area.width-24)/dialog.sizeDelta.x,(area.height-24)/dialog.sizeDelta.y));
            }
            if(Motion!=null)Motion.Evaluate(0);
            } finally { applyingLayout=false; }
        }
        async void OnApplicationFocus(bool focused){if(focused){backKey.Synchronize();EvaluateLayout();RefreshDepartureClock();}var owner=accountState;if(owner==null)return;owner.RuntimeAds.Invalidate();if(focused)await owner.RefreshAdSkipAsync();}
        void OnApplicationPause(bool paused){if(!paused&&isActiveAndEnabled){EvaluateLayout();RefreshDepartureClock();}}
        void OnDisable(){homeController?.Dispose();CancelResourceDialog();CloseDialog();CancelDepartureExpiry();dialogInput?.Dispose();dialogInput=null;dialogBack=null;operationProgress?.Dispose();operationProgress=null;if(accountState!=null)accountState.Changed-=AccountChanged;seasonPassView?.Unbind();offlineSupplyView?.Unbind();monthlyView?.Unbind();victoryView?.Unbind();settingsView?.Unbind();pageReturn=null;operatorCodexView?.Unbind();storyArchiveView?.Unbind();CloseDepartureGuide();Pages?.Close();if(localDeparture!=null)localDeparture.Changed-=UpdateDepartureDialog;if(dialog!=null)dialog.parent.gameObject.SetActive(false);if(Motion!=null)Motion.gameObject.SetActive(false);}
        void OnEnable(){backKey.Synchronize();CloseDialog();BindOperationProgress();if(accountState!=null){accountState.Changed-=AccountChanged;accountState.Changed+=AccountChanged;AccountChanged();}if(localDeparture!=null){localDeparture.Changed-=UpdateDepartureDialog;localDeparture.Changed+=UpdateDepartureDialog;}EvaluateLayout();if(Motion!=null)Motion.gameObject.SetActive(true);}
        // Environment belongs to the same screen hierarchy and is destroyed by that owner.
        void OnDestroy(){homeController?.Dispose();CancelResourceDialog();operationProgress?.Dispose();operationProgress=null;if(accountState!=null)accountState.Changed-=AccountChanged;if(localDeparture!=null)localDeparture.Changed-=UpdateDepartureDialog;if(frameSkin!=null){if(Application.isPlaying)Destroy(frameSkin);else DestroyImmediate(frameSkin);}}
        static RectTransform Rect(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);return (RectTransform)g.transform;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;r.anchoredPosition=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=Rect(name,parent);Place(r,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.035f,.044f,.049f,1f);image.raycastTarget=false;
            var outline=r.gameObject.AddComponent<Outline>();outline.enabled=false;
            var skin=Rect("MetalFrame",r);Stretch(skin);var border=skin.gameObject.AddComponent<Image>();border.sprite=ControlSkin(name);border.type=Image.Type.Sliced;border.fillCenter=false;border.raycastTarget=false;
            border.pixelsPerUnitMultiplier=1;
            return r;
        }
        Sprite ControlSkin(string name) => frameSkin;
        Button Button(string name,Transform parent,float x,float y,float w,float h,string text,Action action,bool clear=false)
        {
            var r=Panel(name,parent,x,y,w,h);var image=r.GetComponent<Image>();image.raycastTarget=true;
            if(clear){image.color=Color.clear;r.GetComponent<Outline>().enabled=false;r.Find("MetalFrame").gameObject.SetActive(false);}
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>{if(this!=null&&isActiveAndEnabled&&button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())action?.Invoke();});
            if(text!=null)Label("Label",r,text,0,0,w,h,26,TextAlignmentOptions.Center);
            return button;
        }
        TMP_Text Label(string name,Transform parent,string text,float x,float y,float w,float h,float size,TextAlignmentOptions alignment)
        {
            var r=Rect(name,parent);Place(r,x,y,w,h);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=layout!=null&&(parent==layout||parent.IsChildOf(layout))?lobbyFont:font;label.text=text;label.richText=false;label.raycastTarget=false;
            label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=size*.65f;label.fontSizeMax=size;label.color=new Color(.91f,.90f,.84f);label.alignment=alignment;
            return label;
        }
        RawImage Crop(string name,Transform parent,float x,float y,float w,float h,float sx,float sy,float sw,float sh)
        {
            var r=Rect(name,parent);Place(r,x,y,w,h);var image=r.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            if(name.StartsWith("SquadPortrait")){image.color=new Color(.12f,.15f,.18f);return image;}
            if(name=="OperationLandscape"){
                image.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/cabin-landscape-v01");
                image.uvRect=new Rect(.2f,.68f,.62f,.2f);return image;
            }
            int index=0;
            if(name=="CurrencyIcon")index=1+int.Parse(parent.name.Substring(8));
            else if(name=="MenuIcon")index=4;
            else if(name=="ShortcutIcon")index=5+int.Parse(parent.name.Substring(8));
            else if(name=="FormationIcon")index=9;
            else if(name=="DepartureChevron")index=14;
            else if(name=="Icon")index=new[]{10,9,11,12,13}[int.Parse(parent.name.Substring(10))];
            image.texture=atlas;image.uvRect=LobbyArtwork.IconUv(index);return image;
        }
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
        // Explicit synthetic visual fixtures for the marked LOCAL QA recording, never player data.
        public void SetEditorVisualFixture()
        {
            visualFixture=true;level.text="Lv.12";string[] values={"80 / 100","24,800","1,250"};for(int i=0;i<3;i++)balances[i].text=values[i];
            foreach(var p in portraits)p.color=Color.white;foreach(var e in emptyLabels)e.SetActive(false);
        }
#endif
    }
}




