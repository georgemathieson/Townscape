using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Townscape.State;

namespace Townscape.Tests.State
{
    public sealed class AudioReducerTests
    {
        [TestCase(-0.5f, 0f)]
        [TestCase(0.3f, 0.3f)]
        [TestCase(2f, 1f)]
        public void Volume_IsClamped(float input, float expected)
        {
            Assert.That(AudioReducer.Reduce(AudioState.Default, new SetVolume(input)).Volume, Is.EqualTo(expected));
        }

        [Test]
        public void Muting_KeepsTheVolume_ButPlaysNothing()
        {
            var muted = AudioReducer.Reduce(AudioState.Default, new SetMuted(true));

            Assert.That(muted.Volume, Is.EqualTo(AudioState.Default.Volume));
            Assert.That(muted.EffectiveVolume, Is.EqualTo(0f));
            Assert.That(AudioReducer.Reduce(muted, new SetMuted(false)).EffectiveVolume, Is.EqualTo(AudioState.Default.Volume));
        }

        [Test]
        public void TownReducer_RoutesSoundActions_ToTheAudioSlice()
        {
            var state = TownState.CreateDefault();
            var next = TownReducer.Reduce(state, new SetVolume(0.2f));

            Assert.That(next.Audio.Volume, Is.EqualTo(0.2f));
            Assert.That(next.Weather, Is.SameAs(state.Weather));
        }
    }

    public sealed class TimeSliderTests
    {
        [Test]
        public void DraggingTheSlider_BlendsAlmostAtOnce()
        {
            var state = TimeOfDayReducer.Reduce(TimeOfDayState.FromPreset(TimePreset.Day), new SetTargetHour(15f, Scrub: true));

            Assert.That(state.TargetHour, Is.EqualTo(15f));
            Assert.That(state.BlendSeconds, Is.EqualTo(TimeOfDayState.ScrubBlendSeconds));
        }

        [Test]
        public void PresetsAndHourKeys_BlendGently()
        {
            var scrubbed = TimeOfDayReducer.Reduce(TimeOfDayState.FromPreset(TimePreset.Day), new SetTargetHour(15f, Scrub: true));

            Assert.That(TimeOfDayReducer.Reduce(scrubbed, new SelectTimePreset(TimePreset.Night)).BlendSeconds, Is.EqualTo(TimeOfDayState.DefaultBlendSeconds));
            Assert.That(TimeOfDayReducer.Reduce(scrubbed, new SetTargetHour(16f)).BlendSeconds, Is.EqualTo(TimeOfDayState.DefaultBlendSeconds));
        }
    }

    public sealed class ResetSettingsTests
    {
        [Test]
        public void Reset_RestoresEverySetting()
        {
            var changed = TownState.CreateDefault();
            foreach (var action in new IAction[] { new SetRainIntensity(0.1f), new SetVolume(0.1f), new SetMuted(true), new SelectTimePreset(TimePreset.Night) })
            {
                changed = TownReducer.Reduce(changed, action);
            }

            var reset = TownReducer.Reduce(changed, new ResetSettings());

            Assert.That(reset.TimeOfDay, Is.EqualTo(TownState.CreateDefault().TimeOfDay));
            Assert.That(reset.Weather.RainIntensity, Is.EqualTo(WeatherState.DefaultStorm.RainIntensity));
            Assert.That(reset.Audio, Is.EqualTo(AudioState.Default));
        }

        [Test]
        public void Reset_NeverWindsBackTheStrikeCount()
        {
            var state = TownReducer.Reduce(TownState.CreateDefault(), new RequestLightningStrike());
            var reset = TownReducer.Reduce(state, new ResetSettings());

            Assert.That(reset.Weather.StrikeRequests, Is.EqualTo(1), "the storm would see a change and strike");
        }
    }

    public sealed class SavedSettingsTests
    {
        private static readonly TownState Defaults = TownState.CreateDefault();

        [Test]
        public void Settings_SurviveARoundTrip()
        {
            var state = Defaults;
            foreach (var action in new IAction[]
            {
                new SetTargetHour(14.5f), new SetCycleSpeed(5f), new SetRainIntensity(0.3f),
                new SetLightningFrequency(0.9f), new SetWindStrength(0.6f), new SetVolume(0.4f), new SetMuted(true),
                new SetWeatherKind(WeatherKind.Snow), new SetSnowIntensity(0.35f),
            })
            {
                state = TownReducer.Reduce(state, action);
            }

            var read = SavedSettings.Read(SavedSettings.Write(state), Defaults);

            Assert.That(read.TimeOfDay, Is.EqualTo(state.TimeOfDay));
            Assert.That(read.Weather, Is.EqualTo(state.Weather));
            Assert.That(read.Audio, Is.EqualTo(state.Audio));
        }

        [Test]
        public void Presets_AreRememberedByName()
        {
            var night = TownReducer.Reduce(Defaults, new SelectTimePreset(TimePreset.Night));
            var read = SavedSettings.Read(SavedSettings.Write(night), Defaults);

            Assert.That(read.TimeOfDay.Preset, Is.EqualTo(TimePreset.Night));
            Assert.That(read.TimeOfDay.TargetHour, Is.EqualTo(TimePresets.HourOf(TimePreset.Night)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("complete nonsense")]
        [TestCase("rain=lots;volume=;preset=Teatime;weather=Hail")]
        public void DamagedSaves_FallBackToDefaults(string text)
        {
            var read = SavedSettings.Read(text, Defaults);

            Assert.That(read.TimeOfDay, Is.EqualTo(Defaults.TimeOfDay));
            Assert.That(read.Weather, Is.EqualTo(Defaults.Weather));
            Assert.That(read.Audio, Is.EqualTo(Defaults.Audio));
        }

        [Test]
        public void SavesFromBeforeTheSnow_StillLoad()
        {
            var read = SavedSettings.Read("version=1;weather=Storm;rain=0.3;lightning=0.2;wind=0.6", Defaults);

            Assert.That(read.Weather.Kind, Is.EqualTo(WeatherKind.Storm));
            Assert.That(read.Weather.RainIntensity, Is.EqualTo(0.3f));
            Assert.That(read.Weather.SnowIntensity, Is.EqualTo(Defaults.Weather.SnowIntensity));
        }

        [Test]
        public void OutOfRangeValues_AreClamped()
        {
            var read = SavedSettings.Read("rain=7;snow=4;wind=-2;volume=NaN;hour=26;speed=9999", Defaults);

            Assert.That(read.Weather.RainIntensity, Is.EqualTo(1f));
            Assert.That(read.Weather.SnowIntensity, Is.EqualTo(1f));
            Assert.That(read.Weather.WindStrength, Is.EqualTo(0f));
            Assert.That(read.Audio.Volume, Is.EqualTo(Defaults.Audio.Volume));
            Assert.That(read.TimeOfDay.TargetHour, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(read.TimeOfDay.CycleHoursPerMinute, Is.EqualTo(TimeOfDayState.MaxCycleHoursPerMinute));
        }

        [Test]
        public void Saves_DoNotDependOnTheComputersLanguage()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var text = SavedSettings.Write(TownReducer.Reduce(Defaults, new SetRainIntensity(0.25f)));

                Assert.That(text, Does.Contain("rain=0.25"));
                Assert.That(SavedSettings.Read(text, Defaults).Weather.RainIntensity, Is.EqualTo(0.25f));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }
    }
}
