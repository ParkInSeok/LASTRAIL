using System.Linq;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public class CabinLightingTests
    {
        [Test]
        public void SmallShadedLampDoesNotFloodWindowsOrPeriodicallyBlackOut()
        {
            var root=new GameObject("LampTest",typeof(RectTransform));
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,true);
                var light=root.GetComponentsInChildren<RawImage>().Single(x=>x.name.Contains("illumination"));
                motion.Evaluate(1); var idle=light.color;
                motion.Evaluate(6.5f); Assert.That(light.color,Is.EqualTo(idle),"Remove the arbitrary periodic power outage.");
                Assert.That(light.rectTransform.sizeDelta.x,Is.LessThanOrEqualTo(360),"Pool remains inside the central aisle, away from windows.");
                motion.Show(true,true); motion.Evaluate(4);
                Assert.That(light.color.a,Is.LessThanOrEqualTo(.1f),"Existing art already contains direct bulb light.");
            } finally { Object.DestroyImmediate(root); }
        }
    }
}
