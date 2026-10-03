namespace Townscape.State
{
    /// <summary>Root reducer: hands each action to every slice reducer (like Redux's combineReducers).</summary>
    public static class TownReducer
    {
        public static TownState Reduce(TownState state, IAction action)
        {
            var timeOfDay = TimeOfDayReducer.Reduce(state.TimeOfDay, action);
            var weather = WeatherReducer.Reduce(state.Weather, action);

            if (ReferenceEquals(timeOfDay, state.TimeOfDay) && ReferenceEquals(weather, state.Weather))
            {
                return state;
            }

            return state with { TimeOfDay = timeOfDay, Weather = weather };
        }
    }
}
