using System;
using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// A small seeded random number generator (SplitMix64). The game uses its own rather than
    /// <see cref="Random"/> because a seeded <see cref="Random"/> is not guaranteed to give the
    /// same numbers in Unity's runtime and in .NET, and the same seed must always give the same day.
    /// Only integer arithmetic and exact conversions are used, so results match on every platform.
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = seed;
        }

        /// <summary>The seed for one day of a game, mixed from the game's seed and the day number.</summary>
        public static ulong DaySeed(int gameSeed, int day) => Mix(((ulong)(uint)gameSeed << 32) | (uint)day);

        public ulong NextULong()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                return Mix(_state);
            }
        }

        /// <summary>A number in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>A whole number from 0 up to, but not including, <paramref name="maxExclusive"/>.</summary>
        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextULong() % (ulong)maxExclusive);

        public bool Chance(double probability) => NextDouble() < probability;

        /// <summary>
        /// A random count averaging <paramref name="mean"/>, for how many customers turn up.
        /// Drawn as a binomial of four trials per expected customer, which spreads like real
        /// footfall and needs no floating-point functions that could differ between platforms.
        /// </summary>
        public int Count(double mean)
        {
            if (mean <= 0)
            {
                return 0;
            }

            var trials = (int)Math.Ceiling(mean * 4.0);
            var chance = mean / trials;
            var count = 0;
            for (var i = 0; i < trials; i++)
            {
                if (Chance(chance))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Picks an id with probability proportional to its weight, or null if nothing has any weight.
        /// Ids are taken in order, so the result is repeatable.
        /// </summary>
        public string Pick(IReadOnlyList<Weighted> weights)
        {
            var total = 0.0;
            foreach (var weighted in weights)
            {
                total += Math.Max(0.0, weighted.Weight);
            }

            if (total <= 0)
            {
                return null;
            }

            var target = NextDouble() * total;
            string last = null;
            foreach (var weighted in weights)
            {
                if (weighted.Weight <= 0)
                {
                    continue;
                }

                last = weighted.Id;
                target -= weighted.Weight;
                if (target < 0)
                {
                    return weighted.Id;
                }
            }

            return last;
        }

        /// <summary>
        /// Orders ids from most to least preferred by repeated weighted picks without replacement.
        /// Ids with no weight are left out.
        /// </summary>
        public IReadOnlyList<string> Rank(IReadOnlyList<Weighted> weights)
        {
            var left = new List<Weighted>(weights);
            left.RemoveAll(weighted => weighted.Weight <= 0);
            var ranked = new List<string>(left.Count);
            while (left.Count > 0)
            {
                var pick = Pick(left);
                ranked.Add(pick);
                left.RemoveAll(weighted => weighted.Id == pick);
            }

            return ranked;
        }

        /// <summary>A number spread evenly between <paramref name="low"/> and <paramref name="high"/>.</summary>
        public double Between(double low, double high) => low + (NextDouble() * (high - low));

        private static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
