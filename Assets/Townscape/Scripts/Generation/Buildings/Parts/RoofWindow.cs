using System;
using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>
    /// A roof window lying in the slope (the kind with a grey metal frame that pivots open about
    /// its middle): the frame stands on the slates round a <see cref="RoofOpening"/>, and the
    /// glazed sash is a <see cref="TownDoor"/> that turns about a level line across its middle,
    /// its top tipping into the room and its bottom out over the slates.
    /// </summary>
    public static class RoofWindow
    {
        /// <summary>How far the window opens on its pivot, in degrees.</summary>
        public const float OpenDegrees = 35f;

        private const float Cladding = 0.06f;
        private const float CladdingHeight = 0.09f;
        private const float SashBar = 0.07f;
        private const float SashDepth = 0.055f;

        public static void Build(BuildContext context, GableRoof roof, RoofOpening hole, string name)
        {
            var builder = context.Builder;

            // Work on the slates: x across the hole, y up the slope from its lower edge, z out of the roof.
            var (bLow, bHigh) = Math.Abs(hole.B0 - 0.5f) > Math.Abs(hole.B1 - 0.5f) ? (hole.B0, hole.B1) : (hole.B1, hole.B0);
            var origin = roof.Top(hole.A0, bLow);
            var acrossEdge = roof.Top(hole.A1, bLow) - origin;
            var upEdge = roof.Top(hole.A0, bHigh) - origin;
            var width = acrossEdge.Length();
            var length = upEdge.Length();
            var across = acrossEdge / width;
            var up = upEdge / length;
            var normal = Vector3.Normalize(Vector3.Cross(across, up));
            if (normal.Y < 0f)
            {
                normal = -normal;
            }

            Vector3 P(float x, float y, float z) => origin + (across * x) + (up * y) + (normal * z);

            void Box(MeshBuilder into, float x0, float y0, float z0, float x1, float y1, float z1, SurfaceMaterial material) =>
                into.AddBox(P((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), across, normal, up, new Vector3((x1 - x0) * 0.5f, (z1 - z0) * 0.5f, (y1 - y0) * 0.5f), material, includeBottom: true);

            // The frame on the slates, and a lead apron below it.
            Box(builder, -Cladding, -Cladding, -0.02f, width + Cladding, 0f, CladdingHeight, SurfaceMaterial.Iron);
            Box(builder, -Cladding, length, -0.02f, width + Cladding, length + Cladding, CladdingHeight, SurfaceMaterial.Iron);
            Box(builder, -Cladding, 0f, -0.02f, 0f, length, CladdingHeight, SurfaceMaterial.Iron);
            Box(builder, width, 0f, -0.02f, width + Cladding, length, CladdingHeight, SurfaceMaterial.Iron);
            Box(builder, -Cladding - 0.05f, -Cladding - 0.16f, -0.01f, width + Cladding + 0.05f, -Cladding, 0.012f, SurfaceMaterial.StoneDark);

            // The sash, pivoting about a level line across the middle of the hole.
            var pivot = P(0f, length * 0.5f, 0f);
            context.Door(name, pivot, up, -normal, length * 0.5f, width, sash =>
            {
                var leaf = sash.Builder;

                // Grey metal outside, painted white inside, with the handle bar along the top.
                void Bar(float x0, float y0, float x1, float y1)
                {
                    Box(leaf, x0, y0, 0f, x1, y1, SashDepth * 0.5f, SurfaceMaterial.Iron);
                    Box(leaf, x0, y0, -SashDepth * 0.5f, x1, y1, 0f, SurfaceMaterial.PaintWhite);
                }

                Bar(0.005f, 0.005f, width - 0.005f, SashBar);
                Bar(0.005f, length - SashBar, width - 0.005f, length - 0.005f);
                Bar(0.005f, SashBar, SashBar, length - SashBar);
                Bar(width - SashBar, SashBar, width - 0.005f, length - SashBar);
                Box(leaf, width * 0.15f, length - SashBar - 0.01f, (-SashDepth * 0.5f) - 0.035f, width * 0.85f, length - SashBar + 0.03f, -SashDepth * 0.5f, SurfaceMaterial.PaintWhite);

                // Glass seen from both sides.
                leaf.AddQuadFacing(P(SashBar, SashBar, 0f), P(width - SashBar, SashBar, 0f), P(width - SashBar, length - SashBar, 0f), P(SashBar, length - SashBar, 0f), normal, SurfaceMaterial.ClearGlass);
                leaf.AddQuadFacing(P(SashBar, SashBar, 0f), P(width - SashBar, SashBar, 0f), P(width - SashBar, length - SashBar, 0f), P(SashBar, length - SashBar, 0f), -normal, SurfaceMaterial.ClearGlass);
            }, OpenDegrees, across, "window");
        }
    }
}
