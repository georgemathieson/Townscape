using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>Tarmac carriageway at road level with raised stone pavements either side.</summary>
    public sealed class RoadFeature : IGroundFeature
    {
        public const float RoadHeight = 0f;
        public const float PavementHeight = 0.14f;

        private static readonly ISurfaceRegion Carriageway =
            new FlatRegion(RegionKind.Road, SurfaceMaterial.Road, SurfaceMaterial.Kerb, RoadHeight);

        private static readonly ISurfaceRegion Pavement =
            new FlatRegion(RegionKind.Pavement, SurfaceMaterial.Pavement, SurfaceMaterial.Kerb, PavementHeight);

        private readonly RoadSpec _road;
        private readonly float _outer;

        public RoadFeature(RoadSpec road)
        {
            _road = road;
            _outer = road.HalfWidth + road.PavementWidth;
            Bounds = road.Centre.Bounds.Expand(_outer + 2f);
        }

        public Rect2 Bounds { get; }

        public bool TryClaim(Vector2 p, out SurfaceClaim claim)
        {
            claim = default;
            if (!Bounds.Contains(p))
            {
                return false;
            }

            var distance = _road.Centre.Closest(p).Distance;
            if (distance < _road.HalfWidth)
            {
                claim = new SurfaceClaim(Carriageway, GroundPriority.Road);
                return true;
            }

            if (_road.PavementWidth > 0f && distance < _outer)
            {
                claim = new SurfaceClaim(Pavement, GroundPriority.Pavement);
                return true;
            }

            return false;
        }

        public float DistanceToEdge(Vector2 p) => _road.Centre.Closest(p).Distance - _outer;

        public bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit)
        {
            hit = default;
            if (!Bounds.Contains(p))
            {
                return false;
            }

            var closest = _road.Centre.Closest(p);
            var found = false;
            ContourSnapping.ConsiderOffsetContour(p, closest, _road.HalfWidth, 0, maxDistance, ref found, ref hit);
            if (_road.PavementWidth > 0f)
            {
                ContourSnapping.ConsiderOffsetContour(p, closest, _outer, 1, maxDistance, ref found, ref hit);
            }

            return found;
        }
    }
}
