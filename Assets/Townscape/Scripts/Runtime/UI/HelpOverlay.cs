using Townscape.Runtime.Lighting;
using Townscape.State;
using UnityEngine;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// Minimal on-screen help and clock. A proper control panel (buttons, time slider, storm
    /// sliders) replaces this in a later milestone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HelpOverlay : MonoBehaviour
    {
        private const string Controls =
            "Hold right mouse  look around\n" +
            "W A S D  move    Q / E  down / up\n" +
            "Shift  faster    Scroll  change speed\n" +
            "1 Dawn   2 Day   3 Dusk   4 Night\n" +
            "[ / ]  an hour earlier / later\n" +
            "T  run the clock    H  hide this help\n" +
            "R  rain    L  lightning    G  wind\n" +
            "B  lightning strike now";

        private Store<TownState> _store;
        private TimeOfDayLighting _lighting;
        private GUIStyle _style;

        public bool Visible { get; set; } = true;

        public void Initialize(Store<TownState> store, TimeOfDayLighting lighting)
        {
            _store = store;
            _lighting = lighting;
        }

        private void OnGUI()
        {
            if (!Visible || _store == null || _lighting == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                padding = new RectOffset(12, 12, 10, 10),
                richText = true,
            };

            var text = $"<b>Townscape</b>   {Describe(_store.State.TimeOfDay, _lighting.CurrentHour)}\n{Describe(_store.State.Weather)}\n\n{Controls}";
            var size = _style.CalcSize(new GUIContent(text));
            GUI.Box(new Rect(16f, 16f, size.x, size.y), text, _style);
        }

        private static string Describe(WeatherState weather) =>
            $"Rain {Percent(weather.RainIntensity)}   Lightning {Percent(weather.LightningFrequency)}   Wind {Percent(weather.WindStrength)}";

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        private static string Describe(TimeOfDayState time, float currentHour)
        {
            var hours = Mathf.FloorToInt(currentHour);
            var minutes = Mathf.FloorToInt((currentHour - hours) * 60f);
            var clock = $"{hours:00}:{minutes:00}";
            if (time.AutoCycle)
            {
                return $"{clock}  (clock running)";
            }

            return time.Preset.HasValue ? $"{clock}  {time.Preset.Value}" : clock;
        }
    }
}
