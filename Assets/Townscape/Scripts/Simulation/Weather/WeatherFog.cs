using System;
using Townscape.Generation.Maths;

namespace Townscape.Simulation.Weather
{
    /// <summary>How the weather thickens the fog and colours it.</summary>
    public static class WeatherFog
    {
        /// <summary>
        /// Multiplies the time of day's fog density: a little more in mist, and a lot more in
        /// driving snow, so a blizzard closes the valley in.
        /// </summary>
        public static float Scale(in WeatherConditions conditions)
        {
            var windiness = GeoMath.Clamp01(conditions.WindSpeed / ThunderstormProfile.GaleSpeed);
            var snow = MathF.Pow(GeoMath.Clamp01(conditions.Snow), 1.3f) * (0.6f + (0.4f * windiness));
            return 0.9f + (0.3f * conditions.Mist) + (1.8f * snow);
        }

        /// <summary>How far the fog turns from the colour of the sky to the pale grey of falling snow, from 0 to 0.8.</summary>
        public static float Whiteness(in WeatherConditions conditions) => 0.8f * GeoMath.SmoothStep(0f, 0.9f, conditions.Snow);
    }
}
