using System.Collections.Generic;
using System.Linq;
using Townscape.CoffeeShop;
using Townscape.Runtime.CoffeeShop;
using UnityEngine;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// The Fellside Coffee window: prep before opening, the review of the day just traded, and the
    /// upgrades. It takes the control panel's place while open. Like the panel it only dispatches
    /// actions; everything it shows (the plan, the forecast, the day's results) is worked out by the
    /// game code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoffeeShopWindow : MonoBehaviour
    {
        private const float Width = 460f;
        private const float NameWidth = 160f;
        private const float StepWidth = 30f;
        private const float CountWidth = 38f;
        private const float MoneyWidth = 90f;
        private const int ReserveStep = 500;
        private const int MaxReserve = 50000;
        private const int RecentDays = 7;

        private CoffeeShopGame _game;
        private PanelSkin _skin;
        private Vector2 _scroll;
        private bool _showUpgrades;
        private bool _confirmNewGame;
        private ShopPhase _lastPhase;

        // What this frame shows, fixed on the layout pass. A click can change the game or the tab
        // part-way through a pass; drawing the rest of the pass from these keeps every pass's
        // controls the same as its layout's, which IMGUI requires.
        private bool _open;
        private CoffeeShopState _state;
        private bool _upgradesShown;
        private bool _confirmShown;

        public void Initialize(CoffeeShopGame game)
        {
            _game = game;
        }

        private void OnDestroy()
        {
            _skin?.Dispose();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                _open = _game != null && _game.IsOpen && _game.Store != null;
                if (_open)
                {
                    _state = _game.Store.State;
                    if (_state.Phase != _lastPhase)
                    {
                        // A new screen starts at the top.
                        _lastPhase = _state.Phase;
                        _scroll = Vector2.zero;
                        _showUpgrades = false;
                    }

                    _upgradesShown = _showUpgrades;
                    _confirmShown = _confirmNewGame;
                }
            }

            if (!_open || _state == null)
            {
                return;
            }

            _skin ??= new PanelSkin();
            var scale = Mathf.Clamp(Screen.height / 900f, 1f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            var state = _state;

            GUILayout.BeginArea(new Rect(16f, 16f, Width, (Screen.height / scale) - 32f));
            GUILayout.BeginVertical(_skin.Panel);
            Header(state);
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
            if (_upgradesShown)
            {
                Upgrades(state);
            }
            else if (state.Phase == ShopPhase.Prep)
            {
                Prep(state);
            }
            else
            {
                Review(state);
            }

            GUILayout.EndScrollView();
            Footer(state);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private CoffeeShopBalance Balance => _game.Balance;

        private void Dispatch(Townscape.State.IAction action) => _game.Store.Dispatch(action);

        private void Header(CoffeeShopState state)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fellside Coffee", _skin.Title);
            var day = state.Phase == ShopPhase.Prep ? state.Day : state.LastResult?.Day ?? state.Day - 1;
            GUILayout.Label(state.Phase == ShopPhase.ClosedDown ? "Closed down" : $"Day {day}", _skin.Clock);
            GUILayout.EndHorizontal();

            var when = state.Phase == ShopPhase.Prep ? "Before opening" : state.Phase == ShopPhase.Review ? "After closing" : "The bank has closed the shop";
            GUILayout.Label($"{when}  ·  cash {Money.Format(state.CashPence)}  ·  keeping back {Money.Format(state.ReservePence)}", _skin.Status);

            GUILayout.BeginHorizontal();
            var today = state.Phase == ShopPhase.Prep ? "Prep" : state.Phase == ShopPhase.Review ? "Today's results" : "The last day";
            if (GUILayout.Toggle(!_upgradesShown, today, _skin.Button) && _upgradesShown)
            {
                _showUpgrades = false;
                _scroll = Vector2.zero;
            }

            if (GUILayout.Toggle(_upgradesShown, "Upgrades", _skin.Button) && !_upgradesShown)
            {
                _showUpgrades = true;
                _scroll = Vector2.zero;
            }

            if (GUILayout.Button("Close", _skin.Button, GUILayout.Width(64f)))
            {
                _game.Close();
            }

            GUILayout.EndHorizontal();
        }

        private void Prep(CoffeeShopState state)
        {
            var plan = PrepPlan.For(Balance, state);
            var site = Balance.Site(state.SiteId);
            var forecast = Forecast.For(Balance, site);

            Heading("WHO COMES IN");
            foreach (var slot in forecast.Slots)
            {
                var crowd = string.Join(", ", slot.Segments.Select(s => $"{Balance.Segment(s.Id).Name.ToLowerInvariant()} {s.Weight:0}"));
                GUILayout.Label($"<b>{slot.Name}</b> ({Duration(slot.DurationSeconds)}): about {slot.Customers:0}: {crowd}", _skin.Body);
            }

            foreach (var segment in forecast.Segments.Select(s => Balance.Segment(s.Id)))
            {
                GUILayout.Label(
                    $"<b>{segment.Name}</b>: {segment.Description} Spend up to about {Money.Format(segment.BudgetPence)}; "
                    + $"{Percent(segment.DairyFreeShare)} can't have dairy.",
                    _skin.Status);
            }

            GUILayout.Label("Some days are busier than others.", _skin.Status);

            Heading("MENU");
            foreach (var item in Balance.Items)
            {
                MenuRow(state, plan, forecast, item);
            }

            Heading("MILK");
            GUILayout.BeginHorizontal();
            foreach (var milk in Balance.MilkOptions)
            {
                var chosen = state.MilkId == milk.Id;
                var label = $"{milk.Name} {milk.CostPerPortionPence}p" + (milk.DairyFree ? " (dairy-free)" : string.Empty);
                if (GUILayout.Toggle(chosen, label, _skin.Button) && !chosen)
                {
                    Dispatch(new SetMilk(milk.Id));
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Label("Every milky drink is made with it. Milk doesn't keep overnight.", _skin.Status);

            Heading("STOCK");
            Row("Display case", $"{plan.PastriesPlanned} of {plan.DisplayCapacity}");
            Row("Fridge (milk)", $"{plan.MilkPlanned} of {plan.FridgeCapacity}");
            foreach (var line in plan.Lines.Where(l => l.Kind == ItemKind.Pastry && l.Count > 0))
            {
                Row($"Bake {line.Count} {Name(Balance.Item(line.ItemId), line.Count)}", Money.Format(line.CostPence));
            }

            foreach (var line in plan.Ingredients)
            {
                var ingredient = Balance.Ingredient(line.IngredientId);
                var label = ingredient.IsMilk ? Balance.Milk(state.MilkId).Name : ingredient.Name;
                var pantry = line.InPantry > 0 ? $", {line.InPantry} left over" : string.Empty;
                Row($"{label}: buy {line.ToBuy}{pantry}", Money.Format(line.CostPence));
            }

            Row("Cups, lids and ice", $"{Balance.SuppliesPerDrinkPence}p a drink");
            Row("Stock to buy now", Money.Format(plan.StockCostPence), _skin.Bold);
            Row("Rent and upkeep, at closing", Money.Format(plan.FixedCostPence));

            foreach (var warning in plan.Warnings)
            {
                GUILayout.Label("• " + warning, _skin.Warning);
            }

            Heading("KEEP BACK");
            GUILayout.BeginHorizontal();
            var shown = (float)Mathf.Min(state.ReservePence, MaxReserve);
            var reserve = GUILayout.HorizontalSlider(shown, 0f, MaxReserve, _skin.Slider, _skin.Thumb);
            GUILayout.Label(Money.Format(state.ReservePence), _skin.Value, GUILayout.Width(MoneyWidth));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(reserve - shown) > 0.5f)
            {
                // In steps of £5, and only when the slider is moved.
                Dispatch(new SetReserve(Mathf.RoundToInt(reserve / ReserveStep) * ReserveStep));
            }

            GUILayout.Label($"Upgrades can't spend this. A bad day can take cash below zero, but past the {Money.Format(Balance.OverdraftPence)} overdraft the bank closes the shop.", _skin.Status);
        }

        private void MenuRow(CoffeeShopState state, PrepPlan plan, Forecast forecast, ItemDef item)
        {
            var onMenu = state.Menu.Contains(item.Id);
            GUILayout.BeginHorizontal();
            var toggled = GUILayout.Toggle(onMenu, $"{item.Name}  {Money.Format(item.PricePence)}", _skin.Button, GUILayout.Width(NameWidth));
            if (toggled != onMenu)
            {
                Dispatch(new SetOnMenu(item.Id, toggled));
            }

            var line = plan.Line(item.Id);
            if (line == null)
            {
                GUILayout.Label("not on the menu", _skin.Value);
                GUILayout.EndHorizontal();
                return;
            }

            // Shift steps by five.
            var step = Event.current.shift ? 5 : 1;
            var planned = state.Planned.Get(item.Id);
            if (GUILayout.Button("−", _skin.Button, GUILayout.Width(StepWidth)))
            {
                Dispatch(new SetPlanned(item.Id, planned - step));
            }

            GUILayout.Label(line.Count.ToString(), _skin.Bold, GUILayout.Width(CountWidth));
            if (GUILayout.Button("+", _skin.Button, GUILayout.Width(StepWidth)))
            {
                Dispatch(new SetPlanned(item.Id, planned + step));
            }

            var wanted = forecast.FirstChoicesOf(item.Id);
            var unit = item.Kind == ItemKind.Pastry ? "to bake" : "servings";
            var extra = item.Kind == ItemKind.Drink && line.Makeable > line.Count ? $", can make {line.Makeable}" : string.Empty;
            GUILayout.Label($"{unit}{extra}  ·  ~{wanted:0} want", _skin.Value);
            GUILayout.EndHorizontal();
        }

        private void Review(CoffeeShopState state)
        {
            if (state.Phase == ShopPhase.ClosedDown)
            {
                Heading("CLOSED DOWN");
                GUILayout.Label(
                    $"Cash fell to {Money.Format(state.CashPence)}, past the {Money.Format(Balance.OverdraftPence)} overdraft, "
                    + "so the bank has closed Fellside Coffee. Keeping some cash back for bad days helps the next shop last longer.",
                    _skin.Warning);
            }

            var result = state.LastResult;
            if (result == null)
            {
                return;
            }

            var usual = Forecast.For(Balance, Balance.Site(result.SiteId)).Customers;
            Heading($"DAY {result.Day}");
            GUILayout.Label(
                $"{result.Customers} customers came in ({Busyness(result.Customers, usual)}). {result.Served} bought something"
                + (result.WalkedOut > 0 ? $" and {result.WalkedOut} gave up queuing." : "."),
                _skin.Body);

            Heading("MONEY");
            Row("Takings", Money.Format(result.RevenuePence));
            Row("Stock bought", Money.Format(-result.StockPence));
            Row("Cups, lids and ice", Money.Format(-result.SuppliesPence));
            Row("Rent", Money.Format(-result.SiteCostPence));
            if (result.RunningCostPence > 0)
            {
                Row("Upgrade upkeep", Money.Format(-result.RunningCostPence));
            }

            Row("Profit", Money.Signed(result.ProfitPence), _skin.Bold, result.ProfitPence >= 0 ? _skin.Positive : _skin.Negative);
            Row("Cash now", Money.Format(result.CashAfterPence));

            Heading("SOLD, WASTED, MISSED");
            var milkName = Balance.HasMilk(result.MilkId) ? Balance.Milk(result.MilkId).Name.ToLowerInvariant() : "the milk";
            foreach (var item in result.Items)
            {
                var def = Balance.Item(item.ItemId);
                var text = $"<b>{def.Name}</b>: sold {item.Sold}";
                if (item.SoldAsSecondChoice > 0)
                {
                    text += $" ({item.SoldAsSecondChoice} as someone's second choice)";
                }

                if (item.Wasted > 0)
                {
                    text += $", wasted {item.Wasted}";
                }

                if (item.Missed.Total > 0)
                {
                    text += $", missed {item.Missed.Total}: {Reasons(item.Missed, milkName)}";
                }

                GUILayout.Label(text + ".", _skin.Body);
            }

            if (result.MilkPouredAway > 0)
            {
                GUILayout.Label($"{result.MilkPouredAway} portions of milk poured away at closing.", _skin.Body);
            }

            if (result.WastePence > 0)
            {
                GUILayout.Label($"Waste cost {Money.Format(result.WastePence)}.", _skin.Status);
            }

            Heading("CUSTOMERS");
            foreach (var segment in result.Segments)
            {
                var walked = segment.WalkedOut > 0 ? $"; {segment.WalkedOut} walked out of the queue" : string.Empty;
                GUILayout.Label(
                    $"<b>{Balance.Segment(segment.SegmentId).Name}</b>: {segment.Customers} came and got {Percent(segment.ServedShare)} of what they wanted{walked}. Spent {Money.Format(segment.RevenuePence)}.",
                    _skin.Body);
            }

            Heading("THE QUEUE");
            foreach (var slot in result.Slots)
            {
                var walked = slot.WalkedOut > 0 ? $", {slot.WalkedOut} gave up" : string.Empty;
                GUILayout.Label($"<b>{slot.Name}</b>: {slot.Customers} customers, longest wait {Duration(slot.LongestWaitSeconds)}{walked}.", _skin.Body);
            }

            if (result.UpgradeImpacts.Count > 0)
            {
                Heading("WHAT UPGRADES DID");
                foreach (var impact in result.UpgradeImpacts)
                {
                    var upgrade = Balance.Upgrade(impact.UpgradeId);
                    GUILayout.Label(
                        $"<b>{upgrade.Name}</b>: {Money.Signed(impact.ProfitPence)} today against the same day without it "
                        + $"({Count(impact.Sold)} sold, {Count(impact.Wasted)} wasted, {Count(impact.Missed)} missed). "
                        + $"{Money.Signed(state.UpgradeEarnings.Get(impact.UpgradeId))} so far, for a price of {Money.Format(upgrade.CostPence)}.",
                        _skin.Body);
                }
            }

            if (state.History.Count > 1)
            {
                Heading("RECENT DAYS");
                foreach (var day in state.History.TakeLast(RecentDays).Reverse())
                {
                    Row($"Day {day.Day}: sold {day.Sold}, wasted {day.Wasted}, missed {day.Missed}", Money.Signed(day.ProfitPence), _skin.Label, day.ProfitPence >= 0 ? _skin.Positive : _skin.Negative);
                }
            }
        }

        private void Upgrades(CoffeeShopState state)
        {
            var spendable = CoffeeShopReducer.SpendableOnUpgrades(state);
            GUILayout.Label(
                $"You can spend {Money.Format(spendable)}: your cash less the {Money.Format(state.ReservePence)} you keep back. "
                + "An upgrade works from the next day you open.",
                _skin.Body);

            foreach (var group in Balance.Upgrades.GroupBy(u => u.Kind))
            {
                Heading(group.Key.ToString().ToUpperInvariant());
                foreach (var upgrade in group)
                {
                    UpgradeCard(state, upgrade, spendable);
                }
            }

            Heading("START AGAIN");
            if (!_confirmShown)
            {
                if (GUILayout.Button("Start a new shop…", _skin.Button))
                {
                    _confirmNewGame = true;
                }

                return;
            }

            GUILayout.Label("This replaces the shop you have, and can't be undone.", _skin.Warning);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start again", _skin.Button))
            {
                StartNewGame();
            }

            if (GUILayout.Button("Keep this shop", _skin.Button))
            {
                _confirmNewGame = false;
            }

            GUILayout.EndHorizontal();
        }

        private void UpgradeCard(CoffeeShopState state, UpgradeDef upgrade, int spendable)
        {
            GUILayout.Label($"<b>{upgrade.Name}</b>", _skin.Body);
            GUILayout.Label(upgrade.Summary, _skin.Body);
            GUILayout.Label("Trade-off: " + upgrade.TradeOff, _skin.Status);
            Row("Price", Money.Format(upgrade.CostPence));
            Row("Upkeep", $"{Money.Format(upgrade.RunningCostPence)} a day");

            if (state.Upgrades.ContainsKey(upgrade.Id))
            {
                var from = state.Upgrades.Get(upgrade.Id);
                var text = state.Day <= from
                    ? $"Bought. It's in place for day {from}."
                    : $"Bought for day {from}. It has made {Money.Signed(state.UpgradeEarnings.Get(upgrade.Id))} more than going without, against its price of {Money.Format(upgrade.CostPence)}.";
                GUILayout.Label(text, _skin.Body);
                return;
            }

            var canBuy = CoffeeShopReducer.CanBuy(Balance, state, upgrade.Id);
            GUI.enabled = canBuy;
            if (GUILayout.Button($"Buy for {Money.Format(upgrade.CostPence)}", _skin.Button))
            {
                Dispatch(new BuyUpgrade(upgrade.Id));
            }

            GUI.enabled = true;
            if (!canBuy && state.Phase != ShopPhase.ClosedDown)
            {
                GUILayout.Label($"You need {Money.Format(upgrade.CostPence - spendable)} more above what you keep back.", _skin.Status);
            }
        }

        private void Footer(CoffeeShopState state)
        {
            GUILayout.Space(8f);
            switch (state.Phase)
            {
                case ShopPhase.Prep:
                    var plan = PrepPlan.For(Balance, state);
                    GUI.enabled = plan.CanAfford;
                    if (GUILayout.Button($"Open for day {state.Day}", _skin.Button))
                    {
                        Dispatch(new OpenForTheDay());
                    }

                    GUI.enabled = true;
                    break;
                case ShopPhase.Review:
                    if (GUILayout.Button($"Plan day {state.Day}", _skin.Button))
                    {
                        Dispatch(new ContinueToPrep());
                    }

                    break;
                default:
                    if (GUILayout.Button("Start a new shop", _skin.Button))
                    {
                        StartNewGame();
                    }

                    break;
            }
        }

        private void StartNewGame()
        {
            Dispatch(new StartNewGame(CoffeeShopSaveFile.NewSeed()));
            _confirmNewGame = false;
            _showUpgrades = false;
            _scroll = Vector2.zero;
        }

        private void Heading(string text) => GUILayout.Label(text, _skin.Heading);

        private void Row(string label, string value, GUIStyle labelStyle = null, GUIStyle valueStyle = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle ?? _skin.Label);
            GUILayout.Label(value, valueStyle ?? _skin.Value, GUILayout.Width(MoneyWidth));
            GUILayout.EndHorizontal();
        }

        private static string Reasons(Misses missed, string milkName)
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

        private static string Busyness(int customers, double usual)
        {
            var ratio = usual > 0 ? customers / usual : 1.0;
            var feel = ratio < 0.85 ? "a quiet day" : ratio > 1.15 ? "a busy day" : "about usual";
            return $"{feel}; usually about {usual:0}";
        }

        private static string Name(ItemDef item, int count) => (count == 1 ? item.Name : item.PluralName).ToLowerInvariant();

        private static string Count(int change) => change > 0 ? $"+{change}" : change.ToString();

        private static string Percent(double share) => $"{Mathf.RoundToInt((float)(share * 100.0))}%";

        private static string Duration(int seconds)
        {
            if (seconds < 60)
            {
                return $"{seconds}s";
            }

            if (seconds < 3600)
            {
                var minutes = seconds / 60;
                var rest = seconds % 60;
                return rest == 0 || seconds >= 600 ? $"{minutes} min" : $"{minutes}m {rest:00}s";
            }

            var hours = seconds / 3600.0;
            return $"{hours:0.#} hours";
        }
    }
}
