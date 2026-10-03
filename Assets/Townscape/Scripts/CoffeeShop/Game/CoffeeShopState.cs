using System.Linq;

namespace Townscape.CoffeeShop
{
    public enum ShopPhase
    {
        /// <summary>
        /// The morning before opening: yesterday's results are in the paper, and the menu, the milk
        /// and the stock are being chosen.
        /// </summary>
        Prep,

        /// <summary>The overdraft ran out: the bank has closed the shop. Only a new game goes on from here.</summary>
        ClosedDown,
    }

    /// <summary>
    /// Everything about one game of Fellside Coffee. Immutable: the reducer makes a new one for every
    /// change, and the whole of it (bar the last day's details) is what gets saved.
    /// </summary>
    public sealed record CoffeeShopState
    {
        /// <summary>Every day's customers are drawn from this and the day number, so a game always replays the same.</summary>
        public int Seed { get; init; }

        /// <summary>The next day to trade, counting from 1.</summary>
        public int Day { get; init; } = 1;

        public ShopPhase Phase { get; init; }

        public int CashPence { get; init; }

        /// <summary>Cash kept back: upgrades can't be bought with it.</summary>
        public int ReservePence { get; init; }

        public string SiteId { get; init; }

        public string MilkId { get; init; }

        /// <summary>Item ids on the menu, in the balance's order.</summary>
        public ValueList<string> Menu { get; init; } = ValueList<string>.Empty;

        /// <summary>
        /// How many of each item to stock for: pastries to bake, servings of each drink to buy
        /// ingredients for. Kept for items off the menu too, so putting one back remembers its number.
        /// </summary>
        public ValueMap<int> Planned { get; init; } = ValueMap<int>.Empty;

        /// <summary>Portions of ingredients that keep, left over from earlier days.</summary>
        public ValueMap<int> Pantry { get; init; } = ValueMap<int>.Empty;

        /// <summary>Owned upgrades, each with the first day it works on.</summary>
        public ValueMap<int> Upgrades { get; init; } = ValueMap<int>.Empty;

        /// <summary>For each owned upgrade, how much more profit the shop has made with it than it would have without.</summary>
        public ValueMap<int> UpgradeEarnings { get; init; } = ValueMap<int>.Empty;

        /// <summary>The most recent days' totals, oldest first.</summary>
        public ValueList<DaySummary> History { get; init; } = ValueList<DaySummary>.Empty;

        /// <summary>The full results of the day just traded, for the morning paper. Not saved.</summary>
        public DayResult LastResult { get; init; }

        public static CoffeeShopState NewGame(CoffeeShopBalance balance, int seed) => new CoffeeShopState
        {
            Seed = seed,
            Day = 1,
            Phase = ShopPhase.Prep,
            CashPence = balance.StartingCashPence,
            ReservePence = balance.StartingReservePence,
            SiteId = balance.StartingSiteId,
            MilkId = balance.StartingMilkId,
            Menu = ValueList<string>.From(balance.Items.Select(i => i.Id).Where(id => balance.StartingMenu.Contains(id))),
            Planned = ValueMap<int>.From(balance.StartingPlan.Select(p => new System.Collections.Generic.KeyValuePair<string, int>(p.ItemId, p.Count))),
        };

        /// <summary>Whether an upgrade is owned and working on <see cref="Day"/>.</summary>
        public bool HasActiveUpgrade(string upgradeId) => Upgrades.Get(upgradeId, int.MaxValue) <= Day;
    }
}
