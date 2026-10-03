using System.Linq;
using Townscape.CoffeeShop;
using Townscape.Runtime.CoffeeShop;
using UnityEngine;

namespace Townscape.Runtime.UI
{
    /// <summary>
    /// Fellside Coffee as a newspaper, the Fellside Herald, filling the screen. Each morning's paper
    /// reports yesterday's trading on the front page, forecasts who'll come in, and carries the
    /// classifieds: suppliers to order today's stock from, upgrades for sale, and notices for the menu
    /// and the cash kept back. Like the control panel it only dispatches actions; everything it prints
    /// is worked out by the game code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoffeeShopPaper : MonoBehaviour
    {
        private const float Margin = 28f;
        private const float Gutter = 24f;
        private const float MastheadHeight = 128f;
        private const float OrderHeight = 140f;
        private const float StepWidth = 28f;
        private const float CountWidth = 40f;
        private const float DrinkWidth = 100f;
        private const float MoneyWidth = 80f;
        private const float NumberWidth = 50f;
        private const float OpenWidth = 250f;
        private const int ReserveStep = 500;
        private const int MaxReserve = 50000;
        private const int RecentDays = 7;

        private CoffeeShopGame _game;
        private PaperSkin _skin;
        private Vector2 _newsScroll;
        private Vector2 _middleScroll;
        private Vector2 _suppliersScroll;
        private Vector2 _noticesScroll;
        private bool _confirmNewGame;

        // What this frame prints, fixed on the layout pass. A click can change the game part-way
        // through a pass; drawing the rest of the pass from these keeps every pass's controls the
        // same as its layout's, which IMGUI requires.
        private bool _open;
        private CoffeeShopState _state;
        private bool _confirmShown;
        private int _edition;

        public void Initialize(CoffeeShopGame game)
        {
            _game = game;
        }

        private CoffeeShopBalance Balance => _game.Balance;

        private void OnDestroy()
        {
            _skin?.Dispose();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                _open = _game != null && _game.IsReading && _game.Store != null;
                if (_open)
                {
                    _state = _game.Store.State;
                    if (_state.Day != _edition)
                    {
                        // A new edition starts at the top of every column.
                        _edition = _state.Day;
                        _newsScroll = _middleScroll = _suppliersScroll = _noticesScroll = Vector2.zero;
                        _confirmNewGame = false;
                    }

                    _confirmShown = _confirmNewGame;
                }
            }

            if (!_open || _state == null)
            {
                return;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                _game.Close();
                return;
            }

            _skin ??= new PaperSkin();
            var scale = Mathf.Clamp(Screen.height / 900f, 0.75f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            var height = Screen.height / scale;

            var state = _state;
            var plan = PrepPlan.For(Balance, state);
            var forecast = Forecast.For(Balance, Balance.Site(state.SiteId));

            GUI.DrawTexture(new Rect(0f, 0f, width, height), _skin.PaperTexture);
            Masthead(state, width);

            var top = MastheadHeight + 10f;
            var bottom = height - 18f;
            var inner = width - (2f * Margin) - (2f * Gutter);
            var newsWidth = inner * 0.335f;
            var middleWidth = inner * 0.215f;
            var classifiedsWidth = inner - newsWidth - middleWidth;
            var middleX = Margin + newsWidth + Gutter;
            var classifiedsX = middleX + middleWidth + Gutter;
            VerticalRule(middleX - (Gutter * 0.5f), top, bottom - top);
            VerticalRule(classifiedsX - (Gutter * 0.5f), top, bottom - top);

            BeginColumn(new Rect(Margin, top, newsWidth, bottom - top), ref _newsScroll);
            FrontPage(state);
            EndColumn();

            BeginColumn(new Rect(middleX, top, middleWidth, bottom - top), ref _middleScroll);
            Inside(state, forecast);
            EndColumn();

            GUI.Label(new Rect(classifiedsX, top, classifiedsWidth, 26f), "CLASSIFIED ADVERTISEMENTS", _skin.Banner);
            var adsTop = top + 36f;
            if (state.Phase == ShopPhase.ClosedDown)
            {
                GUILayout.BeginArea(new Rect(classifiedsX, adsTop, classifiedsWidth, bottom - adsTop));
                BusinessForSale();
                GUILayout.EndArea();
                return;
            }

            var orderTop = bottom - OrderHeight;
            var halfWidth = (classifiedsWidth - Gutter) * 0.5f;
            BeginColumn(new Rect(classifiedsX, adsTop, halfWidth, orderTop - adsTop - 10f), ref _suppliersScroll);
            Suppliers(state, plan, forecast);
            EndColumn();
            VerticalRule(classifiedsX + halfWidth + (Gutter * 0.5f), adsTop, orderTop - adsTop - 10f);
            BeginColumn(new Rect(classifiedsX + halfWidth + Gutter, adsTop, halfWidth, orderTop - adsTop - 10f), ref _noticesScroll);
            Notices(state);
            EndColumn();

            HorizontalRule(classifiedsX, orderTop - 4f, classifiedsWidth, 2f);
            GUILayout.BeginArea(new Rect(classifiedsX, orderTop + 2f, classifiedsWidth, OrderHeight - 2f));
            Order(state, plan);
            GUILayout.EndArea();
        }

        private void Masthead(CoffeeShopState state, float width)
        {
            GUI.Label(new Rect(Margin, 10f, 240f, 20f), $"No. {state.Day}", _skin.StrapLeft);
            GUI.Label(new Rect(0f, 10f, width, 20f), "Serving the valley since 1868", _skin.Strap);
            if (GUI.Button(new Rect(width - Margin - 130f, 8f, 130f, 24f), "Close the paper", _skin.Button))
            {
                _game.Close();
            }

            GUI.Label(new Rect(0f, 30f, width, 62f), NewsDesk.PaperName, _skin.Masthead);
            HorizontalRule(Margin, 96f, width - (2f * Margin), 2f);
            var edition = state.Phase == ShopPhase.ClosedDown ? "Final edition" : "Morning edition";
            GUI.Label(new Rect(Margin, 100f, 300f, 20f), $"Day {state.Day}  ·  {edition}", _skin.StrapLeft);
            GUI.Label(new Rect(0f, 100f, width, 20f), $"Cash in the till {Money.Format(state.CashPence)}  ·  keeping back {Money.Format(state.ReservePence)}", _skin.Strap);
            GUI.Label(new Rect(width - Margin - 300f, 100f, 300f, 20f), $"{Balance.Site(state.SiteId).Name}  ·  Price 30p", _skin.StrapRight);
            HorizontalRule(Margin, 122f, width - (2f * Margin), 1f);
            HorizontalRule(Margin, 125f, width - (2f * Margin), 1f);
        }

        private void FrontPage(CoffeeShopState state)
        {
            var story = NewsDesk.Lead(Balance, state);
            GUILayout.Label(story.Headline, _skin.Headline);
            GUILayout.Label(story.Standfirst, _skin.Standfirst);
            GUILayout.Label("By our business correspondent", _skin.Byline);
            foreach (var paragraph in story.Paragraphs)
            {
                GUILayout.Label(paragraph, _skin.Body);
            }

            var result = state.LastResult;
            if (result == null)
            {
                if (state.History.Count == 0)
                {
                    GUILayout.Label("The first figures will be in tomorrow's edition.", _skin.Quiet);
                    Section("NOTES FOR THE NEW OWNER");
                    foreach (var note in NewsDesk.FirstDayNotes(Balance))
                    {
                        GUILayout.Label(note, _skin.Body);
                    }
                }

                return;
            }

            Section("THE DAY IN NUMBERS");
            Row("Takings", Money.Format(result.RevenuePence));
            Row("Stock bought", Money.Format(-result.StockPence));
            Row("Cups, lids and ice", Money.Format(-result.SuppliesPence));
            Row("Rent", Money.Format(-result.SiteCostPence));
            if (result.RunningCostPence > 0)
            {
                Row("Upgrade upkeep", Money.Format(-result.RunningCostPence));
            }

            Row("Profit", Money.Signed(result.ProfitPence), _skin.Strong, result.ProfitPence >= 0 ? _skin.Gain : _skin.Loss);
            Row("Cash in the till", Money.Format(result.CashAfterPence));

            Section("SOLD, WASTED, MISSED");
            TableRow(_skin.ColumnLeft, _skin.Column, string.Empty, "Sold", "Wasted", "Missed");
            foreach (var item in result.Items)
            {
                TableRow(_skin.Label, _skin.Value, Balance.Item(item.ItemId).Name, item.Sold.ToString(), item.Wasted.ToString(), item.Missed.Total.ToString());
            }

            if (result.MilkPouredAway > 0)
            {
                GUILayout.Label($"{result.MilkPouredAway} portions of milk were poured away at closing.", _skin.Quiet);
            }
        }

        private void Inside(CoffeeShopState state, Forecast forecast)
        {
            Section("FOOTFALL FORECAST");
            foreach (var slot in forecast.Slots)
            {
                var crowd = string.Join(", ", slot.Segments.Select(s => $"{Balance.Segment(s.Id).Name.ToLowerInvariant()} {s.Weight:0}"));
                GUILayout.Label($"<b>{slot.Name}</b> ({Duration(slot.DurationSeconds)}): about {slot.Customers:0}, {crowd}.", _skin.Small);
            }

            GUILayout.Label("Some days are busier than others.", _skin.Quiet);

            Section("WHO'S ABOUT");
            foreach (var segment in forecast.Segments.Select(s => Balance.Segment(s.Id)))
            {
                GUILayout.Label(
                    $"<b>{segment.Name}.</b> {segment.Description} They spend up to about {Money.Format(segment.BudgetPence)}, "
                    + $"and {Percent(segment.DairyFreeShare)} can't have dairy.",
                    _skin.Small);
            }

            var result = state.LastResult;
            if (result != null)
            {
                Section("THE QUEUE YESTERDAY");
                foreach (var slot in result.Slots)
                {
                    var wait = slot.LongestWaitSeconds > 0 ? $"longest wait {Duration(slot.LongestWaitSeconds)}" : "no waiting";
                    var walked = slot.WalkedOut > 0 ? $", {slot.WalkedOut} gave up" : string.Empty;
                    GUILayout.Label($"<b>{slot.Name}</b>: {slot.Customers} customers, {wait}{walked}.", _skin.Small);
                }

                if (result.UpgradeImpacts.Count > 0)
                {
                    Section("UPGRADE REPORT");
                    foreach (var impact in result.UpgradeImpacts)
                    {
                        var upgrade = Balance.Upgrade(impact.UpgradeId);
                        GUILayout.Label(
                            $"<b>{upgrade.Name}</b>: {Money.Signed(impact.ProfitPence)} yesterday against the same day without it "
                            + $"({Signed(impact.Sold)} sold, {Signed(impact.Wasted)} wasted, {Signed(impact.Missed)} missed). "
                            + $"{Money.Signed(state.UpgradeEarnings.Get(impact.UpgradeId))} since day {state.Upgrades.Get(impact.UpgradeId)}, for a price of {Money.Format(upgrade.CostPence)}.",
                            _skin.Small);
                    }
                }
            }

            if (state.History.Count > 0)
            {
                Section("RECENT TRADE");
                TableRow(_skin.ColumnLeft, _skin.Column, "Day", "Sold", "Wasted", "Missed", "Profit");
                foreach (var day in state.History.TakeLast(RecentDays).Reverse())
                {
                    TableRow(
                        _skin.Label,
                        _skin.Value,
                        $"Day {day.Day}",
                        day.Sold.ToString(),
                        day.Wasted.ToString(),
                        day.Missed.ToString(),
                        Money.Signed(day.ProfitPence),
                        day.ProfitPence >= 0 ? _skin.Gain : _skin.Loss);
                }
            }
        }

        private void Suppliers(CoffeeShopState state, PrepPlan plan, Forecast forecast)
        {
            Section("SUPPLIERS");

            foreach (var pastry in Balance.Items.Where(i => i.Kind == ItemKind.Pastry))
            {
                GUILayout.BeginVertical(_skin.Advert);
                AdHeader(pastry.Supplier?.Name ?? pastry.PluralName, Advert(pastry.Supplier, $"{pastry.PluralName}, {Money.Format(pastry.UnitCostPence)} each.", $"{Money.Format(pastry.UnitCostPence)} each."));
                StockRow(state, plan, forecast, pastry);
                if (plan.Line(pastry.Id) != null)
                {
                    GUILayout.Label($"Your case holds {plan.DisplayCapacity}; unsold ones are thrown away.", _skin.Quiet);
                }

                GUILayout.EndVertical();
            }

            foreach (var ingredient in Balance.Ingredients.Where(i => !i.IsMilk))
            {
                var drinks = Balance.Items.Where(i => i.Kind == ItemKind.Drink && Balance.MainIngredient(i) == ingredient).ToList();
                if (drinks.Count == 0)
                {
                    continue;
                }

                GUILayout.BeginVertical(_skin.Advert);
                var price = $"{ingredient.CostPerPortionPence}p a serving; keeps.";
                AdHeader(ingredient.Supplier?.Name ?? ingredient.Name, Advert(ingredient.Supplier, $"{ingredient.Name}, {price}", price));
                foreach (var drink in drinks)
                {
                    StockRow(state, plan, forecast, drink);
                }

                OrderLine(plan, ingredient.Id);
                GUILayout.EndVertical();
            }

            var milk = Balance.MilkIngredient;
            if (milk == null || Balance.MilkOptions.Count == 0)
            {
                return;
            }

            GUILayout.BeginVertical(_skin.Advert);
            AdHeader("Milk", "One supplier for every milky drink; milk doesn't keep.");
            foreach (var option in Balance.MilkOptions)
            {
                var chosen = state.MilkId == option.Id;
                var label = $"{option.Supplier?.Name ?? option.Name}: {option.Name.ToLowerInvariant()}, {option.CostPerPortionPence}p" + (option.DairyFree ? ", dairy-free" : string.Empty);
                if (GUILayout.Toggle(chosen, label, _skin.Button) && !chosen)
                {
                    _game.Store.Dispatch(new SetMilk(option.Id));
                }
            }

            var supplier = Balance.Milk(state.MilkId).Supplier;
            if (supplier != null)
            {
                GUILayout.Label(supplier.Advert, _skin.Quiet);
            }

            OrderLine(plan, milk.Id);
            GUILayout.EndVertical();
        }

        // How many to bake or stock for: − count + and how many usually want it.
        private void StockRow(CoffeeShopState state, PrepPlan plan, Forecast forecast, ItemDef item)
        {
            GUILayout.BeginHorizontal();
            if (item.Kind == ItemKind.Drink)
            {
                GUILayout.Label(item.PluralName, _skin.RowLabel, GUILayout.Width(DrinkWidth));
            }

            var line = plan.Line(item.Id);
            if (line == null)
            {
                GUILayout.Label("not on the menu today", _skin.Note);
                GUILayout.EndHorizontal();
                return;
            }

            // Shift steps by five.
            var step = Event.current.shift ? 5 : 1;
            var planned = state.Planned.Get(item.Id);
            if (GUILayout.Button("−", _skin.Button, GUILayout.Width(StepWidth)))
            {
                _game.Store.Dispatch(new SetPlanned(item.Id, planned - step));
            }

            GUILayout.Label(line.Count.ToString(), _skin.Count, GUILayout.Width(CountWidth));
            if (GUILayout.Button("+", _skin.Button, GUILayout.Width(StepWidth)))
            {
                _game.Store.Dispatch(new SetPlanned(item.Id, planned + step));
            }

            var unit = item.Kind == ItemKind.Pastry ? "to bake, " : string.Empty;
            GUILayout.Label($"{unit}~{forecast.FirstChoicesOf(item.Id):0} want", _skin.Note);
            GUILayout.EndHorizontal();
        }

        private void OrderLine(PrepPlan plan, string ingredientId)
        {
            var line = plan.Ingredient(ingredientId);
            if (line == null)
            {
                GUILayout.Label("Nothing to order today.", _skin.Quiet);
                return;
            }

            var leftOver = line.InPantry > 0 ? $"; {line.InPantry} left from before" : string.Empty;
            GUILayout.Label($"<b>Order {line.ToBuy}: {Money.Format(line.CostPence)}</b>{leftOver}.", _skin.Small);
        }

        private void Notices(CoffeeShopState state)
        {
            var spendable = CoffeeShopReducer.SpendableOnUpgrades(state);
            Section("FOR SALE");
            GUILayout.Label($"{Money.Format(spendable)} to spend, after the {Money.Format(state.ReservePence)} kept back.", _skin.Quiet);
            foreach (var upgrade in Balance.Upgrades)
            {
                UpgradeAdvert(state, upgrade, spendable);
            }

            Section("NOTICES");
            GUILayout.BeginVertical(_skin.Advert);
            AdHeader("Today's menu at Fellside Coffee", null);
            foreach (var item in Balance.Items)
            {
                var onMenu = state.Menu.Contains(item.Id);
                var toggled = GUILayout.Toggle(onMenu, $"{item.Name}, {Money.Format(item.PricePence)}", _skin.Button);
                if (toggled != onMenu)
                {
                    _game.Store.Dispatch(new SetOnMenu(item.Id, toggled));
                }
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical(_skin.Advert);
            AdHeader("Fellside Building Society", "Put something by for a rainy day.");
            GUILayout.BeginHorizontal();
            var shown = (float)Mathf.Min(state.ReservePence, MaxReserve);
            var reserve = GUILayout.HorizontalSlider(shown, 0f, MaxReserve, _skin.Slider, _skin.Thumb);
            GUILayout.Label(Money.Format(state.ReservePence), _skin.Count, GUILayout.Width(MoneyWidth));
            GUILayout.EndHorizontal();
            if (Mathf.Abs(reserve - shown) > 0.5f)
            {
                // In steps of £5, and only when the slider is moved.
                _game.Store.Dispatch(new SetReserve(Mathf.RoundToInt(reserve / ReserveStep) * ReserveStep));
            }

            GUILayout.Label($"Upgrades can't spend it. Past the {Money.Format(Balance.OverdraftPence)} overdraft, the bank closes the shop.", _skin.Quiet);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(_skin.Advert);
            AdHeader("Business for sale", "Fully fitted café on the High Street.");
            if (!_confirmShown)
            {
                if (GUILayout.Button("Start a new shop…", _skin.Button))
                {
                    _confirmNewGame = true;
                }
            }
            else
            {
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

            GUILayout.EndVertical();
        }

        private void UpgradeAdvert(CoffeeShopState state, UpgradeDef upgrade, int spendable)
        {
            GUILayout.BeginVertical(_skin.Advert);
            AdHeader(upgrade.Name, upgrade.Summary);
            GUILayout.Label(upgrade.TradeOff, _skin.Quiet);
            var seller = string.IsNullOrEmpty(upgrade.Seller) ? string.Empty : $" {upgrade.Seller}.";
            GUILayout.Label($"<b>{Money.Format(upgrade.CostPence)}</b>, then {Money.Format(upgrade.RunningCostPence)} a day.{seller}", _skin.Small);

            if (state.Upgrades.ContainsKey(upgrade.Id))
            {
                var from = state.Upgrades.Get(upgrade.Id);
                var text = state.Day <= from
                    ? "SOLD to you. It's in place for today."
                    : $"SOLD to you for day {from}. It has made {Money.Signed(state.UpgradeEarnings.Get(upgrade.Id))} more than going without.";
                GUILayout.Label(text, _skin.Small);
                GUILayout.EndVertical();
                return;
            }

            var canBuy = CoffeeShopReducer.CanBuy(Balance, state, upgrade.Id);
            GUI.enabled = canBuy;
            if (GUILayout.Button($"Buy for {Money.Format(upgrade.CostPence)}", _skin.Button))
            {
                _game.Store.Dispatch(new BuyUpgrade(upgrade.Id));
            }

            GUI.enabled = true;
            if (!canBuy)
            {
                GUILayout.Label($"{Money.Format(upgrade.CostPence - spendable)} short, after what you keep back.", _skin.Quiet);
            }

            GUILayout.EndVertical();
        }

        private void Order(CoffeeShopState state, PrepPlan plan)
        {
            GUILayout.Label($"YOUR ORDER FOR DAY {state.Day}", _skin.Section);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            Row("Display case", $"{plan.PastriesPlanned} of {plan.DisplayCapacity}");
            Row("Fridge (milk)", $"{plan.MilkPlanned} of {plan.FridgeCapacity}");
            Row("Stock to pay for now", Money.Format(plan.StockCostPence), _skin.Strong, _skin.StrongValue);
            Row("Rent and upkeep, at closing", Money.Format(plan.FixedCostPence));
            GUILayout.EndVertical();
            GUILayout.Space(Gutter);
            GUILayout.BeginVertical(GUILayout.Width(OpenWidth));
            GUILayout.Space(6f);
            GUI.enabled = plan.CanAfford;
            if (GUILayout.Button($"Open for day {state.Day}", _skin.Primary))
            {
                _game.Store.Dispatch(new OpenForTheDay());
            }

            GUI.enabled = true;
            if (plan.Warnings.Count == 0)
            {
                GUILayout.Label("Tomorrow's Herald reports how it went.", _skin.Quiet);
            }

            foreach (var warning in plan.Warnings)
            {
                GUILayout.Label(warning, _skin.Warning);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void BusinessForSale()
        {
            GUILayout.BeginVertical(_skin.Advert);
            AdHeader("Business for sale", "Café on the High Street, fully fitted, with its suppliers ready to deliver. The bank seeks a new owner.");
            GUILayout.Space(6f);
            if (GUILayout.Button("Start a new shop", _skin.Primary))
            {
                StartNewGame();
            }

            GUILayout.EndVertical();
        }

        private void StartNewGame()
        {
            _game.Store.Dispatch(new StartNewGame(CoffeeShopSaveFile.NewSeed()));
            _confirmNewGame = false;
        }

        // The supplier's advert with the price after it, or just the price line without a supplier.
        private static string Advert(SupplierDef supplier, string withoutSupplier, string price) =>
            supplier == null ? withoutSupplier : $"{supplier.Advert} {price}";

        private void AdHeader(string title, string advert)
        {
            GUILayout.Label(title.ToUpperInvariant(), _skin.AdTitle);
            if (!string.IsNullOrEmpty(advert))
            {
                GUILayout.Label(advert, _skin.Small);
            }
        }

        private void Section(string title) => GUILayout.Label(title, _skin.Section);

        private void Row(string label, string value, GUIStyle labelStyle = null, GUIStyle valueStyle = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle ?? _skin.Label);
            GUILayout.Label(value, valueStyle ?? _skin.Value, GUILayout.Width(MoneyWidth));
            GUILayout.EndHorizontal();
        }

        // A label, three narrow number columns and, optionally, a money column.
        private void TableRow(GUIStyle labelStyle, GUIStyle numberStyle, string label, string a, string b, string c, string money = null, GUIStyle moneyStyle = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle);
            GUILayout.Label(a, numberStyle, GUILayout.Width(NumberWidth));
            GUILayout.Label(b, numberStyle, GUILayout.Width(NumberWidth));
            GUILayout.Label(c, numberStyle, GUILayout.Width(NumberWidth));
            if (money != null)
            {
                GUILayout.Label(money, moneyStyle ?? numberStyle, GUILayout.Width(MoneyWidth));
            }

            GUILayout.EndHorizontal();
        }

        private void BeginColumn(Rect rect, ref Vector2 scroll)
        {
            GUILayout.BeginArea(rect);
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
        }

        private static void EndColumn()
        {
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void HorizontalRule(float x, float y, float width, float thickness) =>
            GUI.DrawTexture(new Rect(x, y, width, thickness), _skin.InkTexture);

        private void VerticalRule(float x, float y, float height) =>
            GUI.DrawTexture(new Rect(x, y, 1f, height), _skin.InkTexture);

        private static string Signed(int change) => change > 0 ? $"+{change}" : change.ToString();

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

            return $"{seconds / 3600.0:0.#} hours";
        }
    }
}
