using System.Linq;
using NUnit.Framework;
using Townscape.CoffeeShop;
using static Townscape.Tests.CoffeeShop.CoffeeShopTesting;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class TradingDayTests
    {
        private static CoffeeShopState Stocked() =>
            NewGame().WithPlan((DefaultBalance.Latte, 30), (DefaultBalance.Tea, 20), (DefaultBalance.Croissant, 10));

        [Test]
        public void TheSameSeed_GivesTheSameDay()
        {
            var a = TradingDay.Run(Balance, Stocked());
            var b = TradingDay.Run(Balance, Stocked());

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.Customers, Is.GreaterThan(0));
        }

        [Test]
        public void DifferentDaysAndGames_GetDifferentCustomers()
        {
            var day1 = TradingDay.CustomersFor(Balance, Stocked());
            var day2 = TradingDay.CustomersFor(Balance, Stocked() with { Day = 2 });
            var otherGame = TradingDay.CustomersFor(Balance, Stocked() with { Seed = 2 });

            Assert.That(day2.Select(c => c.ArrivalSeconds), Is.Not.EqualTo(day1.Select(c => c.ArrivalSeconds)));
            Assert.That(otherGame.Select(c => c.ArrivalSeconds), Is.Not.EqualTo(day1.Select(c => c.ArrivalSeconds)));
        }

        [Test]
        public void SomeDays_AreBusierThanOthers()
        {
            var counts = Enumerable.Range(1, 60).Select(day => TradingDay.CustomersFor(Balance, Stocked() with { Day = day }).Count).ToList();
            var usual = Forecast.For(Balance, Balance.Site(DefaultBalance.HighStreet)).Customers;

            Assert.That(counts.Average(), Is.EqualTo(usual).Within(usual * 0.05));
            Assert.That(counts.Min(), Is.LessThan(usual * 0.8));
            Assert.That(counts.Max(), Is.GreaterThan(usual * 1.2));
        }

        [Test]
        public void Customers_ArriveInOrder_FromTheRightSegments()
        {
            var customers = TradingDay.CustomersFor(Balance, Stocked());

            Assert.That(customers.Select(c => c.ArrivalSeconds), Is.Ordered);
            Assert.That(customers.Select(c => c.SegmentId).Distinct(), Is.SubsetOf(new[] { DefaultBalance.Commuters, DefaultBalance.Locals }));
            Assert.That(customers.Where(c => c.SlotIndex == 0).Count(c => c.SegmentId == DefaultBalance.Commuters),
                Is.GreaterThan(customers.Where(c => c.SlotIndex == 0).Count(c => c.SegmentId == DefaultBalance.Locals)),
                "the morning rush is mostly commuters");
        }

        [Test]
        public void AGoodMatch_Sells()
        {
            var result = TradingDay.Trade(Balance, Stocked(), new[] { Customer(drink: DefaultBalance.Latte, pastry: DefaultBalance.Croissant) });

            Assert.That(result.Item(DefaultBalance.Latte).Sold, Is.EqualTo(1));
            Assert.That(result.Item(DefaultBalance.Croissant).Sold, Is.EqualTo(1));
            Assert.That(result.RevenuePence, Is.EqualTo(Price(DefaultBalance.Latte) + Price(DefaultBalance.Croissant)));
            Assert.That(result.Served, Is.EqualTo(1));
            Assert.That(result.SuppliesPence, Is.EqualTo(Balance.SuppliesPerDrinkPence), "one drink's cup");
        }

        [Test]
        public void SomethingNotOnTheMenu_IsAMiss_UnlessTheyTakeASecondChoice()
        {
            var state = Stocked().WithMenu(DefaultBalance.Tea);
            var fussy = Customer(drinks: new[] { DefaultBalance.Latte, DefaultBalance.Tea });
            var easy = Customer(drinks: new[] { DefaultBalance.Latte, DefaultBalance.Tea }, secondChoice: true);

            var result = TradingDay.Trade(Balance, state, new[] { fussy, easy });

            Assert.That(result.Item(DefaultBalance.Latte).Missed.NotOnMenu, Is.EqualTo(1));
            Assert.That(result.Item(DefaultBalance.Latte).Wanted, Is.EqualTo(2));
            Assert.That(result.Item(DefaultBalance.Tea).Sold, Is.EqualTo(1));
            Assert.That(result.Item(DefaultBalance.Tea).SoldAsSecondChoice, Is.EqualTo(1));
        }

        [Test]
        public void DairyFreeCustomers_NeedTheAlternativeMilk_ForMilkyDrinks()
        {
            var customers = new[] { Customer(drink: DefaultBalance.Latte, dairyFree: true), Customer(drink: DefaultBalance.Tea, dairyFree: true) };

            var dairy = TradingDay.Trade(Balance, Stocked() with { MilkId = DefaultBalance.DairyMilk }, customers);
            var oat = TradingDay.Trade(Balance, Stocked() with { MilkId = DefaultBalance.OatMilk }, customers);

            Assert.That(dairy.Item(DefaultBalance.Latte).Missed.Dietary, Is.EqualTo(1));
            Assert.That(dairy.Item(DefaultBalance.Tea).Sold, Is.EqualTo(1), "tea is fine either way");
            Assert.That(oat.Item(DefaultBalance.Latte).Sold, Is.EqualTo(1));
        }

        [Test]
        public void DairyFreeCustomers_CantHaveCroissants()
        {
            var result = TradingDay.Trade(Balance, Stocked(), new[] { Customer(pastry: DefaultBalance.Croissant, dairyFree: true) });

            Assert.That(result.Item(DefaultBalance.Croissant).Missed.Dietary, Is.EqualTo(1));
        }

        [Test]
        public void SomeCustomers_DontFancyOatMilk()
        {
            var fussy = Customer(drink: DefaultBalance.Latte, milkFussiness: 0.95);
            var relaxed = Customer(drink: DefaultBalance.Latte, milkFussiness: 0.5);

            var oat = TradingDay.Trade(Balance, Stocked() with { MilkId = DefaultBalance.OatMilk }, new[] { fussy, relaxed });
            var dairy = TradingDay.Trade(Balance, Stocked() with { MilkId = DefaultBalance.DairyMilk }, new[] { fussy, relaxed });

            Assert.That(oat.Item(DefaultBalance.Latte).Missed.MilkChoice, Is.EqualTo(1));
            Assert.That(oat.Item(DefaultBalance.Latte).Sold, Is.EqualTo(1));
            Assert.That(dairy.Item(DefaultBalance.Latte).Sold, Is.EqualTo(2));
        }

        [Test]
        public void ItemsOverBudget_DontSell_AndTheDrinkComesFirst()
        {
            var customer = Customer(drink: DefaultBalance.Latte, pastry: DefaultBalance.Croissant, budget: 500);

            var result = TradingDay.Trade(Balance, Stocked(), new[] { customer });

            Assert.That(result.Item(DefaultBalance.Latte).Sold, Is.EqualTo(1));
            Assert.That(result.Item(DefaultBalance.Croissant).Missed.TooDear, Is.EqualTo(1));
        }

        [Test]
        public void StockRunsOut_ThenSalesAreMissed()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 2), (DefaultBalance.Croissant, 1));
            var customers = Enumerable.Range(0, 3).Select(i => Customer(i * 1000, DefaultBalance.Latte, DefaultBalance.Croissant)).ToArray();

            var result = TradingDay.Trade(Balance, state, customers);

            Assert.That(result.Item(DefaultBalance.Latte).Sold, Is.EqualTo(2));
            Assert.That(result.Item(DefaultBalance.Latte).Missed.SoldOut, Is.EqualTo(1));
            Assert.That(result.Item(DefaultBalance.Croissant).Sold, Is.EqualTo(1));
            Assert.That(result.Item(DefaultBalance.Croissant).Missed.SoldOut, Is.EqualTo(2));
        }

        [Test]
        public void UnsoldPastries_AreWasted_MilkIsPouredAway_TheRestKeeps()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 10), (DefaultBalance.Tea, 5), (DefaultBalance.Croissant, 8));
            var customers = new[] { Customer(drink: DefaultBalance.Latte, pastry: DefaultBalance.Croissant), Customer(10, DefaultBalance.Tea) };

            var result = TradingDay.Trade(Balance, state, customers);

            Assert.That(result.Item(DefaultBalance.Croissant).Wasted, Is.EqualTo(7));
            Assert.That(result.MilkPouredAway, Is.EqualTo(9));
            Assert.That(result.WastePence, Is.EqualTo((7 * Balance.Item(DefaultBalance.Croissant).UnitCostPence) + (9 * MilkCost(DefaultBalance.DairyMilk))));
            Assert.That(result.PantryAfter.Get(DefaultBalance.CoffeeBeans), Is.EqualTo(9));
            Assert.That(result.PantryAfter.Get(DefaultBalance.TeaLeaves), Is.EqualTo(4));
            Assert.That(result.PantryAfter.ContainsKey(DefaultBalance.Milk), Is.False);
        }

        [Test]
        public void ALongQueue_LosesImpatientCustomers()
        {
            // Five commuters at once, each a 70 s latte plus 15 s at the till, with two minutes' patience.
            var customers = Enumerable.Range(0, 5)
                .Select(_ => Customer(0, DefaultBalance.Latte, segment: DefaultBalance.Commuters, patience: 120))
                .ToArray();

            var result = TradingDay.Trade(Balance, Stocked(), customers);

            Assert.That(result.Item(DefaultBalance.Latte).Sold, Is.EqualTo(2), "waits of 0 and 85 s are fine; 170 s is too long");
            Assert.That(result.WalkedOut, Is.EqualTo(3));
            Assert.That(result.Item(DefaultBalance.Latte).Missed.Queue, Is.EqualTo(3));
            Assert.That(result.Slots[0].LongestWaitSeconds, Is.EqualTo(85));
            Assert.That(result.Segments.Single().WalkedOut, Is.EqualTo(3));
        }

        [Test]
        public void Profit_IsRevenueLessEveryCost()
        {
            var result = TradingDay.Run(Balance, Stocked());

            Assert.That(result.RevenuePence, Is.EqualTo(result.Items.Sum(i => i.Sold * Balance.Item(i.ItemId).PricePence)));
            Assert.That(result.StockPence, Is.EqualTo(PrepPlan.For(Balance, Stocked()).StockCostPence));
            Assert.That(result.SiteCostPence, Is.EqualTo(Rent));
            Assert.That(result.CostsPence, Is.EqualTo(result.StockPence + result.SuppliesPence + result.SiteCostPence + result.RunningCostPence));
            Assert.That(result.ProfitPence, Is.EqualTo(result.RevenuePence - result.CostsPence));
            Assert.That(result.CashAfterPence, Is.EqualTo(Stocked().CashPence + result.ProfitPence));
        }

        [Test]
        public void EveryWant_IsSoldOrMissed()
        {
            var state = Stocked();
            var customers = TradingDay.CustomersFor(Balance, state);
            var wants = customers.Count(c => c.Drinks.Count > 0) + customers.Count(c => c.Pastries.Count > 0);

            var result = TradingDay.Trade(Balance, state, customers);

            Assert.That(result.Sold + result.Missed, Is.EqualTo(wants));
            Assert.That(result.Items.Sum(i => i.Wanted), Is.EqualTo(wants));
            Assert.That(result.Segments.Sum(s => s.Wants), Is.EqualTo(wants));
            Assert.That(result.Segments.Sum(s => s.Customers), Is.EqualTo(customers.Count));
            Assert.That(result.Segments.Sum(s => s.RevenuePence), Is.EqualTo(result.RevenuePence));
        }

        [Test]
        public void TheBiggerDisplay_ShowsWhatItEarned()
        {
            var state = NewGame().WithPlan((DefaultBalance.Latte, 40), (DefaultBalance.Tea, 25), (DefaultBalance.Croissant, 26)) with
            {
                Upgrades = ValueMap<int>.Empty.With(DefaultBalance.BiggerDisplay, 1),
            };

            var result = TradingDay.Run(Balance, state);
            var without = TradingDay.Trade(Balance, state with { Upgrades = ValueMap<int>.Empty }, TradingDay.CustomersFor(Balance, state));
            var impact = result.UpgradeImpacts.Single();

            Assert.That(impact.UpgradeId, Is.EqualTo(DefaultBalance.BiggerDisplay));
            Assert.That(impact.ProfitPence, Is.EqualTo(result.ProfitPence - without.ProfitPence));
            Assert.That(impact.Sold, Is.EqualTo(result.Sold - without.Sold));
            Assert.That(result.Item(DefaultBalance.Croissant).Stocked, Is.EqualTo(26));
            Assert.That(without.Item(DefaultBalance.Croissant).Stocked, Is.EqualTo(12));
            Assert.That(result.RunningCostPence, Is.EqualTo(Balance.Upgrade(DefaultBalance.BiggerDisplay).RunningCostPence));
        }
    }
}
