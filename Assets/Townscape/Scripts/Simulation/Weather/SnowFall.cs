using System;
using System.Numerics;
using Townscape.Generation.Maths;

namespace Townscape.Simulation.Weather
{
    /// <summary>How falling snow moves and how much of it to draw.</summary>
    public static class SnowFall
    {
        /// <summary>Snowflakes drift down at about a metre a second.</summary>
        public const float FallSpeed = 1.1f;

        /// <summary>Flakes in each cubic metre of air round the camera in the heaviest snow.</summary>
        public const float MaxFlakesPerCubicMetre = 2.6f;

        /// <summary>Flakes are so light that the wind carries them along almost at its own speed.</summary>
        public static Vector3 Velocity(Vector2 wind) => new Vector3(wind.X * 0.95f, -FallSpeed, wind.Y * 0.95f);

        public static float FlakesPerCubicMetre(float snow) => MaxFlakesPerCubicMetre * MathF.Pow(GeoMath.Clamp01(snow), 1.2f);

        /// <summary>How hard flakes flutter and swirl as they fall, in metres per second: more in the wind.</summary>
        public static float Flutter(float windSpeed) => 0.35f + (0.04f * windSpeed);
    }
}
