using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>A stone or rendered chimney stack with a cap and terracotta pots.</summary>
    public static class Chimney
    {
        /// <param name="centre">Ground-plane centre of the stack.</param>
        /// <param name="along">Unit direction the stack is long in (the pots line up along it).</param>
        public static void Stack(BuildContext context, Vector2 centre, Vector2 along, float bottom, float top, float length, float thickness, SurfaceMaterial wall, int pots)
        {
            var builder = context.Builder;
            var right = GeoMath.At(Vector2.Normalize(along), 0f);
            var forward = Vector3.Cross(right, Vector3.UnitY);
            var middle = GeoMath.At(centre, (bottom + top) * 0.5f);
            builder.AddBox(middle, right, Vector3.UnitY, forward, new Vector3(length * 0.5f, (top - bottom) * 0.5f, thickness * 0.5f), wall);

            // Projecting cap course.
            var capCentre = GeoMath.At(centre, top + 0.07f);
            builder.AddBox(capCentre, right, Vector3.UnitY, forward, new Vector3((length * 0.5f) + 0.06f, 0.07f, (thickness * 0.5f) + 0.06f), SurfaceMaterial.StoneDark, includeBottom: true);

            var potTop = top + 0.14f;
            for (var i = 0; i < pots; i++)
            {
                var offset = pots == 1 ? 0f : ((i / (float)(pots - 1)) - 0.5f) * (length - 0.32f);
                var potBase = GeoMath.At(centre, potTop) + (right * offset);
                var height = 0.32f + (0.12f * (float)context.Random.NextDouble());
                builder.AddPrism(potBase, 0.11f, height, 7, SurfaceMaterial.ChimneyPot, rotation: i);
                context.Anchor(AnchorKind.Chimney, potBase + (Vector3.UnitY * height), Vector3.UnitY, 0.22f);
            }
        }
    }
}
