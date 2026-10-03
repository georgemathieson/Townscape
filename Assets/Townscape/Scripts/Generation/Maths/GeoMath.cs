using System;
using System.Numerics;

namespace Townscape.Generation.Maths
{
    /// <summary>
    /// Small helpers shared by the generators. 2D vectors are positions on the ground plane:
    /// <c>Vector2.X</c> is world x (east) and <c>Vector2.Y</c> is world z (north).
    /// </summary>
    public static class GeoMath
    {
        public static float Lerp(float a, float b, float t) => a + ((b - a) * t);

        public static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static float InverseLerp(float a, float b, float value) =>
            System.Math.Abs(b - a) < 1e-12f ? 0f : Clamp01((value - a) / (b - a));

        /// <summary>Hermite smoothstep: 0 at or below <paramref name="edge0"/>, 1 at or above <paramref name="edge1"/>.</summary>
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = InverseLerp(edge0, edge1, x);
            return t * t * (3f - (2f * t));
        }

        /// <summary>Lifts a ground-plane point to 3D at height <paramref name="y"/>.</summary>
        public static Vector3 At(Vector2 groundPoint, float y) => new Vector3(groundPoint.X, y, groundPoint.Y);

        /// <summary>Drops a 3D point onto the ground plane.</summary>
        public static Vector2 Flat(Vector3 point) => new Vector2(point.X, point.Z);

        /// <summary>The direction 90 degrees to the left of <paramref name="direction"/> when seen from above.</summary>
        public static Vector2 Left(Vector2 direction) => new Vector2(-direction.Y, direction.X);

        /// <summary>Distance along the largest axis (a square "radius" around the origin).</summary>
        public static float ChebyshevLength(Vector2 p) => MathF.Max(MathF.Abs(p.X), MathF.Abs(p.Y));

        public static Vector2 SafeNormalize(Vector2 v, Vector2 fallback)
        {
            var length = v.Length();
            return length > 1e-6f ? v / length : fallback;
        }
    }
}
