using System;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing.Props
{
    /// <summary>A Victorian cast-iron street lamp with a square lantern, optionally carrying a hanging flower basket.</summary>
    public sealed class LampPost : IProp
    {
        public const float LanternHeight = 3.62f;

        private readonly bool _basket;

        public LampPost(bool basket)
        {
            _basket = basket;
        }

        public void Build(PropFrame f)
        {
            f.Prism(0f, 0f, 0f, 0.17f, 0.55f, 8, SurfaceMaterial.Iron);
            f.Prism(0f, 0.55f, 0f, 0.12f, 0.12f, 8, SurfaceMaterial.Iron);
            f.Prism(0f, 0.67f, 0f, 0.065f, 2.65f, 8, SurfaceMaterial.Iron);
            f.Prism(0f, 2.35f, 0f, 0.09f, 0.08f, 8, SurfaceMaterial.Iron);

            // Ladder bar, a lamplighter's rest.
            f.Span(-0.32f, 3.02f, -0.025f, 0.32f, 3.07f, 0.025f, SurfaceMaterial.Iron);

            // Lantern: base, glass, corner frames, pyramid cap and finial.
            f.Box(0f, 3.36f, 0f, 0.13f, 0.04f, 0.13f, SurfaceMaterial.Iron, bottom: true);
            f.Box(0f, LanternHeight, 0f, 0.17f, 0.22f, 0.17f, SurfaceMaterial.LampGlass, bottom: true);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                f.Box(x * 0.17f, LanternHeight, z * 0.17f, 0.018f, 0.22f, 0.018f, SurfaceMaterial.Iron);
            }

            f.Builder.AddPyramid(f.Point(0f, 3.84f, 0f), f.Right, f.Forward, new Vector2(0.22f, 0.22f), 0.24f, Vector3.UnitY, SurfaceMaterial.Iron);
            f.Prism(0f, 4.06f, 0f, 0.025f, 0.14f, 6, SurfaceMaterial.Iron);
            f.Anchor(AnchorKind.StreetLamp, 0f, LanternHeight, 0f, 0.35f);

            if (_basket)
            {
                f.Span(-0.02f, 2.8f, 0f, 0.02f, 2.84f, 0.6f, SurfaceMaterial.Iron);
                f.Span(-0.01f, 2.45f, 0.54f, 0.01f, 2.8f, 0.56f, SurfaceMaterial.Iron);
                Planting.Basket(f, 0f, 2.25f, 0.55f, 0.26f);
            }
        }
    }

    /// <summary>The red K6 telephone kiosk: glazed on all sides, with lit TELEPHONE signs and a domed roof.</summary>
    public sealed class PhoneBox : IProp
    {
        private const float Half = 0.45f;

        public void Build(PropFrame f)
        {
            f.Box(0f, 0.05f, 0f, Half + 0.05f, 0.05f, Half + 0.05f, SurfaceMaterial.PaintRed);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                f.Box(x * (Half - 0.05f), 1.2f, z * (Half - 0.05f), 0.06f, 1.1f, 0.06f, SurfaceMaterial.PaintRed);
            }

            var centre = GeoMath.Flat(f.Origin);
            var forward = GeoMath.Flat(f.Forward);
            for (var side = 0; side < 4; side++)
            {
                var outward = Rotate(forward, side);
                var right = GeoMath.Left(outward);
                var faceCentre = centre + (outward * Half);
                var wall = WallFrame.FromBase(faceCentre - (right * (Half - 0.1f)), faceCentre + (right * (Half - 0.1f)));
                Face(f, wall, f.Origin.Y);
            }

            // Cornice, sign band roof and dome.
            f.Box(0f, 2.48f, 0f, Half + 0.06f, 0.05f, Half + 0.06f, SurfaceMaterial.PaintRed, bottom: true);
            f.Builder.AddPyramid(f.Point(0f, 2.53f, 0f), f.Right, f.Forward, new Vector2(Half + 0.04f, Half + 0.04f), 0.22f, Vector3.UnitY, SurfaceMaterial.PaintRed);
            f.Anchor(AnchorKind.LitSign, 0f, 2.32f, 0f, 0.9f);
        }

        private static void Face(PropFrame f, WallFrame wall, float y)
        {
            var b = f.Builder;
            var width = wall.Width;
            wall.Quad(b, 0f, y + 0.1f, width, y + 0.55f, 0f, SurfaceMaterial.PaintRed);
            wall.Quad(b, 0f, y + 0.55f, width, y + 2.05f, -0.02f, SurfaceMaterial.WindowGlass);
            wall.Quad(b, 0f, y + 2.05f, width, y + 2.2f, 0f, SurfaceMaterial.PaintRed);

            // Glazing bars: three columns, eight rows of small panes.
            for (var i = 1; i < 3; i++)
            {
                var x = width * i / 3f;
                wall.Block(b, x - 0.015f, y + 0.55f, x + 0.015f, y + 2.05f, -0.02f, 0.01f, SurfaceMaterial.PaintRed);
            }

            for (var j = 0; j <= 8; j++)
            {
                var row = y + 0.55f + (1.5f * j / 8f);
                wall.Block(b, 0f, row - 0.015f, width, row + 0.015f, -0.02f, 0.01f, SurfaceMaterial.PaintRed);
            }

            // Back-lit sign.
            wall.Quad(b, 0f, y + 2.2f, width, y + 2.43f, 0.005f, SurfaceMaterial.LampGlass);
            PixelFont.Write(b, wall, "TELEPHONE", width * 0.5f, y + 2.315f, 0.02f, width - 0.08f, 0.01f, SurfaceMaterial.PaintBlack);
        }

        private static Vector2 Rotate(Vector2 v, int quarterTurns)
        {
            for (var i = 0; i < quarterTurns; i++)
            {
                v = GeoMath.Left(v);
            }

            return v;
        }
    }

    /// <summary>A red cylindrical pillar box.</summary>
    public sealed class PillarBox : IProp
    {
        public void Build(PropFrame f)
        {
            f.Prism(0f, 0f, 0f, 0.3f, 0.1f, 10, SurfaceMaterial.Iron);
            f.Prism(0f, 0.1f, 0f, 0.27f, 1.2f, 10, SurfaceMaterial.PaintRed);
            f.Prism(0f, 1.3f, 0f, 0.31f, 0.08f, 10, SurfaceMaterial.PaintRed);
            f.Cone(0f, 1.38f, 0f, 0.3f, 0.17f, 10, SurfaceMaterial.PaintRed);
            f.Span(-0.12f, 1.07f, 0.24f, 0.12f, 1.12f, 0.29f, SurfaceMaterial.Iron);
            f.Span(-0.08f, 0.72f, 0.24f, 0.08f, 0.9f, 0.275f, SurfaceMaterial.PaintWhite);
        }
    }

    /// <summary>A park bench: timber slats on cast-iron ends.</summary>
    public sealed class Bench : IProp
    {
        public void Build(PropFrame f)
        {
            foreach (var x in new[] { -0.78f, 0.78f })
            {
                f.Span(x - 0.03f, 0f, 0.16f, x + 0.03f, 0.44f, 0.21f, SurfaceMaterial.Iron);
                f.Span(x - 0.03f, 0f, -0.22f, x + 0.03f, 0.92f, -0.17f, SurfaceMaterial.Iron);
                f.Span(x - 0.03f, 0.6f, -0.2f, x + 0.03f, 0.65f, 0.25f, SurfaceMaterial.Iron);
                f.Span(x - 0.03f, 0.4f, -0.2f, x + 0.03f, 0.44f, 0.2f, SurfaceMaterial.Iron);
            }

            foreach (var z in new[] { -0.12f, 0f, 0.12f })
            {
                f.Box(0f, 0.46f, z, 0.88f, 0.02f, 0.05f, SurfaceMaterial.Timber);
            }

            foreach (var y in new[] { 0.62f, 0.8f })
            {
                f.Box(0f, y, -0.23f, 0.88f, 0.05f, 0.02f, SurfaceMaterial.Timber);
            }
        }
    }

    /// <summary>A bus stop: a pole with a round flag and a timetable case. Its front faces the road.</summary>
    public sealed class BusStop : IProp
    {
        public void Build(PropFrame f)
        {
            f.Prism(0f, 0f, 0f, 0.045f, 2.8f, 6, SurfaceMaterial.Iron);
            f.Span(-0.06f, 1.15f, -0.25f, 0.06f, 1.65f, 0.25f, SurfaceMaterial.Iron);
            f.Span(-0.065f, 1.2f, -0.21f, 0.065f, 1.6f, 0.21f, SurfaceMaterial.PaintCream);

            // The flag faces along the road, so draw it on both sides of a plane through the pole.
            var flat = GeoMath.Flat(f.Origin);
            var forward = GeoMath.Flat(f.Forward);
            var plane = WallFrame.FromBase(flat - (forward * 0.36f), flat + (forward * 0.36f));
            var back = WallFrame.FromBase(flat + (forward * 0.36f), flat - (forward * 0.36f));
            foreach (var wall in new[] { plane, back })
            {
                var cx = wall.Width * 0.5f;
                var cy = f.Origin.Y + 2.45f;
                wall.Polygon(f.Builder, 0.05f, SurfaceMaterial.PaintRed, Disc(cx, cy, 0.34f));
                wall.Polygon(f.Builder, 0.055f, SurfaceMaterial.PaintWhite, Disc(cx, cy, 0.25f));
                PixelFont.Write(f.Builder, wall, "BUS", cx, cy, 0.035f, 0.4f, 0.06f, SurfaceMaterial.PaintBlack);
            }
        }

        private static Vector2[] Disc(float cx, float cy, float radius)
        {
            var points = new Vector2[14];
            for (var i = 0; i < points.Length; i++)
            {
                var angle = MathF.PI * 2f * i / points.Length;
                points[i] = new Vector2(cx + (MathF.Cos(angle) * radius), cy + (MathF.Sin(angle) * radius));
            }

            return points;
        }
    }

    /// <summary>A Belisha beacon: a black and white striped pole with a flashing orange globe.</summary>
    public sealed class BelishaBeacon : IProp
    {
        public void Build(PropFrame f)
        {
            const int bands = 8;
            const float height = 2.3f;
            for (var i = 0; i < bands; i++)
            {
                f.Prism(0f, height * i / bands, 0f, 0.055f, height / bands, 8, i % 2 == 0 ? SurfaceMaterial.PaintBlack : SurfaceMaterial.PaintWhite);
            }

            f.Prism(0f, height, 0f, 0.08f, 0.06f, 8, SurfaceMaterial.PaintBlack);
            f.Builder.AddBlob(f.Point(0f, height + 0.27f, 0f), new Vector3(0.22f), SurfaceMaterial.Beacon);
            f.Anchor(AnchorKind.Beacon, 0f, height + 0.27f, 0f, 0.44f);
        }
    }

    /// <summary>A stone war memorial on stepped plinths, with a ring of poppies at its foot.</summary>
    public sealed class Memorial : IProp
    {
        public void Build(PropFrame f)
        {
            f.Box(0f, 0.12f, 0f, 1.3f, 0.12f, 1.3f, SurfaceMaterial.Stone);
            f.Box(0f, 0.36f, 0f, 1.0f, 0.12f, 1.0f, SurfaceMaterial.Stone);
            f.Box(0f, 0.6f, 0f, 0.7f, 0.12f, 0.7f, SurfaceMaterial.StoneDark);
            f.Box(0f, 1.2f, 0f, 0.42f, 0.48f, 0.42f, SurfaceMaterial.Stone);
            f.Box(0f, 1.71f, 0f, 0.48f, 0.04f, 0.48f, SurfaceMaterial.StoneDark);
            f.Box(0f, 3.0f, 0f, 0.28f, 1.25f, 0.28f, SurfaceMaterial.Stone);
            f.Builder.AddPyramid(f.Point(0f, 4.25f, 0f), f.Right, f.Forward, new Vector2(0.3f, 0.3f), 0.45f, Vector3.UnitY, SurfaceMaterial.Stone);
            f.Span(-0.25f, 1.0f, 0.42f, 0.25f, 1.45f, 0.44f, SurfaceMaterial.PaintGold);

            // Poppy wreath.
            for (var i = 0; i < 9; i++)
            {
                var angle = MathF.PI * 2f * i / 9f;
                f.Blob(MathF.Cos(angle) * 0.22f, 0.78f, 0.86f + (MathF.Sin(angle) * 0.1f), 0.07f, 0.05f, 0.07f, i % 3 == 2 ? SurfaceMaterial.LeafDark : SurfaceMaterial.PaintRed, 0f);
            }
        }
    }

    /// <summary>A weathered headstone: a slab with a pointed top, or a small cross.</summary>
    public sealed class Gravestone : IProp
    {
        public void Build(PropFrame f)
        {
            var material = f.Random.NextDouble() < 0.5 ? SurfaceMaterial.Stone : SurfaceMaterial.StoneDark;
            if (f.Random.NextDouble() < 0.2)
            {
                f.Box(0f, 0.5f, 0f, 0.05f, 0.5f, 0.05f, material);
                f.Box(0f, 0.72f, 0f, 0.26f, 0.05f, 0.05f, material);
                return;
            }

            var height = f.Range(0.55f, 0.95f);
            var width = f.Range(0.22f, 0.34f);
            f.Box(0f, height * 0.5f, 0f, width, height * 0.5f, 0.06f, material);
            f.Builder.AddPyramid(f.Point(0f, height, 0f), f.Right, f.Forward, new Vector2(width, 0.06f), width * 0.45f, Vector3.UnitY, material);
        }
    }
}
