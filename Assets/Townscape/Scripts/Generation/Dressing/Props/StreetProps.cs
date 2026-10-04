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

    /// <summary>
    /// The red K6 telephone kiosk: glazed on all sides, with lit TELEPHONE signs and a domed roof.
    /// Its front is a door that swings out, and inside is a payphone on the back wall, a shelf of
    /// directories and a light in the ceiling.
    /// </summary>
    public sealed class PhoneBox : IProp
    {
        public const string DoorName = "Phone box door";

        /// <summary>Half its width: a shade roomier than a real K6, so there's space to step inside.</summary>
        public const float Half = 0.48f;

        // The corner posts, and the glazed panels between them.
        private const float Post = 0.05f;
        private const float Panel = Half - (2f * Post);
        private const float Depth = 0.04f;
        private const float Floor = 0.1f;
        private const float DoorTop = 2.2f;
        private const float Inside = Half - Depth;

        public void Build(PropFrame f)
        {
            f.Box(0f, Floor * 0.5f, 0f, Half + 0.05f, Floor * 0.5f, Half + 0.05f, SurfaceMaterial.PaintRed);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                f.Box(x * (Half - Post), 1.2f, z * (Half - Post), Post, 1.1f, Post, SurfaceMaterial.PaintRed);
            }

            for (var side = 0; side < 4; side++)
            {
                var wall = FaceOf(f, side);
                Sign(f.Builder, wall, f.Origin.Y);
                if (side > 0)
                {
                    Glazing(f.Builder, wall, f.Origin.Y);
                }
            }

            // The door: the whole front, hung on its left and swinging out to the pavement.
            f.Door(DoorName, -Panel, Floor, Half, 1f, 2f * Panel, DoorTop - Floor, leaf =>
            {
                Glazing(leaf.Builder, FaceOf(leaf, 0), leaf.Origin.Y);
                leaf.Span(Panel - 0.1f, 0.95f, Half, Panel - 0.07f, 1.3f, Half + 0.025f, SurfaceMaterial.PaintGold);
                leaf.Span(Panel - 0.1f, 0.95f, Inside - 0.025f, Panel - 0.07f, 1.3f, Inside, SurfaceMaterial.PaintGold);
            }, openDegrees: 95f, noun: "phone box door");

            // Cornice, sign band roof and dome.
            f.Box(0f, 2.48f, 0f, Half + 0.06f, 0.05f, Half + 0.06f, SurfaceMaterial.PaintRed, bottom: true);
            f.Builder.AddPyramid(f.Point(0f, 2.53f, 0f), f.Right, f.Forward, new Vector2(Half + 0.04f, Half + 0.04f), 0.22f, Vector3.UnitY, SurfaceMaterial.PaintRed);
            f.Anchor(AnchorKind.LitSign, 0f, 2.12f, 0f, 0.9f);

            Interior(f);
        }

        // The face on one side (0 the front, then round to the right), from post to post, facing out.
        private static WallFrame FaceOf(PropFrame f, int side)
        {
            var outward = GeoMath.Flat(f.Forward);
            for (var i = 0; i < side; i++)
            {
                outward = GeoMath.Left(outward);
            }

            var right = GeoMath.Left(outward);
            var faceCentre = GeoMath.Flat(f.Origin) + (outward * Half);
            return WallFrame.FromBase(faceCentre - (right * Panel), faceCentre + (right * Panel));
        }

        // A glazed panel, solid enough to see from inside as well as out: a kick panel, eight rows
        // of three small panes of clear glass, and a rail along the top.
        private static void Glazing(MeshBuilder b, WallFrame wall, float y)
        {
            var width = wall.Width;
            wall.Block(b, 0f, y + Floor, width, y + 0.55f, -Depth, 0f, SurfaceMaterial.PaintRed);
            wall.QuadInward(b, 0f, y + Floor, width, y + 0.55f, -Depth, SurfaceMaterial.PaintRed);
            wall.Quad(b, 0f, y + 0.55f, width, y + 2.05f, -0.02f, SurfaceMaterial.ClearGlass);
            wall.QuadInward(b, 0f, y + 0.55f, width, y + 2.05f, -0.02f, SurfaceMaterial.ClearGlass);
            wall.Block(b, 0f, y + 2.05f, width, y + DoorTop, -Depth, 0f, SurfaceMaterial.PaintRed);
            wall.QuadInward(b, 0f, y + 2.05f, width, y + DoorTop, -Depth, SurfaceMaterial.PaintRed);

            for (var i = 1; i < 3; i++)
            {
                var x = width * i / 3f;
                wall.Block(b, x - 0.015f, y + 0.55f, x + 0.015f, y + 2.05f, -Depth, 0f, SurfaceMaterial.PaintRed);
                wall.QuadInward(b, x - 0.015f, y + 0.55f, x + 0.015f, y + 2.05f, -Depth, SurfaceMaterial.PaintRed);
            }

            for (var j = 0; j <= 8; j++)
            {
                var row = y + 0.55f + (1.5f * j / 8f);
                wall.Block(b, 0f, row - 0.015f, width, row + 0.015f, -Depth, 0f, SurfaceMaterial.PaintRed);
                wall.QuadInward(b, 0f, row - 0.015f, width, row + 0.015f, -Depth, SurfaceMaterial.PaintRed);
            }
        }

        // The back-lit sign over each face.
        private static void Sign(MeshBuilder b, WallFrame wall, float y)
        {
            var width = wall.Width;
            wall.Quad(b, 0f, y + DoorTop, width, y + 2.43f, 0.005f, SurfaceMaterial.SignGlass);
            PixelFont.Write(b, wall, "TELEPHONE", width * 0.5f, y + 2.315f, 0.02f, width - 0.08f, 0.01f, SurfaceMaterial.PaintBlack);
        }

        // A concrete floor, a red ceiling with a light, the payphone on the back wall and a shelf
        // of directories on the left.
        private static void Interior(PropFrame f)
        {
            f.Builder.AddQuadFacing(f.Point(-Inside, Floor + 0.002f, -Inside), f.Point(Inside, Floor + 0.002f, -Inside), f.Point(Inside, Floor + 0.002f, Inside), f.Point(-Inside, Floor + 0.002f, Inside), Vector3.UnitY, SurfaceMaterial.Concrete);
            f.Builder.AddQuadFacing(f.Point(-Half, DoorTop, -Half), f.Point(Half, DoorTop, -Half), f.Point(Half, DoorTop, Half), f.Point(-Half, DoorTop, Half), -Vector3.UnitY, SurfaceMaterial.PaintRed);
            f.Span(-0.09f, DoorTop - 0.035f, -0.09f, 0.09f, DoorTop, 0.09f, SurfaceMaterial.SignGlass);

            // The payphone: a black board on the back glass, a steel case with a little screen,
            // a keypad and a coin slot, and the handset in its cradle on the left.
            var back = -Inside;
            f.Span(-0.23f, 0.85f, back, 0.23f, 1.85f, back + 0.015f, SurfaceMaterial.PaintBlack);
            var front = back + 0.11f;
            f.Span(-0.11f, 1.05f, back + 0.015f, 0.14f, 1.58f, front, SurfaceMaterial.Chrome);
            f.Span(-0.06f, 1.44f, front, 0.1f, 1.53f, front + 0.004f, SurfaceMaterial.Screen);
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var x = -0.035f + (column * 0.045f);
                    var y = 1.37f - (row * 0.045f);
                    f.Span(x, y, front, x + 0.032f, y + 0.032f, front + 0.008f, SurfaceMaterial.PaintBlack);
                }
            }

            f.Span(0.095f, 1.3f, front, 0.11f, 1.38f, front + 0.006f, SurfaceMaterial.PaintBlack);
            f.Span(-0.06f, 1.08f, front, 0.1f, 1.13f, front + 0.01f, SurfaceMaterial.PaintBlack);

            f.Span(-0.19f, 1.22f, back + 0.015f, -0.11f, 1.5f, back + 0.06f, SurfaceMaterial.PaintBlack);
            f.Span(-0.175f, 1.18f, back + 0.06f, -0.125f, 1.54f, back + 0.095f, SurfaceMaterial.PaintBlack);
            f.Span(-0.18f, 1.48f, back + 0.06f, -0.12f, 1.56f, back + 0.105f, SurfaceMaterial.PaintBlack);
            f.Span(-0.18f, 1.16f, back + 0.06f, -0.12f, 1.24f, back + 0.105f, SurfaceMaterial.PaintBlack);
            f.Span(-0.155f, 0.98f, back + 0.07f, -0.145f, 1.18f, back + 0.08f, SurfaceMaterial.PaintBlack);

            // A white card above it: the emergency number.
            f.Span(-0.15f, 1.64f, back + 0.015f, 0.15f, 1.76f, back + 0.02f, SurfaceMaterial.PaintWhite);
            var card = WallFrame.FromBase(GeoMath.Flat(f.Point(-0.15f, 0f, back + 0.02f)), GeoMath.Flat(f.Point(0.15f, 0f, back + 0.02f)));
            PixelFont.Write(f.Builder, card, "999", 0.15f, f.Origin.Y + 1.7f, 0.012f, 0.26f, 0.002f, SurfaceMaterial.PaintRed);

            // The directories on a shelf: the Yellow Pages and the phone book.
            var side = -Inside;
            f.Span(side, 0.92f, -0.3f, side + 0.11f, 0.95f, 0.05f, SurfaceMaterial.PaintBlack);
            f.Span(side + 0.01f, 0.95f, -0.27f, side + 0.1f, 1.0f, -0.05f, SurfaceMaterial.PaintButter);
            f.Span(side + 0.015f, 1.0f, -0.25f, side + 0.095f, 1.035f, -0.06f, SurfaceMaterial.PaintWhite);
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
