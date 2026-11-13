using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle
{
    // A brief grant receipt decoration. It never represents remaining shield or an impact.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatKyleGrantGraphic : MaskableGraphic
    {
        public bool Ultimate;
        public float Progress { get; private set; }
        public void SetProgress(float value) { Progress = Mathf.Clamp01(value); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Progress <= 0 || Progress >= 1) return;
            var r = GetPixelAdjustedRect();
            float enter = Mathf.SmoothStep(0, 1, Progress / .18f);
            float fade = 1 - Mathf.SmoothStep(0, 1, (Progress - .65f) / .35f);
            Color tint = Ultimate ? new Color32(91, 158, 255, 255) : new Color32(69, 217, 232, 255);
            tint.a = .65f * enter * fade;
            Vector2 P(float x, float y) => r.center + new Vector2(x * r.width, y * r.height);
            void Line(Vector2 a, Vector2 b, float width, Color color)
            {
                if (Mathf.Max(a.y, b.y) > r.yMin + r.height * enter) return;
                var normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
                int start = vh.currentVertCount;
                vh.AddVert(a - normal, color, Vector2.zero); vh.AddVert(a + normal, color, Vector2.zero);
                vh.AddVert(b + normal, color, Vector2.zero); vh.AddVert(b - normal, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
            void Hex(Vector2 center, float radius, Color color)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3, b = (i + 1) * Mathf.PI / 3;
                    Line(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                         center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, 1, color);
                }
            }
            if (!Ultimate)
            {
                // Open above the face; only sparse edge geometry, no overlapping translucent fill.
                for (int i = 0; i < 42; i++)
                {
                    float a = (120 + 300f * i / 42) * Mathf.Deg2Rad;
                    float b = (120 + 300f * (i + 1) / 42) * Mathf.Deg2Rad;
                    Line(P(Mathf.Cos(a) * .46f, Mathf.Sin(a) * .46f), P(Mathf.Cos(b) * .46f, Mathf.Sin(b) * .46f), 2, tint);
                }
                var dim = tint; dim.a *= .56f;
                foreach (float side in new[] { -1f, 1f })
                    foreach (float y in new[] { -.22f, -.06f, .10f }) Hex(P(side * .39f, y), Mathf.Min(r.width, r.height) * .046f, dim);
            }
            else
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Line(P(side * .27f, .46f), P(side * .44f, .34f), 2.2f, tint);
                    foreach (float y in new[] { -.28f, -.07f, .14f }) Line(P(side * .44f, y), P(side * .44f, y + .16f), 2.2f, tint);
                    Line(P(side * .44f, -.33f), P(side * .28f, -.43f), 2, tint);
                    var dim = tint; dim.a *= .6f;
                    Hex(P(side * .35f, -.08f), Mathf.Min(r.width, r.height) * .06f, dim);
                    var corner = Color.Lerp(tint, Color.white, .55f); corner.a = tint.a * (1 - Mathf.Clamp01((Progress - .38f) / .27f));
                    Line(P(side * .28f, .43f), P(side * .38f, .36f), 3, corner);
                }
                for (int i = 0; i < 14; i++)
                {
                    float a = (195 + i * 150f / 14) * Mathf.Deg2Rad, b = (195 + (i + 1) * 150f / 14) * Mathf.Deg2Rad;
                    Line(P(Mathf.Cos(a) * .34f, -.39f + Mathf.Sin(a) * .07f), P(Mathf.Cos(b) * .34f, -.39f + Mathf.Sin(b) * .07f), 2, tint);
                }
            }
        }
    }
}
