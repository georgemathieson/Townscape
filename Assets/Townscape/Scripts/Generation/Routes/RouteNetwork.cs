using System;
using System.Collections.Generic;
using System.Numerics;

namespace Townscape.Generation.Routes
{
    /// <summary>One way on from a stop in a <see cref="RouteNetwork"/>.</summary>
    public readonly struct RouteLink
    {
        public RouteLink(int to, float length, string door)
        {
            To = to;
            Length = length;
            Door = door;
        }

        public int To { get; }

        public float Length { get; }

        /// <summary>The door this way goes through, if any.</summary>
        public string Door { get; }
    }

    /// <summary>A way from one stop to another, ready to walk or drive.</summary>
    public sealed class RoutePath
    {
        public RoutePath(IReadOnlyList<RouteStop> stops)
        {
            Stops = stops;
            for (var i = 1; i < stops.Count; i++)
            {
                Length += Vector3.Distance(stops[i - 1].Position, stops[i].Position);
            }
        }

        /// <summary>From where you start to where you're going, each with the door to go through to reach it.</summary>
        public IReadOnlyList<RouteStop> Stops { get; }

        public float Length { get; }

        public Vector3 End => Stops[Stops.Count - 1].Position;
    }

    /// <summary>
    /// The ways people (or cars) get about the town: stops joined by links, some through doors,
    /// with the shortest way between any two found by A*. Walking, the stops run along the
    /// pavements, lanes and footpaths and on through the buildings people go into; driving, they
    /// run along each side of every road, one way.
    /// </summary>
    public sealed class RouteNetwork
    {
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<string> _names = new List<string>();
        private readonly List<List<RouteLink>> _links = new List<List<RouteLink>>();
        private readonly Dictionary<string, int> _named = new Dictionary<string, int>();
        private readonly List<int> _entrances = new List<int>();
        private readonly List<int> _exits = new List<int>();
        private readonly List<bool> _indoors = new List<bool>();

        public int Count => _points.Count;

        /// <summary>Where people (or cars) come into the town from beyond its edge.</summary>
        public IReadOnlyList<int> Entrances => _entrances;

        /// <summary>Where they leave it.</summary>
        public IReadOnlyList<int> Exits => _exits;

        public Vector3 Point(int stop) => _points[stop];

        /// <summary>The stop's name, if it's somewhere people go on purpose.</summary>
        public string Name(int stop) => _names[stop];

        /// <summary>Whether the stop is inside a building rather than out on the street.</summary>
        public bool Indoors(int stop) => _indoors[stop];

        public IReadOnlyList<RouteLink> Links(int stop) => _links[stop];

        /// <summary>The stop with this name, or -1.</summary>
        public int Find(string name) => name != null && _named.TryGetValue(name, out var stop) ? stop : -1;

        /// <summary>The closest stop to <paramref name="position"/> (only outdoor ones if asked), or -1 if there are none.</summary>
        public int Nearest(Vector3 position, bool outdoorsOnly = false)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < _points.Count; i++)
            {
                if (outdoorsOnly && _indoors[i])
                {
                    continue;
                }

                var distance = Vector3.DistanceSquared(_points[i], position);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public RoutePath Path(string from, string to) => Path(Find(from), Find(to));

        /// <summary>The shortest way from one stop to another, or null if there's none.</summary>
        public RoutePath Path(int from, int to)
        {
            if (from < 0 || to < 0 || from >= Count || to >= Count)
            {
                return null;
            }

            var cost = new float[Count];
            var came = new int[Count];
            var door = new string[Count];
            var done = new bool[Count];
            for (var i = 0; i < Count; i++)
            {
                cost[i] = float.MaxValue;
                came[i] = -1;
            }

            var goal = _points[to];
            var open = new MinHeap();
            cost[from] = 0f;
            open.Push(from, Vector3.Distance(_points[from], goal));
            while (open.Count > 0)
            {
                var current = open.Pop();
                if (done[current])
                {
                    continue;
                }

                if (current == to)
                {
                    break;
                }

                done[current] = true;
                foreach (var link in _links[current])
                {
                    var next = cost[current] + link.Length;
                    if (next < cost[link.To])
                    {
                        cost[link.To] = next;
                        came[link.To] = current;
                        door[link.To] = link.Door;
                        open.Push(link.To, next + Vector3.Distance(_points[link.To], goal));
                    }
                }
            }

            if (from != to && came[to] < 0)
            {
                return null;
            }

            var stops = new List<RouteStop>();
            for (var stop = to; stop >= 0; stop = came[stop])
            {
                stops.Add(new RouteStop(_points[stop], _names[stop], stop == from ? null : door[stop]));
                if (stop == from)
                {
                    break;
                }
            }

            stops.Reverse();
            return new RoutePath(stops);
        }

        /// <summary>Adds a stop and returns its number. A named stop can be found again by its name.</summary>
        public int Add(Vector3 position, string name = null, bool indoors = false)
        {
            if (name != null && _named.TryGetValue(name, out var existing))
            {
                return existing;
            }

            _points.Add(position);
            _names.Add(name);
            _indoors.Add(indoors);
            _links.Add(new List<RouteLink>());
            if (name != null)
            {
                _named[name] = _points.Count - 1;
            }

            return _points.Count - 1;
        }

        /// <summary>Joins two stops: both ways for walking, one way (from <paramref name="a"/>) for driving.</summary>
        public void Link(int a, int b, string door = null, bool bothWays = true)
        {
            if (a == b || a < 0 || b < 0)
            {
                return;
            }

            var length = Vector3.Distance(_points[a], _points[b]);
            if (!Linked(a, b))
            {
                _links[a].Add(new RouteLink(b, length, door));
            }

            if (bothWays && !Linked(b, a))
            {
                _links[b].Add(new RouteLink(a, length, door));
            }
        }

        public bool Linked(int a, int b) => _links[a].Exists(l => l.To == b);

        public void AddEntrance(int stop)
        {
            if (!_entrances.Contains(stop))
            {
                _entrances.Add(stop);
            }
        }

        public void AddExit(int stop)
        {
            if (!_exits.Contains(stop))
            {
                _exits.Add(stop);
            }
        }

        // A small binary heap of stops by priority, lowest first.
        private sealed class MinHeap
        {
            private readonly List<(int Item, float Priority)> _items = new List<(int, float)>();

            public int Count => _items.Count;

            public void Push(int item, float priority)
            {
                _items.Add((item, priority));
                var i = _items.Count - 1;
                while (i > 0)
                {
                    var parent = (i - 1) / 2;
                    if (_items[parent].Priority <= _items[i].Priority)
                    {
                        break;
                    }

                    (_items[parent], _items[i]) = (_items[i], _items[parent]);
                    i = parent;
                }
            }

            public int Pop()
            {
                var top = _items[0].Item;
                var last = _items[_items.Count - 1];
                _items.RemoveAt(_items.Count - 1);
                if (_items.Count == 0)
                {
                    return top;
                }

                _items[0] = last;
                var i = 0;
                while (true)
                {
                    var left = (2 * i) + 1;
                    var right = left + 1;
                    var smallest = i;
                    if (left < _items.Count && _items[left].Priority < _items[smallest].Priority)
                    {
                        smallest = left;
                    }

                    if (right < _items.Count && _items[right].Priority < _items[smallest].Priority)
                    {
                        smallest = right;
                    }

                    if (smallest == i)
                    {
                        break;
                    }

                    (_items[smallest], _items[i]) = (_items[i], _items[smallest]);
                    i = smallest;
                }

                return top;
            }
        }
    }
}
