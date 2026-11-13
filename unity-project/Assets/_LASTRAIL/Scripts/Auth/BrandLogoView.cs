using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Auth
{
    public static class BrandLogoView
    {
        public const string ResourcePath = "CombatSecondFinal/title_emblem-v01";

        public static RawImage Create(RectTransform container)
        {
            var texture = UnityEngine.Resources.Load<Texture2D>(ResourcePath);
            if (texture == null) throw new InvalidOperationException("Approved brand logo is missing: " + ResourcePath);
            var rect = new GameObject("BrandLogo", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(container, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            var fit = rect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)texture.width / texture.height;
            return image;
        }
    }
}
