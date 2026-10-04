using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Townscape.CoffeeShop;
using Townscape.State;
using static Townscape.Tests.CoffeeShop.CoffeeShopTesting;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class SaveTests
    {
        private static CoffeeShopState Played()
        {
            var store = new Store<CoffeeShopState>(CoffeeShopReducer.Create(Balance), NewGame(5) with { CashPence = 50000 });
            store.Dispatch(new SetMilk(DefaultBalance.OatMilk));
            store.Dispatch(new SetReserve(2500));
            store.Dispatch(new BuyUpgrade(DefaultBalance.BiggerDisplay));
            store.Dispatch(new SetPlanned(DefaultBalance.Croissant, 20));
            store.Dispatch(new SetPlanned(DefaultBalance.Tea, 45));
            for (var day = 0; day < 3; day++)
            {
                store.Dispatch(new OpenForTheDay());
            }

            store.Dispatch(new SetOnMenu(DefaultBalance.Tea, false));
            return store.State;
        }

        private static CoffeeShopState Read(string text)
        {
            Assert.That(CoffeeShopSave.TryRead(text, Balance, out var state, out var problem), Is.True, problem);
            return state;
        }

        [Test]
        public void AGame_SurvivesSavingAndLoading()
        {
            var played = Played();

            var loaded = Read(CoffeeShopSave.Write(played));

            Assert.That(loaded, Is.EqualTo(played with { LastResult = null }));
            Assert.That(loaded.Pantry.Count, Is.GreaterThan(0), "leftover tea keeps");
            Assert.That(loaded.UpgradeEarnings.ContainsKey(DefaultBalance.BiggerDisplay), Is.True);
        }

        [Test]
        public void ALoadedGame_CarriesOnExactlyAsIfItHadNeverStopped()
        {
            var played = Played();
            var loaded = Read(CoffeeShopSave.Write(played));

            var continued = CoffeeShopReducer.Reduce(Balance, played, new OpenForTheDay());
            var resumed = CoffeeShopReducer.Reduce(Balance, loaded, new OpenForTheDay());

            Assert.That(resumed.LastResult, Is.EqualTo(continued.LastResult));
            Assert.That(resumed.CashPence, Is.EqualTo(continued.CashPence));
        }

        [Test]
        public void ALoadedGame_HasNoFullResults_ButKeepsTheHistory()
        {
            var traded = CoffeeShopReducer.Reduce(Balance, NewGame(), new OpenForTheDay());

            var loaded = Read(CoffeeShopSave.Write(traded));

            Assert.That(loaded.LastResult, Is.Null, "the full results aren't saved");
            Assert.That(loaded.History.Single(), Is.EqualTo(traded.LastResult.Summary));
            Assert.That(loaded.Day, Is.EqualTo(2));
        }

        [Test]
        public void AClosedShop_StaysClosed()
        {
            var closed = NewGame() with { Phase = ShopPhase.ClosedDown, CashPence = -9000 };

            Assert.That(Read(CoffeeShopSave.Write(closed)).Phase, Is.EqualTo(ShopPhase.ClosedDown));
        }

        [Test]
        public void TheSave_IsReadableJson()
        {
            var text = CoffeeShopSave.Write(Played());

            Assert.That(text, Does.Contain("\"format\": \"fellside-coffee\""));
            Assert.That(text, Does.Contain("\"milk\": \"oat\""));
            Assert.That(text, Does.Contain("\"bigger-display\": 1"));
        }

        [Test]
        public void MissingAndUnknownFields_FallBackOrAreDropped()
        {
            var text = "{ \"format\": \"fellside-coffee\", \"version\": 1, \"seed\": 3, \"day\": 4, \"cash\": \"lots\","
                + " \"milk\": \"goat\", \"menu\": [\"scone\", \"tea\", 7], \"planned\": { \"tea\": -2, \"scone\": 9 },"
                + " \"pantry\": { \"milk\": 5, \"tea-leaves\": 6 }, \"upgrades\": { \"rocket\": 2 }, \"history\": [ { \"day\": 3, \"revenue\": 900 }, 5 ],"
                + " \"somethingNew\": true }";

            var state = Read(text);

            Assert.That(state.Seed, Is.EqualTo(3));
            Assert.That(state.Day, Is.EqualTo(4));
            Assert.That(state.CashPence, Is.EqualTo(Balance.StartingCashPence));
            Assert.That(state.MilkId, Is.EqualTo(Balance.StartingMilkId));
            Assert.That(state.Menu, Is.EqualTo(new[] { DefaultBalance.Tea }));
            Assert.That(state.Planned, Is.EqualTo(ValueMap<int>.Empty.With(DefaultBalance.Tea, 0)));
            Assert.That(state.Pantry, Is.EqualTo(ValueMap<int>.Empty.With(DefaultBalance.TeaLeaves, 6)), "milk never keeps");
            Assert.That(state.Upgrades.Count, Is.Zero);
            Assert.That(state.History, Has.Count.EqualTo(1));
            Assert.That(state.History[0].RevenuePence, Is.EqualTo(900));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{ \"format\": \"fellside-coffee\", \"version\": 1")]
        [TestCase("[1, 2, 3]")]
        [TestCase("{ \"format\": \"something-else\", \"version\": 1 }")]
        [TestCase("{ \"format\": \"fellside-coffee\", \"version\": 99 }")]
        public void NotASave_IsRefusedWithAReason(string text)
        {
            Assert.That(CoffeeShopSave.TryRead(text, Balance, out var state, out var problem), Is.False);
            Assert.That(state, Is.Null);
            Assert.That(problem, Is.Not.Empty);
        }

        [Test]
        public void Json_RoundTripsValues()
        {
            var value = new JsonObject
            {
                ["text"] = "quote \" slash \\ newline \n tab \t bell \u0007 pound £",
                ["whole"] = -12345678901L,
                ["fraction"] = 0.1,
                ["yes"] = true,
                ["nothing"] = null,
                ["list"] = new List<object> { 1L, "two", new List<object>(), new JsonObject() },
            };

            var read = (JsonObject)Json.Read(Json.Write(value));

            Assert.That(read.Keys, Is.EqualTo(value.Keys));
            Assert.That(read["text"], Is.EqualTo(value["text"]));
            Assert.That(read["whole"], Is.EqualTo(-12345678901L));
            Assert.That(read["fraction"], Is.EqualTo(0.1));
            Assert.That(read["yes"], Is.EqualTo(true));
            Assert.That(read["nothing"], Is.Null);
            Assert.That(((List<object>)read["list"])[1], Is.EqualTo("two"));
        }

        [Test]
        public void Json_ReadsEscapesAndSpacing()
        {
            var read = (JsonObject)Json.Read(" {\"a\" :[ 1 ,2.5e1, \"\\u00e9\\/\" ] } ");

            Assert.That((List<object>)read["a"], Is.EqualTo(new object[] { 1L, 25.0, "é/" }));
            Assert.Throws<FormatException>(() => Json.Read("{\"a\": tru}"));
            Assert.Throws<FormatException>(() => Json.Read("{\"a\": 1} extra"));
        }
    }
}
