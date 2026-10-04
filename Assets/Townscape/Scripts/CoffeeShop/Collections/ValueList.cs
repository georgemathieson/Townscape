using System;
using System.Collections;
using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// An immutable list that compares by its contents, so records holding one keep value equality
    /// (a plain list in a record compares by reference, and the store would see every copy as a change).
    /// </summary>
    public sealed class ValueList<T> : IReadOnlyList<T>, IEquatable<ValueList<T>>
    {
        public static readonly ValueList<T> Empty = new ValueList<T>(new T[0]);

        private readonly T[] _items;

        private ValueList(T[] items)
        {
            _items = items;
        }

        public int Count => _items.Length;

        public T this[int index] => _items[index];

        public static ValueList<T> From(IEnumerable<T> items)
        {
            var array = new List<T>(items ?? throw new ArgumentNullException(nameof(items))).ToArray();
            return array.Length == 0 ? Empty : new ValueList<T>(array);
        }

        public ValueList<T> Add(T item)
        {
            var array = new T[_items.Length + 1];
            Array.Copy(_items, array, _items.Length);
            array[_items.Length] = item;
            return new ValueList<T>(array);
        }

        /// <summary>The last <paramref name="count"/> items (all of them if there are fewer).</summary>
        public ValueList<T> TakeLast(int count)
        {
            if (count >= _items.Length)
            {
                return this;
            }

            var array = new T[Math.Max(0, count)];
            Array.Copy(_items, _items.Length - array.Length, array, 0, array.Length);
            return new ValueList<T>(array);
        }

        public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(ValueList<T> other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || other._items.Length != _items.Length)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < _items.Length; i++)
            {
                if (!comparer.Equals(_items[i], other._items[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as ValueList<T>);

        public override int GetHashCode()
        {
            var hash = 17;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in _items)
            {
                hash = unchecked((hash * 31) + (item is null ? 0 : comparer.GetHashCode(item)));
            }

            return hash;
        }

        public override string ToString() => "[" + string.Join(", ", _items) + "]";
    }
}
