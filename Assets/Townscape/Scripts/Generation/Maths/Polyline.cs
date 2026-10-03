using System;
using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation.Maths
{
    /// <summary>Result of projecting a point onto a <see cref="Polyline"/>.</summary>
    public readonly struct PolylineHit
    {
        public PolylineHit(Vector2 point, float distance, float along, Vector2 tangent)
        {
            Point = point;
            Distance = distance;
            Along = along;
            Tangent = tangent;
        }

        /// <summary>Closest point on the line.</summary>
        public Vector2 Point { get; }

        /// <summary>Distance from the query point to <see cref="Point"/>.</summary>
        public float Distance { get; }

        /// <summary>Arc length from the start of the line to <see cref="Point"/>.</summary>
        public float Along { get; }

        /// <summary>Unit direction of the line at <see cref="Point"/>.</summary>
        public Vector2 Tangent { get; }
    }

    /// <summary>An open chain of straight segments on the ground plane (roads, rivers, paths).</summary>
    public sealed class Polyline
    {
        private readonly Vector2[] _points;
        private readonly float[] _cumulative;

        public Polyline(params Vector2[] points)
            : this((IEnumerable<Vector2>)points)
        {
        }

        public Polyline(IEnumerable<Vector2> points)
        {
            var list = new List<Vector2>();
            foreach (var point in points)
            {
                if (list.Count == 0 || Vector2.DistanceSquared(list[list.Count - 1], point) > 1e-8f)
                {
                    list.Add(point);
                }
            }

            if (list.Count < 2)
            {
                throw new ArgumentException("A polyline needs at least two distinct points.", nameof(points));
            }

            _points = list.ToArray();
            _cumulative = new float[_points.Length];
            for (var i = 1; i < _points.Length; i++)
            {
                _cumulative[i] = _cumulative[i - 1] + Vector2.Distance(_points[i - 1], _points[i]);
            }

            var min = _points[0];
            var max = _points[0];
            foreach (var point in _points)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            Bounds = new Rect2(min, max);
        }

        public IReadOnlyList<Vector2> Points => _points;

        /// <summary>
        /// A smooth line through <paramref name="controlPoints"/> (centripetal Catmull-Rom), sampled
        /// into straight segments. Three collinear control points in a row give a straight stretch.
        /// </summary>
        public static Polyline Smooth(IReadOnlyList<Vector2> controlPoints, int samplesPerSpan = 8)
        {
            if (controlPoints.Count < 3)
            {
                return new Polyline(controlPoints);
            }

            var count = controlPoints.Count;
            Vector2 Control(int i)
            {
                if (i < 0)
                {
                    return (2f * controlPoints[0]) - controlPoints[1];
                }

                if (i >= count)
                {
                    return (2f * controlPoints[count - 1]) - controlPoints[count - 2];
                }

                return controlPoints[i];
            }

            var points = new List<Vector2>();
            for (var span = 0; span < count - 1; span++)
            {
                var p0 = Control(span - 1);
                var p1 = Control(span);
                var p2 = Control(span + 1);
                var p3 = Control(span + 2);
                for (var k = 0; k < samplesPerSpan; k++)
                {
                    points.Add(CentripetalCatmullRom(p0, p1, p2, p3, (float)k / samplesPerSpan));
                }
            }

            points.Add(controlPoints[count - 1]);
            return new Polyline(points);
        }

        public float Length => _cumulative[_cumulative.Length - 1];

        public Rect2 Bounds { get; }

        public PolylineHit Closest(Vector2 p)
        {
            var bestDistanceSq = float.MaxValue;
            var bestPoint = _points[0];
            var bestAlong = 0f;
            var bestTangent = Vector2.UnitX;

            for (var i = 0; i < _points.Length - 1; i++)
            {
                var a = _points[i];
                var b = _points[i + 1];
                var ab = b - a;
                var lengthSq = ab.LengthSquared();
                var t = GeoMath.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                var q = a + (ab * t);
                var distanceSq = Vector2.DistanceSquared(p, q);
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestPoint = q;
                    var segmentLength = MathF.Sqrt(lengthSq);
                    bestAlong = _cumulative[i] + (t * segmentLength);
                    bestTangent = ab / segmentLength;
                }
            }

            return new PolylineHit(bestPoint, MathF.Sqrt(bestDistanceSq), bestAlong, bestTangent);
        }

        /// <summary>Point at arc length <paramref name="along"/>, clamped to the ends.</summary>
        public Vector2 PointAt(float along)
        {
            var i = SegmentAt(along);
            var a = _points[i];
            var b = _points[i + 1];
            var segmentLength = _cumulative[i + 1] - _cumulative[i];
            var t = GeoMath.Clamp01((along - _cumulative[i]) / segmentLength);
            return Vector2.Lerp(a, b, t);
        }

        /// <summary>Unit direction at arc length <paramref name="along"/>.</summary>
        public Vector2 TangentAt(float along)
        {
            var i = SegmentAt(along);
            return Vector2.Normalize(_points[i + 1] - _points[i]);
        }

        /// <summary>Arc lengths of the interior vertices, where the direction changes.</summary>
        public IEnumerable<float> VertexDistances()
        {
            for (var i = 1; i < _cumulative.Length - 1; i++)
            {
                yield return _cumulative[i];
            }
        }

        private static Vector2 CentripetalCatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
        {
            // Barry and Goldman's pyramidal formulation with alpha = 0.5.
            float Knot(float previous, Vector2 a, Vector2 b) => previous + MathF.Max(MathF.Sqrt(Vector2.Distance(a, b)), 1e-4f);

            var t0 = 0f;
            var t1 = Knot(t0, p0, p1);
            var t2 = Knot(t1, p1, p2);
            var t3 = Knot(t2, p2, p3);
            var t = GeoMath.Lerp(t1, t2, u);

            var a1 = (((t1 - t) / (t1 - t0)) * p0) + (((t - t0) / (t1 - t0)) * p1);
            var a2 = (((t2 - t) / (t2 - t1)) * p1) + (((t - t1) / (t2 - t1)) * p2);
            var a3 = (((t3 - t) / (t3 - t2)) * p2) + (((t - t2) / (t3 - t2)) * p3);
            var b1 = (((t2 - t) / (t2 - t0)) * a1) + (((t - t0) / (t2 - t0)) * a2);
            var b2 = (((t3 - t) / (t3 - t1)) * a2) + (((t - t1) / (t3 - t1)) * a3);
            return (((t2 - t) / (t2 - t1)) * b1) + (((t - t1) / (t2 - t1)) * b2);
        }

        private int SegmentAt(float along)
        {
            for (var i = 0; i < _points.Length - 2; i++)
            {
                if (along < _cumulative[i + 1])
                {
                    return i;
                }
            }

            return _points.Length - 2;
        }
    }
}
