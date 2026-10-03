using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Townscape.CoffeeShop;
using Townscape.State;

namespace Townscape.CoffeeSim
{
    /// <summary>
    /// Plays many seeded games with a few simple players and reports averages, so a balance change
    /// can be judged in seconds rather than by playing for an evening.
    /// <code>dotnet run -c Release --project tools/coffee-sim -- --games 200 --days 30</code>
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var games = Option(args, "--games", 200);
            var days = Option(args, "--days", 30);
            var balance = DefaultBalance.Create();
            var problems = balance.Validate();
            if (problems.Count > 0)
            {
                Console.Error.WriteLine("The balance has problems:\n  " + string.Join("\n  ", problems));
                return 1;
            }

            var forecast = Forecast.For(balance, balance.Site(balance.StartingSiteId));
            Console.WriteLine($"Fellside Coffee: {games} games of {days} days per player\n");
            Console.WriteLine($"Forecast: {forecast.Customers:0} customers a day");
            foreach (var slot in forecast.Slots)
            {
                Console.WriteLine($"  {slot.Name,-14} {slot.Customers,5:0.0}  " + string.Join(", ", slot.Segments.Select(s => $"{s.Id} {s.Weight:0.0}")));
            }

            Console.WriteLine("  first choices: " + string.Join(", ", forecast.FirstChoices.Select(f => $"{f.Id} {f.Weight:0.0}")));
            Console.WriteLine();

            var players = new[]
            {
                new Player("Starter plan, never upgrades", Starter, buys: false),
                new Player("Forecast plan, dairy, never upgrades", p => Forecaster(p, DefaultBalance.DairyMilk), buys: false),
                new Player("Forecast plan, oat, never upgrades", p => Forecaster(p, DefaultBalance.OatMilk), buys: false),
                new Player("Forecast plan, dairy, buys the case, bakes 70% of forecast", p => Forecaster(p, DefaultBalance.DairyMilk, pastryShare: 0.7), buys: true),
                new Player("Forecast plan, dairy, buys the case", p => Forecaster(p, DefaultBalance.DairyMilk), buys: true),
                new Player("Forecast plan, dairy, buys the case, bakes 50% of forecast", p => Forecaster(p, DefaultBalance.DairyMilk, pastryShare: 0.5), buys: true),
                new Player("Forecast plan, dairy, buys the case with no reserve", p => Forecaster(p, DefaultBalance.DairyMilk), buys: true, reserve: false),
            };

            foreach (var player in players)
            {
                Report(balance, player, games, days);
            }

