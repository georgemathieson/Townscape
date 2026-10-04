using System;

namespace Townscape.Simulation.Audio
{
    /// <summary>
    /// The police car's siren and the sound of a door being forced, synthesised. The siren is the
    /// old two-tone: a high note and a low one a fourth below, each half a second or so, with the
    /// reedy edge of a horn.
    /// </summary>
    public static class PoliceSounds
    {
        public const float HighTone = 740f;

        /// <summary>A fourth below the high one.</summary>
        public const float LowTone = HighTone * 3f / 4f;

        /// <summary>How long each note lasts, near enough (each holds a whole number of cycles).</summary>
        public const float NoteSeconds = 0.55f;

        /// <summary>
        /// The two-tone siren, as a seamless loop of <paramref name="pairs"/> high-low pairs. Every
        /// note starts and ends on a whole cycle, so the notes and the loop join without a click.
        /// </summary>
        public static float[] Siren(int pairs = 2, int sampleRate = ProceduralSounds.SampleRate)
        {
            var samples = new System.Collections.Generic.List<float>();
            for (var pair = 0; pair < Math.Max(1, pairs); pair++)
            {
                foreach (var frequency in new[] { HighTone, LowTone })
                {
                    var cycles = (int)MathF.Round(NoteSeconds * frequency);
                    var count = (int)MathF.Round(cycles * (float)sampleRate / frequency);
                    for (var i = 0; i < count; i++)
                    {
                        // Whole cycles exactly, whatever the rounding of the sample count.
                        var phase = 2f * MathF.PI * cycles * i / count;
                        var horn = MathF.Sin(phase) + (0.45f * MathF.Sin(2f * phase)) + (0.25f * MathF.Sin(3f * phase)) + (0.1f * MathF.Sin(4f * phase));
                        samples.Add(horn * 0.42f);
                    }
                }
            }

            return samples.ToArray();
        }

        /// <summary>A door being forced: a dull thump of a shoulder, and the splintering crack of the frame.</summary>
        public static float[] DoorForced(int seed = 5, int sampleRate = ProceduralSounds.SampleRate)
        {
            var random = new Random(seed);
            var count = (int)(0.45f * sampleRate);
            var samples = new float[count];
            var smoothed = 0f;
            for (var i = 0; i < count; i++)
            {
                var t = (float)i / sampleRate;
                var thump = MathF.Sin(2f * MathF.PI * 75f * t) * MathF.Exp(-t * 18f);
                var noise = ((float)random.NextDouble() * 2f) - 1f;
                smoothed += (noise - smoothed) * 0.55f;

                // The crack comes a moment after the thump, and dies away fast.
                var crack = t < 0.03f ? 0f : smoothed * MathF.Exp(-(t - 0.03f) * 30f) * 1.4f;
                var edge = Math.Min(1f, i / (0.002f * sampleRate));
                samples[i] = Math.Clamp(((0.8f * thump) + crack) * 0.6f * edge, -1f, 1f);
            }

            return samples;
        }
    }
}
