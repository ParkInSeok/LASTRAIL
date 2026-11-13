#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    // Literal fixtures catch end-frame-only checks, square corners, wrong capsule caps,
    // open boundary tests and nearest-center selection instead of first contact.
    public class MiraSweepKernelTests
    {
        static long[] V(long x, long y, long z) => new[] { x, y, z };
        static double? Call(string method, params object[] args)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("LastRail.Battle.MiraSweepKernel")).FirstOrDefault(t => t != null);
            Assert.IsNotNull(type, "MiraSweepKernel must implement the pure swept-contact contract");
            var member = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(member, method + " must be public and static");
            Assert.AreEqual(typeof(double?), member.ReturnType);
            try { return (double?)member.Invoke(null, args); }
            catch (TargetInvocationException e) { throw e.InnerException; }
        }
        static double? Capsule(long[] a, long[] b, long[] c, long[] d, long h = 1700, long r = 300, long p = 60)
            => Call("Capsule", a, b, c, d, h, r, p);
        static double? Box(long[] a, long[] b, long[] c, long[] d, long[] size, long p = 60)
            => Call("Box", a, b, c, d, size, p);
        static void Time(double? value, double expected)
        {
            Assert.IsTrue(value.HasValue, "Expected contact");
            Assert.That(value.Value, Is.EqualTo(expected).Within(1e-12));
            Assert.That(value.Value, Is.InRange(0d, 1d));
        }

        [TestCase(1700, 300, 360)]
        [TestCase(1800, 350, 410)]
        public void CylinderExactBoundaryIncludesTangency(long height, long radius, long offset)
        {
            Time(Capsule(V(-1000, 900, offset), V(1000, 900, offset), V(0, 0, 0), V(0, 0, 0), height, radius), .5);
            Assert.IsNull(Capsule(V(-1000, 900, offset + 1), V(1000, 900, offset + 1), V(0, 0, 0), V(0, 0, 0), height, radius));
        }
        [Test] public void CylinderReturnsFirstSurfaceContact()
            => Time(Capsule(V(-1000, 900, 0), V(1000, 900, 0), V(0, 0, 0), V(0, 0, 0)), .32);
        [Test] public void CapsuleUsesFeetAndRoundedTopCap()
        {
            Time(Capsule(V(0, 2000, 0), V(0, 1000, 0), V(0, 0, 0), V(0, 0, 0)), .24);
            Assert.IsNull(Capsule(V(350, 1700, 0), V(350, 1700, 0), V(0, 0, 0), V(0, 0, 0)));
            Time(Capsule(V(216, 1688, 0), V(216, 1688, 0), V(0, 0, 0), V(0, 0, 0)), 0);
            Assert.IsNull(Capsule(V(217, 1688, 0), V(217, 1688, 0), V(0, 0, 0), V(0, 0, 0)));
        }
        [Test] public void BottomCapIncludesProjectileBelowFeet()
        {
            Time(Capsule(V(0, -60, 0), V(0, -60, 0), V(0, 0, 0), V(0, 0, 0)), 0);
            Assert.IsNull(Capsule(V(0, -61, 0), V(0, -61, 0), V(0, 0, 0), V(0, 0, 0)));
        }
        [Test] public void DiagonalCapsuleDistanceIsEuclidean()
        {
            Time(Capsule(V(216, 900, 288), V(216, 900, 288), V(0, 0, 0), V(0, 0, 0)), 0);
            Assert.IsNull(Capsule(V(255, 900, 255), V(255, 900, 255), V(0, 0, 0), V(0, 0, 0)));
        }
        [Test] public void TranslatedTargetCrossesStationaryProjectileBetweenFrames()
            => Time(Capsule(V(0, 900, 0), V(0, 900, 0), V(-1000, 0, 0), V(1000, 0, 0)), .32);
        [Test] public void RelativeMotionHandlesBothActorsMoving()
            => Time(Capsule(V(-1000, 900, 0), V(1000, 900, 0), V(1000, 0, 0), V(-1000, 0, 0)), .41);
        [Test] public void CapsuleParallelMotionAndInitialOverlap()
        {
            Assert.IsNull(Capsule(V(1000, 900, 0), V(2000, 900, 0), V(0, 0, 0), V(1000, 0, 0)));
            Time(Capsule(V(0, 900, 0), V(2000, 900, 0), V(0, 0, 0), V(0, 0, 0)), 0);
        }
        [Test] public void CapsuleSegmentEndingOnSurfaceIncludesEndpoint()
            => Time(Capsule(V(-1000, 900, 0), V(-360, 900, 0), V(0, 0, 0), V(0, 0, 0)), 1);
        [Test] public void ThinBoxCannotBeSkippedByEndpointSampling()
            => Time(Box(V(-1000, 0, 0), V(1000, 0, 0), V(0, 0, 0), V(0, 0, 0), V(2, 1000, 1000)), .4695);
        [Test] public void BoxCornersAreRoundRatherThanExpandedSlabs()
        {
            Assert.IsNull(Box(V(150, 150, -1000), V(150, 150, 1000), V(0, 0, 0), V(0, 0, 0), V(200, 200, 200)));
            Time(Box(V(136, 148, -1000), V(136, 148, 1000), V(0, 0, 0), V(0, 0, 0), V(200, 200, 200)), .45);
        }
        [Test] public void BoxRoundedCornerFirstContactIsNotSlabEntry()
            => Time(Box(V(-1000, 136, 148), V(1000, 136, 148), V(0, 0, 0), V(0, 0, 0), V(200, 200, 200)), .45);
        [Test] public void MovingBoxCrossesStationaryProjectile()
            => Time(Box(V(0, 0, 0), V(0, 0, 0), V(-1000, 0, 0), V(1000, 0, 0), V(2, 1000, 1000)), .4695);
        [Test] public void BoxOddSizeUsesExactHalfMillimetres()
            => Time(Box(V(-1000, 0, 0), V(1000, 0, 0), V(0, 0, 0), V(0, 0, 0), V(3, 101, 101)), .46925);
        [Test] public void BoxInitialOverlapAndEndpointContact()
        {
            Time(Box(V(0, 0, 0), V(1000, 0, 0), V(0, 0, 0), V(0, 0, 0), V(200, 200, 200)), 0);
            Time(Box(V(-1000, 0, 0), V(-160, 0, 0), V(0, 0, 0), V(0, 0, 0), V(200, 200, 200)), 1);
        }
        [Test] public void InvalidVectorsAndDimensionsFailInsteadOfBecomingMisses()
        {
            Assert.Throws<ArgumentException>(() => Capsule(null, V(0, 0, 0), V(0, 0, 0), V(0, 0, 0)));
            Assert.Throws<ArgumentException>(() => Capsule(new long[2], V(0, 0, 0), V(0, 0, 0), V(0, 0, 0)));
            Assert.Throws<ArgumentException>(() => Capsule(V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), 599));
            Assert.Throws<ArgumentException>(() => Capsule(V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), 1700, -1));
            Assert.Throws<ArgumentException>(() => Box(V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 20, 20)));
            Assert.Throws<ArgumentException>(() => Box(V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 0, 0), V(20, 20, 20), -1));
            Assert.Throws<ArgumentException>(() => Capsule(V(long.MaxValue, 0, 0), V(0, 0, 0), V(0, 0, 0), V(0, 0, 0)));
        }
    }
}
#endif