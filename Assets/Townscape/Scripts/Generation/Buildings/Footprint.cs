using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Buildings
{
    /// <summary>
    /// A building's four ground corners. "Left" and "right" are as seen from the street, looking
    /// at the front. Terraced units on a curved street are slightly wedge-shaped, so this is a
    /// general quadrilateral rather than a rectangle.
    /// </summary>
    public sealed class Footprint
    {
        public Footprint(Vector2 frontLeft, Vector2 frontRight, Vector2 backRight, Vector2 backLeft)
        {
            FrontLeft = frontLeft;
            FrontRight = frontRight;
            BackRight = backRight;
            BackLeft = backLeft;
        }

        public Vector2 FrontLeft { get; }

        public Vector2 FrontRight { get; }

        public Vector2 BackRight { get; }

        public Vector2 BackLeft { get; }

        public Vector2[] Corners => new[] { FrontLeft, FrontRight, BackRight, BackLeft };

        public float FrontWidth => Vector2.Distance(FrontLeft, FrontRight);

        public float Depth => (Vector2.Distance(FrontLeft, BackLeft) + Vector2.Distance(FrontRight, BackRight)) * 0.5f;

        public Vector2 Centre => (FrontLeft + FrontRight + BackRight + BackLeft) * 0.25f;

        /// <summary>Unit direction out of the front, towards the street.</summary>
        public Vector2 Outward
        {
            get
            {
                var right = Vector2.Normalize(FrontRight - FrontLeft);
                return new Vector2(right.Y, -right.X);
            }
        }

        public WallFrame FrontWall => WallFrame.FromBase(FrontLeft, FrontRight);

        public WallFrame RightWall => WallFrame.FromBase(FrontRight, BackRight);

        public WallFrame BackWall => WallFrame.FromBase(BackRight, BackLeft);

        public WallFrame LeftWall => WallFrame.FromBase(BackLeft, FrontLeft);

        /// <summary>
        /// Bilinear point: <paramref name="a"/> runs 0 (left) to 1 (right), <paramref name="b"/> runs
        /// 0 (front) to 1 (back). Values outside [0, 1] extrapolate, which is how roofs overhang.
        /// </summary>
        public Vector2 At(float a, float b)
        {
            var front = Vector2.Lerp(FrontLeft, FrontRight, a);
            var back = Vector2.Lerp(BackLeft, BackRight, a);
            return Vector2.Lerp(front, back, b);
        }

        /// <summary>A rectangle centred on <paramref name="frontCentre"/>, facing <paramref name="outward"/>.</summary>
        public static Footprint FromFront(Vector2 frontCentre, Vector2 outward, float width, float depth)
        {
            var normal = Vector2.Normalize(outward);
            var right = GeoMath.Left(normal) * (width * 0.5f);
            var frontLeft = frontCentre - right;
            var frontRight = frontCentre + right;
            return new Footprint(frontLeft, frontRight, frontRight - (normal * depth), frontLeft - (normal * depth));
        }
    }
}
