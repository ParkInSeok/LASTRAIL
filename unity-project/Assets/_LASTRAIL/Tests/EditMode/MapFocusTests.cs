using System.Linq;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public class MapFocusTests
    {
        [Test]
        public void FinalZoomCentersTheMapAndReducedMotionKeepsItStill()
        {
            var root=new GameObject("MapFocusTest",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,true);
                var focus=typeof(StartupMotionBackdrop).GetMethod("SetMapFocus");
                Assert.That(focus,Is.Not.Null,"Arrival must target the actual map.");
                var map=root.GetComponentsInChildren<RawImage>().Single(x=>x.name=="login-map-base-v01");
                focus.Invoke(motion,new object[]{.5f}); motion.Evaluate(0);
                var midway=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,map.transform);
                Assert.That(Mathf.Abs(midway.center.x),Is.LessThan(50));
                Assert.That(Mathf.Abs(midway.center.y),Is.LessThan(90),"Map should approach screen center throughout zoom, not drift upward first.");
                focus.Invoke(motion,new object[]{1f}); motion.Evaluate(0);
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,map.transform);
                Assert.That(Mathf.Abs(bounds.center.x),Is.LessThan(25)); Assert.That(Mathf.Abs(bounds.center.y),Is.LessThan(35));
                Assert.That(typeof(StartupMotionBackdrop).Assembly.GetType("LastRail.Presentation.Startup.SharedRailMap"),Is.Not.Null);
                Assert.That(bounds.size.y,Is.InRange(1300,1450));
                motion.ReducedMotion=true; motion.Evaluate(0);
                bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,map.transform);
                Assert.That(bounds.size.y,Is.LessThan(150));
            } finally {Object.DestroyImmediate(root);}
        }
        [Test]
        public void LampDipsOnlyDuringResourceLoadingAndStaysSubtle()
        {
            var root=new GameObject("LoadingLampTest",typeof(RectTransform));
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,true);
                var loading=typeof(StartupMotionBackdrop).GetMethod("SetResourceLoading");
                Assert.That(loading,Is.Not.Null);
                var lamp=root.GetComponentsInChildren<RawImage>().Single(x=>x.name=="login-hanging-props-v01");
                motion.Evaluate(1); Assert.That(lamp.color.r,Is.EqualTo(1));
                loading.Invoke(motion,new object[]{true}); motion.Evaluate(1);
                Assert.That(lamp.color.r,Is.InRange(.7f,.9f));
                loading.Invoke(motion,new object[]{false}); motion.Evaluate(0);
                Assert.That(lamp.color.r,Is.EqualTo(1));
                motion.ReducedMotion=true; loading.Invoke(motion,new object[]{true}); motion.Evaluate(1);
                Assert.That(lamp.color.r,Is.EqualTo(1));
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
