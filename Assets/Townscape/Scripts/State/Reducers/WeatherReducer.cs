namespace Townscape.State
{
    public static class WeatherReducer
    {
        public static WeatherState Reduce(WeatherState state, IAction action)
        {
            WeatherState next;
            switch (action)
            {
                case SetWeatherKind kind:
                    next = state with { Kind = kind.Kind };
                    break;
                case SetRainIntensity rain:
                    next = state with { RainIntensity = TimeMath.Clamp01(rain.Intensity) };
                    break;
                case SetSnowIntensity snow:
                    next = state with { SnowIntensity = TimeMath.Clamp01(snow.Intensity) };
                    break;
                case SetLightningFrequency lightning:
                    next = state with { LightningFrequency = TimeMath.Clamp01(lightning.Frequency) };
                    break;
                case SetWindStrength wind:
                    next = state with { WindStrength = TimeMath.Clamp01(wind.Strength) };
                    break;
                case RequestLightningStrike:
                    next = state with { StrikeRequests = state.StrikeRequests + 1 };
                    break;
                default:
                    return state;
            }

            return next == state ? state : next;
        }
    }
}
