using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Maths;
using Townscape.Generation.Markings;

namespace Townscape.Generation.Layout
{
    public enum RoadKind
    {
        HighStreet,
        Lane,
    }

    /// <summary>A road: a carriageway along a centreline with optional raised pavements either side.</summary>
    public sealed class RoadSpec
    {
        public RoadSpec(string name, RoadKind kind, Polyline centre, float carriagewayWidth, float pavementWidth, IReadOnlyList<IRoadMarking> markings = null)
        {
            Name = name;
            Kind = kind;
            Centre = centre;
            CarriagewayWidth = carriagewayWidth;
            PavementWidth = pavementWidth;
            Markings = markings ?? new IRoadMarking[0];
        }

        public string Name { get; }

        public RoadKind Kind { get; }

        public Polyline Centre { get; }

        public float CarriagewayWidth { get; }

        public float HalfWidth => CarriagewayWidth * 0.5f;

        public float PavementWidth { get; }

        public IReadOnlyList<IRoadMarking> Markings { get; }
    }

    /// <summary>
    /// The river. Inside <see cref="WalledRadius"/> of <see cref="WalledCentre"/> it runs in a
    /// stone-walled channel through the village; elsewhere it has sloping grassy banks.
    /// </summary>
    public sealed class RiverSpec
    {
        public RiverSpec(Polyline centre, float waterHalfWidth, float bankWidth, float wallHalfWidth, Vector2 walledCentre, float walledRadius, float bedDepth)
        {
            Centre = centre;
            WaterHalfWidth = waterHalfWidth;
            BankWidth = bankWidth;
            WallHalfWidth = wallHalfWidth;
            WalledCentre = walledCentre;
            WalledRadius = walledRadius;
            BedDepth = bedDepth;
        }

        public Polyline Centre { get; }

        public float WaterHalfWidth { get; }

        public float BankWidth { get; }

        /// <summary>Half the width between the embankment walls, in the walled section.</summary>
        public float WallHalfWidth { get; }

        public Vector2 WalledCentre { get; }

        public float WalledRadius { get; }

        /// <summary>Extra depth at the middle of the channel compared with its edges.</summary>
        public float BedDepth { get; }

        public bool IsWalledAt(Vector2 centrelinePoint) =>
            Vector2.Distance(centrelinePoint, WalledCentre) < WalledRadius;
    }

    /// <summary>A gravel footpath.</summary>
    public sealed class PathSpec
    {
        public PathSpec(string name, Polyline centre, float width)
        {
            Name = name;
            Centre = centre;
            Width = width;
        }

        public string Name { get; }

        public Polyline Centre { get; }

        public float Width { get; }
    }

    /// <summary>A stone humpback bridge carrying a road over the river.</summary>
    public sealed class BridgeSpec
    {
        public BridgeSpec(string name, Vector2 centre, Vector2 direction)
        {
            Name = name;
            Centre = centre;
            Direction = Vector2.Normalize(direction);
        }

        public string Name { get; }

        public Vector2 Centre { get; }

        /// <summary>Unit direction along the deck.</summary>
        public Vector2 Direction { get; }

        /// <summary>Half the deck length; the deck meets the road at road level at both ends.</summary>
        public float HalfLength { get; init; } = 13.5f;

        public float RoadHalfWidth { get; init; } = 2.8f;

        public float ParapetThickness { get; init; } = 0.45f;

        public float ParapetHeight { get; init; } = 0.95f;

        /// <summary>Height of the deck above road level at the middle of the bridge.</summary>
        public float HumpHeight { get; init; } = 1.8f;

        /// <summary>Half the arch span; set it to the river's wall half-width so the arch springs from the embankments.</summary>
        public float ArchHalfSpan { get; init; } = 5.6f;

        public float ArchRise { get; init; } = 1.5f;

        public float SpringingHeight { get; init; } = -0.65f;

        public float FoundationHeight { get; init; } = -2.8f;

        public float FootprintHalfWidth => RoadHalfWidth + ParapetThickness;
    }

    /// <summary>A lake (a "mere" or "water") the river drains into, out in the fells.</summary>
    public sealed class LakeSpec
    {
        public LakeSpec(Vector2 centre, Vector2 radii, float depth)
        {
            Centre = centre;
            Radii = radii;
            Depth = depth;
        }

        public Vector2 Centre { get; }

        public Vector2 Radii { get; }

        public float Depth { get; }

        /// <summary>Normalised elliptical distance: below 1 inside the lake.</summary>
        public float EllipseDistance(Vector2 p)
        {
            var d = (p - Centre) / Radii;
            return d.Length();
        }
    }

    /// <summary>Everything that describes one town. Generators turn this into meshes.</summary>
    public sealed class TownLayout
    {
        public TownLayout(
            string name,
            int seed,
            float waterLevel,
            float grassLevel,
            IReadOnlyList<RoadSpec> roads,
            RiverSpec river,
            IReadOnlyList<PathSpec> paths,
            IReadOnlyList<BridgeSpec> bridges,
            LakeSpec lake)
        {
            Name = name;
            Seed = seed;
            WaterLevel = waterLevel;
            GrassLevel = grassLevel;
            Roads = roads;
            River = river;
            Paths = paths;
            Bridges = bridges;
            Lake = lake;
        }

        public string Name { get; }

        public int Seed { get; }

        /// <summary>Height of the water surface for the river and the lake.</summary>
        public float WaterLevel { get; }

        /// <summary>Height of flat open ground. Roads sit at 0 and pavements just above.</summary>
        public float GrassLevel { get; }

        public IReadOnlyList<RoadSpec> Roads { get; }

        public RiverSpec River { get; }

        public IReadOnlyList<PathSpec> Paths { get; }

        public IReadOnlyList<BridgeSpec> Bridges { get; }

        public LakeSpec Lake { get; }
    }

    /// <summary>Strategy for producing a town layout. Swap it to generate a different town.</summary>
    public interface ITownLayoutSource
    {
        TownLayout Create();
    }
}
