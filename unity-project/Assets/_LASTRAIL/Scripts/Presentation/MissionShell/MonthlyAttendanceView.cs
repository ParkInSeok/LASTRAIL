using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Presentation.MissionShell
{
    // Owns display objects and callback registrations; the deck retains attendance commands.
    public sealed class MonthlyAttendanceView : MonoBehaviour
    {
        public const float MaximumViewportHeight=1368;
        public MonthlyAttendanceDisplayState CurrentState { get; private set; }
        Action close,retry,recover;bool bound;TMP_FontAsset font;
        TMP_Text count,cycle,notice,recovery,retryText,unavailable;Button retryButton,recoverButton;
        readonly GameObject[] cells=new GameObject[28],checks=new GameObject[28];
        readonly TMP_Text[] rewards=new TMP_Text[28];readonly RewardResourceSymbol[] symbols=new RewardResourceSymbol[28];
        float viewportHeight=-1;RectTransform statusCheck;IndependentMetalSurface statusSurface;readonly IndependentMetalSurface[] cellSurfaces=new IndependentMetalSurface[28];
        public static MonthlyAttendanceView Create(Transform parent,TMP_FontAsset font)
        {var root=AuthFormVisuals.Rect("MonthlyAttendance",parent);var view=root.gameObject.AddComponent<MonthlyAttendanceView>();view.font=font;view.Build();return view;}
        public void Bind(Action close,Action retry,Action recover){this.close=close;this.retry=retry;this.recover=recover;bound=true;}
        public void Unbind(){bound=false;close=retry=recover=null;}
        readonly LandscapeSectionLayout landscape=new LandscapeSectionLayout();
        public void SetViewportHeight(float height)
        {
            landscape.Apply(transform,(name,r)=>
            {
                if(name.StartsWith("MonthlyCell")&&int.TryParse(name.Substring(11),out var day))return new Rect(690+(day-1)%7*119,190+(day-1)/7*135,113,128);
                if(name=="MonthlyTitle")return new Rect(35,35,650,80);
                if(name.StartsWith("MonthlyTitleLine"))return new Rect(r.x,74,60,2.5f);
                if(name=="CloseMonthlyX")return new Rect(1490,26,76,78);
                if(name=="MonthlyBanner")return new Rect(35,160,600,150);
                if(name=="MonthlyCountPanel")return new Rect(45,330,310,139);
                if(name=="MonthlyCycle")return new Rect(690,115,830,47);
                if(name=="MonthlyUnavailable")return new Rect(740,350,740,132);
                if(name=="MonthlyStatusPanel")return new Rect(35,490,600,90);
                if(name=="MonthlyRecoveryPanel")return new Rect(35,600,600,119);
                if(name=="RefreshMonthly")return new Rect(48,760,205,68);
                if(name=="CloseMonthly")return new Rect(1020,760,421,104);
                return r;
            });
        }
        void OnDisable()=>Unbind();
        void OnDestroy()=>Unbind();
        void Build()
        {
            var outer=IndependentMetalSurface.Create((RectTransform)transform,false,new Color(.40f,.43f,.45f));outer.Lights.gameObject.SetActive(false);outer.Backplate.Inset=new Vector2(18,18);outer.Frame.BorderScale=.55f;outer.Frame.color=new Color(.60f,.60f,.58f);
            Text("MonthlyTitle",transform,"월간 출석",120,33,650,80,65,TextAlignmentOptions.Center);
            foreach(var x in new[]{177f,663f}){var line=Rect("MonthlyTitleLine"+(int)x,transform,x,74,60,2.5f).gameObject.AddComponent<Image>();line.color=new Color(.65f,.58f,.42f);line.raycastTarget=false;}
            Control("CloseMonthlyX",transform,"×",798,26,76,78,()=>close?.Invoke(),32);
            var banner=Rect("MonthlyBanner",transform,35,135,830,207).gameObject.AddComponent<RawImage>();banner.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatAttendance/banner-v01");banner.enabled=banner.texture!=null;banner.raycastTarget=false;
            banner.uvRect=new Rect(0,.12f,1,(2172f*207/830)/724);
            var countPlate=Plate("MonthlyCountPanel",transform,45,190,310,139);
            Text("CountHeading",countPlate,"이번 달 출석",15,8,280,44,31,TextAlignmentOptions.Center);
            count=Text("MonthlyCount",countPlate,"",12,54,286,65,46,TextAlignmentOptions.Center);count.color=new Color(.28f,.88f,1);
            cycle=Text("MonthlyCycle",transform,"",376,269,460,47,27,TextAlignmentOptions.Right);
            for(int i=0;i<28;i++)
            {
                var cell=Plate("MonthlyCell"+(i+1),transform,37+(i%7)*119,361+(i/7)*135,113,128);cells[i]=cell.gameObject;cellSurfaces[i]=cell.GetComponentInChildren<IndependentMetalSurface>();cellSurfaces[i].Frame.BorderScale=.19f;
                Text("Day",cell,(i+1).ToString(),10,5,50,29,25,TextAlignmentOptions.Left);
                symbols[i]=Rect("RewardSymbol",cell,28,32,58,53).gameObject.AddComponent<RewardResourceSymbol>();symbols[i].raycastTarget=false;
                rewards[i]=Text("Reward",cell,"",6,92,101,32,16,TextAlignmentOptions.Center);rewards[i].fontSizeMin=14;
                foreach(var boundary in new[]{"DaySeparator","RewardSeparator"}){var line=Rect(boundary,cell,6,boundary=="DaySeparator"?34:90,101,1.5f).gameObject.AddComponent<Image>();line.color=new Color(.22f,.29f,.31f);line.raycastTarget=false;}
                var check=Rect("Received",cell,79,9,22,22).gameObject.AddComponent<RewardResourceSymbol>();check.Kind="CHECK";check.raycastTarget=false;checks[i]=check.gameObject;
            }
            unavailable=Text("MonthlyUnavailable",transform,"출석 기록을 확인해 주세요.",80,535,740,132,35,TextAlignmentOptions.Center);
            var status=Plate("MonthlyStatusPanel",transform,37,910,827,90);statusSurface=status.GetComponentInChildren<IndependentMetalSurface>();
            notice=Text("MonthlyNotice",status,"",20,7,787,76,25,TextAlignmentOptions.Center);statusCheck=Rect("MonthlyStatusCheck",status,20,27,36,36);var glyph=statusCheck.gameObject.AddComponent<LastRail.Presentation.Settings.SettingsSymbolGraphic>();glyph.Kind=LastRail.Presentation.Settings.SettingsSymbolGraphic.Symbol.Check;glyph.color=new Color(.18f,.8f,.87f);glyph.raycastTarget=false;
            var recoveryPlate=Plate("MonthlyRecoveryPanel",transform,37,1017,827,119);recovery=Text("MonthlyRecoveryCount",recoveryPlate,"",15,30,312,56,30,TextAlignmentOptions.Center);
            recoverButton=Control("RecoverMonthly",recoveryPlate,"출석 복구  ·  100 크리스탈",336,22,473,76,()=>recover?.Invoke(),27);
            retryButton=Control("RefreshMonthly",transform,"다시 확인",48,1157,205,68,()=>retry?.Invoke(),26);retryText=retryButton.GetComponentInChildren<TMP_Text>();
            Button confirm=null;confirm=AuthFormVisuals.MetalButton(transform,"CloseMonthly","확인",font,true,()=>Dispatch(confirm,close));AuthFormVisuals.Place((RectTransform)confirm.transform,276,1149,421,104);
        }
        public void Render(MonthlyAttendanceDisplayState state)
        {
            CurrentState=state;count.text=state.CountText;cycle.text=state.CycleText;notice.text=state.Notice;recovery.text=state.RecoveryText;
            statusSurface.Backplate.color=state.TodayAttendanceConfirmed?new Color(.18f,.50f,.56f):new Color(.40f,.43f,.45f);statusCheck.gameObject.SetActive(state.TodayAttendanceConfirmed);if(state.TodayAttendanceConfirmed){float textWidth=Mathf.Min(720,notice.GetPreferredValues().x+8);float start=(827-36-16-textWidth)/2;AuthFormVisuals.Place(statusCheck,start,27,36,36);AuthFormVisuals.Place(notice.rectTransform,start+52,7,textWidth,76);}else AuthFormVisuals.Place(notice.rectTransform,20,7,787,76);
            retryText.text=state.RetryText;retryButton.interactable=state.CanRetry;recoverButton.interactable=state.CanRecover;unavailable.gameObject.SetActive(!state.HasCurrentCalendar);
            for(int i=0;i<28;i++)
            {bool visible=i<state.Cells.Count;cells[i].SetActive(visible);if(!visible)continue;var cell=state.Cells[i];rewards[i].text=cell.Reward;checks[i].SetActive(cell.Received);symbols[i].Kind=cell.Symbol;symbols[i].SetVerticesDirty();cellSurfaces[i].Backplate.color=cell.Received?new Color(.20f,.38f,.43f):new Color(.40f,.43f,.45f);}
            foreach(var button in GetComponentsInChildren<Button>())foreach(var label in button.GetComponentsInChildren<TMP_Text>())label.color=button.interactable?new Color(.93f,.93f,.88f):new Color(.48f,.53f,.55f);
        }
        void Dispatch(Button button,Action action){if(bound&&isActiveAndEnabled&&button.interactable&&button.gameObject.activeInHierarchy)action?.Invoke();}
        static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height){var rect=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(rect,x,y,width,height);return rect;}
        RectTransform Plate(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=Rect(name,parent,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.025f,.036f,.042f);image.raycastTarget=false;
            var surface=IndependentMetalSurface.Create(rect,true,new Color(.40f,.43f,.45f));surface.Lights.gameObject.SetActive(false);surface.Backplate.Inset=new Vector2(8,8);surface.Backplate.Bevel=8;surface.Frame.BorderScale=Mathf.Min(.38f,w/500);surface.Frame.color=new Color(.60f,.60f,.58f);return rect;
        }
        Button Control(string name,Transform parent,string label,float x,float y,float w,float h,Action action,float size)
        {var rect=Plate(name,parent,x,y,w,h);var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Text("Label",rect,label,8,6,w-16,h-12,size,TextAlignmentOptions.Center);button.onClick.AddListener(()=>Dispatch(button,action));return button;}
        TMP_Text Text(string name,Transform parent,string value,float x,float y,float w,float h,float size,TextAlignmentOptions alignment)
        {var text=AuthFormVisuals.Text(parent,name,value,font,size,false,alignment);AuthFormVisuals.Place(text.rectTransform,x,y,w,h);text.enableAutoSizing=true;text.fontSizeMax=size;text.fontSizeMin=size*.75f;return text;}
    }
}


