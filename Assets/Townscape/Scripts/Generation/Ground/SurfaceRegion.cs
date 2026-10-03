using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Ground
{
    public enum RegionKind
    {
        OpenGround,
        RiverBed,
        RiverBank,
        Road,
        Pavement,
        Path,

        /// <summary>Flagged apron under and around a building.</summary>
        Yard,

        /// <summary>Left empty because a structure (the bridge) provides its own surface.</summary>
        Hole,
    }

    /// <summary>A ground grid vertex: where it is, and the height of open ground there.</summary>
    public readonly struct GroundPoint
    {
        public GroundPoint(Vector2 position, float baseHeight)
        {
            Position = position;
            BaseHeight = baseHeight;
        }

        public Vector2 Position { get; }

        /// <summary>Height of open ground (grass) at <see cref="Position"/>, for regions that follow the terrain.</summary>
        public float BaseHeight { get; }
    }

    /// <summary>
    /// Strategy for one kind of ground surface: its height profile and materials. Where two
    /// neighbouring regions sit at different heights, the ground generator joins them with a
    /// vertical face (a kerb, a verge edge, an embankment wall).
    /// </summary>
    public interface ISurfaceRegion
    {
        RegionKind Kind { get; }

        /// <summary>Material for vertical faces where this region is the higher side.</summary>
        SurfaceMaterial Side { get; }

        /// <summary>Material for the top of a triangle centred at <paramref name="centroid"/>.</summary>
        SurfaceMaterial TopAt(Vector2 centroid);

        float HeightAt(in GroundPoint point);
    }

    /// <summary>A region with a fixed height and a single top material.</summary>
    public sealed class FlatRegion : ISurfaceRegion
    {
        private readonly SurfaceMaterial _top;
        private readonly float _height;

        public FlatRegion(RegionKind kind, SurfaceMaterial top, SurfaceMaterial side, float height)
        {
            Kind = kind;
            _top = top;
            Side = side;
            _height = height;
        }

        public RegionKind Kind { get; }

        public SurfaceMaterial Side { get; }

        public SurfaceMaterial TopAt(Vector2 centroid) => _top;

        public float HeightAt(in GroundPoint point) => _height;
    }

    /// <summary>A region that sits a fixed amount above the open terrain (footpaths).</summary>
    public sealed class TerrainFollowingRegion : ISurfaceRegion
    {
        private readonly SurfaceMaterial _top;
        private readonly float _offset;

        public TerrainFollowingRegion(RegionKind kind, SurfaceMaterial top, SurfaceMaterial side, float offset)
        {
            Kind = kind;
            _top = top;
            Side = side;
            _offset = offset;
        }

        public RegionKind Kind { get; }

        public SurfaceMaterial Side { get; }

        public SurfaceMaterial TopAt(Vector2 centroid) => _top;

        public float HeightAt(in GroundPoint point) => point.BaseHeight + _offset;
    }
}
