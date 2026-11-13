#if UNITY_5_3_OR_NEWER
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.UI
{
    public sealed class LastRailUiButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private string actionKey;

        private bool callbacksBound;

        public event Action Clicked;

        public string ActionKey => actionKey ?? string.Empty;

        public void BindInteractionCallbacks()
        {
            if (button == null || callbacksBound)
            {
                return;
            }

            button.onClick.AddListener(HandleClicked);
            callbacksBound = true;
        }

        public void UnbindInteractionCallbacks()
        {
            if (button == null || !callbacksBound)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClicked);
            callbacksBound = false;
        }

        public void SetLabel(string value)
        {
            if (labelText != null)
            {
                labelText.text = value ?? string.Empty;
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
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

        private void HandleClicked()
        {
            Clicked?.Invoke();
        }
    }
}
#endif