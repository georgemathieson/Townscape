namespace Townscape.State
{
    public static class TimeOfDayReducer
    {
        public static TimeOfDayState Reduce(TimeOfDayState state, IAction action)
        {
            TimeOfDayState next;
            switch (action)
            {
                case SelectTimePreset select:
                    next = state with { TargetHour = TimePresets.HourOf(select.Preset), Preset = select.Preset, AutoCycle = false };
                    break;
                case SetTargetHour setHour:
                    next = state with { TargetHour = TimeMath.WrapHour(setHour.Hour), Preset = null, AutoCycle = false };
                    break;
                case SetAutoCycle autoCycle:
                    next = state with { TargetHour = TimeMath.WrapHour(autoCycle.FromHour), Preset = null, AutoCycle = autoCycle.Enabled };
                    break;
                case SetCycleSpeed speed:
                    next = state with
                    {
                        CycleHoursPerMinute = TimeMath.Clamp(
                            speed.HoursPerMinute,
                            TimeOfDayState.MinCycleHoursPerMinute,
                            TimeOfDayState.MaxCycleHoursPerMinute),
                    };
                    break;
                default:
                    return state;
            }

            return next == state ? state : next;
        }
    }
}
