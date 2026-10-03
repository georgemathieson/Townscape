using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Terrain
{
    /// <summary>
    /// One flat sheet of water at water level under the whole map. Terrain sits above it everywhere
    /// except the river channel and the lake basin, so it only shows where water belongs. The
    /// river has its own surface on top (see <see cref="RiverSurfaceGenerator"/>), so the sheet
    /// sits a hair lower to keep the two from flickering where they overlap.
    /// </summary>
    public static class WaterPlaneGenerator
    {
        /// <summary>How far below the river's surface the sheet sits.</summary>
        public const float RiverClearance = 0.03f;

        private const int Cells = 16;

        public static MeshData Generate(float halfExtent, float waterLevel)
        {
            var builder = new MeshBuilder();
            var step = halfExtent * 2f / Cells;
            for (var j = 0; j < Cells; j++)
            {
                for (var i = 0; i < Cells; i++)
                {
                    var x0 = -halfExtent + (i * step);
                    var z0 = -halfExtent + (j * step);
                    // Texture coordinates in metres, seen from above, for the rain ripples.
                    builder.AddQuad(
                        new Vector3(x0, waterLevel, z0),
                        new Vector3(x0, waterLevel, z0 + step),
                        new Vector3(x0 + step, waterLevel, z0 + step),
                        new Vector3(x0 + step, waterLevel, z0),
                        new Vector2(x0, z0),
                        new Vector2(x0, z0 + step),
                        new Vector2(x0 + step, z0 + step),
                        new Vector2(x0 + step, z0),
                        SurfaceMaterial.Water);
                }
            }

            return builder.Build("Water");
        }
    }
}
