using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.Startup;

namespace LastRail.Tests.EditMode
{
    public class StartupMotionTests
    {
        [TestCase(false)] [TestCase(true)]
        public void PresentationSurfacesLoadAndDoNotInterceptInput(bool login)
        {
            var root=new GameObject("MotionTest",typeof(RectTransform));
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,login);
                Assert.AreEqual(1,motion.LayerCount);
                if(!login) {
                    var reference=root.GetComponentInChildren<RawImage>();
                    Assert.AreEqual("landscape-first-frame-v01",reference.texture.name);
                    Assert.Greater(reference.texture.width,reference.texture.height);
                    Assert.AreEqual("UI/Default",reference.material.shader.name);
                }
                foreach(var image in root.GetComponentsInChildren<RawImage>()) { Assert.NotNull(image.texture); Assert.IsFalse(image.raycastTarget); }
                if(!login) {
                    motion.Evaluate(1);
                    motion.Evaluate(90);
                    Assert.AreEqual("UI/Default",root.GetComponentInChildren<RawImage>().material.shader.name,"No procedural train motion in the fallback.");
                }
                Assert.IsFalse(motion.Authenticated);
                motion.ReducedMotion=true; motion.Evaluate(2);
                var graphics=root.GetComponentsInChildren<RawImage>();
                var rects=System.Array.ConvertAll(graphics,g=>g.uvRect);
                motion.Evaluate(7);
                for(int i=0;i<graphics.Length;i++) Assert.AreEqual(rects[i],graphics[i].uvRect,"Reduced motion must freeze scrolling/sheet coordinates.");
            } finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void LoginPresentationTracksAuthenticationWithoutOldRouteOverlay()
        {
            var root=new GameObject("MotionTest",typeof(RectTransform));
            try {
                var m=StartupMotionBackdrop.Attach(root.transform,true); m.Evaluate(5);
                Assert.IsFalse(m.Authenticated);
                m.Show(true,true);m.Evaluate(4);Assert.IsTrue(m.Authenticated);
                m.Show(true,false);m.Evaluate(0);Assert.IsFalse(m.Authenticated);
                Assert.That(root.GetComponentsInChildren<RawImage>().Length,Is.EqualTo(1),"Approved login background does not embed an old route overlay.");
            } finally { Object.DestroyImmediate(root); }
        }
    }
}
