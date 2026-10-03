using System;
using System.Numerics;
using Townscape.Generation.Maths;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// Rain that comes in surges, a south-westerly wind that gusts and veers, and storm cells that
    /// drift through and make the lightning come in bursts.
    /// </summary>
    public sealed class ThunderstormProfile : IWeatherProfile
    {
        /// <summary>Wind speed with the wind setting at full: a gale, in metres per second.</summary>
        public const float GaleSpeed = 14f;

        /// <summary>Strikes a minute with the lightning setting at full and a storm cell overhead.</summary>
        public const float MaxStrikesPerMinute = 12f;

        // The prevailing wind comes from the south-west, so it blows towards the north-east.
        private const float PrevailingBearingDegrees = 45f;

        private readonly GradientNoise _noise;

        public ThunderstormProfile(int seed = 1)
        {
            _noise = new GradientNoise(seed);
        }

        public string Name => "Thunderstorm";

        public WeatherConditions Sample(WeatherSettings settings, float time)
        {
            var surge = 0.78f + (0.32f * Noise(time / 40f, 0.5f));
            var rain = GeoMath.Clamp01(settings.Rain * surge);

            var cell = 0.5f + (0.5f * Noise(time / 75f, 3.5f));
            var strikes = settings.Lightning * MaxStrikesPerMinute * (0.25f + (0.75f * cell));

            var steady = settings.Wind * GaleSpeed * (0.85f + (0.15f * Noise(time / 30f, 7.5f)));
            var gust = MathF.Max(0f, Noise(time / 3.5f, 11.5f)) * 0.55f * steady;
            var bearing = (PrevailingBearingDegrees + (25f * Noise(time / 120f, 15.5f))) * MathF.PI / 180f;
            var wind = new Vector2(MathF.Sin(bearing), MathF.Cos(bearing)) * (steady + gust);

            var mist = GeoMath.Clamp01(0.25f + (0.45f * rain) + (0.2f * Noise(time / 60f, 19.5f)));
            return new WeatherConditions(rain, wind, strikes, mist);
        }

        private float Noise(float t, float lane) => GeoMath.Clamp(_noise.Sample(t, lane), -1f, 1f);
    }
}
