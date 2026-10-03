using System;
using System.Linq;
using Townscape.State;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// The rules of the game. Pure, like every reducer: trading a day is a calculation from the state,
    /// so opening for the day is just another action.
    /// </summary>
    public static class CoffeeShopReducer
    {
        public static Reducer<CoffeeShopState> Create(CoffeeShopBalance balance) => (state, action) => Reduce(balance, state, action);

        public static CoffeeShopState Reduce(CoffeeShopBalance balance, CoffeeShopState state, IAction action)
        {
            if (action is StartNewGame newGame)
            {
                return CoffeeShopState.NewGame(balance, newGame.Seed);
            }

            CoffeeShopState next;
            switch (action)
            {
                case SetOnMenu menu when state.Phase == ShopPhase.Prep && balance.HasItem(menu.ItemId):
                    next = state with
                    {
                        Menu = ValueList<string>.From(balance.Items
                            .Select(i => i.Id)
                            .Where(id => id == menu.ItemId ? menu.OnMenu : state.Menu.Contains(id))),
                    };
                    break;
                case SetMilk milk when state.Phase == ShopPhase.Prep && balance.HasMilk(milk.MilkId):
                    next = state with { MilkId = milk.MilkId };
                    break;
                case SetPlanned planned when state.Phase == ShopPhase.Prep && balance.HasItem(planned.ItemId):
                    var count = Math.Max(0, Math.Min(planned.Count, PrepPlan.Room(balance, state, planned.ItemId)));
                    next = state with { Planned = state.Planned.With(planned.ItemId, count) };
                    break;
                case SetReserve reserve:
                    next = state with { ReservePence = Math.Max(0, reserve.Pence) };
                    break;
                case BuyUpgrade buy when CanBuy(balance, state, buy.UpgradeId):
                    next = state with
                    {
                        CashPence = state.CashPence - balance.Upgrade(buy.UpgradeId).CostPence,
                        Upgrades = state.Upgrades.With(buy.UpgradeId, state.Day),
                        UpgradeEarnings = state.UpgradeEarnings.With(buy.UpgradeId, 0),
                    };
                    break;
                case OpenForTheDay when state.Phase == ShopPhase.Prep && PrepPlan.For(balance, state).CanAfford:
                    next = Trade(balance, state);
                    break;
                default:
                    return state;
            }

            return next == state ? state : next;
        }

        /// <summary>Cash that upgrades may spend: everything above the reserve.</summary>
        public static int SpendableOnUpgrades(CoffeeShopState state) => Math.Max(0, state.CashPence - state.ReservePence);

        public static bool CanBuy(CoffeeShopBalance balance, CoffeeShopState state, string upgradeId) =>
            state.Phase != ShopPhase.ClosedDown
            && balance.HasUpgrade(upgradeId)
            && !state.Upgrades.ContainsKey(upgradeId)
            && balance.Upgrade(upgradeId).CostPence <= SpendableOnUpgrades(state);

        private static CoffeeShopState Trade(CoffeeShopBalance balance, CoffeeShopState state)
        {
            var result = TradingDay.Run(balance, state);
            var earnings = state.UpgradeEarnings;
            foreach (var impact in result.UpgradeImpacts)
            {
                earnings = earnings.With(impact.UpgradeId, earnings.Get(impact.UpgradeId) + impact.ProfitPence);
            }

            // Below the overdraft at closing, the bank calls it in.
            var closedDown = result.CashAfterPence < -balance.OverdraftPence;
            return state with
            {
                Day = state.Day + 1,
                Phase = closedDown ? ShopPhase.ClosedDown : ShopPhase.Prep,
                CashPence = result.CashAfterPence,
                Pantry = result.PantryAfter,
                UpgradeEarnings = earnings,
                History = state.History.Add(result.Summary).TakeLast(balance.HistoryDays),
                LastResult = result,
            };
        }
    }
}
