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

        // Share of the sound's energy above a frequency, measured through three one-pole high-passes.
        private static float ShareAbove(float[] samples, float frequency, int sampleRate)
        {
            var a = (float)Math.Exp(-2.0 * Math.PI * frequency / sampleRate);
            var high = (float[])samples.Clone();
            for (var stage = 0; stage < 3; stage++)
            {
                var low = 0f;
                for (var i = 0; i < high.Length; i++)
                {
                    low = (a * low) + ((1f - a) * high[i]);
                    high[i] -= low;
                }
            }

            return (float)(high.Sum(x => (double)x * x) / samples.Sum(x => (double)x * x));
        }

        // How much the loudness jumps about from one 20 ms slice to the next, relative to its average.
        private static float Choppiness(float[] samples, int sampleRate)
        {
            var window = sampleRate / 50;
            var levels = Enumerable.Range(0, samples.Length / window).Select(w => Rms(samples, w * window, (w + 1) * window)).ToList();
            var mean = levels.Average();
            return (float)Math.Sqrt(levels.Average(l => (l - mean) * (l - mean))) / mean;
        }

        [Test]
        public void Rain_IsASoftPatter_NotAHiss()
        {
            var rain = ProceduralSounds.Rain(11, 2f);

            Assert.That(ShareAbove(rain, 4000f, ProceduralSounds.SampleRate), Is.LessThan(0.05f), "rain on an umbrella, not static");
        }

        [Test]
        public void River_Flows_WithoutChopping()
        {
            var river = ProceduralSounds.River(13, 2f);

            Assert.That(Choppiness(river, ProceduralSounds.SampleRate), Is.LessThan(0.16f));
            Assert.That(ShareAbove(river, 4000f, ProceduralSounds.SampleRate), Is.LessThan(0.05f));
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

        [Test]
        public void TheSiren_GoesHighThenLow_AndLoopsWithoutAClick()
        {
            var siren = PoliceSounds.Siren(pairs: 2);
            var rate = ProceduralSounds.SampleRate;

            Assert.That(siren.Length / (float)rate, Is.EqualTo(4f * PoliceSounds.NoteSeconds).Within(0.02f));
            Assert.That(siren.Max(System.Math.Abs), Is.InRange(0.3f, 1f));
            // From the end back to the start is no bigger a step than any within it: no click.
            var steepest = Enumerable.Range(1, siren.Length - 1).Max(i => System.Math.Abs(siren[i] - siren[i - 1]));
            Assert.That(System.Math.Abs(siren[0] - siren[siren.Length - 1]), Is.LessThanOrEqualTo(steepest * 1.01f), "the loop comes back to the start");

            // Count the upward zero crossings of the first and second notes: their pitch.
            float Pitch(int from, int count)
            {
                var crossings = 0;
                for (var i = from + 1; i < from + count; i++)
                {
                    if (siren[i - 1] < 0f && siren[i] >= 0f)
                    {
                        crossings++;
                    }
                }

                return crossings * rate / (float)count;
            }

            var note = (int)(PoliceSounds.NoteSeconds * rate * 0.9f);
            Assert.That(Pitch(0, note), Is.EqualTo(PoliceSounds.HighTone).Within(25f));
            Assert.That(Pitch((int)(PoliceSounds.NoteSeconds * rate) + 50, note), Is.EqualTo(PoliceSounds.LowTone).Within(25f));
        }

        [Test]
        public void AForcedDoor_ThumpsAndCracks_ThenStops()
        {
            var forced = PoliceSounds.DoorForced();
            var rate = ProceduralSounds.SampleRate;

            Assert.That(forced.Max(System.Math.Abs), Is.InRange(0.2f, 1f));
            var early = forced.Take(rate / 10).Max(System.Math.Abs);
            var late = forced.Skip(forced.Length - (rate / 20)).Max(System.Math.Abs);
            Assert.That(late, Is.LessThan(early * 0.1f), "it dies away");
        }
    }
}
