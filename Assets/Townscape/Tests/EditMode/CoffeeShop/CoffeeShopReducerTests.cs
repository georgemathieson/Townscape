using System.Linq;
using NUnit.Framework;
using Townscape.CoffeeShop;
using Townscape.State;
using static Townscape.Tests.CoffeeShop.CoffeeShopTesting;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class CoffeeShopReducerTests
    {
        private static Store<CoffeeShopState> CreateStore(CoffeeShopState state = null) =>
            new Store<CoffeeShopState>(CoffeeShopReducer.Create(Balance), state ?? NewGame());

        private static Store<CoffeeShopState> Rich() => CreateStore(NewGame() with { CashPence = 100000 });

        [Test]
        public void ANewGame_StartsReadyToPrep()
        {
            var state = NewGame(7);

            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.Prep));
            Assert.That(state.CashPence, Is.EqualTo(Balance.StartingCashPence));
            Assert.That(state.Menu, Is.EqualTo(new[] { DefaultBalance.Latte, DefaultBalance.Tea, DefaultBalance.Croissant }));
            Assert.That(state.Planned.Get(DefaultBalance.Latte), Is.EqualTo(30));
            Assert.That(PrepPlan.For(Balance, state).Warnings, Is.Empty, "the starting plan is sensible");
        }

        [Test]
        public void Planning_IsLimitedByCapacity()
        {
            var store = CreateStore();

            store.Dispatch(new SetPlanned(DefaultBalance.Croissant, 40));
            store.Dispatch(new SetPlanned(DefaultBalance.Latte, 200));
            store.Dispatch(new SetPlanned(DefaultBalance.Tea, -3));

            Assert.That(store.State.Planned.Get(DefaultBalance.Croissant), Is.EqualTo(12));
            Assert.That(store.State.Planned.Get(DefaultBalance.Latte), Is.EqualTo(60));
            Assert.That(store.State.Planned.Get(DefaultBalance.Tea), Is.EqualTo(0));
        }

        [Test]
        public void TheMenu_KeepsTheBalanceOrder_AndRemembersPlans()
        {
            var store = CreateStore();

            store.Dispatch(new SetOnMenu(DefaultBalance.Latte, false));
            Assert.That(store.State.Menu, Is.EqualTo(new[] { DefaultBalance.Tea, DefaultBalance.Croissant }));

            store.Dispatch(new SetOnMenu(DefaultBalance.Latte, true));
            Assert.That(store.State.Menu, Is.EqualTo(new[] { DefaultBalance.Latte, DefaultBalance.Tea, DefaultBalance.Croissant }));
            Assert.That(store.State.Planned.Get(DefaultBalance.Latte), Is.EqualTo(30));
        }

        [Test]
        public void UnknownIds_AreIgnored()
        {
            var store = CreateStore();
            var before = store.State;

            store.Dispatch(new SetOnMenu("scone", true));
            store.Dispatch(new SetMilk("goat"));
            store.Dispatch(new SetPlanned("scone", 3));
            store.Dispatch(new BuyUpgrade("rocket"));

            Assert.That(store.State, Is.SameAs(before));
        }

        [Test]
        public void OpeningForTheDay_TradesIt_AndCarriesTheCash()
        {
            var store = CreateStore();
            var expected = TradingDay.Run(Balance, store.State);

            store.Dispatch(new OpenForTheDay());

            var state = store.State;
            Assert.That(state.Phase, Is.EqualTo(ShopPhase.Review));
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(state.LastResult, Is.EqualTo(expected));
            Assert.That(state.CashPence, Is.EqualTo(expected.CashAfterPence));
            Assert.That(state.Pantry, Is.EqualTo(expected.PantryAfter));
            Assert.That(state.History.Single(), Is.EqualTo(expected.Summary));
        }

        [Test]
        public void ADay_IsTradedOnce_AndPrepWaitsForTheReview()
        {
            var store = CreateStore();
            store.Dispatch(new OpenForTheDay());
            var reviewing = store.State;

            store.Dispatch(new OpenForTheDay());
            store.Dispatch(new SetPlanned(DefaultBalance.Latte, 5));
            Assert.That(store.State, Is.SameAs(reviewing));

            store.Dispatch(new ContinueToPrep());
            store.Dispatch(new SetPlanned(DefaultBalance.Latte, 5));
            Assert.That(store.State.Phase, Is.EqualTo(ShopPhase.Prep));
            Assert.That(store.State.Planned.Get(DefaultBalance.Latte), Is.EqualTo(5));
            Assert.That(store.State.Planned.Get(DefaultBalance.Croissant), Is.EqualTo(10), "yesterday's plan stays");
        }

        [Test]
        public void AGame_ReplaysExactlyFromItsSeed()
        {
            CoffeeShopState Play(int seed)
            {
                var store = CreateStore(NewGame(seed));
                for (var day = 0; day < 5; day++)
                {
                    store.Dispatch(new OpenForTheDay());
                    store.Dispatch(new ContinueToPrep());
                }

                return store.State;
            }

            Assert.That(Play(11), Is.EqualTo(Play(11)));
            Assert.That(Play(11).CashPence, Is.Not.EqualTo(Play(12).CashPence));
        }

        [Test]
        public void StockThatCantBeAfforded_KeepsTheShopShut()
        {
            var store = CreateStore(NewGame() with { CashPence = -4900 });
            var before = store.State;

            store.Dispatch(new OpenForTheDay());

            Assert.That(store.State, Is.SameAs(before));
        }

        [Test]
        public void Upgrades_CantBeBoughtBeyondCashAboveTheReserve()
        {
            var store = CreateStore(NewGame() with { CashPence = 30000 });

            store.Dispatch(new SetReserve(10000));
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            Assert.That(store.State.Upgrades.Count, Is.Zero, "£300 less a £100 reserve leaves £200, short of £240");
            Assert.That(CoffeeShopReducer.CanBuy(Balance, store.State, DefaultBalance.BiggerDisplay), Is.False);

            store.Dispatch(new SetReserve(6000));
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            Assert.That(store.State.Upgrades.ContainsKey(DefaultBalance.BiggerDisplay), Is.True);
            Assert.That(store.State.CashPence, Is.EqualTo(6000));
        }

        [Test]
        public void AnUpgrade_IsBoughtOnce()
        {
            var store = Rich();

            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));

            Assert.That(store.State.CashPence, Is.EqualTo(100000 - 24000));
        }

        [Test]
        public void TheReserve_CantGoNegative()
        {
            var store = CreateStore();

            store.Dispatch(new SetReserve(-500));

            Assert.That(store.State.ReservePence, Is.Zero);
        }

        [Test]
        public void AnUpgradeBoughtAfterADay_WorksFromTheNextDay()
        {
            var store = Rich();
            store.Dispatch(new SetPlanned(DefaultBalance.Croissant, 26));
            store.Dispatch(new OpenForTheDay());
            Assert.That(store.State.LastResult.Item(DefaultBalance.Croissant).Stocked, Is.EqualTo(12), "day 1: the small case");

            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            Assert.That(store.State.LastResult.UpgradeImpacts, Is.Empty, "the day already traded is unchanged");
            Assert.That(store.State.Upgrades.Get(DefaultBalance.BiggerDisplay), Is.EqualTo(2));

            store.Dispatch(new ContinueToPrep());
            store.Dispatch(new SetPlanned(DefaultBalance.Croissant, 26));
            store.Dispatch(new OpenForTheDay());

            var result = store.State.LastResult;
            Assert.That(result.Day, Is.EqualTo(2));
            Assert.That(result.Item(DefaultBalance.Croissant).Stocked, Is.EqualTo(26));
            Assert.That(result.UpgradeImpacts.Single().UpgradeId, Is.EqualTo(DefaultBalance.BiggerDisplay));
        }

        [Test]
        public void UpgradeEarnings_AddUpDayByDay()
        {
            var store = Rich();
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            store.Dispatch(new SetPlanned(DefaultBalance.Croissant, 20));
            var total = 0;
            for (var day = 0; day < 3; day++)
            {
                store.Dispatch(new OpenForTheDay());
                total += store.State.LastResult.UpgradeImpacts.Single().ProfitPence;
                store.Dispatch(new ContinueToPrep());
            }

            Assert.That(store.State.UpgradeEarnings.Get(DefaultBalance.BiggerDisplay), Is.EqualTo(total));
        }

        [Test]
        public void ABadDayPastTheOverdraft_ClosesTheShop()
        {
            // Nothing to sell and the rent still due.
            var store = CreateStore(NewGame().WithMenu() with { CashPence = 0 });

            store.Dispatch(new OpenForTheDay());

            Assert.That(store.State.CashPence, Is.EqualTo(-7500));
            Assert.That(store.State.Phase, Is.EqualTo(ShopPhase.ClosedDown));

            var closed = store.State;
            store.Dispatch(new ContinueToPrep());
            store.Dispatch(new OpenForTheDay());
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            Assert.That(store.State, Is.SameAs(closed));
        }

        [Test]
        public void AReserve_LetsTheShopSurviveABadDay()
        {
            // The same empty day, with £30 kept back.
            var store = CreateStore(NewGame().WithMenu() with { CashPence = 3000 });

            store.Dispatch(new OpenForTheDay());

            Assert.That(store.State.CashPence, Is.EqualTo(-4500));
            Assert.That(store.State.Phase, Is.EqualTo(ShopPhase.Review));
        }

        [Test]
        public void History_KeepsTheLatestDays()
        {
            var store = CreateStore(NewGame() with { CashPence = 1000000 });
            for (var day = 0; day < Balance.HistoryDays + 3; day++)
            {
                store.Dispatch(new OpenForTheDay());
                store.Dispatch(new ContinueToPrep());
            }

            Assert.That(store.State.History, Has.Count.EqualTo(Balance.HistoryDays));
            Assert.That(store.State.History.Last().Day, Is.EqualTo(Balance.HistoryDays + 3));
        }

        [Test]
        public void StartingANewGame_ResetsEverything()
        {
            var store = Rich();
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            store.Dispatch(new OpenForTheDay());

            store.Dispatch(new StartNewGame(99));

            Assert.That(store.State, Is.EqualTo(CoffeeShopState.NewGame(Balance, 99)));
        }
    }
}
