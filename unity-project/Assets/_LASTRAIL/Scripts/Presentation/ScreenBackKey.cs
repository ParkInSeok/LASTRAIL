using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastRail.Presentation
{
    // One physical press belongs to one layer, even if that layer closes on key-down.
    public sealed class ScreenBackKey
    {
        bool held;
        public void ResetIfReleased(bool physicallyHeld){if(!physicallyHeld)held=false;}
        public void Synchronize()=>ResetIfReleased(UnityEngine.InputSystem.Keyboard.current?.escapeKey.isPressed==true);
        public void Handle(Event input,Action requestBack,bool editing)
        {
            if(input==null||input.keyCode!=KeyCode.Escape)return;
            if(input.type==EventType.KeyUp){held=false;input.Use();return;}
            if(input.type!=EventType.KeyDown)return;
            bool first=!held;held=true;input.Use();
            if(first&&!editing)requestBack?.Invoke();
        }
        public static bool Editing
        {
            get
            {
                var selected=EventSystem.current?.currentSelectedGameObject;
                return TouchScreenKeyboard.visible || selected!=null&&selected.GetComponent<TMP_InputField>()?.isFocused==true;
            }
        }
    }
}
