using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Captures presentation coordinates once. Reflow never reparents inputs or changes their owners.
    internal sealed class LandscapeSectionLayout
    {
        readonly Dictionary<RectTransform, Rect> original = new Dictionary<RectTransform, Rect>();
        public void Apply(Transform root, Func<string, Rect, Rect> arrange, bool scaleChildren = true)
        {
            foreach (RectTransform child in root)
            {
                if (child.anchorMin != child.anchorMax) continue;
                if (!original.TryGetValue(child, out var source))
                {
                    source = new Rect(child.anchoredPosition.x, -child.anchoredPosition.y, child.sizeDelta.x, child.sizeDelta.y);
                    original.Add(child, source);
                }
                var target = arrange(child.name, source);
                if (child.GetComponent<Button>() != null) target.height = Mathf.Max(44, target.height);
                child.anchorMin = child.anchorMax = child.pivot = new Vector2(0, 1);
                child.anchoredPosition = new Vector2(target.x, -target.y);
                // Scale sections uniformly, preserving text, hit geometry, masks and their local children.
                float scale = scaleChildren && source.width > 0 ? target.width / source.width : 1;
                child.localScale = Vector3.one * scale;
                child.sizeDelta = new Vector2(scaleChildren ? source.width : target.width, scale > 0 ? target.height / scale : source.height);
            }
        }
        public static Rect Section(Rect source, float sourceTop, float left, float top, float scale = .8f)
            => new Rect(left + source.x * scale, top + (source.y - sourceTop) * scale, source.width * scale, source.height * scale);
    }
}
