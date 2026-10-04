using System;
using System.Numerics;

namespace Townscape.Simulation.Security
{
    /// <summary>
    /// A passive infrared sensor high in the corner of a room: it sees someone moving within its
    /// range and inside a wide cone around the way it faces. Walls are left to the caller, who
    /// checks there's a clear line from the sensor to the person.
    /// </summary>
    public static class MotionSensor
    {
        /// <summary>How far it sees, in metres: more than across any room.</summary>
        public const float Range = 10f;

        /// <summary>Half the width of its view, in degrees: from a corner it covers the whole room.</summary>
        public const float HalfAngle = 62f;

        /// <summary>How far below the sensor it looks, in degrees, so it covers the floor near the walls too.</summary>
        public const float Tilt = 28f;

        /// <summary>Where it looks from a corner: towards the far corner, tilted down into the room.</summary>
        public static Vector3 Facing(Vector3 intoRoom)
        {
            var level = new Vector3(intoRoom.X, 0f, intoRoom.Z);
            level = level.LengthSquared() > 1e-8f ? Vector3.Normalize(level) : Vector3.UnitZ;
            var tilt = Tilt * MathF.PI / 180f;
            return Vector3.Normalize((level * MathF.Cos(tilt)) - (Vector3.UnitY * MathF.Sin(tilt)));
        }

        /// <summary>Whether someone at <paramref name="target"/> is within the sensor's range and view.</summary>
        public static bool Covers(Vector3 sensor, Vector3 facing, Vector3 target)
        {
            var offset = target - sensor;
            var distance = offset.Length();
            if (distance > Range)
            {
                return false;
            }

            if (distance < 0.3f)
            {
                return true;
            }

            var cos = Vector3.Dot(offset / distance, Vector3.Normalize(facing));
            return cos >= MathF.Cos(HalfAngle * MathF.PI / 180f);
        }
    }
}
