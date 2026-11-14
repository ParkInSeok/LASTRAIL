#if UNITY_5_3_OR_NEWER
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.UI
{
    public sealed class LastRailUiToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private string actionKey;

        private bool callbacksBound;

        public event Action<bool> ValueChanged;

        public string ActionKey => actionKey ?? string.Empty;

        public void BindInteractionCallbacks()
        {
            if (!isActiveAndEnabled || toggle == null || callbacksBound)
            {
                return;
            }

            toggle.onValueChanged.AddListener(HandleValueChanged);
            callbacksBound = true;
        }

        public void UnbindInteractionCallbacks()
        {
            if (toggle == null || !callbacksBound)
            {
                return;
            }

            toggle.onValueChanged.RemoveListener(HandleValueChanged);
            callbacksBound = false;
        }

        public void SetLabel(string value)
        {
            if (labelText != null)
            {
                labelText.text = value ?? string.Empty;
            }
        }

        private void OnEnable()
        {
            BindInteractionCallbacks();
        }

        private void OnDisable()
        {
            UnbindInteractionCallbacks();
        }

        private void HandleValueChanged(bool value)
        {
            if (!isActiveAndEnabled) return;
            ValueChanged?.Invoke(value);
        }
    }
}
#endif