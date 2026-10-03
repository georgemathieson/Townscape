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

        /// <summary>Steady rain: a soft hiss and body with drops pattering through it.</summary>
        public static float[] Rain(int seed, float seconds = 6f, int sampleRate = SampleRate)
        {
            var count = Samples(seconds, sampleRate);
            var total = count + Samples(LoopFadeSeconds, sampleRate);
            var random = new Random(seed);
            var samples = new float[total];

            var hissTop = new OnePole(6500f, sampleRate);
            var hissBottom = new OnePole(600f, sampleRate);
            var body = new OnePole(900f, sampleRate);
            for (var i = 0; i < total; i++)
            {
                var white = Noise(random);
                var hiss = hissTop.Low(white);
                hiss -= hissBottom.Low(hiss);
                samples[i] = (0.55f * hiss) + (0.3f * body.Low(white));
            }

            // Individual drops: short, bright ticks of noise.
            var drops = (int)(total / (float)sampleRate * 160f);
            for (var d = 0; d < drops; d++)
            {
                var start = random.Next(total);
                var size = (float)random.NextDouble();
                var amplitude = 0.04f + (0.3f * size * size);
                var decay = (0.0015f + (0.005f * (float)random.NextDouble())) * sampleRate;
                var length = Math.Min(total - start, (int)(decay * 6f));
                var brightness = new OnePole(1800f + (2500f * (float)random.NextDouble()), sampleRate);
                for (var k = 0; k < length; k++)
                {
                    var white = Noise(random);
                    samples[start + k] += (white - brightness.Low(white)) * amplitude * MathF.Exp(-k / decay);
                }
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

        /// <summary>The river: a low rush with water babbling over stones.</summary>
        public static float[] River(int seed, float seconds = 8f, int sampleRate = SampleRate)
        {
            var count = Samples(seconds, sampleRate);
            var total = count + Samples(LoopFadeSeconds, sampleRate);
            var random = new Random(seed);
            var samples = new float[total];

            // Bubbles: short swells of brightness at random.
            var bubbles = new float[total];
            var bubbleCount = (int)(total / (float)sampleRate * 35f);
            for (var b = 0; b < bubbleCount; b++)
            {
                var start = random.Next(total);
                var length = (int)((0.02f + (0.07f * (float)random.NextDouble())) * sampleRate);
                var amplitude = 0.3f + (0.7f * (float)random.NextDouble());
                for (var k = 0; k < length && start + k < total; k++)
                {
                    bubbles[start + k] += amplitude * 0.5f * (1f - MathF.Cos(2f * MathF.PI * k / length));
                }
            }

            var rush1 = new OnePole(220f, sampleRate);
            var rush2 = new OnePole(300f, sampleRate);
            var babbleTop = new OnePole(2800f, sampleRate);
            var babbleBottom = new OnePole(700f, sampleRate);
            for (var i = 0; i < total; i++)
            {
                var white = Noise(random);
                var rush = rush2.Low(rush1.Low(white));
                var babble = babbleTop.Low(white);
                babble -= babbleBottom.Low(babble);
                samples[i] = (rush * 2.5f) + (babble * (0.25f + bubbles[i]) * 0.6f);
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
