using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Townscape.Generation.Buildings.Parts;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Interiors
{
    /// <summary>A rectangle in a wall, in the wall's own coordinates: across, then up.</summary>
    public readonly struct Hole
    {
        public Hole(float from, float bottom, float to, float top)
        {
            From = from;
            Bottom = bottom;
            To = to;
            Top = top;
        }

        public float From { get; }

        public float Bottom { get; }

        public float To { get; }

        public float Top { get; }
    }

    /// <summary>
    /// The inside of a building, measured the way you'd pace it out: <c>x</c> across from the
    /// left of the front as seen from the street, <c>d</c> back from the front wall, <c>y</c> up.
    /// Everything for building rooms: floors, ceilings, walls with doorways and windows cut out,
    /// boxes and round things. Every surface faces into the room it belongs to.
    /// </summary>
    public readonly struct UnitSpace
    {
        private readonly WallFrame _front;

        public UnitSpace(Footprint footprint)
        {
            _front = footprint.FrontWall;
            Width = footprint.FrontWidth;
            Depth = footprint.Depth;
        }

        public float Width { get; }

        public float Depth { get; }

        /// <summary>Across the building, left to right as seen from the street.</summary>
        public Vector3 Right => _front.Right;

        /// <summary>From the front of the building towards the back.</summary>
        public Vector3 Back => -_front.Out;

        public Vector3 At(float x, float y, float d) => _front.Point(x, y, -d);

        /// <summary>Where a point in the town is in this space: (x, y, d).</summary>
        public Vector3 Local(Vector3 point)
        {
            var offset = point - _front.Origin;
            return new Vector3(Vector3.Dot(offset, Right), offset.Y, -Vector3.Dot(offset, _front.Out));
        }

        /// <summary>A level rectangle seen from above.</summary>
        public void Floor(MeshBuilder builder, float x0, float d0, float x1, float d1, float y, SurfaceMaterial material) =>
            builder.AddQuadFacing(At(x0, y, d0), At(x1, y, d0), At(x1, y, d1), At(x0, y, d1), Vector3.UnitY, material);

        /// <summary>A level rectangle seen from below.</summary>
        public void Ceiling(MeshBuilder builder, float x0, float d0, float x1, float d1, float y, SurfaceMaterial material) =>
            builder.AddQuadFacing(At(x0, y, d0), At(x1, y, d0), At(x1, y, d1), At(x0, y, d1), -Vector3.UnitY, material);

        /// <summary>
        /// A wall running across the building at depth <paramref name="d"/>, from <paramref name="x0"/>
        /// to <paramref name="x1"/>, seen from the back (<paramref name="facingBack"/>) or the front.
        /// Holes are given across (x) and up (y).
        /// </summary>
        public void WallAcross(MeshBuilder builder, float d, float x0, float x1, float y0, float y1, bool facingBack, SurfaceMaterial material, params Hole[] holes)
        {
            var space = this;
            var facing = facingBack ? Back : -Back;
            Wall(builder, x0, x1, y0, y1, holes, facing, material, (across, up) => space.At(across, up, d));
        }

        /// <summary>
        /// A wall running back through the building at <paramref name="x"/>, from depth
        /// <paramref name="d0"/> to <paramref name="d1"/>, seen from the right
        /// (<paramref name="facingRight"/>) or the left. Holes are given back (d) and up (y).
        /// </summary>
        public void WallAlong(MeshBuilder builder, float x, float d0, float d1, float y0, float y1, bool facingRight, SurfaceMaterial material, params Hole[] holes)
        {
            var space = this;
            var facing = facingRight ? Right : -Right;
            Wall(builder, d0, d1, y0, y1, holes, facing, material, (along, up) => space.At(x, up, along));
        }

        /// <summary>A solid box lined up with the building.</summary>
        public void Box(MeshBuilder builder, float x0, float y0, float d0, float x1, float y1, float d1, SurfaceMaterial material)
        {
            var centre = At((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (d0 + d1) * 0.5f);
            var half = new Vector3(Math.Abs(x1 - x0) * 0.5f, Math.Abs(y1 - y0) * 0.5f, Math.Abs(d1 - d0) * 0.5f);
            builder.AddBox(centre, Right, Vector3.UnitY, Back, half, material, includeBottom: true);
        }

        /// <summary>An upright many-sided cylinder standing on (x, y, d).</summary>
        public void Round(MeshBuilder builder, float x, float y, float d, float radius, float height, SurfaceMaterial material, int sides = 10) =>
            builder.AddPrism(At(x, y, d), radius, height, sides, material, capBottom: true);

        // Cuts the holes out of a flat wall: full-height strips between the holes, and pieces
        // above and below each one.
        private static void Wall(MeshBuilder builder, float a0, float a1, float y0, float y1, IEnumerable<Hole> holes, Vector3 facing, SurfaceMaterial material, Func<float, float, Vector3> point)
        {
            void Piece(float pa0, float py0, float pa1, float py1)
            {
                if (pa1 - pa0 > 1e-4f && py1 - py0 > 1e-4f)
                {
                    builder.AddQuadFacing(point(pa0, py0), point(pa1, py0), point(pa1, py1), point(pa0, py1), facing, material);
                }
            }

            var a = a0;
            foreach (var hole in holes.OrderBy(h => h.From))
            {
                var from = Math.Max(a0, hole.From);
                var to = Math.Min(a1, hole.To);
                if (to <= from)
                {
                    continue;
                }

                Piece(a, y0, from, y1);
                Piece(from, y0, to, Math.Max(y0, hole.Bottom));
                Piece(from, Math.Min(y1, hole.Top), to, y1);
                a = to;
            }

            Piece(a, y0, a1, y1);
        }
    }
}
