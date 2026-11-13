#if UNITY_5_3_OR_NEWER
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Battle
{
    public sealed class CombatHudTransitionView : MonoBehaviour
    {
        private const float Duration = 1.4f;
        private CanvasGroup group;
        private Image icon;
        private Image warning;
        private TMP_Text label;
        private AudioSource audioSource;
        private Sprite[] sprites;
        private AudioClip[] clips;
        private float elapsed;

        public int PresentationCount { get; private set; }
        public int AudioDispatchCount { get; private set; }
        public CombatHudTransition Current { get; private set; }

        public static CombatHudTransitionView Create(RectTransform parent, TMP_Text style, Sprite[] sprites, AudioClip[] clips)
        {
            var rect = CreateRect(parent, "CombatTransitionBanner", 0, 150, 900, 96);
            var view = rect.gameObject.AddComponent<CombatHudTransitionView>();
            view.sprites = sprites;
            view.clips = clips;
            view.group = rect.gameObject.AddComponent<CanvasGroup>();
            view.group.blocksRaycasts = false;
            view.group.interactable = false;
            var frame = CreateImage(rect, "TransitionFrame", view.SpriteAt(0), 0, 0, 900, 96);
            frame.type = Image.Type.Sliced;
            frame.preserveAspect = false;
            view.icon = CreateImage(rect, "TransitionIcon", null, -366, 0, 76, 76);
            view.warning = CreateImage(rect, "TransitionWarningAccent", view.SpriteAt(2), 0, 43, 800, 8);
            view.warning.preserveAspect = false;
            view.label = CreateRect(rect, "TransitionLabel", 32, 0, 660, 70).gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null) view.label.font = style.font;
            view.label.fontSize = 36;
            view.label.alignment = TextAlignmentOptions.Center;
            view.label.raycastTarget = false;
            view.audioSource = rect.gameObject.AddComponent<AudioSource>();
            view.audioSource.playOnAwake = false;
            view.audioSource.loop = false;
            view.audioSource.spatialBlend = 0;
            view.audioSource.volume = 0.65f;
            view.Cancel();
            return view;
        }

        public void Show(CombatHudTransition transition)
        {
            Cancel();
            if (transition == null) return;
            var phase = transition.Kind == CombatHudTransitionKind.BossPhaseChanged;
            icon.sprite = SpriteAt(phase ? 3 : 1);
            if (icon.sprite == null) return;
            Current = transition;
            label.text = transition.Label;
            warning.gameObject.SetActive(phase);
            group.alpha = 0;
            gameObject.SetActive(true);
            PresentationCount++;
            var index = phase ? 1 : 0;
            if (clips != null && index < clips.Length && clips[index] != null)
            {
                audioSource.clip = clips[index];
                AudioDispatchCount++;
                if (Application.isPlaying) audioSource.Play();
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (Current == null) return;
            // Cosmetic lifetime only. This never creates or advances a battle event.
            elapsed += Mathf.Max(0, deltaSeconds);
            if (elapsed >= Duration) { Cancel(); return; }
            group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / 0.12f), Mathf.Clamp01((Duration - elapsed) / 0.25f));
        }

        public void Cancel()
        {
            Clear();
            gameObject.SetActive(false);
        }

        private void Clear()
        {
            Current = null;
            elapsed = 0;
            if (group != null) group.alpha = 0;
            if (label != null) label.text = string.Empty;
            if (audioSource != null) { audioSource.Stop(); audioSource.clip = null; }
        }

        private void Update() => Tick(Time.unscaledDeltaTime);
        private void OnDisable() => Clear();
        private Sprite SpriteAt(int index) => sprites != null && index < sprites.Length ? sprites[index] : null;

        private static RectTransform CreateRect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
        {
            var image = CreateRect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
#endif
