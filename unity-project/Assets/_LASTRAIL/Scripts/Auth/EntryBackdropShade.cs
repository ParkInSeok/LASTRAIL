using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Auth
{
    // Four-stop readability gradient from the approved login mockup; no texture allocation.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EntryBackdropShade : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            float[] stops = { 0, .25f, .76f, 1 };
            float[] alpha = { .20f, .25f, .88f, 1 };
            for (int i = 0; i < stops.Length; i++)
            {
                float y = Mathf.Lerp(r.yMax, r.yMin, stops[i]);
                var tint = new Color(.012f, .047f, .063f, alpha[i]);
                vh.AddVert(new Vector3(r.xMin, y), tint, Vector2.zero);
                vh.AddVert(new Vector3(r.xMax, y), tint, Vector2.one);
                if (i > 0) { int v = i * 2; vh.AddTriangle(v - 2, v - 1, v); vh.AddTriangle(v - 1, v + 1, v); }
            }
        }
    }
}
