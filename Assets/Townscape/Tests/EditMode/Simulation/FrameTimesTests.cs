using NUnit.Framework;
using Townscape.Simulation.Diagnostics;

namespace Townscape.Tests.Simulation
{
    public sealed class FrameTimesTests
    {
        [Test]
        public void NoFrames_ReadsZero()
        {
            var frames = new FrameTimes();

            Assert.That(frames.FramesPerSecond, Is.EqualTo(0f));
            Assert.That(frames.WorstMilliseconds, Is.EqualTo(0f));
        }

        [Test]
        public void SteadyFrames_GiveTheirRate()
        {
            var frames = new FrameTimes();
            for (var i = 0; i < 100; i++)
            {
                frames.Add(1f / 60f);
            }

            Assert.That(frames.FramesPerSecond, Is.EqualTo(60f).Within(0.01f));
            Assert.That(frames.AverageMilliseconds, Is.EqualTo(16.667f).Within(0.01f));
        }

        [Test]
        public void AHitch_ShowsAsTheWorstFrame_UntilItLeavesTheWindow()
        {
            var frames = new FrameTimes(10);
            frames.Add(0.1f);
            for (var i = 0; i < 9; i++)
            {
                frames.Add(0.01f);
            }

            Assert.That(frames.WorstMilliseconds, Is.EqualTo(100f).Within(0.01f));

            frames.Add(0.01f);
            Assert.That(frames.WorstMilliseconds, Is.EqualTo(10f).Within(0.01f));
            Assert.That(frames.Count, Is.EqualTo(10));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void NonsenseFrameTimes_AreIgnored(float seconds)
        {
            var frames = new FrameTimes();
            frames.Add(seconds);

            Assert.That(frames.Count, Is.EqualTo(0));
        }

        [Test]
        public void SmoothedTimings_StartAtTheFirstValue_AndSettleOnTheNew()
        {
            var timing = new SmoothedTiming();
            timing.Add(4f);
            Assert.That(timing.Milliseconds, Is.EqualTo(4f));

            for (var i = 0; i < 200; i++)
            {
                timing.Add(1f);
            }

            Assert.That(timing.Milliseconds, Is.EqualTo(1f).Within(0.01f));
        }
    }
}
