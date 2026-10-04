using System;

namespace Townscape.Simulation.Audio
{
    /// <summary>
    /// The burglar alarm's sounds, synthesised: the bell (a hammer striking a steel gong twenty
    /// times a second, so it rings as one long, rattling "rrring") and the panel's short beep.
    /// </summary>
    public static class AlarmSounds
    {
        private const int StrikesPerSecond = 20;

        // The gong's modes: frequency, loudness and how long each rings on, in seconds.
        private static readonly (float Frequency, float Amplitude, float Decay)[] Modes =
        {
            (1180f, 1f, 0.22f),
            (2830f, 0.55f, 0.14f),
            (4410f, 0.3f, 0.08f),
            (6150f, 0.14f, 0.04f),
        };

        /// <summary>
        /// The bell ringing, as a seamless loop: every strike's ring is wrapped round from the end
        /// to the start, so the loop is exactly as loud where it joins as anywhere else.
        /// </summary>
        public static float[] Bell(int seed, float seconds = 2f, int sampleRate = ProceduralSounds.SampleRate)
        {
            var period = sampleRate / StrikesPerSecond;
            var strikes = Math.Max(1, (int)MathF.Round(seconds * StrikesPerSecond));
            var count = strikes * period;
            var samples = new float[count];
            var random = new Random(seed);

            for (var s = 0; s < strikes; s++)
            {
                // The hammer doesn't hit quite evenly, or quite as hard each time.
                var start = (s * period) + random.Next(-period / 12, (period / 12) + 1);
                var force = 0.8f + (0.2f * (float)random.NextDouble());
                foreach (var (frequency, amplitude, decay) in Modes)
                {
                    // A decaying sine by rotation: cheap, and exactly in tune.
                    var step = 2f * MathF.PI * frequency * (1f + (0.002f * ((float)random.NextDouble() - 0.5f))) / sampleRate;
                    var (cos, sin) = (MathF.Cos(step), MathF.Sin(step));
                    var phase = (float)random.NextDouble() * 2f * MathF.PI;
                    var (x, y) = (MathF.Cos(phase), MathF.Sin(phase));
                    var fade = MathF.Exp(-1f / (decay * sampleRate));
                    var level = amplitude * force;
                    var length = (int)(decay * sampleRate * 5f);
                    for (var k = 0; k < length; k++)
                    {
                        samples[Wrap(start + k, count)] += y * level;
                        (x, y) = ((x * cos) - (y * sin), (x * sin) + (y * cos));
                        level *= fade;
                    }
                }

                // The click of the hammer itself.
                for (var k = 0; k < sampleRate / 1000; k++)
                {
                    samples[Wrap(start + k, count)] += (((float)random.NextDouble() * 2f) - 1f) * 0.5f * force * (1f - (k / (sampleRate / 1000f)));
                }
            }

            Normalise(samples, 0.85f);
            return samples;
        }

        /// <summary>A short, piercing beep from the panel's buzzer, eased in and out so it doesn't click.</summary>
        public static float[] Beep(float frequency = 2600f, float seconds = 0.11f, int sampleRate = ProceduralSounds.SampleRate)
        {
            var count = Math.Max(1, (int)MathF.Round(seconds * sampleRate));
            var samples = new float[count];
            var ramp = 0.005f * sampleRate;
            for (var i = 0; i < count; i++)
            {
                var t = (float)i / sampleRate;
                var edge = Math.Min(1f, Math.Min(i / ramp, (count - 1 - i) / ramp));
                var envelope = 0.5f - (0.5f * MathF.Cos(MathF.PI * edge));
                var phase = 2f * MathF.PI * frequency * t;

                // A piezo buzzer is nearly square: a little of the third harmonic gives it its edge.
                samples[i] = (MathF.Sin(phase) + (0.2f * MathF.Sin(3f * phase))) * 0.45f * envelope;
            }

            return samples;
        }

        private static int Wrap(int index, int count) => ((index % count) + count) % count;

        private static void Normalise(float[] samples, float peak)
        {
            var loudest = 0f;
            foreach (var sample in samples)
            {
                loudest = Math.Max(loudest, Math.Abs(sample));
            }

            if (loudest <= 0f)
            {
                return;
            }

            var scale = peak / loudest;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
        }
    }
}
