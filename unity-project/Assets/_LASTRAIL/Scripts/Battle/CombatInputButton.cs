using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Battle {
// Gesture validity only. The existing onClick listener still owns the command.
public sealed class CombatInputButton:Button,ICancelHandler {
 int? pointer;bool eligible,released;
 void StopReleaseExpiry(){UnityEngine.Canvas.willRenderCanvases-=ExpireRelease;}
 public void CancelGesture(){StopReleaseExpiry();if(pointer.HasValue){InstantClearState();GetComponent<CombatCommandPress>()?.CancelFeedback();}pointer=null;eligible=false;released=false;}
 public override void OnPointerDown(PointerEventData e){
  if(pointer.HasValue||e.button!=PointerEventData.InputButton.Left||!IsActive()||!IsInteractable())return;
  pointer=e.pointerId;eligible=true;released=false;base.OnPointerDown(e);
 }
 public override void OnPointerExit(PointerEventData e){if(pointer==e.pointerId)eligible=false;base.OnPointerExit(e);}
 public override void OnPointerUp(PointerEventData e){if(pointer!=e.pointerId)return;if(CombatPointerCancellation.IsCanceled(e)){CancelGesture();return;}released=true;base.OnPointerUp(e);StopReleaseExpiry();UnityEngine.Canvas.willRenderCanvases+=ExpireRelease;}
 // One end-of-frame expiry for an up without click; do not block a fresh down next frame.
 void ExpireRelease(){StopReleaseExpiry();if(this!=null&&released)CancelGesture();}
 public override void OnPointerClick(PointerEventData e){
  if(pointer!=e.pointerId)return;StopReleaseExpiry();bool execute=eligible&&released;pointer=null;eligible=false;released=false;if(execute)base.OnPointerClick(e);
 }
 public override void OnSubmit(BaseEventData e){if(pointer.HasValue)return;base.OnSubmit(e);}
 public void OnCancel(BaseEventData e)=>CancelGesture();
 protected override void DoStateTransition(SelectionState state,bool instant){base.DoStateTransition(state,instant);if(state==SelectionState.Disabled){CancelGesture();GetComponent<CombatCommandPress>()?.CancelFeedback();}}
 protected override void OnCanvasGroupChanged(){base.OnCanvasGroupChanged();if(!IsInteractable())CancelGesture();}
 protected override void OnDisable(){CancelGesture();base.OnDisable();}
 void OnApplicationFocus(bool focused){if(!focused)CancelGesture();}
 void OnApplicationPause(bool paused){if(paused)CancelGesture();}
}
}
