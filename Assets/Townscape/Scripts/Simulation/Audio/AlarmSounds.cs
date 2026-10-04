using System;

namespace Townscape.Simulation.Audio
{
    /// <summary>
    /// The burglar alarm's sounds, synthesised: the bell box's piezo sounder (a piercing,
    /// nearly square tone sweeping up and down several times a second) and the panel's short beep.
    /// </summary>
    public static class AlarmSounds
    {
        /// <summary>The sounder sweeps between these, in hertz.</summary>
        public const float SweepLow = 2400f;

        public const float SweepHigh = 3600f;

        /// <summary>Sweeps up and back down per second.</summary>
        public const int SweepsPerSecond = 5;

        /// <summary>
        /// The bell box's sounder, as a seamless loop: its pitch rises and falls in a smooth
        /// triangle, and the loop holds a whole number of sweeps and of cycles, so it joins without
        /// a click.
        /// </summary>
        public static float[] Sounder(float seconds = 2f, int sampleRate = ProceduralSounds.SampleRate)
        {
            var sweeps = Math.Max(1, (int)MathF.Round(seconds * SweepsPerSecond));
            var count = (int)MathF.Round(sweeps * (float)sampleRate / SweepsPerSecond);
            var frequencies = new float[count];
            var cycles = 0.0;
            for (var i = 0; i < count; i++)
            {
                // A triangle sweep, rounded a little at the top and bottom as a real sounder's is.
                var t = (float)i / count * sweeps;
                var triangle = 1f - (2f * MathF.Abs((t - MathF.Floor(t)) - 0.5f));
                var eased = triangle * triangle * (3f - (2f * triangle));
                var shape = (0.6f * triangle) + (0.4f * eased);
                frequencies[i] = SweepLow + ((SweepHigh - SweepLow) * shape);
                cycles += frequencies[i] / sampleRate;
            }

            // Stretch the pitch a hair so the loop ends on a whole cycle.
            var stretch = Math.Round(cycles) / cycles;
            var samples = new float[count];
            var phase = 0.0;
            for (var i = 0; i < count; i++)
            {
                var angle = (float)(phase * 2.0 * Math.PI);

                // A piezo disc driven by a square wave: strong odd harmonics, kept below the
                // Nyquist limit so nothing folds back into a whine.
                samples[i] = 0.62f * (MathF.Sin(angle) + (0.3f * MathF.Sin(3f * angle)));
                phase += frequencies[i] * stretch / sampleRate;
                phase -= Math.Floor(phase);
            }

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
    }
}
