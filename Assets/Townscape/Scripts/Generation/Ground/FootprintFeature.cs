using System;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>
    /// An oriented rectangle where the ground is left open because a structure supplies its own
    /// surface, like the bridge deck and its approach ramps. The river still wins inside it, so the
    /// water and river bed stay visible under the arch.
    /// </summary>
    public sealed class FootprintFeature : IGroundFeature
    {
        private static readonly ISurfaceRegion Hole =
            new FlatRegion(RegionKind.Hole, SurfaceMaterial.Earth, SurfaceMaterial.Earth, 0f);

        private readonly Vector2 _centre;
        private readonly Vector2 _along;
        private readonly Vector2 _across;
        private readonly float _halfLength;
        private readonly float _halfWidth;

        public FootprintFeature(Vector2 centre, Vector2 direction, float halfLength, float halfWidth)
        {
            _centre = centre;
            _along = Vector2.Normalize(direction);
            _across = GeoMath.Left(_along);
            _halfLength = halfLength;
            _halfWidth = halfWidth;

            var reach = new Vector2(
                (MathF.Abs(_along.X) * halfLength) + (MathF.Abs(_across.X) * halfWidth),
                (MathF.Abs(_along.Y) * halfLength) + (MathF.Abs(_across.Y) * halfWidth));
            Bounds = new Rect2(centre - reach, centre + reach).Expand(2f);
        }

        public Rect2 Bounds { get; }

        public bool TryClaim(Vector2 p, out SurfaceClaim claim)
        {
            claim = default;
            var local = ToLocal(p);
            if (MathF.Abs(local.X) >= _halfLength || MathF.Abs(local.Y) >= _halfWidth)
            {
                return false;
            }

            claim = new SurfaceClaim(Hole, GroundPriority.StructureFootprint);
            return true;
        }

        public float DistanceToEdge(Vector2 p)
        {
            var local = ToLocal(p);
            var dx = MathF.Abs(local.X) - _halfLength;
            var dy = MathF.Abs(local.Y) - _halfWidth;
            var outside = new Vector2(MathF.Max(dx, 0f), MathF.Max(dy, 0f)).Length();
            var inside = MathF.Min(MathF.Max(dx, dy), 0f);
            return outside + inside;
        }

        public bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit)
        {
            hit = default;
            var local = ToLocal(p);
            var nearest = local;
            var insideX = MathF.Abs(local.X) <= _halfLength;
            var insideY = MathF.Abs(local.Y) <= _halfWidth;

            if (insideX && insideY)
            {
                // Push out to whichever side is closest.
                if (_halfLength - MathF.Abs(local.X) < _halfWidth - MathF.Abs(local.Y))
                {
                    nearest.X = local.X < 0f ? -_halfLength : _halfLength;
                }
                else
                {
                    nearest.Y = local.Y < 0f ? -_halfWidth : _halfWidth;
                }
            }
            else
            {
                nearest = new Vector2(
                    GeoMath.Clamp(local.X, -_halfLength, _halfLength),
                    GeoMath.Clamp(local.Y, -_halfWidth, _halfWidth));
            }

            var distance = Vector2.Distance(local, nearest);
            if (distance > maxDistance)
            {
                return false;
            }

            hit = new ContourHit(_centre + (_along * nearest.X) + (_across * nearest.Y), distance, 0);
            return true;
        }

        private Vector2 ToLocal(Vector2 p)
        {
            var d = p - _centre;
            return new Vector2(Vector2.Dot(d, _along), Vector2.Dot(d, _across));
        }
    }
}
