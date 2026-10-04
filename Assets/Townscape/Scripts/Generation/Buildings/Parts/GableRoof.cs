using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>
    /// A rectangular hole in one slope of a <see cref="GableRoof"/>, for a roof window, in the
    /// footprint's (a, b) fractions: across from <see cref="A0"/> to <see cref="A1"/> and from
    /// <see cref="B0"/> to <see cref="B1"/> front to back, all on one side of the ridge.
    /// </summary>
    public readonly struct RoofOpening
    {
        public RoofOpening(float a0, float a1, float b0, float b1)
        {
            A0 = a0;
            A1 = a1;
            B0 = b0;
            B1 = b1;
        }

        public float A0 { get; }

        public float A1 { get; }

        public float B0 { get; }

        public float B1 { get; }

        public float CentreA => (A0 + A1) * 0.5f;

        public float CentreB => (B0 + B1) * 0.5f;
    }

    /// <summary>
    /// A dual-pitch slate roof whose ridge runs left to right across a <see cref="Footprint"/>.
    /// Works in the footprint's bilinear (a, b) frame, so it fits wedge-shaped terraced units and
    /// neighbouring roofs meet exactly.
    /// </summary>
    public sealed class GableRoof
    {
        private readonly Footprint _footprint;

        /// <param name="eaves">Height where the roof meets the front and back walls.</param>
        /// <param name="ridge">Height of the ridge.</param>
        /// <param name="overhang">How far the eaves stick out past the front and back walls, in metres.</param>
        /// <param name="verge">How far the roof sticks out past the gable walls, in metres (0 for terraces).</param>
        public GableRoof(Footprint footprint, float eaves, float ridge, float overhang, float verge, float thickness = 0.14f)
        {
            _footprint = footprint;
            Eaves = eaves;
            Ridge = ridge;
            Thickness = thickness;
            BFront = -overhang / footprint.Depth;
            BBack = 1f + (overhang / footprint.Depth);
            ALeft = -verge / footprint.FrontWidth;
            ARight = 1f + (verge / footprint.FrontWidth);
        }

        public float Eaves { get; }

        public float Ridge { get; }

        public float Thickness { get; }

        public float BFront { get; }

        public float BBack { get; }

        public float ALeft { get; }

        public float ARight { get; }

        /// <summary>Height of the underside of the roof (the roof plane) at depth fraction <paramref name="b"/>.</summary>
        public float PlaneHeight(float b) => Eaves + ((Ridge - Eaves) * (1f - MathF.Abs((2f * b) - 1f)));

        /// <summary>The depth fraction on the front slope where the roof plane reaches height <paramref name="y"/>.</summary>
        public float FrontSlopeAt(float y) => (y - Eaves) / (2f * (Ridge - Eaves));

        public Vector3 Underside(float a, float b) => GeoMath.At(_footprint.At(a, b), PlaneHeight(b));

        public Vector3 Top(float a, float b) => GeoMath.At(_footprint.At(a, b), PlaneHeight(b) + Thickness);

        /// <param name="openings">Holes left in the slates for roof windows.</param>
        public void Build(MeshBuilder builder, SurfaceMaterial slate, SurfaceMaterial trim, IReadOnlyList<RoofOpening> openings = null)
        {
            var up = Vector3.UnitY;
            var outward = GeoMath.At(_footprint.Outward, 0f);
            var leftward = GeoMath.At(Vector2.Normalize(_footprint.FrontLeft - _footprint.FrontRight), 0f);

            // Slopes, with the slates cut round any roof windows.
            Slope(builder, BFront, 0.5f, up + outward, slate, openings);
            Slope(builder, 0.5f, BBack, up - outward, slate, openings);

            // Ridge tiles: a slightly raised dark strip.
            const float capWidth = 0.035f;
            var capLift = new Vector3(0f, 0.035f, 0f);
            builder.AddQuadFacing(Top(ALeft, 0.5f - capWidth) + (capLift * 0.4f), Top(ARight, 0.5f - capWidth) + (capLift * 0.4f), Top(ARight, 0.5f) + capLift, Top(ALeft, 0.5f) + capLift, up + outward, SurfaceMaterial.StoneDark);
            builder.AddQuadFacing(Top(ALeft, 0.5f) + capLift, Top(ARight, 0.5f) + capLift, Top(ARight, 0.5f + capWidth) + (capLift * 0.4f), Top(ALeft, 0.5f + capWidth) + (capLift * 0.4f), up - outward, SurfaceMaterial.StoneDark);

            // Edges: eaves along the front and back, rakes up the gable ends.
            builder.AddQuadFacing(Underside(ALeft, BFront), Underside(ARight, BFront), Top(ARight, BFront), Top(ALeft, BFront), outward, trim);
            builder.AddQuadFacing(Underside(ALeft, BBack), Underside(ARight, BBack), Top(ARight, BBack), Top(ALeft, BBack), -outward, trim);
            foreach (var (a, facing) in new[] { (ALeft, leftward), (ARight, -leftward) })
            {
                builder.AddQuadFacing(Underside(a, BFront), Underside(a, 0.5f), Top(a, 0.5f), Top(a, BFront), facing, trim);
                builder.AddQuadFacing(Underside(a, 0.5f), Underside(a, BBack), Top(a, BBack), Top(a, 0.5f), facing, trim);
            }

            // Soffits under the overhangs.
            if (BFront < 0f)
            {
                builder.AddQuadFacing(Underside(ALeft, BFront), Underside(ARight, BFront), Underside(ARight, 0f), Underside(ALeft, 0f), -up, trim);
                builder.AddQuadFacing(Underside(ALeft, 1f), Underside(ARight, 1f), Underside(ARight, BBack), Underside(ALeft, BBack), -up, trim);
            }

            if (ALeft < 0f)
            {
                foreach (var (from, to) in new[] { (ALeft, 0f), (1f, ARight) })
                {
                    builder.AddQuadFacing(Underside(from, 0f), Underside(to, 0f), Underside(to, 0.5f), Underside(from, 0.5f), -up, trim);
                    builder.AddQuadFacing(Underside(from, 0.5f), Underside(to, 0.5f), Underside(to, 1f), Underside(from, 1f), -up, trim);
                }
            }
        }

        // One slope from b0 to b1, in strips across between the holes in it, and above and below each.
        private void Slope(MeshBuilder builder, float b0, float b1, Vector3 facing, SurfaceMaterial slate, IReadOnlyList<RoofOpening> openings)
        {
            void Piece(float pa0, float pb0, float pa1, float pb1)
            {
                if (pa1 - pa0 > 1e-5f && pb1 - pb0 > 1e-5f)
                {
                    builder.AddQuadFacing(Top(pa0, pb0), Top(pa1, pb0), Top(pa1, pb1), Top(pa0, pb1), facing, slate);
                }
            }

            var a = ALeft;
            if (openings != null)
            {
                foreach (var hole in openings.Where(h => h.B0 >= b0 && h.B1 <= b1).OrderBy(h => h.A0))
                {
                    Piece(a, b0, hole.A0, b1);
                    Piece(hole.A0, b0, hole.A1, hole.B0);
                    Piece(hole.A0, hole.B1, hole.A1, b1);
                    a = hole.A1;
                }
            }

            Piece(a, b0, ARight, b1);
        }

        /// <summary>The triangle of wall under the roof at a gable end, between eaves and ridge.</summary>
        public static void GableWall(MeshBuilder builder, WallFrame wall, float eaves, float ridge, SurfaceMaterial material)
        {
            wall.Polygon(builder, 0f, material, new Vector2(0f, eaves), new Vector2(wall.Width, eaves), new Vector2(wall.Width * 0.5f, ridge));
        }
    }
}
