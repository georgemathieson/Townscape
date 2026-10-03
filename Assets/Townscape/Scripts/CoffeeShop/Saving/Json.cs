using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Townscape.CoffeeShop
{
    /// <summary>
    /// Just enough JSON for the save file, so the game needs no library. Values are plain objects:
    /// <see cref="JsonObject"/>, <see cref="List{T}"/> of object, string, long, double, bool or null.
    /// </summary>
    public static class Json
    {
        /// <summary>Indented JSON for <paramref name="value"/>.</summary>
        public static string Write(object value)
        {
            var text = new StringBuilder();
            Write(text, value, 0);
            text.Append('\n');
            return text.ToString();
        }

        /// <summary>The value in <paramref name="text"/>. Throws <see cref="FormatException"/> if it isn't JSON.</summary>
        public static object Read(string text)
        {
            var reader = new Reader(text ?? throw new FormatException("No text."));
            var value = reader.Value();
            reader.SkipSpace();
            if (!reader.AtEnd)
            {
                throw reader.Error("Unexpected text after the value");
            }

            return value;
        }

        private static void Write(StringBuilder text, object value, int depth)
        {
            switch (value)
            {
                case null:
                    text.Append("null");
                    break;
                case bool flag:
                    text.Append(flag ? "true" : "false");
                    break;
                case string s:
                    WriteString(text, s);
                    break;
                case int or long:
                    text.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                    break;
                case double or float:
                    var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    text.Append(double.IsNaN(number) || double.IsInfinity(number) ? "null" : number.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case JsonObject obj:
                    WriteBlock(text, '{', '}', obj.Count, depth, i =>
                    {
                        WriteString(text, obj.Keys[i]);
                        text.Append(": ");
                        Write(text, obj[obj.Keys[i]], depth + 1);
                    });
                    break;
                case System.Collections.IList list:
                    WriteBlock(text, '[', ']', list.Count, depth, i => Write(text, list[i], depth + 1));
                    break;
                default:
                    throw new ArgumentException($"Can't write a {value.GetType().Name} as JSON.");
            }
        }

        private static void WriteBlock(StringBuilder text, char open, char close, int count, int depth, Action<int> writeItem)
        {
            text.Append(open);
            if (count == 0)
            {
                text.Append(close);
                return;
            }

            for (var i = 0; i < count; i++)
            {
                text.Append(i == 0 ? "\n" : ",\n");
                text.Append(' ', (depth + 1) * 2);
                writeItem(i);
            }

            text.Append('\n').Append(' ', depth * 2).Append(close);
        }

        private static void WriteString(StringBuilder text, string s)
        {
            text.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    case '\n': text.Append("\\n"); break;
                    case '\r': text.Append("\\r"); break;
                    case '\t': text.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            text.Append(c);
                        }

                        break;
                }
            }

            text.Append('"');
        }

        private sealed class Reader
        {
            private readonly string _text;
            private int _at;

            public Reader(string text)
            {
                _text = text;
            }

            public bool AtEnd => _at >= _text.Length;

            public FormatException Error(string message) => new FormatException($"{message} at character {_at}.");

            public void SkipSpace()
            {
                while (!AtEnd && char.IsWhiteSpace(_text[_at]))
                {
                    _at++;
                }
            }

            public object Value()
            {
                SkipSpace();
                if (AtEnd)
                {
                    throw Error("Expected a value");
                }

                switch (_text[_at])
                {
                    case '{': return Object();
                    case '[': return Array();
                    case '"': return String();
                    case 't': Word("true"); return true;
                    case 'f': Word("false"); return false;
                    case 'n': Word("null"); return null;
                    default: return Number();
                }
            }

            private JsonObject Object()
            {
                var obj = new JsonObject();
                _at++;
                SkipSpace();
                if (Take('}'))
                {
                    return obj;
                }

                do
                {
                    SkipSpace();
                    if (AtEnd || _text[_at] != '"')
                    {
                        throw Error("Expected a name in quotes");
                    }

                    var key = String();
                    SkipSpace();
                    Expect(':');
                    obj[key] = Value();
                    SkipSpace();
                }
                while (Take(','));

                Expect('}');
                return obj;
            }

            private List<object> Array()
            {
                var list = new List<object>();
                _at++;
                SkipSpace();
                if (Take(']'))
                {
                    return list;
                }

                do
                {
                    list.Add(Value());
                    SkipSpace();
                }
                while (Take(','));

                Expect(']');
                return list;
            }

            private string String()
            {
                _at++;
                var text = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                    {
                        throw Error("Unfinished string");
                    }

                    var c = _text[_at++];
                    if (c == '"')
                    {
                        return text.ToString();
                    }

                    if (c != '\\')
                    {
                        text.Append(c);
                        continue;
                    }

                    if (AtEnd)
                    {
                        throw Error("Unfinished escape");
                    }

                    var escape = _text[_at++];
                    switch (escape)
                    {
                        case '"': case '\\': case '/': text.Append(escape); break;
                        case 'b': text.Append('\b'); break;
                        case 'f': text.Append('\f'); break;
                        case 'n': text.Append('\n'); break;
                        case 'r': text.Append('\r'); break;
                        case 't': text.Append('\t'); break;
                        case 'u':
                            if (_at + 4 > _text.Length || !int.TryParse(_text.Substring(_at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                            {
                                throw Error("Bad \\u escape");
                            }

                            text.Append((char)code);
                            _at += 4;
                            break;
                        default:
                            throw Error($"Unknown escape \\{escape}");
                    }
                }
            }

            private object Number()
            {
                var start = _at;
                while (!AtEnd && "+-0123456789.eE".IndexOf(_text[_at]) >= 0)
                {
                    _at++;
                }

                var token = _text.Substring(start, _at - start);
                if (long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole))
                {
                    return whole;
                }

                if (token.Length > 0 && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return number;
                }

                _at = start;
                throw Error("Expected a value");
            }

            private void Word(string word)
            {
                if (string.CompareOrdinal(_text, _at, word, 0, word.Length) != 0)
                {
                    throw Error($"Expected {word}");
                }

                _at += word.Length;
            }

            private bool Take(char c)
            {
                if (!AtEnd && _text[_at] == c)
                {
                    _at++;
                    return true;
                }

                return false;
            }

            private void Expect(char c)
            {
                if (!Take(c))
                {
                    throw Error($"Expected '{c}'");
                }
            }
        }
    }

    /// <summary>A JSON object that keeps its names in the order they were added.</summary>
    public sealed class JsonObject
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly List<string> _keys = new List<string>();

        public IReadOnlyList<string> Keys => _keys;

        public int Count => _keys.Count;

        public object this[string key]
        {
            get => _values.TryGetValue(key, out var value) ? value : null;
            set
            {
                if (!_values.ContainsKey(key))
                {
                    _keys.Add(key);
                }

                _values[key] = value;
            }
        }

        public bool Has(string key) => _values.ContainsKey(key);
    }
}
