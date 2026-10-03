using System.Numerics;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>
    /// A flat vertical wall seen from outside. Wall coordinates: <c>x</c> runs from the wall's
    /// left edge to its right edge, <c>y</c> is world height and <c>z</c> is distance out of the
    /// wall (negative is into the building). Everything on a facade is placed in these coordinates.
    /// </summary>
    public readonly struct WallFrame
    {
        private WallFrame(Vector3 origin, Vector3 right, Vector3 outward, float width)
        {
            Origin = origin;
            Right = right;
            Out = outward;
            Width = width;
        }

        /// <summary>Bottom-left corner of the wall at height 0.</summary>
        public Vector3 Origin { get; }

        /// <summary>Unit vector to the right, as seen from outside.</summary>
        public Vector3 Right { get; }

        /// <summary>Unit vector out of the wall.</summary>
        public Vector3 Out { get; }

        public float Width { get; }

        /// <summary>A wall standing on the ground-plane edge from <paramref name="left"/> to <paramref name="right"/>, as seen from outside.</summary>
        public static WallFrame FromBase(Vector2 left, Vector2 right)
        {
            var leftPoint = new Vector3(left.X, 0f, left.Y);
            var span = new Vector3(right.X - left.X, 0f, right.Y - left.Y);
            var width = span.Length();
            var unitRight = span / width;
            return new WallFrame(leftPoint, unitRight, Vector3.Cross(Vector3.UnitY, unitRight), width);
        }

        public Vector3 Point(float x, float y, float z = 0f) => Origin + (Right * x) + (Vector3.UnitY * y) + (Out * z);

        /// <summary>A rectangle parallel to the wall at depth <paramref name="z"/>, facing out.</summary>
        public void Quad(MeshBuilder builder, float x0, float y0, float x1, float y1, float z, SurfaceMaterial material)
        {
            builder.AddQuadFacing(Point(x0, y0, z), Point(x1, y0, z), Point(x1, y1, z), Point(x0, y1, z), Out, material);
        }

        /// <summary>A rectangle parallel to the wall facing into it, for the back of a recess.</summary>
        public void QuadInward(MeshBuilder builder, float x0, float y0, float x1, float y1, float z, SurfaceMaterial material)
        {
            builder.AddQuadFacing(Point(x0, y0, z), Point(x1, y0, z), Point(x1, y1, z), Point(x0, y1, z), -Out, material);
        }

        /// <summary>
        /// A box standing proud of the wall between depths <paramref name="zBack"/> and
        /// <paramref name="zFront"/>: front, both ends, top and underside. The back is left open.
        /// </summary>
        public void Block(MeshBuilder builder, float x0, float y0, float x1, float y1, float zBack, float zFront, SurfaceMaterial material)
        {
            Quad(builder, x0, y0, x1, y1, zFront, material);
            builder.AddQuadFacing(Point(x0, y0, zBack), Point(x0, y0, zFront), Point(x0, y1, zFront), Point(x0, y1, zBack), -Right, material);
            builder.AddQuadFacing(Point(x1, y0, zBack), Point(x1, y0, zFront), Point(x1, y1, zFront), Point(x1, y1, zBack), Right, material);
            builder.AddQuadFacing(Point(x0, y1, zBack), Point(x1, y1, zBack), Point(x1, y1, zFront), Point(x0, y1, zFront), Vector3.UnitY, material);
            builder.AddQuadFacing(Point(x0, y0, zBack), Point(x1, y0, zBack), Point(x1, y0, zFront), Point(x0, y0, zFront), -Vector3.UnitY, material);
        }

        /// <summary>A convex polygon on the wall plane (wall-space x, y points), facing out.</summary>
        public void Polygon(MeshBuilder builder, float z, SurfaceMaterial material, params Vector2[] points)
        {
            for (var i = 1; i < points.Length - 1; i++)
            {
                builder.AddTriangleFacing(
                    Point(points[0].X, points[0].Y, z),
                    Point(points[i].X, points[i].Y, z),
                    Point(points[i + 1].X, points[i + 1].Y, z),
                    Out,
                    material);
            }
        }
    }
}
