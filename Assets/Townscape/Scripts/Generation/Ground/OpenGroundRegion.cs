using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Terrain;

namespace Townscape.Generation.Ground
{
    /// <summary>Grass wherever no feature claims the ground, in two shades for a patchwork look.</summary>
    public sealed class OpenGroundRegion : ISurfaceRegion
    {
        private readonly BaseTerrain _terrain;

        public OpenGroundRegion(BaseTerrain terrain)
        {
            _terrain = terrain;
        }

        public RegionKind Kind => RegionKind.OpenGround;

        public SurfaceMaterial Side => SurfaceMaterial.Earth;

        public SurfaceMaterial TopAt(Vector2 centroid) =>
            _terrain.PatchNoise(centroid, 0.06f) > 0.62f ? SurfaceMaterial.GrassDark : SurfaceMaterial.Grass;

        public float HeightAt(in GroundPoint point) => point.BaseHeight;
    }
}
