using System.Numerics;
using Townscape.Generation.Dressing.Props;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>A painted window box on brackets under a window, spilling with flowers.</summary>
    public static class WindowBox
    {
        public static void Build(BuildContext context, WallFrame wall, float x0, float x1, float sillY, SurfaceMaterial paint)
        {
            var builder = context.Builder;
            var left = x0 - 0.04f;
            var right = x1 + 0.04f;
            wall.Block(builder, left, sillY - 0.34f, right, sillY - 0.12f, 0f, 0.24f, paint);
            foreach (var x in new[] { left + 0.12f, right - 0.12f })
            {
                wall.Block(builder, x - 0.02f, sillY - 0.48f, x + 0.02f, sillY - 0.34f, 0f, 0.14f, SurfaceMaterial.Iron);
            }

            var centre = wall.Point((left + right) * 0.5f, sillY - 0.1f, 0.12f);
            Planting.Mound(builder, context.Random, centre, wall.Right, wall.Out, (right - left) * 0.5f, 0.12f, 7);
        }
    }
}
