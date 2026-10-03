using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Markings
{
    /// <summary>
    /// Paints flat marking quads just above the road surface. Anything that would land off the
    /// carriageway (over the bridge hole, the river or a pavement) is skipped automatically.
    /// </summary>
    public sealed class MarkingCanvas
    {
        /// <summary>Height above the road surface, enough to avoid z-fighting at a distance.</summary>
        public const float Lift = 0.012f;

        private const float MaxStep = 1f;
        private const float TangentProbe = 0.75f;

        private readonly MeshBuilder _builder;
        private readonly GroundModel _ground;

        public MarkingCanvas(MeshBuilder builder, GroundModel ground)
        {
            _builder = builder;
            _ground = ground;
        }

        /// <summary>Position on a road at arc length <paramref name="along"/>, shifted sideways by <paramref name="offset"/> (left is positive).</summary>
        public static Vector2 PointOn(RoadSpec road, float along, float offset)
        {
            return road.Centre.PointAt(along) + (GeoMath.Left(SmoothTangent(road, along)) * offset);
        }

        /// <summary>Road direction averaged over a short window so strips bend smoothly round corners.</summary>
        public static Vector2 SmoothTangent(RoadSpec road, float along)
        {
            var line = road.Centre;
            var a = line.PointAt(MathF.Max(0f, along - TangentProbe));
            var b = line.PointAt(MathF.Min(line.Length, along + TangentProbe));
            return GeoMath.SafeNormalize(b - a, line.TangentAt(along));
        }

        /// <summary>A strip following the road between two arc lengths, at a sideways offset.</summary>
        public void PaintAlong(RoadSpec road, float from, float to, float offset, float width, SurfaceMaterial material)
        {
            from = MathF.Max(0f, from);
            to = MathF.Min(road.Centre.Length, to);
            if (to - from < 0.01f)
            {
                return;
            }

            var stations = new List<float> { from, to };
            foreach (var vertex in road.Centre.VertexDistances())
            {
                if (vertex > from && vertex < to)
                {
                    stations.Add(vertex);
                }
            }

            var steps = (int)MathF.Ceiling((to - from) / MaxStep);
            for (var i = 1; i < steps; i++)
            {
                stations.Add(from + ((to - from) * i / steps));
            }

            stations.Sort();

            var half = width * 0.5f;
            for (var i = 0; i < stations.Count - 1; i++)
            {
                var s0 = stations[i];
                var s1 = stations[i + 1];
                if (s1 - s0 < 1e-3f)
                {
                    continue;
                }

                var c0 = PointOn(road, s0, offset);
                var c1 = PointOn(road, s1, offset);
                var l0 = GeoMath.Left(SmoothTangent(road, s0)) * half;
                var l1 = GeoMath.Left(SmoothTangent(road, s1)) * half;
                PaintQuad(c0 + l0, c1 + l1, c1 - l1, c0 - l0, material);
            }
        }

        /// <summary>A straight bar of the given width from <paramref name="a"/> to <paramref name="b"/>.</summary>
        public void PaintSegment(Vector2 a, Vector2 b, float width, SurfaceMaterial material)
        {
            var direction = b - a;
            if (direction.LengthSquared() < 1e-6f)
            {
                return;
            }

            var side = GeoMath.Left(Vector2.Normalize(direction)) * (width * 0.5f);
            PaintQuad(a + side, b + side, b - side, a - side, material);
        }

        /// <summary>A quad given by its four ground-plane corners in order around the edge.</summary>
        public void PaintQuad(Vector2 c0, Vector2 c1, Vector2 c2, Vector2 c3, SurfaceMaterial material)
        {
            var centre = (c0 + c1 + c2 + c3) * 0.25f;
            if (_ground.Classify(centre).Kind != RegionKind.Road)
            {
                return;
            }

            var y = _ground.HeightAt(centre) + Lift;
            _builder.AddQuadFacing(GeoMath.At(c0, y), GeoMath.At(c1, y), GeoMath.At(c2, y), GeoMath.At(c3, y), Vector3.UnitY, material);
        }
    }
}
