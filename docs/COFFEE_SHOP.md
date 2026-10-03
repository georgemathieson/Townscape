# Fellside Coffee

A small, cosy management game played in the village's coffee shop, inspired by *Tiny Bookshop*:
stock the shop, trade for a day, look at how it went, and put the profit into upgrades. This is the
**minimum playable slice**: one site, two kinds of customer, three things on the menu, one upgrade.

## Playing it

Press **C** (or **Run the coffee shop** on the control panel). The camera glides across the high
street to Fellside Coffee and the game's window takes the panel's place. Press **C** again, or
**Close**, to go back to the town; the game saves itself.

Each day has three steps:

1. **Prep.** See who usually comes in at each time of day. Choose the menu, how many croissants to
   bake and how many servings of each drink to stock for, and the milk. The shop works out the
   ingredients, what they cost and anything that can't be made. Then **Open for the day**.
2. **Trade.** The day runs in one go.
3. **Review.** Money in and out, what sold, what was wasted, what customers wanted and couldn't get
   (and why), how each kind of customer was served, how bad the queue got, and what each upgrade
   earned compared with the same day without it.

Between days, spend profit on **upgrades**. An upgrade works from the next day you open. Keep some
cash back with **Keep back**: upgrades can't spend it. A bad day can take cash below zero, but past
the overdraft the bank closes the shop.

## How it's built

```
Assets/Townscape/Scripts/CoffeeShop/   Townscape.CoffeeShop   engine-free: the whole game
  Balance/       every number: items, ingredients, milk, segments, time slots, sites, upgrades
  Collections/   ValueList / ValueMap: immutable, compare by contents (for records)
  Trading/       PrepPlan, Forecast, CustomerStream, TradingDay, DayResult
  Game/          CoffeeShopState, actions, CoffeeShopReducer
  Saving/        a small JSON reader and writer, CoffeeShopSave
  SeededRandom   SplitMix64, identical in Unity and .NET
Assets/Townscape/Scripts/Runtime/
  CoffeeShop/    CoffeeShopGame (store, load/save, camera), CoffeeShopSaveFile
  UI/            CoffeeShopWindow (IMGUI on the control panel's skin)
tools/coffee-sim/                      plays many games and reports what the numbers add up to
```

- **Game logic is pure and engine-free.** `Townscape.CoffeeShop` has `noEngineReferences`, like
  State and Simulation, so every rule runs under `dotnet test` and in the sim tool. The Unity side
  only shows state and dispatches actions.
- **Its own store.** `Store<CoffeeShopState>` with `CoffeeShopReducer`, separate from the town's
  store. Trading a day is a pure calculation from the state, so `OpenForTheDay` is an ordinary
  action.
- **Every number is data.** `CoffeeShopBalance` holds prices, costs, capacities, demand and
  upgrades; `DefaultBalance` has the shipped values. The rules contain no numbers of their own, and
  `Validate()` catches a balance that refers to something missing.
- **Reproducible.** Each day's customers come from `SeededRandom.DaySeed(gameSeed, day)`. The same
  game and day always bring the same customers, in tests, in the sim and after loading a save.
  `SeededRandom` uses integer maths only, because a seeded `System.Random` isn't guaranteed to
  match between Unity's runtime and .NET.
- **Customers are rolled before trading.** Nothing about a customer depends on the shop, so the
  review can replay the same customers without an upgrade to show what the upgrade was worth.
- **Money is whole pence** (`int`), so sums are exact. `Money.Format` shows it as pounds.

### Data model

