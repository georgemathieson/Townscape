using NUnit.Framework;
using Townscape.State;

namespace Townscape.Tests.State
{
    public sealed class WeatherReducerTests
    {
        [TestCase(-1f, 0f)]
        [TestCase(0.4f, 0.4f)]
        [TestCase(3f, 1f)]
        public void Sliders_AreClampedToUnitRange(float input, float expected)
        {
            var storm = WeatherState.DefaultStorm;

            Assert.That(WeatherReducer.Reduce(storm, new SetRainIntensity(input)).RainIntensity, Is.EqualTo(expected));
            Assert.That(WeatherReducer.Reduce(storm, new SetSnowIntensity(input)).SnowIntensity, Is.EqualTo(expected));
            Assert.That(WeatherReducer.Reduce(storm, new SetLightningFrequency(input)).LightningFrequency, Is.EqualTo(expected));
            Assert.That(WeatherReducer.Reduce(storm, new SetWindStrength(input)).WindStrength, Is.EqualTo(expected));
        }

        [Test]
        public void StrikeRequests_CountUp()
        {
            var once = WeatherReducer.Reduce(WeatherState.DefaultStorm, new RequestLightningStrike());
            var twice = WeatherReducer.Reduce(once, new RequestLightningStrike());

            Assert.That(WeatherState.DefaultStorm.StrikeRequests, Is.EqualTo(0));
            Assert.That(twice.StrikeRequests, Is.EqualTo(2));
            Assert.That(twice.RainIntensity, Is.EqualTo(WeatherState.DefaultStorm.RainIntensity));
        }

        [Test]
        public void SwitchingWeather_KeepsTheRainAndTheSnowAsTheyWereSet()
        {
            var wet = WeatherReducer.Reduce(WeatherState.DefaultStorm, new SetRainIntensity(0.3f));
            var snowy = WeatherReducer.Reduce(WeatherReducer.Reduce(wet, new SetWeatherKind(WeatherKind.Snow)), new SetSnowIntensity(0.9f));
            var back = WeatherReducer.Reduce(snowy, new SetWeatherKind(WeatherKind.Storm));

            Assert.That(snowy.Kind, Is.EqualTo(WeatherKind.Snow));
            Assert.That(back.RainIntensity, Is.EqualTo(0.3f));
            Assert.That(back.SnowIntensity, Is.EqualTo(0.9f));
            Assert.That(WeatherState.DefaultStorm.SnowIntensity, Is.EqualTo(WeatherState.DefaultSnow));
        }

        [Test]
        public void WeatherKinds_CycleRound_WithNamesForThePanel()
        {
            Assert.That(WeatherKinds.Next(WeatherKind.Storm), Is.EqualTo(WeatherKind.Snow));
            Assert.That(WeatherKinds.Next(WeatherKind.Snow), Is.EqualTo(WeatherKind.Storm));
            Assert.That(WeatherKinds.All, Is.EquivalentTo(System.Enum.GetValues(typeof(WeatherKind))));
            Assert.That(WeatherKinds.Title(WeatherKind.Snow), Is.EqualTo("Snowstorm"));
            Assert.That(WeatherKinds.Title(WeatherKind.Storm), Is.EqualTo("Thunderstorm"));
        }

        [TestCase(0.25f, 0.5f)]
        [TestCase(0.6f, 0.75f)]
        [TestCase(1f, 0.25f)]
        public void Steps_CycleUpwards_AndWrap(float current, float expected)
        {
            Assert.That(WeatherSteps.Next(current, WeatherSteps.Rain), Is.EqualTo(expected));
        }

        [Test]
        public void TownReducer_RoutesToTheRightSlice()
        {
            var state = TownState.CreateDefault();

            var next = TownReducer.Reduce(state, new SetWindStrength(0.1f));

            Assert.That(next.Weather.WindStrength, Is.EqualTo(0.1f));
            Assert.That(next.TimeOfDay, Is.SameAs(state.TimeOfDay));
        }
    }
}
