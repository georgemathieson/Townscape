using System;

namespace Townscape.Simulation.Diagnostics
{
    /// <summary>Frame times over the last few seconds, for the on-screen frame rate readout.</summary>
    public sealed class FrameTimes
    {
        private readonly float[] _seconds;
        private int _next;

        public FrameTimes(int capacity = 240)
        {
            _seconds = new float[Math.Max(1, capacity)];
        }

        /// <summary>How many frames the figures cover (up to the capacity).</summary>
        public int Count { get; private set; }

        public float AverageMilliseconds
        {
            get
            {
                if (Count == 0)
                {
                    return 0f;
                }

                var sum = 0f;
                for (var i = 0; i < Count; i++)
                {
                    sum += _seconds[i];
                }

                return sum / Count * 1000f;
            }
        }

        public float FramesPerSecond => Count == 0 ? 0f : 1000f / AverageMilliseconds;

        /// <summary>The slowest frame in the window: a hitch shows up here even when the average looks fine.</summary>
        public float WorstMilliseconds
        {
            get
            {
                var worst = 0f;
                for (var i = 0; i < Count; i++)
                {
                    worst = Math.Max(worst, _seconds[i]);
                }

                return worst * 1000f;
            }
        }

        /// <summary>Records a frame that took <paramref name="seconds"/>. Nonsense values are ignored.</summary>
        public void Add(float seconds)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds))
            {
                return;
            }

            _seconds[_next] = seconds;
            _next = (_next + 1) % _seconds.Length;
            Count = Math.Min(Count + 1, _seconds.Length);
        }
    }

    /// <summary>A timing smoothed over many frames, so a readout of it is steady enough to read.</summary>
    public sealed class SmoothedTiming
    {
        private const float Blend = 0.05f;
        private bool _started;

        public float Milliseconds { get; private set; }

        public void Add(float milliseconds)
        {
            Milliseconds = _started ? Milliseconds + ((milliseconds - Milliseconds) * Blend) : milliseconds;
            _started = true;
        }
    }
}
