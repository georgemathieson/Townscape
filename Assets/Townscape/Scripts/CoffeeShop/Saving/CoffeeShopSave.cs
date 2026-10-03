using System;
using System.Collections.Generic;
using System.Linq;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// A game of Fellside Coffee as JSON, for the save file. Everything the player has built up is
    /// saved; the last day's full results are not (they are only for the review), so a game saved
    /// mid-review carries on at prep.
    /// </summary>
    /// <remarks>
    /// Reading is forgiving, like the town's settings: a missing or unreadable field falls back to
    /// a new game's value, and anything the balance no longer has (an item, an upgrade) is dropped.
    /// Only text that isn't a Fellside Coffee save at all, or comes from a newer version, is refused.
    /// </remarks>
    public static class CoffeeShopSave
    {
        public const string Format = "fellside-coffee";
        public const int Version = 1;

        public static string Write(CoffeeShopState state)
        {
            var save = new JsonObject
            {
                ["format"] = Format,
                ["version"] = Version,
                ["seed"] = state.Seed,
                ["day"] = state.Day,
                ["closedDown"] = state.Phase == ShopPhase.ClosedDown,
                ["cash"] = state.CashPence,
                ["reserve"] = state.ReservePence,
                ["site"] = state.SiteId,
                ["milk"] = state.MilkId,
                ["menu"] = state.Menu.Cast<object>().ToList(),
                ["planned"] = Counts(state.Planned),
                ["pantry"] = Counts(state.Pantry),
                ["upgrades"] = Counts(state.Upgrades),
                ["upgradeEarnings"] = Counts(state.UpgradeEarnings),
                ["history"] = state.History.Select(day => (object)new JsonObject
                {
                    ["day"] = day.Day,
                    ["customers"] = day.Customers,
                    ["sold"] = day.Sold,
                    ["wasted"] = day.Wasted,
                    ["missed"] = day.Missed,
                    ["revenue"] = day.RevenuePence,
                    ["costs"] = day.CostsPence,
                    ["cashAfter"] = day.CashAfterPence,
                }).ToList(),
            };

            return Json.Write(save);
        }

        /// <summary>
        /// The game saved in <paramref name="text"/>, or false with the reason if it can't be read
        /// (not JSON, not a Fellside Coffee save, or from a newer version of the game).
        /// </summary>
        public static bool TryRead(string text, CoffeeShopBalance balance, out CoffeeShopState state, out string problem)
        {
            state = null;
            JsonObject save;
            try
            {
                save = Json.Read(text) as JsonObject;
            }
            catch (FormatException e)
            {
                problem = "The save isn't valid JSON: " + e.Message;
                return false;
            }

            if (save == null || save["format"] as string != Format)
            {
                problem = "The file isn't a Fellside Coffee save.";
                return false;
            }

            var version = Int(save, "version", 0);
            if (version < 1 || version > Version)
            {
                problem = $"The save is version {version}; this game reads up to version {Version}.";
                return false;
            }

            var defaults = CoffeeShopState.NewGame(balance, Int(save, "seed", 0));
            var siteId = save["site"] as string;
            var milkId = save["milk"] as string;
            var menu = (save["menu"] as List<object> ?? new List<object>()).OfType<string>().ToList();

            var loaded = defaults with
            {
                Day = Math.Max(1, Int(save, "day", defaults.Day)),
                Phase = Flag(save, "closedDown") ? ShopPhase.ClosedDown : ShopPhase.Prep,
                CashPence = Int(save, "cash", defaults.CashPence),
                ReservePence = Math.Max(0, Int(save, "reserve", defaults.ReservePence)),
                SiteId = siteId != null && balance.HasSite(siteId) ? siteId : defaults.SiteId,
                MilkId = milkId != null && balance.HasMilk(milkId) ? milkId : defaults.MilkId,
                Menu = save.Has("menu") ? ValueList<string>.From(balance.Items.Select(i => i.Id).Where(menu.Contains)) : defaults.Menu,
                Planned = save.Has("planned") ? Counts(save, "planned", balance.HasItem) : defaults.Planned,
                Pantry = Counts(save, "pantry", id => balance.Ingredients.Any(i => i.Id == id && !i.SpoilsOvernight)),
                Upgrades = Counts(save, "upgrades", balance.HasUpgrade),
                UpgradeEarnings = Counts(save, "upgradeEarnings", balance.HasUpgrade, allowNegative: true),
                History = ValueList<DaySummary>.From((save["history"] as List<object> ?? new List<object>())
                    .OfType<JsonObject>()
                    .Select(day => new DaySummary(
                        Int(day, "day", 0),
                        Int(day, "customers", 0),
                        Int(day, "sold", 0),
                        Int(day, "wasted", 0),
                        Int(day, "missed", 0),
                        Int(day, "revenue", 0),
                        Int(day, "costs", 0),
                        Int(day, "cashAfter", 0)))
                    .Where(day => day.Day > 0))
                    .TakeLast(balance.HistoryDays),
            };

            // Earnings are only tracked for upgrades the shop owns.
            foreach (var id in loaded.UpgradeEarnings.Keys.Where(id => !loaded.Upgrades.ContainsKey(id)).ToList())
            {
                loaded = loaded with { UpgradeEarnings = loaded.UpgradeEarnings.Without(id) };
            }

            state = loaded;
            problem = null;
            return true;
        }

        private static JsonObject Counts(ValueMap<int> counts)
        {
            var obj = new JsonObject();
            foreach (var pair in counts)
            {
                obj[pair.Key] = pair.Value;
            }

            return obj;
        }

        private static ValueMap<int> Counts(JsonObject save, string key, Func<string, bool> known, bool allowNegative = false)
        {
            var map = ValueMap<int>.Empty;
            if (save[key] is JsonObject obj)
            {
                foreach (var id in obj.Keys.Where(known))
                {
                    var value = Int(obj, id, 0);
                    map = map.With(id, allowNegative ? value : Math.Max(0, value));
                }
            }

            return map;
        }

        private static int Int(JsonObject obj, string key, int fallback) =>
            obj[key] is long value && value >= int.MinValue && value <= int.MaxValue ? (int)value : fallback;

        private static bool Flag(JsonObject obj, string key) => obj[key] is bool flag && flag;
    }
}
