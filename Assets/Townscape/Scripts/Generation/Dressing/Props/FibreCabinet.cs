using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing.Props
{
    /// <summary>
    /// A dark green street cabinet for fibre broadband, on a concrete plinth, with double doors
    /// that open. Inside, on a 19 inch rack: a patch tray where each customer's fibre plugs in,
    /// the fibre switch, the edge router, a UPS with its battery and a power strip; to the left,
    /// a little computer on a shelf with its screen on a bracket.
    /// </summary>
    public sealed class FibreCabinet : IProp
    {
        public const float HalfWidth = 0.5f;
        public const float HalfDepth = 0.23f;
        public const float Height = 1.38f;

        // Rack units, and the rack's place inside: off to the right, leaving room for the screen.
        private const float Unit = 0.0445f;
        private const float RackX = 0.17f;
        private const float RackHalf = 0.22f;
        private const float KitFront = 0.11f;
        private const float Plinth = 0.08f;
        private const float Floor = 0.13f;
        private const float DoorTop = 1.3f;
        private const float Wall = 0.03f;
        private const int Ports = 24;

        private readonly string _name;

        public FibreCabinet(string name)
        {
            _name = name;
        }

        public void Build(PropFrame f)
        {
            Shell(f);
            Doors(f);
            var leds = new List<CabinetLed>();
            Rack(f, leds);
            var screen = Computer(f);

            var inside = HalfWidth - Wall;
            var rack = new WallMount(f.Point(0f, (Floor + DoorTop) * 0.5f, KitFront + 0.01f), f.Forward, 2f * inside, DoorTop - Floor);
            f.Cabinets.Add(new TownCabinet(_name, rack, f.Forward, leds, screen));
        }

        // The point a light at (x, y) on the front of the kit sits at.
        private static Vector3 Led(PropFrame f, float x, float y) => f.Point(x, y, KitFront + 0.006f);

        private static void Shell(PropFrame f)
        {
            var green = SurfaceMaterial.PaintDarkGreen;
            f.Span(-HalfWidth - 0.06f, 0f, -HalfDepth - 0.06f, HalfWidth + 0.06f, Plinth, HalfDepth + 0.06f, SurfaceMaterial.Concrete);
            f.Span(-HalfWidth, Plinth, -HalfDepth, HalfWidth, Height - 0.05f, -HalfDepth + Wall, green);
            f.Span(-HalfWidth, Plinth, -HalfDepth + Wall, -HalfWidth + Wall, Height - 0.05f, HalfDepth, green);
            f.Span(HalfWidth - Wall, Plinth, -HalfDepth + Wall, HalfWidth, Height - 0.05f, HalfDepth, green);
            f.Span(-HalfWidth + Wall, Plinth, -HalfDepth + Wall, HalfWidth - Wall, Floor, HalfDepth, green);
            f.Span(-HalfWidth + Wall, DoorTop, -HalfDepth + Wall, HalfWidth - Wall, Height - 0.05f, HalfDepth, green);

            // A lid that overhangs a little, to shed the rain.
            f.Span(-HalfWidth - 0.03f, Height - 0.05f, -HalfDepth - 0.03f, HalfWidth + 0.03f, Height, HalfDepth + 0.03f, green);
        }

        private void Doors(PropFrame f)
        {
            var inside = HalfWidth - Wall;
            var front = HalfDepth;
            for (var side = -1; side <= 1; side += 2)
            {
                var hinge = side * inside;
                var name = side < 0 ? "Cabinet left door" : "Cabinet right door";
                var label = side < 0;
                f.Door(name, hinge, Floor, front, -side, inside, DoorTop - Floor, leaf =>
                {
                    float X(float across) => hinge - (side * across);
                    Box(leaf, X(0f), Floor, front - Wall, X(inside - 0.004f), DoorTop, front, SurfaceMaterial.PaintDarkGreen);

                    // Vents near the bottom and a handle with a lock by the meeting edge.
                    for (var i = 0; i < 4; i++)
                    {
                        var y = Floor + 0.08f + (i * 0.035f);
                        Box(leaf, X(0.08f), y, front, X(inside - 0.08f), y + 0.015f, front + 0.01f, SurfaceMaterial.Iron);
                    }

                    Box(leaf, X(inside - 0.06f), 0.62f, front, X(inside - 0.035f), 0.8f, front + 0.025f, SurfaceMaterial.Iron);
                    Box(leaf, X(inside - 0.058f), 0.84f, front, X(inside - 0.037f), 0.86f, front + 0.012f, SurfaceMaterial.Chrome);

                    if (label)
                    {
                        // The cabinet's number on a white plate, and a yellow warning label.
                        Box(leaf, X(0.08f), 1.06f, front, X(0.38f), 1.18f, front + 0.004f, SurfaceMaterial.PaintWhite);
                        var a = GeoMath.Flat(leaf.Point(X(0.08f), 0f, front + 0.004f));
                        var b = GeoMath.Flat(leaf.Point(X(0.38f), 0f, front + 0.004f));
                        var plate = side < 0 ? WallFrame.FromBase(a, b) : WallFrame.FromBase(b, a);
                        PixelFont.Write(leaf.Builder, plate, _name, 0.15f, leaf.Origin.Y + 1.12f, 0.014f, 0.26f, 0.002f, SurfaceMaterial.PaintBlack);
                        Box(leaf, X(0.18f), 0.9f, front, X(0.28f), 0.99f, front + 0.004f, SurfaceMaterial.PaintButter);
                    }
                }, openDegrees: 105f, noun: "cabinet door");
            }
        }

        private static void Box(PropFrame f, float x0, float y0, float z0, float x1, float y1, float z1, SurfaceMaterial material) =>
            f.Span(System.MathF.Min(x0, x1), y0, z0, System.MathF.Max(x0, x1), y1, z1, material);

        // The rack, from the top: patch tray, fibre switch, edge router, a brush panel, the UPS and
        // its battery, and the power strip, with yellow patch leads and black power cords.
        private static void Rack(PropFrame f, List<CabinetLed> leds)
        {
            var left = RackX - RackHalf;
            var right = RackX + RackHalf;
            var back = -HalfDepth + Wall + 0.03f;
            foreach (var x in new[] { left - 0.012f, right + 0.012f })
            {
                f.Span(x - 0.012f, Floor, KitFront - 0.02f, x + 0.012f, DoorTop - 0.02f, KitFront, SurfaceMaterial.Iron);
                f.Span(x - 0.012f, Floor, back, x + 0.012f, DoorTop - 0.02f, back + 0.02f, SurfaceMaterial.Iron);
            }

            float Top(int unitsDown) => DoorTop - 0.04f - (unitsDown * Unit);
            void Kit(int unitsDown, int units, SurfaceMaterial material) =>
                f.Span(left, Top(unitsDown + units) + 0.002f, back + 0.02f, right, Top(unitsDown) - 0.002f, KitFront, material);

            // Patch tray: a row of fibre adapters, with the customers' two in use.
            Kit(0, 1, SurfaceMaterial.PaintBlack);
            var trayY = Top(0) - (Unit * 0.5f);
            for (var i = 0; i < 12; i++)
            {
                var x = left + 0.06f + (i * 0.028f);
                f.Span(x - 0.008f, trayY - 0.009f, KitFront, x + 0.008f, trayY + 0.009f, KitFront + 0.01f, SurfaceMaterial.PaintBlue);
            }

            // Fibre switch: 24 ports in two rows, with a light above each column.
            Kit(1, 1, SurfaceMaterial.Iron);
            var switchY = Top(1) - (Unit * 0.5f);
            for (var port = 1; port <= Ports; port++)
            {
                var (x, y) = PortAt(left, switchY, port);
                f.Span(x - 0.007f, y - 0.006f, KitFront, x + 0.007f, y + 0.006f, KitFront + 0.003f, SurfaceMaterial.PaintBlack);
                leds.Add(new CabinetLed(port == Ports ? CabinetLedKind.Uplink : CabinetLedKind.Port, port, Led(f, x + 0.011f, y)));
            }

            // Edge router, silver, with its own lights.
            Kit(2, 1, SurfaceMaterial.Chrome);
            var routerY = Top(2) - (Unit * 0.5f);
            for (var i = 0; i < 4; i++)
            {
                var x = right - 0.2f + (i * 0.025f);
                f.Span(x - 0.008f, routerY - 0.007f, KitFront, x + 0.008f, routerY + 0.007f, KitFront + 0.003f, SurfaceMaterial.PaintBlack);
            }

            leds.Add(new CabinetLed(CabinetLedKind.Wan, 0, Led(f, right - 0.06f, routerY)));

            // A brush panel, and a shelf of splice cassettes where the fibres from the duct are joined.
            Kit(3, 1, SurfaceMaterial.PaintBlack);
            Kit(4, 2, SurfaceMaterial.PaintWhite);
            for (var i = 0; i < 8; i++)
            {
                var x = left + 0.04f + (i * 0.046f);
                f.Span(x, Top(4) - (Unit * 1.75f), KitFront, x + 0.036f, Top(4) - (Unit * 0.25f), KitFront + 0.004f, SurfaceMaterial.Iron);
            }

            // Lower down: a blanking panel, then the power strip with plugs in it, the UPS with
            // its little display, and the UPS's battery pack at the bottom.
            Kit(8, 2, SurfaceMaterial.Iron);
            Kit(10, 1, SurfaceMaterial.PaintBlack);
            var stripY = Top(10) - (Unit * 0.5f);
            for (var i = 0; i < 6; i++)
            {
                var x = left + 0.05f + (i * 0.065f);
                f.Span(x, stripY - 0.012f, KitFront, x + 0.035f, stripY + 0.012f, KitFront + 0.004f, SurfaceMaterial.PaintWhite);
                if (i < 3)
                {
                    f.Span(x + 0.008f, stripY - 0.01f, KitFront + 0.004f, x + 0.027f, stripY + 0.01f, KitFront + 0.025f, SurfaceMaterial.PaintBlack);
                }
            }

            Kit(11, 2, SurfaceMaterial.PaintBlack);
            var upsY = Top(11) - Unit;
            f.Span(left + 0.05f, upsY - 0.018f, KitFront, left + 0.13f, upsY + 0.018f, KitFront + 0.003f, SurfaceMaterial.Screen);
            leds.Add(new CabinetLed(CabinetLedKind.Ups, 0, Led(f, left + 0.16f, upsY)));
            Kit(13, 4, SurfaceMaterial.PaintBlack);
            for (var i = 0; i < 6; i++)
            {
                var x = left + 0.05f + (i * 0.06f);
                f.Span(x, Top(13) - (Unit * 3.6f), KitFront, x + 0.04f, Top(13) - (Unit * 0.4f), KitFront + 0.003f, SurfaceMaterial.Iron);
            }

            // Power cords up the right of the rack.
            f.Span(right - 0.03f, stripY, KitFront + 0.02f, right - 0.02f, Top(1), KitFront + 0.03f, SurfaceMaterial.PaintBlack);

            // Yellow patch leads from the customers' adapters down to their switch ports, and
            // the bundle of fibre coming up from the duct below.
            for (var line = 1; line <= 2; line++)
            {
                var trayX = left + 0.06f + ((line - 1) * 0.028f);
                var (portX, portY) = PortAt(left, switchY, line);
                var z = KitFront + 0.03f + (line * 0.006f);
                f.Span(trayX - 0.003f, trayY - 0.009f, KitFront + 0.01f, trayX + 0.003f, trayY + 0.009f, z + 0.003f, SurfaceMaterial.PaintButter);
                f.Span(trayX - 0.003f, portY - 0.003f, z - 0.003f, trayX + 0.003f, trayY, z + 0.003f, SurfaceMaterial.PaintButter);
                f.Span(System.MathF.Min(trayX, portX) - 0.003f, portY - 0.003f, z - 0.003f, System.MathF.Max(trayX, portX) + 0.003f, portY + 0.003f, z + 0.003f, SurfaceMaterial.PaintButter);
                f.Span(portX - 0.003f, portY - 0.003f, KitFront + 0.003f, portX + 0.003f, portY + 0.003f, z, SurfaceMaterial.PaintButter);
            }

            f.Span(left - 0.06f, Floor, back, left - 0.03f, Top(0), back + 0.03f, SurfaceMaterial.PaintButter);
        }

        // Port 1 is top left, port 2 below it, and so on in pairs.
        private static (float X, float Y) PortAt(float left, float centreY, int port)
        {
            var column = (port - 1) / 2;
            var row = (port - 1) % 2;
            return (left + 0.04f + (column * 0.03f), centreY + (row == 0 ? 0.01f : -0.01f));
        }

        // The little computer on a shelf to the left of the rack, with its screen on a bracket
        // above it, and the duct the fibre comes up through.
        private static WallMount Computer(PropFrame f)
        {
            var left = -HalfWidth + Wall + 0.02f;
            var right = RackX - RackHalf - 0.08f;
            f.Span(left, 0.62f, -HalfDepth + Wall, right, 0.645f, KitFront - 0.02f, SurfaceMaterial.Iron);
            f.Span(left + 0.03f, 0.645f, -0.08f, right - 0.04f, 0.69f, 0.06f, SurfaceMaterial.PaintBlack);
            f.Span(left + 0.05f, 0.66f, 0.06f, left + 0.07f, 0.67f, 0.062f, SurfaceMaterial.PaintBlue);

            var centreX = (left + right) * 0.5f;
            f.Span(centreX - 0.015f, 0.85f, -HalfDepth + Wall, centreX + 0.015f, 1.0f, -0.02f, SurfaceMaterial.Iron);
            f.Span(left + 0.01f, 0.84f, -0.02f, right - 0.01f, 1.12f, KitFront - 0.02f, SurfaceMaterial.PaintBlack);
            var screenFront = KitFront - 0.02f;
            f.Span(left + 0.025f, 0.855f, screenFront, right - 0.025f, 1.105f, screenFront + 0.002f, SurfaceMaterial.PaintBlack);

            // A terminal: a few lines of green text.
            var lines = new[] { 0.8f, 0.55f, 0.7f, 0.3f, 0.6f, 0.45f };
            for (var i = 0; i < lines.Length; i++)
            {
                var y = 1.08f - (i * 0.03f);
                f.Span(left + 0.04f, y - 0.006f, screenFront + 0.002f, left + 0.04f + ((right - left - 0.08f) * lines[i]), y + 0.006f, screenFront + 0.003f, SurfaceMaterial.PaintGreen);
            }

            f.Prism(left + 0.1f, Floor, -0.08f, 0.05f, 0.06f, 8, SurfaceMaterial.PaintBlack);
            return new WallMount(f.Point(centreX, 0.98f, screenFront + 0.002f), f.Forward, right - left - 0.05f, 0.25f);
        }
    }
}
