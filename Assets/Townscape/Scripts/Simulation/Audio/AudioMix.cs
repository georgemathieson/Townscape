using System;
using Townscape.Simulation.Weather;

namespace Townscape.Simulation.Audio
{
    /// <summary>How loud each sound plays for the weather at the moment.</summary>
    public static class AudioMix
    {
        /// <summary>Wind speed at which the wind is at its loudest, in metres per second.</summary>
        private const float LoudestWind = 16f;

        public static float RainVolume(float rain) => rain <= 0.001f ? 0f : 0.2f + (0.8f * MathF.Pow(Math.Clamp(rain, 0f, 1f), 0.8f));

        public static float WindVolume(float windSpeed) => 0.75f * MathF.Pow(Math.Clamp(windSpeed / LoudestWind, 0f, 1f), 1.2f);

        /// <summary>Stronger wind sounds a little higher.</summary>
        public static float WindPitch(float windSpeed) => 0.85f + (0.3f * Math.Clamp(windSpeed / LoudestWind, 0f, 1f));

        /// <summary>The river runs a little fuller in heavy rain.</summary>
        public static float RiverVolume(float rain) => 0.55f + (0.35f * Math.Clamp(rain, 0f, 1f));

        public static float ThunderVolume(Thunder thunder) => MathF.Pow(thunder.Loudness, 0.7f);

        /// <summary>Each peal is pitched slightly differently, and far thunder lower still.</summary>
        public static float ThunderPitch(Thunder thunder) =>
            (0.9f + (0.2f * LightSchedule.Hash01(thunder.StrikeId))) * (thunder.Distance > 1500f ? 0.85f : 1f);

        /// <summary>Which of <paramref name="count"/> recordings to play for a strike, or -1 if there are none.</summary>
        public static int Variant(int strikeId, int count) =>
            count <= 0 ? -1 : Math.Min(count - 1, (int)(LightSchedule.Hash01((strikeId * 7) + 3) * count));
    }
}
