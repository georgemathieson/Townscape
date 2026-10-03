using NUnit.Framework;
using Townscape.CoffeeShop;
using static Townscape.Tests.CoffeeShop.CoffeeShopTesting;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class PrepPlanTests
    {
        [Test]
        public void IngredientNeeds_AreWorkedOutFromTheDrinks()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 30), (DefaultBalance.Tea, 20), (DefaultBalance.Croissant, 10));

            var plan = PrepPlan.For(Balance, state);

            Assert.That(plan.Ingredient(DefaultBalance.CoffeeBeans).Needed, Is.EqualTo(30));
            Assert.That(plan.Ingredient(DefaultBalance.Milk).Needed, Is.EqualTo(30));
            Assert.That(plan.Ingredient(DefaultBalance.TeaLeaves).Needed, Is.EqualTo(20));
            Assert.That(plan.StockCostPence, Is.EqualTo(
                (30 * PortionCost(DefaultBalance.CoffeeBeans))
                + (30 * MilkCost(DefaultBalance.DairyMilk))
                + (20 * PortionCost(DefaultBalance.TeaLeaves))
                + (10 * Balance.Item(DefaultBalance.Croissant).UnitCostPence)));
        }

        [Test]
        public void ChangingTheMenu_ChangesTheNeeds()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 30), (DefaultBalance.Tea, 20));

            var plan = PrepPlan.For(Balance, state.WithMenu(DefaultBalance.Tea));

            Assert.That(plan.Ingredient(DefaultBalance.CoffeeBeans), Is.Null);
            Assert.That(plan.Ingredient(DefaultBalance.Milk), Is.Null);
            Assert.That(plan.Ingredient(DefaultBalance.TeaLeaves).Needed, Is.EqualTo(20));
            Assert.That(plan.Line(DefaultBalance.Latte), Is.Null);
        }

        [Test]
        public void ThePantry_IsUsedBeforeBuyingMore()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 30)) with
            {
                Pantry = ValueMap<int>.Empty.With(DefaultBalance.CoffeeBeans, 12),
            };

            var coffee = PrepPlan.For(Balance, state).Ingredient(DefaultBalance.CoffeeBeans);

            Assert.That(coffee.InPantry, Is.EqualTo(12));
            Assert.That(coffee.ToBuy, Is.EqualTo(18));
            Assert.That(coffee.CostPence, Is.EqualTo(18 * PortionCost(DefaultBalance.CoffeeBeans)));
        }

        [Test]
        public void TheMilkChoice_SetsTheMilkPrice()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 10));

            var dairy = PrepPlan.For(Balance, state with { MilkId = DefaultBalance.DairyMilk });
            var oat = PrepPlan.For(Balance, state with { MilkId = DefaultBalance.OatMilk });

            Assert.That(dairy.Ingredient(DefaultBalance.Milk).CostPence, Is.EqualTo(10 * MilkCost(DefaultBalance.DairyMilk)));
            Assert.That(oat.Ingredient(DefaultBalance.Milk).CostPence, Is.EqualTo(10 * MilkCost(DefaultBalance.OatMilk)));
            Assert.That(MilkCost(DefaultBalance.OatMilk), Is.GreaterThan(MilkCost(DefaultBalance.DairyMilk)), "the alternative costs more");
        }

        [Test]
        public void UnmakeableItems_AreFlagged()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 0), (DefaultBalance.Tea, 5), (DefaultBalance.Croissant, 0));

            var plan = PrepPlan.For(Balance, state);

            Assert.That(plan.Line(DefaultBalance.Latte).CanMake, Is.False);
            Assert.That(plan.Line(DefaultBalance.Tea).CanMake, Is.True);
            Assert.That(plan.Line(DefaultBalance.Croissant).CanMake, Is.False);
            Assert.That(plan.Warnings, Has.Some.Contains("Latte can't be made"));
            Assert.That(plan.Warnings, Has.Some.Contains("No croissants"));
        }

        [Test]
        public void LeftoverIngredients_CanMakeADrinkWithNothingPlanned()
        {
            var state = NewGame().WithPlan((DefaultBalance.Tea, 0)) with
            {
                Pantry = ValueMap<int>.Empty.With(DefaultBalance.TeaLeaves, 4),
            };

            var tea = PrepPlan.For(Balance, state).Line(DefaultBalance.Tea);

            Assert.That(tea.Makeable, Is.EqualTo(4));
        }

        [Test]
        public void Capacity_LimitsPastriesAndMilk()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 500), (DefaultBalance.Croissant, 500));

            var plan = PrepPlan.For(Balance, state);

            Assert.That(plan.Line(DefaultBalance.Croissant).Count, Is.EqualTo(Balance.DisplayCapacity));
            Assert.That(plan.Line(DefaultBalance.Latte).Count, Is.EqualTo(Balance.FridgeCapacity));
            Assert.That(plan.Warnings, Has.Some.Contains("Only room for 12 croissants"));
            Assert.That(PrepPlan.Room(Balance, state, DefaultBalance.Tea), Is.EqualTo(int.MaxValue), "tea needs no fridge space");
        }

        [Test]
        public void TheBiggerDisplay_AddsRoom_OnlyOnceItIsWorking()
        {
            var state = NewGame().WithPlan((DefaultBalance.Croissant, 500)) with { Day = 3 };

            var boughtToday = state with { Upgrades = ValueMap<int>.Empty.With(DefaultBalance.BiggerDisplay, 3) };
            var notYet = state with { Upgrades = ValueMap<int>.Empty.With(DefaultBalance.BiggerDisplay, 4) };

            Assert.That(PrepPlan.For(Balance, boughtToday).DisplayCapacity, Is.EqualTo(26));
            Assert.That(PrepPlan.For(Balance, boughtToday).Line(DefaultBalance.Croissant).Count, Is.EqualTo(26));
            Assert.That(PrepPlan.For(Balance, notYet).DisplayCapacity, Is.EqualTo(12));
            Assert.That(PrepPlan.For(Balance, boughtToday).FixedCostPence,
                Is.EqualTo(Rent + Balance.Upgrade(DefaultBalance.BiggerDisplay).RunningCostPence), "the case costs to run");
        }

        [Test]
        public void StockBeyondCashAndOverdraft_CantBeAfforded()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 60), (DefaultBalance.Croissant, 12)) with { CashPence = -4900 };

            var plan = PrepPlan.For(Balance, state);

            Assert.That(plan.SpendLimitPence, Is.EqualTo(100));
            Assert.That(plan.CanAfford, Is.False);
            Assert.That(plan.Warnings, Has.Some.Contains("only £1 is available"));
        }

        [Test]
        public void Forecast_ShowsWhoComesWhen()
        {
            var forecast = Forecast.For(Balance, Balance.Site(DefaultBalance.HighStreet));

            Assert.That(forecast.Customers, Is.EqualTo(62).Within(0.001));
            Assert.That(forecast.Slots[0].Segments[0].Id, Is.EqualTo(DefaultBalance.Commuters));
            Assert.That(forecast.Slots[0].Segments[0].Weight, Is.EqualTo(26 * 0.7).Within(0.001));
            Assert.That(forecast.FirstChoicesOf(DefaultBalance.Latte), Is.GreaterThan(forecast.FirstChoicesOf(DefaultBalance.Tea)));
            Assert.That(forecast.FirstChoicesOf(DefaultBalance.Croissant), Is.GreaterThan(Balance.DisplayCapacity), "the small case should sell out on an average day");
        }
    }
}
