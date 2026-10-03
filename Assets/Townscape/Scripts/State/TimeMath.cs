using System;

namespace Townscape.State
{
    /// <summary>Engine-free helpers for working with a 24 hour clock.</summary>
    public static class TimeMath
    {
        public const float HoursPerDay = 24f;

        /// <summary>Wraps any hour value into [0, 24).</summary>
        public static float WrapHour(float hour)
        {
            if (float.IsNaN(hour) || float.IsInfinity(hour))
            {
                return 0f;
            }

            var wrapped = hour % HoursPerDay;
            if (wrapped < 0f)
            {
                wrapped += HoursPerDay;
            }

            // -0.000001 % 24 + 24 can round to exactly 24.
            return wrapped >= HoursPerDay ? 0f : wrapped;
        }

        /// <summary>Signed number of hours along the shortest way round the clock from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static float ShortestDelta(float from, float to)
        {
            var delta = WrapHour(to) - WrapHour(from);
            if (delta > HoursPerDay / 2f)
            {
                delta -= HoursPerDay;
            }
            else if (delta < -HoursPerDay / 2f)
            {
                delta += HoursPerDay;
            }

            return delta;
        }

        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));

        public static float Clamp01(float value) => float.IsNaN(value) ? 0f : Clamp(value, 0f, 1f);
    }
}
