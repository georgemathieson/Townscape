using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;
using Townscape.Generation.Structures;

namespace Townscape.Generation.Routes
{
    /// <summary>
    /// Lays out the town's <see cref="RouteNetwork"/>s. Walking: along both pavements of the high
    /// street, along both edges of each lane (they have no pavements), down the middle of each
    /// footpath, across each road now and then, joined end to end where they meet, and on into
    /// every building with an alarm by the routes its interior gives. Driving: along each side of
    /// every road, keeping left, with turns into the lanes and places to turn round.
    /// </summary>
    public static class RouteNetworkBuilder
    {
        /// <summary>How far apart the stops along a pavement or footpath are.</summary>
        public const float WalkSpacing = 2f;

        public const float DriveSpacing = 4f;

        /// <summary>How far in from a lane's edge people walk.</summary>
        public const float LaneEdge = 0.6f;

        /// <summary>How far from the edge of the town (from the bridge) a dead end has to be to be a way in or out.</summary>
        public const float EdgeOfTown = 70f;

        private const float JoinReach = 9f;
        private const float SiteReach = 15f;
        private const float PavementCrossingEvery = 30f;
        private const float LaneCrossingEvery = 12f;
        private const float TurnRoundEvery = 40f;
        private const float BridgeApproach = 2.2f;

        // How far to keep from a building's walls, clear of its sills, steps and door hoods.
        private const float BuildingClearance = 0.3f;

        /// <summary>
        /// The height of whatever you'd stand on at <paramref name="p"/>: a bridge's deck, the
        /// ground in the village, or the fells beyond it.
        /// </summary>
        public static float SurfaceHeight(TownContext context, Vector2 p)
        {
            foreach (var bridge in context.Layout.Bridges)
            {
                if (OnDeck(bridge, p, out var u))
                {
                    return new HumpbackBridgeBuilder(bridge).DeckHeight(u);
                }
            }

            var core = context.Settings.CoreHalfExtent;
            return MathF.Abs(p.X) < core && MathF.Abs(p.Y) < core ? context.Ground.HeightAt(p) : context.Terrain.FarHeightAt(p);
        }

        public static RouteNetwork Walking(TownContext context, IEnumerable<TownSite> sites)
        {
            var network = new RouteNetwork();
            var runs = new List<List<int>>();
            foreach (var road in context.Layout.Roads)
            {
                // The pavements stop at the river; the way over is along the bridge's deck, below.
                var offset = road.PavementWidth > 0f ? road.HalfWidth + (road.PavementWidth * 0.5f) : road.HalfWidth - LaneEdge;
                var left = Sample(context, network, road.Centre, along => road.PointAt(along, offset), WalkSpacing);
                var right = Sample(context, network, road.Centre, along => road.PointAt(along, -offset), WalkSpacing);
                runs.AddRange(Runs(network, left, bothWays: true));
                runs.AddRange(Runs(network, right, bothWays: true));

                // Across the road every so often, and at each end.
                var every = Math.Max(1, (int)MathF.Round((road.PavementWidth > 0f ? PavementCrossingEvery : LaneCrossingEvery) / WalkSpacing));
                for (var i = 0; i < left.Length; i++)
                {
                    if ((i % every == 0 || i == left.Length - 1) && left[i] >= 0 && right[i] >= 0 && Clear(context, network.Point(left[i]), network.Point(right[i])))
                    {
                        network.Link(left[i], right[i]);
                    }
                }
            }

            // Over each bridge, along either edge of its deck (it has no pavements), from a little
            // short of one end to a little past the other so the ends come in inside the pillars.
            foreach (var bridge in context.Layout.Bridges)
            {
                var reach = bridge.HalfLength + BridgeApproach;
                var line = new Polyline(bridge.Centre - (bridge.Direction * reach), bridge.Centre + (bridge.Direction * reach));
                foreach (var side in new[] { -1f, 1f })
                {
                    var across = GeoMath.Left(bridge.Direction) * (side * (bridge.RoadHalfWidth - 0.5f));
                    var stops = Sample(context, network, line, along => line.PointAt(along) + across, WalkSpacing);
                    runs.AddRange(Runs(network, stops, bothWays: true));
                }
            }

            foreach (var path in context.Layout.Paths)
            {
                var stops = Sample(context, network, path.Centre, along => path.Centre.PointAt(along), WalkSpacing);
                runs.AddRange(Runs(network, stops, bothWays: true));
            }

            // Join each run's ends to whatever other run is nearest: lanes on to the high street,
            // footpaths on to the lanes and pavements, the bridge on to the pavements. Ends that
            // join nothing, out at the edge of the town, are where people come and go.
            var street = network.Count;
            var runOf = new int[street];
            for (var r = 0; r < runs.Count; r++)
            {
                foreach (var stop in runs[r])
                {
                    runOf[stop] = r;
                }
            }

            foreach (var run in runs)
            {
                foreach (var end in new[] { run[0], run[run.Count - 1] })
                {
                    var target = Nearest(network, network.Point(end), street, s => runOf[s] != runOf[end] && Clear(context, network.Point(end), network.Point(s)), JoinReach);
                    if (target >= 0)
                    {
                        network.Link(end, target);
                    }
                    else if (Flat(network.Point(end)).Length() > EdgeOfTown)
                    {
                        network.AddEntrance(end);
                        network.AddExit(end);
                    }
                }
            }

            foreach (var site in sites ?? Enumerable.Empty<TownSite>())
            {
                foreach (var route in site.Routes)
                {
                    AddRoute(context, network, route, street);
                }
            }

            return network;
        }

