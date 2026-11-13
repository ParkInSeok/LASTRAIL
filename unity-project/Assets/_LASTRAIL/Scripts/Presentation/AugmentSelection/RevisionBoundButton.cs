using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace LastRail.Presentation.AugmentSelection {
 // A gesture belongs to the displayed revision at pointer-down. Business
 // commands remain in the existing Button listener/Controller path.
 internal sealed class RevisionBoundButton:Button {
  readonly HashSet<int> pressed=new HashSet<int>();
  InputAction heldSubmit;
  public void InvalidateGesture(){
   pressed.Clear();ReleaseSubmit();
   var action=(EventSystem.current?.currentInputModule as InputSystemUIInputModule)?.submit?.action;
   if(action!=null&&action.IsPressed()){heldSubmit=action;heldSubmit.canceled+=SubmitReleased;}
  }
  void SubmitReleased(InputAction.CallbackContext context)=>ReleaseSubmit();
  void ReleaseSubmit(){if(heldSubmit!=null)heldSubmit.canceled-=SubmitReleased;heldSubmit=null;}
  public override void OnPointerDown(PointerEventData data){
   pressed.Remove(data.pointerId);
   if(data.button==PointerEventData.InputButton.Left&&IsActive()&&IsInteractable())pressed.Add(data.pointerId);
   base.OnPointerDown(data);
  }
  public override void OnPointerClick(PointerEventData data){if(!pressed.Remove(data.pointerId))return;base.OnPointerClick(data);}
  public override void OnSubmit(BaseEventData data){if(heldSubmit!=null)return;base.OnSubmit(data);}
  protected override void OnDisable(){pressed.Clear();ReleaseSubmit();base.OnDisable();}
 }
}
