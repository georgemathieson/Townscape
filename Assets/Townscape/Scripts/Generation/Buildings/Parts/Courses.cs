using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>Horizontal bands of masonry: a plinth at the foot of a wall and string courses between floors.</summary>
    public static class Courses
    {
        /// <summary>A slightly projecting dark plinth from below ground up to just above the floor.</summary>
        public static void Plinth(MeshBuilder builder, WallFrame wall)
        {
            wall.Block(builder, 0f, BuildingLevels.Base, wall.Width, BuildingLevels.Floor + 0.04f, -0.02f, 0.05f, SurfaceMaterial.StoneDark);
        }

        /// <summary>A projecting band along height <paramref name="y"/>, which also hides the join between wall bands.</summary>
        public static void String(MeshBuilder builder, WallFrame wall, float y, SurfaceMaterial material)
        {
            wall.Block(builder, 0f, y - 0.05f, wall.Width, y + 0.07f, -0.02f, 0.05f, material);
        }
    }
}