            return 0;
        }

        private sealed class Player
        {
            public Player(string name, Func<Prep, IEnumerable<IAction>> plan, bool buys, bool reserve = true)
            {
                Name = name;
                Plan = plan;
                Buys = buys;
                Reserve = reserve;
            }

            public string Name { get; }

            public Func<Prep, IEnumerable<IAction>> Plan { get; }

            public bool Buys { get; }

            public bool Reserve { get; }
        }

        private sealed record Prep(CoffeeShopBalance Balance, CoffeeShopState State, Forecast Forecast);

        private static IEnumerable<IAction> Starter(Prep prep) => Enumerable.Empty<IAction>();

        // Stocks drinks for everyone expected to want them most (plus a little for second
        // choices), and pastries for the expected demand, up to the case.
        private static IEnumerable<IAction> Forecaster(Prep prep, string milk, double pastryShare = 0.9)
        {
            yield return new SetMilk(milk);
            foreach (var item in prep.Balance.Items)
            {
                var expected = prep.Forecast.FirstChoicesOf(item.Id);
                var count = item.Kind == ItemKind.Pastry
                    ? (int)Math.Round(expected * pastryShare)
                    : (int)Math.Ceiling(expected * 1.15);
                yield return new SetPlanned(item.Id, count);
            }
        }

        private static void Report(CoffeeShopBalance balance, Player player, int games, int days)
        {
            var reducer = CoffeeShopReducer.Create(balance);
            var forecast = Forecast.For(balance, balance.Site(balance.StartingSiteId));
            var results = new List<DayResult>();
            var finalCash = new List<int>();
            var boughtOn = new List<int>();
            var closed = 0;

            for (var game = 0; game < games; game++)
            {
                var store = new Store<CoffeeShopState>(reducer, CoffeeShopState.NewGame(balance, 1000 + game));
                // Keep a day's fixed costs back so a bad day can be survived.
                store.Dispatch(new SetReserve(player.Reserve ? balance.Site(balance.StartingSiteId).DailyCostPence : 0));
                for (var day = 0; day < days && store.State.Phase != ShopPhase.ClosedDown; day++)
                {
                    foreach (var action in player.Plan(new Prep(balance, store.State, forecast)))
                    {
                        store.Dispatch(action);
                    }

                    store.Dispatch(new OpenForTheDay());
                    results.Add(store.State.LastResult);
                    if (player.Buys && !store.State.Upgrades.ContainsKey(DefaultBalance.BiggerDisplay)
                        && CoffeeShopReducer.CanBuy(balance, store.State, DefaultBalance.BiggerDisplay))
                    {
                        store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
                        boughtOn.Add(store.State.Day);
                    }

                    store.Dispatch(new ContinueToPrep());
                }

                closed += store.State.Phase == ShopPhase.ClosedDown ? 1 : 0;
                finalCash.Add(store.State.CashPence);
            }

            double Mean(Func<DayResult, double> of) => results.Average(of);
            Console.WriteLine($"== {player.Name}");
            Console.WriteLine($"  per day: customers {Mean(r => r.Customers):0.0}, served {Mean(r => r.Served):0.0}, walked out {Mean(r => r.WalkedOut):0.0}");
            Console.WriteLine($"  money:   revenue {Pounds(Mean(r => r.RevenuePence))}, stock {Pounds(Mean(r => r.StockPence))}, supplies {Pounds(Mean(r => r.SuppliesPence))}, "
                + $"site {Pounds(Mean(r => r.SiteCostPence))}, upkeep {Pounds(Mean(r => r.RunningCostPence))}, waste {Pounds(Mean(r => r.WastePence))}");
            Console.WriteLine($"  profit:  {Pounds(Mean(r => r.ProfitPence))} a day (worst {Pounds(results.Min(r => r.ProfitPence))}, best {Pounds(results.Max(r => r.ProfitPence))}), "
                + $"losing days {results.Count(r => r.ProfitPence < 0) * 100.0 / results.Count:0}%");
            foreach (var item in balance.Items)
            {
                var outcomes = results.Select(r => r.Items.FirstOrDefault(i => i.ItemId == item.Id)).Where(o => o != null).ToList();
                var reasons = Enum.GetValues(typeof(MissReason)).Cast<MissReason>()
                    .Select(reason => (reason, mean: outcomes.Average(o => o.Missed.Of(reason))))
                    .Where(x => x.mean >= 0.05)
                    .Select(x => $"{x.reason} {x.mean:0.0}");
                Console.WriteLine($"  {item.Id,-10} stocked {outcomes.Average(o => o.Stocked),5:0.0}  wanted {outcomes.Average(o => o.Wanted),5:0.0}  sold {outcomes.Average(o => o.Sold),5:0.0}"
                    + $"  wasted {outcomes.Average(o => o.Wasted),4:0.0}  missed: {string.Join(", ", reasons)}");
            }

            foreach (var segment in balance.Segments)
            {
                var outcomes = results.SelectMany(r => r.Segments).Where(s => s.SegmentId == segment.Id).ToList();
                Console.WriteLine($"  {segment.Id,-10} served {outcomes.Average(s => s.ServedShare) * 100,3:0}% of wants, walked out {outcomes.Average(s => s.WalkedOut):0.0} a day");
            }

            var slotNames = balance.Site(balance.StartingSiteId).Slots.Select(s => s.Name).ToList();
            Console.WriteLine("  queue:   " + string.Join(", ", slotNames.Select((name, i) =>
                $"{name} walk-outs {results.Average(r => r.Slots[i].WalkedOut):0.0}, longest wait {results.Average(r => r.Slots[i].LongestWaitSeconds):0}s")));

            var impacts = results.SelectMany(r => r.UpgradeImpacts).ToList();
            if (boughtOn.Count > 0)
            {
                var cost = balance.Upgrade(DefaultBalance.BiggerDisplay).CostPence;
                var perDay = impacts.Average(i => i.ProfitPence);
                Console.WriteLine($"  case:    bought by {boughtOn.Count * 100 / games}% of games, median day {Median(boughtOn)}; "
                    + $"worth {Pounds(perDay)} a day (+{impacts.Average(i => i.Sold):0.0} sold, +{impacts.Average(i => i.Wasted):0.0} wasted), "
                    + (perDay > 0 ? $"pays back in {cost / perDay:0} days" : "never pays back"));
            }

            Console.WriteLine($"  after {days} days: cash median {Pounds(Median(finalCash))} (10th percentile {Pounds(Percentile(finalCash, 0.1))}, "
                + $"90th {Pounds(Percentile(finalCash, 0.9))}); closed down {closed * 100.0 / games:0}%\n");
        }

        private static string Pounds(double pence) => (pence < 0 ? "-£" : "£") + Math.Abs(pence / 100.0).ToString("0.00", CultureInfo.InvariantCulture);

        private static int Median(List<int> values) => Percentile(values, 0.5);

        private static int Percentile(List<int> values, double p)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[Math.Min(sorted.Count - 1, (int)(p * sorted.Count))];
        }

        private static int Option(string[] args, string name, int fallback)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var value) ? value : fallback;
        }
    }
}
