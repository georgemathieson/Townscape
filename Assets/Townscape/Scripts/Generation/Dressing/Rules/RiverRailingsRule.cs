using System;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing.Rules
{
    /// <summary>
    /// Dark stone coping along the top of the river embankments with black iron railings on it,
    /// on both banks wherever the river runs between walls. It stops at the bridge.
    /// </summary>
    public sealed class RiverRailingsRule : IDressingRule
    {
        private const float Step = 1.8f;
        private const float Height = 1.0f;
        private const float BarSpacing = 0.16f;

        private static readonly RegionKind[] Allowed = { RegionKind.OpenGround, RegionKind.Pavement, RegionKind.Yard, RegionKind.Path };

        private readonly RiverSpec _river;

        public RiverRailingsRule(RiverSpec river)
        {
            _river = river;
        }

        public void Apply(DressingContext context, Random random)
        {
            var line = _river.Centre;
            foreach (var side in new[] { 1f, -1f })
            {
                Vector3? previous = null;
                for (var along = 0f; along <= line.Length; along += Step)
                {
                    var centre = line.PointAt(along);
                    var offset = GeoMath.Left(line.TangentAt(along)) * side;
                    var spot = centre + (offset * (_river.WallHalfWidth + 0.16f));
                    if (!_river.IsWalledAt(centre) || !context.InCore(spot) || !context.IsClear(spot, 0.1f, Allowed))
                    {
                        previous = null;
                        continue;
                    }

                    var post = GeoMath.At(spot, context.HeightAt(spot));
                    if (previous is Vector3 start)
                    {
                        Segment(context.BuilderAt(spot, DressingLayer.Furniture), start, post);
                    }

                    previous = post;
                }
            }
        }

        private static void Segment(MeshBuilder builder, Vector3 a, Vector3 b)
        {
            var span = b - a;
            var flat = new Vector3(span.X, 0f, span.Z);
            var length = flat.Length();
            if (length < 0.2f)
            {
                return;
            }

            var along = flat / length;
            var across = Vector3.Cross(Vector3.UnitY, along);
            var up = Vector3.UnitY;

            // Coping stones, and the iron work on top of them.
            var mid = (a + b) * 0.5f;
            builder.AddBox(mid + new Vector3(0f, 0.01f, 0f), along, up, across, new Vector3(length * 0.5f, 0.07f, 0.24f), SurfaceMaterial.StoneDark);
            var top = new Vector3(0f, 0.08f, 0f);
            builder.AddBox(a + top + new Vector3(0f, Height * 0.5f, 0f), along, up, across, new Vector3(0.04f, Height * 0.5f, 0.04f), SurfaceMaterial.Iron);
            builder.AddBox(mid + top + new Vector3(0f, Height - 0.03f, 0f), along, up, across, new Vector3(length * 0.5f, 0.025f, 0.03f), SurfaceMaterial.Iron);
            builder.AddBox(mid + top + new Vector3(0f, 0.12f, 0f), along, up, across, new Vector3(length * 0.5f, 0.02f, 0.025f), SurfaceMaterial.Iron);

            var bars = (int)(length / BarSpacing);
            for (var i = 1; i < bars; i++)
            {
                var point = Vector3.Lerp(a, b, i / (float)bars) + top;
                builder.AddBox(point + new Vector3(0f, Height * 0.5f, 0f), along, up, across, new Vector3(0.011f, Height * 0.5f, 0.011f), SurfaceMaterial.Iron);
            }
        }
    }
}
