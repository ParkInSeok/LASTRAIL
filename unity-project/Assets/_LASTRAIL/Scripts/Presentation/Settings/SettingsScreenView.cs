using System;
using System.Collections.Generic;
using LastRail.Auth;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Settings
{
    // Owns presentation and input subscriptions; the application owns the borrowed models.
    public sealed class SettingsScreenView : MonoBehaviour, ISettingsScreenView
    {
        public event Action<int> TabRequested;
        public event Action<LocalSetting, float> ValueRequested;
        public event Action BackRequested, RefreshRequested, AccountLinkRequested;
        public SettingsScreenState CurrentState { get; private set; }
        SettingsScreenController controller;
        TMP_FontAsset font;
        Sprite frame, neutralPlate;
        static readonly Color PlateTone = new Color(.3f, .35f, .36f);
        static readonly Color SelectedTone = new Color(.16f, .62f, .85f);
        RectTransform panel, content, floor, returnButton;
        TMP_Text notice, qualityCaption, frameCaption, displayHelp, accountNotice, accountHelp;
        Button qualityChoice, displayRetry, accountRetry, accountLink;
        Button reducedChoice;
        SettingsSymbolGraphic savedCheck;
        RectTransform qualityOptions;
        readonly RectTransform[] panes = new RectTransform[4];
        readonly Image[] tabs = new Image[4];
        readonly Button[] modes = new Button[3];
        readonly GameObject[] modeDots = new GameObject[3];
        SettingsSymbolGraphic qualityArrow;
        readonly Dictionary<LocalSetting, Slider> sliders = new Dictionary<LocalSetting, Slider>();
        readonly Dictionary<LocalSetting, Button[]> volumeButtons = new Dictionary<LocalSetting, Button[]>();
        bool bound,rendering;
        TMP_InputField diagnosticsText;GameObject diagnosticsPanel;
        RectTransform settingsViewport;
        ScrollRect settingsScroll;

        public static SettingsScreenView Create(Transform parent, TMP_FontAsset font, Sprite borrowedFrame,
            LocalSettingsModel model, AccountScreenRuntime account, Action back, Action link)
        {
            var root = AuthFormVisuals.Rect("SettingsScreen", parent);
            var view = root.gameObject.AddComponent<SettingsScreenView>();
            view.font = font; view.frame = borrowedFrame; view.Build(); view.bound = true;
            view.BuildLandscapeLayout();
            view.controller = new SettingsScreenController(model, account, view, back, link);
            if(view.controller.DiagnosticsAvailable)view.BuildDiagnostics();
            return view;
        }
        public void Unbind() { bound = false; controller?.Dispose(); controller = null; if(diagnosticsText!=null)diagnosticsText.SetTextWithoutNotify("");if(diagnosticsPanel!=null)diagnosticsPanel.SetActive(false); }
        void OnDisable() { Unbind(); }
        void OnDestroy() { Unbind(); if(neutralPlate!=null){if(Application.isPlaying)Destroy(neutralPlate);else DestroyImmediate(neutralPlate);} }
        bool CanInput => bound && !rendering && isActiveAndEnabled;
        public void RequestBack()
        {
            if(!CanInput)return;
            if(diagnosticsPanel!=null&&diagnosticsPanel.activeSelf){diagnosticsText.SetTextWithoutNotify("");diagnosticsPanel.SetActive(false);return;}
            if(qualityOptions!=null&&qualityOptions.gameObject.activeSelf){qualityOptions.gameObject.SetActive(false);return;}
            BackRequested?.Invoke();
        }
        void BuildDiagnostics(){
            Control("CopyLifecycleDiagnostics",panes[3],"DEV 진단 전체 복사",81,840,375,70,()=>{if(CanInput&&controller.DiagnosticsAvailable)GUIUtility.systemCopyBuffer=controller.ExportDiagnostics();});
            Control("ExportLifecycleDiagnostics",panes[3],"DEV 진단 텍스트",486,840,375,70,()=>{if(!CanInput||!controller.DiagnosticsAvailable)return;diagnosticsText.SetTextWithoutNotify(controller.ExportDiagnostics());diagnosticsPanel.SetActive(true);diagnosticsPanel.transform.SetAsLastSibling();});
            var panel=Plate("LifecycleDiagnosticsText",transform,27,387,887,1100);diagnosticsPanel=panel.gameObject;
            var viewport=Rect("TextViewport",panel,25,100,837,960);viewport.gameObject.AddComponent<RectMask2D>();
            var label=Text("DiagnosticText",viewport,"",0,0,837,960,20);label.alignment=TextAlignmentOptions.TopLeft;label.textWrappingMode=TextWrappingModes.Normal;
            diagnosticsText=viewport.gameObject.AddComponent<TMP_InputField>();diagnosticsText.textViewport=viewport;diagnosticsText.textComponent=label;diagnosticsText.readOnly=true;diagnosticsText.lineType=TMP_InputField.LineType.MultiLineNewline;
            Control("CloseLifecycleDiagnostics",panel,"닫기",25,20,240,65,()=>{diagnosticsText.SetTextWithoutNotify("");diagnosticsPanel.SetActive(false);});
            Control("SelectLifecycleDiagnostics",panel,"전체 선택",570,20,290,65,()=>{diagnosticsText.ActivateInputField();diagnosticsText.Select();diagnosticsText.selectionStringAnchorPosition=0;diagnosticsText.selectionStringFocusPosition=diagnosticsText.text.Length;});
            diagnosticsPanel.SetActive(false);
        }

        void Build()
        {
            var material = LastRail.Resources.PreparedContent.Load<Texture2D>("EntryUi/secondary");
            if(material!=null)neutralPlate=Sprite.Create(material,new Rect(material.width*.15f,material.height*.2f,material.width*.74f,material.height*.5f),new Vector2(.5f,.5f));
            var texture = LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatSettings/room-v01");
            var upper = Art("SettingsRoomUpper", transform, 0, 0, 941, 360);
            upper.texture = texture; upper.uvRect = new Rect(0, 1 - 360f / 1672, 1, 360f / 1672);
            var lower = Art("SettingsRoomFloor", transform, 0, 1340, 941, 332);
            lower.texture = texture; lower.uvRect = new Rect(0, 0, 1, 332f / 1672); floor = lower.rectTransform;
            Plate("SettingsHeaderPlate",transform,29,25,305,79);
            var topBack=Control("SettingsBack", transform, "", 29, 25, 94, 79, RequestBack);
            Symbol("BackArrow",topBack.transform,SettingsSymbolGraphic.Symbol.Back,25,15,44,49);
            Text("SettingsTitle", transform, "설 정", 146, 25, 590, 84, 51);
            string[] labels = { "게임", "사운드", "화면", "계정" };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var button = Control("SettingsTab" + i, transform, labels[i], 40 + i * 216, 260, 211, 91,
                    () => TabRequested?.Invoke(index));
                tabs[i] = (Image)button.targetGraphic;
                Symbol("TabSymbol",button.transform,(SettingsSymbolGraphic.Symbol)(i+1),20,24,44,44);
                AuthFormVisuals.Place(button.GetComponentInChildren<TMP_Text>().rectTransform,68,3,133,85);
            }
            panel = Plate("SettingsMainPlate", transform, 20, 361, 901, 980);
            // Cropped Sprite padding extended beyond tall panel bounds. RawImage keeps this borrowed surface inside its explicit rectangle.
            var panelImage=panel.GetComponent<Image>();panelImage.sprite=null;panelImage.color=Color.clear;var panelMaterial=AuthFormVisuals.Rect("SettingsMainMaterial",panel).gameObject.AddComponent<RawImage>();panelMaterial.transform.SetAsFirstSibling();panelMaterial.texture=material;panelMaterial.uvRect=new Rect(.15f,.2f,.74f,.5f);panelMaterial.color=PlateTone;panelMaterial.raycastTarget=false;
            content = Rect("SettingsContent", transform, 0, 0, 941, 1672);
            for (int i = 0; i < 4; i++) panes[i] = Rect("SettingsPane" + i, content, 0, 0, 941, 1672);
            BuildGame(); BuildSound(); BuildDisplay(); BuildAccount();
            notice = Text("SettingsNotice", content, "", 85, 1260, 771, 64, 25);
            notice.alignment = TextAlignmentOptions.Center;
            Symbol("SavedCheck",content,SettingsSymbolGraphic.Symbol.Check,326,1274,36,36);savedCheck=content.Find("SavedCheck").GetComponent<SettingsSymbolGraphic>();savedCheck.color=new Color(.2f,.82f,.83f);
            returnButton = (RectTransform)Metal("SettingsReturn", transform, "돌아가기", 217, 1374, 508, 112,
                RequestBack).transform;
            Symbol("ReturnArrow",returnButton,SettingsSymbolGraphic.Symbol.Back,93,34,39,45);
        }
        void BuildGame()
        {
            var root = panes[0]; Text("GameHeading", root, "전투 연출", 86, 413, 768, 63, 43);
            string[] labels = { "전체 연출", "간소화", "첫 사용만 전체" };
            string[] descriptions = { "준비 동작과 캐릭터 강조 연출을 모두 표시합니다.", "컷인을 생략하고 발동과 타격 효과를 표시합니다.", "전투마다 첫 사용은 전체, 이후에는 간소화합니다." };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = Control("PresentationMode" + i, root, "", 81, 502 + i * 148, 780, 132,
                    () => ValueRequested?.Invoke(LocalSetting.PresentationMode, index));
                modes[i] = b;
                Circle("ModeRing",b.transform,16,50,30,2.5f,new Color(.76f,.81f,.8f));
                modeDots[i]=Circle("ModeDot",b.transform,24,58,14,0,new Color(.2f,.82f,.93f)).gameObject;
                Text("ModeName", b.transform, labels[i], 61, 12, 684, 48, 34);
                Text("ModeDescription", b.transform, descriptions[i], 61, 65, 684, 49, 25);
            }
            Text("ModeRule", root, "피해량 · 발동 시간 · 게이지 소모는 동일합니다.", 85, 953, 771, 59, 25);
            reducedChoice=Control("ReducedPresentation",root,"",81,1031,780,61,
                ()=>ValueRequested?.Invoke(LocalSetting.ReducedPresentation,CurrentState.Value(LocalSetting.ReducedPresentation)==1?0:1));
            string[] unsupported = { "화면 흔들림", "진동" };
            for (int i = 0; i < unsupported.Length; i++)
            {
                var b = Control("UnsupportedGameOption" + i, root, unsupported[i] + "     연결 확인 필요", 81, 1100 + i * 69, 780, 61, null);
                b.interactable = false;
            }
            Text("ModeApplyHelp", root, "변경한 연출 모드는 다음 발동부터 적용됩니다.", 85, 1236, 771, 33, 23);
        }
        void BuildSound()
        {
            var root = panes[1]; Symbol("SoundHeadingIcon",root,SettingsSymbolGraphic.Symbol.Sound,86,418,58,53);Text("SoundHeading", root, "사운드 설정", 166, 413, 688, 63, 43);
            LocalSetting[] keys = { LocalSetting.MasterVolume, LocalSetting.MusicVolume, LocalSetting.EffectsVolume, LocalSetting.VoiceVolume };
            string[] labels = { "전체 음량", "배경음", "효과음", "음성" };
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i]; var row = Plate(key + "Plate", root, 81, 500 + i * 174, 780, 155);
                Symbol("VolumeIcon",row,i==0?SettingsSymbolGraphic.Symbol.Sound:i==1?SettingsSymbolGraphic.Symbol.Music:i==2?SettingsSymbolGraphic.Symbol.Effects:SettingsSymbolGraphic.Symbol.Voice,29,23,53,53);Text("VolumeLabel", row, labels[i], 116, 22, 183, 47, 34);
                Text("SupportNotice", row, i == 0 ? "모든 사운드의 음량을 조절합니다." : "개별 음량 연결 확인 필요", 308, 25, 445, 42, 24);
                var minus = Control(key + "Minus", row, "−", 29, 86, 66, 54, () => Nudge(key, -.05f));
                var plus = Control(key + "Plus", row, "+", 687, 86, 66, 54, () => Nudge(key, .05f));
                var sliderRect = Rect(key + "Slider", row, 119, 80, 547, 66);
                var slider = sliderRect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
                var hit = sliderRect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                var edge=Rect("TrackBorder",sliderRect,0,24,547,18).gameObject.AddComponent<Image>();edge.color=new Color(.4f,.46f,.48f);edge.raycastTarget=false;
                var track = Rect("Track", sliderRect, 1, 26, 545, 14).gameObject.AddComponent<Image>(); track.color = new Color(.09f, .11f, .12f); track.raycastTarget = false;
                var fillArea = Rect("FillArea", sliderRect, 1, 27, 545, 12);
                var fill = AuthFormVisuals.Rect("Fill", fillArea).gameObject.AddComponent<Image>(); fill.color = new Color(.2f, .71f, .84f); fill.raycastTarget = false;
                slider.fillRect = fill.rectTransform;
                var handleArea = Rect("HandleArea", sliderRect, 0, 33, 547, 0);
                var handle = AuthFormVisuals.Rect("Handle", handleArea).gameObject.AddComponent<SettingsSymbolGraphic>();handle.Kind=SettingsSymbolGraphic.Symbol.Knob;
                handle.rectTransform.sizeDelta = new Vector2(40, 40); handle.color = new Color(.17f, .81f, .95f);
                slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
                slider.onValueChanged.AddListener(value => { if (CanInput && slider.interactable) ValueRequested?.Invoke(key, value); });
                sliders.Add(key, slider); volumeButtons.Add(key, new[] { minus, plus });
            }
            Text("VoiceHelp", root, "음성 콘텐츠가 있을 때 적용됩니다.", 85, 1212, 771, 42, 24);
        }
        void Nudge(LocalSetting key, float delta)
        {
            if (CurrentState != null && CurrentState.Supports(key))
                ValueRequested?.Invoke(key, Mathf.Clamp01(CurrentState.Value(key) + delta));
        }
        void BuildDisplay()
        {
            var root = panes[2]; Text("DisplayHeading", root, "화면 설정", 86, 413, 768, 63, 43);
            Plate("QualityPlate", root, 81, 527, 780, 180);
            Text("QualityLabel", root, "화질", 111, 560, 400, 51, 35);
            Text("QualityHelp", root, "이 기기의 실행 설정을 확인합니다.", 111, 619, 448, 60, 25);
            qualityChoice = Control("QualityChoice", root, "확인 중", 585, 580, 246, 70, ToggleQuality);
            qualityCaption = qualityChoice.GetComponentInChildren<TMP_Text>();
            Symbol("QualityArrow",qualityChoice.transform,SettingsSymbolGraphic.Symbol.Down,207,30,18,10);
            qualityArrow=qualityChoice.transform.Find("QualityArrow").GetComponent<SettingsSymbolGraphic>();
            AuthFormVisuals.Place(qualityCaption.rectTransform,10,3,189,64);
            Plate("FramePlate", root, 81, 728, 780, 180);
            Text("FrameLabel", root, "프레임", 111, 756, 400, 51, 35);
            frameCaption = Text("FrameValue", root, "확인 중", 111, 817, 715, 60, 27);
            displayHelp = Text("DisplayHelp", root, "", 100, 943, 743, 86, 27);
            var reset = Control("DisplayDefault", root, "기본 설정", 90, 1107, 357, 86, null); reset.interactable = false;
            displayRetry = Metal("DisplayRetry", root, "다시 확인", 493, 1107, 360, 86, () => RefreshRequested?.Invoke());
            qualityOptions = Plate("QualityOptions", root, 420, 658, 414, 300);
            qualityOptions.gameObject.SetActive(false);
        }
        readonly List<Button> qualityRows=new List<Button>();
        void ToggleQuality()
        {
            if (CurrentState?.Supports(LocalSetting.Quality) != true) return;
            if (qualityOptions.gameObject.activeSelf) { qualityOptions.gameObject.SetActive(false); return; }
            var names = CurrentState.Display.QualityNames;
            float row = Mathf.Min(64, 510f / Math.Max(1,names.Count)); qualityOptions.sizeDelta = new Vector2(414, row * names.Count);
            while(qualityRows.Count>names.Count){var retired=qualityRows[qualityRows.Count-1];qualityRows.RemoveAt(qualityRows.Count-1);retired.gameObject.SetActive(false);if(Application.isPlaying)Destroy(retired.gameObject);else DestroyImmediate(retired.gameObject);}
            for (int i = 0; i < names.Count; i++)
            {
                if(i==qualityRows.Count){int index=i;qualityRows.Add(Control("QualityOption"+i,qualityOptions,"",0,0,414,row,()=>{
                    if(CurrentState?.Supports(LocalSetting.Quality)!=true||index>=CurrentState.Display.QualityNames.Count)return;
                    qualityOptions.gameObject.SetActive(false);ValueRequested?.Invoke(LocalSetting.Quality,index);
                }));}
                AuthFormVisuals.Place((RectTransform)qualityRows[i].transform,0,row*i,414,row);
                qualityRows[i].GetComponentInChildren<TMP_Text>().text=names[i];
            }            qualityOptions.gameObject.SetActive(true);
        }
        void BuildAccount()
        {
            var root = panes[3];
            var medallion=Rect("AccountSymbol",root,354,462,235,213);
            var face=Circle("AccountFace",medallion,22.5f,11.5f,190,0,new Color(.18f,.22f,.24f));face.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01")??throw new InvalidOperationException("Account medallion material missing");
            Circle("AccountRim",medallion,18.5f,7.5f,198,4,new Color(.56f,.6f,.6f));
            Symbol("AccountPerson",medallion,SettingsSymbolGraphic.Symbol.Person,69.5f,53.5f,96,106);
            accountNotice = Text("AccountNotice", root, "", 85, 696, 771, 68, 36); accountNotice.alignment = TextAlignmentOptions.Center;
            accountHelp = Text("AccountHelp", root, "", 85, 765, 771, 59, 27); accountHelp.alignment = TextAlignmentOptions.Center;
            accountRetry = Metal("SettingsAccountRetry", root, "다시 확인", 215, 826, 512, 107, () => RefreshRequested?.Invoke());
            accountLink = Control("SettingsAccountLink", root, "계정 연결", 81, 985, 780, 110, () => AccountLinkRequested?.Invoke());
            var logout = Control("SettingsLogout", root, "로그아웃 · 연결 확인 필요", 81, 1114, 780, 110, null); logout.interactable = false;
        }
        public void Render(SettingsScreenState state)
        {
            rendering=true;
            try
            {
            CurrentState = state;
            reducedChoice.interactable=state.Supports(LocalSetting.ReducedPresentation);
            reducedChoice.GetComponentInChildren<TMP_Text>().text="컷신 연출 강도: "+(state.Value(LocalSetting.ReducedPresentation)==1?"줄이기":"기본");
            if (settingsScroll != null && CurrentState != null && (int)state.Tab != shownTab) { settingsScroll.StopMovement(); settingsScroll.verticalNormalizedPosition = 1; }
            shownTab = (int)state.Tab;
            for (int i = 0; i < 4; i++) { panes[i].gameObject.SetActive(i == (int)state.Tab); tabs[i].color = i == (int)state.Tab ? SelectedTone : PlateTone; }
            for (int i = 0; i < modes.Length; i++)
            { modes[i].interactable = state.Supports(LocalSetting.PresentationMode); modes[i].targetGraphic.color = state.Value(LocalSetting.PresentationMode) == i ? SelectedTone : PlateTone; modeDots[i].SetActive(state.Value(LocalSetting.PresentationMode)==i); }
            foreach (var pair in sliders)
            {
                bool available = state.Supports(pair.Key); pair.Value.interactable = available;
                pair.Value.fillRect.GetComponent<Image>().color=available?new Color(.13f,.76f,.9f):new Color(.23f,.26f,.27f);
                pair.Value.SetValueWithoutNotify(state.Value(pair.Key)); pair.Value.handleRect.gameObject.SetActive(available);
                foreach (var button in volumeButtons[pair.Key]) button.interactable = available;
            }
            bool displayReady = state.DisplayStatus == SettingsReadStatus.Ready;
            qualityChoice.interactable = state.Supports(LocalSetting.Quality);
            qualityArrow.color=qualityChoice.interactable?new Color(.76f,.81f,.8f):new Color(.45f,.49f,.5f);
            qualityCaption.text = displayReady ? state.Display.QualityNames[(int)state.Value(LocalSetting.Quality)] : state.DisplayStatus == SettingsReadStatus.Unavailable ? "확인 불가" : "확인 중";
            frameCaption.text = displayReady ? state.Display.FrameText : "이 기기에서 지원하는 설정을 확인하고 있습니다.";
            displayHelp.text = displayReady ? "현재 기기의 설정입니다. 프레임 변경과 기본 설정은 연결 확인이 필요합니다." : state.DisplayStatus == SettingsReadStatus.Unavailable ? "화면 정보를 불러오지 못했습니다. 다시 확인해 주세요." : "이 기기에서 지원하는 설정을 확인하고 있습니다.";
            displayRetry.interactable = state.DisplayStatus != SettingsReadStatus.Loading;
            if (!qualityChoice.interactable || state.Tab != SettingsTab.Display) qualityOptions.gameObject.SetActive(false);
            accountNotice.text = state.AccountNotice;
            accountHelp.text = state.AccountReady ? "계정 연결은 기존 계정 관리 화면에서 진행합니다." : "로그인 상태와 연결을 확인해 주세요.";
            accountRetry.interactable = !state.AccountLoading; accountLink.interactable = state.CanLink;
            notice.text = state.Tab == SettingsTab.Account ? "진행 중인 전투 기록은 유지됩니다." : state.Tab == SettingsTab.Display && state.DisplayStatus == SettingsReadStatus.Loading ? "설정 정보를 불러오는 중" : state.SaveNotice;
            savedCheck.gameObject.SetActive(state.SaveStatus==SettingsSaveStatus.Saved&&state.Tab!=SettingsTab.Account&&!(state.Tab==SettingsTab.Display&&state.DisplayStatus==SettingsReadStatus.Loading));
            foreach(var button in GetComponentsInChildren<Button>(true))
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
                    label.color=button.interactable?new Color(.93f,.9f,.81f):new Color(.45f,.49f,.5f);
            }
            finally{rendering=false;}
        }
        int shownTab = -1;
        void BuildLandscapeLayout()
        {
            // Navigation stays visible; only the active settings pane scrolls.
            settingsViewport = Rect("SettingsViewport", transform, 350, 120, 1200, 660);
            settingsViewport.gameObject.AddComponent<RectMask2D>();
            settingsViewport.gameObject.AddComponent<Image>().color = new Color(0,0,0,.001f);
            settingsScroll = settingsViewport.gameObject.AddComponent<ScrollRect>();
            settingsScroll.horizontal = false; settingsScroll.movementType = ScrollRect.MovementType.Clamped;
            settingsScroll.viewport = settingsViewport; settingsScroll.content = content;
            content.SetParent(settingsViewport, false);
            content.anchorMin = content.anchorMax = new Vector2(.5f,1); content.pivot = new Vector2(.5f,1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(941,950);
            foreach (var pane in panes)
            {
                pane.sizeDelta = new Vector2(941,950);
                foreach (RectTransform child in pane) child.anchoredPosition += new Vector2(0,400);
            }
            notice.transform.SetParent(transform,false); savedCheck.transform.SetParent(transform,false);
            SetViewportHeight(900);
        }
        public void SetViewportHeight(float height)
        {
            if (settingsViewport == null) return;
            float width = Mathf.Max(1200, ((RectTransform)transform).rect.width);
            AuthFormVisuals.Place(panel,350,110,width-380,height-200);
            AuthFormVisuals.Place(settingsViewport,350,120,width-380,height-220);
            AuthFormVisuals.Place(floor,0,height-100,width,100);
            AuthFormVisuals.Place((RectTransform)transform.Find("SettingsRoomUpper"),0,0,width,110);
            for(int i=0;i<tabs.Length;i++) AuthFormVisuals.Place(tabs[i].rectTransform,40,150+i*120,270,100);
            AuthFormVisuals.Place(returnButton,40,height-160,270,100);
            var arrow = returnButton.Find("ReturnArrow"); if(arrow!=null) arrow.gameObject.SetActive(false);
            AuthFormVisuals.Place(notice.rectTransform,400,height-90,width-480,64);
            AuthFormVisuals.Place(savedCheck.rectTransform,360,height-76,36,36);
            if(diagnosticsPanel!=null)
            {
                AuthFormVisuals.Place((RectTransform)diagnosticsPanel.transform,350,110,width-380,height-200);
                var viewport=(RectTransform)diagnosticsText.transform;
                AuthFormVisuals.Place(viewport,25,100,width-430,height-320);
                diagnosticsText.textComponent.rectTransform.sizeDelta=new Vector2(width-430,height-320);
            }
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        { var r = AuthFormVisuals.Rect(name, parent); AuthFormVisuals.Place(r, x, y, w, h); return r; }
        RawImage Art(string name, Transform parent, float x, float y, float w, float h)
        { var r = Rect(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>(); r.raycastTarget = false; return r; }
        void Border(Transform parent)
        { var b = AuthFormVisuals.Rect("Frame", parent).gameObject.AddComponent<Image>(); b.sprite = frame; b.type = Image.Type.Sliced; b.fillCenter = false; b.color = new Color(.82f, .71f, .53f); b.raycastTarget = false; }
        RectTransform Plate(string name, Transform parent, float x, float y, float w, float h)
        { var r = Rect(name, parent, x, y, w, h); var i = r.gameObject.AddComponent<Image>(); i.sprite=neutralPlate; i.color = PlateTone; i.raycastTarget = false; Border(r); return r; }
        TMP_Text Text(string name, Transform parent, string value, float x, float y, float w, float h, float size)
        { var t = AuthFormVisuals.Text(parent, name, value, font, size); AuthFormVisuals.Place(t.rectTransform, x, y, w, h); t.enableAutoSizing = true; t.fontSizeMin = size * .8f; t.fontSizeMax = size; t.color = new Color(.93f, .9f, .81f); return t; }
        Button Control(string name, Transform parent, string label, float x, float y, float w, float h, Action action)
        {
            var r = Plate(name, parent, x, y, w, h); var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = r.GetComponent<Image>();
            button.targetGraphic.raycastTarget=true;
            Text("Label", r, label, 10, 3, w - 20, h - 6, 31).alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => { if (CanInput && button.interactable && button.gameObject.activeInHierarchy) action?.Invoke(); }); return button;
        }
        RouletteSurfaceGraphic Circle(string name,Transform parent,float x,float y,float diameter,float stroke,Color tint)
        {var circle=Rect(name,parent,x,y,diameter,diameter).gameObject.AddComponent<RouletteSurfaceGraphic>();circle.Radius=.5f;circle.InnerRadius=stroke>0?.5f-stroke/diameter:0;circle.EdgeBrightness=1;circle.color=tint;circle.raycastTarget=false;return circle;}
        void Symbol(string name,Transform parent,SettingsSymbolGraphic.Symbol kind,float x,float y,float w,float h)
        {var symbol=Rect(name,parent,x,y,w,h).gameObject.AddComponent<SettingsSymbolGraphic>();symbol.Kind=kind;symbol.color=new Color(.76f,.81f,.8f);symbol.raycastTarget=false;}
        Button Metal(string name, Transform parent, string label, float x, float y, float w, float h, Action action)
        {
            Button button = null;
            button = AuthFormVisuals.MetalButton(parent, name, label, font, true, () => { if (CanInput && button.interactable && button.gameObject.activeInHierarchy) action?.Invoke(); });
            AuthFormVisuals.Place((RectTransform)button.transform, x, y, w, h); return button;
        }
    }
}

