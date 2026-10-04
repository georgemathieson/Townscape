using NUnit.Framework;
using Townscape.CoffeeShop;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class BalanceTests
    {
        [Test]
        public void DefaultBalance_IsSound()
        {
            Assert.That(DefaultBalance.Create().Validate(), Is.Empty);
        }

        [Test]
        public void DefaultBalance_IsTheMinimumSlice()
        {
            var balance = DefaultBalance.Create();

            Assert.That(balance.Sites, Has.Count.EqualTo(1));
            Assert.That(balance.Items, Has.Count.EqualTo(3));
            Assert.That(balance.Upgrades, Has.Count.EqualTo(1));
            Assert.That(balance.IsMilky(balance.Item(DefaultBalance.Latte)), Is.True);
            Assert.That(balance.IsMilky(balance.Item(DefaultBalance.Tea)), Is.False);
        }

        [Test]
        public void EveryUpgrade_HasATradeOff()
        {
            foreach (var upgrade in DefaultBalance.Create().Upgrades)
            {
                Assert.That(upgrade.RunningCostPence, Is.GreaterThan(0), upgrade.Id);
                Assert.That(upgrade.TradeOff, Is.Not.Empty, upgrade.Id);
            }
        }

        [Test]
        public void Validate_FindsBrokenReferences()
        {
            var balance = DefaultBalance.Create() with
            {
                StartingSiteId = "moon",
                StartingMenu = new[] { "latte", "scone" },
                Upgrades = new[] { new UpgradeDef { Id = "free-lunch", CostPence = 0 } },
            };

            var problems = balance.Validate();

            Assert.That(problems, Has.Some.Contains("moon"));
            Assert.That(problems, Has.Some.Contains("scone"));
            Assert.That(problems, Has.Some.Contains("free-lunch is free"));
            Assert.That(problems, Has.Some.Contains("free-lunch has no trade-off"));
        }

        [Test]
        public void Money_FormatsPence()
        {
            Assert.That(Money.Format(340), Is.EqualTo("£3.40"));
            Assert.That(Money.Format(24000), Is.EqualTo("£240"));
            Assert.That(Money.Format(-1205), Is.EqualTo("-£12.05"));
            Assert.That(Money.Format(123456), Is.EqualTo("£1,234.56"));
            Assert.That(Money.Signed(500), Is.EqualTo("+£5"));
            Assert.That(Money.Signed(-5), Is.EqualTo("-£0.05"));
        }
    }
}
