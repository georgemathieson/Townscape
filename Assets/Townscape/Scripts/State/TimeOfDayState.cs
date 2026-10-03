namespace Townscape.State
{
    public enum TimePreset
    {
        Dawn,
        Day,
        Dusk,
        Night,
    }

    /// <summary>
    /// What the user asked for. The lighting system eases its own clock towards
    /// <see cref="TargetHour"/>, or runs the clock forward when <see cref="AutoCycle"/> is on.
    /// </summary>
    /// <param name="TargetHour">Hour of day in [0, 24).</param>
    /// <param name="Preset">The preset that produced <see cref="TargetHour"/>, or null for a custom hour.</param>
    /// <param name="AutoCycle">When true the clock advances on its own.</param>
    /// <param name="CycleHoursPerMinute">In-game hours that pass per real minute while auto-cycling.</param>
    /// <param name="BlendSeconds">
    /// How quickly the user wants to get there: a few seconds for a preset, a moment while
    /// dragging the time slider so the clock follows the hand.
    /// </param>
    public sealed record TimeOfDayState(float TargetHour, TimePreset? Preset, bool AutoCycle, float CycleHoursPerMinute, float BlendSeconds = 4f)
    {
        /// <summary>Blend time for presets and the hour keys (the default for <see cref="BlendSeconds"/>).</summary>
        public const float DefaultBlendSeconds = 4f;

        /// <summary>Blend time while the time slider is being dragged.</summary>
        public const float ScrubBlendSeconds = 0.15f;

        public const float DefaultCycleHoursPerMinute = 2f;
        public const float MinCycleHoursPerMinute = 0.1f;
        public const float MaxCycleHoursPerMinute = 60f;

        public static TimeOfDayState FromPreset(TimePreset preset) =>
            new TimeOfDayState(TimePresets.HourOf(preset), preset, false, DefaultCycleHoursPerMinute);
    }

    public static class TimePresets
    {
        public static readonly TimePreset[] All = { TimePreset.Dawn, TimePreset.Day, TimePreset.Dusk, TimePreset.Night };

        public static float HourOf(TimePreset preset)
        {
            switch (preset)
            {
                case TimePreset.Dawn: return 6.25f;
                case TimePreset.Day: return 12.5f;
                case TimePreset.Dusk: return 19.25f;
                case TimePreset.Night: return 23f;
                default: return 12f;
            }
        }
    }
}
