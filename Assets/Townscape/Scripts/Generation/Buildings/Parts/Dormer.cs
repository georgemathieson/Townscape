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

        /// <summary>Where a dormer sits on its roof: across (a) and back (b) as footprint fractions, and its heights.</summary>
        public readonly struct Span
        {
            public Span(float a0, float a1, float frontB, float backB, float sill, float top)
            {
                A0 = a0;
                A1 = a1;
                FrontB = frontB;
                BackB = backB;
                Sill = sill;
                Top = top;
            }

            public float A0 { get; }

            public float A1 { get; }

            /// <summary>Depth fraction of the dormer's face.</summary>
            public float FrontB { get; }

            /// <summary>Depth fraction where the dormer's eaves run into the main roof.</summary>
            public float BackB { get; }

            /// <summary>Height of the bottom of the dormer's face, on top of the roof.</summary>
            public float Sill { get; }

            /// <summary>Height of the dormer's eaves.</summary>
            public float Top { get; }
        }

        public static Span Measure(Footprint footprint, GableRoof roof, float centreA, float width)
        {
            var halfA = width * 0.5f / footprint.FrontWidth;
            const float frontB = 0.1f;
            var sill = roof.PlaneHeight(frontB) + roof.Thickness;
            var top = sill + FaceHeight;
            return new Span(centreA - halfA, centreA + halfA, frontB, roof.FrontSlopeAt(top - roof.Thickness), sill, top);
        }

        /// <param name="centreA">Position across the footprint (0 left to 1 right).</param>
        /// <param name="width">Dormer width in metres.</param>
        /// <param name="seeThrough">
        /// A real window in an opening, finished inside too, for a loft you can stand in; otherwise
        /// a window drawn on the face.
        /// </param>
        public static void Build(BuildContext context, Footprint footprint, GableRoof roof, float centreA, float width, SurfaceMaterial cheeks, WindowStyle window, bool seeThrough = false)
        {
            var builder = context.Builder;
            var span = Measure(footprint, roof, centreA, width);
            var a0 = span.A0;
            var a1 = span.A1;
            var frontB = span.FrontB;
            var sill = span.Sill;
            var top = span.Top;
            var peak = top + RoofRise;
            var backB = span.BackB;
            var peakBackB = roof.FrontSlopeAt(peak - roof.Thickness);

            Vector3 P(float a, float b, float y) => GeoMath.At(footprint.At(a, b), y);

            // Front face with its window.
            var face = WallFrame.FromBase(footprint.At(a0, frontB), footprint.At(a1, frontB));
            var glass = new Opening(0.16f, sill + 0.18f, face.Width - 0.16f, top - 0.12f, 0.08f);
            if (seeThrough)
            {
                WallBuilder.Build(builder, face, sill, top, new[] { glass }, cheeks);
                Glazing.FillWindow(context, face, glass, window);
                Inside(builder, face, glass, sill, top, peak);
            }
            else
            {
                builder.AddQuadFacing(P(a0, frontB, sill), P(a1, frontB, sill), P(a1, frontB, top), P(a0, frontB, top), face.Out, cheeks);
                Glazing.AppliedWindow(context, face, glass.X0, glass.Y0, glass.X1, glass.Y1, window);
            }

            face.Polygon(builder, 0f, cheeks, new Vector2(0f, top), new Vector2(face.Width, top), new Vector2(face.Width * 0.5f, peak));

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

            if (seeThrough)
            {
                // Inside: the cheeks and the ceiling, and a plastered slope over the main roof up to the window.
                var lining = SurfaceMaterial.Interior;
                const float inset = 0.03f;
                var ia0 = a0 + (inset / footprint.FrontWidth);
                var ia1 = a1 - (inset / footprint.FrontWidth);
                builder.AddTriangleFacing(P(ia0, frontB, sill), P(ia0, frontB, top - inset), P(ia0, backB, top - inset), face.Right, lining);
                builder.AddTriangleFacing(P(ia1, frontB, sill), P(ia1, frontB, top - inset), P(ia1, backB, top - inset), -face.Right, lining);
                builder.AddQuadFacing(P(ia0, frontB, top - inset), P(centreA, frontB, peak - inset), P(centreA, peakBackB, peak - inset), P(ia0, backB, top - inset), -up + face.Right, lining);
                builder.AddQuadFacing(P(centreA, frontB, peak - inset), P(ia1, frontB, top - inset), P(ia1, backB, top - inset), P(centreA, peakBackB, peak - inset), -up - face.Right, lining);
                var sillBackB = roof.FrontSlopeAt(top - 0.1f - roof.Thickness);
                var lift = roof.Thickness + 0.02f;
                builder.AddQuadFacing(
                    P(ia0, frontB, sill + 0.02f), P(ia1, frontB, sill + 0.02f),
                    P(ia1, sillBackB, roof.PlaneHeight(sillBackB) + lift), P(ia0, sillBackB, roof.PlaneHeight(sillBackB) + lift),
                    up, lining);
            }
        }

        // The inside of the face, round the window, and of its little gable.
        private static void Inside(MeshBuilder builder, WallFrame face, Opening glass, float sill, float top, float peak)
        {
            const float z = -0.1f;
            var lining = SurfaceMaterial.Interior;
            var w = face.Width;
            face.QuadInward(builder, 0f, sill, w, glass.Y0, z, lining);
            face.QuadInward(builder, 0f, glass.Y1, w, top, z, lining);
            face.QuadInward(builder, 0f, glass.Y0, glass.X0, glass.Y1, z, lining);
            face.QuadInward(builder, glass.X1, glass.Y0, w, glass.Y1, z, lining);
            builder.AddTriangleFacing(face.Point(0f, top, z), face.Point(w, top, z), face.Point(w * 0.5f, peak, z), -face.Out, lining);
        }
    }
}
