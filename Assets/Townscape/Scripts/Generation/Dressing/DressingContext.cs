using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Ground;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing
{
    /// <summary>Furniture (lamps, boxes, walls), vegetation and puddles are kept in separate meshes because they are treated differently: plants sway in the wind and puddles ripple.</summary>
    public enum DressingLayer
    {
        Furniture,
        Vegetation,
        Puddles,
    }

    /// <summary>
    /// What dressing rules work with: ground queries that cover both the detailed core and the
    /// fells, and mesh builders chunked by position so far-away dressing can be culled.
    /// </summary>
    public sealed class DressingContext
    {
        private const float CoreChunkSize = 50f;
        private const float FarChunkSize = 200f;
        private const float OccupancyCell = 8f;

        private readonly Dictionary<(DressingLayer Layer, bool Far, int X, int Z), MeshBuilder> _builders = new Dictionary<(DressingLayer, bool, int, int), MeshBuilder>();
        private readonly List<(DressingLayer Layer, bool Far, int X, int Z)> _order = new List<(DressingLayer, bool, int, int)>();
        private readonly Dictionary<(int X, int Z), List<(Vector2 Centre, float Radius)>> _occupied = new Dictionary<(int, int), List<(Vector2, float)>>();

        public DressingContext(TownContext town, ICollection<TownAnchor> anchors, ICollection<TownDoor> doors = null, ICollection<TownCabinet> cabinets = null)
        {
            Town = town;
            Anchors = anchors;
            Doors = doors ?? new List<TownDoor>();
            Cabinets = cabinets ?? new List<TownCabinet>();
        }

        public TownContext Town { get; }

        public GroundModel Ground => Town.Ground;

        public ICollection<TownAnchor> Anchors { get; }

        /// <summary>Doors that open on props (a phone box's, a cabinet's), each with its own leaf mesh.</summary>
        public ICollection<TownDoor> Doors { get; }

        /// <summary>Street cabinets for the fibre broadband.</summary>
        public ICollection<TownCabinet> Cabinets { get; }

        public bool InCore(Vector2 p) => GeoMath.ChebyshevLength(p) < Town.Settings.CoreHalfExtent - 0.5f;

        /// <summary>Height of the ground surface, matching whichever terrain mesh covers <paramref name="p"/>.</summary>
        public float HeightAt(Vector2 p) => InCore(p) ? Ground.HeightAt(p) : Town.Terrain.FarHeightAt(p);

        /// <summary>True if <paramref name="p"/> is inside a building, or within <paramref name="margin"/> metres of its walls.</summary>
        public bool InsideBuilding(Vector2 p, float margin = 0f)
        {
            foreach (var building in Town.Buildings)
            {
                if (building.Footprint.Contains(p, margin))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The region at <paramref name="p"/>. Outside the core everything is open ground.</summary>
        public RegionKind KindAt(Vector2 p) => InCore(p) ? Ground.Classify(p).Kind : RegionKind.OpenGround;

        /// <summary>True if <paramref name="p"/> and eight points around it at <paramref name="radius"/> are all in allowed regions.</summary>
        public bool IsClear(Vector2 p, float radius, params RegionKind[] allowed)
        {
            if (!Allowed(KindAt(p), allowed))
            {
                return false;
            }

            for (var i = 0; i < 8; i++)
            {
                var angle = MathF.PI * 0.25f * i;
                var probe = p + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
                if (!Allowed(KindAt(probe), allowed))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Marks a circle as taken, so later rules (trees) keep out of it.</summary>
        public void Occupy(Vector2 centre, float radius)
        {
            var key = ((int)MathF.Floor(centre.X / OccupancyCell), (int)MathF.Floor(centre.Y / OccupancyCell));
            if (!_occupied.TryGetValue(key, out var list))
            {
                list = new List<(Vector2, float)>();
                _occupied.Add(key, list);
            }

            list.Add((centre, radius));
        }

        /// <summary>True if a circle at <paramref name="p"/> would overlap anything already placed.</summary>
        public bool IsOccupied(Vector2 p, float radius)
        {
            var reach = (int)MathF.Ceiling((radius + 2f) / OccupancyCell);
            var cx = (int)MathF.Floor(p.X / OccupancyCell);
            var cz = (int)MathF.Floor(p.Y / OccupancyCell);
            for (var x = cx - reach; x <= cx + reach; x++)
            {
                for (var z = cz - reach; z <= cz + reach; z++)
                {
                    if (!_occupied.TryGetValue((x, z), out var list))
                    {
                        continue;
                    }

                    foreach (var (centre, other) in list)
                    {
                        if (Vector2.Distance(p, centre) < radius + other)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public MeshBuilder BuilderAt(Vector2 p, DressingLayer layer)
        {
            var far = !InCore(p);
            var size = far ? FarChunkSize : CoreChunkSize;
            var key = (layer, far, (int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size));
            if (!_builders.TryGetValue(key, out var builder))
            {
                builder = new MeshBuilder();
                _builders.Add(key, builder);
                _order.Add(key);
            }

            return builder;
        }

        /// <summary>Builds <paramref name="prop"/> standing on the ground at <paramref name="position"/>, its front towards <paramref name="facing"/>.</summary>
        public void Place(IProp prop, Vector2 position, Vector2 facing, DressingLayer layer, Random random, float footprint = 0.8f)
        {
            var origin = GeoMath.At(position, HeightAt(position));
            var builder = BuilderAt(position, layer);

            // Plants remember how high each vertex is above their base, so the wind can bend them.
            builder.SwayBase = layer == DressingLayer.Vegetation ? origin.Y : (float?)null;
            prop.Build(new PropFrame(builder, Anchors, random, origin, facing, Doors, Cabinets));
            builder.SwayBase = null;
            Occupy(position, footprint);
        }

        public IEnumerable<(string Name, DressingLayer Layer, MeshData Mesh)> Build()
        {
            foreach (var key in _order)
            {
                var builder = _builders[key];
                if (!builder.IsEmpty)
                {
                    var name = key.Far ? $"{key.Layer} (fells) {key.X},{key.Z}" : $"{key.Layer} {key.X},{key.Z}";
                    yield return (name, key.Layer, builder.Build(name));
                }
            }
        }

        private static bool Allowed(RegionKind kind, RegionKind[] allowed)
        {
            foreach (var candidate in allowed)
            {
                if (candidate == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Strategy for one kind of dressing: lamps along roads, trees on open ground, walls along lanes.</summary>
    public interface IDressingRule
    {
        void Apply(DressingContext context, Random random);
    }

    /// <summary>Strategy for one kind of object, built around its own origin.</summary>
    public interface IProp
    {
        void Build(PropFrame frame);
    }
}
