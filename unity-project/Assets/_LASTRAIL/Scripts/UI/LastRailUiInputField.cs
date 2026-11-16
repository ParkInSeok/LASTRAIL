#if UNITY_5_3_OR_NEWER
using System;
using TMPro;
using UnityEngine;

namespace LastRail.UI
{
    public sealed class LastRailUiInputField : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private string actionKey;

        private bool callbacksBound;

        public event Action<string> ValueChanged;
        public event Action<string> Submitted;

        public string ActionKey => actionKey ?? string.Empty;

        public void BindInteractionCallbacks()
        {
            if (!isActiveAndEnabled || inputField == null || callbacksBound)
            {
                return;
            }

            inputField.onValueChanged.AddListener(HandleValueChanged);
            inputField.onSubmit.AddListener(HandleSubmitted);
            callbacksBound = true;
        }

        public void UnbindInteractionCallbacks()
        {
            if (inputField == null || !callbacksBound)
            {
                return;
            }

            inputField.onValueChanged.RemoveListener(HandleValueChanged);
            inputField.onSubmit.RemoveListener(HandleSubmitted);
            callbacksBound = false;
        }

        public void SetLabel(string value)
        {
            if (labelText != null)
            {
                labelText.text = value ?? string.Empty;
            }
        }

        public void SetError(string value)
        {
            if (errorText != null)
            {
                errorText.text = value ?? string.Empty;
                errorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
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

        private void HandleValueChanged(string value)
        {
            if (!isActiveAndEnabled) return;
            ValueChanged?.Invoke(value ?? string.Empty);
        }

        private void HandleSubmitted(string value)
        {
            if (!isActiveAndEnabled) return;
            Submitted?.Invoke(value ?? string.Empty);
        }
    }
}
#endif