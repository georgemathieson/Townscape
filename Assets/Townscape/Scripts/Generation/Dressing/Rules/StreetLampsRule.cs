using System;
using System.Collections.Generic;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Ground;
using Townscape.Generation.Layout;
using Townscape.Generation.Markings;

namespace Townscape.Generation.Dressing.Rules
{
    /// <summary>
    /// Lamp posts at regular intervals along a road, staggered on both sides of the high street or
    /// on one verge of a lane. Spots that would land on a junction, a crossing or someone's front
    /// yard are skipped.
    /// </summary>
    public sealed class StreetLampsRule : IDressingRule
    {
        private readonly RoadSpec _road;
        private readonly float _spacing;
        private readonly bool _bothSides;
        private readonly bool _baskets;

        public StreetLampsRule(RoadSpec road, float spacing, bool bothSides, bool baskets)
        {
            _road = road;
            _spacing = spacing;
            _bothSides = bothSides;
            _baskets = baskets;
        }

        public void Apply(DressingContext context, Random random)
        {
            var onPavement = _road.PavementWidth > 0f;
            var offset = onPavement ? _road.HalfWidth + 0.4f : _road.HalfWidth + 0.8f;
            var allowed = onPavement ? new[] { RegionKind.Pavement } : new[] { RegionKind.OpenGround };
            var keepClear = new List<DistanceRange>();
            foreach (var marking in _road.Markings)
            {
                if (marking is ZebraCrossingMarking zebra)
                {
                    // The beacons light the crossing itself; lamps may stand by the zig-zags.
                    keepClear.Add(new DistanceRange(zebra.Along - 4f, zebra.Along + 4f));
                }
            }

            var index = 0;
            foreach (var side in _bothSides ? new[] { 1f, -1f } : new[] { 1f })
            {
                var start = side > 0f ? _spacing * 0.5f : _spacing;
                for (var along = start; along < _road.Centre.Length - 2f; along += _spacing)
                {
                    if (IsKeptClear(keepClear, along))
                    {
                        continue;
                    }

                    var position = _road.PointAt(along, side * offset);
                    if (!context.IsClear(position, 0.3f, allowed))
                    {
                        continue;
                    }

                    var facing = _road.PointAt(along, 0f) - position;
                    context.Place(new LampPost(_baskets && index++ % 2 == 0), position, facing, DressingLayer.Furniture, random);
                }
            }
        }

        private static bool IsKeptClear(List<DistanceRange> ranges, float along)
        {
            foreach (var range in ranges)
            {
                if (range.Overlaps(along - 0.5f, along + 0.5f))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>A Belisha beacon at each kerb beside every zebra crossing on a road.</summary>
    public sealed class BelishaBeaconsRule : IDressingRule
    {
        private readonly RoadSpec _road;

        public BelishaBeaconsRule(RoadSpec road)
        {
            _road = road;
        }

        public void Apply(DressingContext context, Random random)
        {
            foreach (var marking in _road.Markings)
            {
                if (!(marking is ZebraCrossingMarking zebra))
                {
                    continue;
                }

                foreach (var side in new[] { 1f, -1f })
                {
                    var along = zebra.Along + (side * ((ZebraCrossingMarking.CrossingWidth * 0.5f) + 0.35f));
                    var position = _road.PointAt(along, side * (_road.HalfWidth + 0.35f));
                    context.Place(new BelishaBeacon(), position, _road.PointAt(along, 0f) - position, DressingLayer.Furniture, random);
                }
            }
        }
    }

    /// <summary>A prop at a hand-picked spot.</summary>
    public sealed class PlacedProp
    {
        public PlacedProp(IProp prop, System.Numerics.Vector2 position, System.Numerics.Vector2 facing, DressingLayer layer = DressingLayer.Furniture)
        {
            Prop = prop;
            Position = position;
            Facing = facing;
            Layer = layer;
        }

        public IProp Prop { get; }

        public System.Numerics.Vector2 Position { get; }

        public System.Numerics.Vector2 Facing { get; }

        public DressingLayer Layer { get; }
    }

    /// <summary>Places hand-picked props: the phone box, pillar boxes, benches, the bus stop, the memorial.</summary>
    public sealed class PlacedPropsRule : IDressingRule
    {
        private readonly IReadOnlyList<PlacedProp> _props;

        public PlacedPropsRule(IReadOnlyList<PlacedProp> props)
        {
            _props = props;
        }

        public void Apply(DressingContext context, Random random)
        {
            foreach (var placed in _props)
            {
                context.Place(placed.Prop, placed.Position, placed.Facing, placed.Layer, random);
            }
        }
    }
}
