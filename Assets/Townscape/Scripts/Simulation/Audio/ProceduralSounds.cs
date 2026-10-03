using System;

namespace Townscape.Simulation.Audio
{
    /// <summary>
    /// The storm's sounds, synthesised from filtered noise so the village is never silent, even
    /// before real recordings are added: rain on the street, wind, the river, and thunder.
    /// Loops are seamless (their ends are crossfaded) and every sound is the same for the same seed.
    /// </summary>
    public static class ProceduralSounds
    {
        public const int SampleRate = 32000;

        private const float LoopFadeSeconds = 0.6f;

        /// <summary>
        /// Rain on an umbrella: a soft, warm wash with a dense patter of drops on taut fabric. Each
        /// drop is a gentle tap with a short low "tok", eased in so nothing crackles or fizzes.
        /// </summary>
        public static float[] Rain(int seed, float seconds = 6f, int sampleRate = SampleRate)
        {
            var count = Samples(seconds, sampleRate);
            var total = count + Samples(LoopFadeSeconds, sampleRate);
            var random = new Random(seed);
            var samples = new float[total];

            // The wash: pink noise with its hiss taken off, swelling gently with the gusts.
            var pink = new Pink();
            var wash = new LowPass2(3600f, sampleRate);
            var rumble = new OnePole(500f, sampleRate);
            for (var i = 0; i < total; i++)
            {
                var loop = (float)(i % count) / count;
                var swell = 1f + (0.12f * MathF.Sin((2f * MathF.PI * 3f * loop) + 0.7f)) + (0.06f * MathF.Sin((2f * MathF.PI * 7f * loop) + 2.1f));
                var soft = wash.Low(pink.Next(Noise(random)));
                samples[i] = (soft - rumble.Low(soft)) * 0.5f * swell;
            }

            // The patter: drops drumming on the fabric, many soft and a few heavier.
            var drops = (int)(total / (float)sampleRate * 320f);
            for (var d = 0; d < drops; d++)
            {
                var rib = random.NextDouble() < 0.12;
                var frequency = rib ? Between(random, 1000f, 2000f) : MathF.Exp(Between(random, MathF.Log(220f), MathF.Log(700f)));
                var decay = (rib ? Between(random, 0.006f, 0.012f) : Between(random, 0.012f, 0.035f)) * sampleRate;
                var weight = (float)random.NextDouble();
                var amplitude = 0.03f + (0.2f * weight * weight * weight);
                Tone(samples, random.Next(total), frequency, 0f, decay, amplitude * 0.55f, sampleRate, random);

                // The tap of the drop itself: a very short puff, crisp but with no fizz.
                var tapTop = new LowPass2(4000f, sampleRate);
                var tapBottom = new OnePole(700f, sampleRate);
                var start = random.Next(total);
                var tapDecay = 0.003f * sampleRate;
                for (var k = 0; k < tapDecay * 5f && start + k < total; k++)
                {
                    var puff = tapTop.Low(Noise(random));
                    samples[start + k] += (puff - tapBottom.Low(puff)) * amplitude * 1.6f * Ease(k, sampleRate) * MathF.Exp(-k / tapDecay);
                }
            }

            // Nothing above a gentle top end: rain on fabric, not static.
            var top = new LowPass2(5000f, sampleRate);
            for (var i = 0; i < total; i++)
            {
                samples[i] = top.Low(samples[i]);
            }

            return Normalise(Loop(samples, count, sampleRate), 0.7f);
        }

        /// <summary>Wind: a hollow, breathy roar that swells and falls, with a faint whistle in the gusts.</summary>
        public static float[] Wind(int seed, float seconds = 8f, int sampleRate = SampleRate)
        {
            var count = Samples(seconds, sampleRate);
            var total = count + Samples(LoopFadeSeconds, sampleRate);
            var random = new Random(seed);
            var samples = new float[total];

            // Two low-pass stages, so no hiss gets through: wind is a dull roar, not static.
            var top1 = new OnePole(380f, sampleRate);
            var top2 = new OnePole(520f, sampleRate);
            var bottom = new OnePole(70f, sampleRate);
            var whistle = new Resonator(sampleRate);
            for (var i = 0; i < total; i++)
            {
                // Swells built from whole cycles of the loop, so they repeat seamlessly.
                var loop = (float)(i % count) / count;
                var swell = 0.55f + (0.25f * MathF.Sin((2f * MathF.PI * 2f * loop) + 0.3f)) + (0.15f * MathF.Sin((2f * MathF.PI * 5f * loop) + 1.1f)) + (0.05f * MathF.Sin((2f * MathF.PI * 11f * loop) + 2f));
                var white = Noise(random);
                var band = top2.Low(top1.Low(white));
                band -= bottom.Low(band);
                var tone = whistle.Next(white, 520f + (380f * swell));
                samples[i] = (band * swell * 3f) + (tone * 0.05f * swell * swell);
            }

            return Normalise(Loop(samples, count, sampleRate), 0.65f);
        }

