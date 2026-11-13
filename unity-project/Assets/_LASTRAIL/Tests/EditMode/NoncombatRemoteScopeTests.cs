using System;
using System.Reflection;
using System.Threading;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class NoncombatRemoteScopeTests
    {
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        [Test]
        public void ClosingPendingPageCancelsOpeningAndReturnsToOwnerOnce()
        {
            var root=new GameObject("PendingPage");var page=root.AddComponent<LobbyDestinationPages>();
            using(var request=new CancellationTokenSource())
            try
            {
                int closed=0;Set(page,"closed",(Action)(()=>closed++));Set(page,"pageResourceRequest",request);
                page.Close();page.Close();
                Assert.IsTrue(request.IsCancellationRequested);Assert.AreEqual(1,closed);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void ClosingPageClearsHiddenVisualReferencesAndContent()
        {
            var root=new GameObject("Page",typeof(RectTransform));var page=root.AddComponent<LobbyDestinationPages>();
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(root.transform);
            var background=new GameObject("Background",typeof(RectTransform),typeof(RawImage));background.transform.SetParent(root.transform);
            var texture=new Texture2D(2,2);
            try
            {
                var image=background.GetComponent<RawImage>();image.texture=texture;
                Set(page,"content",content.GetComponent<RectTransform>());Set(page,"backdrop",image);Set(page,"art",texture);
                page.Close();
                Assert.IsNull(image.texture);Assert.IsTrue(content==null);
                Assert.IsNull(typeof(LobbyDestinationPages).GetField("art",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(page));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(texture);}
        }
        [Test]
        public void ClosingDialogCancelsPendingOpenWithoutBuildingAView()
        {
            var root=new GameObject("PendingDialog");var deck=root.AddComponent<LobbyCommandDeck>();
            using(var request=new CancellationTokenSource())
            try
            {
                Set(deck,"resourceDialogRequest",request);deck.CloseDialog();
                Assert.IsTrue(request.IsCancellationRequested);Assert.IsFalse(deck.IsDialogOpen);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
