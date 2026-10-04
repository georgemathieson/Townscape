namespace Townscape.CoffeeShop
{
    /// <summary>Why a customer went without something they wanted.</summary>
    public enum MissReason
    {
        /// <summary>The queue was too long and they gave up before ordering.</summary>
        Queue,

        /// <summary>The shop doesn't sell it.</summary>
        NotOnMenu,

        /// <summary>It ran out.</summary>
        SoldOut,

        /// <summary>They can't have dairy, and it has some.</summary>
        Dietary,

        /// <summary>They didn't fancy it with the shop's milk.</summary>
        MilkChoice,

        /// <summary>It was more than they had left to spend.</summary>
        TooDear,
    }

    /// <summary>Wants that went unmet, by reason.</summary>
    public sealed record Misses(int Queue, int NotOnMenu, int SoldOut, int Dietary, int MilkChoice, int TooDear)
    {
        public static readonly Misses None = new Misses(0, 0, 0, 0, 0, 0);

        public int Total => Queue + NotOnMenu + SoldOut + Dietary + MilkChoice + TooDear;

        public int Of(MissReason reason) => reason switch
        {
            MissReason.Queue => Queue,
            MissReason.NotOnMenu => NotOnMenu,
            MissReason.SoldOut => SoldOut,
            MissReason.Dietary => Dietary,
            MissReason.MilkChoice => MilkChoice,
            _ => TooDear,
        };

        public Misses Plus(MissReason reason) => reason switch
        {
            MissReason.Queue => this with { Queue = Queue + 1 },
            MissReason.NotOnMenu => this with { NotOnMenu = NotOnMenu + 1 },
            MissReason.SoldOut => this with { SoldOut = SoldOut + 1 },
            MissReason.Dietary => this with { Dietary = Dietary + 1 },
            MissReason.MilkChoice => this with { MilkChoice = MilkChoice + 1 },
            _ => this with { TooDear = TooDear + 1 },
        };
    }

    /// <summary>How one item did.</summary>
    /// <param name="Stocked">Pastries baked, or servings the ingredients covered at opening.</param>
    /// <param name="Wanted">Customers whose first choice it was.</param>
    /// <param name="Sold">Sold, including to customers who'd have preferred something else.</param>
    /// <param name="SoldAsSecondChoice">Of those sold, how many went to someone whose first choice wasn't available.</param>
    /// <param name="Wasted">Pastries left at closing and thrown away.</param>
    /// <param name="Missed">Customers whose first choice it was and who went without, by reason.</param>
    public sealed record ItemOutcome(string ItemId, int Stocked, int Wanted, int Sold, int SoldAsSecondChoice, int Wasted, Misses Missed);

    /// <summary>How well one kind of customer was looked after.</summary>
    /// <param name="Wants">Things they came in for: a drink, a pastry, or both.</param>
    /// <param name="GotFirstChoice">Wants met with exactly what they wanted.</param>
    /// <param name="GotSecondChoice">Wants met with something else they'd settle for.</param>
    public sealed record SegmentOutcome(
        string SegmentId,
        int Customers,
        int WalkedOut,
        int Wants,
        int GotFirstChoice,
        int GotSecondChoice,
        Misses Missed,
        int RevenuePence)
    {
        /// <summary>Share of their wants that were met, 0 to 1 (1 if they wanted nothing).</summary>
        public double ServedShare => Wants == 0 ? 1.0 : (GotFirstChoice + GotSecondChoice) / (double)Wants;
    }

    /// <summary>How busy one part of the day was.</summary>
    public sealed record SlotOutcome(string Name, int Customers, int WalkedOut, int LongestWaitSeconds);

    /// <summary>How the day went with an upgrade compared with the same customers and plan without it.</summary>
    public sealed record UpgradeImpact(string UpgradeId, int ProfitPence, int Sold, int Wasted, int Missed);

    /// <summary>One day's headline numbers, kept in the history and the save.</summary>
    public sealed record DaySummary(int Day, int Customers, int Sold, int Wasted, int Missed, int RevenuePence, int CostsPence, int CashAfterPence)
    {
        public int ProfitPence => RevenuePence - CostsPence;
    }

    /// <summary>Everything that happened on one trading day.</summary>
    public sealed record DayResult
    {
        public int Day { get; init; }

        public string SiteId { get; init; }

        public string MilkId { get; init; }

        public int Customers { get; init; }

        /// <summary>Customers who bought something.</summary>
        public int Served { get; init; }

        public int WalkedOut { get; init; }

        public ValueList<ItemOutcome> Items { get; init; } = ValueList<ItemOutcome>.Empty;

        public ValueList<SegmentOutcome> Segments { get; init; } = ValueList<SegmentOutcome>.Empty;

        public ValueList<SlotOutcome> Slots { get; init; } = ValueList<SlotOutcome>.Empty;

        public ValueList<UpgradeImpact> UpgradeImpacts { get; init; } = ValueList<UpgradeImpact>.Empty;

        public int RevenuePence { get; init; }

        /// <summary>Pastries and ingredients bought before opening.</summary>
        public int StockPence { get; init; }

        /// <summary>Cups, lids and ice used.</summary>
        public int SuppliesPence { get; init; }

        /// <summary>Rent and wages for the site.</summary>
        public int SiteCostPence { get; init; }

        /// <summary>Upkeep of upgrades.</summary>
        public int RunningCostPence { get; init; }

        /// <summary>What the thrown-away pastries and poured-away milk had cost.</summary>
        public int WastePence { get; init; }

        /// <summary>Milk left at closing, which doesn't keep.</summary>
        public int MilkPouredAway { get; init; }

        public int CashBeforePence { get; init; }

        /// <summary>Ingredients that keep, left for tomorrow.</summary>
        public ValueMap<int> PantryAfter { get; init; } = ValueMap<int>.Empty;

        public int CostsPence => StockPence + SuppliesPence + SiteCostPence + RunningCostPence;

        public int ProfitPence => RevenuePence - CostsPence;

        public int CashAfterPence => CashBeforePence + ProfitPence;

        public int Sold => Sum(i => i.Sold);

        public int Wasted => Sum(i => i.Wasted);

        public int Missed => Sum(i => i.Missed.Total);

        public DaySummary Summary => new DaySummary(Day, Customers, Sold, Wasted, Missed, RevenuePence, CostsPence, CashAfterPence);

        private int Sum(System.Func<ItemOutcome, int> of)
        {
            var total = 0;
            foreach (var item in Items)
            {
                total += of(item);
            }

            return total;
        }
    }
}
