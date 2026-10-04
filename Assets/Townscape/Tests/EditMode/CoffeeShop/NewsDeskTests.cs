using NUnit.Framework;
using Townscape.CoffeeShop;
using Townscape.State;
using static Townscape.Tests.CoffeeShop.CoffeeShopTesting;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class NewsDeskTests
    {
        // An ordinary day: the usual crowd, a profit, nothing sold out, no queue, no waste.
        private static DayResult Ordinary() => new DayResult
        {
            Day = 3,
            SiteId = DefaultBalance.HighStreet,
            MilkId = DefaultBalance.DairyMilk,
            Customers = 62,
            Served = 58,
            Items = ValueList<ItemOutcome>.From(new[]
            {
                new ItemOutcome(DefaultBalance.Latte, 36, 32, 31, 0, 0, Misses.None with { Dietary = 1 }),
                new ItemOutcome(DefaultBalance.Tea, 24, 22, 23, 2, 0, Misses.None),
                new ItemOutcome(DefaultBalance.Croissant, 12, 14, 12, 0, 0, Misses.None with { SoldOut = 2 }),
            }),
            Segments = ValueList<SegmentOutcome>.From(new[]
            {
                new SegmentOutcome(DefaultBalance.Commuters, 22, 0, 30, 25, 2, Misses.None, 7000),
                new SegmentOutcome(DefaultBalance.Locals, 40, 0, 52, 44, 3, Misses.None, 12000),
            }),
            Slots = ValueList<SlotOutcome>.From(new[]
            {
                new SlotOutcome("Morning rush", 22, 0, 90),
                new SlotOutcome("Midday", 20, 0, 30),
                new SlotOutcome("Afternoon", 20, 0, 0),
            }),
            RevenuePence = 19000,
            StockPence = 3000,
            SuppliesPence = 700,
            SiteCostPence = 9500,
            CashBeforePence = 10000,
        };

        private static NewsStory Lead(DayResult result, CoffeeShopState state = null) =>
            NewsDesk.Lead(Balance, (state ?? NewGame()) with { Day = result.Day + 1, LastResult = result });

        private static DayResult WithItem(DayResult result, int index, ItemOutcome item)
        {
            var items = new System.Collections.Generic.List<ItemOutcome>(result.Items) { [index] = item };
            return result with { Items = ValueList<ItemOutcome>.From(items) };
        }

        [Test]
        public void DayOne_WelcomesTheNewOwner()
        {
            var story = NewsDesk.Lead(Balance, NewGame());

            Assert.That(story.Headline, Is.EqualTo("New owner takes on Fellside Coffee"));
            Assert.That(story.Standfirst, Does.Contain("£120 in the till"));
            Assert.That(story.Paragraphs[0], Is.EqualTo("On the menu: lattes at £3.40, pots of tea at £2.40 and croissants at £2.80."));
        }

        [Test]
        public void DayOne_HasNotesForTheNewOwner()
        {
            var notes = NewsDesk.FirstDayNotes(Balance);

            Assert.That(notes, Has.Some.EqualTo("Coffee and tea keep for another day; milk is poured away at closing; unsold croissants go in the bin."));
            Assert.That(notes, Has.Some.EqualTo("Customers who can't have dairy need oat milk in a milky drink, and they can't have croissants. Not everyone likes oat milk, though."));
        }

        [Test]
        public void AnOrdinaryDay_IsSteadyTrade_WithTheMoneyInTheStandfirst()
        {
            var story = Lead(Ordinary());

            Assert.That(story.Headline, Is.EqualTo("Steady trade at Fellside Coffee"));
            Assert.That(story.Standfirst, Is.EqualTo("62 customers called in on day 3 and spent £190. After stock, cups, rent and upkeep, the shop made £58."));
        }

        [Test]
        public void TheStory_SaysWhatSoldAndWhoWentWithout()
        {
            var story = Lead(Ordinary());

            Assert.That(story.Paragraphs[0], Is.EqualTo(
                "31 lattes were sold; 1 customer who wanted one went without (1 can't have dairy). "
                + "23 pots of tea were sold. "
                + "All 12 croissants sold; 2 customers who wanted one went without (2 sold out)."));
            Assert.That(story.Paragraphs[1], Is.EqualTo("Commuters got 90% of what they came for; locals got 90%."));
        }

        [Test]
        public void ALoss_IsTheHeadline()
        {
            var result = Ordinary() with { RevenuePence = 9000 };

            Assert.That(Lead(result).Headline, Is.EqualTo("Fellside Coffee loses £42"));
            Assert.That(Lead(result with { Customers = 40 }).Headline, Is.EqualTo("Quiet day leaves Fellside Coffee £42 down"));
            Assert.That(Lead(result).Standfirst, Does.Contain("the shop lost £42"));
        }

        [Test]
        public void ANewUpgrade_IsNewsOnItsFirstDay()
        {
            var result = Ordinary() with
            {
                UpgradeImpacts = ValueList<UpgradeImpact>.From(new[] { new UpgradeImpact(DefaultBalance.BiggerDisplay, 1250, 8, 2, -8) }),
            };
            var firstDay = NewGame() with { Upgrades = ValueMap<int>.Empty.With(DefaultBalance.BiggerDisplay, 3) };
            var later = NewGame() with { Upgrades = ValueMap<int>.Empty.With(DefaultBalance.BiggerDisplay, 2) };

            var story = Lead(result, firstDay);

            Assert.That(story.Headline, Is.EqualTo("New bigger display case earns its keep"));
            Assert.That(story.Paragraphs, Has.Some.Contains("a difference of +£12.50: +8 sold and +2 wasted"));
            Assert.That(Lead(result, later).Headline, Is.EqualTo("Steady trade at Fellside Coffee"));
        }

        [Test]
        public void ABusyDay_ACrowd()
        {
            var story = Lead(Ordinary() with { Customers = 80 });

            Assert.That(story.Headline, Is.EqualTo("Crowds pack Fellside Coffee"));
            Assert.That(story.Standfirst, Does.EndWith("Usually about 62 come in."));
        }

        [Test]
        public void ASellOut_ThenTheQueue_ThenWaste()
        {
            var soldOut = WithItem(Ordinary(), 2, new ItemOutcome(DefaultBalance.Croissant, 12, 25, 12, 0, 0, Misses.None with { SoldOut = 13 }));
            var queue = Ordinary() with
            {
                Slots = ValueList<SlotOutcome>.From(new[] { new SlotOutcome("Morning rush", 30, 4, 200), new SlotOutcome("Midday", 20, 0, 30) }),
            };
            var waste = WithItem(Ordinary(), 2, new ItemOutcome(DefaultBalance.Croissant, 20, 14, 14, 0, 6, Misses.None));

            Assert.That(Lead(soldOut).Headline, Is.EqualTo("Croissants sell out at Fellside Coffee"));
            Assert.That(Lead(queue).Headline, Is.EqualTo("4 give up on the morning rush queue"));
            Assert.That(Lead(waste).Headline, Is.EqualTo("6 croissants go to waste"));
            Assert.That(Lead(waste).Paragraphs[0], Does.Contain("14 of 20 croissants sold, and 6 went in the bin."));
        }

        [Test]
        public void AQuietDay_WithAProfit()
        {
            Assert.That(Lead(Ordinary() with { Customers = 45 }).Headline, Is.EqualTo("Quiet day on the High Street"));
        }

        [Test]
        public void ALoadedGame_ReportsFromTheHistory()
        {
            var state = NewGame() with
            {
                Day = 5,
                History = ValueList<DaySummary>.From(new[] { new DaySummary(4, 60, 66, 1, 7, 18000, 12000, 30000) }),
            };

            var story = NewsDesk.Lead(Balance, state);

            Assert.That(story.Headline, Is.EqualTo("Fellside Coffee opens for day 5"));
            Assert.That(story.Standfirst, Is.EqualTo("On day 4 the shop sold 66, wasted 1 and missed 7, and made £60."));
        }

        [Test]
        public void ClosingDown_IsTheOnlyNews()
        {
            var state = NewGame() with { Day = 6, Phase = ShopPhase.ClosedDown, CashPence = -6200, LastResult = Ordinary() with { Day = 5 } };

            var story = NewsDesk.Lead(Balance, state);

            Assert.That(story.Headline, Is.EqualTo("Bank closes Fellside Coffee"));
            Assert.That(story.Standfirst, Is.EqualTo("After 5 days of trading, the shop's cash fell to -£62, past its £50 overdraft."));
        }

        [Test]
        public void EveryRealDay_GetsAStory()
        {
            var store = new Store<CoffeeShopState>(CoffeeShopReducer.Create(Balance), NewGame(3));
            for (var day = 0; day < 20; day++)
            {
                store.Dispatch(new OpenForTheDay());
                var story = NewsDesk.Lead(Balance, store.State);
                Assert.That(story.Headline, Is.Not.Empty);
                Assert.That(story.Paragraphs, Is.Not.Empty);
                Assert.That(story.Paragraphs, Has.None.Contains("{"));
            }
        }

        [Test]
        public void Reasons_ListOnlyWhatHappened()
        {
            var missed = Misses.None with { SoldOut = 13, Dietary = 3, MilkChoice = 2 };

            Assert.That(NewsDesk.Reasons(missed, "oat milk"), Is.EqualTo("13 sold out, 3 can't have dairy, 2 didn't want oat milk"));
            Assert.That(NewsDesk.Reasons(Misses.None, "oat milk"), Is.Empty);
        }
    }
}
