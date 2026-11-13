using System;
using TMPro;
using UnityEngine;
namespace LastRail.Battle
{
    // Borrows the authoritative driver. Navigation belongs to a separately bound host.
    public sealed class CombatPauseController : MonoBehaviour
    {
        CombatSceneDriver driver;Transform canvas;TMP_FontAsset font;CombatPauseView view;
        Action settings,lobby,abandon;string error;CombatPauseIntent intent;bool submitted;
        readonly LastRail.Presentation.ScreenBackKey backKey=new LastRail.Presentation.ScreenBackKey();
        public bool IsOpen=>view!=null&&view.gameObject.activeSelf;
        public void Initialize(CombatSceneDriver owner,Transform parent,TMP_FontAsset textFont)
        {if(driver!=null&&driver!=owner)throw new InvalidOperationException("A different combat owner requires a fresh pause controller.");if(driver!=null)driver.StateChanged-=OwnerChanged;driver=owner??throw new ArgumentNullException(nameof(owner));canvas=parent;font=textFont;if(isActiveAndEnabled)driver.StateChanged+=OwnerChanged;}
        void OwnerChanged(){if(driver.IsRetired)Close();else Refresh();}
        public void BindNavigation(Action openSettings,Action returnToLobby,Action abandonOperation=null){settings=openSettings;lobby=returnToLobby;abandon=abandonOperation;intent=CombatPauseIntent.Menu;Refresh();}
        public void UnbindNavigation(){settings=null;lobby=null;abandon=null;intent=CombatPauseIntent.Menu;Refresh();}
        public void Open()
        {
            if(!isActiveAndEnabled||driver==null||submitted||driver.Outcome!=BasicCombatOutcome.Running)return;
            // Also validates ownership when a restored battle is already paused; no redundant save.
            try{driver.SetPaused(true);error=null;}catch(Exception e){error="기록을 저장하지 못했습니다. 복구가 필요합니다.";Debug.LogWarning("Pause open failed: "+e.GetType().Name);}
            if(view==null)view=CombatPauseView.Create(canvas,font);
            view.gameObject.SetActive(true);view.transform.SetAsLastSibling();
            intent=CombatPauseIntent.Menu;view.Bind(Continue,Continue,OpenSettings,ReturnToLobby,Abandon);Refresh();
        }
        public void Refresh()
        {if(IsOpen)view.Render(driver.RecoveryRequired||submitted||driver.Outcome!=BasicCombatOutcome.Running,settings!=null,lobby!=null,abandon!=null,intent,error);}
        void Continue()
        {
            if(!isActiveAndEnabled||!IsOpen||submitted||driver.Outcome!=BasicCombatOutcome.Running)return;
            try{driver.SetPaused(false);Close();}
            catch(Exception e){error="기록을 저장하지 못했습니다. 복구가 필요합니다.";Debug.LogWarning("Pause resume failed: "+e.GetType().Name);Refresh();}
        }
        public void Close(){if(view==null)return;view.Unbind();view.gameObject.SetActive(false);error=null;}
        public void RequestBack()=>Continue();
        void OpenSettings(){if(intent==CombatPauseIntent.Menu&&!submitted)Navigate(settings);}
        void ReturnToLobby()=>Confirm(CombatPauseIntent.Lobby,lobby);
        void Abandon()=>Confirm(CombatPauseIntent.Abandon,abandon);
        void Confirm(CombatPauseIntent requested,Action action)
        {
            if(!isActiveAndEnabled||!IsOpen||action==null||submitted||driver.RecoveryRequired||driver.Outcome!=BasicCombatOutcome.Running)return;
            if(intent!=requested){intent=requested;error=null;Refresh();return;}
            // Consume the UI intent before the host may close or reenter this view. The host owns the durable command.
            submitted=true;Refresh();Navigate(action);
        }
        void Navigate(Action action)
        {
            if(!isActiveAndEnabled||!IsOpen||action==null||driver.RecoveryRequired||driver.Outcome!=BasicCombatOutcome.Running)return;
            try{driver.SetPaused(true);action();}
            catch(Exception e){error="화면을 열지 못했습니다. 연결 상태를 확인해 주세요.";Debug.LogWarning("Pause navigation failed: "+e.GetType().Name);Refresh();}
        }
        void OnGUI(){if(IsOpen)backKey.Handle(Event.current,RequestBack,LastRail.Presentation.ScreenBackKey.Editing);}
        void OnEnable(){backKey.Synchronize();if(driver!=null){driver.StateChanged-=OwnerChanged;driver.StateChanged+=OwnerChanged;}}
        void OnApplicationFocus(bool focused){if(focused)backKey.Synchronize();}
        void OnDisable(){if(driver!=null)driver.StateChanged-=OwnerChanged;Close();UnbindNavigation();}
        void OnDestroy(){Close();UnbindNavigation();driver=null;if(view!=null){if(Application.isPlaying)Destroy(view.gameObject);else DestroyImmediate(view.gameObject);}view=null;}
    }
}
