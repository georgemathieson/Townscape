using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing
{
    /// <summary>
    /// Puddles: irregular pools of water on roads (mostly in the gutters), footpaths, yards and
    /// pavements. Each stays inside the surface it starts on, so none spills over a kerb, and
    /// clear of buildings, whose flagged yards run underneath them.
    /// </summary>
    public sealed class PuddlesRule : IDressingRule
    {
        /// <summary>Height above the surface: just above the road markings, so puddles cover them.</summary>
        public const float Lift = 0.02f;

        private const int Sides = 12;

        public int Count { get; init; } = 60;

        public void Apply(DressingContext context, Random random)
        {
            var placed = new List<(Vector2 Centre, float Radius)>();
            var core = context.Town.Settings.CoreHalfExtent - 2f;
            for (var attempt = 0; attempt < Count * 40 && placed.Count < Count; attempt++)
            {
                var centre = new Vector2(Range(random, -core, core), Range(random, -core, core));
                var kind = context.KindAt(centre);
                if (!Accept(context, kind, centre, random))
                {
                    continue;
                }

                var radius = Range(random, 0.45f, kind == RegionKind.Path ? 1.5f : 1.2f);
                if (placed.Exists(other => Vector2.Distance(other.Centre, centre) < other.Radius + radius + 0.5f))
                {
                    continue;
                }

                var outline = Outline(context, kind, centre, radius, random);
                if (outline == null)
                {
                    continue;
                }

                Build(context.BuilderAt(centre, DressingLayer.Puddles), context, centre, outline);
                placed.Add((centre, radius * 1.6f));
            }
        }

        private static bool Accept(DressingContext context, RegionKind kind, Vector2 centre, Random random)
        {
            switch (kind)
            {
                case RegionKind.Road:
                    // Water collects in the gutters, so puddles away from the kerb are rarer.
                    return NearEdge(context, centre, RegionKind.Road) || random.NextDouble() < 0.2;
                case RegionKind.Path:
                    return true;
                case RegionKind.Yard:
                    return random.NextDouble() < 0.5;
                case RegionKind.Pavement:
                    return random.NextDouble() < 0.35;
                default:
                    return false;
            }
        }

        private static bool NearEdge(DressingContext context, Vector2 p, RegionKind kind)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = i * MathF.PI / 4f;
                if (context.KindAt(p + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 1.3f)) != kind)
                {
                    return true;
                }
            }

            return false;
        }

        // An irregular, slightly stretched ring of points, shrunk until it fits inside one surface.
        private static Vector2[] Outline(DressingContext context, RegionKind kind, Vector2 centre, float radius, Random random)
        {
            var stretch = Range(random, 1f, 1.7f);
            var turn = Range(random, 0f, MathF.PI);
            var wobble = new float[Sides];
            for (var i = 0; i < Sides; i++)
            {
                wobble[i] = Range(random, 0.7f, 1f);
            }

            for (var scale = 1f; scale > 0.5f; scale *= 0.8f)
            {
                var outline = new Vector2[Sides];
                var fits = true;
                for (var i = 0; i < Sides && fits; i++)
                {
                    var angle = i * 2f * MathF.PI / Sides;
                    var local = new Vector2(MathF.Cos(angle) * stretch, MathF.Sin(angle)) * (radius * wobble[i] * scale);
                    var rotated = new Vector2((local.X * MathF.Cos(turn)) - (local.Y * MathF.Sin(turn)), (local.X * MathF.Sin(turn)) + (local.Y * MathF.Cos(turn)));
                    outline[i] = centre + rotated;
                    fits = context.KindAt(outline[i]) == kind && !context.InsideBuilding(outline[i], 0.3f);
                }

                if (fits)
                {
                    return outline;
                }
            }

            return null;
        }

        private static void Build(MeshBuilder builder, DressingContext context, Vector2 centre, Vector2[] outline)
        {
            var middle = GeoMath.At(centre, context.HeightAt(centre) + Lift);
            for (var i = 0; i < outline.Length; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Length];

                // Points run anticlockwise seen from above, so centre-b-a is clockwise: facing up.
                builder.AddTriangle(
                    middle,
                    GeoMath.At(b, context.HeightAt(b) + Lift),
                    GeoMath.At(a, context.HeightAt(a) + Lift),
                    centre,
                    b,
                    a,
                    SurfaceMaterial.Puddle);
            }
        }

        private static float Range(Random random, float min, float max) => min + ((float)random.NextDouble() * (max - min));
    }
}
