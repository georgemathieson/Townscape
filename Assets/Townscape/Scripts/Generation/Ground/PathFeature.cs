using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>A gravel footpath that follows the lie of the land.</summary>
    public sealed class PathFeature : IGroundFeature
    {
        private static readonly ISurfaceRegion Gravel =
            new TerrainFollowingRegion(RegionKind.Path, SurfaceMaterial.Gravel, SurfaceMaterial.Earth, 0.03f);

        private readonly PathSpec _path;
        private readonly float _halfWidth;

        public PathFeature(PathSpec path)
        {
            _path = path;
            _halfWidth = path.Width * 0.5f;
            Bounds = path.Centre.Bounds.Expand(_halfWidth + 2f);
        }

        public Rect2 Bounds { get; }

        public bool TryClaim(Vector2 p, out SurfaceClaim claim)
        {
            claim = default;
            if (!Bounds.Contains(p) || _path.Centre.Closest(p).Distance >= _halfWidth)
            {
                return false;
            }

            claim = new SurfaceClaim(Gravel, GroundPriority.Path);
            return true;
        }

        public float DistanceToEdge(Vector2 p) => _path.Centre.Closest(p).Distance - _halfWidth;

        public bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit)
        {
            hit = default;
            if (!Bounds.Contains(p))
            {
                return false;
            }

            var found = false;
            ContourSnapping.ConsiderOffsetContour(p, _path.Centre.Closest(p), _halfWidth, 0, maxDistance, ref found, ref hit);
            return found;
        }
    }
}
