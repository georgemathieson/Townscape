using System.Collections.Generic;
using System.Linq;
using Townscape.CoffeeShop;

namespace Townscape.Tests.CoffeeShop
{
    /// <summary>Shared set-ups for the coffee shop tests.</summary>
    internal static class CoffeeShopTesting
    {
        public static readonly CoffeeShopBalance Balance = DefaultBalance.Create();

        public static int Rent => Balance.Site(DefaultBalance.HighStreet).DailyCostPence;

        public static int CaseCost => Balance.Upgrade(DefaultBalance.BiggerDisplay).CostPence;

        public static int Price(string itemId) => Balance.Item(itemId).PricePence;

        public static int PortionCost(string ingredientId) => Balance.Ingredient(ingredientId).CostPerPortionPence;

        public static int MilkCost(string milkId) => Balance.Milk(milkId).CostPerPortionPence;

        public static CoffeeShopState NewGame(int seed = 1) => CoffeeShopState.NewGame(Balance, seed);

        public static CoffeeShopState WithPlan(this CoffeeShopState state, params (string Item, int Count)[] plan) =>
            state with { Planned = ValueMap<int>.From(plan.Select(p => new KeyValuePair<string, int>(p.Item, p.Count))) };

        public static CoffeeShopState WithMenu(this CoffeeShopState state, params string[] items) =>
            state with { Menu = ValueList<string>.From(items) };

        /// <summary>A customer who arrives at <paramref name="seconds"/>, patient, with money to spare, who'll take only their first choice.</summary>
        public static Customer Customer(
            double seconds = 0,
            string drink = null,
            string pastry = null,
            string segment = DefaultBalance.Locals,
            bool dairyFree = false,
            double milkFussiness = 0,
            int budget = 2000,
            int patience = 100000,
            bool secondChoice = false,
            int slot = 0,
            string[] drinks = null) => new Customer
            {
                SlotIndex = slot,
                ArrivalSeconds = seconds,
                SegmentId = segment,
                DairyFree = dairyFree,
                MilkFussiness = milkFussiness,
                BudgetPence = budget,
                PatienceSeconds = patience,
                Drinks = drinks ?? (drink == null ? new string[0] : new[] { drink }),
                Pastries = pastry == null ? new string[0] : new[] { pastry },
                TakesSecondChoice = secondChoice,
            };

        public static ItemOutcome Item(this DayResult result, string id) => result.Items.First(i => i.ItemId == id);

        public static IngredientLine Ingredient(this PrepPlan plan, string id) => plan.Ingredients.FirstOrDefault(i => i.IngredientId == id);
    }
}
