using System;
using System.Numerics;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// How far wind pushes a point on a plant. Plants lean downwind and rock: tall trees slowly,
    /// grass quickly. Gusts roll across the land as waves, so neighbouring trees move together
    /// rather than in lockstep or at random.
    /// </summary>
    /// <remarks>
    /// Split in two so it is cheap to run on many vertices: <see cref="Motion"/> is worked out once
    /// per plant (or patch of ground), and each vertex scales it by its own <see cref="Bend"/>.
    /// </remarks>
    public static class WindSway
    {
        // Wavelength and speed of the gust waves that roll downwind, in metres and as a share of the wind speed.
        private const float GustWavelength = 110f;
        private const float GustSpeedShare = 0.9f;

        /// <summary>
        /// How far a point <paramref name="height"/> metres above the base of its plant bends in a
        /// full gale. A cantilever bends more the taller it is: a 9 m tree top moves about 0.75 m.
        /// </summary>
        public static float Bend(float height) => height <= 0f ? 0f : (0.035f * height) + (0.0055f * height * height);

        /// <summary>
        /// The sideways movement of a plant at <paramref name="position"/>, as a share of its full
        /// <see cref="Bend"/>. <paramref name="plantHeight"/> sets how fast it rocks.
        /// </summary>
        public static Vector2 Motion(Vector2 position, float plantHeight, Vector2 wind, float time)
        {
            var speed = wind.Length();
            if (speed < 1e-3f)
            {
                return Vector2.Zero;
            }

            var downwind = wind / speed;
            var across = new Vector2(-downwind.Y, downwind.X);
            var along = Vector2.Dot(position, downwind);
            var side = Vector2.Dot(position, across);
            var strength = speed / ThunderstormProfile.GaleSpeed;

            var wave = (2f * MathF.PI / GustWavelength) * (along - (time * speed * GustSpeedShare));
            var gust = 0.5f + (0.35f * MathF.Sin(wave)) + (0.15f * MathF.Sin((wave * 2.3f) + 1.7f));
            var lean = 0.45f + (0.55f * gust);

            var frequency = 0.3f + (1.1f / (1f + (0.5f * MathF.Max(0f, plantHeight))));
            var phase = (2f * MathF.PI * frequency * time) + (0.13f * along) + (0.29f * side);
            var rock = 0.22f * MathF.Sin(phase);
            var twist = 0.09f * MathF.Sin((phase * 1.37f) + 0.6f);

            return ((downwind * (lean + rock)) + (across * twist)) * strength;
        }

        /// <summary>
        /// The offset of a vertex <paramref name="height"/> metres above the base of its plant at
        /// <paramref name="position"/>, in a wind of <paramref name="wind"/> metres per second.
        /// </summary>
        public static Vector3 Offset(Vector3 position, float height, Vector2 wind, float time)
        {
            if (height <= 0f)
            {
                return Vector3.Zero;
            }

            var horizontal = Motion(new Vector2(position.X, position.Z), height, wind, time) * Bend(height);
            return new Vector3(horizontal.X, -Drop(horizontal.LengthSquared(), height), horizontal.Y);
        }

        /// <summary>How far a point sinks as it bends sideways, so branches swing rather than stretch.</summary>
        public static float Drop(float sidewaysSquared, float height) => sidewaysSquared / (2f * (height + 0.5f));
    }
}
