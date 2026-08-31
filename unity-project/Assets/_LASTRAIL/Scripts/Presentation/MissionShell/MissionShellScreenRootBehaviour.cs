#if UNITY_5_3_OR_NEWER
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed class MissionShellScreenRootBehaviour : MonoBehaviour, IMissionShellScreenView
    {
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text missionRowsText;
        [SerializeField] private TMP_Text primaryButtonText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text secondaryButtonText;
        [SerializeField] private Button secondaryButton;

        public MissionShellScreenViewState CurrentState { get; private set; }
        public event Action PrimaryActionRequested;
        public event Action SecondaryActionRequested;

        private void OnEnable()
        {
            BindInteractionCallbacks();
        }

        private void OnDisable()
        {
            UnbindInteractionCallbacks();
        }

        public void BindInteractionCallbacks()
        {
            BindButton(primaryButton, OnPrimaryActionClicked);
            BindButton(secondaryButton, OnSecondaryActionClicked);
        }

        public void Render(MissionShellScreenViewState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            CurrentState = state;
            ApplySafeArea();
            SetText(titleText, state.Title);
            SetText(subtitleText, state.Subtitle);
            SetText(bodyText, JoinLines(state.BodyLines));
            SetText(missionRowsText, FormatMissionRows(state));
            SetText(primaryButtonText, state.PrimaryActionLabel);
            SetButton(primaryButton, state.IsPrimaryActionEnabled);
            SetText(secondaryButtonText, state.SecondaryActionLabel);
            SetButton(secondaryButton, state.SecondaryActionLabel != null);
            BindInteractionCallbacks();
        }

        private void OnPrimaryActionClicked()
        {
            if (primaryButton == null || !primaryButton.interactable)
            {
                return;
            }

            PrimaryActionRequested?.Invoke();
        }

        private void OnSecondaryActionClicked()
        {
            if (secondaryButton == null || !secondaryButton.interactable)
            {
                return;
            }

            SecondaryActionRequested?.Invoke();
        }

        private static void BindButton(Button target, UnityEngine.Events.UnityAction action)
        {
            if (target == null)
            {
                return;
            }

            target.onClick.RemoveListener(action);
            target.onClick.AddListener(action);
        }

        private void UnbindInteractionCallbacks()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveListener(OnPrimaryActionClicked);
            }

            if (secondaryButton != null)
            {
                secondaryButton.onClick.RemoveListener(OnSecondaryActionClicked);
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            safeAreaRoot.anchorMin = min;
            safeAreaRoot.anchorMax = max;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetButton(Button target, bool enabled)
        {
            if (target != null)
            {
                target.interactable = enabled;
            }
        }

        private static string JoinLines(System.Collections.Generic.IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        private static string FormatMissionRows(MissionShellScreenViewState state)
        {
            if (state.MissionRows.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < state.MissionRows.Count; i++)
            {
                var row = state.MissionRows[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(row.IsServerListedUnlockedForDisplay ? "[열림] " : "[잠김] ");
                builder.Append(row.DisplayLabel);
            }

            return builder.ToString();
        }
    }
}
#endif
