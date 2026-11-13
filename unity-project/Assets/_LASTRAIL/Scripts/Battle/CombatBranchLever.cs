using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Battle {
// A single gesture owns preview + commit. Programmatic values never submit.
public sealed class CombatBranchLever:CombatPointerSlider,ISubmitHandler {
 public Action Commit;
 public void EndGesture(int id,bool inside){if(!Owns(id))return;CancelGesture();if(inside&&IsInteractable()&&Mathf.Abs(value-Mathf.Round(value))<.01f)Commit?.Invoke();}
 public override void OnPointerUp(PointerEventData e){bool commit=Owns(e.pointerId)&&!CombatPointerCancellation.IsCanceled(e)&&RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,e.position,e.pressEventCamera);base.OnPointerUp(e);if(commit&&IsInteractable()&&Mathf.Abs(value-Mathf.Round(value))<.01f)Commit?.Invoke();}
 public void OnSubmit(BaseEventData e){if(!Gesture&&IsInteractable())Commit?.Invoke();}
}
}
