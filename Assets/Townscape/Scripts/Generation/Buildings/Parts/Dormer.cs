using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>A gabled dormer window on the front slope of a <see cref="GableRoof"/>, lighting the loft.</summary>
    public static class Dormer
    {
        private const float FaceHeight = 1.25f;
        private const float RoofRise = 0.5f;
        private const float RoofOverhang = 0.12f;

        /// <param name="centreA">Position across the footprint (0 left to 1 right).</param>
        /// <param name="width">Dormer width in metres.</param>
        public static void Build(BuildContext context, Footprint footprint, GableRoof roof, float centreA, float width, SurfaceMaterial cheeks, WindowStyle window)
        {
            var builder = context.Builder;
            var halfA = width * 0.5f / footprint.FrontWidth;
            var a0 = centreA - halfA;
            var a1 = centreA + halfA;
            var frontB = 0.1f;
            var sill = roof.PlaneHeight(frontB) + roof.Thickness;
            var top = sill + FaceHeight;
            var peak = top + RoofRise;
            var backB = roof.FrontSlopeAt(top - roof.Thickness);
            var peakBackB = roof.FrontSlopeAt(peak - roof.Thickness);

            Vector3 P(float a, float b, float y) => GeoMath.At(footprint.At(a, b), y);

            // Front face with its window.
            var face = WallFrame.FromBase(footprint.At(a0, frontB), footprint.At(a1, frontB));
            builder.AddQuadFacing(P(a0, frontB, sill), P(a1, frontB, sill), P(a1, frontB, top), P(a0, frontB, top), face.Out, cheeks);
            face.Polygon(builder, 0f, cheeks, new Vector2(0f, top), new Vector2(face.Width, top), new Vector2(face.Width * 0.5f, peak));
            Glazing.AppliedWindow(context, face, 0.16f, sill + 0.18f, face.Width - 0.16f, top - 0.12f, window);

            // Cheeks: triangles from the roof slope up to the dormer's eaves.
            builder.AddTriangleFacing(P(a0, frontB, sill), P(a0, frontB, top), P(a0, backB, top), -face.Right, cheeks);
            builder.AddTriangleFacing(P(a1, frontB, sill), P(a1, frontB, top), P(a1, backB, top), face.Right, cheeks);

            // Dormer roof: two small slopes whose back edges run into the main roof.
            var eaveA0 = a0 - (RoofOverhang / footprint.FrontWidth);
            var eaveA1 = a1 + (RoofOverhang / footprint.FrontWidth);
            var frontEdgeB = frontB - (RoofOverhang / footprint.Depth);
            var up = Vector3.UnitY;
            builder.AddQuadFacing(P(eaveA0, frontEdgeB, top - 0.05f), P(centreA, frontEdgeB, peak + 0.05f), P(centreA, peakBackB, peak + 0.05f), P(eaveA0, backB, top - 0.05f), up - face.Right, SurfaceMaterial.Slate);
            builder.AddQuadFacing(P(centreA, frontEdgeB, peak + 0.05f), P(eaveA1, frontEdgeB, top - 0.05f), P(eaveA1, backB, top - 0.05f), P(centreA, peakBackB, peak + 0.05f), up + face.Right, SurfaceMaterial.Slate);
        }
    }
}
