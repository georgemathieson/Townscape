using System;
using System.Collections.Generic;
using Townscape.Generation.Geometry;

namespace Townscape.Generation.Buildings.Parts
{
    /// <summary>What a building style draws into: the mesh, anchors for later systems, and a seeded random.</summary>
    public sealed class BuildContext
    {
        public BuildContext(MeshBuilder builder, ICollection<TownAnchor> anchors, int seed)
        {
            Builder = builder;
            Anchors = anchors;
            Random = new Random(seed);
        }

        public MeshBuilder Builder { get; }

        public ICollection<TownAnchor> Anchors { get; }

        public Random Random { get; }

        public float Range(float min, float max) => min + ((float)Random.NextDouble() * (max - min));

        public bool Chance(float probability) => Random.NextDouble() < probability;

        public T Pick<T>(IReadOnlyList<T> options) => options[Random.Next(options.Count)];

        public void Anchor(AnchorKind kind, System.Numerics.Vector3 position, System.Numerics.Vector3 facing, float size) =>
            Anchors.Add(new TownAnchor(kind, position, facing, size, Random.Next()));
    }
}
