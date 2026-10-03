namespace Townscape.State
{
    /// <summary>Root state for the whole scene. Each slice is owned by one reducer.</summary>
    public sealed record TownState(TimeOfDayState TimeOfDay, WeatherState Weather, AudioState Audio)
    {
        public static TownState CreateDefault(TimePreset preset = TimePreset.Dusk) =>
            new TownState(TimeOfDayState.FromPreset(preset), WeatherState.DefaultStorm, AudioState.Default);
    }

    /// <summary>Puts every setting back to how it started, with the clock at <paramref name="Preset"/>.</summary>
    public sealed record ResetSettings(TimePreset Preset = TimePreset.Dusk) : IAction;
}
