namespace Townscape.State
{
    /// <summary>Root state for the whole scene. Each slice is owned by one reducer.</summary>
    public sealed record TownState(TimeOfDayState TimeOfDay, WeatherState Weather)
    {
        public static TownState CreateDefault(TimePreset preset = TimePreset.Dusk) =>
            new TownState(TimeOfDayState.FromPreset(preset), WeatherState.DefaultStorm);
    }
}
