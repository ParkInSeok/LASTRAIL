using System.Linq;
using System.Threading;
using LastRail.Presentation.Startup;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public class SharedMapArrivalTests
    {
        [TestCase(720,1280)]
        [TestCase(1080,2400)]
        public void CabinAndLobbyShareTextureLandmarkAndLightSize(int width,int height)
        {
            var root=new GameObject("SharedMapTest",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
            try {
                var motion=StartupMotionBackdrop.Attach(root.transform,true);motion.Show(true,true);motion.SetMapFocus(1);motion.Evaluate(.8f);
                var cabin=motion.GetComponentInChildren<SharedRailMap>();
                var lobby=SharedRailMap.Create(root.transform);lobby.ConfigureLobby();
                Assert.That(lobby.Map.texture,Is.SameAs(cabin.Map.texture));
                Assert.That(lobby.Map.texture.width,Is.GreaterThanOrEqualTo(1400));
                var a=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,cabin.Map.transform);
                var b=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,lobby.Map.transform);
                Assert.That(Vector3.Distance(a.center,b.center),Is.LessThan(.1f));
                Assert.That(Vector3.Distance(a.size,b.size),Is.LessThan(.1f));
                var an=cabin.GetComponentsInChildren<RawImage>().Single(x=>x.name=="RailwayJunctionLight");
                var bn=lobby.GetComponentsInChildren<RawImage>().Single(x=>x.name=="RailwayJunctionLight");
                a=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,an.transform);
                b=RectTransformUtility.CalculateRelativeRectTransformBounds(root.transform,bn.transform);
                Assert.That(Vector3.Distance(a.center,b.center),Is.LessThan(.1f));
                Assert.That(Vector3.Distance(a.size,b.size),Is.LessThan(.1f));
            } finally {Object.DestroyImmediate(root);}
        }
        [Test]
        public void ReducedMotionDissolvesCabinInsteadOfHardCutting()
        {
            var root=new GameObject("ReducedSharedMap",typeof(RectTransform));
            try {
                var previous=StartupMotionBackdrop.Attach(root.transform,true);previous.ReducedMotion=true;
                var transition=LobbyArrivalTransition.Create(root.transform,previous);
                transition.IlluminateAsync(CancellationToken.None);transition.Evaluate(2);
                transition.RevealAsync(CancellationToken.None);transition.Evaluate(.25f);
                Assert.That(transition.GetComponentInChildren<StartupMotionBackdrop>(),Is.Not.Null);
                Assert.That(transition.GetComponent<CanvasGroup>().alpha,Is.InRange(.4f,.6f));
                transition.Evaluate(.25f);Assert.That(transition.GetComponent<CanvasGroup>().alpha,Is.Zero);
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
