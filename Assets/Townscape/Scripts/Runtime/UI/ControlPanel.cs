using Townscape.Runtime.CoffeeShop;
using Townscape.Runtime.Lighting;
using Townscape.Runtime.Walking;
using Townscape.Runtime.Weather;
using Townscape.Simulation.Weather;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// The on-screen control panel: time of day (presets, a time slider, the running clock),
    /// the storm (rain, lightning, wind, a strike on demand) and sound. Like the keyboard
    /// shortcuts it only ever dispatches actions to the store; it reads the clock and the
    /// weather from their systems just to show them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ControlPanel : MonoBehaviour
    {
        private const float Width = 320f;
        private const float LabelWidth = 76f;
        private const float ValueWidth = 64f;
        private const float MinSpeed = 0.25f;
        private const float MaxSpeed = 30f;

        private const string KeyList =
            "<b>Right mouse</b> look   <b>WASD</b> move   <b>Q/E</b> down/up\n" +
            "<b>Shift</b> faster   <b>Scroll</b> speed\n" +
            "<b>1–4</b> dawn, day, dusk, night   <b>[ ]</b> an hour\n" +
            "<b>T</b> run the clock   <b>N</b> thunderstorm or snowstorm\n" +
            "<b>R L G</b> rain or snow, lightning, wind\n" +
            "<b>B</b> strike   <b>M</b> mute   <b>F</b> frame rate\n" +
            "<b>C</b> Fellside Coffee   <b>H</b> hide this panel\n" +
            "<b>V</b> walk or fly   <b>Space</b> jump   <b>E</b> open";

        private Store<TownState> _store;
        private TimeOfDayLighting _lighting;
        private StormSystem _storm;
        private CoffeeShopGame _coffee;
        private bool _coffeeOpen;
        private PanelSkin _skin;
        private GUIStyle _hint;
        private bool _showKeys;

        public bool Visible { get; set; } = true;

        /// <summary>Walking mode, for the panel's walk button; set once both exist.</summary>
        public WalkingController Walking { get; set; }

        public void Initialize(Store<TownState> store, TimeOfDayLighting lighting, StormSystem storm, CoffeeShopGame coffee = null)
        {
            _store = store;
            _lighting = lighting;
            _storm = storm;
            _coffee = coffee;
        }

        private void OnDestroy()
        {
            _skin?.Dispose();
        }

        private void OnGUI()
        {
            // The coffee shop's window takes the panel's place while it's open. Decided on the
            // layout pass so a frame never lays out one and then draws the other.
            if (Event.current.type == EventType.Layout)
            {
                _coffeeOpen = _coffee != null && _coffee.IsOpen;
            }

            if (_store == null || _lighting == null || _coffeeOpen)
            {
                return;
            }

            _skin ??= new PanelSkin();

            // Scale with the screen, so the panel is readable on a high-resolution display.
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (!Visible)
            {
                _hint ??= new GUIStyle(_skin.Status) { normal = { textColor = new Color(1f, 1f, 1f, 0.55f) } };
                GUI.Label(new Rect(16f, (Screen.height / scale) - 30f, 300f, 20f), "Press H for the controls", _hint);
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, Width, (Screen.height / scale) - 32f));
            GUILayout.BeginVertical(_skin.Panel);
            var state = _store.State;
            Header(state);
            TimeOfDay(state.TimeOfDay);
            Weather(state.Weather);
            Sound(state.Audio);
            CoffeeShopButton();
            Footer();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void Header(TownState state)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Townscape", _skin.Title);
            GUILayout.Label(Clock(_lighting.CurrentHour), _skin.Clock);
            GUILayout.EndHorizontal();

            var time = state.TimeOfDay;
            var when = time.AutoCycle ? "Clock running" : time.Preset.HasValue ? time.Preset.Value.ToString() : "Custom time";
            var status = when;
            if (_storm != null)
            {
                var conditions = _storm.Conditions;
                status += $"  ·  wind {Mathf.RoundToInt(conditions.WindSpeed)} m/s from the {WindDirection.From(conditions.Wind)}";
            }

            GUILayout.Label(status, _skin.Status);
        }

        private void TimeOfDay(TimeOfDayState time)
        {
            GUILayout.Label("TIME OF DAY", _skin.Heading);
            GUILayout.BeginHorizontal();
            foreach (var preset in TimePresets.All)
            {
                var selected = !time.AutoCycle && time.Preset == preset;
                if (GUILayout.Toggle(selected, preset.ToString(), _skin.Button) && !selected)
                {
                    _store.Dispatch(new SelectTimePreset(preset));
                }
            }

            GUILayout.EndHorizontal();

            // While the clock runs the slider follows it; dragging it takes over.
            var shown = time.AutoCycle ? _lighting.CurrentHour : time.TargetHour;
            var hour = Slider("Time", shown, 0f, 23.99f, Clock(shown));
            if (Changed(hour, shown))
            {
                _store.Dispatch(new SetTargetHour(hour, Scrub: true));
            }

            GUILayout.BeginHorizontal();
            var running = GUILayout.Toggle(time.AutoCycle, "Run the clock", _skin.Button, GUILayout.Width(118f));
            if (running != time.AutoCycle)
            {
                _store.Dispatch(new SetAutoCycle(running, _lighting.CurrentHour));
            }

            // Speed on a logarithmic scale: from a quarter of an hour to 30 hours a minute.
            var position = Mathf.InverseLerp(Mathf.Log(MinSpeed), Mathf.Log(MaxSpeed), Mathf.Log(Mathf.Clamp(time.CycleHoursPerMinute, MinSpeed, MaxSpeed)));
            var moved = GUILayout.HorizontalSlider(position, 0f, 1f, _skin.Slider, _skin.Thumb);
            GUILayout.Label($"{time.CycleHoursPerMinute:0.#} h/min", _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();
            if (Changed(moved, position))
            {
                _store.Dispatch(new SetCycleSpeed(Mathf.Exp(Mathf.Lerp(Mathf.Log(MinSpeed), Mathf.Log(MaxSpeed), moved))));
            }
        }

        private void Weather(WeatherState weather)
        {
            GUILayout.Label("WEATHER", _skin.Heading);
            GUILayout.BeginHorizontal();
            foreach (var kind in WeatherKinds.All)
            {
                var selected = weather.Kind == kind;
                if (GUILayout.Toggle(selected, WeatherKinds.Title(kind), _skin.Button) && !selected)
                {
                    _store.Dispatch(new SetWeatherKind(kind));
                }
            }

            GUILayout.EndHorizontal();

            if (weather.Kind == WeatherKind.Snow)
            {
                var snow = Slider("Snow", weather.SnowIntensity, 0f, 1f, Percent(weather.SnowIntensity));
                if (Changed(snow, weather.SnowIntensity))
                {
                    _store.Dispatch(new SetSnowIntensity(snow));
                }
            }
            else
            {
                var rain = Slider("Rain", weather.RainIntensity, 0f, 1f, Percent(weather.RainIntensity));
                if (Changed(rain, weather.RainIntensity))
                {
                    _store.Dispatch(new SetRainIntensity(rain));
                }
            }

            var lightning = Slider("Lightning", weather.LightningFrequency, 0f, 1f, Percent(weather.LightningFrequency));
            if (Changed(lightning, weather.LightningFrequency))
            {
                _store.Dispatch(new SetLightningFrequency(lightning));
            }

            var wind = Slider("Wind", weather.WindStrength, 0f, 1f, Percent(weather.WindStrength));
            if (Changed(wind, weather.WindStrength))
            {
                _store.Dispatch(new SetWindStrength(wind));
            }

            if (GUILayout.Button("Lightning strike now", _skin.Button))
            {
                _store.Dispatch(new RequestLightningStrike());
            }
        }

        private void Sound(AudioState audio)
        {
            GUILayout.Label("SOUND", _skin.Heading);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume", _skin.Label, GUILayout.Width(LabelWidth));
            var volume = GUILayout.HorizontalSlider(audio.Volume, 0f, 1f, _skin.Slider, _skin.Thumb);
            var muted = GUILayout.Toggle(audio.Muted, audio.Muted ? "Muted" : "Mute", _skin.Button, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();

            if (Changed(volume, audio.Volume))
            {
                _store.Dispatch(new SetVolume(volume));
            }

            if (muted != audio.Muted)
            {
                _store.Dispatch(new SetMuted(muted));
            }
        }

        private void CoffeeShopButton()
        {
            if (_coffee == null && Walking == null)
            {
                return;
            }

            GUILayout.Label("GET ABOUT", _skin.Heading);
            GUILayout.BeginHorizontal();
            if (Walking != null)
            {
                var walking = Walking.Active || Walking.Waiting;
                if (GUILayout.Toggle(walking, "Walk (V)", _skin.Button) != walking)
                {
                    Walking.Toggle();
                }
            }

            if (_coffee != null && GUILayout.Button("Run the coffee shop", _skin.Button))
            {
                _coffee.Open();
            }

            GUILayout.EndHorizontal();
        }

        private void Footer()
        {
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset all", _skin.Button))
            {
                _store.Dispatch(new ResetSettings());
            }

            _showKeys = GUILayout.Toggle(_showKeys, _showKeys ? "Hide keys" : "Keys", _skin.Button);
            if (GUILayout.Button("Hide", _skin.Button))
            {
                Visible = false;
            }

            GUILayout.EndHorizontal();
            if (_showKeys)
            {
                GUILayout.Label(KeyList, _skin.Keys);
            }
        }

        private float Slider(string label, float value, float min, float max, string shown)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _skin.Label, GUILayout.Width(LabelWidth));
            var result = GUILayout.HorizontalSlider(value, min, max, _skin.Slider, _skin.Thumb);
            GUILayout.Label(shown, _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();
            return result;
        }

        private static bool Changed(float now, float before) => Mathf.Abs(now - before) > 0.0005f;

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        private static string Clock(float hour)
        {
            var hours = Mathf.FloorToInt(hour) % 24;
            var minutes = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
            return $"{hours:00}:{minutes:00}";
        }
    }
}