        public static RouteNetwork Driving(TownContext context)
        {
            var network = new RouteNetwork();
            var lines = new List<(RoadSpec Road, int[] Forward, int[] Back)>();
            foreach (var road in context.Layout.Roads)
            {
                // Keep left: the forward lane is on the left of the centreline.
                var half = road.HalfWidth * 0.5f;
                var forward = Sample(context, network, road.Centre, along => road.PointAt(along, half), DriveSpacing);
                var back = Sample(context, network, road.Centre, along => road.PointAt(along, -half), DriveSpacing);
                for (var i = 0; i + 1 < forward.Length; i++)
                {
                    network.Link(forward[i], forward[i + 1], bothWays: false);
                    network.Link(back[i + 1], back[i], bothWays: false);
                }

                // Places to turn round: at each end, and every so often along the way.
                var every = Math.Max(1, (int)MathF.Round(TurnRoundEvery / DriveSpacing));
                for (var i = 0; i < forward.Length; i++)
                {
                    if (i % every == 0 || i == forward.Length - 1)
                    {
                        network.Link(forward[i], back[i], bothWays: false);
                        network.Link(back[i], forward[i], bothWays: false);
                    }
                }

                // Roads that run off the edge of the town are where cars come and go.
                if (Flat(network.Point(forward[0])).Length() > EdgeOfTown)
                {
                    network.AddEntrance(forward[0]);
                    network.AddExit(back[0]);
                }

                var last = forward.Length - 1;
                if (Flat(network.Point(forward[last])).Length() > EdgeOfTown)
                {
                    network.AddEntrance(back[last]);
                    network.AddExit(forward[last]);
                }

                lines.Add((road, forward, back));
            }

            // Lanes start on another road's centreline: turn in from that road's lanes just
            // before the junction, and out on to them just after it.
            foreach (var lane in lines)
            {
                var start = lane.Road.Centre.PointAt(0f);
                foreach (var main in lines.Where(l => l.Road != lane.Road && l.Road.Centre.Closest(start).Distance < l.Road.HalfWidth))
                {
                    var k = NearestIndex(network, main.Forward, start);
                    network.Link(main.Forward[Math.Max(0, k - 1)], lane.Forward[0], bothWays: false);
                    network.Link(lane.Back[0], main.Forward[Math.Min(main.Forward.Length - 1, k + 1)], bothWays: false);
                    k = NearestIndex(network, main.Back, start);
                    network.Link(main.Back[Math.Min(main.Back.Length - 1, k + 1)], lane.Forward[0], bothWays: false);
                    network.Link(lane.Back[0], main.Back[Math.Max(0, k - 1)], bothWays: false);
                }
            }

            return network;
        }

        // ---- Laying out the stops --------------------------------------------------------------

        // Stops every `spacing` along a line, wherever `place` puts them, -1 where there's nowhere to stand.
        private static int[] Sample(TownContext context, RouteNetwork network, Polyline line, Func<float, Vector2> place, float spacing)
        {
            var count = Math.Max(1, (int)MathF.Ceiling(line.Length / spacing));
            var stops = new int[count + 1];
            for (var i = 0; i <= count; i++)
            {
                var flat = place(Math.Min(line.Length, i * spacing));
                stops[i] = Standable(context, flat) ? network.Add(new Vector3(flat.X, SurfaceHeight(context, flat), flat.Y)) : -1;
            }

            return stops;
        }

        // The stretches of stops with nowhere missing, each joined up stop to stop.
        private static IEnumerable<List<int>> Runs(RouteNetwork network, int[] stops, bool bothWays)
        {
            var run = new List<int>();
            foreach (var stop in stops)
            {
                if (stop < 0)
                {
                    if (run.Count > 0)
                    {
                        yield return run;
                    }

                    run = new List<int>();
                    continue;
                }

                if (run.Count > 0)
                {
                    network.Link(run[run.Count - 1], stop, bothWays: bothWays);
                }

                run.Add(stop);
            }

            if (run.Count > 0)
            {
                yield return run;
            }
        }

