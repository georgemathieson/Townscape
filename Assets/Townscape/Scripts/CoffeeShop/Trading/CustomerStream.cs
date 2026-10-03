using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>Rolls a day's customers for a site from a seed.</summary>
    public static class CustomerStream
    {
        /// <summary>Everyone who comes in, in order of arrival. The same seed always gives the same customers.</summary>
        public static IReadOnlyList<Customer> Roll(CoffeeShopBalance balance, SiteDef site, ulong seed)
        {
            var random = new SeededRandom(seed);
            var customers = new List<Customer>();
            var slotStart = 0.0;
            for (var slotIndex = 0; slotIndex < site.Slots.Count; slotIndex++)
            {
                var slot = site.Slots[slotIndex];
                var count = random.Count(slot.Footfall);
                for (var i = 0; i < count; i++)
                {
                    customers.Add(RollOne(balance, slot, slotIndex, slotStart, random));
                }

                slotStart += slot.DurationSeconds;
            }

            // Stable: customers arriving at the same moment keep the order they were rolled in.
            return customers.OrderBy(c => c.ArrivalSeconds).ToList();
        }

        private static Customer RollOne(CoffeeShopBalance balance, SlotDef slot, int slotIndex, double slotStart, SeededRandom random)
        {
            // Every customer takes the same draws in the same order, so changing one segment's
            // numbers never shuffles who else turns up.
            var segment = balance.Segment(random.Pick(slot.SegmentMix));
            var arrival = slotStart + (random.NextDouble() * slot.DurationSeconds);
            var dairyFree = random.Chance(segment.DairyFreeShare);
            var milkFussiness = random.NextDouble();
            var budget = Spread(segment.BudgetPence, balance.BudgetSpread, random);
            var patience = Spread(segment.PatienceSeconds, balance.PatienceSpread, random);
            var wantsDrink = random.Chance(segment.DrinkChance);
            var drinks = random.Rank(segment.DrinkTaste);
            var wantsPastry = random.Chance(segment.PastryChance);
            var pastries = random.Rank(segment.PastryTaste);
            var takesSecondChoice = random.Chance(segment.SecondChoiceChance);

            return new Customer
            {
                SlotIndex = slotIndex,
                ArrivalSeconds = arrival,
                SegmentId = segment.Id,
                DairyFree = dairyFree,
                MilkFussiness = milkFussiness,
                BudgetPence = budget,
                PatienceSeconds = patience,
                Drinks = wantsDrink ? drinks : new string[0],
                Pastries = wantsPastry ? pastries : new string[0],
                TakesSecondChoice = takesSecondChoice,
            };
        }

        private static int Spread(int value, double spread, SeededRandom random) =>
            (int)Math.Round(value * random.Between(1.0 - spread, 1.0 + spread), MidpointRounding.AwayFromZero);
    }
}
