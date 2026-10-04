using System;

namespace Townscape.Simulation
{
    /// <summary>
    /// When things are lit, as pure functions of the hour, how dark it is (0 day to 1 night) and
    /// real time for flicker. Engine-free, so Unity, the tests and the offline preview agree.
    /// </summary>
    public static class LightSchedule
    {
        /// <summary>Home windows are split into groups that switch on and off at different times.</summary>
        public const int HomeWindowGroups = 8;

        /// <summary>The group whose windows show the cold, flickering glow of a television.</summary>
        public const int TelevisionGroup = 7;

        private const float FadeHours = 0.12f;

        /// <summary>
        /// How lit home windows in <paramref name="group"/> are, from 0 to 1. As dusk deepens the
        /// groups come on one after another, and they go dark again at staggered bedtimes. On a
        /// gloomy stormy day a few are on anyway.
        /// </summary>
        public static float HomeWindow(int group, float hour, float darkness)
        {
            var g = Clamp01(group / (float)(HomeWindowGroups - 1));
            var needed = 0.16f + (0.3f * Hash01((group * 97) + 11));
            var dark = SmoothStep(needed - 0.04f, needed + 0.04f, darkness);
            var bedtime = 22f + (3.2f * Hash01((group * 31) + 7));
            var evening = Window(hour, 15f + g, bedtime) * dark;
            var morning = Window(hour, 6f + (0.4f * g), 8.3f + (0.2f * g)) * dark;
            var gloom = group == 0 && darkness > 0.05f ? Window(hour, 8f, 16f) * 0.8f : 0f;
            return Math.Max(Math.Max(evening, morning), gloom);
        }

        /// <summary>A street lamp's photocell: each lamp comes on at a slightly different darkness.</summary>
        public static float StreetLamp(int seed, float darkness)
        {
            var threshold = 0.28f + (0.15f * Hash01(seed));
            return SmoothStep(threshold, threshold + 0.05f, darkness);
        }

        /// <summary>Shops are lit while open and keep a dim display light on overnight.</summary>
        public static float Shop(float hour) => 0.3f + (0.7f * Window(hour, 7.5f, 21f));

        /// <summary>The inn is lit from late morning until closing time, and glows a little after.</summary>
        public static float Inn(float hour) => 0.15f + (0.85f * Window(hour, 11f, 23.5f));

        /// <summary>Signs lit from inside: dim by day, full once it gets dark.</summary>
        public static float Sign(float darkness) => 0.15f + (0.85f * SmoothStep(0.2f, 0.3f, darkness));

        /// <summary>
        /// String lights in a window: on all the time, brighter once it's dark, and each colour
        /// slowly breathing at its own pace so the string twinkles gently rather than blinking.
        /// </summary>
        public static float StringLights(int colour, float darkness, float time)
        {
            var pace = 0.55f + (0.35f * Hash01((colour * 53) + 5));
            var phase = 6.2831853f * Hash01((colour * 29) + 3);
            var breath = 0.5f + (0.5f * (float)Math.Sin((time * pace) + phase));
            return (0.3f + (0.7f * SmoothStep(0.15f, 0.35f, darkness))) * (0.6f + (0.4f * breath));
        }

        /// <summary>A petrol station canopy: bright by night, a dim glow on a gloomy day.</summary>
        public static float Canopy(float darkness) => 0.12f + (0.88f * SmoothStep(0.15f, 0.3f, darkness));

        /// <summary>Belisha beacons flash about 75 times a minute, day and night.</summary>
        public static float BeaconPulse(float time)
        {
            var phase = Fraction(time / 0.8f);
            return SmoothStep(0f, 0.06f, phase) * (1f - SmoothStep(0.46f, 0.54f, phase));
        }

        /// <summary>The restless brightness of a television, between about 0.3 and 1.</summary>
        public static float Television(float time, int seed)
        {
            var scene = Hash01((int)MathF.Floor(time * 0.45f) + seed);
            var wobble = (0.12f * MathF.Sin((time * 7.3f) + seed)) + (0.08f * MathF.Sin((time * 13.1f) + (seed * 2)));
            return Clamp01(0.45f + (0.45f * scene) + wobble);
        }

        /// <summary>A lamp stutters for a moment as it comes on, then holds steady.</summary>
        public static float SwitchOnFlicker(float secondsSinceOn, int seed)
        {
            if (secondsSinceOn >= 0.9f)
            {
                return 1f;
            }

            var step = (int)(secondsSinceOn * 14f);
            return Hash01((seed * 13) + step) > 0.45f ? 1f : 0.15f;
        }

        /// <summary>Deterministic hash of an integer to [0, 1).</summary>
        public static float Hash01(int value)
        {
            unchecked
            {
                var h = (uint)value * 0x9E3779B1u;
                h ^= h >> 15;
                h *= 0x85EBCA77u;
                h ^= h >> 13;
                h *= 0xC2B2AE3Du;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>1 between <paramref name="start"/> and <paramref name="end"/> on the 24 hour clock (wrapping past midnight), with soft edges.</summary>
        public static float Window(float hour, float start, float end)
        {
            var span = Wrap(end - start);
            var into = Wrap(hour - start);
            if (into > span)
            {
                return 0f;
            }

            return Math.Min(SmoothStep(0f, FadeHours, into), SmoothStep(0f, FadeHours, span - into));
        }

        private static float Wrap(float hour)
        {
            var wrapped = hour % 24f;
            return wrapped < 0f ? wrapped + 24f : wrapped;
        }

        private static float Fraction(float value) => value - MathF.Floor(value);

        private static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - (2f * t));
        }
    }
}
