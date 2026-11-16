#if UNITY_5_3_OR_NEWER
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.UI
{
    public sealed class LastRailUiSegmentedControl : MonoBehaviour
    {
        [SerializeField] private Button[] segmentButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] segmentLabels = Array.Empty<TMP_Text>();
        [SerializeField] private string actionKey;

        private bool callbacksBound;
        private Button[] boundButtons;
        private UnityEngine.Events.UnityAction[] handlers;

        public event Action<int> SegmentSelected;

        public string ActionKey => actionKey ?? string.Empty;

        public void BindInteractionCallbacks()
        {
            if (!isActiveAndEnabled || callbacksBound)
            {
                return;
            }

            boundButtons = (Button[])segmentButtons.Clone();
            handlers = new UnityEngine.Events.UnityAction[boundButtons.Length];
            for (var i = 0; i < boundButtons.Length; i++)
            {
                var index = i;
                if (boundButtons[i] != null)
                {
                    handlers[i] = () => HandleSegmentSelected(index);
                    boundButtons[i].onClick.AddListener(handlers[i]);
                }
            }

            callbacksBound = true;
        }

        public void UnbindInteractionCallbacks()
        {
            if (!callbacksBound) return;
            for (var i = 0; i < boundButtons.Length; i++)
                if (boundButtons[i] != null && handlers[i] != null)
                    boundButtons[i].onClick.RemoveListener(handlers[i]);
            boundButtons = null;
            handlers = null;
            callbacksBound = false;
        }

        private void OnDisable() => UnbindInteractionCallbacks();

        public void SetLabels(params string[] labels)
        {
            for (var i = 0; i < segmentLabels.Length; i++)
            {
                if (segmentLabels[i] != null)
                {
                    segmentLabels[i].text = labels != null && i < labels.Length ? labels[i] ?? string.Empty : string.Empty;
                }
            }
        }

        private void OnEnable()
        {
            BindInteractionCallbacks();
        }

        private void HandleSegmentSelected(int index)
        {
            if (!isActiveAndEnabled) return;
            SegmentSelected?.Invoke(index);
        }
    }
}
#endif