        // A building's route: its stops, each joined to the next through its door, and the first
        // joined to the nearest pavement if it's outside.
        private static void AddRoute(TownContext context, RouteNetwork network, TownRoute route, int street)
        {
            var previous = -1;
            for (var i = 0; i < route.Stops.Count; i++)
            {
                var stop = route.Stops[i];
                var outside = route.JoinsStreet && i == 0;
                var position = stop.Position;
                if (outside)
                {
                    position = new Vector3(position.X, SurfaceHeight(context, Flat(position)), position.Z);
                }

                var node = stop.Name != null ? network.Add(position, stop.Name, indoors: !outside) : Existing(network, position, street);
                if (node < 0)
                {
                    node = network.Add(position, indoors: !outside);
                }

                if (previous >= 0)
                {
                    network.Link(previous, node, stop.Door);
                }

                if (outside)
                {
                    var pavement = Nearest(network, position, street, s => Clear(context, position, network.Point(s)), SiteReach);
                    network.Link(node, pavement);
                }

                previous = node;
            }
        }

        // An unnamed stop already in a building at (very nearly) this place, so routes can share one.
        private static int Existing(RouteNetwork network, Vector3 position, int street)
        {
            for (var i = street; i < network.Count; i++)
            {
                if (Vector3.DistanceSquared(network.Point(i), position) < 0.0025f)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int Nearest(RouteNetwork network, Vector3 position, int count, Func<int, bool> allowed, float reach)
        {
            var best = -1;
            var bestDistance = reach * reach;
            for (var i = 0; i < count; i++)
            {
                var distance = Vector3.DistanceSquared(network.Point(i), position);
                if (distance < bestDistance && allowed(i))
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static int NearestIndex(RouteNetwork network, int[] stops, Vector2 p)
        {
            var best = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < stops.Length; i++)
            {
                var distance = Vector2.DistanceSquared(Flat(network.Point(stops[i])), p);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }

        // ---- Where you can stand, and walk straight across --------------------------------------

        // Somewhere to stand: on a bridge's deck, or on dry ground outside every building.
        private static bool Standable(TownContext context, Vector2 p)
        {
            foreach (var bridge in context.Layout.Bridges)
            {
                if (OnDeck(bridge, p, out _))
                {
                    return true;
                }
            }

            var kind = context.Ground.Classify(p).Kind;
            if (kind == RegionKind.RiverBed || kind == RegionKind.RiverBank || kind == RegionKind.Hole)
            {
                return false;
            }

            return !context.Buildings.Any(b => b.Footprint.Contains(p, BuildingClearance));
        }

        /// <summary>
        /// Whether you can walk straight from <paramref name="a"/> to <paramref name="b"/>:
        /// on somewhere to stand all the way, and not over a bridge's parapet.
        /// </summary>
        public static bool Clear(TownContext context, Vector3 a, Vector3 b)
        {
            var from = Flat(a);
            var to = Flat(b);
            var steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(from, to) / 0.4f));
            var wasOnDeck = OnAnyDeck(context, from, out _);
            for (var i = 1; i <= steps; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)steps);
                var onDeck = OnAnyDeck(context, p, out var bridge);
                if (onDeck != wasOnDeck)
                {
                    // On or off a bridge only over its ends, never its sides.
                    var probe = onDeck ? p : Vector2.Lerp(from, to, (i - 1) / (float)steps);
                    if (bridge == null)
                    {
                        OnAnyDeck(context, probe, out bridge);
                    }

                    if (bridge != null && MathF.Abs(Vector2.Dot(probe - bridge.Centre, bridge.Direction)) < bridge.HalfLength - 0.5f)
                    {
                        return false;
                    }
                }

                wasOnDeck = onDeck;
                if (i < steps && (!Standable(context, p) || context.Layout.Bridges.Any(b => HumpbackBridgeBuilder.NearPillar(b, p, 0.35f))))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool OnAnyDeck(TownContext context, Vector2 p, out BridgeSpec on)
        {
            foreach (var bridge in context.Layout.Bridges)
            {
                if (OnDeck(bridge, p, out _))
                {
                    on = bridge;
                    return true;
                }
            }

            on = null;
            return false;
        }

        private static bool OnDeck(BridgeSpec bridge, Vector2 p, out float u)
        {
            var offset = p - bridge.Centre;
            u = Vector2.Dot(offset, bridge.Direction);
            var v = Vector2.Dot(offset, GeoMath.Left(bridge.Direction));
            return MathF.Abs(u) <= bridge.HalfLength && MathF.Abs(v) <= bridge.FootprintHalfWidth;
        }

        private static Vector2 Flat(Vector3 p) => new Vector2(p.X, p.Z);
    }
}
