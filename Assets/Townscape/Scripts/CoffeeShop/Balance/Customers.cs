using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    /// <summary>An id with a relative weight: how much a segment likes an item, or how much of a crowd it makes up.</summary>
    public sealed record Weighted(string Id, double Weight);

    /// <summary>A kind of customer: what they like, what they can have, what they'll pay and how long they'll queue.</summary>
    public sealed record SegmentDef
    {
        public string Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public double DrinkChance { get; init; }

        public double PastryChance { get; init; }

        /// <summary>How much they like each drink, relative to the others.</summary>
        public IReadOnlyList<Weighted> DrinkTaste { get; init; } = new Weighted[0];

        /// <summary>How much they like each pastry, relative to the others.</summary>
        public IReadOnlyList<Weighted> PastryTaste { get; init; } = new Weighted[0];

        /// <summary>Share who can't have dairy.</summary>
        public double DairyFreeShare { get; init; }

        /// <summary>The most they'll spend on one visit, on average (each customer varies a little).</summary>
        public int BudgetPence { get; init; }

        /// <summary>The longest they'll wait in the queue before giving up.</summary>
        public int PatienceSeconds { get; init; }

        /// <summary>Chance they'll take something else when their favourite isn't available.</summary>
        public double SecondChoiceChance { get; init; }
    }

    /// <summary>A part of the trading day with its own crowd: the morning rush, lunchtime, the afternoon.</summary>
    public sealed record SlotDef
    {
        public string Name { get; init; }

        public int DurationSeconds { get; init; }

        /// <summary>Customers expected, on average.</summary>
        public double Footfall { get; init; }

        /// <summary>Which segments make up the crowd, by weight.</summary>
        public IReadOnlyList<Weighted> SegmentMix { get; init; } = new Weighted[0];

        /// <summary>People serving at the counter.</summary>
        public int Baristas { get; init; } = 1;
    }

    /// <summary>Where the shop trades. Different sites draw different crowds.</summary>
    public sealed record SiteDef
    {
        public string Id { get; init; }

        public string Name { get; init; }

        /// <summary>Rent and wages for one day's trading.</summary>
        public int DailyCostPence { get; init; }

        public IReadOnlyList<SlotDef> Slots { get; init; } = new SlotDef[0];
    }
}
