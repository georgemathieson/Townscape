using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// Runs one trading day, start to finish, in one go. Pure: the same balance and state always
    /// give the same result, because the customers come from the game's seed and the day number.
    /// </summary>
    public static class TradingDay
    {
        /// <summary>Trades <paramref name="state"/>'s day and works out what each active upgrade was worth.</summary>
        public static DayResult Run(CoffeeShopBalance balance, CoffeeShopState state)
        {
            var customers = CustomersFor(balance, state);
            var result = Trade(balance, state, customers);

            // Replay the same customers with the same plan, minus one upgrade at a time.
            var impacts = PrepPlan.ActiveUpgrades(balance, state)
                .Select(upgrade =>
                {
                    var without = Trade(balance, state with { Upgrades = state.Upgrades.Without(upgrade.Id) }, customers);
                    return new UpgradeImpact(
                        upgrade.Id,
                        result.ProfitPence - without.ProfitPence,
                        result.Sold - without.Sold,
                        result.Wasted - without.Wasted,
                        result.Missed - without.Missed);
                })
                .ToList();

            return result with { UpgradeImpacts = ValueList<UpgradeImpact>.From(impacts) };
        }

        public static IReadOnlyList<Customer> CustomersFor(CoffeeShopBalance balance, CoffeeShopState state) =>
            CustomerStream.Roll(balance, balance.Site(state.SiteId), SeededRandom.DaySeed(state.Seed, state.Day));

        /// <summary>Trades one day with the given customers, without working out upgrade impacts.</summary>
        public static DayResult Trade(CoffeeShopBalance balance, CoffeeShopState state, IReadOnlyList<Customer> customers)
        {
            var plan = PrepPlan.For(balance, state);
            var site = balance.Site(state.SiteId);
            var milk = balance.MilkOptions.Count > 0 ? balance.Milk(state.MilkId) : null;
            var day = new Day(balance, state, plan, milk);

            var baristas = new double[Math.Max(1, site.Slots.Max(s => s.Baristas))];
            var slots = site.Slots.Select(s => new SlotTally(s.Name)).ToArray();

            foreach (var customer in customers)
            {
                var segment = day.Segment(customer.SegmentId);
                var slot = slots[customer.SlotIndex];
                segment.Customers++;
                slot.Customers++;

                // First come, first served, by whichever barista on shift is free soonest.
                var working = site.Slots[customer.SlotIndex].Baristas;
                var barista = 0;
                for (var i = 1; i < working; i++)
                {
                    if (baristas[i] < baristas[barista])
                    {
                        barista = i;
                    }
                }

                var start = Math.Max(customer.ArrivalSeconds, baristas[barista]);
                var wait = start - customer.ArrivalSeconds;
                if (wait > customer.PatienceSeconds)
                {
                    segment.WalkedOut++;
                    slot.WalkedOut++;
                    day.Miss(customer, customer.Drinks, MissReason.Queue);
                    day.Miss(customer, customer.Pastries, MissReason.Queue);
                    continue;
                }

                slot.LongestWait = Math.Max(slot.LongestWait, wait);

                var budget = customer.BudgetPence;
                var seconds = day.Serve(customer, customer.Drinks, ref budget) + day.Serve(customer, customer.Pastries, ref budget);
                if (budget < customer.BudgetPence)
                {
                    day.Served++;
                    seconds += balance.PaymentSeconds;
                }

                baristas[barista] = start + seconds;
            }

            return day.Close(slots.Select(s => new SlotOutcome(s.Name, s.Customers, s.WalkedOut, (int)Math.Ceiling(s.LongestWait))));
        }

        private sealed class SlotTally
        {
            public SlotTally(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public int Customers { get; set; }

            public int WalkedOut { get; set; }

            public double LongestWait { get; set; }
        }

        private sealed class ItemTally
        {
            public int Wanted { get; set; }

            public int Sold { get; set; }

            public int SoldAsSecondChoice { get; set; }

            public Misses Missed { get; set; } = Misses.None;
        }

        private sealed class SegmentTally
        {
            public int Customers { get; set; }

            public int WalkedOut { get; set; }

            public int Wants { get; set; }

            public int GotFirstChoice { get; set; }

            public int GotSecondChoice { get; set; }

            public Misses Missed { get; set; } = Misses.None;

            public int Revenue { get; set; }
        }

        /// <summary>The shop while it trades: stock on hand and running totals.</summary>
        private sealed class Day
        {
            private readonly CoffeeShopBalance _balance;
            private readonly CoffeeShopState _state;
            private readonly PrepPlan _plan;
            private readonly MilkOption _milk;
            private readonly Dictionary<string, int> _stock;
            private readonly HashSet<string> _menu;
            private readonly Dictionary<string, ItemTally> _items = new Dictionary<string, ItemTally>();
            private readonly Dictionary<string, SegmentTally> _segments = new Dictionary<string, SegmentTally>();
            private int _revenue;
            private int _drinksSold;

            public Day(CoffeeShopBalance balance, CoffeeShopState state, PrepPlan plan, MilkOption milk)
            {
                _balance = balance;
                _state = state;
                _plan = plan;
                _milk = milk;
                _stock = new Dictionary<string, int>(plan.OpeningStock);
                _menu = new HashSet<string>(state.Menu);
                foreach (var item in balance.Items)
                {
                    _items[item.Id] = new ItemTally();
                }
            }

            public int Served { get; set; }

            public SegmentTally Segment(string id)
            {
                if (!_segments.TryGetValue(id, out var tally))
                {
                    tally = new SegmentTally();
                    _segments[id] = tally;
                }

                return tally;
            }

            /// <summary>Records a want that went unmet, against the customer's first choice.</summary>
            public void Miss(Customer customer, IReadOnlyList<string> choices, MissReason reason)
            {
                if (choices.Count == 0)
                {
                    return;
                }

                var segment = Segment(customer.SegmentId);
                var item = _items[choices[0]];
                segment.Wants++;
                segment.Missed = segment.Missed.Plus(reason);
                item.Wanted++;
                item.Missed = item.Missed.Plus(reason);
            }

            /// <summary>
            /// Sells the customer the first of <paramref name="choices"/> the shop can give them (only
            /// the first, unless they'll settle for something else). Returns the seconds it takes.
            /// </summary>
            public int Serve(Customer customer, IReadOnlyList<string> choices, ref int budget)
            {
                if (choices.Count == 0)
                {
                    return 0;
                }

                var firstReason = MissReason.NotOnMenu;
                for (var rank = 0; rank < choices.Count; rank++)
                {
                    if (rank > 0 && !customer.TakesSecondChoice)
                    {
                        break;
                    }

                    var item = _balance.Item(choices[rank]);
                    var reason = Check(customer, item, budget);
                    if (reason == null)
                    {
                        Sell(customer, item, rank, choices[0]);
                        budget -= item.PricePence;
                        return item.ServeSeconds;
                    }

                    if (rank == 0)
                    {
                        firstReason = reason.Value;
                    }
                }

                Miss(customer, choices, firstReason);
                return 0;
            }

            public DayResult Close(IEnumerable<SlotOutcome> slots)
            {
                var wastePence = 0;
                var items = new List<ItemOutcome>();
                foreach (var item in _balance.Items)
                {
                    var tally = _items[item.Id];
                    var line = _plan.Line(item.Id);
                    var wasted = item.Kind == ItemKind.Pastry && _stock.TryGetValue(item.Id, out var left) ? left : 0;
                    wastePence += wasted * item.UnitCostPence;
                    if (line != null || tally.Wanted > 0)
                    {
                        items.Add(new ItemOutcome(item.Id, line?.Makeable ?? 0, tally.Wanted, tally.Sold, tally.SoldAsSecondChoice, wasted, tally.Missed));
                    }
                }

                // Milk is poured away; everything else keeps for tomorrow.
                var pantry = ValueMap<int>.Empty;
                var milkPouredAway = 0;
                foreach (var ingredient in _balance.Ingredients)
                {
                    _stock.TryGetValue(ingredient.Id, out var left);
                    if (ingredient.SpoilsOvernight)
                    {
                        if (ingredient.IsMilk)
                        {
                            milkPouredAway = left;
                        }

                        wastePence += left * (ingredient.IsMilk && _milk != null ? _milk.CostPerPortionPence : ingredient.CostPerPortionPence);
                    }
                    else if (left > 0)
                    {
                        pantry = pantry.With(ingredient.Id, left);
                    }
                }

                var segments = _balance.Segments
                    .Where(s => _segments.ContainsKey(s.Id))
                    .Select(s =>
                    {
                        var t = _segments[s.Id];
                        return new SegmentOutcome(s.Id, t.Customers, t.WalkedOut, t.Wants, t.GotFirstChoice, t.GotSecondChoice, t.Missed, t.Revenue);
                    });

                var slotList = ValueList<SlotOutcome>.From(slots);
                return new DayResult
                {
                    Day = _state.Day,
                    SiteId = _state.SiteId,
                    MilkId = _state.MilkId,
                    Customers = slotList.Sum(s => s.Customers),
                    Served = Served,
                    WalkedOut = slotList.Sum(s => s.WalkedOut),
                    Items = ValueList<ItemOutcome>.From(items),
                    Segments = ValueList<SegmentOutcome>.From(segments),
                    Slots = slotList,
                    RevenuePence = _revenue,
                    StockPence = _plan.StockCostPence,
                    SuppliesPence = _drinksSold * _balance.SuppliesPerDrinkPence,
                    SiteCostPence = _balance.Site(_state.SiteId).DailyCostPence,
                    RunningCostPence = _plan.FixedCostPence - _balance.Site(_state.SiteId).DailyCostPence,
                    WastePence = wastePence,
                    MilkPouredAway = milkPouredAway,
                    CashBeforePence = _state.CashPence,
                    PantryAfter = pantry,
                };
            }

            private MissReason? Check(Customer customer, ItemDef item, int budget)
            {
                if (!_menu.Contains(item.Id))
                {
                    return MissReason.NotOnMenu;
                }

                var milky = item.Kind == ItemKind.Drink && _balance.IsMilky(item);
                if (customer.DairyFree && (milky ? _milk == null || !_milk.DairyFree : !IsDairyFree(item)))
                {
                    return MissReason.Dietary;
                }

                if (milky && !customer.DairyFree && _milk != null && customer.MilkFussiness >= _milk.Appeal)
                {
                    return MissReason.MilkChoice;
                }

                if (item.PricePence > budget)
                {
                    return MissReason.TooDear;
                }

                return InStock(item) ? (MissReason?)null : MissReason.SoldOut;
            }

            private static bool IsDairyFree(ItemDef item) => item.Kind == ItemKind.Drink || item.DairyFree;

            private bool InStock(ItemDef item)
            {
                if (item.Kind == ItemKind.Pastry)
                {
                    return _stock.TryGetValue(item.Id, out var count) && count > 0;
                }

                return item.Recipe.All(part => _stock.TryGetValue(part.IngredientId, out var have) && have >= part.Portions);
            }

            private void Sell(Customer customer, ItemDef item, int rank, string firstChoice)
            {
                if (item.Kind == ItemKind.Pastry)
                {
                    _stock[item.Id]--;
                }
                else
                {
                    foreach (var part in item.Recipe)
                    {
                        _stock[part.IngredientId] -= part.Portions;
                    }

                    _drinksSold++;
                }

                _revenue += item.PricePence;
                var tally = _items[item.Id];
                tally.Sold++;
                _items[firstChoice].Wanted++;

                var segment = Segment(customer.SegmentId);
                segment.Wants++;
                segment.Revenue += item.PricePence;
                if (rank == 0)
                {
                    segment.GotFirstChoice++;
                }
                else
                {
                    segment.GotSecondChoice++;
                    tally.SoldAsSecondChoice++;
                }
            }
        }
    }
}
