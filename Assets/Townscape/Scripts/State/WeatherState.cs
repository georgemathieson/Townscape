namespace Townscape.State
{
    /// <summary>Weather profiles. Only the storm exists for now; others slot in as new strategies.</summary>
    public enum WeatherKind
    {
        Storm,
    }

    /// <summary>User-controlled weather settings. All intensities are normalised to [0, 1].</summary>
    public sealed record WeatherState(WeatherKind Kind, float RainIntensity, float LightningFrequency, float WindStrength)
    {
        public static readonly WeatherState DefaultStorm = new WeatherState(WeatherKind.Storm, 0.75f, 0.5f, 0.45f);
    }
}
