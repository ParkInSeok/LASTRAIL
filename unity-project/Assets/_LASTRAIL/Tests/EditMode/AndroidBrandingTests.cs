#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace LastRail.Tests.EditMode
{
    public sealed class AndroidBrandingTests
    {
        [Test]
        public void AndroidIcons_HaveExplicitTexturesInEveryRequiredSlot()
        {
            foreach (var kind in new[] { AndroidPlatformIconKind.Legacy, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Adaptive })
            {
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                Assert.That(icons.Length, Is.GreaterThan(0));
                foreach (var icon in icons)
                {
                    Assert.That(icon.layerCount, Is.GreaterThanOrEqualTo(icon.minLayerCount));
                    for (var layer = 0; layer < icon.minLayerCount; layer++)
                    {
                        Assert.That(icon.GetTexture(layer), Is.Not.Null, kind + " " + icon.width + " layer " + layer);
                        Assert.That(icon.GetTexture(layer).width, Is.EqualTo(icon.width));
                        Assert.That(icon.GetTexture(layer).height, Is.EqualTo(icon.height));
                    }
                }
            }
        }

        [Test]
        public void AdaptiveLayers_UseOpaqueBackgroundAndSafeTransparentForeground()
        {
            foreach (var icon in PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive))
            {
                Assert.That(AssetDatabase.GetAssetPath(icon.GetTexture(0)), Does.Contain("adaptive_background_"));
                Assert.That(AssetDatabase.GetAssetPath(icon.GetTexture(1)), Does.Contain("adaptive_foreground_"));
                var bg = Decode(icon.GetTexture(0));
                var fg = Decode(icon.GetTexture(1));
                try
                {
                    Assert.That(bg.GetPixels32().All(p => p.a == 255), Is.True);
                    var pixels = fg.GetPixels32();
                    Assert.That(pixels.Any(p => p.a > 200), Is.True);
                    Assert.That(pixels.Any(p => p.a == 0), Is.True);
                    for (var y = 0; y < fg.height; y++)
                    for (var x = 0; x < fg.width; x++)
                        if (pixels[y * fg.width + x].a != 0)
                            Assert.That(new Vector2((x + 0.5f) / fg.width - 0.5f, (y + 0.5f) / fg.height - 0.5f).magnitude, Is.LessThan(0.3056f), "Symbol exceeds safe circle");
                }
                finally { Object.DestroyImmediate(bg); Object.DestroyImmediate(fg); }
            }
        }

        [Test]
        public void Splash_HasCandidateSpriteAndPreservesUnityBranding()
        {
            Assert.That(PlayerSettings.SplashScreen.show, Is.True);
            Assert.That(PlayerSettings.SplashScreen.showUnityLogo, Is.True);
            var matches = PlayerSettings.SplashScreen.logos.Where(l => AssetDatabase.GetAssetPath(l.logo) == "Assets/_LASTRAIL/UI/Branding/splash_logo_candidate.png").ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].duration, Is.GreaterThanOrEqualTo(2f));
            Assert.That(matches[0].logo.rect.width, Is.EqualTo(512));
        }

        private static Texture2D Decode(Texture2D asset)
        {
            Assert.That(asset, Is.Not.Null);
            var texture = new Texture2D(2, 2);
            Assert.That(texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(asset))), Is.True);
            return texture;
        }
    }
}
#endif
