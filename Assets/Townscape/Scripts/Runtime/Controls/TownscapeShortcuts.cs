using Townscape.Runtime.CoffeeShop;
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
        private ControlPanel _panel;
        private PerformanceOverlay _performance;
        private CoffeeShopGame _coffee;

        public void Initialize(Store<TownState> store, ITownscapeInput input, TimeOfDayLighting lighting, ControlPanel panel, PerformanceOverlay performance, CoffeeShopGame coffee = null)
        {
            _coffee = coffee;
            _store = store;
            _input = input;
            _lighting = lighting;
            _panel = panel;
            _performance = performance;
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
            if (_input.WasPressed(Shortcut.CycleWeather))
            {
                _store.Dispatch(new SetWeatherKind(WeatherKinds.Next(weather.Kind)));
            }

            // R turns up whatever is falling: rain in the thunderstorm, snow in the snowstorm.
            if (_input.WasPressed(Shortcut.CycleRain) && weather.Kind == WeatherKind.Snow)
            {
                _store.Dispatch(new SetSnowIntensity(WeatherSteps.Next(weather.SnowIntensity, WeatherSteps.Snow)));
            }
            else if (_input.WasPressed(Shortcut.CycleRain))
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

            if (_input.WasPressed(Shortcut.ToggleMute))
            {
                _store.Dispatch(new SetMuted(!_store.State.Audio.Muted));
            }

            // Showing the panel is how it looks on screen, not a setting, so it isn't in the store.
            if (_input.WasPressed(Shortcut.ToggleHelp) && _panel != null)
            {
                _panel.Visible = !_panel.Visible;
            }

            if (_input.WasPressed(Shortcut.CyclePerformance) && _performance != null)
            {
                _performance.Cycle();
            }

            if (_input.WasPressed(Shortcut.ToggleCoffeeShop) && _coffee != null)
            {
                _coffee.Toggle();
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