        /// <summary>
        /// Flowing water: a smooth, steady rush and wash that only swell slowly, with a dense cloud of
        /// tiny bubbles gurgling through it. The bubbles overlap so much that the water flows rather
        /// than chops.
        /// </summary>
        public static float[] River(int seed, float seconds = 8f, int sampleRate = SampleRate)
        {
            var count = Samples(seconds, sampleRate);
            var total = count + Samples(LoopFadeSeconds, sampleRate);
            var random = new Random(seed);
            var samples = new float[total];

            var rush = new LowPass2(380f, sampleRate);
            var pink = new Pink();
            var washTop = new LowPass2(2200f, sampleRate);
            var washBottom = new OnePole(260f, sampleRate);
            for (var i = 0; i < total; i++)
            {
                // Slow swells only (whole cycles of the loop), so the flow never jerks.
                var loop = (float)(i % count) / count;
                var flow = 1f + (0.1f * MathF.Sin((2f * MathF.PI * 2f * loop) + 0.4f)) + (0.05f * MathF.Sin((2f * MathF.PI * 5f * loop) + 1.9f));
                var lap = 1f + (0.12f * MathF.Sin((2f * MathF.PI * 3f * loop) + 2.6f));
                var wash = washTop.Low(pink.Next(Noise(random)));
                wash -= washBottom.Low(wash);
                samples[i] = (rush.Low(Noise(random)) * 0.7f * flow) + (wash * 0.55f * lap);
            }

            // Bubbles: short tones that rise a little in pitch as they burst, hundreds a second.
            var bubbles = (int)(total / (float)sampleRate * 420f);
            for (var b = 0; b < bubbles; b++)
            {
                var frequency = MathF.Exp(Between(random, MathF.Log(260f), MathF.Log(1300f)));
                var decay = Between(random, 0.008f, 0.022f) * sampleRate;
                var amplitude = Between(random, 0.02f, 0.07f);
                Tone(samples, random.Next(total), frequency, Between(random, 4f, 12f), decay, amplitude, sampleRate, random);
            }

            var top = new LowPass2(3200f, sampleRate);
            for (var i = 0; i < total; i++)
            {
                samples[i] = top.Low(samples[i]);
            }

            return Normalise(Loop(samples, count, sampleRate), 0.6f);
        }

        /// <summary>
        /// One peal of thunder. Close thunder opens with a sharp crack; distant thunder only rolls,
        /// lower, slower to build and longer.
        /// </summary>
        public static float[] Thunder(int seed, bool close, int sampleRate = SampleRate)
        {
            var seconds = close ? 7f : 9f;
            var total = Samples(seconds, sampleRate);
            var random = new Random(seed);
            var samples = new float[total];

            // The rumble rolls: a few overlapping swells of decreasing strength.
            var rolls = 4 + random.Next(4);
            var centres = new float[rolls];
            var widths = new float[rolls];
            var heights = new float[rolls];
            var attack = close ? 0.02f : 0.35f;
            for (var r = 0; r < rolls; r++)
            {
                centres[r] = attack + ((float)random.NextDouble() * seconds * 0.45f);
                widths[r] = 0.4f + ((float)random.NextDouble() * 1.1f);
                heights[r] = (0.5f + (0.5f * (float)random.NextDouble())) * (1f - (0.5f * r / rolls));
            }

            var low1 = new OnePole(close ? 160f : 90f, sampleRate);
            var low2 = new OnePole(close ? 260f : 150f, sampleRate);
            var crackBottom = new OnePole(1500f, sampleRate);
            var secondCrack = 0.05f + (0.1f * (float)random.NextDouble());
            for (var i = 0; i < total; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = 0f;
                for (var r = 0; r < rolls; r++)
                {
                    var x = (t - centres[r]) / widths[r];
                    envelope += heights[r] * MathF.Exp(-x * x);
                }

                envelope *= Smooth(Math.Min(1f, t / attack)) * MathF.Exp(-t / (seconds * 0.4f));
                var white = Noise(random);
                var sample = low2.Low(low1.Low(white)) * envelope * 6f;

                if (close)
                {
                    var bright = white - crackBottom.Low(white);
                    sample += bright * 1.3f * MathF.Exp(-t / 0.035f);
                    if (t > secondCrack)
                    {
                        sample += bright * 0.6f * MathF.Exp(-(t - secondCrack) / 0.05f);
                    }
                }

                // Fade the very end so it never clicks off.
                var tail = (seconds - t) / 0.5f;
                samples[i] = sample * (tail < 1f ? Smooth(Math.Max(0f, tail)) : 1f);
            }

            return Normalise(samples, 0.9f);
        }

