using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>
    /// The ground under and just around a building: flat, flagged and at pavement height, so
    /// hummocks never poke through floors or shop displays and every building sits on a neat apron.
    /// </summary>
    public sealed class PlotFeature : IGroundFeature
    {
        private static readonly ISurfaceRegion Yard =
            new FlatRegion(RegionKind.Yard, SurfaceMaterial.Pavement, SurfaceMaterial.Kerb, RoadFeature.PavementHeight);

        private readonly Vector2[] _corners;
        private readonly Vector2[] _normals;
        private readonly float _margin;

        /// <param name="corners">A convex polygon, in either winding.</param>
        /// <param name="margin">How far the apron extends beyond the polygon.</param>
        public PlotFeature(IReadOnlyList<Vector2> corners, float margin)
        {
            _corners = new Vector2[corners.Count];
            _normals = new Vector2[corners.Count];
            _margin = margin;

            var centre = Vector2.Zero;
            for (var i = 0; i < corners.Count; i++)
            {
                _corners[i] = corners[i];
                centre += corners[i];
            }

            centre /= corners.Count;
            var min = _corners[0];
            var max = _corners[0];
            for (var i = 0; i < _corners.Length; i++)
            {
                var a = _corners[i];
                var b = _corners[(i + 1) % _corners.Length];
                var normal = Vector2.Normalize(GeoMath.Left(b - a));
                if (Vector2.Dot(normal, ((a + b) * 0.5f) - centre) < 0f)
                {
                    normal = -normal;
                }

                _normals[i] = normal;
                min = Vector2.Min(min, a);
                max = Vector2.Max(max, a);
            }

            Bounds = new Rect2(min, max).Expand(margin + 2f);
        }

        public Rect2 Bounds { get; }

        public bool TryClaim(Vector2 p, out SurfaceClaim claim)
        {
            claim = default;
            if (!Bounds.Contains(p) || SignedDistance(p, out _) >= _margin)
            {
                return false;
            }

            claim = new SurfaceClaim(Yard, GroundPriority.Plot);
            return true;
        }

        public float DistanceToEdge(Vector2 p) => SignedDistance(p, out _) - _margin;

        public bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit)
        {
            hit = default;
            if (!Bounds.Contains(p))
            {
                return false;
            }

            var distance = SignedDistance(p, out var nearest);
            var gap = MathF.Abs(distance - _margin);
            if (gap > maxDistance)
            {
                return false;
            }

            // Push out from the nearest boundary point to the apron's edge.
            var direction = distance > 1e-4f ? Vector2.Normalize(p - nearest) : NearestEdgeNormal(p);
            hit = new ContourHit(nearest + (direction * _margin), gap, 0);
            return true;
        }

        /// <summary>Distance to the polygon (negative inside), and the nearest point on its boundary.</summary>
        private float SignedDistance(Vector2 p, out Vector2 nearest)
        {
            var inside = true;
            var maxPlane = float.MinValue;
            var best = float.MaxValue;
            nearest = _corners[0];
            for (var i = 0; i < _corners.Length; i++)
            {
                var a = _corners[i];
                var b = _corners[(i + 1) % _corners.Length];
                var plane = Vector2.Dot(p - a, _normals[i]);
                maxPlane = MathF.Max(maxPlane, plane);
                if (plane > 0f)
                {
                    inside = false;
                }

                var ab = b - a;
                var t = GeoMath.Clamp01(Vector2.Dot(p - a, ab) / ab.LengthSquared());
                var q = a + (ab * t);
                var d = Vector2.Distance(p, q);
                if (d < best)
                {
                    best = d;
                    nearest = q;
                }
            }

            return inside ? maxPlane : best;
        }

        private Vector2 NearestEdgeNormal(Vector2 p)
        {
            var bestPlane = float.MinValue;
            var normal = _normals[0];
            for (var i = 0; i < _corners.Length; i++)
            {
                var plane = Vector2.Dot(p - _corners[i], _normals[i]);
                if (plane > bestPlane)
                {
                    bestPlane = plane;
                    normal = _normals[i];
                }
            }

            return normal;
        }
    }
}
