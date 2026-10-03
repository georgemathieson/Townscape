using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing.Rules
{
    /// <summary>
    /// Lakeland dry-stone walls: battered (narrower at the top), with a row of upright cam stones
    /// along the top. Walls follow given runs and break wherever they would cross a road, path,
    /// yard or the river, which leaves natural gateways at cottages and junctions.
    /// </summary>
    public sealed class DryStoneWallsRule : IDressingRule
    {
        private const float Step = 1.2f;
        private const float BaseHalfWidth = 0.36f;
        private const float TopHalfWidth = 0.2f;

        private static readonly SurfaceMaterial[] Stones = { SurfaceMaterial.Stone, SurfaceMaterial.StoneDark, SurfaceMaterial.StoneGreen, SurfaceMaterial.Stone };

        private readonly IReadOnlyList<Polyline> _runs;

        public DryStoneWallsRule(IReadOnlyList<Polyline> runs)
        {
            _runs = runs;
        }

        /// <summary>Wall runs either side of a lane, set back from the carriageway edge.</summary>
        public static IEnumerable<Polyline> AlongRoad(RoadSpec road, float fromAlong, float toAlong, float setback)
        {
            foreach (var side in new[] { 1f, -1f })
            {
                var points = new List<Vector2>();
                for (var along = fromAlong; along <= toAlong; along += 2f)
                {
                    points.Add(road.PointAt(along, side * (road.HalfWidth + setback)));
                }

                if (points.Count >= 2)
                {
                    yield return new Polyline(points);
                }
            }
        }

        public void Apply(DressingContext context, Random random)
        {
            foreach (var run in _runs)
            {
                var count = (int)(run.Length / Step);
                if (count < 1)
                {
                    continue;
                }

                var stations = new Vector2[count + 1];
                var heights = new float[count + 1];
                var tops = new float[count + 1];
                var clear = new bool[count + 1];
                for (var i = 0; i <= count; i++)
                {
                    stations[i] = run.PointAt(run.Length * i / count);
                    clear[i] = context.IsClear(stations[i], 0.45f, RegionKind.OpenGround);
                    heights[i] = context.HeightAt(stations[i]);
                    tops[i] = 1.0f + (0.12f * ((float)random.NextDouble() - 0.5f));
                }

                for (var i = 0; i < count; i++)
                {
                    var midpoint = (stations[i] + stations[i + 1]) * 0.5f;
                    if (!clear[i] || !clear[i + 1] || !context.IsClear(midpoint, 0.45f, RegionKind.OpenGround))
                    {
                        continue;
                    }

                    var startsRun = i == 0 || !clear[i - 1];
                    var endsRun = i == count - 1 || !clear[i + 2];
                    Segment(context, random, stations[i], stations[i + 1], heights[i], heights[i + 1], tops[i], tops[i + 1], startsRun, endsRun);
                }
            }
        }

        private static void Segment(DressingContext context, Random random, Vector2 a, Vector2 b, float groundA, float groundB, float topA, float topB, bool capStart, bool capEnd)
        {
            var builder = context.BuilderAt((a + b) * 0.5f, DressingLayer.Furniture);
            context.Occupy((a + b) * 0.5f, 0.8f);
            var direction = GeoMath.SafeNormalize(b - a, Vector2.UnitX);
            var side = GeoMath.Left(direction);
            var stone = Stones[random.Next(Stones.Length)];
            var baseA = groundA - 0.2f;
            var baseB = groundB - 0.2f;
            var crestA = groundA + topA;
            var crestB = groundB + topB;

            Vector3 P(Vector2 at, float offset, float y) => GeoMath.At(at + (side * offset), y);

            foreach (var s in new[] { 1f, -1f })
            {
                builder.AddQuadFacing(P(a, s * BaseHalfWidth, baseA), P(b, s * BaseHalfWidth, baseB), P(b, s * TopHalfWidth, crestB), P(a, s * TopHalfWidth, crestA), GeoMath.At(side * s, 0.2f), stone);
            }

            builder.AddQuadFacing(P(a, -TopHalfWidth, crestA), P(b, -TopHalfWidth, crestB), P(b, TopHalfWidth, crestB), P(a, TopHalfWidth, crestA), Vector3.UnitY, stone);
            if (capStart)
            {
                builder.AddQuadFacing(P(a, -BaseHalfWidth, baseA), P(a, BaseHalfWidth, baseA), P(a, TopHalfWidth, crestA), P(a, -TopHalfWidth, crestA), GeoMath.At(-direction, 0f), stone);
            }

            if (capEnd)
            {
                builder.AddQuadFacing(P(b, -BaseHalfWidth, baseB), P(b, BaseHalfWidth, baseB), P(b, TopHalfWidth, crestB), P(b, -TopHalfWidth, crestB), GeoMath.At(direction, 0f), stone);
            }

            // Cam stones standing on edge along the top.
            var right = GeoMath.At(direction, 0f);
            var across = GeoMath.At(side, 0f);
            for (var t = 0.17f; t < 1f; t += 0.33f)
            {
                var at = Vector2.Lerp(a, b, t);
                var height = 0.12f + (0.08f * (float)random.NextDouble());
                var crest = MathF.Min(crestA, crestB) + (MathF.Abs(crestB - crestA) * t);
                builder.AddBox(GeoMath.At(at, crest + (height * 0.5f) - 0.02f), right, Vector3.UnitY, across, new Vector3(0.06f, height * 0.5f, TopHalfWidth + 0.02f), SurfaceMaterial.StoneDark);
            }
        }
    }
}
