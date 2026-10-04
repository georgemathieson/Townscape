using System.Collections.Generic;
using NUnit.Framework;
using Townscape.CoffeeShop;

namespace Townscape.Tests.CoffeeShop
{
    public sealed class ValueCollectionTests
    {
        [Test]
        public void ValueList_ComparesByContents()
        {
            var a = ValueList<string>.From(new[] { "latte", "tea" });
            var b = ValueList<string>.Empty.Add("latte").Add("tea");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(ValueList<string>.From(new[] { "tea", "latte" })));
        }

        [Test]
        public void ValueList_TakeLast_KeepsTheNewest()
        {
            var list = ValueList<int>.From(new[] { 1, 2, 3, 4, 5 });

            Assert.That(list.TakeLast(2), Is.EqualTo(new[] { 4, 5 }));
            Assert.That(list.TakeLast(10), Is.SameAs(list));
        }

        [Test]
        public void ValueMap_KeepsKeysInOrder_AndComparesByContents()
        {
            var a = ValueMap<int>.Empty.With("tea", 2).With("latte", 1).With("milk", 3);
            var b = ValueMap<int>.From(new[]
            {
                new KeyValuePair<string, int>("milk", 3),
                new KeyValuePair<string, int>("latte", 1),
                new KeyValuePair<string, int>("tea", 2),
            });

            Assert.That(a.Keys, Is.EqualTo(new[] { "latte", "milk", "tea" }));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.With("tea", 9), Is.Not.EqualTo(b));
        }

        [Test]
        public void ValueMap_WithAndWithout()
        {
            var map = ValueMap<int>.Empty.With("a", 1).With("b", 2);

            Assert.That(map.Get("a"), Is.EqualTo(1));
            Assert.That(map.Get("missing", -1), Is.EqualTo(-1));
            Assert.That(map.With("a", 1), Is.SameAs(map), "setting the same value changes nothing");
            Assert.That(map.Without("a").Keys, Is.EqualTo(new[] { "b" }));
            Assert.That(map.Without("missing"), Is.SameAs(map));
            Assert.That(map.Without("a").Without("b"), Is.EqualTo(ValueMap<int>.Empty));
        }
    }
}
