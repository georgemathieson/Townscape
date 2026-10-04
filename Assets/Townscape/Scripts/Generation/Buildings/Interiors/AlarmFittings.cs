using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>
    /// A burglar alarm's hardware: motion sensors high in the corners of rooms, keypads on the
    /// wall by the doors, the control box the zones are wired back to, and the bell box outside.
    /// Each returns where it is, so the alarm in Unity can watch through the sensors, light their
    /// LEDs and the keypads' lights, flash the strobe, and open the control box.
    /// </summary>
    public static class AlarmFittings
    {
        /// <summary>A bell box about the size of a real one: 26 by 34 cm, 8 cm deep.</summary>
        public const float BellBoxWidth = 0.26f;

        public const float BellBoxHeight = 0.34f;

        public const float BellBoxDepth = 0.08f;

        public const float KeypadWidth = 0.16f;

        public const float KeypadHeight = 0.22f;

        /// <summary>How far the keypad stands out from its wall.</summary>
        public const float KeypadDepth = 0.028f;

        /// <summary>The control box: a white square 30 cm across.</summary>
        public const float ControlBoxSize = 0.3f;

        public const float ControlBoxDepth = 0.09f;

        private const float SensorWidth = 0.065f;
        private const float SensorHeight = 0.11f;
        private const float SensorDepth = 0.05f;

        /// <summary>
        /// A sensor in the corner at (<paramref name="x"/>, <paramref name="d"/>), just under the
        /// ceiling, looking diagonally into the room (along <paramref name="intoX"/>, <paramref name="intoD"/>).
        /// </summary>
        public static AlarmZone CornerSensor(BuildContext context, UnitSpace space, string name, float x, float ceiling, float d, float intoX, float intoD)
        {
            // Square across the corner, its back edges touching both walls.
            var facing = Vector3.Normalize((space.Right * intoX) + (space.Back * intoD));
            return Sensor(context, name, space.At(x, ceiling - 0.03f - (SensorHeight * 0.5f), d), facing, SensorWidth * 0.5f);
        }

        /// <summary>A sensor flat on a wall at (x, y, d), looking straight out of it (along <paramref name="intoX"/>, <paramref name="intoD"/>).</summary>
        public static AlarmZone WallSensor(BuildContext context, UnitSpace space, string name, float x, float y, float d, float intoX, float intoD)
        {
            var facing = Vector3.Normalize((space.Right * intoX) + (space.Back * intoD));
            return Sensor(context, name, space.At(x, y, d), facing, 0f);
        }

        /// <summary>A contact on a door (or a window that opens), wired back as a zone of its own.</summary>
        public static AlarmZone DoorContact(string name, TownDoor door) =>
            new AlarmZone(name, AlarmZoneKind.Door, door.Hinge, Vector3.Zero, Vector3.Zero, door.Name);

        /// <summary>
        /// A keypad on a wall running back through the building at <paramref name="wallX"/>,
        /// standing out of it towards <paramref name="outX"/> (1 to the right, -1 to the left),
        /// centred at depth <paramref name="d"/> and height <paramref name="y"/>: a white case
        /// with a little screen, a power light and a fault light, and three columns of keys.
        /// </summary>
        public static AlarmKeypadMount Keypad(BuildContext context, UnitSpace space, float wallX, float outX, float d, float y)
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

            // The two lights under the screen, dark until the alarm lights them.
            var lightY = y + 0.032f;
            var lightDs = new[] { d - 0.03f, d + 0.03f };
            foreach (var lightD in lightDs)
            {
                Slab(face, face + (outX * 0.003f), lightY - 0.004f, lightD - 0.004f, lightY + 0.004f, lightD + 0.004f, SurfaceMaterial.AlarmLed);
            }

            var outward = space.Right * outX;
            var panel = new WallMount(space.At(face, y, d), outward, KeypadWidth, KeypadHeight);
            Vector3 Light(float lightD) => space.At(face + (outX * 0.003f), lightY, lightD);
            return new AlarmKeypadMount(panel, Light(lightDs[0]), Light(lightDs[1]));
        }

        /// <summary>
        /// The control box on a wall running back through the building at <paramref name="wallX"/>
        /// (standing out towards <paramref name="outX"/>), centred at depth <paramref name="d"/> and
        /// height <paramref name="y"/>: a plain white square with a screw in each corner of its lid.
        /// </summary>
        public static WallMount ControlBox(BuildContext context, UnitSpace space, float wallX, float outX, float d, float y)
        {
            var builder = context.Builder;
            var face = wallX + (outX * ControlBoxDepth);
            var half = ControlBoxSize * 0.5f;
            space.Box(builder, System.Math.Min(wallX, face), y - half, d - half, System.Math.Max(wallX, face), y + half, d + half, SurfaceMaterial.Porcelain);
            foreach (var (sy, sd) in new[] { (-1f, -1f), (-1f, 1f), (1f, -1f), (1f, 1f) })
            {
                var screwY = y + (sy * (half - 0.025f));
                var screwD = d + (sd * (half - 0.025f));
                space.Box(builder, System.Math.Min(face, face + (outX * 0.004f)), screwY - 0.007f, screwD - 0.007f, System.Math.Max(face, face + (outX * 0.004f)), screwY + 0.007f, screwD + 0.007f, SurfaceMaterial.Chrome);
            }

            return new WallMount(space.At(face, y, d), space.Right * outX, ControlBoxSize, ControlBoxSize);
        }

        /// <summary>
        /// A bell box on the outside of a wall, its top at <paramref name="top"/> and its back
        /// <paramref name="zBack"/> out from the wall (on a shop's fascia, say): one plain case in
        /// <paramref name="casing"/> with the blue strobe set into its foot. Returns the strobe.
        /// </summary>
        public static WallMount BellBox(BuildContext context, WallFrame wall, float centreX, float top, float zBack, SurfaceMaterial casing)
        {
            var builder = context.Builder;
            var x0 = centreX - (BellBoxWidth * 0.5f);
            var x1 = centreX + (BellBoxWidth * 0.5f);
            var y0 = top - BellBoxHeight;
            var front = zBack + BellBoxDepth;
            wall.Block(builder, x0, y0, x1, top, zBack, front, casing);
            wall.Block(builder, x0 + 0.02f, y0 + 0.012f, x1 - 0.02f, y0 + 0.058f, zBack + 0.03f, front + 0.004f, SurfaceMaterial.AlarmStrobe);
            return new WallMount(wall.Point(centreX, y0 + 0.035f, front + 0.004f), wall.Out, BellBoxWidth - 0.04f, 0.046f);
        }

        // A small white box with a pale lens and a red LED, its back standOff from the corner or wall.
        private static AlarmZone Sensor(BuildContext context, string name, Vector3 mount, Vector3 facing, float standOff)
        {
            var builder = context.Builder;
            var up = Vector3.UnitY;
            var across = Vector3.Normalize(Vector3.Cross(up, facing));
            var centre = mount + (facing * (standOff + (SensorDepth * 0.5f)));
            var front = centre + (facing * (SensorDepth * 0.5f));
            var led = front + (facing * 0.003f) - (up * 0.035f);
            builder.AddBox(centre, across, up, facing, new Vector3(SensorWidth * 0.5f, SensorHeight * 0.5f, SensorDepth * 0.5f), SurfaceMaterial.Porcelain, includeBottom: true);
            builder.AddBox(front + (facing * 0.006f) + (up * 0.015f), across, up, facing, new Vector3(SensorWidth * 0.4f, SensorHeight * 0.28f, 0.006f), SurfaceMaterial.Linen, includeBottom: true);
            builder.AddBox(led, across, up, facing, new Vector3(0.005f, 0.005f, 0.003f), SurfaceMaterial.AlarmLed, includeBottom: true);
            return new AlarmZone(name, AlarmZoneKind.Motion, front, facing, led + (facing * 0.003f));
        }
    }
}
