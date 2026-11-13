using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastRail.Auth
{
    public sealed class AuthKeyboardAvoidance : MonoBehaviour
    {
        ScrollRect scroll;
        Vector2 restingOffset;
        TMP_InputField previousFocus;
        int heightBeforeKeyboard;
        bool keyboardWasVisible;
        public void Initialize(ScrollRect owner)
        { scroll = owner; restingOffset = scroll.viewport.offsetMin; heightBeforeKeyboard = Screen.height; }
        void LateUpdate()
        {
            if (scroll == null) return;
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            var focus = selected == null ? null : selected.GetComponentInParent<TMP_InputField>();
            if (focus != null && (!focus.transform.IsChildOf(scroll.content) || !focus.isActiveAndEnabled)) focus = null;
            bool visible = TouchScreenKeyboard.visible;
            float coveredPixels = visible ? TouchScreenKeyboard.area.yMax : 0;
            if (!keyboardWasVisible && !visible) heightBeforeKeyboard = Screen.height;
            // Some Android keyboards report an empty area. A resized player already excludes the keyboard.
            if (visible && coveredPixels <= 0 && Screen.height >= heightBeforeKeyboard * .85f) coveredPixels = Screen.height * .42f;
            keyboardWasVisible = visible;
            Adjust(coveredPixels, focus);
        }
        void Adjust(float bottomPixels, TMP_InputField focus)
        {
            if (scroll == null || Screen.height <= 0) return;
            var safe = (RectTransform)scroll.transform;
            var canvas = scroll.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, new Vector2(Screen.width * .5f, bottomPixels), camera, out var point);
            float inset = bottomPixels > 0 ? Mathf.Max(restingOffset.y, point.y - safe.rect.yMin + 24) : restingOffset.y;
            inset = Mathf.Min(inset, Mathf.Max(restingOffset.y, safe.rect.height + scroll.viewport.offsetMax.y - 240));
            bool changed = Mathf.Abs(scroll.viewport.offsetMin.y - inset) > .5f || focus != previousFocus;
            scroll.viewport.offsetMin = new Vector2(restingOffset.x, inset);
            if (changed) Canvas.ForceUpdateCanvases();
            previousFocus = focus;
            if (focus == null) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, (RectTransform)focus.transform);
            float delta = bounds.min.y < scroll.viewport.rect.yMin + 24 ? scroll.viewport.rect.yMin + 24 - bounds.min.y : 0;
            if (bounds.max.y + delta > scroll.viewport.rect.yMax - 24) delta += scroll.viewport.rect.yMax - 24 - (bounds.max.y + delta);
            if (Mathf.Abs(delta) < .5f) return;
            scroll.StopMovement();
            var position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + delta, 0, Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height));
            scroll.content.anchoredPosition = position;
        }
        void OnDisable()
        { if (scroll != null) scroll.viewport.offsetMin = restingOffset; previousFocus = null; keyboardWasVisible = false; }
#if UNITY_EDITOR
        // Editor-only layout probe; does not claim a native keyboard was opened.
        public void EvaluateEditorKeyboard(float bottomPixels, TMP_InputField focus) { Adjust(bottomPixels, focus); }
#endif
    }
}
