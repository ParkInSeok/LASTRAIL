#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Tutorial
{
    public sealed class TutorialGuideView : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Image pointer;
        [SerializeField] private Image[] borders;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;
        private Behaviour owner;
        private RectTransform target;
        private RectTransform safeArea;
        private RectTransform[] extraAvoid = Array.Empty<RectTransform>();
        private Action next;
        private Action close;
        private readonly List<Rect> obstacles = new List<Rect>();
        private readonly Vector3[] corners = new Vector3[4];
        public string StepId { get; private set; }
        public bool IsVisible => panel != null && panel.gameObject.activeSelf;
        public Rect TargetBounds { get; private set; }
        public Rect PanelBounds => LocalBounds(panel);
        public string LastClearReason { get; private set; }

        private void Awake() { Bind(); Clear(); }
        private void OnDisable() { Clear("guide_disabled"); Unbind(); }
        private void LateUpdate() { if (target != null || StepId != null) Refresh(); }

        public bool Show(Behaviour screenOwner, RectTransform visibleSafeArea, RectTransform stepTarget,
            string stepId, string resolvedText, Action onNext = null, Action onClose = null,
            RectTransform[] avoid = null)
        {
            if (string.IsNullOrWhiteSpace(stepId)) throw new ArgumentException("Step id required", nameof(stepId));
            owner = screenOwner; safeArea = visibleSafeArea; target = stepTarget;
            StepId = stepId; next = onNext; close = onClose;
            extraAvoid = avoid == null ? Array.Empty<RectTransform>() : (RectTransform[])avoid.Clone();
            body.text = resolvedText ?? string.Empty;
            Bind();
            nextButton.interactable = next != null;
            closeButton.interactable = close != null;
            LastClearReason = null;
            return Refresh();
        }

        public bool Refresh()
        {
            if (!isActiveAndEnabled || owner == null || !owner.isActiveAndEnabled || target == null || safeArea == null
                || !target.gameObject.activeInHierarchy || !safeArea.gameObject.activeInHierarchy
                || !target.IsChildOf(owner.transform) || !transform.IsChildOf(owner.transform)
                || Hidden(target)) return Clear("target_or_owner_unavailable");
            var canvas = GetComponentInParent<Canvas>();
            var targetCanvas = target.GetComponentInParent<Canvas>();
            if (canvas == null || targetCanvas == null || !canvas.isActiveAndEnabled || !targetCanvas.isActiveAndEnabled || canvas.rootCanvas != targetCanvas.rootCanvas)
                return Clear("different_canvas");
            var graphic = target.GetComponent<Graphic>();
            if (graphic != null && (!graphic.isActiveAndEnabled || graphic.color.a <= 0)) return Clear("target_graphic_hidden");
            TargetBounds = LocalBounds(target);
            var safe = LocalBounds(safeArea);
            if (TargetBounds.width < 1 || TargetBounds.height < 1 || !Contains(safe, Inflate(TargetBounds, 12)))
                return Clear("target_outside_safe_area");
            foreach (var mask in target.GetComponentsInParent<RectMask2D>())
                if (mask.isActiveAndEnabled && !Contains(LocalBounds(mask.rectTransform), TargetBounds))
                    return Clear("target_clipped");

            obstacles.Clear();
            obstacles.Add(Inflate(TargetBounds, 16));
            foreach (var text in owner.GetComponentsInChildren<TMP_Text>())
                if (!text.transform.IsChildOf(transform) && !string.IsNullOrEmpty(text.text) && !Hidden(text.transform))
                    obstacles.Add(Inflate(LocalBounds(text.rectTransform), 8));
            foreach (var button in owner.GetComponentsInChildren<Button>())
                if (!button.transform.IsChildOf(transform) && !Hidden(button.transform))
                    obstacles.Add(Inflate(LocalBounds((RectTransform)button.transform), 8));
            foreach (var rect in extraAvoid)
                if (rect != null && rect.gameObject.activeInHierarchy && !Hidden(rect)) obstacles.Add(Inflate(LocalBounds(rect), 8));

            var frame = Inflate(TargetBounds, 10);
            PositionPointer(safe, frame, default);
            if (pointer.gameObject.activeSelf) obstacles.Add(Inflate(LocalBounds(pointer.rectTransform), 18));

            var width = Mathf.Min(960, safe.width - 24);
            if (width < 940) return Clear("insufficient_width");
            const float height = 240;
            var best = new Rect();
            var score = float.PositiveInfinity;
            // Search actual free screen space; never cover the target or protected controls to force a fit.
            for (var y = safe.yMin + 12; y + height <= safe.yMax - 12; y += 12)
            {
                var candidate = new Rect(safe.center.x - width / 2, y, width, height);
                if (Blocked(candidate)) continue;
                var distance = (candidate.center - TargetBounds.center).sqrMagnitude;
                if (distance < score) { best = candidate; score = distance; }
            }
            if (float.IsPositiveInfinity(score)) return Clear("no_unobstructed_space");
            Place(panel, best);
            panel.gameObject.SetActive(true);
            body.ForceMeshUpdate();
            if (body.isTextOverflowing) return Clear("text_does_not_fit");
            var lines = new[] {
                new Rect(frame.xMin, frame.yMax - 4, frame.width, 4), new Rect(frame.xMin, frame.yMin, frame.width, 4),
                new Rect(frame.xMin, frame.yMin, 4, frame.height), new Rect(frame.xMax - 4, frame.yMin, 4, frame.height) };
            for (var i = 0; i < borders.Length; i++) { Place(borders[i].rectTransform, lines[i]); borders[i].gameObject.SetActive(true); }
            return true;
        }

        private void PositionPointer(Rect safe, Rect frame, Rect panelBounds)
        {
            const float size = 84;
            var centers = new[] {
                new Vector2(frame.xMax + size / 2 + 32, frame.center.y), new Vector2(frame.xMin - size / 2 - 32, frame.center.y),
                new Vector2(frame.center.x, frame.yMax + size / 2 + 32), new Vector2(frame.center.x, frame.yMin - size / 2 - 32) };
            pointer.gameObject.SetActive(false);
            foreach (var center in centers)
            {
                var bounds = new Rect(center - Vector2.one * size / 2, Vector2.one * size);
                // Rotating a square arrow can occupy a larger axis-aligned box.
                var occupied = Inflate(bounds, 18);
                if (!Contains(safe, occupied) || Blocked(occupied) || panelBounds.Overlaps(occupied)) continue;
                Place(pointer.rectTransform, bounds);
                var direction = TargetBounds.center - center;
                pointer.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180);
                pointer.gameObject.SetActive(true);
                break;
            }
        }

        public bool Clear(string reason = "cleared")
        {
            if (panel != null) panel.gameObject.SetActive(false);
            if (body != null) body.text = string.Empty;
            if (pointer != null) pointer.gameObject.SetActive(false);
            if (borders != null) foreach (var border in borders) if (border != null) border.gameObject.SetActive(false);
            next = close = null; owner = null; target = safeArea = null;
            extraAvoid = Array.Empty<RectTransform>(); StepId = null; TargetBounds = default;
            LastClearReason = reason;
            if (nextButton != null) nextButton.interactable = false;
            if (closeButton != null) closeButton.interactable = false;
            return false;
        }

        private void Bind() { Unbind(); nextButton.onClick.AddListener(Next); closeButton.onClick.AddListener(Close); }
        private void Unbind()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
        private void Next() { Dispatch(true); }
        private void Close() { Dispatch(false); }
        private void Dispatch(bool forward)
        {
            if (!IsVisible || !Refresh()) return;
            var action = forward ? next : close;
            if (action == null) return;
            Clear("action_requested");
            action();
        }
        private bool Blocked(Rect rect) { foreach (var other in obstacles) if (rect.Overlaps(other)) return true; return false; }
        private static bool Hidden(Transform item)
        {
            foreach (var group in item.GetComponentsInParent<CanvasGroup>()) if (group.alpha <= 0) return true;
            return false;
        }
        private Rect LocalBounds(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                var local = (Vector2)transform.InverseTransformPoint(corner);
                min = Vector2.Min(min, local); max = Vector2.Max(max, local);
            }
            return new Rect(min, max - min);
        }
        private static Rect Inflate(Rect rect, float padding) => new Rect(rect.xMin - padding, rect.yMin - padding, rect.width + 2 * padding, rect.height + 2 * padding);
        private static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin && inner.xMax <= outer.xMax && inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
        private static void Place(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = bounds.center;
            rect.sizeDelta = bounds.size;
        }
    }
}
#endif
