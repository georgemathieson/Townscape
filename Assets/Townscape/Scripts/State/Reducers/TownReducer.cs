namespace Townscape.State
{
    /// <summary>Root reducer: hands each action to every slice reducer (like Redux's combineReducers).</summary>
    public static class TownReducer
    {
        public static TownState Reduce(TownState state, IAction action)
        {
            if (action is ResetSettings reset)
            {
                // The strike count only ever goes up (the storm strikes when it changes), so it survives a reset.
                var defaults = TownState.CreateDefault(reset.Preset);
                return defaults with { Weather = defaults.Weather with { StrikeRequests = state.Weather.StrikeRequests } };
            }

            var timeOfDay = TimeOfDayReducer.Reduce(state.TimeOfDay, action);
            var weather = WeatherReducer.Reduce(state.Weather, action);
            var audio = AudioReducer.Reduce(state.Audio, action);

            if (ReferenceEquals(timeOfDay, state.TimeOfDay) && ReferenceEquals(weather, state.Weather) && ReferenceEquals(audio, state.Audio))
            {
                return state;
            }

            return state with { TimeOfDay = timeOfDay, Weather = weather, Audio = audio };
        }
    }
}
