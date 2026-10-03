using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Buildings;
using Townscape.Generation.Buildings.Shops;
using Townscape.Generation.Markings;

namespace Townscape.Generation.Layout
{
    /// <summary>One unit in a terrace: a shop, or a house when <see cref="Shop"/> is null.</summary>
    public sealed class TerraceUnit
    {
        public TerraceUnit(ShopDefinition shop = null, float widthWeight = 1f)
        {
            Shop = shop;
            WidthWeight = widthWeight;
        }

        public ShopDefinition Shop { get; }

        /// <summary>Relative width: a unit with weight 1.3 is about 30% wider than its neighbours.</summary>
        public float WidthWeight { get; }

        public static TerraceUnit House(float widthWeight = 1f) => new TerraceUnit(null, widthWeight);
    }

    /// <summary>A continuous row of terraced buildings fronting a road between two arc lengths.</summary>
    public sealed class TerraceSpec
    {
        public TerraceSpec(string name, RoadSpec road, KerbSide side, float fromAlong, float toAlong, IReadOnlyList<TerraceUnit> units, int seed)
        {
            Name = name;
            Road = road;
            Side = side;
            FromAlong = fromAlong;
            ToAlong = toAlong;
            Units = units;
            Seed = seed;
        }

        public string Name { get; }

        public RoadSpec Road { get; }

        /// <summary>Left or right of the road, looking along its centreline.</summary>
        public KerbSide Side { get; }

        public float FromAlong { get; }

        public float ToAlong { get; }

        /// <summary>Units in order of increasing arc length along the road.</summary>
        public IReadOnlyList<TerraceUnit> Units { get; }

        public int Seed { get; }

        /// <summary>Gap between the back of the pavement (or the kerb, for lanes) and the building fronts.</summary>
        public float Setback { get; init; }

        public float Depth { get; init; } = 9f;
    }

    /// <summary>A free-standing building with its own style: a cottage, a detached shop, the church.</summary>
    public sealed class DetachedBuildingSpec
    {
        public DetachedBuildingSpec(string name, Footprint footprint, IBuildingStyle style, int seed)
        {
            Name = name;
            Footprint = footprint;
            Style = style;
            Seed = seed;
        }

        public string Name { get; }

        public Footprint Footprint { get; }

        public IBuildingStyle Style { get; }

        public int Seed { get; }

        /// <summary>A building set back from a road and facing it.</summary>
        public static DetachedBuildingSpec FacingRoad(string name, RoadSpec road, float along, KerbSide side, float setback, float width, float depth, IBuildingStyle style, int seed)
        {
            var sign = side == KerbSide.Right ? -1f : 1f;
            var offset = sign * (road.HalfWidth + road.PavementWidth + setback);
            var front = road.PointAt(along, offset);
            var outward = road.PointAt(along, 0f) - front;
            return new DetachedBuildingSpec(name, Footprint.FromFront(front, outward, width, depth), style, seed);
        }

        /// <summary>A building whose front faces a given direction.</summary>
        public static DetachedBuildingSpec Facing(string name, Vector2 frontCentre, Vector2 outward, float width, float depth, IBuildingStyle style, int seed) =>
            new DetachedBuildingSpec(name, Footprint.FromFront(frontCentre, outward, width, depth), style, seed);
    }
}
