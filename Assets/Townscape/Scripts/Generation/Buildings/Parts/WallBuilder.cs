using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>Builds a flat wall with rectangular openings, and the reveals around each opening.</summary>
    public static class WallBuilder
    {
        /// <summary>
        /// Fills the rectangle (0, <paramref name="y0"/>)–(width, <paramref name="y1"/>) with wall,
        /// leaving holes for <paramref name="openings"/> and lining each hole with reveal faces.
        /// Whatever goes inside a hole (glass, a door, a shop display) is up to the caller.
        /// </summary>
        public static void Build(MeshBuilder builder, WallFrame wall, float y0, float y1, IReadOnlyList<Opening> openings, SurfaceMaterial material, SurfaceMaterial? revealMaterial = null)
        {
            var xs = new SortedSet<float> { 0f, wall.Width };
            var ys = new SortedSet<float> { y0, y1 };
            foreach (var opening in openings)
            {
                xs.Add(opening.X0);
                xs.Add(opening.X1);
                ys.Add(opening.Y0);
                ys.Add(opening.Y1);
            }

            // One quad per grid cell. Merging cells would be cheaper but leaves T-junctions, which
            // show up as hairline cracks; every cell here shares its corners with its neighbours.
            var xList = new List<float>(xs);
            var yList = new List<float>(ys);
            for (var j = 0; j < yList.Count - 1; j++)
            {
                for (var i = 0; i < xList.Count - 1; i++)
                {
                    if (IsSolid(openings, (xList[i] + xList[i + 1]) * 0.5f, (yList[j] + yList[j + 1]) * 0.5f))
                    {
                        wall.Quad(builder, xList[i], yList[j], xList[i + 1], yList[j + 1], 0f, material);
                    }
                }
            }

            foreach (var opening in openings)
            {
                AddReveal(builder, wall, opening, revealMaterial ?? material);
            }
        }

        public static void AddReveal(MeshBuilder builder, WallFrame wall, Opening opening, SurfaceMaterial material)
        {
            var back = -opening.Depth;
            var up = System.Numerics.Vector3.UnitY;

            // Head (faces down), sill (faces up), and the two jambs (face into the opening).
            builder.AddQuadFacing(wall.Point(opening.X0, opening.Y1, 0f), wall.Point(opening.X1, opening.Y1, 0f), wall.Point(opening.X1, opening.Y1, back), wall.Point(opening.X0, opening.Y1, back), -up, material);
            builder.AddQuadFacing(wall.Point(opening.X0, opening.Y0, 0f), wall.Point(opening.X1, opening.Y0, 0f), wall.Point(opening.X1, opening.Y0, back), wall.Point(opening.X0, opening.Y0, back), up, material);
            builder.AddQuadFacing(wall.Point(opening.X0, opening.Y0, 0f), wall.Point(opening.X0, opening.Y1, 0f), wall.Point(opening.X0, opening.Y1, back), wall.Point(opening.X0, opening.Y0, back), wall.Right, material);
            builder.AddQuadFacing(wall.Point(opening.X1, opening.Y0, 0f), wall.Point(opening.X1, opening.Y1, 0f), wall.Point(opening.X1, opening.Y1, back), wall.Point(opening.X1, opening.Y0, back), -wall.Right, material);
        }

        private static bool IsSolid(IReadOnlyList<Opening> openings, float x, float y)
        {
            foreach (var opening in openings)
            {
                if (opening.Contains(x, y))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
