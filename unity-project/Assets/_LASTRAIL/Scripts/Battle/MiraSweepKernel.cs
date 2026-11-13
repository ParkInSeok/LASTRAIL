using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace LastRail.Battle
{
    /// <summary>
    /// Pure continuous contact against linearly translated, fixed-shape targets.
    /// Coordinates and dimensions are millimetres; times are normalized to the segment.
    /// Every input component/dimension is bounded to +/-1,000,000,000 mm (dimensions
    /// positive, radii nonnegative). This computational bound is not gameplay geometry.
    /// Integer coefficients, rational slab boundaries and discriminant signs are exact;
    /// only the returned contact time and square root use IEEE double precision.
    /// </summary>
    public static class MiraSweepKernel
    {
        const long LimitMm = 1000000000;

        public static double? Capsule(long[] start, long[] end, long[] targetStartFeet,
            long[] targetEndFeet, long heightMm, long targetRadiusMm, long projectileRadiusMm)
        {
            ValidateVector(start, nameof(start)); ValidateVector(end, nameof(end));
            ValidateVector(targetStartFeet, nameof(targetStartFeet)); ValidateVector(targetEndFeet, nameof(targetEndFeet));
            ValidateDimension(heightMm, false, nameof(heightMm));
            ValidateDimension(targetRadiusMm, true, nameof(targetRadiusMm));
            ValidateDimension(projectileRadiusMm, true, nameof(projectileRadiusMm));
            if (heightMm < 2 * targetRadiusMm) throw new ArgumentException("Capsule height must contain both caps.", nameof(heightMm));
            Relative(start, end, targetStartFeet, targetEndFeet, out var origin, out var velocity);
            // A capsule is a sphere swept along the vertical cap-center segment.
            var lower = new BigInteger[] { 0, 2 * targetRadiusMm, 0 };
            var upper = new BigInteger[] { 0, 2 * (heightMm - targetRadiusMm), 0 };
            return Contact(origin, velocity, lower, upper, 2 * (targetRadiusMm + projectileRadiusMm));
        }

        public static double? Box(long[] start, long[] end, long[] boxStartCenter,
            long[] boxEndCenter, long[] boxSizeMm, long projectileRadiusMm)
        {
            ValidateVector(start, nameof(start)); ValidateVector(end, nameof(end));
            ValidateVector(boxStartCenter, nameof(boxStartCenter)); ValidateVector(boxEndCenter, nameof(boxEndCenter));
            ValidateVector(boxSizeMm, nameof(boxSizeMm));
            ValidateDimension(projectileRadiusMm, true, nameof(projectileRadiusMm));
            var lower = new BigInteger[3]; var upper = new BigInteger[3];
            for (int i = 0; i < 3; i++)
            {
                ValidateDimension(boxSizeMm[i], false, nameof(boxSizeMm));
                // All geometry is doubled, so odd-sized boxes retain half-mm faces.
                lower[i] = -boxSizeMm[i]; upper[i] = boxSizeMm[i];
            }
            Relative(start, end, boxStartCenter, boxEndCenter, out var origin, out var velocity);
            return Contact(origin, velocity, lower, upper, 2 * projectileRadiusMm);
        }

        // Exact rational coordinates share one scale. No endpoint is rounded to an mm grid.
        // Upper scale and original coordinate bounds keep double root conversions finite.
        static void ScaledVectors(BigInteger scale,params BigInteger[][] vectors)
        {
            if(scale<=0 || scale>BigInteger.Pow(10,50))throw new ArgumentException("Continuous coordinate scale.");
            foreach(var v in vectors)
                if(v==null || v.Length!=3 || v.Any(x=>BigInteger.Abs(x)>LimitMm*scale))
                    throw new ArgumentException("Continuous coordinate bounds.");
        }
        static void RelativeScaled(BigInteger[] start,BigInteger[] end,BigInteger[] targetStart,BigInteger[] targetEnd,
            out BigInteger[] origin,out BigInteger[] velocity)
        {
            origin=start.Select((x,i)=>2*(x-targetStart[i])).ToArray();
            var from=origin;velocity=end.Select((x,i)=>2*(x-targetEnd[i])-from[i]).ToArray();
        }
        internal static double? CapsuleScaled(BigInteger[] start,BigInteger[] end,BigInteger[] targetStartFeet,
            BigInteger[] targetEndFeet,long heightMm,long targetRadiusMm,long projectileRadiusMm,BigInteger scale)
        {
            ScaledVectors(scale,start,end,targetStartFeet,targetEndFeet);
            ValidateDimension(heightMm,false,nameof(heightMm));ValidateDimension(targetRadiusMm,true,nameof(targetRadiusMm));
            ValidateDimension(projectileRadiusMm,true,nameof(projectileRadiusMm));
            if(heightMm<2*targetRadiusMm)throw new ArgumentException("Capsule height.");
            RelativeScaled(start,end,targetStartFeet,targetEndFeet,out var origin,out var velocity);
            return Contact(origin,velocity,new BigInteger[]{0,2*targetRadiusMm*scale,0},
                new BigInteger[]{0,2*(heightMm-targetRadiusMm)*scale,0},2*(targetRadiusMm+projectileRadiusMm)*scale);
        }
        internal static double? BoxScaled(BigInteger[] start,BigInteger[] end,BigInteger[] boxStartCenter,
            BigInteger[] boxEndCenter,long[] boxSizeMm,long projectileRadiusMm,BigInteger scale)
        {
            ScaledVectors(scale,start,end,boxStartCenter,boxEndCenter);ValidateVector(boxSizeMm,nameof(boxSizeMm));
            foreach(var x in boxSizeMm)ValidateDimension(x,false,nameof(boxSizeMm));
            ValidateDimension(projectileRadiusMm,true,nameof(projectileRadiusMm));
            RelativeScaled(start,end,boxStartCenter,boxEndCenter,out var origin,out var velocity);
            return Contact(origin,velocity,boxSizeMm.Select(x=>-x*scale).ToArray(),
                boxSizeMm.Select(x=>x*scale).ToArray(),2*projectileRadiusMm*scale);
        }
        static void ValidateVector(long[] value, string name)
        {
            if (value == null || value.Length != 3) throw new ArgumentException("Expected exactly three millimetre components.", name);
            foreach (long component in value)
                if (component < -LimitMm || component > LimitMm) throw new ArgumentException("Coordinate exceeds the supported computational range.", name);
        }
        static void ValidateDimension(long value, bool zeroAllowed, string name)
        {
            if (value < (zeroAllowed ? 0 : 1) || value > LimitMm)
                throw new ArgumentException("Dimension is outside the supported computational range.", name);
        }
        static void Relative(long[] start, long[] end, long[] targetStart, long[] targetEnd,
            out BigInteger[] origin, out BigInteger[] velocity)
        {
            origin = new BigInteger[3]; velocity = new BigInteger[3];
            for (int i = 0; i < 3; i++)
            {
                origin[i] = 2 * (start[i] - targetStart[i]);
                velocity[i] = 2 * (end[i] - targetEnd[i]) - origin[i];
            }
        }

        // Between successive face crossings, squared distance to the AABB (or cap
        // center segment) is one quadratic. Its rounded edge/corner contact is exact.
        static double? Contact(BigInteger[] origin, BigInteger[] velocity, BigInteger[] lower, BigInteger[] upper, BigInteger radius)
        {
            var cuts = new List<Fraction> { Fraction.Zero, Fraction.One };
            for (int axis = 0; axis < 3; axis++)
            {
                if (velocity[axis] == 0) continue;
                AddCut(cuts, new Fraction(lower[axis] - origin[axis], velocity[axis]));
                AddCut(cuts, new Fraction(upper[axis] - origin[axis], velocity[axis]));
            }
            cuts.Sort((a, b) => a.CompareTo(b));
            for (int i = 0; i < cuts.Count - 1; i++)
            {
                var low = cuts[i]; var high = cuts[i + 1];
                if (low.CompareTo(high) == 0) continue;
                var middle = new Fraction(low.N * high.D + high.N * low.D, 2 * low.D * high.D);
                BigInteger a = 0, b = 0, c = -(BigInteger)radius * radius;
                for (int axis = 0; axis < 3; axis++)
                {
                    BigInteger position = origin[axis] * middle.D + velocity[axis] * middle.N;
                    BigInteger offset;
                    if (position < lower[axis] * middle.D) offset = origin[axis] - lower[axis];
                    else if (position > upper[axis] * middle.D) offset = origin[axis] - upper[axis];
                    else continue;
                    a += (BigInteger)velocity[axis] * velocity[axis];
                    b += 2 * (BigInteger)offset * velocity[axis];
                    c += (BigInteger)offset * offset;
                }
                var result = FirstRoot(a, b, c, low, high);
                if (result.HasValue) return result;
            }
            return null;
        }
        static void AddCut(List<Fraction> cuts, Fraction cut)
        {
            if (cut.CompareTo(Fraction.Zero) > 0 && cut.CompareTo(Fraction.One) < 0) cuts.Add(cut);
        }
        static BigInteger Evaluate(BigInteger a, BigInteger b, BigInteger c, Fraction t)
            => a * t.N * t.N + b * t.N * t.D + c * t.D * t.D;

        static double? FirstRoot(BigInteger a, BigInteger b, BigInteger c, Fraction low, Fraction high)
        {
            if (Evaluate(a, b, c, low).Sign <= 0) return low.ToDouble();
            if (a.IsZero) return null; // Squared-distance coefficients cannot form a nonconstant linear function.
            var minimum = new Fraction(-b, 2 * a);
            if (minimum.CompareTo(low) <= 0) return null;
            if (minimum.CompareTo(high) > 0) minimum = high;
            if (Evaluate(a, b, c, minimum).Sign > 0) return null;
            BigInteger discriminant = b * b - 4 * a * c;
            if (discriminant.Sign < 0) return null;
            double root;
            if (discriminant.IsZero) root = new Fraction(-b, 2 * a).ToDouble();
            else
            {
                double sqrt = Math.Sqrt((double)discriminant);
                // Stable lower quadratic root; exact sign checks above establish that
                // the root belongs to this interval before floating point conversion.
                root = b.Sign < 0 ? (2 * (double)c) / (-(double)b + sqrt)
                    : (-(double)b - sqrt) / (2 * (double)a);
            }
            // Only conversion rounding can move an established root past a boundary.
            return Math.Max(low.ToDouble(), Math.Min(high.ToDouble(), root));
        }

        readonly struct Fraction : IComparable<Fraction>
        {
            public readonly BigInteger N;
            public readonly BigInteger D;
            public static readonly Fraction Zero = new Fraction(0, 1);
            public static readonly Fraction One = new Fraction(1, 1);
            public Fraction(BigInteger numerator, BigInteger denominator)
            {
                if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
                N = numerator; D = denominator;
            }
            public int CompareTo(Fraction other) => (N * other.D).CompareTo(other.N * D);
            public double ToDouble() => (double)N / (double)D;
        }
    }
}