        private static int Samples(float seconds, int sampleRate) => (int)MathF.Round(seconds * sampleRate);

        private static float Between(Random random, float min, float max) => min + ((float)random.NextDouble() * (max - min));

        // A 1.5 ms raised-cosine fade-in, so a sound never starts with a click.
        private static float Ease(int sample, int sampleRate)
        {
            var t = sample / (0.0015f * sampleRate);
            return t >= 1f ? 1f : 0.5f - (0.5f * MathF.Cos(MathF.PI * t));
        }

        // Adds a decaying tone (a drop on fabric, a bubble) that glides up by `rise` per second of pitch.
        private static void Tone(float[] samples, int start, float frequency, float rise, float decay, float amplitude, int sampleRate, Random random)
        {
            var phase = (float)(random.NextDouble() * Math.PI * 2.0);
            var length = (int)(decay * 5f);
            for (var k = 0; k < length && start + k < samples.Length; k++)
            {
                var t = (float)k / sampleRate;
                phase += 2f * MathF.PI * frequency * (1f + (rise * t)) / sampleRate;
                samples[start + k] += MathF.Sin(phase) * amplitude * Ease(k, sampleRate) * MathF.Exp(-k / decay);
            }
        }

        private static float Noise(Random random) => ((float)random.NextDouble() * 2f) - 1f;

        private static float Smooth(float t) => t * t * (3f - (2f * t));

        // The first `count` samples, with the extra samples after them crossfaded into the start, so
        // playing it on repeat flows straight from the end back into the beginning.
        private static float[] Loop(float[] samples, int count, int sampleRate)
        {
            var fade = samples.Length - count;
            var loop = new float[count];
            Array.Copy(samples, loop, count);
            for (var i = 0; i < fade && i < count; i++)
            {
                var w = (float)i / fade;
                loop[i] = (samples[i] * MathF.Sqrt(w)) + (samples[count + i] * MathF.Sqrt(1f - w));
            }

            return loop;
        }

        private static float[] Normalise(float[] samples, float peak)
        {
            var max = 0f;
            foreach (var sample in samples)
            {
                max = MathF.Max(max, MathF.Abs(sample));
            }

            if (max > 1e-6f)
            {
                var scale = peak / max;
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] *= scale;
                }
            }

            return samples;
        }

        /// <summary>A one-pole low-pass filter; subtract its output from the input for a high-pass.</summary>
        private struct OnePole
        {
            private readonly float _a;
            private float _y;

            public OnePole(float cutoff, int sampleRate)
            {
                _a = 1f - MathF.Exp(-2f * MathF.PI * cutoff / sampleRate);
                _y = 0f;
            }

            public float Low(float x)
            {
                _y += _a * (x - _y);
                return _y;
            }
        }

        /// <summary>Two one-pole low-passes in a row: a steeper slope, for taking hiss off.</summary>
        private struct LowPass2
        {
            private OnePole _first;
            private OnePole _second;

            public LowPass2(float cutoff, int sampleRate)
            {
                _first = new OnePole(cutoff, sampleRate);
                _second = new OnePole(cutoff, sampleRate);
            }

            public float Low(float x) => _second.Low(_first.Low(x));
        }

        /// <summary>Pink noise from white (Paul Kellet's filter): softer and warmer than white noise.</summary>
        private struct Pink
        {
            private float _b0;
            private float _b1;
            private float _b2;

            public float Next(float white)
            {
                _b0 = (0.99765f * _b0) + (white * 0.099046f);
                _b1 = (0.963f * _b1) + (white * 0.2965164f);
                _b2 = (0.57f * _b2) + (white * 1.0526913f);
                return (_b0 + _b1 + _b2 + (white * 0.1848f)) * 0.25f;
            }
        }

        /// <summary>A narrow two-pole band-pass whose pitch can glide, for the wind's whistle.</summary>
        private struct Resonator
        {
            private readonly int _sampleRate;
            private float _y1;
            private float _y2;

            public Resonator(int sampleRate)
            {
                _sampleRate = sampleRate;
                _y1 = 0f;
                _y2 = 0f;
            }

            public float Next(float x, float frequency)
            {
                const float r = 0.997f;
                var omega = 2f * MathF.PI * frequency / _sampleRate;
                var y = (2f * r * MathF.Cos(omega) * _y1) - (r * r * _y2) + ((1f - r) * x);
                _y2 = _y1;
                _y1 = y;
                return y;
            }
        }
    }
}
