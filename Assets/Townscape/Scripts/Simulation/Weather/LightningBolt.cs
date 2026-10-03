using System;
using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Simulation.Weather
{
    /// <summary>One straight piece of a bolt. Width is relative: 1 for the main channel.</summary>
    public readonly struct BoltSegment
    {
        public BoltSegment(Vector3 start, Vector3 end, float width, bool isMain)
        {
            Start = start;
            End = end;
            Width = width;
            IsMain = isMain;
        }

        public Vector3 Start { get; }

        public Vector3 End { get; }

        public float Width { get; }

        /// <summary>True for the channel that reaches the ground; false for branches.</summary>
        public bool IsMain { get; }
    }

    /// <summary>
    /// The jagged shape of a bolt, by midpoint displacement: split each segment in two and nudge
    /// the middle sideways, a little less at each level. Branches fork off downwards and fade out
    /// before reaching the ground.
    /// </summary>
    public static class LightningBolt
    {
        private const int Levels = 7;
        private const float Roughness = 0.16f;

        public static IReadOnlyList<BoltSegment> Generate(int seed, Vector3 cloud, Vector3 ground)
        {
            var random = new Random(seed);
            var segments = new List<BoltSegment>();
            var main = Jagged(random, cloud, ground, Levels);
            AddChain(segments, main, 1f, true);

            // Fork a few branches off the upper two thirds of the main channel.
            var branches = 2 + random.Next(4);
            for (var b = 0; b < branches; b++)
            {
                var from = main[1 + random.Next((main.Count * 2 / 3) - 1)];
                var remaining = from.Y - ground.Y;
                var length = remaining * (0.18f + (0.3f * (float)random.NextDouble()));
                var angle = (float)(random.NextDouble() * Math.PI * 2.0);
                var sideways = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * (length * (0.45f + (0.35f * (float)random.NextDouble())));
                var to = from + sideways - new Vector3(0f, length, 0f);
                AddChain(segments, Jagged(random, from, to, Levels - 2), 0.45f, false);
            }

            return segments;
        }

        private static List<Vector3> Jagged(Random random, Vector3 start, Vector3 end, int levels)
        {
            var points = new List<Vector3> { start, end };
            var offset = Vector3.Distance(start, end) * Roughness;
            for (var level = 0; level < levels; level++)
            {
                var next = new List<Vector3>(points.Count * 2) { points[0] };
                for (var i = 0; i < points.Count - 1; i++)
                {
                    var a = points[i];
                    var c = points[i + 1];
                    var middle = (a + c) * 0.5f;
                    middle.X += Signed(random) * offset;
                    middle.Z += Signed(random) * offset;
                    middle.Y += Signed(random) * offset * 0.3f;

                    // Never kink back up above the start of the segment or below its end.
                    middle.Y = Math.Clamp(middle.Y, MathF.Min(a.Y, c.Y), MathF.Max(a.Y, c.Y));
                    next.Add(middle);
                    next.Add(c);
                }

                // Halving the nudge as the segments halve keeps the bolt equally rough at every
                // scale; anything slower turns the finest level into a sawtooth.
                points = next;
                offset *= 0.5f;
            }

            return points;
        }

        private static void AddChain(List<BoltSegment> segments, List<Vector3> points, float width, bool isMain)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                // Branches taper towards their tips.
                var taper = isMain ? 1f : 1f - (0.7f * i / points.Count);
                segments.Add(new BoltSegment(points[i], points[i + 1], width * taper, isMain));
            }
        }

        private static float Signed(Random random) => ((float)random.NextDouble() * 2f) - 1f;
    }
}
