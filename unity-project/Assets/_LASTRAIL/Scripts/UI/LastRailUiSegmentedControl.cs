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

        public event Action<int> SegmentSelected;

        public string ActionKey => actionKey ?? string.Empty;

        public void BindInteractionCallbacks()
        {
            if (callbacksBound)
            {
                return;
            }

            for (var i = 0; i < segmentButtons.Length; i++)
            {
                var index = i;
                if (segmentButtons[i] != null)
                {
                    segmentButtons[i].onClick.AddListener(() => HandleSegmentSelected(index));
                }
            }

            callbacksBound = true;
        }

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
            SegmentSelected?.Invoke(index);
        }
    }
}
#endif