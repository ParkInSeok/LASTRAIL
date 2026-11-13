using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Auth
{
    public sealed class IndependentMetalSurface : MonoBehaviour
    {
        public MetalSliceGraphic Frame { get; private set; }
        public ChamferedMaterialGraphic Backplate { get; private set; }
        public RectTransform Lights { get; private set; }
        Material emphasisMaterial;
        float primaryIntensity=2.7f;

        public static IndependentMetalSurface Create(RectTransform parent, bool control, Color plateColor)
        {
            var root = AuthFormVisuals.Rect("IndependentMetalSurface", parent);
            root.SetAsFirstSibling();
            var surface = root.gameObject.AddComponent<IndependentMetalSurface>();
            var prefix = "EntryPopup/" + (control ? "control" : "popup");
            surface.Backplate = AuthFormVisuals.Rect("Backplate", root).gameObject.AddComponent<ChamferedMaterialGraphic>();
            surface.Backplate.texture = Load(prefix + "-backplate");
            surface.Backplate.Inset = control ? new Vector2(48, 28) : new Vector2(67, 67);
            surface.Backplate.Bevel = control ? 25 : 36;
            surface.Backplate.uvRect = control ? new Rect(.15f, .25f, .7f, .5f) : new Rect(.2f, .2f, .6f, .6f);
            surface.Backplate.color = plateColor; surface.Backplate.raycastTarget = false;
            surface.Frame = AuthFormVisuals.Rect("Frame", root).gameObject.AddComponent<MetalSliceGraphic>();
            surface.Frame.Control = control; surface.Frame.texture = Load(prefix + "-frame-v03"); surface.Frame.raycastTarget = false;
            surface.Lights = AuthFormVisuals.Rect("StateLights", root);
            AddLight(surface.Lights, "LeftGlow", new Vector2(0, control ? .5f : 1), new Vector2(control ? 25 : 29, control ? 0 : -175), new Vector2(9, control ? 38 : 53));
            AddLight(surface.Lights, "RightGlow", new Vector2(1, control ? .5f : 1), new Vector2(control ? -25 : -29, control ? 0 : -175), new Vector2(9, control ? 38 : 53));
            if (!control) AddLight(surface.Lights, "TopGlow", new Vector2(.5f, 1), new Vector2(0, -25), new Vector2(150, 9));
            return surface;
        }

        static Texture2D Load(string path) => UnityEngine.Resources.Load<Texture2D>(path) ?? throw new InvalidOperationException("Independent UI resource missing: " + path);
        static void AddLight(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = AuthFormVisuals.Rect(name, parent); rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size; rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.15f, .9f, 1); image.raycastTarget = false;
            var glow = image.gameObject.AddComponent<Shadow>(); glow.effectColor = new Color(.1f, .8f, 1, .4f); glow.effectDistance = new Vector2(3, 0);
        }

        public void SetLightColor(Color color)
        {
            foreach (var image in Lights.GetComponentsInChildren<Image>()) image.color = color;
        }

        public void EmphasizePrimary(float intensity=2.7f)
        {
            primaryIntensity=Mathf.Clamp(intensity,1,2.7f);
            SetPrimaryPlate(true);
        }

        // The authored slots cross the fixed corner / stretching rail boundary.
        // Map them through the same 9-slice geometry as Frame instead of fixed pixel offsets.
        public void AlignPopupLights(bool showTop)
        {
            var size=((RectTransform)transform).rect.size;float cap=126*Mathf.Max(.01f,Frame.BorderScale);
            float Map(float value,float source,float extent){float edge=Mathf.Min(cap,extent/2);return value<170?value/170*edge:value>source-170?extent-(source-value)/170*edge:edge+(value-170)/(source-340)*(extent-2*edge);}
            float x0=Map(29,1110,size.x),x1=Map(44,1110,size.x),y0=Map(164,1163,size.y),y1=Map(216,1163,size.y);
            foreach(var name in new[]{"LeftGlow","RightGlow"}){var r=(RectTransform)Lights.Find(name);r.anchoredPosition=new Vector2((name=="LeftGlow"?1:-1)*(x0+x1)/2,-(y0+y1)/2);r.sizeDelta=new Vector2(x1-x0,y1-y0);}
            var top=Lights.Find("TopGlow");if(top!=null){top.gameObject.SetActive(showTop);var r=(RectTransform)top;r.anchoredPosition=new Vector2(0,-Map(39,1163,size.y));r.sizeDelta=new Vector2(Map(661,1110,size.x)-Map(448,1110,size.x),Mathf.Max(2,7*Frame.BorderScale));}
        }

        // Display-only state supplied by the owning control; does not change input authority.
        public void SetPrimaryInteractable(bool interactable)
        {
            SetPrimaryPlate(interactable);
            Frame.color = interactable ? Color.white : new Color(.40f, .43f, .44f);
            Lights.gameObject.SetActive(interactable);
        }

        void SetPrimaryPlate(bool interactable)
        {
            if (emphasisMaterial == null) emphasisMaterial = new Material(Backplate.defaultMaterial) { name = "OwnedPrimaryPlateTint" };
            emphasisMaterial.SetColor("_Color", interactable ? new Color(primaryIntensity, primaryIntensity, primaryIntensity, 1) : Color.white);
            Backplate.material = emphasisMaterial;
            Backplate.color = interactable ? new Color(.12f, .72f, 1) : new Color(.20f, .23f, .24f);
        }

        void OnDestroy()
        {
            if (emphasisMaterial == null) return;
            if (Application.isPlaying) Destroy(emphasisMaterial); else DestroyImmediate(emphasisMaterial);
        }
    }

    public sealed class MetalSliceGraphic : RawImage
    {
        public bool Control;
        // Instance-specific caps let small composed cells reuse the frame without oversized corners.
        public float BorderScale=1;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            // Native UV slices preserve corner/rail proportions; image files remain unchanged.
            var crop = Control ? new Rect(37f / 2172, 141f / 724, 2100f / 2172, 474f / 724)
                : new Rect(57f / 1223, 68f / 1286, 1110f / 1223, 1163f / 1286);
            var cap = (Control ? new Vector2(77, 34) : new Vector2(126, 126))*Mathf.Max(.01f,BorderScale);
            cap.x = Mathf.Min(cap.x, r.width / 2); cap.y = Mathf.Min(cap.y, r.height / 2);
            var uvCap = Control ? new Vector2(230f / 2100, 92f / 474) : new Vector2(170f / 1110, 170f / 1163);
            var xs = new[] { r.xMin, r.xMin + cap.x, r.xMax - cap.x, r.xMax };
            var ys = new[] { r.yMin, r.yMin + cap.y, r.yMax - cap.y, r.yMax };
            var us = new[] { crop.xMin, crop.xMin + crop.width * uvCap.x, crop.xMax - crop.width * uvCap.x, crop.xMax };
            var vs = new[] { crop.yMin, crop.yMin + crop.height * uvCap.y, crop.yMax - crop.height * uvCap.y, crop.yMax };
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) mesh.AddVert(new Vector3(xs[x], ys[y]), color, new Vector2(us[x], vs[y]));
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
            { if (x == 1 && y == 1) continue; var i = y * 4 + x; mesh.AddTriangle(i, i + 4, i + 5); mesh.AddTriangle(i, i + 5, i + 1); }
        }
    }

    public sealed class ChamferedMaterialGraphic : RawImage
    {
        public Vector2 Inset;
        public float Bevel;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var full = rectTransform.rect;
            var r = new Rect(full.x + Inset.x, full.y + Inset.y, full.width - 2 * Inset.x, full.height - 2 * Inset.y);
            if (r.width <= 0 || r.height <= 0) return;
            var b = Mathf.Min(Bevel, Mathf.Min(r.width, r.height) / 2);
            var points = new[] { r.center, new Vector2(r.x + b, r.y), new Vector2(r.xMax - b, r.y), new Vector2(r.xMax, r.y + b), new Vector2(r.xMax, r.yMax - b), new Vector2(r.xMax - b, r.yMax), new Vector2(r.x + b, r.yMax), new Vector2(r.x, r.yMax - b), new Vector2(r.x, r.y + b) };
            foreach (var point in points) mesh.AddVert(point, color, new Vector2(uvRect.x + uvRect.width * (point.x - r.x) / r.width, uvRect.y + uvRect.height * (point.y - r.y) / r.height));
            for (var i = 1; i <= 8; i++) mesh.AddTriangle(0, i, i == 8 ? 1 : i + 1);
        }
    }
}
