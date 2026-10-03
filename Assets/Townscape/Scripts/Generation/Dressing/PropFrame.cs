using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Dressing
{
    /// <summary>
    /// Local coordinates for building a prop: <c>x</c> to the right as seen from the front,
    /// <c>y</c> up from the ground, <c>z</c> towards the front.
    /// </summary>
    public sealed class PropFrame
    {
        public PropFrame(MeshBuilder builder, ICollection<TownAnchor> anchors, Random random, Vector3 origin, Vector2 facing)
        {
            Builder = builder;
            Anchors = anchors;
            Random = random;
            Origin = origin;
            var forward = GeoMath.SafeNormalize(facing, Vector2.UnitY);
            Forward = GeoMath.At(forward, 0f);
            Right = GeoMath.At(GeoMath.Left(forward), 0f);
        }

        public MeshBuilder Builder { get; }

        public ICollection<TownAnchor> Anchors { get; }

        public Random Random { get; }

        public Vector3 Origin { get; }

        public Vector3 Right { get; }

        public Vector3 Forward { get; }

        public Vector3 Point(float x, float y, float z) => Origin + (Right * x) + (Vector3.UnitY * y) + (Forward * z);

        public float Range(float min, float max) => min + ((float)Random.NextDouble() * (max - min));

        /// <summary>A box centred at (x, y, z) with half sizes along right, up and forward.</summary>
        public void Box(float x, float y, float z, float halfX, float halfY, float halfZ, SurfaceMaterial material, bool bottom = false)
        {
            Builder.AddBox(Point(x, y, z), Right, Vector3.UnitY, Forward, new Vector3(halfX, halfY, halfZ), material, bottom);
        }

        /// <summary>A box between two corners.</summary>
        public void Span(float x0, float y0, float z0, float x1, float y1, float z1, SurfaceMaterial material, bool bottom = false)
        {
            Box((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f, MathF.Abs(x1 - x0) * 0.5f, MathF.Abs(y1 - y0) * 0.5f, MathF.Abs(z1 - z0) * 0.5f, material, bottom);
        }

        public void Prism(float x, float y, float z, float radius, float height, int sides, SurfaceMaterial material, bool bottom = false)
        {
            Builder.AddPrism(Point(x, y, z), radius, height, sides, material, bottom);
        }

        public void Cone(float x, float y, float z, float radius, float height, int sides, SurfaceMaterial material, bool capBase = false)
        {
            Builder.AddCone(Point(x, y, z), radius, height, sides, material, capBase);
        }

        public void Blob(float x, float y, float z, float radiusX, float radiusY, float radiusZ, SurfaceMaterial material, float jitter = 0.15f)
        {
            Builder.AddBlob(Point(x, y, z), new Vector3(radiusX, radiusY, radiusZ), material, Random, jitter);
        }

        /// <summary>A flat panel facing forward (or backward), for signs.</summary>
        public void Panel(float x0, float y0, float x1, float y1, float z, SurfaceMaterial material, bool facingBack = false)
        {
            Builder.AddQuadFacing(Point(x0, y0, z), Point(x1, y0, z), Point(x1, y1, z), Point(x0, y1, z), facingBack ? -Forward : Forward, material);
        }

        public void Anchor(AnchorKind kind, float x, float y, float z, float size)
        {
            Anchors.Add(new TownAnchor(kind, Point(x, y, z), Forward, size, Random.Next()));
        }
    }
}
