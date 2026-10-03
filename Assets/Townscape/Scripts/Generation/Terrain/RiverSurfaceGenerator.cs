using System;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Layout;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Terrain
{
    /// <summary>
    /// The river's surface: a ribbon of water following the river from the fells to the lake.
    /// Its texture coordinates run downstream (v is metres along the river, u metres across), so
    /// scrolling the texture makes the water flow round every bend.
    /// </summary>
    public static class RiverSurfaceGenerator
    {
        private const float Step = 2f;

        public static MeshData Generate(RiverSpec river, float waterLevel)
        {
            // Wide enough to reach under the banks and walls; the ground hides whatever is too wide.
            var halfWidth = MathF.Max(river.WallHalfWidth, river.WaterHalfWidth + (river.BankWidth * 0.5f));
            var centre = river.Centre;
            var builder = new MeshBuilder();
            var count = Math.Max(1, (int)MathF.Ceiling(centre.Length / Step));

            for (var i = 0; i < count; i++)
            {
                var along0 = centre.Length * i / count;
                var along1 = centre.Length * (i + 1) / count;
                var (left0, right0) = Edges(centre, along0, halfWidth, waterLevel);
                var (left1, right1) = Edges(centre, along1, halfWidth, waterLevel);

                // Wound clockwise when seen from above: left, then downstream, then across.
                builder.AddQuad(
                    left0, left1, right1, right0,
                    new Vector2(-halfWidth, along0), new Vector2(-halfWidth, along1), new Vector2(halfWidth, along1), new Vector2(halfWidth, along0),
                    SurfaceMaterial.RiverWater);
            }

            return builder.Build("River Surface");
        }

        private static (Vector3 Left, Vector3 Right) Edges(Polyline centre, float along, float halfWidth, float waterLevel)
        {
            var point = centre.PointAt(along);
            var left = GeoMath.Left(centre.TangentAt(along)) * halfWidth;
            return (GeoMath.At(point + left, waterLevel), GeoMath.At(point - left, waterLevel));
        }
    }
}
