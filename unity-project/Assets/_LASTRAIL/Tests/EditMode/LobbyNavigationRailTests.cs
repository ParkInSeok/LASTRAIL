using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.MissionShell;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyNavigationRailTests
    {
        [Test]
        public void LobbyMatchAssetsStayRemoteAndOutsideOfflineBootstrap()
        {
            var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var dependencies=UnityEditor.AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",true);
            foreach(var key in new[]{"LobbyDeck/navigation-shapes-v06","LobbyDeck/frame-v06","LobbyDeck/button-surface-v06","LobbyDeck/commander-rim-v07","LobbyDeck/currency-icons-v07","LobbyDeck/serif-v07"})
            {
                var entry=LastRail.Resources.RemoteContentCatalog.Find(key);Assert.NotNull(entry);Assert.IsFalse(entry.Local);
                Assert.NotNull(UnityEditor.AssetDatabase.LoadAssetAtPath(entry.Path,entry.ResourceType));Assert.IsFalse(dependencies.Contains(entry.Path));
                var address=settings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(entry.Path));
                Assert.NotNull(address);Assert.AreEqual(entry.Address,address.address);
                Assert.IsTrue(address.labels.Contains(LastRail.Resources.AddressableContentBackend.RequiredLabel));
                Assert.AreEqual("remote_resources",address.parentGroup.Name);
            }
        }
        [Test]
        public void RailOwnsOneFrameAndFiveInputsAndStopsOldEntryCallbacks()
        {
            var host = new GameObject("RailTest", typeof(RectTransform));
            var texture = new Texture2D(16, 16);
            var frame = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f);
            int calls = 0, last = -1;
            try
            {
                var rail = LobbyNavigationRail.Create(host.transform, frame, texture, 2, "Navigation", i => { calls++; last = i; });
                Assert.That(rail.GetComponentsInChildren<Button>().Length, Is.EqualTo(5));
                Assert.That(rail.Cast<Transform>().Count(t => t.name == "Divider"), Is.EqualTo(4));
                Assert.That(rail.GetComponentsInChildren<Image>().Count(i => i.sprite != null), Is.EqualTo(1));
                var oldClick = rail.Find("Navigation2").GetComponent<Button>().onClick;
                var material = rail.Find("Navigation2/Icon").GetComponent<RawImage>().material;
                var inactive=rail.Find("Navigation1/Icon").GetComponent<RawImage>();
                Assert.AreEqual(inactive.defaultMaterial,inactive.material,"Unselected icons must retain original metal shading");
                Assert.NotNull(rail.Find("Navigation2/BlueInset"));
                Assert.AreEqual(LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/button-surface-v06"),rail.Find("Navigation2/BlueInset").GetComponent<RawImage>().texture);
                Assert.AreEqual(LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/navigation-shapes-v06"),rail.Find("Navigation4/Icon").GetComponent<RawImage>().texture);
                Assert.IsTrue(rail.Find("Navigation2/BlueInset").GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),"Decoration must not intercept clicks");
                oldClick.Invoke(); Assert.That(calls, Is.EqualTo(1)); Assert.That(last, Is.EqualTo(2));
                rail.gameObject.SetActive(false);
                oldClick.Invoke(); Assert.That(calls, Is.EqualTo(1));
                rail.gameObject.SetActive(true);
                oldClick.Invoke(); Assert.That(calls, Is.EqualTo(2));
                // EditMode does not dispatch this non-ExecuteAlways component's destroy callback.
                typeof(LobbyNavigationRail).GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rail.GetComponent<LobbyNavigationRail>(),null);
                Object.DestroyImmediate(rail.gameObject);
                LobbyNavigationRail.Create(host.transform, frame, texture, 0, "Navigation", i => calls++);
                oldClick.Invoke(); Assert.That(calls, Is.EqualTo(2));
                Assert.That(material == null, Is.True, "Owned material must be released");
                Assert.That(frame != null && texture != null, Is.True, "Borrowed resources stay owned by the screen");
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(frame); Object.DestroyImmediate(texture); }
        }
    }
}
