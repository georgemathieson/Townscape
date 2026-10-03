using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Ground;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing.Rules
{
    /// <summary>A circle that tree and ground cover rules leave open, such as the village green's lawn.</summary>
    public readonly struct OpenSpace
    {
        public OpenSpace(Vector2 centre, float radius)
        {
            Centre = centre;
            Radius = radius;
        }

        public Vector2 Centre { get; }

        public float Radius { get; }

        public bool Contains(Vector2 p) => Vector2.Distance(p, Centre) < Radius;
    }

    /// <summary>
    /// Scatters trees on open ground: sparse in the village, thicker towards its edges and along
    /// the river banks, then patchy woodland on the lower fells. Conifers grow further out.
    /// </summary>
    public sealed class TreesRule : IDressingRule
    {
        private readonly IReadOnlyList<OpenSpace> _keepOpen;

        public TreesRule(IReadOnlyList<OpenSpace> keepOpen = null)
        {
            _keepOpen = keepOpen ?? Array.Empty<OpenSpace>();
        }

        public float CoreCell { get; init; } = 7f;

        public float FellCell { get; init; } = 15f;

        public float FellReach { get; init; } = 230f;

        public void Apply(DressingContext context, Random random)
        {
            var noise = new GradientNoise(random.Next());
            var core = context.Town.Settings.CoreHalfExtent;
            var river = context.Town.Layout.River;

            for (var x = -core; x < core; x += CoreCell)
            {
                for (var z = -core; z < core; z += CoreCell)
                {
                    var p = new Vector2(x + (CoreCell * Jitter(random)), z + (CoreCell * Jitter(random)));
                    var edge = GeoMath.SmoothStep(55f, 92f, GeoMath.ChebyshevLength(p));
                    var riverDistance = river.Centre.Closest(p).Distance;
                    var bank = riverDistance > river.WaterHalfWidth + river.BankWidth + 1f && riverDistance < 22f ? 0.3f : 0f;
                    var patch = 0.5f + (0.5f * noise.Fractal(p.X * 0.03f, p.Y * 0.03f, 2));
                    var density = 0.06f + (0.5f * edge) + bank + (0.25f * GeoMath.SmoothStep(0.55f, 0.8f, patch));
                    if (random.NextDouble() > density || IsKeptOpen(p) || context.IsOccupied(p, 2.2f) || !context.IsClear(p, 2.8f, RegionKind.OpenGround))
                    {
                        continue;
                    }

                    var roll = random.NextDouble();
                    var kind = edge > 0.6f && roll < 0.3 ? TreeKind.Conifer : (roll > 0.82 ? TreeKind.Birch : TreeKind.Broadleaf);
                    context.Place(new Tree(kind, 0.85f + (0.3f * (float)random.NextDouble())), p, Vector2.UnitY, DressingLayer.Vegetation, random, footprint: 1.2f);
                }
            }

            var outer = core + FellReach;
            for (var x = -outer; x < outer; x += FellCell)
            {
                for (var z = -outer; z < outer; z += FellCell)
                {
                    var p = new Vector2(x + (FellCell * Jitter(random)), z + (FellCell * Jitter(random)));
                    if (GeoMath.ChebyshevLength(p) < core + 2f)
                    {
                        continue;
                    }

                    var woodland = 0.5f + (0.5f * noise.Fractal(p.X * 0.008f, p.Y * 0.008f, 3));
                    var density = woodland > 0.58f ? 0.55f : 0.04f;
                    if (random.NextDouble() > density)
                    {
                        continue;
                    }

                    var height = context.HeightAt(p);
                    var slope = MathF.Abs(context.HeightAt(p + new Vector2(2f, 0f)) - height) + MathF.Abs(context.HeightAt(p + new Vector2(0f, 2f)) - height);
                    if (height < context.Town.Layout.WaterLevel + 0.8f || slope > 1.6f)
                    {
                        continue;
                    }

                    var kind = height > 18f || random.NextDouble() < 0.35 ? TreeKind.Conifer : (random.NextDouble() < 0.2 ? TreeKind.Birch : TreeKind.Broadleaf);
                    context.Place(new Tree(kind, 0.9f + (0.4f * (float)random.NextDouble())), p, Vector2.UnitY, DressingLayer.Vegetation, random);
                }
            }
        }

        private static float Jitter(Random random) => 0.15f + (0.7f * (float)random.NextDouble());

        private bool IsKeptOpen(Vector2 p)
        {
            foreach (var space in _keepOpen)
            {
                if (space.Contains(p))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Tufts of longer grass and clumps of wild flowers across open ground in the village.</summary>
    public sealed class GroundCoverRule : IDressingRule
    {
        public float TuftSpacing { get; init; } = 1.7f;

        public float FlowerSpacing { get; init; } = 2.6f;

        public void Apply(DressingContext context, Random random)
        {
            var noise = new GradientNoise(random.Next());
            var core = context.Town.Settings.CoreHalfExtent - 1f;

            for (var x = -core; x < core; x += TuftSpacing)
            {
                for (var z = -core; z < core; z += TuftSpacing)
                {
                    var p = new Vector2(x + (TuftSpacing * (float)random.NextDouble()), z + (TuftSpacing * (float)random.NextDouble()));
                    var patch = 0.5f + (0.5f * noise.Fractal(p.X * 0.08f, p.Y * 0.08f, 2));
                    if (patch < 0.5f || random.NextDouble() > (patch - 0.4f) * 1.6f || context.KindAt(p) != RegionKind.OpenGround)
                    {
                        continue;
                    }

                    var root = GeoMath.At(p, context.HeightAt(p) - 0.02f);
                    Planting.Tuft(context.BuilderAt(p, DressingLayer.Vegetation), random, root, 0.22f + (0.25f * (float)random.NextDouble()));
                }
            }

            for (var x = -core; x < core; x += FlowerSpacing)
            {
                for (var z = -core; z < core; z += FlowerSpacing)
                {
                    var p = new Vector2(x + (FlowerSpacing * (float)random.NextDouble()), z + (FlowerSpacing * (float)random.NextDouble()));
                    var meadow = 0.5f + (0.5f * noise.Fractal((p.X * 0.05f) + 50f, p.Y * 0.05f, 2));
                    var verge = context.Ground.DistanceToFeatures(p) < 3f;
                    var chance = (verge ? 0.12f : 0.02f) + (meadow > 0.65f ? 0.25f : 0f);
                    if (random.NextDouble() > chance || !context.IsClear(p, 0.3f, RegionKind.OpenGround))
                    {
                        continue;
                    }

                    context.Place(new FlowerClump(), p, Vector2.UnitY, DressingLayer.Vegetation, random);
                }
            }
        }
    }

    /// <summary>A ring-shaped bed of flowers, round a memorial or a tree.</summary>
    public sealed class FlowerBedRule : IDressingRule
    {
        private readonly Vector2 _centre;
        private readonly float _inner;
        private readonly float _outer;

        public FlowerBedRule(Vector2 centre, float inner, float outer)
        {
            _centre = centre;
            _inner = inner;
            _outer = outer;
        }

        public void Apply(DressingContext context, Random random)
        {
            var ring = (_inner + _outer) * 0.5f;
            var count = (int)(MathF.PI * 2f * ring / 0.45f);
            for (var i = 0; i < count; i++)
            {
                var angle = MathF.PI * 2f * i / count;
                var radius = _inner + ((_outer - _inner) * (float)random.NextDouble());
                var p = _centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
                context.Place(new FlowerClump(), p, Vector2.UnitY, DressingLayer.Vegetation, random);
            }
        }
    }

    /// <summary>Rows of headstones on the open ground of a churchyard, and yews at its corners.</summary>
    public sealed class ChurchyardRule : IDressingRule
    {
        private readonly Vector2 _min;
        private readonly Vector2 _max;
        private readonly Vector2 _headstoneFacing;

        public ChurchyardRule(Vector2 min, Vector2 max, Vector2 headstoneFacing)
        {
            _min = min;
            _max = max;
            _headstoneFacing = headstoneFacing;
        }

        public void Apply(DressingContext context, Random random)
        {
            for (var x = _min.X + 1.5f; x < _max.X - 1f; x += 2.3f)
            {
                for (var z = _min.Y + 1.5f; z < _max.Y - 1f; z += 1.9f)
                {
                    var p = new Vector2(x + (((float)random.NextDouble() - 0.5f) * 0.5f), z + (((float)random.NextDouble() - 0.5f) * 0.4f));
                    if (random.NextDouble() < 0.25 || !context.IsClear(p, 0.7f, RegionKind.OpenGround))
                    {
                        continue;
                    }

                    context.Place(new Gravestone(), p, _headstoneFacing, DressingLayer.Furniture, random);
                }
            }

            foreach (var corner in new[] { new Vector2(_min.X + 3f, _min.Y + 3f), new Vector2(_max.X - 3f, _min.Y + 3f), new Vector2(_max.X - 3f, _max.Y - 3f) })
            {
                if (context.IsClear(corner, 2f, RegionKind.OpenGround) && !context.IsOccupied(corner, 1.5f))
                {
                    context.Place(new Tree(TreeKind.Yew), corner, Vector2.UnitY, DressingLayer.Vegetation, random);
                }
            }
        }
    }
}
