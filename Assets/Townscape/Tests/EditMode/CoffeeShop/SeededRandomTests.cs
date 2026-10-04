using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Townscape.CoffeeShop;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class SeededRandomTests
    {
        [Test]
        public void MatchesTheSplitMix64Reference()
        {
            // The published SplitMix64 outputs for seed 0: if these change, every saved game replays differently.
            var random = new SeededRandom(0);

            Assert.That(random.NextULong(), Is.EqualTo(0xE220A8397B1DCDAFUL));
            Assert.That(random.NextULong(), Is.EqualTo(0x6E789E6AA1B965F4UL));
        }

        [Test]
        public void SameSeed_GivesSameNumbers()
        {
            var a = new SeededRandom(42);
            var b = new SeededRandom(42);

            for (var i = 0; i < 100; i++)
            {
                Assert.That(a.NextDouble(), Is.EqualTo(b.NextDouble()));
            }
        }

        [Test]
        public void DaySeed_DiffersByDayAndGame()
        {
            var seeds = new HashSet<ulong>();
            for (var game = 0; game < 10; game++)
            {
                for (var day = 1; day <= 30; day++)
                {
                    seeds.Add(SeededRandom.DaySeed(game, day));
                }
            }

            Assert.That(seeds, Has.Count.EqualTo(300));
            Assert.That(SeededRandom.DaySeed(7, 3), Is.EqualTo(SeededRandom.DaySeed(7, 3)));
        }

        [Test]
        public void NextDouble_StaysInRange()
        {
            var random = new SeededRandom(1);
            for (var i = 0; i < 10000; i++)
            {
                Assert.That(random.NextDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }

        [Test]
        public void Count_AveragesTheMean()
        {
            var random = new SeededRandom(2);
            var total = 0;
            const int days = 4000;
            for (var i = 0; i < days; i++)
            {
                total += random.Count(20);
            }

            Assert.That(total / (double)days, Is.EqualTo(20).Within(0.3));
            Assert.That(random.Count(0), Is.Zero);
        }

        [Test]
        public void Pick_FollowsTheWeights()
        {
            var random = new SeededRandom(3);
            var weights = new[] { new Weighted("a", 3), new Weighted("b", 1), new Weighted("never", 0) };
            var picks = Enumerable.Range(0, 8000).Select(_ => random.Pick(weights)).ToList();

            Assert.That(picks.Count(p => p == "a") / 8000.0, Is.EqualTo(0.75).Within(0.02));
            Assert.That(picks, Has.None.EqualTo("never"));
            Assert.That(random.Pick(new[] { new Weighted("x", 0) }), Is.Null);
        }

        [Test]
        public void Rank_ListsEveryLikedIdOnce_FavouritesFirstMoreOften()
        {
            var random = new SeededRandom(4);
            var weights = new[] { new Weighted("latte", 3), new Weighted("tea", 1), new Weighted("never", 0) };
            var latteFirst = 0;
            for (var i = 0; i < 4000; i++)
            {
                var ranked = random.Rank(weights);
                Assert.That(ranked, Is.EquivalentTo(new[] { "latte", "tea" }));
                latteFirst += ranked[0] == "latte" ? 1 : 0;
            }

            Assert.That(latteFirst / 4000.0, Is.EqualTo(0.75).Within(0.03));
        }
    }
}
