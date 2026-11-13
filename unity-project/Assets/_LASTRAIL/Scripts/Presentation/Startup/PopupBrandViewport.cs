using UnityEngine;

namespace LastRail.Presentation.Startup
{
    // Keep the small brand inside the visible content and the safe area at either aspect.
    [ExecuteAlways]
    public sealed class PopupBrandViewport : MonoBehaviour
    {
        public RectTransform Root;
        readonly Vector3[] corners = new Vector3[4];
        void OnEnable() { Canvas.willRenderCanvases += Refresh; }
        void OnDisable() { Canvas.willRenderCanvases -= Refresh; }
        void Refresh()
        {
            if (Root == null) return;
            var rect = (RectTransform)transform; var parent = (RectTransform)rect.parent;
            var motion = Root.GetComponentInChildren<StartupMotionBackdrop>();
            var viewport = motion != null ? motion.transform.Find("Artboard") as RectTransform : Root.Find("StartupKeyVisual") as RectTransform;
            if (viewport == null || !viewport.gameObject.activeInHierarchy) viewport = Root;
            viewport.GetWorldCorners(corners);
            var top = Mathf.Min(parent.rect.yMax, parent.InverseTransformPoint(corners[1]).y);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, top - 90);
        }
    }
}
