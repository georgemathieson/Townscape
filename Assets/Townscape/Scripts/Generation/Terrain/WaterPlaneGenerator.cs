using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Terrain
{
    /// <summary>
    /// One flat sheet of water at water level under the whole map. Terrain sits above it everywhere
    /// except the river channel and the lake basin, so it only shows where water belongs.
    /// </summary>
    public static class WaterPlaneGenerator
    {
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
                    builder.AddQuadFacing(
                        new Vector3(x0, waterLevel, z0),
                        new Vector3(x0, waterLevel, z0 + step),
                        new Vector3(x0 + step, waterLevel, z0 + step),
                        new Vector3(x0 + step, waterLevel, z0),
                        Vector3.UnitY,
                        SurfaceMaterial.Water);
                }
            }

            return builder.Build("Water");
        }
    }
}
