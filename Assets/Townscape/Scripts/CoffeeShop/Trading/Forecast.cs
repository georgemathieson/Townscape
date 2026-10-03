using System.Collections.Generic;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>Who to expect in one part of the day.</summary>
    /// <param name="Customers">Expected customers in this slot.</param>
    /// <param name="Segments">Expected customers from each segment.</param>
    public sealed record SlotForecast(string Name, int DurationSeconds, double Customers, IReadOnlyList<Weighted> Segments);

    /// <summary>
    /// What a site's crowd usually looks like, worked out from the balance rather than rolled, so the
    /// player can stock for who is actually there. Real days vary around it.
    /// </summary>
    public sealed class Forecast
    {
        private Forecast(IReadOnlyList<SlotForecast> slots, IReadOnlyList<Weighted> segments, IReadOnlyList<Weighted> firstChoices)
        {
            Slots = slots;
            Segments = segments;
            FirstChoices = firstChoices;
        }

        public IReadOnlyList<SlotForecast> Slots { get; }

        /// <summary>Expected customers from each segment over the whole day.</summary>
        public IReadOnlyList<Weighted> Segments { get; }

        /// <summary>For each item, how many customers are expected to want it most.</summary>
        public IReadOnlyList<Weighted> FirstChoices { get; }

        public double Customers => Slots.Sum(s => s.Customers);

        public double FirstChoicesOf(string itemId) => FirstChoices.FirstOrDefault(w => w.Id == itemId)?.Weight ?? 0;

        public static Forecast For(CoffeeShopBalance balance, SiteDef site)
        {
            var slots = new List<SlotForecast>();
            var bySegment = new Dictionary<string, double>();
            foreach (var slot in site.Slots)
            {
                var total = slot.SegmentMix.Sum(m => System.Math.Max(0, m.Weight));
                var segments = slot.SegmentMix
                    .Select(m => new Weighted(m.Id, total > 0 ? slot.Footfall * System.Math.Max(0, m.Weight) / total : 0))
                    .ToList();
                foreach (var segment in segments)
                {
                    bySegment.TryGetValue(segment.Id, out var sum);
                    bySegment[segment.Id] = sum + segment.Weight;
                }

                slots.Add(new SlotForecast(slot.Name, slot.DurationSeconds, slot.Footfall, segments));
            }

            var firstChoices = balance.Items.ToDictionary(i => i.Id, _ => 0.0);
            foreach (var pair in bySegment)
            {
                var segment = balance.Segment(pair.Key);
                AddFirstChoices(firstChoices, segment.DrinkTaste, pair.Value * segment.DrinkChance);
                AddFirstChoices(firstChoices, segment.PastryTaste, pair.Value * segment.PastryChance);
            }

            return new Forecast(
                slots,
                balance.Segments.Where(s => bySegment.ContainsKey(s.Id)).Select(s => new Weighted(s.Id, bySegment[s.Id])).ToList(),
                balance.Items.Select(i => new Weighted(i.Id, firstChoices[i.Id])).ToList());
        }

        // The chance an item is someone's first choice is its share of their taste.
        private static void AddFirstChoices(Dictionary<string, double> firstChoices, IReadOnlyList<Weighted> taste, double wanting)
        {
            var total = taste.Sum(t => System.Math.Max(0, t.Weight));
            if (total <= 0)
            {
                return;
            }

            foreach (var item in taste.Where(t => t.Weight > 0))
            {
                firstChoices[item.Id] += wanting * item.Weight / total;
            }
        }
    }
}
