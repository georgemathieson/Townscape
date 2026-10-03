using System.Numerics;

namespace Townscape.Simulation.Weather
{
    /// <summary>The user's weather settings, each from 0 to 1.</summary>
    public readonly struct WeatherSettings
    {
        public WeatherSettings(float rain, float lightning, float wind)
        {
            Rain = rain;
            Lightning = lightning;
            Wind = wind;
        }

        public float Rain { get; }

        public float Lightning { get; }

        public float Wind { get; }
    }

    /// <summary>What the weather is doing at one moment.</summary>
    public readonly struct WeatherConditions
    {
        public WeatherConditions(float rain, Vector2 wind, float strikesPerMinute, float mist)
        {
            Rain = rain;
            Wind = wind;
            StrikesPerMinute = strikesPerMinute;
            Mist = mist;
        }

        /// <summary>How hard it is raining, from 0 (dry) to 1 (a downpour).</summary>
        public float Rain { get; }

        /// <summary>Wind velocity in metres per second: x east, y north, pointing where the wind blows to.</summary>
        public Vector2 Wind { get; }

        public float WindSpeed => Wind.Length();

        public float StrikesPerMinute { get; }

        /// <summary>Low mist and haze, from 0 to 1.</summary>
        public float Mist { get; }
    }

    /// <summary>
    /// Strategy for a kind of weather. A profile turns the user's settings and the clock into
    /// conditions; the effects in Unity only ever see conditions, so new weather (clear skies, fog,
    /// snow) is a new profile rather than a change to every effect.
    /// </summary>
    public interface IWeatherProfile
    {
        string Name { get; }

        WeatherConditions Sample(WeatherSettings settings, float time);
    }
}
