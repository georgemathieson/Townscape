using System.Numerics;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    internal static class ContourSnapping
    {
        /// <summary>
        /// Considers the line at distance <paramref name="radius"/> from a centreline as a snap target,
        /// replacing <paramref name="best"/> if it is the closest candidate so far.
        /// </summary>
        public static void ConsiderOffsetContour(
            Vector2 p,
            PolylineHit closest,
            float radius,
            int contourId,
            float maxDistance,
            ref bool found,
            ref ContourHit best)
        {
            if (closest.Distance < 1e-4f)
            {
                return;
            }

            var gap = System.Math.Abs(closest.Distance - radius);
            if (gap > maxDistance || (found && gap >= best.Distance))
            {
                return;
            }

            var outward = (p - closest.Point) / closest.Distance;
            best = new ContourHit(closest.Point + (outward * radius), gap, contourId);
            found = true;
        }
    }
}
