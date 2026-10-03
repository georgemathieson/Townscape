using System;
using System.Linq;
using NUnit.Framework;
using Townscape.Simulation.Audio;
using Townscape.Simulation.Weather;

namespace Townscape.Tests.Simulation
{
    public sealed class ProceduralSoundsTests
    {
        private const int Rate = 8000;

        private static float Rms(float[] samples, int from, int to)
        {
            var sum = 0.0;
            for (var i = from; i < to; i++)
            {
                sum += samples[i] * samples[i];
            }

            return (float)Math.Sqrt(sum / Math.Max(1, to - from));
        }

        // Average size of a step from one sample to the next, relative to loudness: higher means brighter.
        private static float Brightness(float[] samples, int from, int to)
        {
            var steps = 0.0;
            for (var i = from + 1; i < to; i++)
            {
                steps += Math.Abs(samples[i] - samples[i - 1]);
            }

            return (float)(steps / (to - from)) / Math.Max(1e-6f, Rms(samples, from, to));
        }

        private static readonly Func<int, float[]>[] Loops =
        {
            seed => ProceduralSounds.Rain(seed, 2f, Rate),
            seed => ProceduralSounds.Wind(seed, 2f, Rate),
            seed => ProceduralSounds.River(seed, 2f, Rate),
        };

        [Test]
        public void Loops_HaveTheRightLength_AndAreAudible_WithoutClipping()
        {
            foreach (var loop in Loops)
            {
                var samples = loop(1);
                Assert.That(samples.Length, Is.EqualTo(2 * Rate));
                Assert.That(samples.Max(Math.Abs), Is.LessThanOrEqualTo(1f));
                Assert.That(Rms(samples, 0, samples.Length), Is.GreaterThan(0.05f));
            }
        }

        [Test]
        public void Loops_RunOnFromTheEndIntoTheStart_WithoutAClick()
        {
            foreach (var loop in Loops)
            {
                var samples = loop(2);
                var seam = Math.Abs(samples[0] - samples[samples.Length - 1]);
                var largestStep = Enumerable.Range(1, samples.Length - 1).Max(i => Math.Abs(samples[i] - samples[i - 1]));
                Assert.That(seam, Is.LessThanOrEqualTo(largestStep));
            }
        }

        [Test]
        public void Sounds_AreRepeatable_AndVaryBySeed()
        {
            Assert.That(ProceduralSounds.Rain(5, 1f, Rate), Is.EqualTo(ProceduralSounds.Rain(5, 1f, Rate)));
            Assert.That(ProceduralSounds.Rain(5, 1f, Rate), Is.Not.EqualTo(ProceduralSounds.Rain(6, 1f, Rate)));
        }

        [Test]
        public void Rain_IsBrighterThanTheWind()
        {
            var rain = ProceduralSounds.Rain(3, 2f, 16000);
            var wind = ProceduralSounds.Wind(3, 2f, 16000);

            Assert.That(Brightness(rain, 0, rain.Length), Is.GreaterThan(Brightness(wind, 0, wind.Length) * 1.5f));
        }

        [Test]
        public void Thunder_RollsAndThenDiesAway()
        {
            foreach (var close in new[] { true, false })
            {
                var thunder = ProceduralSounds.Thunder(4, close, Rate);
                var length = thunder.Length;
                Assert.That(thunder.Max(Math.Abs), Is.LessThanOrEqualTo(1f));
                Assert.That(Rms(thunder, (int)(length * 0.9f), length), Is.LessThan(Rms(thunder, 0, length / 2) * 0.1f));
                Assert.That(Math.Abs(thunder[length - 1]), Is.LessThan(0.01f), "no click at the end");
            }
        }

        [Test]
        public void CloseThunder_Cracks_DistantThunderOnlyRumbles()
        {
            var close = ProceduralSounds.Thunder(8, true, Rate);
            var far = ProceduralSounds.Thunder(8, false, Rate);
            var opening = Rate / 4;

            Assert.That(Brightness(close, 0, opening), Is.GreaterThan(Brightness(far, 0, opening) * 2f));
            Assert.That(far.Length, Is.GreaterThan(close.Length), "distant thunder rolls on longer");
        }
    }

    public sealed class AudioMixTests
    {
        [Test]
        public void NoRain_IsSilent_AndHeavierRainIsLouder()
        {
            Assert.That(AudioMix.RainVolume(0f), Is.EqualTo(0f));
            Assert.That(AudioMix.RainVolume(1f), Is.GreaterThan(AudioMix.RainVolume(0.3f)));
            Assert.That(AudioMix.RainVolume(1f), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Wind_GetsLouderAndHigher_AsItStrengthens()
        {
            Assert.That(AudioMix.WindVolume(0f), Is.EqualTo(0f));
            Assert.That(AudioMix.WindVolume(12f), Is.GreaterThan(AudioMix.WindVolume(4f)));
            Assert.That(AudioMix.WindPitch(12f), Is.GreaterThan(AudioMix.WindPitch(4f)));
        }

        [Test]
        public void CloseThunder_IsLouder()
        {
            Assert.That(AudioMix.ThunderVolume(new Thunder(1, 0f, 250f)), Is.GreaterThan(AudioMix.ThunderVolume(new Thunder(1, 0f, 2000f))));
        }

        [Test]
        public void Variants_PickAValidRecording()
        {
            Assert.That(AudioMix.Variant(3, 0), Is.EqualTo(-1));
            Assert.That(Enumerable.Range(0, 200).Select(id => AudioMix.Variant(id, 3)).Distinct().OrderBy(v => v), Is.EqualTo(new[] { 0, 1, 2 }));
        }
    }
}
