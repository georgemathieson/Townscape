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
