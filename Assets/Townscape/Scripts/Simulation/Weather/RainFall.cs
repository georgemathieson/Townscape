using System;
using System.Numerics;

namespace Townscape.Simulation.Weather
{
    /// <summary>How the rain moves and how much of it to draw.</summary>
    public static class RainFall
    {
        /// <summary>Terminal speed of a large raindrop, in metres per second.</summary>
        public const float FallSpeed = 9f;

        /// <summary>Drops a second over the area round the camera in a downpour.</summary>
        public const float MaxDropsPerSecond = 4200f;

        /// <summary>Drops are carried almost at the speed of the wind, so they slant downwind.</summary>
        public static Vector3 Velocity(Vector2 wind) => new Vector3(wind.X * 0.85f, -FallSpeed, wind.Y * 0.85f);

        public static float DropsPerSecond(float rain) => MaxDropsPerSecond * MathF.Pow(Math.Clamp(rain, 0f, 1f), 1.3f);

        /// <summary>Splashes per drop that lands: more in heavy rain, so it does not look sparse up close.</summary>
        public static float SplashChance(float rain) => 0.35f + (0.4f * Math.Clamp(rain, 0f, 1f));
    }

    /// <summary>Which chimneys have a fire lit. More fires burn in the evening.</summary>
    public static class ChimneySmoke
    {
        public static bool IsBurning(int seed, float hour)
        {
            var evening = LightSchedule.Window(hour, 16f, 23.5f);
            var morning = LightSchedule.Window(hour, 6.5f, 9.5f);
            var share = 0.35f + (0.35f * MathF.Max(evening, morning * 0.6f));
            return LightSchedule.Hash01((seed * 31) + 5) < share;
        }

        /// <summary>
        /// Puffs a second from one chimney: enough that they overlap into a plume, and a few more
        /// when the wind draws the fire.
        /// </summary>
        public static float PuffsPerSecond(float windSpeed) => 3f + (0.1f * windSpeed);
    }
}
