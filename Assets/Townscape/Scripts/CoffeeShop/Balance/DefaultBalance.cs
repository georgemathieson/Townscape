namespace Townscape.CoffeeShop
{
    /// <summary>
    /// The shipped numbers for Fellside Coffee. Money is in pence. See docs/COFFEE_SHOP.md for why
    /// each number is what it is, and tools/coffee-sim to see what a change does over many days.
    /// </summary>
    public static class DefaultBalance
    {
        public const string Latte = "latte";
        public const string Tea = "tea";
        public const string Croissant = "croissant";

        public const string CoffeeBeans = "coffee-beans";
        public const string TeaLeaves = "tea-leaves";
        public const string Milk = "milk";

        public const string DairyMilk = "dairy";
        public const string OatMilk = "oat";

        public const string Commuters = "commuters";
        public const string Locals = "locals";

        public const string HighStreet = "high-street";

        public const string BiggerDisplay = "bigger-display";

        public static CoffeeShopBalance Create() => new CoffeeShopBalance
        {
            StartingCashPence = 12000,
            OverdraftPence = 5000,
            StartingReservePence = 0,
            DisplayCapacity = 12,
            FridgeCapacity = 60,
            SuppliesPerDrinkPence = 14,
            PaymentSeconds = 15,
            BudgetSpread = 0.25,
            PatienceSpread = 0.3,
            HistoryDays = 14,
            StartingSiteId = HighStreet,
            StartingMilkId = DairyMilk,
            StartingMenu = new[] { Latte, Tea, Croissant },
            StartingPlan = new[]
            {
                new ItemCount(Latte, 30),
                new ItemCount(Tea, 20),
                new ItemCount(Croissant, 10),
            },
            Items = new[]
            {
                new ItemDef
                {
                    Id = Latte, Name = "Latte", Kind = ItemKind.Drink, PricePence = 340, ServeSeconds = 70,
                    Recipe = new[] { new RecipePart(CoffeeBeans, 1), new RecipePart(Milk, 1) },
                },
                new ItemDef
                {
                    Id = Tea, Name = "Pot of tea", PluralName = "Pots of tea", Kind = ItemKind.Drink, PricePence = 240, ServeSeconds = 35,
                    Recipe = new[] { new RecipePart(TeaLeaves, 1) },
                },
                new ItemDef
                {
                    Id = Croissant, Name = "Croissant", Kind = ItemKind.Pastry, PricePence = 280, UnitCostPence = 70,
                    ServeSeconds = 12, DairyFree = false,
                },
            },
            Ingredients = new[]
            {
                new IngredientDef { Id = CoffeeBeans, Name = "Coffee", CostPerPortionPence = 28 },
                new IngredientDef { Id = TeaLeaves, Name = "Tea", CostPerPortionPence = 9 },
                new IngredientDef { Id = Milk, Name = "Milk", SpoilsOvernight = true, IsMilk = true },
            },
            MilkOptions = new[]
            {
                new MilkOption { Id = DairyMilk, Name = "Dairy milk", CostPerPortionPence = 22, DairyFree = false, Appeal = 1.0 },
                new MilkOption { Id = OatMilk, Name = "Oat milk", CostPerPortionPence = 38, DairyFree = true, Appeal = 0.88 },
            },
            Segments = new[]
            {
                new SegmentDef
                {
                    Id = Commuters, Name = "Commuters",
                    Description = "On the way to work: want a coffee, and want it quickly.",
                    DrinkChance = 0.95, PastryChance = 0.35,
                    DrinkTaste = new[] { new Weighted(Latte, 3), new Weighted(Tea, 1) },
                    PastryTaste = new[] { new Weighted(Croissant, 1) },
                    DairyFreeShare = 0.15, BudgetPence = 650, PatienceSeconds = 150, SecondChoiceChance = 0.4,
                },
                new SegmentDef
                {
                    Id = Locals, Name = "Locals",
                    Description = "Villagers popping in: happy to wait for a chat, and fond of a pastry.",
                    DrinkChance = 0.85, PastryChance = 0.55,
                    DrinkTaste = new[] { new Weighted(Latte, 1.5), new Weighted(Tea, 1.5) },
                    PastryTaste = new[] { new Weighted(Croissant, 1) },
                    DairyFreeShare = 0.10, BudgetPence = 800, PatienceSeconds = 420, SecondChoiceChance = 0.7,
                },
            },
            Sites = new[]
            {
                new SiteDef
                {
                    Id = HighStreet, Name = "High Street", DailyCostPence = 7500,
                    Slots = new[]
                    {
                        new SlotDef
                        {
                            Name = "Morning rush", DurationSeconds = 3600, Footfall = 26, Baristas = 1,
                            SegmentMix = new[] { new Weighted(Commuters, 0.7), new Weighted(Locals, 0.3) },
                        },
                        new SlotDef
                        {
                            Name = "Midday", DurationSeconds = 7200, Footfall = 20, Baristas = 1,
                            SegmentMix = new[] { new Weighted(Commuters, 0.25), new Weighted(Locals, 0.75) },
                        },
                        new SlotDef
                        {
                            Name = "Afternoon", DurationSeconds = 7200, Footfall = 16, Baristas = 1,
                            SegmentMix = new[] { new Weighted(Commuters, 0.15), new Weighted(Locals, 0.85) },
                        },
                    },
                },
            },
            Upgrades = new[]
            {
                new UpgradeDef
                {
                    Id = BiggerDisplay, Name = "Bigger display case", Kind = UpgradeKind.Capacity,
                    CostPence = 24000, RunningCostPence = 300,
                    Effects = new[] { new UpgradeEffect(UpgradeStat.DisplayCapacity, 14) },
                    Summary = "Room for 14 more pastries on the counter.",
                    TradeOff = "Costs £3 a day to keep chilled, and a fuller case means more to throw away on a quiet day.",
                },
            },
        };
    }
}
