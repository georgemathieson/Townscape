namespace Townscape.State
{
    public sealed record SetWeatherKind(WeatherKind Kind) : IAction;

    public sealed record SetRainIntensity(float Intensity) : IAction;

    public sealed record SetLightningFrequency(float Frequency) : IAction;

    public sealed record SetWindStrength(float Strength) : IAction;

    /// <summary>Ask for a lightning strike now, whatever the frequency.</summary>
    public sealed record RequestLightningStrike : IAction;
}
