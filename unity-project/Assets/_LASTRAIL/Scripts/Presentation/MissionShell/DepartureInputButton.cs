using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Input lifetime only; the controller supplies identity checks and owns the command.
    public sealed class DepartureInputButton : Button
    {
        Func<Func<bool>> capture;
        Func<bool> heldIntent;
        int heldPointer;
        public void BindIntent(Func<Func<bool>> value){CancelIntent();capture=value;}
        public void CancelIntent(){heldIntent=null;}
        Func<bool> Capture()=>capture==null?(()=>true):capture();
        public override void OnPointerDown(PointerEventData data)
        {
            CancelIntent();
            if(data.button==PointerEventData.InputButton.Left&&IsActive()&&IsInteractable()){
                heldPointer=data.pointerId;heldIntent=Capture();
            }
            base.OnPointerDown(data);
        }
        public override void OnPointerClick(PointerEventData data)
        {
            var intent=heldIntent;CancelIntent();
            if(data.button!=PointerEventData.InputButton.Left||data.pointerId!=heldPointer||intent==null||!IsActive()||!IsInteractable()||!intent())return;
            base.OnPointerClick(data);
        }
        public override void OnSubmit(BaseEventData data)
        {
            CancelIntent();if(!IsActive()||!IsInteractable())return;
            var intent=Capture();if(intent?.Invoke()!=true)return;base.OnSubmit(data);
        }
        protected override void OnDisable(){CancelIntent();base.OnDisable();}
        protected override void OnDestroy(){capture=null;CancelIntent();base.OnDestroy();}
    }
}