| Type | What it is |
|---|---|
| `ItemDef` | On the menu: a **drink** (made to order from a recipe) or a **pastry** (baked in the morning, one per customer, wasted if unsold). Price, serve time, dairy-free (pastries). |
| `IngredientDef` | Bought in portions. Keeps overnight unless it spoils (milk). Milk is one ingredient whose kind the player chooses. |
| `MilkOption` | Dairy or oat: cost per portion, dairy-free, and appeal (the share of customers who are happy with it in a latte). |
| `SegmentDef` | A kind of customer: chance of wanting a drink and a pastry, tastes, share who can't have dairy, budget, patience, chance of settling for a second choice. |
| `SlotDef` / `SiteDef` | Part of the day (length, expected customers, segment mix, baristas) and where the shop trades (daily rent, its slots). |
| `UpgradeDef` | Kind (capacity, range, ambience, efficiency, reach), price, daily upkeep, effects, summary and trade-off. |
| `CoffeeShopState` | Seed, next day, phase (prep, review, closed down), cash, reserve, site, milk, menu, plan, pantry, owned upgrades (with the first day each works), each upgrade's earnings, the last 14 days' totals, the last day's full result. |
| `DayResult` | Customers, served, walked out; per item stocked, wanted, sold, sold as a second choice, wasted, missed by reason; per segment and per slot outcomes; the money; waste; the pantry after; each upgrade's impact. |
| Save (JSON) | Everything in the state except the last day's full result. Version 1. |

### The rules

**Prep (`PrepPlan`).** Pastries must fit in the display case and milk in the fridge, filled in menu
order; the reducer limits plans to what fits. Ingredients follow from the drinks: servings times the
recipe, less what's left in the pantry. Milk is priced by the chosen option. The plan flags any
menu item that can't be made, plans cut short by space, an empty menu, and stock costing more than
cash plus the overdraft (which keeps the shop shut). Rent and upkeep are charged at closing.

**Customers (`CustomerStream`).** A daily footfall swing (hidden from the player), then for each
slot a footfall count around its average. Each customer has a segment from the slot's mix, an
arrival time in the slot, and their own budget and patience around the segment's. They may want a
drink and a pastry, each a ranking by taste. Some can't have dairy, some won't take oat milk, and
some will settle for a second choice.

**Trading (`TradingDay`).** Customers are served in order of arrival by whichever barista is free
first. Anyone who would wait past their patience walks out. Otherwise the drink comes first, then
the pastry, from what's left of the budget. A customer gets their first choice if it's on the menu,
suits their diet, suits them with the shop's milk, fits their budget and is in stock. If not, they
may take their next choice; otherwise it's a miss, recorded against their first choice with the
first reason that applied. Serving takes each item's serve time plus time at the till. At closing,
unsold pastries are wasted, milk is poured away and coffee and tea keep.

**Money.** Takings less stock bought, cups and lids (per drink sold), rent and upgrade upkeep.

**Progression (`CoffeeShopReducer`).** Cash carries from day to day. Upgrades can only spend cash
above the reserve, and each is bought once. An upgrade works from the next day traded, never the
day already in the review. If cash ends a day below the overdraft, the bank closes the shop.

## Balance: the numbers and why

Run `dotnet run -c Release --project tools/coffee-sim` after changing anything in `DefaultBalance`.
It plays 200 seeded games of 30 days each with a few simple players.

