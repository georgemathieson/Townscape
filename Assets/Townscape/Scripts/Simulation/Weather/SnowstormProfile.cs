using System;
using System.Numerics;
using Townscape.Generation.Maths;

namespace Townscape.Simulation.Weather
{
    /// <summary>
    /// Snow that comes in squalls on a raw north-easterly, gusting harder than the storm's wind
    /// so the flakes drive sideways, with the odd rumble of thundersnow. Heavy snow and a strong
    /// wind together close the valley in.
    /// </summary>
    public sealed class SnowstormProfile : IWeatherProfile
    {
        /// <summary>Strikes a minute with the lightning setting at full: thundersnow is rare.</summary>
        public const float MaxStrikesPerMinute = 2.5f;

        // Snow comes on a north-easterly, so the wind blows towards the south-west.
        private const float PrevailingBearingDegrees = 225f;

        private readonly GradientNoise _noise;

        public SnowstormProfile(int seed = 2)
        {
            _noise = new GradientNoise(seed);
        }

        public string Name => "Snowstorm";

        public WeatherConditions Sample(WeatherSettings settings, float time)
        {
            var squall = 0.72f + (0.4f * Noise(time / 50f, 0.5f));
            var snow = GeoMath.Clamp01(settings.Snow * squall);

            var cell = 0.5f + (0.5f * Noise(time / 90f, 3.5f));
            var strikes = settings.Lightning * MaxStrikesPerMinute * cell;

            var steady = settings.Wind * ThunderstormProfile.GaleSpeed * (0.85f + (0.15f * Noise(time / 30f, 7.5f)));
            var gust = MathF.Max(0f, Noise(time / 3f, 11.5f)) * 0.7f * steady;
            var bearing = (PrevailingBearingDegrees + (30f * Noise(time / 100f, 15.5f))) * MathF.PI / 180f;
            var wind = new Vector2(MathF.Sin(bearing), MathF.Cos(bearing)) * (steady + gust);

            var mist = GeoMath.Clamp01(0.3f + (0.5f * snow) + (0.15f * settings.Wind) + (0.1f * Noise(time / 60f, 19.5f)));
            return new WeatherConditions(0f, wind, strikes, mist, snow);
        }

        private float Noise(float t, float lane) => GeoMath.Clamp(_noise.Sample(t, lane), -1f, 1f);
    }
}
