using System;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>
    /// The river channel. Through the village it runs between vertical stone embankments (the
    /// ground mesher builds those walls from the height drop); elsewhere the banks slope to the water.
    /// </summary>
    public sealed class RiverFeature : IGroundFeature
    {
        private const float EdgeDepth = 0.45f;
        private const float BankFoot = 0.3f;

        private readonly RiverSpec _river;
        private readonly ISurfaceRegion _bed;
        private readonly ISurfaceRegion _bank;

        public RiverFeature(RiverSpec river, float waterLevel)
        {
            _river = river;
            WaterLevel = waterLevel;
            _bed = new BedRegion(this);
            _bank = new BankRegion(this);
            var reach = MathF.Max(river.WallHalfWidth, river.WaterHalfWidth + river.BankWidth);
            Bounds = river.Centre.Bounds.Expand(reach + 2f);
        }

        public float WaterLevel { get; }

        public Rect2 Bounds { get; }

        public bool TryClaim(Vector2 p, out SurfaceClaim claim)
        {
            claim = default;
            if (!Bounds.Contains(p))
            {
                return false;
            }

            var closest = _river.Centre.Closest(p);
            if (_river.IsWalledAt(closest.Point))
            {
                if (closest.Distance < _river.WallHalfWidth)
                {
                    claim = new SurfaceClaim(_bed, GroundPriority.RiverBed);
                    return true;
                }

                return false;
            }

            if (closest.Distance < _river.WaterHalfWidth)
            {
                claim = new SurfaceClaim(_bed, GroundPriority.RiverBed);
                return true;
            }

            if (closest.Distance < _river.WaterHalfWidth + _river.BankWidth)
            {
                claim = new SurfaceClaim(_bank, GroundPriority.RiverBank);
                return true;
            }

            return false;
        }

        public float DistanceToEdge(Vector2 p)
        {
            var closest = _river.Centre.Closest(p);
            return closest.Distance - OuterRadius(closest.Point);
        }

        public bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit)
        {
            hit = default;
            if (!Bounds.Contains(p))
            {
                return false;
            }

            var closest = _river.Centre.Closest(p);
            var found = false;
            if (_river.IsWalledAt(closest.Point))
            {
                ContourSnapping.ConsiderOffsetContour(p, closest, _river.WallHalfWidth, 0, maxDistance, ref found, ref hit);
            }
            else
            {
                ContourSnapping.ConsiderOffsetContour(p, closest, _river.WaterHalfWidth, 1, maxDistance, ref found, ref hit);
                ContourSnapping.ConsiderOffsetContour(p, closest, _river.WaterHalfWidth + _river.BankWidth, 2, maxDistance, ref found, ref hit);
            }

            return found;
        }

        private float OuterRadius(Vector2 centrelinePoint) =>
            _river.IsWalledAt(centrelinePoint) ? _river.WallHalfWidth : _river.WaterHalfWidth + _river.BankWidth;

        private float BedHeight(Vector2 p)
        {
            var closest = _river.Centre.Closest(p);
            var edge = _river.IsWalledAt(closest.Point) ? _river.WallHalfWidth : _river.WaterHalfWidth;
            var t = GeoMath.Clamp01(closest.Distance / edge);
            return WaterLevel - (EdgeDepth + (_river.BedDepth * (1f - (t * t))));
        }

        private float BankHeight(in GroundPoint point)
        {
            var distance = _river.Centre.Closest(point.Position).Distance;
            var t = GeoMath.SmoothStep(_river.WaterHalfWidth, _river.WaterHalfWidth + _river.BankWidth, distance);
            return GeoMath.Lerp(WaterLevel - BankFoot, point.BaseHeight, t);
        }

        private sealed class BedRegion : ISurfaceRegion
        {
            private readonly RiverFeature _owner;

            public BedRegion(RiverFeature owner) => _owner = owner;

            public RegionKind Kind => RegionKind.RiverBed;

            public SurfaceMaterial Side => SurfaceMaterial.RiverBed;

            public SurfaceMaterial TopAt(Vector2 centroid) => SurfaceMaterial.RiverBed;

            public float HeightAt(in GroundPoint point) => _owner.BedHeight(point.Position);
        }

        private sealed class BankRegion : ISurfaceRegion
        {
            private readonly RiverFeature _owner;

            public BankRegion(RiverFeature owner) => _owner = owner;

            public RegionKind Kind => RegionKind.RiverBank;

            public SurfaceMaterial Side => SurfaceMaterial.Earth;

            public SurfaceMaterial TopAt(Vector2 centroid)
            {
                var distance = _owner._river.Centre.Closest(centroid).Distance;
                return distance < _owner._river.WaterHalfWidth + (_owner._river.BankWidth * 0.35f)
                    ? SurfaceMaterial.Shingle
                    : SurfaceMaterial.Grass;
            }

            public float HeightAt(in GroundPoint point) => _owner.BankHeight(point);
        }
    }
}
