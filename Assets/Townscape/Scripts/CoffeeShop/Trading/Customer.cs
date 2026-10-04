using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// One customer, decided before the day starts: when they arrive, who they are and what they'd
    /// like. Nothing here depends on the shop, so the same customers can be replayed against a
    /// different menu or without an upgrade to see what difference it made.
    /// </summary>
    public sealed record Customer
    {
        public int SlotIndex { get; init; }

        /// <summary>Seconds after opening.</summary>
        public double ArrivalSeconds { get; init; }

        public string SegmentId { get; init; }

        /// <summary>Can't have dairy.</summary>
        public bool DairyFree { get; init; }

        /// <summary>Compared with a milk's appeal: below it, they're happy with that milk.</summary>
        public double MilkFussiness { get; init; }

        public int BudgetPence { get; init; }

        public int PatienceSeconds { get; init; }

        /// <summary>Drinks from most to least wanted. Empty if they don't want a drink.</summary>
        public IReadOnlyList<string> Drinks { get; init; } = new string[0];

        /// <summary>Pastries from most to least wanted. Empty if they don't want one.</summary>
        public IReadOnlyList<string> Pastries { get; init; } = new string[0];

        /// <summary>Will take their next choice when their first isn't available.</summary>
        public bool TakesSecondChoice { get; init; }
    }
}
