using System.Numerics;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>A region claimed by a feature, with the priority that settles overlaps.</summary>
    public readonly struct SurfaceClaim
    {
        public SurfaceClaim(ISurfaceRegion region, int priority)
        {
            Region = region;
            Priority = priority;
        }

        public ISurfaceRegion Region { get; }

        public int Priority { get; }
    }

    /// <summary>The closest point on one of a feature's boundary lines.</summary>
    public readonly struct ContourHit
    {
        public ContourHit(Vector2 point, float distance, int contourId)
        {
            Point = point;
            Distance = distance;
            ContourId = contourId;
        }

        public Vector2 Point { get; }

        public float Distance { get; }

        /// <summary>Identifies the boundary line, so the mesher can tell whether two vertices sit on the same one.</summary>
        public int ContourId { get; }
    }

    /// <summary>Overlap priorities. Higher wins: the river cuts through everything, roads beat pavements.</summary>
    public static class GroundPriority
    {
        public const int RiverBed = 100;
        public const int RiverBank = 90;
        public const int StructureFootprint = 80;
        public const int Road = 60;
        public const int Pavement = 50;
        public const int Path = 40;
    }

    /// <summary>
    /// Something that shapes the ground: a road, the river, a path, a bridge footprint. Features are
    /// described as distance fields, so junctions and overlaps resolve themselves by priority.
    /// </summary>
    public interface IGroundFeature
    {
        /// <summary>Everything the feature claims or snaps to lies inside these bounds.</summary>
        Rect2 Bounds { get; }

        bool TryClaim(Vector2 p, out SurfaceClaim claim);

        /// <summary>Signed distance to the feature's outer edge: negative inside.</summary>
        float DistanceToEdge(Vector2 p);

        /// <summary>
        /// Nearest point on any of the feature's boundary lines within <paramref name="maxDistance"/>.
        /// Grid vertices snap onto these so region edges come out straight instead of jagged.
        /// </summary>
        bool TryNearestContour(Vector2 p, float maxDistance, out ContourHit hit);
    }
}
