using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>What a building style draws into: the mesh, anchors for later systems, and a seeded random.</summary>
    public sealed class BuildContext
    {
        public BuildContext(MeshBuilder builder, ICollection<TownAnchor> anchors, int seed, ICollection<TownDoor> doors = null, MeshBuilder fittings = null)
        {
            Builder = builder;
            Anchors = anchors;
            Doors = doors ?? new List<TownDoor>();
            Fittings = fittings ?? builder;
            Random = new Random(seed);
        }

        public MeshBuilder Builder { get; }

        /// <summary>
        /// Where hanging light fittings go: a mesh of their own that nobody bumps into (or the
        /// building's own mesh when no separate one is given).
        /// </summary>
        public MeshBuilder Fittings { get; }

        public ICollection<TownAnchor> Anchors { get; }

        /// <summary>Doors that open, each built as its own mesh.</summary>
        public ICollection<TownDoor> Doors { get; }

        public Random Random { get; }

        public float Range(float min, float max) => min + ((float)Random.NextDouble() * (max - min));

        public bool Chance(float probability) => Random.NextDouble() < probability;

        public T Pick<T>(IReadOnlyList<T> options) => options[Random.Next(options.Count)];

        public void Anchor(AnchorKind kind, System.Numerics.Vector3 position, System.Numerics.Vector3 facing, float size) =>
            Anchors.Add(new TownAnchor(kind, position, facing, size, Random.Next()));

        /// <summary>
        /// A door that opens. <paramref name="buildLeaf"/> draws the shut leaf, in town coordinates,
        /// into a builder of its own, so it can swing about <paramref name="hinge"/> instead of
        /// being fixed into the building.
        /// </summary>
        /// <param name="axis">What it turns about, if not straight up (a roof window pivots across the slope).</param>
        public void Door(string name, System.Numerics.Vector3 hinge, System.Numerics.Vector3 along, System.Numerics.Vector3 inward, float width, float height, Action<BuildContext> buildLeaf, float openDegrees = 100f, System.Numerics.Vector3? axis = null, string noun = "door")
        {
            var leaf = new BuildContext(new MeshBuilder(), Anchors, Random.Next(), Doors);
            buildLeaf(leaf);
            var mesh = leaf.Builder.Build(name);
            for (var i = 0; i < mesh.Positions.Length; i++)
            {
                mesh.Positions[i] -= hinge;
            }

            Doors.Add(new TownDoor(name, hinge, along, inward, width, height, mesh, openDegrees, axis, noun));
        }
    }
}
