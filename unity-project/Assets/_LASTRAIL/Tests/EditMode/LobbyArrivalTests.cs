using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.Startup;
using Object = UnityEngine.Object;

namespace LastRail.Tests.EditMode
{
    public class LobbyArrivalTests
    {
        [Test]
        public void ReducedMotionUsesSteadySuccessfulLighting()
        {
            var root=new GameObject("ReducedArrival",typeof(RectTransform));
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,true); motion.ReducedMotion=true; motion.Show(true,true); motion.Evaluate(0);
                RawImage light=null;
                foreach(var image in root.GetComponentsInChildren<RawImage>()) if(image.name.Contains("illumination")) light=image;
                var accepted=light.color; motion.Evaluate(3);
                Assert.That(light.color,Is.EqualTo(accepted),"Reduced-motion success must be steady, not a brightness ramp.");
            } finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void CancellingIlluminationDoesNotReleaseSuccessfulReveal()
        {
            var type=typeof(StartupMotionBackdrop).Assembly.GetType("LastRail.Presentation.Startup.LobbyArrivalTransition");
            var root=new GameObject("CancelArrival",typeof(RectTransform));
            var cancel=new CancellationTokenSource();
            try {
                var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,null});
                var task=(Task)type.GetMethod("IlluminateAsync").Invoke(view,new object[]{cancel.Token});
                type.GetMethod("Evaluate").Invoke(view,new object[]{1f});
                cancel.Cancel(); type.GetMethod("Evaluate").Invoke(view,new object[]{3f});
                Assert.That(task.IsCanceled,Is.True);
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            } finally { cancel.Dispose(); Object.DestroyImmediate(root); }
        }

        [Test]
        public void RouteIlluminatesSpatiallyAfterAcceptedSuccess()
        {
            var root=new GameObject("RouteTest",typeof(RectTransform));
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,true);
                RawImage route=null;
                foreach(var image in root.GetComponentsInChildren<RawImage>()) if(image.name.Contains("route-emissive")) route=image;
                motion.Show(true,true); motion.Evaluate(.4f);
                Assert.That(route.color.b,Is.GreaterThan(route.color.r));
                Assert.That(route.uvRect.width,Is.InRange(.35f,.65f),"Reveal part of the route, not the whole mask at once.");
                motion.Evaluate(.4f); Assert.That(route.uvRect.width,Is.EqualTo(1).Within(.001));
                motion.Show(true,false); motion.Evaluate(0); Assert.That(route.color.a,Is.Zero);
            } finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ArrivalHoldsRevealUntilLightingFinishesAndCancellationUnblocksInput()
        {
            var type=typeof(StartupMotionBackdrop).Assembly.GetType("LastRail.Presentation.Startup.LobbyArrivalTransition");
            Assert.That(type,Is.Not.Null,"The guarded arrival presentation must exist.");
            var root=new GameObject("ArrivalTest",typeof(RectTransform));
            var cancel=new CancellationTokenSource();
            try {
                var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,null});
                var task=(Task)type.GetMethod("IlluminateAsync").Invoke(view,new object[]{cancel.Token});
                type.GetMethod("Evaluate").Invoke(view,new object[]{1.9f});
                Assert.That(task.IsCompleted,Is.False,"Do not reveal Main before opaque blue cover.");
                type.GetMethod("Evaluate").Invoke(view,new object[]{.1f});
                Assert.That(task.IsCompletedSuccessfully,Is.True);
                var fade=(Task)type.GetMethod("RevealAsync").Invoke(view,new object[]{cancel.Token});
                type.GetMethod("Evaluate").Invoke(view,new object[]{.3f});
                Assert.That(fade.IsCompleted,Is.False);
                cancel.Cancel(); type.GetMethod("Evaluate").Invoke(view,new object[]{0f});
                Assert.That(fade.IsCanceled,Is.True);
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            } finally { cancel.Dispose(); Object.DestroyImmediate(root); }
        }
    }
}
