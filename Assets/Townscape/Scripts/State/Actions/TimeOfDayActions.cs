namespace Townscape.State
{
    /// <summary>Jump (with a blend) to one of the named presets. Stops auto-cycling.</summary>
    public sealed record SelectTimePreset(TimePreset Preset) : IAction;

    /// <summary>
    /// Blend to a specific hour. Stops auto-cycling. <paramref name="Scrub"/> is true while the
    /// time slider is being dragged, so the clock follows almost at once instead of blending.
    /// </summary>
    public sealed record SetTargetHour(float Hour, bool Scrub = false) : IAction;

    /// <summary>
    /// Start or stop the automatic day cycle. <paramref name="FromHour"/> is the hour currently shown,
    /// so the clock continues (or holds) from where it is rather than jumping.
    /// </summary>
    public sealed record SetAutoCycle(bool Enabled, float FromHour) : IAction;

    /// <summary>Change how fast the automatic cycle runs, in in-game hours per real minute.</summary>
    public sealed record SetCycleSpeed(float HoursPerMinute) : IAction;
}
