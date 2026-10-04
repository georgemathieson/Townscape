using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>
    /// A burglar alarm's hardware: motion sensors high in the corners of rooms, the control panel
    /// on the wall by the door, and the bell box outside. Each leaves an anchor for the alarm in
    /// Unity, which watches through the sensors, lights their LEDs and flashes the strobe.
    /// </summary>
    public static class AlarmFittings
    {
        /// <summary>A bell box about the size of a real one: 26 by 34 cm, 8 cm deep.</summary>
        public const float BellBoxWidth = 0.26f;

        public const float BellBoxHeight = 0.34f;

        private const float BellBoxDepth = 0.08f;

        public const float KeypadWidth = 0.16f;

        public const float KeypadHeight = 0.22f;

        /// <summary>How far the panel stands out from its wall.</summary>
        public const float KeypadDepth = 0.028f;

        private const float SensorWidth = 0.065f;
        private const float SensorHeight = 0.11f;
        private const float SensorDepth = 0.05f;

        /// <summary>
        /// A sensor in the corner at (<paramref name="x"/>, <paramref name="d"/>), just under the
        /// ceiling, looking diagonally into the room (along <paramref name="intoX"/>, <paramref name="intoD"/>).
        /// </summary>
        public static void CornerSensor(BuildContext context, UnitSpace space, float x, float ceiling, float d, float intoX, float intoD)
        {
            // Square across the corner, its back edges touching both walls.
            var facing = Vector3.Normalize((space.Right * intoX) + (space.Back * intoD));
            Sensor(context, space.At(x, ceiling - 0.03f - (SensorHeight * 0.5f), d), facing, SensorWidth * 0.5f);
        }

        /// <summary>A sensor flat on a wall at (x, y, d), looking straight out of it (along <paramref name="intoX"/>, <paramref name="intoD"/>).</summary>
        public static void WallSensor(BuildContext context, UnitSpace space, float x, float y, float d, float intoX, float intoD)
        {
            var facing = Vector3.Normalize((space.Right * intoX) + (space.Back * intoD));
            Sensor(context, space.At(x, y, d), facing, 0f);
        }

        /// <summary>
        /// The control panel on a wall running back through the building at <paramref name="wallX"/>,
        /// standing out of it towards <paramref name="outX"/> (1 to the right, -1 to the left),
        /// centred at depth <paramref name="d"/> and height <paramref name="y"/>: a white case
        /// with a little screen at the top and three columns of keys.
        /// </summary>
        public static void Keypad(BuildContext context, UnitSpace space, float wallX, float outX, float d, float y)
        {
            var builder = context.Builder;
            var face = wallX + (outX * KeypadDepth);
            void Slab(float from, float to, float y0, float d0, float y1, float d1, SurfaceMaterial material) =>
                space.Box(builder, System.Math.Min(from, to), y0, d0, System.Math.Max(from, to), y1, d1, material);

            var halfWidth = KeypadWidth * 0.5f;
            var halfHeight = KeypadHeight * 0.5f;
            Slab(wallX, face, y - halfHeight, d - halfWidth, y + halfHeight, d + halfWidth, SurfaceMaterial.Porcelain);
            Slab(face, face + (outX * 0.003f), y + 0.045f, d - 0.055f, y + 0.088f, d + 0.055f, SurfaceMaterial.Screen);
            for (var row = 0; row < 4; row++)
            {
                for (var column = -1; column <= 1; column++)
                {
                    var keyY = y + 0.012f - (row * 0.03f);
                    var keyD = d + (column * 0.036f);
                    Slab(face, face + (outX * 0.005f), keyY - 0.009f, keyD - 0.013f, keyY + 0.009f, keyD + 0.013f, SurfaceMaterial.Iron);
                }
            }

            context.Anchor(AnchorKind.AlarmKeypad, space.At(face, y, d), space.Right * outX, KeypadWidth);
        }

        /// <summary>
        /// The bell box on the outside of a wall, its top at <paramref name="top"/>: one plain
        /// white case with the blue strobe set into its foot.
        /// </summary>
        public static void BellBox(BuildContext context, WallFrame wall, float centreX, float top)
        {
            var builder = context.Builder;
            var x0 = centreX - (BellBoxWidth * 0.5f);
            var x1 = centreX + (BellBoxWidth * 0.5f);
            var y0 = top - BellBoxHeight;
            wall.Block(builder, x0, y0, x1, top, 0f, BellBoxDepth, SurfaceMaterial.Porcelain);
            wall.Block(builder, x0 + 0.02f, y0 + 0.012f, x1 - 0.02f, y0 + 0.058f, 0.03f, BellBoxDepth + 0.004f, SurfaceMaterial.AlarmStrobe);
            context.Anchor(AnchorKind.AlarmBell, wall.Point(centreX, y0 + 0.035f, BellBoxDepth), wall.Out, BellBoxWidth);
        }

        // A small white box with a pale lens and a red LED, its back standOff from the corner or wall.
        private static void Sensor(BuildContext context, Vector3 mount, Vector3 facing, float standOff)
        {
            var builder = context.Builder;
            var up = Vector3.UnitY;
            var across = Vector3.Normalize(Vector3.Cross(up, facing));
            var centre = mount + (facing * (standOff + (SensorDepth * 0.5f)));
            var front = centre + (facing * (SensorDepth * 0.5f));
            builder.AddBox(centre, across, up, facing, new Vector3(SensorWidth * 0.5f, SensorHeight * 0.5f, SensorDepth * 0.5f), SurfaceMaterial.Porcelain, includeBottom: true);
            builder.AddBox(front + (facing * 0.006f) + (up * 0.015f), across, up, facing, new Vector3(SensorWidth * 0.4f, SensorHeight * 0.28f, 0.006f), SurfaceMaterial.Linen, includeBottom: true);
            builder.AddBox(front + (facing * 0.003f) - (up * 0.035f), across, up, facing, new Vector3(0.005f, 0.005f, 0.003f), SurfaceMaterial.AlarmLed, includeBottom: true);
            context.Anchor(AnchorKind.AlarmSensor, front, facing, SensorWidth);
        }
    }
}
