using System;
using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle
{
    public enum CombatPauseIntent { Menu, Lobby, Abandon }
    // Independent central popup visuals and input intents only; no simulation or scene ownership.
    public sealed class CombatPauseView : MonoBehaviour
    {
        Action resume,close,settings,lobby,abandon;Button continueButton,settingsButton,lobbyButton,abandonButton;TMP_Text notice,title;
        public static CombatPauseView Create(Transform parent,TMP_FontAsset font)
        {
            var overlay=AuthFormVisuals.Rect("CombatPauseOverlay",parent);var view=overlay.gameObject.AddComponent<CombatPauseView>();
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.4f);shade.raycastTarget=true;
            var panel=AuthFormVisuals.Rect("PausePanel",overlay);panel.anchorMin=panel.anchorMax=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(450,600);panel.anchoredPosition=new Vector2(0,12);
            var surface=IndependentMetalSurface.Create(panel,false,new Color(.52f,.54f,.53f));surface.Frame.BorderScale=.44f;surface.Backplate.Inset=new Vector2(17,17);surface.SetLightColor(new Color(.1f,.83f,1));
            surface.Frame.color=new Color(.58f,.49f,.37f);surface.AlignPopupLights(false);
            var emblem=Rect("PauseEmblem",panel,183,-31,84,84).gameObject.AddComponent<PauseEmblemGraphic>();emblem.raycastTarget=false;
            // Borrow the already filtered steel; face, bevel, rim and pause bars remain separate display roles.
            var face=AuthFormVisuals.Rect("PauseMetalFace",emblem.transform).gameObject.AddComponent<LastRail.Presentation.MissionShell.RouletteSurfaceGraphic>();face.Radius=.37f;face.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01");face.color=new Color(.35f,.43f,.44f);face.EdgeBrightness=.65f;face.raycastTarget=false;
            var bevel=AuthFormVisuals.Rect("PauseBevel",emblem.transform).gameObject.AddComponent<LastRail.Presentation.MissionShell.RouletteSurfaceGraphic>();bevel.Radius=.39f;bevel.InnerRadius=.37f;bevel.color=new Color(.58f,.53f,.40f);bevel.EdgeBrightness=.55f;bevel.raycastTarget=false;
            var bars=AuthFormVisuals.Rect("PauseBars",emblem.transform);foreach(float x in new[]{-.23f,.10f}){var ink=Rect("PauseBar",bars,42+x*42,42-.39f*42,.13f*42,.78f*42).gameObject.AddComponent<Image>();ink.color=new Color(.12f,.9f,1);ink.raycastTarget=false;}
            view.title=Text("PauseTitle",panel,"일시정지",font,52,66,346,65,40);
            var ornaments=Rect("PauseTitleOrnaments",panel,0,98,450,2);foreach(float x in new[]{70f,325f}){var ink=Rect("SideLine",ornaments,x,0,55,2).gameObject.AddComponent<Image>();ink.color=new Color(.48f,.38f,.24f);ink.raycastTarget=false;}
            var line=Rect("TitleRule",panel,58,148,334,2).gameObject.AddComponent<Image>();line.color=new Color(.48f,.38f,.24f);line.raycastTarget=false;
            var diamond=Rect("PauseTitleDiamond",panel,225,149,6.36f,6.36f);diamond.pivot=new Vector2(.5f,.5f);diamond.localRotation=Quaternion.Euler(0,0,45);var diamondInk=diamond.gameObject.AddComponent<Image>();diamondInk.color=new Color(.65f,.53f,.33f);diamondInk.raycastTarget=false;
            Button Make(string name,string label,float y,bool primary,Action action)
            {var b=AuthFormVisuals.MetalButton(panel,name,label,font,primary,()=>{if(view.isActiveAndEnabled)action();});AuthFormVisuals.Place((RectTransform)b.transform,47,y,356,65);var text=b.GetComponentInChildren<TMP_Text>();text.rectTransform.offsetMin=new Vector2(42,8);text.rectTransform.offsetMax=new Vector2(-42,-8);text.enableAutoSizing=true;text.fontSizeMin=26;text.fontSizeMax=32;text.textWrappingMode=TextWrappingModes.NoWrap;return b;}
            view.continueButton=Make("PauseContinue","계속하기",166,true,()=>view.resume?.Invoke());
            view.settingsButton=Make("PauseSettings","설정",244,false,()=>view.settings?.Invoke());
            view.lobbyButton=Make("PauseLobby","로비로",322,false,()=>view.lobby?.Invoke());
            view.abandonButton=Make("PauseAbandon","작전 포기",400,false,()=>view.abandon?.Invoke());
            view.notice=Text("PauseNotice",panel,"",font,46,477,358,65,14);
            Text("SettlementNotice",panel,"전투 종료 및 보상 처리는 별도로 확인합니다",font,35,546,380,21,12);
            var closeRect=Rect("PauseClose",panel,403,26,28,28);var hit=closeRect.gameObject.AddComponent<Image>();hit.color=new Color(.06f,.07f,.07f);var closeButton=closeRect.gameObject.AddComponent<Button>();closeButton.targetGraphic=hit;closeButton.onClick.AddListener(()=>{if(view.isActiveAndEnabled)view.close?.Invoke();});Text("Label",closeRect,"×",font,0,0,28,28,22);
            return view;
        }
        public void Bind(Action resume,Action close,Action settings,Action lobby,Action abandon=null){this.resume=resume;this.close=close;this.settings=settings;this.lobby=lobby;this.abandon=abandon;}
        public void Unbind(){resume=null;close=null;settings=null;lobby=null;abandon=null;}
        public void Render(bool recovery,bool hasSettings,bool hasLobby,bool hasAbandon,CombatPauseIntent intent,string error)
        {
            bool menu=intent==CombatPauseIntent.Menu;
            title.text=menu?"일시정지":intent==CombatPauseIntent.Lobby?"로비로 이동":"작전 포기";
            settingsButton.gameObject.SetActive(menu);lobbyButton.gameObject.SetActive(menu||intent==CombatPauseIntent.Lobby);abandonButton.gameObject.SetActive(menu||intent==CombatPauseIntent.Abandon);
            continueButton.GetComponentInChildren<TMP_Text>().text=menu?"계속하기":"계속 전투";
            continueButton.interactable=!recovery;settingsButton.interactable=!recovery&&hasSettings;lobbyButton.interactable=!recovery&&hasLobby;abandonButton.interactable=!recovery&&hasAbandon;
            foreach(var b in new[]{continueButton,settingsButton,lobbyButton,abandonButton})b.GetComponentInChildren<TMP_Text>().color=b.interactable?Color.white:new Color(.54f,.57f,.57f);
            string message=intent==CombatPauseIntent.Lobby?"전투에서 나가시겠어요? 다시 입장하면 이 전투를 시작 당시 상태로 처음부터 진행합니다. 이전에 확정한 작전 진행은 유지됩니다.":intent==CombatPauseIntent.Abandon?"현재 작전을 종료하시겠어요?":!hasAbandon?"계속하기는 현재 전투를 재개합니다.\n현재 작전 포기를 이용할 수 없습니다.":"계속하기는 현재 전투를 재개합니다.";
            notice.text=error??(recovery?"전투 결과 또는 기록을 확인 중입니다.":message);
            AuthFormVisuals.Place(notice.rectTransform,46,menu?477:235,358,menu?65:82);
        }
        void OnDisable()=>Unbind();void OnDestroy()=>Unbind();
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=AuthFormVisuals.Rect(name,parent);AuthFormVisuals.Place(r,x,y,w,h);return r;}
        static TMP_Text Text(string name,Transform parent,string value,TMP_FontAsset font,float x,float y,float w,float h,float size){var t=AuthFormVisuals.Text(parent,name,value,font,size,false,TextAlignmentOptions.Center);AuthFormVisuals.Place(t.rectTransform,x,y,w,h);t.enableAutoSizing=true;t.fontSizeMin=size*.8f;t.fontSizeMax=size;return t;}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PauseEmblemGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;var center=r.center;float radius=Mathf.Min(r.width,r.height)/2;
            for(int i=0;i<64;i++){float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var q=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int n=mesh.currentVertCount;
                mesh.AddVert(center+p*radius,new Color(.48f,.4f,.27f),Vector2.zero);mesh.AddVert(center+q*radius,new Color(.48f,.4f,.27f),Vector2.zero);mesh.AddVert(center+q*radius*.78f,new Color(.16f,.22f,.23f),Vector2.zero);mesh.AddVert(center+p*radius*.78f,new Color(.16f,.22f,.23f),Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
