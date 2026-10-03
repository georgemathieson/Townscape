using System;
using System.Collections.Generic;
using System.Globalization;

namespace Townscape.State
{
    /// <summary>
    /// The user's settings as a short line of text, so they can be remembered between sessions:
    /// <c>version=1;hour=19.25;preset=Dusk;...</c>. Reading is forgiving: anything missing,
    /// unreadable or out of range falls back to the default, so a damaged save never breaks the scene.
    /// </summary>
    /// <remarks>
    /// Only what the user chose is saved. A pending request for a lightning strike is not.
    /// </remarks>
    public static class SavedSettings
    {
        public const int Version = 1;

        public static string Write(TownState state)
        {
            var time = state.TimeOfDay;
            var weather = state.Weather;
            var audio = state.Audio;
            var values = new List<string>
            {
                $"version={Version}",
                $"hour={Number(time.TargetHour)}",
                $"preset={(time.Preset.HasValue ? time.Preset.Value.ToString() : "none")}",
                $"cycle={(time.AutoCycle ? 1 : 0)}",
                $"speed={Number(time.CycleHoursPerMinute)}",
                $"weather={weather.Kind}",
                $"rain={Number(weather.RainIntensity)}",
                $"lightning={Number(weather.LightningFrequency)}",
                $"wind={Number(weather.WindStrength)}",
                $"volume={Number(audio.Volume)}",
                $"muted={(audio.Muted ? 1 : 0)}",
            };

            return string.Join(";", values);
        }

        /// <summary>The settings in <paramref name="text"/>, with <paramref name="defaults"/> for anything missing.</summary>
        public static TownState Read(string text, TownState defaults)
        {
            var values = Parse(text);
            var time = defaults.TimeOfDay;
            var weather = defaults.Weather;
            var audio = defaults.Audio;

            // A saved preset wins; a saved custom hour has none; with neither, keep the default.
            TimePreset? preset = null;
            if (values.TryGetValue("preset", out var presetName) && Enum.TryParse(presetName, out TimePreset parsedPreset) && Enum.IsDefined(typeof(TimePreset), parsedPreset))
            {
                preset = parsedPreset;
            }
            else if (!values.ContainsKey("hour"))
            {
                preset = time.Preset;
            }

            var hour = preset.HasValue ? TimePresets.HourOf(preset.Value) : TimeMath.WrapHour(Float(values, "hour", time.TargetHour));
            time = time with
            {
                TargetHour = hour,
                Preset = preset,
                AutoCycle = Flag(values, "cycle", time.AutoCycle),
                CycleHoursPerMinute = TimeMath.Clamp(Float(values, "speed", time.CycleHoursPerMinute), TimeOfDayState.MinCycleHoursPerMinute, TimeOfDayState.MaxCycleHoursPerMinute),
                BlendSeconds = TimeOfDayState.DefaultBlendSeconds,
            };

            weather = weather with
            {
                Kind = values.TryGetValue("weather", out var kindName) && Enum.TryParse(kindName, out WeatherKind kind) && Enum.IsDefined(typeof(WeatherKind), kind) ? kind : weather.Kind,
                RainIntensity = TimeMath.Clamp01(Float(values, "rain", weather.RainIntensity)),
                LightningFrequency = TimeMath.Clamp01(Float(values, "lightning", weather.LightningFrequency)),
                WindStrength = TimeMath.Clamp01(Float(values, "wind", weather.WindStrength)),
            };

            audio = audio with
            {
                Volume = TimeMath.Clamp01(Float(values, "volume", audio.Volume)),
                Muted = Flag(values, "muted", audio.Muted),
            };

            return new TownState(time, weather, audio);
        }

        private static Dictionary<string, string> Parse(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text))
            {
                return values;
            }

            foreach (var pair in text.Split(';'))
            {
                var equals = pair.IndexOf('=');
                if (equals > 0)
                {
                    values[pair.Substring(0, equals).Trim()] = pair.Substring(equals + 1).Trim();
                }
            }

            return values;
        }

        private static float Float(Dictionary<string, string> values, string key, float fallback) =>
            values.TryGetValue(key, out var text)
            && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && !float.IsNaN(value)
            && !float.IsInfinity(value)
                ? value
                : fallback;

        private static bool Flag(Dictionary<string, string> values, string key, bool fallback) =>
            values.TryGetValue(key, out var text) ? text == "1" : fallback;

        private static string Number(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
