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
        public PropFrame(MeshBuilder builder, ICollection<TownAnchor> anchors, Random random, Vector3 origin, Vector2 facing, ICollection<TownDoor> doors = null, ICollection<TownCabinet> cabinets = null)
        {
            Builder = builder;
            Anchors = anchors;
            Doors = doors ?? new List<TownDoor>();
            Cabinets = cabinets ?? new List<TownCabinet>();
            Facing = facing;
            Random = random;
            Origin = origin;
            var forward = GeoMath.SafeNormalize(facing, Vector2.UnitY);
            Forward = GeoMath.At(forward, 0f);
            Right = GeoMath.At(GeoMath.Left(forward), 0f);
        }

        public MeshBuilder Builder { get; }

        public ICollection<TownAnchor> Anchors { get; }

        /// <summary>Doors that open, each built as its own mesh.</summary>
        public ICollection<TownDoor> Doors { get; }

        /// <summary>Street cabinets for the fibre broadband.</summary>
        public ICollection<TownCabinet> Cabinets { get; }

        public Random Random { get; }

        public Vector3 Origin { get; }

        private Vector2 Facing { get; }

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

        /// <summary>
        /// A door that opens, hung at (<paramref name="hingeX"/>, <paramref name="y"/>,
        /// <paramref name="hingeZ"/>) with its latch edge <paramref name="width"/> along
        /// <paramref name="alongX"/> (+1 to the right, -1 to the left). <paramref name="buildLeaf"/>
        /// draws the shut leaf in this frame's coordinates, into a mesh of its own, so it can swing
        /// out towards the front (or in, with <paramref name="outward"/> false).
        /// </summary>
        public void Door(string name, float hingeX, float y, float hingeZ, float alongX, float width, float height, Action<PropFrame> buildLeaf, bool outward = true, float openDegrees = 100f, string noun = "door")
        {
            var leaf = new PropFrame(new MeshBuilder(), Anchors, new Random(Random.Next()), Origin, Facing, Doors, Cabinets);
            buildLeaf(leaf);
            var hinge = Point(hingeX, y, hingeZ);
            var mesh = leaf.Builder.Build(name);
            for (var i = 0; i < mesh.Positions.Length; i++)
            {
                mesh.Positions[i] -= hinge;
            }

            Doors.Add(new TownDoor(name, hinge, Right * MathF.Sign(alongX), outward ? Forward : -Forward, width, height, mesh, openDegrees, noun: noun));
        }
    }
}
