using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// Every number the game is balanced by: prices, costs, capacities, customers and upgrades.
    /// The game logic reads them from here and contains none of its own, so tuning never means
    /// touching the rules. <see cref="DefaultBalance"/> holds the shipped values.
    /// </summary>
    public sealed record CoffeeShopBalance
    {
        public int StartingCashPence { get; init; }

        /// <summary>How far below zero cash may go to buy stock, so one bad day is recoverable.</summary>
        public int OverdraftPence { get; init; }

        /// <summary>Pastries the display case holds before upgrades.</summary>
        public int DisplayCapacity { get; init; }

        /// <summary>Portions of milk the fridge holds before upgrades.</summary>
        public int FridgeCapacity { get; init; }

        /// <summary>Cups, lids and ice for each drink sold, topped up automatically.</summary>
        public int SuppliesPerDrinkPence { get; init; }

        /// <summary>Seconds at the till for each customer who buys something, on top of making their order.</summary>
        public int PaymentSeconds { get; init; }

        /// <summary>
        /// How much busier or quieter a day can be than usual, either way (0.3 is ±30%). Rolled once
        /// a day and kept from the player, so they stock without knowing exactly who'll come.
        /// </summary>
        public double DailyFootfallSpread { get; init; }

        /// <summary>How far one customer's budget can differ from their segment's, either way (0.25 is ±25%).</summary>
        public double BudgetSpread { get; init; }

        /// <summary>How far one customer's patience can differ from their segment's, either way.</summary>
        public double PatienceSpread { get; init; }

        /// <summary>Cash a new game keeps back from upgrades until the player changes it.</summary>
        public int StartingReservePence { get; init; }

        /// <summary>Days of results kept for the review and the save.</summary>
        public int HistoryDays { get; init; } = 14;

        public string StartingSiteId { get; init; }

        public string StartingMilkId { get; init; }

        public IReadOnlyList<string> StartingMenu { get; init; } = new string[0];

        /// <summary>How many of each item a new game plans for: pastries to bake, servings of drinks to stock.</summary>
        public IReadOnlyList<ItemCount> StartingPlan { get; init; } = new ItemCount[0];

        public IReadOnlyList<ItemDef> Items { get; init; } = new ItemDef[0];

        public IReadOnlyList<IngredientDef> Ingredients { get; init; } = new IngredientDef[0];

        public IReadOnlyList<MilkOption> MilkOptions { get; init; } = new MilkOption[0];

        public IReadOnlyList<SegmentDef> Segments { get; init; } = new SegmentDef[0];

        public IReadOnlyList<SiteDef> Sites { get; init; } = new SiteDef[0];

        public IReadOnlyList<UpgradeDef> Upgrades { get; init; } = new UpgradeDef[0];

        public ItemDef Item(string id) => Find(Items, i => i.Id == id, "item", id);

        public IngredientDef Ingredient(string id) => Find(Ingredients, i => i.Id == id, "ingredient", id);

        public MilkOption Milk(string id) => Find(MilkOptions, m => m.Id == id, "milk", id);

        public SegmentDef Segment(string id) => Find(Segments, s => s.Id == id, "segment", id);

        public SiteDef Site(string id) => Find(Sites, s => s.Id == id, "site", id);

        public UpgradeDef Upgrade(string id) => Find(Upgrades, u => u.Id == id, "upgrade", id);

        public bool HasItem(string id) => Items.Any(i => i.Id == id);

        public bool HasUpgrade(string id) => Upgrades.Any(u => u.Id == id);

        public bool HasMilk(string id) => MilkOptions.Any(m => m.Id == id);

        public bool HasSite(string id) => Sites.Any(s => s.Id == id);

        /// <summary>The ingredient every milky drink uses, if there is one.</summary>
        public IngredientDef MilkIngredient => Ingredients.FirstOrDefault(i => i.IsMilk);

        public bool IsMilky(ItemDef item) => item.Recipe.Any(part => Ingredient(part.IngredientId).IsMilk);

        /// <summary>
        /// What a drink is mostly made of: its first ingredient that isn't milk (coffee for a latte).
        /// The paper lists each drink under that ingredient's supplier.
        /// </summary>
        public IngredientDef MainIngredient(ItemDef drink) =>
            drink.Recipe.Select(part => Ingredient(part.IngredientId)).FirstOrDefault(i => !i.IsMilk)
            ?? Ingredient(drink.Recipe[0].IngredientId);

        /// <summary>Everything that refers to something missing, or can't be right. Empty when the balance is sound.</summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();
            void Check(bool ok, string problem)
            {
                if (!ok)
                {
                    problems.Add(problem);
                }
            }

            bool HasIngredient(string id) => Ingredients.Any(i => i.Id == id);
            bool HasSegment(string id) => Segments.Any(s => s.Id == id);

            CheckUnique(Items.Select(i => i.Id), "item", problems);
            CheckUnique(Ingredients.Select(i => i.Id), "ingredient", problems);
            CheckUnique(MilkOptions.Select(m => m.Id), "milk", problems);
            CheckUnique(Segments.Select(s => s.Id), "segment", problems);
            CheckUnique(Sites.Select(s => s.Id), "site", problems);
            CheckUnique(Upgrades.Select(u => u.Id), "upgrade", problems);

            Check(Ingredients.Count(i => i.IsMilk) <= 1, "More than one ingredient is marked as milk.");
            Check(MilkIngredient == null || MilkOptions.Count > 0, "Drinks use milk but there are no milk options.");
            Check(HasSite(StartingSiteId), $"Starting site '{StartingSiteId}' doesn't exist.");
            Check(MilkOptions.Count == 0 || HasMilk(StartingMilkId), $"Starting milk '{StartingMilkId}' doesn't exist.");
            Check(DisplayCapacity >= 0 && FridgeCapacity >= 0, "Capacities can't be negative.");

            foreach (var id in StartingMenu)
            {
                Check(HasItem(id), $"Starting menu item '{id}' doesn't exist.");
            }

            foreach (var planned in StartingPlan)
            {
                Check(HasItem(planned.ItemId), $"Starting plan item '{planned.ItemId}' doesn't exist.");
            }

            foreach (var item in Items)
            {
                Check(item.PricePence > 0, $"{item.Id} has no price.");
                Check(item.Kind != ItemKind.Drink || item.Recipe.Count > 0, $"Drink {item.Id} has no recipe.");
                Check(item.Kind != ItemKind.Pastry || item.Recipe.Count == 0, $"Pastry {item.Id} shouldn't have a recipe.");
                foreach (var part in item.Recipe)
                {
                    Check(HasIngredient(part.IngredientId), $"{item.Id} uses missing ingredient '{part.IngredientId}'.");
                    Check(part.Portions > 0, $"{item.Id} uses no {part.IngredientId}.");
                }
            }

            foreach (var segment in Segments)
            {
                foreach (var taste in segment.DrinkTaste)
                {
                    Check(HasItem(taste.Id) && Item(taste.Id).Kind == ItemKind.Drink, $"{segment.Id} likes '{taste.Id}', which isn't a drink.");
                }

                foreach (var taste in segment.PastryTaste)
                {
                    Check(HasItem(taste.Id) && Item(taste.Id).Kind == ItemKind.Pastry, $"{segment.Id} likes '{taste.Id}', which isn't a pastry.");
                }
            }

            foreach (var site in Sites)
            {
                Check(site.Slots.Count > 0, $"Site {site.Id} has no trading slots.");
                foreach (var slot in site.Slots)
                {
                    Check(slot.DurationSeconds > 0 && slot.Baristas > 0, $"{site.Id} {slot.Name} needs a length and a barista.");
                    foreach (var mix in slot.SegmentMix)
                    {
                        Check(HasSegment(mix.Id), $"{site.Id} {slot.Name} draws missing segment '{mix.Id}'.");
                    }
                }
            }

            foreach (var upgrade in Upgrades)
            {
                Check(upgrade.CostPence > 0, $"Upgrade {upgrade.Id} is free.");
                Check(!string.IsNullOrEmpty(upgrade.TradeOff), $"Upgrade {upgrade.Id} has no trade-off.");
            }

            return problems;
        }

        private static void CheckUnique(IEnumerable<string> ids, string kind, List<string> problems)
        {
            foreach (var group in ids.GroupBy(id => id).Where(g => g.Count() > 1))
            {
                problems.Add($"More than one {kind} called '{group.Key}'.");
            }
        }

        private static T Find<T>(IEnumerable<T> all, Func<T, bool> match, string kind, string id) where T : class =>
            all.FirstOrDefault(match) ?? throw new ArgumentException($"No {kind} called '{id}' in the balance.");
    }
}
