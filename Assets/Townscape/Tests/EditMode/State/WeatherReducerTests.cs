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
        public void TownReducer_RoutesToTheRightSlice()
        {
            var state = TownState.CreateDefault();

            var next = TownReducer.Reduce(state, new SetWindStrength(0.1f));

            Assert.That(next.Weather.WindStrength, Is.EqualTo(0.1f));
            Assert.That(next.TimeOfDay, Is.SameAs(state.TimeOfDay));
        }
    }
}
