using NUnit.Framework;
using Townscape.State;

namespace Townscape.Tests.State
{
    public sealed class TimeOfDayReducerTests
    {
        private static readonly TimeOfDayState Day = TimeOfDayState.FromPreset(TimePreset.Day);

        [Test]
        public void SelectPreset_SetsHourAndStopsAutoCycle()
        {
            var cycling = Day with { AutoCycle = true };

            var next = TimeOfDayReducer.Reduce(cycling, new SelectTimePreset(TimePreset.Dusk));

            Assert.That(next.Preset, Is.EqualTo(TimePreset.Dusk));
            Assert.That(next.TargetHour, Is.EqualTo(TimePresets.HourOf(TimePreset.Dusk)));
            Assert.That(next.AutoCycle, Is.False);
        }

        [Test]
        public void SetTargetHour_WrapsAndClearsPreset()
        {
            var next = TimeOfDayReducer.Reduce(Day, new SetTargetHour(25.5f));

            Assert.That(next.TargetHour, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(next.Preset, Is.Null);
        }

        [Test]
        public void SetAutoCycle_ContinuesFromGivenHour()
        {
            var next = TimeOfDayReducer.Reduce(Day, new SetAutoCycle(true, 14.2f));

            Assert.That(next.AutoCycle, Is.True);
            Assert.That(next.TargetHour, Is.EqualTo(14.2f).Within(1e-4f));
        }

        [Test]
        public void SetCycleSpeed_IsClamped()
        {
            var tooFast = TimeOfDayReducer.Reduce(Day, new SetCycleSpeed(1000f));
            var tooSlow = TimeOfDayReducer.Reduce(Day, new SetCycleSpeed(-5f));

            Assert.That(tooFast.CycleHoursPerMinute, Is.EqualTo(TimeOfDayState.MaxCycleHoursPerMinute));
            Assert.That(tooSlow.CycleHoursPerMinute, Is.EqualTo(TimeOfDayState.MinCycleHoursPerMinute));
        }

        [Test]
        public void ReselectingSamePreset_ReturnsSameInstance()
        {
            var next = TimeOfDayReducer.Reduce(Day, new SelectTimePreset(TimePreset.Day));

            Assert.That(next, Is.SameAs(Day));
        }

        [TestCase(23f, 1f, 2f)]
        [TestCase(1f, 23f, -2f)]
        [TestCase(6f, 18f, 12f)]
        [TestCase(12.5f, 19.25f, 6.75f)]
        public void ShortestDelta_GoesTheShortWayRound(float from, float to, float expected)
        {
            Assert.That(TimeMath.ShortestDelta(from, to), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(-1f, 23f)]
        [TestCase(24f, 0f)]
        [TestCase(48.25f, 0.25f)]
        public void WrapHour_WrapsIntoDay(float hour, float expected)
        {
            Assert.That(TimeMath.WrapHour(hour), Is.EqualTo(expected).Within(1e-4f));
        }
    }
}
