using System;
using System.Collections;
using System.Collections.Generic;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// An immutable map from ids to values that compares by its contents. Keys are kept in ordinal
    /// order, so walking one (and saving it) always gives the same order.
    /// </summary>
    public sealed class ValueMap<T> : IReadOnlyCollection<KeyValuePair<string, T>>, IEquatable<ValueMap<T>>
    {
        public static readonly ValueMap<T> Empty = new ValueMap<T>(new string[0], new T[0]);

        private readonly string[] _keys;
        private readonly T[] _values;

        private ValueMap(string[] keys, T[] values)
        {
            _keys = keys;
            _values = values;
        }

        public int Count => _keys.Length;

        public IEnumerable<string> Keys => _keys;

        public static ValueMap<T> From(IEnumerable<KeyValuePair<string, T>> pairs)
        {
            var map = Empty;
            foreach (var pair in pairs ?? throw new ArgumentNullException(nameof(pairs)))
            {
                map = map.With(pair.Key, pair.Value);
            }

            return map;
        }

        public bool ContainsKey(string key) => IndexOf(key) >= 0;

        /// <summary>The value for <paramref name="key"/>, or <paramref name="fallback"/> if there isn't one.</summary>
        public T Get(string key, T fallback = default)
        {
            var index = IndexOf(key);
            return index >= 0 ? _values[index] : fallback;
        }

        public ValueMap<T> With(string key, T value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var index = IndexOf(key);
            if (index >= 0)
            {
                if (EqualityComparer<T>.Default.Equals(_values[index], value))
                {
                    return this;
                }

                var values = (T[])_values.Clone();
                values[index] = value;
                return new ValueMap<T>(_keys, values);
            }

            var insert = ~index;
            var newKeys = new string[_keys.Length + 1];
            var newValues = new T[_values.Length + 1];
            Array.Copy(_keys, newKeys, insert);
            Array.Copy(_values, newValues, insert);
            newKeys[insert] = key;
            newValues[insert] = value;
            Array.Copy(_keys, insert, newKeys, insert + 1, _keys.Length - insert);
            Array.Copy(_values, insert, newValues, insert + 1, _values.Length - insert);
            return new ValueMap<T>(newKeys, newValues);
        }

        public ValueMap<T> Without(string key)
        {
            var index = IndexOf(key);
            if (index < 0)
            {
                return this;
            }

            if (_keys.Length == 1)
            {
                return Empty;
            }

            var newKeys = new string[_keys.Length - 1];
            var newValues = new T[_values.Length - 1];
            Array.Copy(_keys, newKeys, index);
            Array.Copy(_values, newValues, index);
            Array.Copy(_keys, index + 1, newKeys, index, _keys.Length - index - 1);
            Array.Copy(_values, index + 1, newValues, index, _values.Length - index - 1);
            return new ValueMap<T>(newKeys, newValues);
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            for (var i = 0; i < _keys.Length; i++)
            {
                yield return new KeyValuePair<string, T>(_keys[i], _values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(ValueMap<T> other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || other._keys.Length != _keys.Length)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] != other._keys[i] || !comparer.Equals(_values[i], other._values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as ValueMap<T>);

        public override int GetHashCode()
        {
            var hash = 17;
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < _keys.Length; i++)
            {
                hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(_keys[i]));
                hash = unchecked((hash * 31) + (_values[i] is null ? 0 : comparer.GetHashCode(_values[i])));
            }

            return hash;
        }

        public override string ToString()
        {
            var parts = new string[_keys.Length];
            for (var i = 0; i < _keys.Length; i++)
            {
                parts[i] = _keys[i] + "=" + _values[i];
            }

            return "{" + string.Join(", ", parts) + "}";
        }

        private int IndexOf(string key) => key == null ? -1 : Array.BinarySearch(_keys, key, StringComparer.Ordinal);
    }
}
