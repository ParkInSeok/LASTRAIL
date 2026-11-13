using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle
{
    // Display categories only; equipment identity and availability remain external state.
    public sealed class CombatSystemSymbol : CombatLayeredGraphic
    {
        public enum Role { Firepower, Intercept, Recovery }
        public Role Kind;
        static readonly string[] Names = { "Symbol" };
        protected override string[] LayerNames => Names;
        protected override void DrawLayer(string layer, VertexHelper mesh)
        {
            mesh.Clear();
            var r = rectTransform.rect;
            void Path(params float[] points)
            {
                for (int p = 0; p + 3 < points.Length; p += 2)
                {
                    var a = new Vector2(r.x + points[p] * r.width, r.yMax - points[p + 1] * r.height);
                    var b = new Vector2(r.x + points[p + 2] * r.width, r.yMax - points[p + 3] * r.height);
                    var n = new Vector2(-(b - a).y, (b - a).x).normalized * r.width * .035f;
                    int i = mesh.currentVertCount;
                    mesh.AddVert(a - n, color, Vector2.zero); mesh.AddVert(a + n, color, Vector2.zero);
                    mesh.AddVert(b + n, color, Vector2.zero); mesh.AddVert(b - n, color, Vector2.zero);
                    mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
                }
            }
            switch (Kind)
            {
                case Role.Firepower:
                    Path(.13f,.42f,.30f,.25f,.60f,.25f,.60f,.13f,.92f,.13f,.92f,.38f,.64f,.38f,.55f,.55f,.37f,.55f,.25f,.87f,.10f,.87f,.25f,.55f,.13f,.55f,.13f,.42f);
                    break;
                case Role.Intercept:
                    Path(.5f,.05f,.88f,.24f,.88f,.61f,.5f,.95f,.12f,.61f,.12f,.24f,.5f,.05f);
                    Path(.55f,.2f,.37f,.54f,.62f,.54f,.41f,.81f);
                    break;
                case Role.Recovery:
                    Path(.36f,.08f,.64f,.08f,.64f,.36f,.92f,.36f,.92f,.64f,.64f,.64f,.64f,.92f,.36f,.92f,.36f,.64f,.08f,.64f,.08f,.36f,.36f,.36f,.36f,.08f);
                    break;
            }
        }
    }
}
