namespace Townscape.State
{
    /// <summary>Weather profiles: each is a strategy in the simulation. More slot in the same way.</summary>
    public enum WeatherKind
    {
        Storm,
        Snow,
    }

    /// <summary>User-controlled weather settings. All intensities are normalised to [0, 1].</summary>
    /// <param name="StrikeRequests">
    /// How many times the user has asked for a lightning strike right now. It only ever counts up:
    /// the storm strikes whenever it sees the count change, which keeps a one-off request in the
    /// store like every other user intent.
    /// </param>
    /// <param name="SnowIntensity">
    /// How hard it snows in a snowstorm. Kept apart from the rain, so switching between the two
    /// keeps each the way it was set.
    /// </param>
    public sealed record WeatherState(WeatherKind Kind, float RainIntensity, float LightningFrequency, float WindStrength, int StrikeRequests = 0, float SnowIntensity = WeatherState.DefaultSnow)
    {
        public const float DefaultSnow = 0.7f;

        public static readonly WeatherState DefaultStorm = new WeatherState(WeatherKind.Storm, 0.75f, 0.5f, 0.45f);
    }

    public static class WeatherKinds
    {
        public static readonly WeatherKind[] All = { WeatherKind.Storm, WeatherKind.Snow };

        /// <summary>The name shown on the control panel.</summary>
        public static string Title(WeatherKind kind) => kind == WeatherKind.Snow ? "Snowstorm" : "Thunderstorm";

        /// <summary>The kind after <paramref name="kind"/>, wrapping round to the first.</summary>
        public static WeatherKind Next(WeatherKind kind) => All[(System.Array.IndexOf(All, kind) + 1) % All.Length];
    }

    /// <summary>The steps the keyboard shortcuts cycle through for each weather setting.</summary>
    public static class WeatherSteps
    {
        public static readonly float[] Rain = { 0.25f, 0.5f, 0.75f, 1f };

        public static readonly float[] Snow = { 0.25f, 0.5f, 0.75f, 1f };

        public static readonly float[] Lightning = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        public static readonly float[] Wind = { 0.15f, 0.45f, 0.75f, 1f };

        /// <summary>The first step above <paramref name="current"/>, wrapping round to the lowest.</summary>
        public static float Next(float current, float[] steps)
        {
            foreach (var step in steps)
            {
                if (step > current + 1e-3f)
                {
                    return step;
                }
            }

            return steps[0];
        }
    }
}
