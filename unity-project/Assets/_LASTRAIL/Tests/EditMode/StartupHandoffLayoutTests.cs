using LastRail.Presentation.Startup;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public class StartupHandoffLayoutTests
    {
        [Test]
        public void CanvasResizeIsReflectedBeforeFirstRenderWithoutAnotherUpdate()
        {
            var root=new GameObject("HandoffCanvas",typeof(RectTransform),typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1600,900);
                var motion=StartupMotionBackdrop.Attach(root.transform,true);motion.Evaluate(0);
                var art=(RectTransform)motion.transform.Find("Artboard");
                Assert.That(art.rect.size,Is.EqualTo(new Vector2(1600,900)));Assert.That(art.localScale,Is.EqualTo(Vector3.one));
                rect.sizeDelta=new Vector2(2400,1350);
                Canvas.ForceUpdateCanvases();
                Assert.That(art.rect.size,Is.EqualTo(new Vector2(2400,1350)),"Anchored landscape art must resize before rendering, without an Update.");
                Assert.That(art.localScale,Is.EqualTo(Vector3.one));
                var before=art.localScale;
                motion.Evaluate(0);
                Assert.That(art.localScale,Is.EqualTo(before),"Next Update must not snap the first-frame scale.");
            }
            finally {Object.DestroyImmediate(root);}
        }
    }
}
