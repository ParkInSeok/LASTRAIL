#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Battle
{
    public sealed class CombatStatusHudView
    {
        private readonly RectTransform root;
        private readonly RectTransform[] rows = new RectTransform[2];
        private readonly Image[] icons = new Image[2];
        private readonly TMP_Text[] labels = new TMP_Text[2];
        private readonly Sprite[] sprites;

        public CombatStatusHudView(RectTransform parent, TMP_Text textStyle, Sprite[] sprites)
        {
            this.sprites = sprites;
            root = Rect(parent, "CombatStatusCueHud", 280, -432, 340, 190);
            for (var i = 0; i < rows.Length; i++)
            {
                rows[i] = Rect(root, "StatusCueRow_" + i, 0, 46 - i * 92, 340, 88);
                icons[i] = Icon(rows[i], "StatusIcon", null, -120, 0, 80, 80);
                labels[i] = Label(rows[i], "StatusLabel", textStyle, 48, 0, 220, 80);
            }
            root.gameObject.SetActive(false);

            // No cooldown, availability, target or magnitude fields exist in the input state.
            // Keep prepared art and fill/text layers inactive until a real producer is connected.
            var pending = Rect(parent, "UnconnectedCombatStatusLayers", 280, -432, 340, 190);
            var cooldown = Icon(pending, "CooldownIcon", SpriteAt(4), -120, 0, 80, 80);
            var track = Icon(cooldown.transform, "CooldownTrack", SpriteAt(8), 120, -25, 160, 16);
            var fill = Icon(track.transform, "CooldownFill", SpriteAt(9), 0, 0, 160, 16);
            track.preserveAspect = false;
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0;
            Label(cooldown.transform, "CooldownValue", textStyle, 120, 14, 160, 44).text = string.Empty;
            Icon(pending, "AvailableIcon", SpriteAt(5), 0, 0, 80, 80);
            Icon(pending, "UnavailableIcon", SpriteAt(6), 0, 0, 80, 80);
            Icon(pending, "SelectedTargetIcon", SpriteAt(7), 0, 0, 100, 100);
            pending.gameObject.SetActive(false);
        }

        public void Render(IReadOnlyList<string> cueIds)
        {
            var visible = 0;
            for (var i = 0; i < rows.Length; i++)
            {
                var index = i < cueIds.Count ? CombatStatusIconCatalog.CueIconIndex(cueIds[i]) : -1;
                var sprite = SpriteAt(index);
                var show = index >= 0 && sprite != null;
                rows[i].gameObject.SetActive(show);
                icons[i].sprite = show ? sprite : null;
                labels[i].text = show ? CueLabel(index) : string.Empty;
                if (show) visible++;
            }
            root.gameObject.SetActive(visible > 0);
        }

        private Sprite SpriteAt(int index) => sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;

        private static string CueLabel(int index)
        {
            switch (index)
            {
                case 0: return "\uBCF4\uD638\uB9C9 \uC5F0\uCD9C";
                case 1: return "\uD68C\uBCF5 \uC5F0\uCD9C";
                case 2: return "\uBC84\uD504 \uC5F0\uCD9C";
                default: return "\uB514\uBC84\uD504 \uC5F0\uCD9C";
            }
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Image Icon(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Label(Transform parent, string name, TMP_Text style, float x, float y, float width, float height)
        {
            var label = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null) label.font = style.font;
            label.fontSize = 30;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }
    }
}
#endif
