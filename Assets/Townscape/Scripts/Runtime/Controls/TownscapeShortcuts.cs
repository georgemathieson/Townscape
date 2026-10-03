using Townscape.Runtime.Lighting;
using Townscape.Runtime.UI;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.Controls
{
    /// <summary>Turns key presses into store actions. It never changes lighting or weather directly.</summary>
    [DisallowMultipleComponent]
    public sealed class TownscapeShortcuts : MonoBehaviour
    {
        private Store<TownState> _store;
        private ITownscapeInput _input;
        private TimeOfDayLighting _lighting;
        private HelpOverlay _help;

        public void Initialize(Store<TownState> store, ITownscapeInput input, TimeOfDayLighting lighting, HelpOverlay help)
        {
            _store = store;
            _input = input;
            _lighting = lighting;
            _help = help;
        }

        private void Update()
        {
            if (_store == null)
            {
                return;
            }

            SelectPresetOnPress(Shortcut.Dawn, TimePreset.Dawn);
            SelectPresetOnPress(Shortcut.Day, TimePreset.Day);
            SelectPresetOnPress(Shortcut.Dusk, TimePreset.Dusk);
            SelectPresetOnPress(Shortcut.Night, TimePreset.Night);

            var time = _store.State.TimeOfDay;
            if (_input.WasPressed(Shortcut.ToggleAutoCycle))
            {
                _store.Dispatch(new SetAutoCycle(!time.AutoCycle, _lighting.CurrentHour));
            }

            // Step from wherever the clock is heading, so repeated presses add up during a blend.
            var baseHour = time.AutoCycle ? _lighting.CurrentHour : time.TargetHour;
            if (_input.WasPressed(Shortcut.HourBack))
            {
                _store.Dispatch(new SetTargetHour(Mathf.Round(baseHour) - 1f));
            }

            if (_input.WasPressed(Shortcut.HourForward))
            {
                _store.Dispatch(new SetTargetHour(Mathf.Round(baseHour) + 1f));
            }

            var weather = _store.State.Weather;
            if (_input.WasPressed(Shortcut.CycleRain))
            {
                _store.Dispatch(new SetRainIntensity(WeatherSteps.Next(weather.RainIntensity, WeatherSteps.Rain)));
            }

            if (_input.WasPressed(Shortcut.CycleLightning))
            {
                _store.Dispatch(new SetLightningFrequency(WeatherSteps.Next(weather.LightningFrequency, WeatherSteps.Lightning)));
            }

            if (_input.WasPressed(Shortcut.CycleWind))
            {
                _store.Dispatch(new SetWindStrength(WeatherSteps.Next(weather.WindStrength, WeatherSteps.Wind)));
            }

            if (_input.WasPressed(Shortcut.StrikeLightning))
            {
                _store.Dispatch(new RequestLightningStrike());
            }

            if (_input.WasPressed(Shortcut.ToggleHelp) && _help != null)
            {
                _help.Visible = !_help.Visible;
            }
        }

        private void SelectPresetOnPress(Shortcut shortcut, TimePreset preset)
        {
            if (_input.WasPressed(shortcut))
            {
                _store.Dispatch(new SelectTimePreset(preset));
            }
        }
    }
}
