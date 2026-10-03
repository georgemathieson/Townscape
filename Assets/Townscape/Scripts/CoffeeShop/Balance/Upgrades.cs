using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    public enum UpgradeKind
    {
        Capacity,
        Range,
        Ambience,
        Efficiency,
        Reach,
    }

    /// <summary>What an upgrade changes.</summary>
    public enum UpgradeStat
    {
        /// <summary>Pastries the display case holds.</summary>
        DisplayCapacity,

        /// <summary>Portions of milk the fridge holds.</summary>
        FridgeCapacity,
    }

    public sealed record UpgradeEffect(UpgradeStat Stat, int Amount);

    /// <summary>Something profit can buy. Every upgrade costs something to keep as well as to buy.</summary>
    public sealed record UpgradeDef
    {
        public string Id { get; init; }

        public string Name { get; init; }

        public UpgradeKind Kind { get; init; }

        public int CostPence { get; init; }

        /// <summary>Charged every day the shop trades with it.</summary>
        public int RunningCostPence { get; init; }

        public IReadOnlyList<UpgradeEffect> Effects { get; init; } = new UpgradeEffect[0];

        /// <summary>What it does, for the player.</summary>
        public string Summary { get; init; }

        /// <summary>What it costs you beyond the price, for the player.</summary>
        public string TradeOff { get; init; }
    }
}
