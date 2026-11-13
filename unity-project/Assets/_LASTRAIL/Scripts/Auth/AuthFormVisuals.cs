using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Auth
{
    public static class AuthFormVisuals
    {
        public static TMP_InputField Input(Transform parent, string name, string hint, TMP_FontAsset font, bool secret, int limit = 64)
        {
            var input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources()).GetComponent<TMP_InputField>();
            input.name = name; input.transform.SetParent(parent, false);
            input.contentType = secret ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            input.characterLimit = secret ? 128 : limit; input.richText = false;
            input.transition = Selectable.Transition.None;
            var background = input.GetComponent<Image>(); background.sprite = null; background.color = new Color(.04f, .08f, .11f, .79f);
            InputEdge(input.transform, "TopBorder", new Vector2(0, 1), Vector2.one, new Vector2(0, 1));
            InputEdge(input.transform, "BottomBorder", Vector2.zero, new Vector2(1, 0), new Vector2(0, 1));
            InputEdge(input.transform, "RightBorder", new Vector2(1, 0), Vector2.one, new Vector2(1, 0));
            var accent = Rect("Accent", input.transform); accent.anchorMax = new Vector2(0, 1); accent.pivot = new Vector2(0, .5f); accent.sizeDelta = new Vector2(6, 0);
            var edge = accent.gameObject.AddComponent<Image>(); edge.color = new Color(.47f, .73f, .78f); edge.raycastTarget = false;
            input.textViewport.offsetMin = new Vector2(40, 12); input.textViewport.offsetMax = new Vector2(-30, -12);
            foreach (var text in input.GetComponentsInChildren<TMP_Text>(true))
            { text.font = font; text.fontSize = 34; text.fontStyle = FontStyles.Normal; text.color = new Color(.82f, .88f, .9f); text.alignment = TextAlignmentOptions.MidlineLeft; text.richText = false; }
            ((TMP_Text)input.placeholder).text = hint; input.customCaretColor = true; input.caretColor = new Color(.5f, .9f, 1); return input;
        }
        private static void InputEdge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size)
        {
            var rect = Rect(name, parent); rect.anchorMin = min; rect.anchorMax = max; rect.sizeDelta = size;
            var edge = rect.gameObject.AddComponent<Image>(); edge.color = new Color(.33f, .44f, .47f); edge.raycastTarget = false;
        }
        public static Button MetalButton(Transform parent, string name, string label, TMP_FontAsset font, bool primary, Action click)
            =>MetalButton<Button>(parent,name,label,font,primary,click);
        public static T MetalButton<T>(Transform parent, string name, string label, TMP_FontAsset font, bool primary, Action click) where T:Button
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = null;
            image.type = Image.Type.Simple;
            image.color=Color.clear;
            var surface=Rect("AuthButtonSurface",rect).gameObject.AddComponent<AuthButtonSurface>();surface.raycastTarget=false;
            surface.Configure(null,primary);
            rect.gameObject.AddComponent<LastRail.Resources.PublicEntryTexture>().Bind(primary?"primary":"secondary",texture=>{if(surface!=null)surface.Configure(texture,primary);});
            var button = rect.gameObject.AddComponent<T>(); button.targetGraphic = surface;
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(.7f, .84f, .9f); colors.disabledColor = new Color(.55f, .55f, .55f, .85f); button.colors = colors;
            var text = Text(rect, "Label", label, font, 38, true, TextAlignmentOptions.Midline); text.color = Color.white;
            // Alpha bounds: primary x=11..310/310, secondary x=10..306/306.
            // Center on the visible frame and slightly below its protruding top rail.
            float frameShift = primary ? 5.5f / 310 : 5f / 306;
            text.rectTransform.anchorMin = new Vector2(frameShift, -.03f);
            text.rectTransform.anchorMax = new Vector2(1 + frameShift, .97f);
            text.rectTransform.offsetMin = new Vector2(72, 10); text.rectTransform.offsetMax = new Vector2(-72, -10);
            var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .9f); shadow.effectDistance = new Vector2(0, -3);
            button.onClick.AddListener(() => click()); return button;
        }
        public static Button Link(Transform parent, string name, string label, TMP_FontAsset font, Action click, float size = 24, bool bold = false)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>();
            var text = Text(rect, "Label", label, font, size, bold, TextAlignmentOptions.Center); button.targetGraphic = text;
            button.onClick.AddListener(() => click()); return button;
        }
        public static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.color = bold ? new Color(.96f, .97f, .95f) : new Color(.77f, .83f, .85f);
            text.text = value; text.alignment = align; text.richText = false; text.raycastTarget = false; return text;
        }
        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; return rect;
        }
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
