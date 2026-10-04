using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>A newspaper story: a headline, a sentence or two under it, and the story itself.</summary>
    public sealed record NewsStory(string Headline, string Standfirst, IReadOnlyList<string> Paragraphs);

    /// <summary>
    /// Writes the Fellside Herald's front page from the game. Each morning's lead story reports the
    /// day before, under a headline about whatever stood out most: a loss, a new upgrade, a crowd,
    /// a sell-out, a long queue, waste, a quiet day, or just steady trade.
    /// </summary>
    public static class NewsDesk
    {
        public const string PaperName = "The Fellside Herald";

        /// <summary>A day this much busier or quieter than usual makes the news.</summary>
        public const double NotableBusyness = 0.15;

        /// <summary>This many customers going without because something sold out makes the news.</summary>
        public const int NotableSellOut = 5;

        /// <summary>This many giving up on the queue makes the news.</summary>
        public const int NotableWalkOuts = 3;

        /// <summary>This many pastries in the bin makes the news.</summary>
        public const int NotableWaste = 4;

        public static NewsStory Lead(CoffeeShopBalance balance, CoffeeShopState state)
        {
            if (state.Phase == ShopPhase.ClosedDown)
            {
                return ClosedDown(balance, state);
            }

            if (state.LastResult != null)
            {
                return Report(balance, state, state.LastResult);
            }

            return state.History.Count > 0 ? Resumed(state, state.History[state.History.Count - 1]) : Opening(balance, state);
        }

        /// <summary>Tips printed on the first day's front page, written from the balance.</summary>
        public static IReadOnlyList<string> FirstDayNotes(CoffeeShopBalance balance)
        {
            var notes = new List<string>
            {
                "The footfall forecast says who usually comes in, and the classifieds say how many usually want each thing most. "
                + "Some days are busier than others.",
            };

            var keeps = balance.Ingredients.Where(i => !i.SpoilsOvernight).Select(i => i.Name.ToLowerInvariant()).ToList();
            var spoils = balance.Ingredients.Where(i => i.SpoilsOvernight).Select(i => i.Name.ToLowerInvariant()).ToList();
            var pastries = balance.Items.Where(i => i.Kind == ItemKind.Pastry).Select(i => i.PluralName.ToLowerInvariant()).ToList();
            if (keeps.Count > 0 || spoils.Count > 0 || pastries.Count > 0)
            {
                var parts = new List<string>();
                if (keeps.Count > 0)
                {
                    parts.Add($"{Capitalise(List(keeps))} {(keeps.Count == 1 ? "keeps" : "keep")} for another day");
                }

                if (spoils.Count > 0)
                {
                    parts.Add($"{List(spoils)} is poured away at closing");
                }

                if (pastries.Count > 0)
                {
                    parts.Add($"unsold {List(pastries)} go in the bin");
                }

                notes.Add(Capitalise(string.Join("; ", parts)) + ".");
            }

            var alternative = balance.MilkOptions.FirstOrDefault(m => m.DairyFree);
            if (alternative != null)
            {
                var notDairyFree = balance.Items.Where(i => i.Kind == ItemKind.Pastry && !i.DairyFree).Select(i => i.PluralName.ToLowerInvariant()).ToList();
                var pastryNote = notDairyFree.Count > 0 ? $", and they can't have {List(notDairyFree)}" : string.Empty;
                notes.Add($"Customers who can't have dairy need {alternative.Name.ToLowerInvariant()} in a milky drink{pastryNote}. "
                    + $"Not everyone likes {alternative.Name.ToLowerInvariant()}, though.");
            }

            notes.Add("Cash kept back with the building society can't be spent on upgrades, so one bad day needn't close the shop.");
            return notes;
        }

        /// <summary>"13 sold out, 3 can't have dairy, 1 over budget".</summary>
        public static string Reasons(Misses missed, string milkName)
        {
            var parts = new List<string>();
            void Add(int count, string reason)
            {
                if (count > 0)
                {
                    parts.Add($"{count} {reason}");
                }
            }

            Add(missed.SoldOut, "sold out");
            Add(missed.NotOnMenu, "not on the menu");
            Add(missed.Dietary, "can't have dairy");
            Add(missed.MilkChoice, $"didn't want {milkName}");
            Add(missed.TooDear, "over budget");
            Add(missed.Queue, "gave up queuing");
            return string.Join(", ", parts);
        }

        /// <summary>How a day's crowd compared with usual, as words: "busy", "quiet" or "usual".</summary>
        public static string Busyness(int customers, double usual)
        {
            var ratio = usual > 0 ? customers / usual : 1.0;
            return ratio < 1.0 - NotableBusyness ? "quiet" : ratio > 1.0 + NotableBusyness ? "busy" : "usual";
        }

        public static string Name(ItemDef item, int count) => (count == 1 ? item.Name : item.PluralName).ToLowerInvariant();

        private static NewsStory Report(CoffeeShopBalance balance, CoffeeShopState state, DayResult result)
        {
            var usual = Forecast.For(balance, balance.Site(result.SiteId)).Customers;
            var busyness = Busyness(result.Customers, usual);
            var paragraphs = new List<string> { ItemsParagraph(balance, result), CustomersParagraph(balance, result) };

            var newUpgrade = result.UpgradeImpacts.FirstOrDefault(i => state.Upgrades.Get(i.UpgradeId, -1) == result.Day);
            if (newUpgrade != null)
            {
                var name = balance.Upgrade(newUpgrade.UpgradeId).Name.ToLowerInvariant();
                paragraphs.Add(
                    $"It was the first day for the shop's new {name}. Against the same customers without it, it made a difference of "
                    + $"{Money.Signed(newUpgrade.ProfitPence)}: {Signed(newUpgrade.Sold)} sold and {Signed(newUpgrade.Wasted)} wasted.");
            }

            if (result.WastePence > 0)
            {
                var milk = result.MilkPouredAway > 0 ? $"{result.MilkPouredAway} portions of milk were poured away at closing. " : string.Empty;
                paragraphs.Add($"{milk}Waste cost the shop {Money.Format(result.WastePence)} in all.");
            }

            var standfirst = $"{result.Customers} customers called in on day {result.Day} and spent {Money.Format(result.RevenuePence)}. "
                + $"After stock, cups, rent and upkeep, the shop {(result.ProfitPence >= 0 ? $"made {Money.Format(result.ProfitPence)}" : $"lost {Money.Format(-result.ProfitPence)}")}."
                + (busyness == "busy" ? $" Usually about {usual:0} come in." : busyness == "quiet" ? $" Usually it's about {usual:0}." : string.Empty);

            return new NewsStory(Headline(balance, result, busyness, newUpgrade), standfirst, paragraphs);
        }

        private static string Headline(CoffeeShopBalance balance, DayResult result, string busyness, UpgradeImpact newUpgrade)
        {
            if (result.ProfitPence < 0)
            {
                var loss = Money.Format(-result.ProfitPence);
                return busyness == "quiet" ? $"Quiet day leaves Fellside Coffee {loss} down" : $"Fellside Coffee loses {loss}";
            }

            if (newUpgrade != null)
            {
                var name = balance.Upgrade(newUpgrade.UpgradeId).Name.ToLowerInvariant();
                return newUpgrade.ProfitPence > 0 ? $"New {name} earns its keep" : $"New {name} yet to pay its way";
            }

            if (busyness == "busy")
            {
                return "Crowds pack Fellside Coffee";
            }

            var soldOut = result.Items.OrderByDescending(i => i.Missed.SoldOut).FirstOrDefault();
            if (soldOut != null && soldOut.Missed.SoldOut >= NotableSellOut)
            {
                return $"{balance.Item(soldOut.ItemId).PluralName} sell out at Fellside Coffee";
            }

            var queue = result.Slots.OrderByDescending(s => s.WalkedOut).FirstOrDefault();
            if (queue != null && queue.WalkedOut >= NotableWalkOuts)
            {
                return $"{queue.WalkedOut} give up on the {queue.Name.ToLowerInvariant()} queue";
            }

            var wasted = result.Items.OrderByDescending(i => i.Wasted).FirstOrDefault();
            if (wasted != null && wasted.Wasted >= NotableWaste)
            {
                return $"{wasted.Wasted} {Name(balance.Item(wasted.ItemId), wasted.Wasted)} go to waste";
            }

            return busyness == "quiet" ? "Quiet day on the High Street" : "Steady trade at Fellside Coffee";
        }

        // "31 lattes were sold; 3 customers who wanted one went without (1 can't have dairy, 2 gave up queuing)."
        private static string ItemsParagraph(CoffeeShopBalance balance, DayResult result)
        {
            var milkName = balance.HasMilk(result.MilkId) ? balance.Milk(result.MilkId).Name.ToLowerInvariant() : "the milk";
            var sentences = new List<string>();
            foreach (var item in result.Items)
            {
                var def = balance.Item(item.ItemId);
                string sentence;
                if (def.Kind == ItemKind.Pastry)
                {
                    sentence = item.Stocked == 0 ? $"There were no {Name(def, 2)}"
                        : item.Wasted == 0 && item.Sold == item.Stocked ? $"All {item.Stocked} {Name(def, item.Stocked)} sold"
                        : $"{item.Sold} of {item.Stocked} {Name(def, item.Stocked)} sold";
                    if (item.Wasted > 0)
                    {
                        sentence += $", and {item.Wasted} went in the bin";
                    }
                }
                else
                {
                    sentence = item.Sold == 0 ? $"No {Name(def, 2)} were sold" : $"{item.Sold} {Name(def, item.Sold)} {(item.Sold == 1 ? "was" : "were")} sold";
                }

                var missed = item.Missed.Total;
                if (missed > 0)
                {
                    sentence += $"; {missed} {(missed == 1 ? "customer" : "customers")} who wanted one went without ({Reasons(item.Missed, milkName)})";
                }

                sentences.Add(Capitalise(sentence) + ".");
            }

            return string.Join(" ", sentences);
        }

        // "Commuters got 77% of what they came for, though 2 gave up on the queue; locals got 77%."
        private static string CustomersParagraph(CoffeeShopBalance balance, DayResult result)
        {
            var parts = result.Segments.Select((segment, index) =>
            {
                var name = balance.Segment(segment.SegmentId).Name;
                var part = $"{(index == 0 ? name : name.ToLowerInvariant())} got {Percent(segment.ServedShare)}{(index == 0 ? " of what they came for" : string.Empty)}";
                return segment.WalkedOut > 0 ? part + $", though {segment.WalkedOut} gave up on the queue" : part;
            });
            return string.Join("; ", parts) + ".";
        }

        private static NewsStory Opening(CoffeeShopBalance balance, CoffeeShopState state)
        {
            var menu = state.Menu.Select(balance.Item).Select(item => $"{item.PluralName.ToLowerInvariant()} at {Money.Format(item.PricePence)}").ToList();
            var menuText = menu.Count == 0 ? "nothing yet"
                : menu.Count == 1 ? menu[0]
                : string.Join(", ", menu.Take(menu.Count - 1)) + " and " + menu[menu.Count - 1];
            var rent = balance.Site(state.SiteId).DailyCostPence;
            return new NewsStory(
                "New owner takes on Fellside Coffee",
                $"The café on the High Street opens under new management this morning, with {Money.Format(state.CashPence)} in the till.",
                new[]
                {
                    $"On the menu: {menuText}.",
                    "The new owner orders the day's stock from the suppliers in our classifieds, then opens the doors. "
                    + "Tomorrow's Herald will report on how it went.",
                    $"Rent and wages come to {Money.Format(rent)} a day. The bank allows an overdraft of {Money.Format(balance.OverdraftPence)}; "
                    + "past that, it will close the shop.",
                });
        }

        private static NewsStory Resumed(CoffeeShopState state, DaySummary yesterday) => new NewsStory(
            $"Fellside Coffee opens for day {state.Day}",
            $"On day {yesterday.Day} the shop sold {yesterday.Sold}, wasted {yesterday.Wasted} and missed {yesterday.Missed}, and "
            + $"{(yesterday.ProfitPence >= 0 ? $"made {Money.Format(yesterday.ProfitPence)}" : $"lost {Money.Format(-yesterday.ProfitPence)}")}.",
            new string[0]);

        private static NewsStory ClosedDown(CoffeeShopBalance balance, CoffeeShopState state)
        {
            var days = state.Day - 1;
            var paragraphs = new List<string>();
            if (state.LastResult != null)
            {
                paragraphs.Add(ItemsParagraph(balance, state.LastResult));
            }

            paragraphs.Add("Keeping some cash back for a bad day helps a shop weather the quiet spells. "
                + "Anyone wanting to start afresh will find the premises advertised in our classifieds.");
            return new NewsStory(
                "Bank closes Fellside Coffee",
                $"After {days} {(days == 1 ? "day" : "days")} of trading, the shop's cash fell to {Money.Format(state.CashPence)}, past its {Money.Format(balance.OverdraftPence)} overdraft.",
                paragraphs);
        }

        private static string List(IReadOnlyList<string> items) =>
            items.Count <= 1 ? string.Join(string.Empty, items) : string.Join(", ", items.Take(items.Count - 1)) + " and " + items[items.Count - 1];

        private static string Signed(int change) => change > 0 ? "+" + change.ToString(CultureInfo.InvariantCulture) : change.ToString(CultureInfo.InvariantCulture);

        private static string Percent(double share) => ((int)System.Math.Round(share * 100.0, System.MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + "%";

        private static string Capitalise(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
