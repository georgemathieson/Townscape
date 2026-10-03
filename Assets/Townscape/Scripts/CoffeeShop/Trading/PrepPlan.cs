using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>One item on the menu as it will be stocked.</summary>
    /// <param name="Count">Pastries to bake, or servings of a drink to buy ingredients for, after capacity limits.</param>
    /// <param name="Requested">What the player asked for, before capacity limits.</param>
    /// <param name="Makeable">Servings the stock covers at opening (for pastries, the number baked).</param>
    /// <param name="CostPence">For pastries, what baking them costs. Drinks' costs are in the ingredient lines.</param>
    public sealed record PlanLine(string ItemId, ItemKind Kind, int Count, int Requested, int Makeable, int CostPence)
    {
        public bool CanMake => Makeable > 0;
    }

    /// <summary>One ingredient the menu needs.</summary>
    /// <param name="Needed">Portions the planned servings use.</param>
    /// <param name="InPantry">Portions left from earlier days.</param>
    /// <param name="ToBuy">Portions to buy now.</param>
    public sealed record IngredientLine(string IngredientId, int Needed, int InPantry, int ToBuy, int CostPerPortionPence)
    {
        public int CostPence => ToBuy * CostPerPortionPence;
    }

    /// <summary>
    /// What the shop will open with: the stock a menu and plan work out to, capacities after upgrades,
    /// what it all costs and anything the player should know before opening. Ingredient needs are
    /// derived from the drinks on the menu, so the player only ever chooses drinks and servings.
    /// </summary>
    public sealed class PrepPlan
    {
        private PrepPlan()
        {
        }

        public int DisplayCapacity { get; private set; }

        public int FridgeCapacity { get; private set; }

        public IReadOnlyList<PlanLine> Lines { get; private set; }

        public IReadOnlyList<IngredientLine> Ingredients { get; private set; }

        /// <summary>Pastries on display at opening.</summary>
        public int PastriesPlanned => Lines.Where(l => l.Kind == ItemKind.Pastry).Sum(l => l.Count);

        /// <summary>Portions of milk in the fridge at opening.</summary>
        public int MilkPlanned { get; private set; }

        /// <summary>Pastries and ingredients to buy before opening.</summary>
        public int StockCostPence => Lines.Sum(l => l.CostPence) + Ingredients.Sum(i => i.CostPence);

        /// <summary>Rent, wages and upgrade upkeep, charged at closing whatever happens.</summary>
        public int FixedCostPence { get; private set; }

        /// <summary>The most the stock can cost: cash plus the overdraft.</summary>
        public int SpendLimitPence { get; private set; }

        public bool CanAfford => StockCostPence <= SpendLimitPence;

        /// <summary>Things to sort out or know about before opening, in menu order.</summary>
        public IReadOnlyList<string> Warnings { get; private set; }

        /// <summary>Stock at opening: ingredient portions and pastries, by id.</summary>
        public IReadOnlyDictionary<string, int> OpeningStock { get; private set; }

        public PlanLine Line(string itemId) => Lines.FirstOrDefault(l => l.ItemId == itemId);

        public static PrepPlan For(CoffeeShopBalance balance, CoffeeShopState state)
        {
            var displayCapacity = Capacity(balance, state, UpgradeStat.DisplayCapacity, balance.DisplayCapacity);
            var fridgeCapacity = Capacity(balance, state, UpgradeStat.FridgeCapacity, balance.FridgeCapacity);
            var milk = balance.MilkOptions.Count > 0 ? balance.Milk(state.MilkId) : null;
            var milkId = balance.MilkIngredient?.Id;
            var menu = state.Menu.Select(balance.Item).ToList();
            var warnings = new List<string>();

            // Fit the plan into the display case and the fridge, in menu order.
            var counts = new Dictionary<string, int>();
            var displayLeft = displayCapacity;
            var fridgeLeft = fridgeCapacity;
            foreach (var item in menu)
            {
                var requested = Math.Max(0, state.Planned.Get(item.Id));
                var count = requested;
                if (item.Kind == ItemKind.Pastry)
                {
                    count = Math.Min(count, displayLeft);
                    displayLeft -= count;
                }
                else
                {
                    var milkPerServing = MilkPerServing(item, milkId);
                    if (milkPerServing > 0)
                    {
                        count = Math.Min(count, fridgeLeft / milkPerServing);
                        fridgeLeft -= count * milkPerServing;
                    }
                }

                counts[item.Id] = count;
            }

            // Ingredients follow from the drinks: servings times the recipe, less what's in the pantry.
            var needed = new Dictionary<string, int>();
            foreach (var item in menu.Where(i => i.Kind == ItemKind.Drink))
            {
                foreach (var part in item.Recipe)
                {
                    needed.TryGetValue(part.IngredientId, out var portions);
                    needed[part.IngredientId] = portions + (counts[item.Id] * part.Portions);
                }
            }

            var ingredients = new List<IngredientLine>();
            var stock = new Dictionary<string, int>();
            foreach (var ingredient in balance.Ingredients)
            {
                needed.TryGetValue(ingredient.Id, out var need);
                var inPantry = ingredient.SpoilsOvernight ? 0 : Math.Max(0, state.Pantry.Get(ingredient.Id));
                if (need == 0 && inPantry == 0)
                {
                    continue;
                }

                var toBuy = Math.Max(0, need - inPantry);
                var cost = ingredient.IsMilk && milk != null ? milk.CostPerPortionPence : ingredient.CostPerPortionPence;
                ingredients.Add(new IngredientLine(ingredient.Id, need, inPantry, toBuy, cost));
                stock[ingredient.Id] = inPantry + toBuy;
            }

            var lines = new List<PlanLine>();
            foreach (var item in menu)
            {
                var count = counts[item.Id];
                var requested = Math.Max(0, state.Planned.Get(item.Id));
                int makeable;
                if (item.Kind == ItemKind.Pastry)
                {
                    stock[item.Id] = count;
                    makeable = count;
                }
                else
                {
                    makeable = item.Recipe.Min(part => stock.TryGetValue(part.IngredientId, out var have) ? have / part.Portions : 0);
                }

                lines.Add(new PlanLine(item.Id, item.Kind, count, requested, makeable, item.Kind == ItemKind.Pastry ? count * item.UnitCostPence : 0));

                if (count < requested)
                {
                    warnings.Add(item.Kind == ItemKind.Pastry
                        ? $"Only room for {count} {Name(item, count)} in the display case."
                        : $"Only enough fridge space for {count} {Name(item, count)}.");
                }

                if (makeable == 0)
                {
                    warnings.Add(item.Kind == ItemKind.Pastry
                        ? $"No {Name(item, 2)} to sell: bake some or take them off the menu."
                        : $"{item.Name} can't be made: nothing is stocked for it.");
                }
            }

            if (menu.Count == 0)
            {
                warnings.Add("The menu is empty: nobody will buy anything.");
            }

            var site = balance.Site(state.SiteId);
            var plan = new PrepPlan
            {
                DisplayCapacity = displayCapacity,
                FridgeCapacity = fridgeCapacity,
                Lines = lines,
                Ingredients = ingredients,
                MilkPlanned = milkId != null && stock.TryGetValue(milkId, out var milkStock) ? milkStock : 0,
                FixedCostPence = site.DailyCostPence + ActiveUpgrades(balance, state).Sum(u => u.RunningCostPence),
                SpendLimitPence = state.CashPence + balance.OverdraftPence,
                OpeningStock = stock,
            };

            if (!plan.CanAfford)
            {
                warnings.Add($"The stock costs {Money.Format(plan.StockCostPence)} but only {Money.Format(Math.Max(0, plan.SpendLimitPence))} is available, overdraft included.");
            }

            plan.Warnings = warnings;
            return plan;
        }

        /// <summary>The most of <paramref name="itemId"/> that fits alongside the rest of the plan.</summary>
        public static int Room(CoffeeShopBalance balance, CoffeeShopState state, string itemId)
        {
            var item = balance.Item(itemId);
            if (item.Kind == ItemKind.Pastry)
            {
                var others = state.Menu
                    .Where(id => id != itemId && balance.Item(id).Kind == ItemKind.Pastry)
                    .Sum(id => Math.Max(0, state.Planned.Get(id)));
                return Math.Max(0, Capacity(balance, state, UpgradeStat.DisplayCapacity, balance.DisplayCapacity) - others);
            }

            var milkId = balance.MilkIngredient?.Id;
            var milkPerServing = MilkPerServing(item, milkId);
            if (milkPerServing == 0)
            {
                return int.MaxValue;
            }

            var otherMilk = state.Menu
                .Where(id => id != itemId)
                .Select(balance.Item)
                .Sum(other => MilkPerServing(other, milkId) * Math.Max(0, state.Planned.Get(other.Id)));
            return Math.Max(0, Capacity(balance, state, UpgradeStat.FridgeCapacity, balance.FridgeCapacity) - otherMilk) / milkPerServing;
        }

        /// <summary>Upgrades owned and working on the state's day.</summary>
        public static IEnumerable<UpgradeDef> ActiveUpgrades(CoffeeShopBalance balance, CoffeeShopState state) =>
            balance.Upgrades.Where(u => state.HasActiveUpgrade(u.Id));

        private static int Capacity(CoffeeShopBalance balance, CoffeeShopState state, UpgradeStat stat, int baseCapacity) =>
            baseCapacity + ActiveUpgrades(balance, state).SelectMany(u => u.Effects).Where(e => e.Stat == stat).Sum(e => e.Amount);

        private static int MilkPerServing(ItemDef item, string milkId) =>
            milkId == null ? 0 : item.Recipe.Where(p => p.IngredientId == milkId).Sum(p => p.Portions);

        private static string Name(ItemDef item, int count) => (count == 1 ? item.Name : item.PluralName).ToLowerInvariant();
    }
}