| Number | Value | Why |
|---|---|---|
| Starting cash | £120 | A day or two of rent: enough to trade from day one, not enough to buy the upgrade straight away. |
| Overdraft | £50 | One bad day's cushion, about half a day's rent. Smaller than rent, so trading with nothing to sell closes the shop. |
| Rent (High Street) | £95 a day | Fixed costs are a café's real squeeze; at £95, good play makes about £60 a day and 3–6% of days lose money. |
| Display case | 12 pastries | Well under the ~29 croissants wanted on an average day, so the case is the obvious first limit. |
| Fridge | 60 portions of milk | Comfortably above latte demand (~34), so it isn't a limit yet; it's there for later upgrades. |
| Latte | £3.40, 70 s | High-street prices. The slowest thing to make, so it drives the morning queue. |
| Pot of tea | £2.40, 35 s | Cheaper and quicker; locals like it as much as lattes. |
| Croissant | £2.80, bought in at £1, 12 s | The perishable bet. At £1 over-baking costs real money: sell 3 to cover 7 wasted. |
| Coffee / tea | 28p / 9p a portion; keep | Drinks have high margins, as in real cafés; leftovers carry over, so stocking extra isn't a risk. |
| Dairy / oat milk | 22p / 38p; oat is dairy-free with 88% appeal | Oat serves dairy-free customers but costs more, and 12% won't take it. Dairy currently wins by about £5 a day with this crowd; a crowd with more dairy-free customers would flip it. |
| Cups, lids, ice | 14p a drink | Topped up automatically, as asked. |
| Time at the till | 15 s a customer | Makes the queue matter in the morning rush. |
| Commuters | drink 95%, pastry 35%, latte 3 : tea 1, 15% dairy-free, ~£6.50, 2.5 min patience, 40% take a second choice | In a hurry, coffee-led, walk out of a long queue. |
| Locals | drink 85%, pastry 55%, latte 1.5 : tea 1.5, 10% dairy-free, ~£8, 7 min patience, 70% take a second choice | Relaxed, pastry lovers, flexible. |
| Spreads | budget ±25%, patience ±30%, daily footfall ±30% | Individual and day-to-day variety, so stock is a bet under uncertainty. |
| Morning rush | 45 min, ~26 customers, 70% commuters | Busy enough that about 1.5 commuters a day walk out, which a faster machine could later fix. |
| Midday / afternoon | 2 h each, ~20 and ~16, mostly locals | Calm; pastry demand comes from here. |
| Bigger display case | £180, £3 a day, +14 pastries (to 26) | See below. |

What the sim shows (30 days, averaged over 200 games):

| Player | Profit a day | Losing days | Cash on day 30 |
|---|---|---|---|
| Keeps the starting plan, never upgrades | £39 | 3% | £1,294 |
| Stocks to the forecast (dairy) | £52 | 3% | £1,675 |
| Stocks to the forecast (oat) | £47 | 4% | £1,538 |
| Buys the case, bakes ~26 a day | £61 | 5% | £1,761 |
| Buys the case, bakes ~20 a day | £59 | 3% | £1,711 |
| Buys the case, bakes ~14 a day | £52 | 3% | £1,504 |

The upgrade is bought around day 4. It earns about **£10 a day if you bake to fill it** (+10
croissants sold, +4 wasted), paying back in about 18 days, and **almost nothing if you don't**. It
changes a decision (how many to bake on an uncertain day) rather than a number, and the review shows
which way the player went.

## Not sure about (please check)

- **Closing down is rare.** No sim player went under: once a shop has a few days' profit, one bad
  day can't reach the overdraft. With one upgrade there isn't much to overspend on. More upgrades
  (and running costs) should make the reserve matter more. Rent or the overdraft could also be
  tightened.
- **Upgrades bought before opening.** "From the next day" here means the next day traded, so an
  upgrade bought during prep works that same day (no day has been traded since it was bought). The
  alternative is to make it wait a further day.
- **Stock is cash accounting.** Coffee and tea bought today are a cost today even if they're used
  tomorrow, so a day that stocks up looks worse than it was.
- **Second choices** only come from a customer's own ranking of what their segment likes. Nobody
  switches from a drink to a pastry.
- **The queue** serves everyone already waiting at closing time.
- **The display case** holds pastries only; drinks need no display space.
- **The forecast** is the true average and is shown exactly. Real days vary by up to ±30% (the
  review says whether it was a quiet or a busy day).

## Next, once the slice has been played

- More upgrades, each unlocking a decision. A better machine (faster lattes against upkeep); cold
  drinks (a new item that needs fridge space); seating and music (customers stay and spend more,
  but the room fills up); a second milk (both dairy and oat, at the cost of fridge space); a
  pop-up at the station (a commuter-heavy site with its own rent).
- Syrups as a small upsell on drinks.
- More segments: students (oat-loving, tight budget), weekend walkers (big appetites, Saturdays).
- Regulars who remember their usual order (user story 9).
- Tying footfall to the town's weather: fewer walkers in a storm, more people sheltering.
