using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Battle {
internal static class CombatPointerCancellation {
 public static bool IsCanceled(PointerEventData data){
  if(data is UnityEngine.InputSystem.UI.ExtendedPointerEventData extended&&extended.device is UnityEngine.InputSystem.Touchscreen screen)
   foreach(var touch in screen.touches)if(touch.touchId.ReadValue()==extended.touchId)return touch.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled;
  return false;
 }
}
// Capture belongs to the first down; cancellation never rolls back an accepted command.
public class CombatPointerSlider:Slider,ICancelHandler {
#if UNITY_EDITOR
 // uGUI's Editor-only Prelayout callback is a display refresh, not player intent.
 public override void Rebuild(CanvasUpdate executing){}
#endif
 protected bool Gesture { get; private set; }
 int pointer;
 public void BeginGesture(int id){if(Gesture||!IsActive()||!IsInteractable())return;Gesture=true;pointer=id;}
 protected bool Owns(int id)=>Gesture&&pointer==id;
 public void CancelGesture(){if(Gesture)InstantClearState();Gesture=false;}
 public override void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left||Gesture)return;BeginGesture(e.pointerId);if(Owns(e.pointerId))base.OnPointerDown(e);}
 public override void OnDrag(PointerEventData e){if(!Owns(e.pointerId))return;if(CombatPointerCancellation.IsCanceled(e)){CancelGesture();return;}if(IsInteractable())base.OnDrag(e);}
 public override void OnPointerUp(PointerEventData e){if(!Owns(e.pointerId))return;base.OnPointerUp(e);CancelGesture();}
 public void OnCancel(BaseEventData e)=>CancelGesture();
 protected override void DoStateTransition(SelectionState state,bool instant){base.DoStateTransition(state,instant);if(state==SelectionState.Disabled)CancelGesture();}
 protected override void OnCanvasGroupChanged(){base.OnCanvasGroupChanged();if(!IsInteractable())CancelGesture();}
 protected override void OnDisable(){CancelGesture();base.OnDisable();}
 protected void OnApplicationFocus(bool focused){if(!focused)CancelGesture();}
 protected void OnApplicationPause(bool paused){if(paused)CancelGesture();}
}
}
